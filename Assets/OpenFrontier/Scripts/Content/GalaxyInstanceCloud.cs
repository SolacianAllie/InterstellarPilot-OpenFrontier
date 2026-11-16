using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// The realtime galaxy band: ONE instanced point-cloud draw, camera-facing
	/// additive billboards sized and coloured by a GalaxyBlueprint. Points are
	/// generated on the CPU once per reseed (a few ms for the default 12k),
	/// uploaded to a compute buffer and drawn every frame with
	/// Graphics.DrawMeshInstancedIndirect - no per-frame CPU, no GameObjects,
	/// no URP types.
	///
	/// One draw serves every camera: the map view and the in-sky view both see
	/// the same cloud, and the billboard orientation is done in clip space
	/// against the per-camera projection matrix, so a single indirect draw is
	/// correct in each view. The object's transform (position/rotation/scale)
	/// is applied live via the _LocalToWorld shader global - rotating or
	/// moving the cloud never rebuilds anything.
	///
	/// Cost model: N sprites of 2 triangles. The Desktop rung draws 40k
	/// instances = 80k triangles, ~1.9 MB of instance data - one draw, still
	/// comfortable on a mid-range phone; 250k is the authored blueprint ceiling.
	///
	/// Phase 2 hooks a per-jump reseed (ReseedRandom) from the generation
	/// trigger; the buffer rewrite is the whole cost.
	/// </summary>
	[ExecuteAlways]
	public class GalaxyInstanceCloud : MonoBehaviour
	{
		/// <summary>The blueprint this cloud renders.</summary>
		public GalaxyBlueprint blueprint;

		/// <summary>Material using OpenFrontier/Galaxy/Cloud. The queue offset (2990) lives on the material, not the shader.</summary>
		public Material material;

		/// <summary>Optional galaxy ladder: caps the drawn instance count and applies ED's inverse brightness/size scaling.</summary>
		public GalaxyQualitySettings quality;

		/// <summary>
		/// Screen-space sprite sizing (default). ED's map billboards are star
		/// sprites, not world-sized discs; turning this off restores the old
		/// world-size path and is useful only for comparison.
		/// </summary>
		public bool pixelSizedSprites = true;

		const string CloudShaderName = "OpenFrontier/Galaxy/Cloud";
		const int FloatsPerInstance = 12; // pos.xyz + size | color.rgba | fx(phase, amp, speed, variant)
		const int InstanceStride = 48;    // three float4s

		static readonly int LocalToWorldId = Shader.PropertyToID("_LocalToWorld");
		static readonly int InstanceDataId = Shader.PropertyToID("_InstanceData");
		static readonly int CloudBrightnessId = Shader.PropertyToID("_GalaxyCloudBrightness");
		static readonly int CloudSizeId = Shader.PropertyToID("_GalaxyCloudSize");
		static readonly int CloudSizeModeId = Shader.PropertyToID("_GalaxyCloudSizeMode");
		static readonly int CloudPxId = Shader.PropertyToID("_GalaxyCloudPx");
		static readonly int CloudWorldId = Shader.PropertyToID("_GalaxyCloudWorld");
		// the sky's clock: UNSCALED time - the stage boots at timeScale 0 and
		// _Time freezes exactly there, so twinkle runs on this pushed global
		// (the _OpenFrontierHoloTime pattern). PropertyToID in static init is
		// fine; URP itself does it everywhere.
		static readonly int GalaxyTimeId = Shader.PropertyToID("_OpenFrontierGalaxyTime");

		Mesh _quad;
		ComputeBuffer _instanceBuffer;
		ComputeBuffer _argsBuffer;
		MaterialPropertyBlock _mpb;
		Material _fallbackMaterial;
		float[] _instanceFloats;
		Vector3[] _positions;
		Color[] _colors;
		float[] _sizes;
		Vector4[] _fx;
		int _capacity; // instance count the buffers were built for
		bool _warnedNoBlueprint;
		bool _warnedNoShader;
		float _brightness = 1f;
		float _sizeScale = 1f;
		float _starPxMin = 1f;
		float _starPxMax = 2.6f;
		float _hazePxMin = 3f;
		float _hazePxMax = 8f;

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

		void Update()
		{
			// advance the sky's clock before anything else - even a cloud with
			// no blueprint keeps the global fresh for any shader using it
			Shader.SetGlobalFloat(GalaxyTimeId, Time.unscaledTime);
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
			_mpb.SetFloat(CloudBrightnessId, _brightness);
			_mpb.SetFloat(CloudSizeId, _sizeScale);
			_mpb.SetFloat(CloudSizeModeId, pixelSizedSprites ? 1f : 0f);
			_mpb.SetVector(CloudPxId, new Vector4(_starPxMin, _starPxMax, _hazePxMin, _hazePxMax));
			_mpb.SetVector(CloudWorldId, new Vector4(
				blueprint.sizeMin, Mathf.Max(blueprint.sizeMin, blueprint.sizeMax),
				blueprint.hazeSizeMin, Mathf.Max(blueprint.hazeSizeMin, blueprint.hazeSizeMax)));

			Graphics.DrawMeshInstancedIndirect(_quad, 0, mat, ComputeBounds(), _argsBuffer, 0, _mpb,
				ShadowCastingMode.Off, false, gameObject.layer);
		}

		/// <summary>
		/// Regenerates the cloud from the blueprint with the given seed. The
		/// whole cost is one CPU pass plus one buffer upload.
		/// </summary>
		public void Reseed(int seed)
		{
			if (blueprint == null || _quad == null || _argsBuffer == null) return;
			int authored = Mathf.Clamp(blueprint.starCount, 0, GalaxyBlueprint.MaxStarCount);
			int cap = quality != null ? Mathf.Clamp(quality.starCloudInstances, 0, GalaxyBlueprint.MaxStarCount) : authored;
			if (cap <= 0) cap = authored;
			int count = Mathf.Min(authored, cap);
			if (count != _capacity) RebuildBuffers(count);
			if (_capacity <= 0) return;

			int written = blueprint.Generate(seed, _positions, _colors, _sizes, _fx);
			for (int i = 0; i < written; i++)
			{
				int b = i * FloatsPerInstance;
				Vector3 p = _positions[i];
				_instanceFloats[b + 0] = p.x;
				_instanceFloats[b + 1] = p.y;
				_instanceFloats[b + 2] = p.z;
				_instanceFloats[b + 3] = _sizes[i];
				Color c = _colors[i];
				_instanceFloats[b + 4] = c.r;
				_instanceFloats[b + 5] = c.g;
				_instanceFloats[b + 6] = c.b;
				_instanceFloats[b + 7] = c.a;
				Vector4 f = _fx[i];
				_instanceFloats[b + 8] = f.x;
				_instanceFloats[b + 9] = f.y;
				_instanceFloats[b + 10] = f.z;
				_instanceFloats[b + 11] = f.w;
			}
			if (written > 0) _instanceBuffer.SetData(_instanceFloats, 0, 0, written * FloatsPerInstance);
			// indirect args: indexCount, instanceCount, firstIndex, baseVertex, firstInstance
			_argsBuffer.SetData(new uint[5] { 6u, (uint)written, 0u, 0u, 0u });
		}

		/// <summary>Per-jump reseed entry - the generation trigger calls this in phase 2.</summary>
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
		/// Re-reads the galaxy ladder. ED scales brightness and size inversely
		/// with instance count (8000/1.5/1.3, 12000/1.3/1.15, 16000/1.0/1.0):
		/// fewer sprites are individually brighter and larger so the band reads
		/// the same. The drawn count is capped at generation time, so a rung
		/// change needs a reseed - call ApplyQualityAndReseed from the UI.
		/// </summary>
		public void ApplyQuality()
		{
			if (quality == null)
			{
				_brightness = 1.05f;
				_sizeScale = 1f;
				_starPxMin = 1f;
				_starPxMax = 2.6f;
				_hazePxMin = 3f;
				_hazePxMax = 8f;
				return;
			}
			_brightness = quality.starCloudBrightness;
			_sizeScale = quality.starCloudSize;
			_starPxMin = quality.cloudStarPxMin;
			_starPxMax = quality.cloudStarPxMax;
			_hazePxMin = quality.cloudHazePxMin;
			_hazePxMax = quality.cloudHazePxMax;
		}

		public void ApplyQualityAndReseed()
		{
			ApplyQuality();
			if (blueprint != null) Reseed(blueprint.seed);
		}

		/// <summary>
		/// The per-jump entry point for M7's generation code: reseeds every
		/// cloud in the scene ONCE, at event time - a scene scan belongs to
		/// events, never the render path (house rule). GenerationTrigger owns
		/// only the legal region; the generation that fires this doesn't exist
		/// yet (docs/galaxy-realtime-plan.md, phase 2/3).
		/// </summary>
		public static void ReseedAllRandom()
		{
			GalaxyInstanceCloud[] clouds = FindObjectsByType<GalaxyInstanceCloud>(FindObjectsSortMode.None);
			for (int i = 0; i < clouds.Length; i++) clouds[i].ReseedRandom();
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
			_positions = new Vector3[count];
			_colors = new Color[count];
			_sizes = new float[count];
			_fx = new Vector4[count];
		}

		Material ResolveMaterial()
		{
			if (material != null) return material;
			if (_fallbackMaterial == null)
			{
				Shader shader = Shader.Find(CloudShaderName);
				if (shader == null)
				{
					WarnNoShader();
					return null;
				}
				// transient convenience material - never written back to the field
				_fallbackMaterial = new Material(shader) { renderQueue = 2990 };
			}
			return _fallbackMaterial;
		}

		/// <summary>
		/// World-space cull bounds around the current transform: the disc's
		/// diameter by its scale, and vertically 3 bulge sigmas.
		/// </summary>
		Bounds ComputeBounds()
		{
			if (blueprint == null) return new Bounds(transform.position, Vector3.one);
			float s = MaxScale();
			float r = blueprint.radius * s;
			float halfHeight = Mathf.Max(blueprint.thickness * (1f + blueprint.bulgeThicknessBoost) * 3f, 10f) * s;
			return new Bounds(transform.position, new Vector3(r * 2f, halfHeight * 2f, r * 2f));
		}

		float MaxScale()
		{
			Vector3 s = transform.lossyScale;
			return Mathf.Max(Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y)), Mathf.Abs(s.z));
		}

		static Mesh BuildQuad()
		{
			var m = new Mesh { name = "GalaxyCloudQuad" };
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
			// The visible geometry is placed entirely in the shader; a unit
			// quad's implicit bounds sit at the galaxy origin and are culled
			// by a perspective camera outside the thin disc. DrawMesh's
			// explicit bounds are not enough for every culling path, so keep
			// the mesh itself uncullable (one indirect draw, one bound test).
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
			_capacity = 0;
			_instanceFloats = null;
			_positions = null;
			_colors = null;
			_sizes = null;
			_fx = null;
			_fallbackMaterial = null;
		}

		void WarnNoBlueprint()
		{
			if (_warnedNoBlueprint) return;
			_warnedNoBlueprint = true;
			Debug.LogWarning($"[GalaxyInstanceCloud] '{name}': no blueprint assigned - the cloud will not draw. Run OpenFrontier/Galaxy/Install Realtime Cloud.", this);
		}

		void WarnNoShader()
		{
			if (_warnedNoShader) return;
			_warnedNoShader = true;
			Debug.LogWarning($"[GalaxyInstanceCloud] '{name}': no material assigned and shader '{CloudShaderName}' not found - the cloud will not draw.", this);
		}
	}
}
