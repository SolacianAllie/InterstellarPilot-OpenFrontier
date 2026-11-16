using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// ED's StarMapStar presentation: catalog stars as textured sprites from
	/// the systemmap_stars sheet (the 512x512 BC1 sheet, repacked here as an
	/// 8x4 atlas of 64px cells). Placements follow the blueprint's density
	/// CDF, so the sprites sit on the same arms the cloud and the sky catalog
	/// do; per-star cells, sizes and tints come from the seed.
	///
	/// Layer 30 only: this is the MAP's presentation of the catalog, exactly
	/// as ED draws textured sprites in the map and the procedural star in the
	/// sky. The map camera includes layer 30 and masks GalaxySkyStars (31);
	/// the in-sky camera does the opposite. Distance and projection never
	/// switch the catalog off, so it survives every zoom and an angled
	/// perspective map preview still shows the map sprites.
	///
	/// Queue offset lives on the MATERIAL (2991); the tag stays a defined name.
	/// </summary>
	[ExecuteAlways]
	[DisallowMultipleComponent]
	public class GalaxyMapStars : MonoBehaviour
	{
		/// <summary>The blueprint whose density field places the sprites.</summary>
		public GalaxyBlueprint blueprint;

		/// <summary>Quality ladder: ED's StarInstanceCount rung (2000/2000/4000).</summary>
		public GalaxyQualitySettings quality;

		[Tooltip("Authored material. Null creates a transient one from OpenFrontier/Galaxy/MapStar.")]
		public Material material;

		[Tooltip("The systemmap_stars atlas (8x4 cells of 64px).")]
		public Texture2D spriteSheet;

		[Header("Map star sprites (px)")]
		public float sizeMin = 8f;
		public float sizeMax = 22f;
		public float brightness = 2f;

		const string ShaderName = "OpenFrontier/Galaxy/MapStar";
		const int PlacementGrid = 128;
		const int FloatsPerInstance = 12; // pos.xyz + cell | color.rgba | params(jitter, alpha, 0, 0)
		const int InstanceStride = 48;
		const int MaxStars = 20000;
		const int AtlasColumns = 8;
		const int AtlasRows = 4;
		// GalaxyStarSprites cells 9 and 13 are square nebula frames, not
		// point stars. Map sprites are 8-22 px, so one of those would read as
		// a cloud pasted into the catalog. Pick from the 30 star cells only.
		const int NebulaCellA = 9;
		const int NebulaCellB = 13;
		// Explicit map presentation layer: MAP cameras see it, the in-sky
		// Galaxy Camera masks it out. Camera distance must never switch the
		// catalog layer off - ED's map shows it at every zoom.
		const int MapStarLayer = 30;

		static readonly int InstanceDataId = Shader.PropertyToID("_InstanceData");
		static readonly int GalaxyToWorldId = Shader.PropertyToID("_GalaxyToWorld");
		static readonly int SpriteTexId = Shader.PropertyToID("_SpriteTex");
		static readonly int SizeMinId = Shader.PropertyToID("_SizeMin");
		static readonly int SizeMaxId = Shader.PropertyToID("_SizeMax");
		static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
		static readonly int TintId = Shader.PropertyToID("_Tint");

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
			ApplyQuality();
			Reseed(blueprint != null ? blueprint.seed : 1);
		}

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

			if (_mpb == null) _mpb = new MaterialPropertyBlock();
			_mpb.SetMatrix(GalaxyToWorldId, transform.localToWorldMatrix);
			_mpb.SetBuffer(InstanceDataId, _instanceBuffer);

			Graphics.DrawMeshInstancedIndirect(_quad, 0, mat, ComputeBounds(), _argsBuffer, 0, _mpb,
				ShadowCastingMode.Off, false, MapStarLayer);
		}

		/// <summary>
		/// Regenerates the sprites for a seed: 128^2 density CDF once, then
		/// count placements with in-plane position, vertical sigma, atlas
		/// cell, size jitter and a warm/cool tint. CPU-only; one upload.
		/// </summary>
		public void Reseed(int seed)
		{
			_settings = quality != null ? quality : GalaxyQualitySettings.Default;
			int count = Mathf.Clamp(_settings.mapStarSprites, 0, MaxStars);
			if (count != _capacity) RebuildBuffers(count);
			if (_capacity <= 0 || blueprint == null || _quad == null || _argsBuffer == null) return;

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
					if (r01 < 1f) d *= 1f - Mathf.SmoothStep(0.96f, 1f, r01);
					weights[y * PlacementGrid + x] = d;
					if (d > maxWeight) maxWeight = d;
				}
			}
			if (maxWeight <= 0f)
			{
				// empty blueprint: draw nothing and zero the args so a stale
				// instance count can never draw garbage
				_argsBuffer.SetData(new uint[5] { 6u, 0u, 0u, 0u, 0u });
				return;
			}
			for (int i = 0; i < weights.Length; i++)
				cdf[i + 1] = cdf[i] + weights[i];
			double total = cdf[weights.Length];

			var rng = new System.Random(seed ^ 0x5eed51a7);
			Color warm = new Color(1.0f, 0.85f, 0.65f);
			Color cool = new Color(0.70f, 0.82f, 1.0f);

			for (int i = 0; i < _capacity; i++)
			{
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

				double u1 = Math.Max(rng.NextDouble(), 1e-12);
				double u2 = rng.NextDouble();
				float y = (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2))
					* blueprint.VerticalSigma(r01);
				y = Mathf.Clamp(y, -blueprint.radius * 0.5f, blueprint.radius * 0.5f);

				// tint: three-roll average concentrates around white
				float ci = (float)((rng.NextDouble() + rng.NextDouble() + rng.NextDouble()) / 1.5 - 1.0);
				Color colour = ci < 0f
					? Color.Lerp(Color.white, warm, -ci)
					: Color.Lerp(Color.white, cool, ci);

				// size/alpha: most of the 2k map sprites stay small, with a
				// sparse bright tail. The old independent rolls made every
				// star roughly the same dot at the same brightness.
				float sizeJitter = Mathf.Pow((float)rng.NextDouble(), 1.8f);
				if (rng.NextDouble() < 0.035f) sizeJitter = 1f;
				float alphaJitter = (0.45f + 0.55f * Mathf.Sqrt(sizeJitter))
					* (0.72f + 0.28f * (float)rng.NextDouble());
				int cell = NextStarCell(rng);
				int b = i * FloatsPerInstance;
				_floats[b + 0] = px * blueprint.radius;
				_floats[b + 1] = y;
				_floats[b + 2] = pz * blueprint.radius;
				_floats[b + 3] = cell;
				_floats[b + 4] = colour.r;
				_floats[b + 5] = colour.g;
				_floats[b + 6] = colour.b;
				_floats[b + 7] = 1f;
				_floats[b + 8] = sizeJitter;
				_floats[b + 9] = alphaJitter;
				_floats[b + 10] = 0f;
				_floats[b + 11] = 0f;
			}

			_instanceBuffer.SetData(_floats, 0, 0, _capacity * FloatsPerInstance);
			_argsBuffer.SetData(new uint[5] { 6u, (uint)_capacity, 0u, 0u, 0u });
		}

		/// <summary>One of the 30 star cells, skipping the two nebula frames.</summary>
		static int NextStarCell(System.Random rng)
		{
			int cell = rng.Next(0, AtlasColumns * AtlasRows - 2);
			if (cell >= NebulaCellA) cell++;
			if (cell >= NebulaCellB) cell++;
			return cell;
		}

		/// <summary>Edit-mode preview of inspector tuning, debounced (see GalaxySkyStars).</summary>
		void OnValidate()
		{
#if UNITY_EDITOR
			ScheduleEditApply();
#endif
		}

#if UNITY_EDITOR
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

		[ContextMenu("Reseed From Blueprint")]
		public void ReseedFromBlueprint()
		{
			Reseed(blueprint != null ? blueprint.seed : 1);
		}

		/// <summary>Per-jump reseed entry - the generation trigger calls this.</summary>
		public void ReseedRandom()
		{
			if (blueprint != null) blueprint.seed = Environment.TickCount;
			Reseed(blueprint != null ? blueprint.seed : Environment.TickCount);
		}

		/// <summary>Re-reads the quality asset's star count knobs into the material.</summary>
		public void ApplyQuality()
		{
			_settings = quality != null ? quality : GalaxyQualitySettings.Default;
			Material mat = ResolveMaterial();
			if (mat == null) return;
			if (spriteSheet != null) mat.SetTexture(SpriteTexId, spriteSheet);
			mat.SetFloat(SizeMinId, Mathf.Max(sizeMin, 1f));
			mat.SetFloat(SizeMaxId, Mathf.Max(sizeMax, sizeMin));
			mat.SetFloat(BrightnessId, Mathf.Max(brightness, 0f));
			mat.SetColor(TintId, Color.white);
		}

		public void ApplyQualityAndReseed()
		{
			ApplyQuality();
			Reseed(blueprint != null ? blueprint.seed : 1);
		}

		/// <summary>Per-jump reseed of every map-star layer in the scene (event time only).</summary>
		public static void ReseedAllRandom()
		{
			GalaxyMapStars[] layers = FindObjectsByType<GalaxyMapStars>(FindObjectsSortMode.None);
			for (int i = 0; i < layers.Length; i++) layers[i].ReseedRandom();
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
			var m = new Mesh { name = "GalaxyMapStarQuad" };
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
			// Shader-placed billboards: keep the unit proxy from being culled
			// by the angled perspective map camera outside the proxy's origin.
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
			Debug.LogWarning($"[GalaxyMapStars] '{name}': no blueprint assigned - the map stars will not draw. Run OpenFrontier/Galaxy/Install ED Renderer.", this);
		}

		void WarnNoShader()
		{
			if (_warnedNoShader) return;
			_warnedNoShader = true;
			Debug.LogWarning($"[GalaxyMapStars] '{name}': shader '{ShaderName}' not found - is OpenFrontier_GalaxyMapStar.shader imported?", this);
		}
	}
}
