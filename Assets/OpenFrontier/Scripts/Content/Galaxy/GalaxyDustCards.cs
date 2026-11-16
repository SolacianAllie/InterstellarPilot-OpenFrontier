using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// ED's LocalDarkNebulaDust as cards (spec §6b): ONE indirect draw of
	/// camera-facing, alpha-blended quads sitting in the galactic plane. The
	/// layer that makes close-up dust read as cloud rather than as a flat
	/// band, and the cheapest mobile win in the stack - one draw call, no
	/// raymarch, a shared 256^2 tiling noise map generated on the CPU once
	/// per reseed (every card samples it; no per-card textures).
	///
	/// Counts and strength come from the quality ladder:
	/// localDustCards 24/40/60 and localDustBrightness 0.13/0.033/0.0 -
	/// ED's LocalDustBrightness is a compensation knob (findings §3.9) that
	/// falls to exactly zero as the real nebula volumes rise, so at Hero
	/// this component draws nothing (Update exits before the draw).
	///
	/// Positions are density-weighted (blueprint.SampleDensity rejection),
	/// off-plane height runs from the blueprint's vertical sigma, so the
	/// cards live in the arms and bulge where the dust actually is.
	/// </summary>
	[ExecuteAlways]
	[DisallowMultipleComponent]
	public class GalaxyDustCards : MonoBehaviour
	{
		/// <summary>The blueprint the cards follow.</summary>
		public GalaxyBlueprint blueprint;

		/// <summary>Quality ladder: card count + strength (ED's compensation knob).</summary>
		public GalaxyQualitySettings quality;

		[Tooltip("Optional authored material. Null creates a transient material from OpenFrontier/Galaxy/DustCard.")]
		public Material material;

		[Header("Card look")]
		[Tooltip("Noise tiling across a card (Repeat-wrapped).")]
		public float noiseScale = 2.2f;

		[Tooltip("Card diameter range, world units (bulge cards run larger).")]
		public float cardSizeMin = 120f;
		public float cardSizeMax = 380f;

		[Tooltip("Peak alpha of a card before the noise/silhouette shaping. Keep low; they overlap.")]
		[Range(0f, 1f)] public float cardAlpha = 0.5f;

		[Tooltip("How far off the plane a card may sit, as a multiple of the blueprint's vertical sigma.")]
		public float planeThicken = 1.5f;

		const string CardShaderName = "OpenFrontier/Galaxy/DustCard";
		const int NoiseSize = 256;
		const int FloatsPerInstance = 12; // pos.xyz + size | color.rgba | rotSeed(rot, seed, 0, 0)
		const int InstanceStride = 48;
		const int MaxCards = 256;
		const int MaxTriesPerCard = 16;

		static readonly int LocalToWorldId = Shader.PropertyToID("_LocalToWorld");
		static readonly int InstanceDataId = Shader.PropertyToID("_InstanceData");
		static readonly int NoiseMapId = Shader.PropertyToID("_NoiseMap");
		static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
		static readonly int CardStrengthId = Shader.PropertyToID("_CardStrength");
		static readonly int DustColourId = Shader.PropertyToID("_DustColour");

		Mesh _quad;
		ComputeBuffer _instanceBuffer;
		ComputeBuffer _argsBuffer;
		MaterialPropertyBlock _mpb;
		Material _fallbackMaterial;
		Texture2D _noise;
		float[] _instanceFloats;
		int _capacity;
		float _strength;
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
			ApplyQuality();
			if (blueprint != null) Reseed(blueprint.seed);
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
			// ED's compensation knob: at Hero the strength is 0.0 and the
			// layer is off - not a zero-instance draw, no submit at all.
			if (_strength <= 0f) return;
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
			_mpb.SetMatrix(LocalToWorldId, transform.localToWorldMatrix);
			_mpb.SetBuffer(InstanceDataId, _instanceBuffer);
			_mpb.SetTexture(NoiseMapId, _noise);
			_mpb.SetFloat(NoiseScaleId, Mathf.Max(0.25f, noiseScale));
			_mpb.SetFloat(CardStrengthId, _strength);
			_mpb.SetColor(DustColourId, _settings.dustColour);

			Graphics.DrawMeshInstancedIndirect(_quad, 0, mat, ComputeBounds(), _argsBuffer, 0, _mpb,
				ShadowCastingMode.Off, false, gameObject.layer);
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

		/// <summary>
		/// Regenerates the cards (positions, sizes, noise offsets) and the
		/// shared noise map from the blueprint with the given seed. The
		/// whole cost is one CPU pass plus one buffer upload.
		/// </summary>
		public void Reseed(int seed)
		{
			_settings = quality != null ? quality : GalaxyQualitySettings.Default;
			int count = Mathf.Clamp(_settings.localDustCards, 0, MaxCards);
			if (count != _capacity) RebuildBuffers(count);
			if (_capacity <= 0 || blueprint == null || _quad == null || _argsBuffer == null) return;

			BuildNoise(seed);

			var rng = new System.Random(seed ^ 0x5f3759df);
			Color dustCol = _settings.dustColour;

			for (int i = 0; i < _capacity; i++)
			{
				// ---- position: uniform disc point accepted by the density
				// field, so the cards sit where the galaxy is actually dense
				float px = 0f, pz = 0f, d = 0f;
				for (int t = 0; t < MaxTriesPerCard; t++)
				{
					float r = Mathf.Sqrt((float)rng.NextDouble());
					float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
					px = Mathf.Cos(ang) * r;
					pz = Mathf.Sin(ang) * r;
					d = blueprint.SampleDensity(px, pz);
					if (rng.NextDouble() < Mathf.Clamp01(d * 1.2f)) break;
				}
				float r01 = Mathf.Sqrt(px * px + pz * pz);

				// ---- height: gaussian around the plane, sigma from the
				// blueprint (swollen over the bulge, like the cloud)
				double u1 = Math.Max(rng.NextDouble(), 1e-12);
				double u2 = rng.NextDouble();
				float y = (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2))
					* blueprint.VerticalSigma(r01) * planeThicken;
				y = Mathf.Clamp(y, -blueprint.radius * 0.25f, blueprint.radius * 0.25f);

				// ---- size: bulge cards run larger, matching the cloud's rule
				float size = Mathf.Lerp(cardSizeMin, cardSizeMax, (float)rng.NextDouble());
				size *= 0.8f + 0.7f * Mathf.Exp(-Mathf.Pow(r01 / Mathf.Max(blueprint.bulgeRadiusFraction, 0.001f), 2f));

				int b = i * FloatsPerInstance;
				_instanceFloats[b + 0] = px * blueprint.radius;
				_instanceFloats[b + 1] = y;
				_instanceFloats[b + 2] = pz * blueprint.radius;
				_instanceFloats[b + 3] = size;
				float bright = 0.8f + 0.4f * (float)rng.NextDouble();
				_instanceFloats[b + 4] = dustCol.r * bright;
				_instanceFloats[b + 5] = dustCol.g * bright;
				_instanceFloats[b + 6] = dustCol.b * bright;
				_instanceFloats[b + 7] = cardAlpha * (0.6f + 0.4f * (float)rng.NextDouble());
				_instanceFloats[b + 8] = (float)rng.NextDouble() * Mathf.PI * 2f;
				_instanceFloats[b + 9] = (float)rng.NextDouble() * 32f;
				_instanceFloats[b + 10] = 0f;
				_instanceFloats[b + 11] = 0f;
			}

			_instanceBuffer.SetData(_instanceFloats, 0, 0, _capacity * FloatsPerInstance);
			// indirect args: indexCount, instanceCount, firstIndex, baseVertex, firstInstance
			_argsBuffer.SetData(new uint[5] { 6u, (uint)_capacity, 0u, 0u, 0u });
		}

		/// <summary>Per-jump reseed entry - the generation trigger calls this.</summary>
		public void ReseedRandom()
		{
			Reseed(Environment.TickCount);
		}

		[ContextMenu("Reseed From Blueprint")]
		public void ReseedFromBlueprint()
		{
			Reseed(blueprint != null ? blueprint.seed : 1);
		}

		[ContextMenu("Reseed Random (Per Jump)")]
		public void ReseedRandomMenu()
		{
			ReseedRandom();
		}

		/// <summary>
		/// Re-reads the ladder's strength (localDustBrightness). A rung change
		/// also changes the count, so UI calls ApplyQualityAndReseed.
		/// </summary>
		public void ApplyQuality()
		{
			_settings = quality != null ? quality : GalaxyQualitySettings.Default;
			_strength = _settings.localDustBrightness;
		}

		public void ApplyQualityAndReseed()
		{
			ApplyQuality();
			if (blueprint != null) Reseed(blueprint.seed);
		}

		/// <summary>
		/// The per-jump entry point for M7's generation code: reseeds every
		/// card field in the scene ONCE, at event time - a scene scan belongs
		/// to events, never the render path (house rule).
		/// </summary>
		public static void ReseedAllRandom()
		{
			GalaxyDustCards[] fields = FindObjectsByType<GalaxyDustCards>(FindObjectsSortMode.None);
			for (int i = 0; i < fields.Length; i++) fields[i].ReseedRandom();
		}

		/// <summary>
		/// The shared 256^2 tiling value-noise map (three octaves, Repeat
		/// wrap). One texture for every card; ED's pNoiseMapTexture plays the
		/// same role (findings §3.6). The shader samples it at three scales
		/// as well, so the silhouette has structure at both card and detail
		/// frequencies.
		/// </summary>
		void BuildNoise(int seed)
		{
			if (_noise == null)
			{
				_noise = new Texture2D(NoiseSize, NoiseSize, TextureFormat.RGBA32, false, true)
				{
					name = "GalaxyDustNoise",
					wrapMode = TextureWrapMode.Repeat,
					filterMode = FilterMode.Bilinear,
					hideFlags = HideFlags.HideAndDontSave
				};
			}
			var pixels = new Color32[NoiseSize * NoiseSize];
			for (int y = 0; y < NoiseSize; y++)
			{
				for (int x = 0; x < NoiseSize; x++)
				{
					float u = x / (float)NoiseSize;
					float v = y / (float)NoiseSize;
					float n = TilingNoise(u * 4f, v * 4f, seed) * 0.55f
						+ TilingNoise(u * 12f, v * 12f, seed + 91) * 0.30f
						+ TilingNoise(u * 28f, v * 28f, seed + 173) * 0.15f;
					byte b = (byte)(Mathf.Clamp01(n) * 255f);
					pixels[y * NoiseSize + x] = new Color32(b, b, b, 255);
				}
			}
			_noise.SetPixels32(pixels);
			_noise.Apply(false, true);
		}

		/// <summary>Value noise that wraps at <see cref="NoiseSize"/> cells, so Repeat-sampled lookups never seam.</summary>
		static float TilingNoise(float x, float z, int seed)
		{
			int xi = Mathf.FloorToInt(x);
			int zi = Mathf.FloorToInt(z);
			float fx = x - xi;
			float fz = z - zi;
			fx = fx * fx * (3f - 2f * fx);
			fz = fz * fz * (3f - 2f * fz);
			int x0 = ((xi % NoiseSize) + NoiseSize) % NoiseSize;
			int z0 = ((zi % NoiseSize) + NoiseSize) % NoiseSize;
			int x1 = (x0 + 1) % NoiseSize;
			int z1 = (z0 + 1) % NoiseSize;
			float a = Hash(x0, z0, seed);
			float b = Hash(x1, z0, seed);
			float c = Hash(x0, z1, seed);
			float d = Hash(x1, z1, seed);
			return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fz);
		}

		static float Hash(int x, int z, int seed)
		{
			unchecked
			{
				uint h = (uint)(x * 374761393 + z * 668265263 + seed * 1442695041);
				h = (h ^ (h >> 13)) * 1274126177u;
				h ^= h >> 16;
				return (h & 0x00FFFFFFu) / 16777215f;
			}
		}

		void EnsureSetup()
		{
			if (_quad == null) _quad = BuildQuad();
			if (_argsBuffer == null) _argsBuffer = new ComputeBuffer(1, 20, ComputeBufferType.IndirectArguments);
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
			_instanceFloats = new float[count * FloatsPerInstance];
		}

		Material ResolveMaterial()
		{
			if (material != null) return material;
			if (_fallbackMaterial == null)
			{
				Shader shader = Shader.Find(CardShaderName);
				if (shader == null)
				{
					WarnNoShader();
					return null;
				}
				// transient convenience material - never written back to the field
				_fallbackMaterial = new Material(shader) { renderQueue = 2979 };
			}
			return _fallbackMaterial;
		}

		/// <summary>
		/// World-space cull bounds around the current transform: the disc's
		/// diameter by its scale, and vertically the card band's reach.
		/// </summary>
		Bounds ComputeBounds()
		{
			if (blueprint == null) return new Bounds(transform.position, Vector3.one);
			Vector3 s = transform.lossyScale;
			float maxScale = Mathf.Max(Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y)), Mathf.Abs(s.z));
			float r = blueprint.radius * maxScale;
			float halfHeight = Mathf.Max(blueprint.thickness * (1f + blueprint.bulgeThicknessBoost) * planeThicken * 3f, 10f) * maxScale;
			return new Bounds(transform.position, new Vector3(r * 2f, halfHeight * 2f, r * 2f));
		}

		static Mesh BuildQuad()
		{
			var m = new Mesh { name = "GalaxyDustCardQuad" };
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
			// Shader-placed billboards: the unit proxy must not be frustum-
			// culled by a camera outside its implicit origin bounds.
			m.bounds = new Bounds(Vector3.zero, Vector3.one * 1e8f);
			return m;
		}

		void ReleaseAll()
		{
			if (_instanceBuffer != null)
			{
				_instanceBuffer.Release();
				_instanceBuffer = null;
			}
			if (_argsBuffer != null)
			{
				_argsBuffer.Release();
				_argsBuffer = null;
			}
			if (_quad != null)
			{
				if (Application.isPlaying) Destroy(_quad); else DestroyImmediate(_quad);
				_quad = null;
			}
			if (_noise != null)
			{
				if (Application.isPlaying) Destroy(_noise); else DestroyImmediate(_noise);
				_noise = null;
			}
			_capacity = 0;
			_instanceFloats = null;
			_fallbackMaterial = null;
		}

		void WarnNoBlueprint()
		{
			if (_warnedNoBlueprint) return;
			_warnedNoBlueprint = true;
			Debug.LogWarning($"[GalaxyDustCards] '{name}': no blueprint assigned - the cards will not draw. Run OpenFrontier/Galaxy/Install ED Renderer.", this);
		}

		void WarnNoShader()
		{
			if (_warnedNoShader) return;
			_warnedNoShader = true;
			Debug.LogWarning($"[GalaxyDustCards] '{name}': no material assigned and shader '{CardShaderName}' not found - the cards will not draw.", this);
		}
	}
}
