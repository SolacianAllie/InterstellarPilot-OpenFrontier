using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// The explorable miniature's star catalog - ED's SkyboxStars rebuilt for
	/// a galaxy you can fly through.
	///
	/// One draw of REAL 3D stars: positions are sampled from the blueprint's
	/// density field (disc CDF + vertical sigma, exactly like the billboard
	/// cloud), so the catalog shares the galaxy's structure and the two can
	/// never disagree. Each star carries an ABSOLUTE magnitude and colour;
	/// the vertex shader turns that into an APPARENT magnitude from the
	/// camera distance (ED's distance modulus, scaled to the miniature),
	/// then runs the recovered SkyboxStar curve - magnitude thresholds,
	/// radius/alpha exponents, super-bright branch, pixel clamps - to size
	/// and shade the sprite.
	///
	/// That is what makes the galaxy readable at every range: from far away
	/// most stars fall below the visibility floor and the catalog collapses
	/// into the band; flying toward a star it brightens and grows to the
	/// pixel clamp. This component is the SKY half only (layer 31); the map's
	/// textured StarMapStar sheet is the separate GalaxyMapStars layer (30),
	/// gated by the cameras' culling masks - distance never switches layers.
	///
	/// The sprite is ED's procedural star shape: halo, spike and ring are
	/// arithmetic. ED's shipped local-star family also has textured variants
	/// (their pixel shaders declare Texture2D resources); this rebuild keeps
	/// the arithmetic shape because the explorable miniature's stars are real
	/// 3D positions, not a flat star sheet.
	/// </summary>
	[ExecuteAlways]
	[DisallowMultipleComponent]
	public class GalaxySkyStars : MonoBehaviour
	{
		/// <summary>The blueprint whose density field places the catalog.</summary>
		public GalaxyBlueprint blueprint;

		/// <summary>Quality ladder: ED's StarInstanceCount rung (2000/2000/4000 -> 2500/5000/8000 here).</summary>
		public GalaxyQualitySettings quality;

		[Tooltip("Optional authored material. Null creates a transient material from OpenFrontier/Galaxy/SkyStar.")]
		public Material material;

		[Header("Miniature distance modulus")]
		[Tooltip("Distance (world units) that plays 10 parsecs: a star at this distance gains no magnitude from range. The miniature galaxy is not to scale; 1000 keeps the brightest catalog stars resolvable from the default sky camera instead of dimming every one below the cap.")]
		public float tenParsecDistance = 1000f;

		[Header("Catalog luminosity")]
		[Tooltip("Absolute magnitude range: rare-bright .. common-dim (lower = brighter).")]
		public float absMagBrightest = -4f;
		public float absMagDimmest = 7f;
		[Tooltip("Bias toward dim stars (higher = more faint stars, ED's 'many faint, few bright').")]
		[Range(0.5f, 4f)] public float luminosityBias = 2.4f;

		const string ShaderName = "OpenFrontier/Galaxy/SkyStar";
		const int PlacementGrid = 128;
		const int FloatsPerInstance = 12; // pos.xyz + absMag | color.rgba | params(jitter, seed, 0, 0)
		const int InstanceStride = 48;
		const int MaxStars = 200000;
		// Explicit sky presentation layer: the in-sky Galaxy Camera sees it,
		// MAP cameras mask it out. The map and sky are two render contexts,
		// not two camera distances.
		const int SkyStarLayer = 31;

		static readonly int InstanceDataId = Shader.PropertyToID("_InstanceData");
		static readonly int GalaxyToWorldId = Shader.PropertyToID("_GalaxyToWorld");
		static readonly int TenParsecId = Shader.PropertyToID("_TenParsecDistance");
		static readonly int DimmestMagId = Shader.PropertyToID("_DimmestMag");
		static readonly int CappedMagId = Shader.PropertyToID("_CappedMag");
		static readonly int SuperBrightMagId = Shader.PropertyToID("_SuperBrightMag");
		static readonly int SizeMinId = Shader.PropertyToID("_SizeMin");
		static readonly int SizeMaxId = Shader.PropertyToID("_SizeMax");
		static readonly int StarRadPowId = Shader.PropertyToID("_StarRadPow");
		static readonly int StarAlphaPowId = Shader.PropertyToID("_StarAlphaPow");
		static readonly int StarBrightnessId = Shader.PropertyToID("_StarBrightness");
		static readonly int StarSizeScaleId = Shader.PropertyToID("_StarSizeScale");
		static readonly int HaloStartId = Shader.PropertyToID("_HaloStart");
		static readonly int HaloStrengthId = Shader.PropertyToID("_HaloStrength");
		static readonly int SpikeStartId = Shader.PropertyToID("_SpikeStart");
		static readonly int SpikeStrengthId = Shader.PropertyToID("_SpikeStrength");
		static readonly int RingStartId = Shader.PropertyToID("_RingStart");
		static readonly int RingStrengthId = Shader.PropertyToID("_RingStrength");
		static readonly int MinVisAlphaId = Shader.PropertyToID("_MinVisAlpha");

		Mesh _quad;
		ComputeBuffer _instanceBuffer;
		ComputeBuffer _argsBuffer;
		MaterialPropertyBlock _mpb;
		Material _fallbackMaterial;
		float[] _floats;
		int _capacity;
		GalaxyQualitySettings _settings;
		bool _warnedNoBlueprint;
		bool _warnedNoShader;

		void OnEnable()
		{
#if UNITY_EDITOR
			GalaxyBlueprint.Changed += ScheduleEditApply;
			GalaxyQualitySettings.Changed += ScheduleEditApply;
#endif
			EnsureSetup();
			_settings = quality != null ? quality : GalaxyQualitySettings.Default;
			_settings.ApplyToShaders();
			ApplyQuality();
			Reseed(blueprint != null ? blueprint.seed : 1);
		}

		/// <summary>
		/// Edit-mode preview: [ExecuteAlways] already draws the catalog in the
		/// Scene view; this only defers inspector tuning so dragging a slider
		/// does not reseed on every keystroke. In play the graphics UI calls
		/// ApplyQualityAndReseed directly.
		/// </summary>
		void OnValidate()
		{
#if UNITY_EDITOR
			ScheduleEditApply();
#endif
		}

#if UNITY_EDITOR
		/// <summary>Debounced preview: asset edits schedule one reseed, not one per keystroke.</summary>
		void ScheduleEditApply()
		{
			if (Application.isPlaying || !isActiveAndEnabled) return;
			GalaxyEditPreview.Schedule(ApplyEditPreview, 0.4f);
		}

		void ApplyEditPreview()
		{
			if (isActiveAndEnabled) ApplyQualityAndReseed();
		}
#endif

		void OnDisable()
		{
#if UNITY_EDITOR
			GalaxyBlueprint.Changed -= ScheduleEditApply;
			GalaxyQualitySettings.Changed -= ScheduleEditApply;
			GalaxyEditPreview.Cancel(ApplyEditPreview);
#endif
			ReleaseAll();
		}

		void Update()
		{
			if (_instanceBuffer == null || _argsBuffer == null || _capacity <= 0) return;
			if (blueprint == null)
			{
				WarnNoBlueprint();
				return;
			}
			Material mat = ResolveMaterial();
			if (mat == null) return;

			// lazy init: no native calls in field initializers (house rule)
			if (_mpb == null) _mpb = new MaterialPropertyBlock();
			_mpb.SetMatrix(GalaxyToWorldId, transform.localToWorldMatrix);
			_mpb.SetBuffer(InstanceDataId, _instanceBuffer);

			Graphics.DrawMeshInstancedIndirect(_quad, 0, mat, ComputeBounds(), _argsBuffer, 0, _mpb,
				ShadowCastingMode.Off, false, SkyStarLayer);
		}

		/// <summary>
		/// Regenerates the catalog for a seed: a 128^2 density CDF once, then
		/// count placements (disc + vertical sigma) with magnitude and colour
		/// draws. CPU-only, a few ms; the whole cost is one buffer upload.
		/// </summary>
		public void Reseed(int seed)
		{
			_settings = quality != null ? quality : GalaxyQualitySettings.Default;
			int count = Mathf.Clamp(_settings.skyStarCount, 0, MaxStars);
			if (count != _capacity) RebuildBuffers(count);
			if (_capacity <= 0 || blueprint == null || _quad == null || _argsBuffer == null) return;

			// ---- placement CDF: the blueprint's density on a 128^2 grid,
			// round disc mask + soft rim exactly like the cloud's grid
			float[] weights = new float[PlacementGrid * PlacementGrid];
			double[] cdf = new double[weights.Length + 1];
			float maxWeight = 0f;
			for (int y = 0; y < PlacementGrid; y++)
			{
				for (int x = 0; x < PlacementGrid; x++)
				{
					float cx = (x + 0.5f) / PlacementGrid * 2f - 1f;
					float cz = (y + 0.5f) / PlacementGrid * 2f - 1f;
					float r01 = Mathf.Sqrt(cx * cx + cz * cz);
					float d = r01 >= 1f ? 0f : blueprint.SampleDensity(cx, cz);
					if (r01 >= 1f) d = 0f;
					else d *= 1f - Mathf.SmoothStep(0.96f, 1f, r01);
					weights[y * PlacementGrid + x] = d;
					if (d > maxWeight) maxWeight = d;
				}
			}
			if (maxWeight <= 0f)
			{
				// empty blueprint (e.g. an all-black map): draw nothing, and
				// zero the args so a stale instance count can't draw garbage
				_argsBuffer.SetData(new uint[5] { 6u, 0u, 0u, 0u, 0u });
				return;
			}
			for (int i = 0; i < weights.Length; i++)
				cdf[i + 1] = cdf[i] + weights[i];
			double total = cdf[weights.Length];

			var rng = new System.Random(seed ^ 0x1a2b3c4d);
			Color warm = new Color(1.0f, 0.70f, 0.45f);
			Color cool = new Color(0.62f, 0.76f, 1.0f);
			float magSpan = Mathf.Max(absMagDimmest - absMagBrightest, 0.1f);

			for (int i = 0; i < _capacity; i++)
			{
				// ---- sample the disc: invert the CDF, jitter inside the cell
				double u = rng.NextDouble() * total;
				int lo = 0, hi = weights.Length;
				while (hi - lo > 1)
				{
					int mid = (lo + hi) >> 1;
					if (cdf[mid] <= u) lo = mid; else hi = mid;
				}
				int cellX = lo % PlacementGrid;
				int cellY = lo / PlacementGrid;
				float fx01 = (cellX + (float)rng.NextDouble()) / PlacementGrid;
				float fy01 = (cellY + (float)rng.NextDouble()) / PlacementGrid;
				float px = fx01 * 2f - 1f;
				float pz = fy01 * 2f - 1f;
				float r01 = Mathf.Sqrt(px * px + pz * pz);

				// ---- vertical: box-muller gaussian, sigma swells over the
				// bulge (the same rule the cloud's points follow)
				double u1 = Math.Max(rng.NextDouble(), 1e-12);
				double u2 = rng.NextDouble();
				float y = (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2))
					* blueprint.VerticalSigma(r01);
				y = Mathf.Clamp(y, -blueprint.radius * 0.5f, blueprint.radius * 0.5f);

				// ---- luminosity: many faint, few bright; the shader adds
				// the distance term (ED's apparent magnitude)
				float absMag = absMagDimmest - Mathf.Pow((float)rng.NextDouble(), luminosityBias) * magSpan;

				// ---- colour index: -1 warm, 0 white, +1 cool; three-roll
				// average concentrates around white
				float ci = (float)((rng.NextDouble() + rng.NextDouble() + rng.NextDouble()) / 1.5 - 1.0);
				Color colour = ci < 0f
					? Color.Lerp(Color.white, warm, -ci)
					: Color.Lerp(Color.white, cool, ci);
				colour *= 0.75f + 0.25f * (float)rng.NextDouble();

				int b = i * FloatsPerInstance;
				_floats[b + 0] = px * blueprint.radius;
				_floats[b + 1] = y;
				_floats[b + 2] = pz * blueprint.radius;
				_floats[b + 3] = absMag;
				_floats[b + 4] = colour.r;
				_floats[b + 5] = colour.g;
				_floats[b + 6] = colour.b;
				_floats[b + 7] = 1f;
				_floats[b + 8] = 0.88f + 0.24f * (float)rng.NextDouble();
				_floats[b + 9] = (float)rng.NextDouble();
				_floats[b + 10] = 0f;
				_floats[b + 11] = 0f;
			}

			_instanceBuffer.SetData(_floats, 0, 0, _capacity * FloatsPerInstance);
			// indirect args: indexCount, instanceCount, firstIndex, baseVertex, firstInstance
			_argsBuffer.SetData(new uint[5] { 6u, (uint)_capacity, 0u, 0u, 0u });
		}

		[ContextMenu("Reseed From Blueprint")]
		public void ReseedFromBlueprint()
		{
			Reseed(blueprint != null ? blueprint.seed : 1);
		}

		/// <summary>Per-jump reseed entry - the generation trigger calls this.</summary>
		public void ReseedRandom()
		{
			Reseed(Environment.TickCount);
		}

		[ContextMenu("Reseed Random (Per Jump)")]
		public void ReseedRandomMenu()
		{
			ReseedRandom();
		}

		/// <summary>
		/// Re-reads the quality asset's star knobs into the material. Call
		/// after a graphics-menu change; the COUNT needs a reseed - call
		/// ApplyQualityAndReseed for that.
		/// </summary>
		public void ApplyQuality()
		{
			_settings = quality != null ? quality : GalaxyQualitySettings.Default;
			Material mat = ResolveMaterial();
			if (mat == null) return;
			mat.SetFloat(TenParsecId, Mathf.Max(tenParsecDistance, 1f));
			mat.SetFloat(DimmestMagId, _settings.starDimmestMag);
			mat.SetFloat(CappedMagId, _settings.starCappedMag);
			mat.SetFloat(SuperBrightMagId, _settings.starSuperBrightMag);
			mat.SetFloat(SizeMinId, _settings.starSizeMin);
			mat.SetFloat(SizeMaxId, _settings.starSizeMax);
			mat.SetFloat(StarRadPowId, _settings.starRadPow);
			mat.SetFloat(StarAlphaPowId, _settings.starAlphaPow);
			mat.SetFloat(StarBrightnessId, _settings.skyStarBrightness);
			mat.SetFloat(StarSizeScaleId, _settings.skyStarSizeScale);
			mat.SetFloat(HaloStartId, _settings.haloStart);
			mat.SetFloat(HaloStrengthId, _settings.haloStrength);
			mat.SetFloat(SpikeStartId, _settings.spikeStart);
			mat.SetFloat(SpikeStrengthId, _settings.spikeStrength);
			mat.SetFloat(RingStartId, _settings.ringStart);
			mat.SetFloat(RingStrengthId, _settings.ringStrength);
			mat.SetFloat(MinVisAlphaId, _settings.minVisAlpha);
		}

		public void ApplyQualityAndReseed()
		{
			ApplyQuality();
			Reseed(blueprint != null ? blueprint.seed : 1);
		}

		/// <summary>
		/// The per-jump entry point for M7's generation code: reseeds every
		/// catalog in the scene ONCE, at event time - a scene scan belongs to
		/// events, never the render path (house rule).
		/// </summary>
		public static void ReseedAllRandom()
		{
			GalaxySkyStars[] catalogs = FindObjectsByType<GalaxySkyStars>(FindObjectsSortMode.None);
			for (int i = 0; i < catalogs.Length; i++) catalogs[i].ReseedRandom();
		}

		void EnsureSetup()
		{
			if (_quad == null) _quad = BuildQuad();
			if (_argsBuffer == null) _argsBuffer = new ComputeBuffer(1, 20, ComputeBufferType.IndirectArguments);
		}

		Material ResolveMaterial()
		{
			if (material != null) return material;
			if (_fallbackMaterial == null)
			{
				Shader shader = Shader.Find(ShaderName);
				if (shader == null)
				{
					WarnNoShader();
					return null;
				}
				// transient convenience material - never written back to the field
				_fallbackMaterial = new Material(shader);
				ApplyQuality();
			}
			return _fallbackMaterial;
		}

		void RebuildBuffers(int count)
		{
			if (_instanceBuffer != null)
			{
				_instanceBuffer.Release();
				_instanceBuffer = null;
			}
			_capacity = count;
			if (count <= 0) return;
			_instanceBuffer = new ComputeBuffer(count, InstanceStride);
			_floats = new float[count * FloatsPerInstance];
		}

		Bounds ComputeBounds()
		{
			if (blueprint == null) return new Bounds(transform.position, Vector3.one);
			Vector3 s = transform.lossyScale;
			float maxScale = Mathf.Max(Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y)), Mathf.Abs(s.z));
			float r = blueprint.radius * maxScale;
			float halfHeight = Mathf.Max(blueprint.thickness * (1f + blueprint.bulgeThicknessBoost) * 3f, 10f) * maxScale;
			return new Bounds(transform.position, new Vector3(r * 2f, halfHeight * 2f, r * 2f));
		}

		static Mesh BuildQuad()
		{
			var m = new Mesh { name = "GalaxySkyStarQuad" };
			m.vertices = new Vector3[4]
			{
				new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
				new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
			};
			m.uv = new Vector2[4]
			{
				new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f)
			};
			m.triangles = new int[6] { 0, 2, 1, 2, 3, 1 };
			// Shader-placed billboards: a unit quad's implicit bounds at the
			// galaxy origin are frustum-culled from a perspective sky camera.
			m.bounds = new Bounds(Vector3.zero, Vector3.one * 1e8f);
			return m;
		}

		void ReleaseAll()
		{
			if (_instanceBuffer != null) { _instanceBuffer.Release(); _instanceBuffer = null; }
			if (_argsBuffer != null) { _argsBuffer.Release(); _argsBuffer = null; }
			if (_quad != null)
			{
				if (Application.isPlaying) Destroy(_quad); else DestroyImmediate(_quad);
				_quad = null;
			}
			_capacity = 0;
			_floats = null;
			_fallbackMaterial = null;
		}

		void WarnNoBlueprint()
		{
			if (_warnedNoBlueprint) return;
			_warnedNoBlueprint = true;
			Debug.LogWarning($"[GalaxySkyStars] '{name}': no blueprint assigned - the catalog will not draw. Run OpenFrontier/Galaxy/Install ED Renderer.", this);
		}

		void WarnNoShader()
		{
			if (_warnedNoShader) return;
			_warnedNoShader = true;
			Debug.LogWarning($"[GalaxySkyStars] '{name}': shader '{ShaderName}' not found - is OpenFrontier_GalaxySkyStar.shader imported?", this);
		}
	}
}
