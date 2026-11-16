using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// One placed 3D nebula with a quality ladder. ED's volumes ship at three
	/// edges: 64^3 (1 MB, the mobile LOD), 128^3 (8 MB, a downsample of ED's
	/// high data) and 256^3 (64 MB, ED's own high LOD). Each is a raw RGBA32
	/// .bytes file (x fastest, then y, then z); this component uploads the
	/// selected one into a Texture3D and hands it to the renderer through a
	/// MaterialPropertyBlock, so the shared NebulaVolume.mat stays untouched
	/// and nothing runtime-created is serialized into the scene.
	///
	/// 'quality' is the authored ceiling: 0 low, 1 medium, 2 high - the
	/// maximum resolution this volume may ever upload. GalaxyNebulaField
	/// swaps the uploaded rung down as the volume's projected size falls and
	/// back up as it grows, both clamped by NebulaQuality.MaxLod, so the
	/// slider is a memory ceiling rather than a fixed choice. A volume with
	/// no field in its parent keeps the authored rung.
	///
	/// The first build is deliberately cheap when a field is present (64^3,
	/// 1 MB): the field raises a volume to its allowed rung on the first
	/// ranking tick if it is large enough on screen. Without a field the
	/// volume builds at the authored rung, which is the hand-placed case.
	///
	/// Build also derives the volume's tight object-space bounds from its
	/// alpha and passes them as _BoundsMin/_BoundsMax. Most ED volumes carry
	/// a lot of empty cube; intersecting the ray with the occupied box skips
	/// that empty march for free (the shader discards a miss outright).
	///
	/// Steps are native at build (32/64/128, two texels per step) and then
	/// owned by GalaxyNebulaField, which lowers them by on-screen size. With
	/// no field in the scene the native count stands.
	///
	/// ExecuteAlways so the nearest editor view already shows the volume
	/// instead of the template's empty box. OnDisable tears the texture down.
	/// A rung switch is a rebuild - old Texture3D destroyed, new one
	/// uploaded - so expect an upload hitch for the high LOD on desktop.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(MeshRenderer))]
	public sealed class GalaxyNebulaVolume : MonoBehaviour
	{
		public TextAsset volumeLow;      // 64^3,  1 MB
		public TextAsset volumeMedium;   // 128^3, 8 MB
		public TextAsset volumeHigh;     // 256^3, 64 MB

		/// <summary>
		/// Authored ceiling: 0 = 64^3, 1 = 128^3, 2 = 256^3. The field swaps
		/// the uploaded LOD beneath this and the platform rung.
		/// </summary>
		[Range(0, 2)] public int quality;

		/// <summary>
		/// Per-instance optical multipliers; the shared NebulaVolume.mat stays
		/// the neutral base (1/1/4) and these ride the MaterialPropertyBlock
		/// next to _Steps. Every ED volume carries the game's own per-volume
		/// density gain baked into the data (ed_gain in the bake tool), stored
		/// as density/peak with this field restoring the peak, so they march
		/// at absorption 1 with brightness 4/gain to hold the emission level
		/// while the opacity is ED's own.
		/// </summary>
		public float density = 1f;
		public float absorption = 1f;
		public float brightness = 4f;

		const byte OpaqueAlpha = 2;

		static readonly Vector3 FullMin = new Vector3(-0.5f, -0.5f, -0.5f);
		static readonly Vector3 FullMax = new Vector3(0.5f, 0.5f, 0.5f);

		Renderer _renderer;
		MaterialPropertyBlock _block;
		Texture3D _volume;
		TextAsset _asset;
		int _edge;
		int _targetLod = -1;
		int _lod = -1;
		bool _hasField;
		float _steps;
		Vector3 _boundsMin = FullMin;
		Vector3 _boundsMax = FullMax;

		/// <summary>Two texels per step at the uploaded LOD's edge (32/64/128).</summary>
		public float NativeSteps => StepsFor(_edge > 0 ? _edge : 64);

		/// <summary>World-space box GalaxyNebulaField ranks this volume by.</summary>
		public Bounds WorldBounds
		{
			get
			{
				if (_renderer == null) _renderer = GetComponent<Renderer>();
				if (_renderer != null) return _renderer.bounds;
				return new Bounds(transform.position, Vector3.one);
			}
		}

		/// <summary>Highest rung this volume may upload (authored ceiling ∩ platform rung).</summary>
		public int MaxAllowedLod => Mathf.Min(Mathf.Clamp(quality, 0, 2), NebulaQuality.MaxLod);

		/// <summary>The rung currently uploaded: 0 = 64^3, 1 = 128^3, 2 = 256^3.</summary>
		public int UploadedLod => _lod;

		void OnEnable()
		{
			NebulaQuality.Changed += RebuildForRung;
			_hasField = GetComponentInParent<GalaxyNebulaField>() != null;
			Build();
		}

		void OnDisable()
		{
			NebulaQuality.Changed -= RebuildForRung;
			Release();
		}

		void OnValidate()
		{
			if (!isActiveAndEnabled) return;
			Release();
			Build();
		}

		/// <summary>Switch the authored ceiling at runtime; lowers immediately if needed.</summary>
		public void SetQuality(int level)
		{
			quality = Mathf.Clamp(level, 0, 2);
			if (!isActiveAndEnabled) return;
			// A cap can make 2 and 0 the same uploaded asset; do not tear
			// down a texture for a no-op.
			if (_asset != null && PickFrom(WantedLod()) == _asset) return;
			Release();
			Build();
		}

		/// <summary>
		/// Field-owned LOD: rebuilds the texture at the requested rung when it
		/// differs from the resident one. Clamped to the authored ceiling, so
		/// a slider at 1 never pays for 256^3 even when the field asks.
		/// </summary>
		public void SetLodLevel(int level)
		{
			int wanted = Mathf.Min(Mathf.Clamp(level, 0, 2), MaxAllowedLod);
			_targetLod = wanted;
			if (!isActiveAndEnabled || _lod == wanted) return;
			TextAsset next = PickFrom(wanted);
			if (next == null || next == _asset) return;
			Release();
			Build();
		}

		/// <summary>
		/// Field-owned march count. Only the property block changes - no
		/// texture rebuild, so this is safe to nudge as the camera moves.
		/// </summary>
		public void SetMarchSteps(float steps)
		{
			float clamped = Mathf.Clamp(steps, 4f, 128f);
			if (Mathf.Abs(clamped - _steps) < 0.01f) return;
			_steps = clamped;
			if (_block == null) return;
			_block.SetFloat("_Steps", _steps);
			if (_renderer != null) _renderer.SetPropertyBlock(_block);
		}

		/// <summary>Budget-owned visibility. In the editor every volume stays visible.</summary>
		public void SetVisible(bool visible)
		{
			if (!Application.isPlaying) return;
			if (_renderer == null) _renderer = GetComponent<Renderer>();
			if (_renderer != null && _renderer.enabled != visible) _renderer.enabled = visible;
		}

		void RebuildForRung()
		{
			// The platform ceiling moved. If it invalidates the uploaded
			// texture, rebuild at the clamp; if it rose, the field asks for
			// the higher rung on its next ranking tick.
			if (!isActiveAndEnabled || _asset == null) return;
			if (PickFrom(WantedLod()) == _asset) return;
			Release();
			Build();
		}

		void Build()
		{
			TextAsset asset = PickFrom(WantedLod());
			if (asset == null) return;

			_renderer = GetComponent<Renderer>();
			if (_renderer == null) return;

			byte[] bytes = asset.bytes;
			int edge = CubeEdge(bytes);
			if (edge == 0)
			{
				Debug.LogError($"[GalaxyNebulaVolume] '{name}': '{asset.name}' is " +
					$"{(bytes == null ? 0 : bytes.Length)} bytes, not a cube RGBA32 " +
					"volume.", this);
				return;
			}

			_asset = asset;
			_edge = edge;
			_lod = edge >= 256 ? 2 : edge >= 128 ? 1 : 0;
			_steps = StepsFor(edge);
			ComputeBounds(bytes, edge);

			_volume = new Texture3D(edge, edge, edge, TextureFormat.RGBA32, false, true)
			{
				name = asset.name,
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Bilinear,
				hideFlags = HideFlags.HideAndDontSave
			};
			_volume.SetPixelData(bytes, 0);
			_volume.Apply(false, true);

			if (_block == null) _block = new MaterialPropertyBlock();
			_block.SetTexture("_Volume", _volume);
			_block.SetVector("_BoundsMin", _boundsMin);
			_block.SetVector("_BoundsMax", _boundsMax);
			PublishScalars();
			_renderer.SetPropertyBlock(_block);
		}

		void PublishScalars()
		{
			_block.SetFloat("_Steps", _steps);
			_block.SetFloat("_Density", Mathf.Max(density, 0f));
			_block.SetFloat("_Absorption", Mathf.Max(absorption, 0f));
			_block.SetFloat("_Brightness", Mathf.Max(brightness, 0f));
		}

		/// <summary>Nearest asset at or below 'wanted', else the nearest above.</summary>
		TextAsset PickFrom(int wanted)
		{
			for (int i = wanted; i >= 0; i--)
			{
				TextAsset asset = At(i);
				if (asset != null) return asset;
			}
			for (int i = wanted + 1; i <= 2; i++)
			{
				TextAsset asset = At(i);
				if (asset != null) return asset;
			}
			return null;
		}

		int WantedLod()
		{
			// With a field in the parent the field owns the rung and starts
			// every volume at 64^3; without one the authored ceiling stands.
			int level = _targetLod >= 0 ? _targetLod : (_hasField ? 0 : MaxAllowedLod);
			return Mathf.Min(level, MaxAllowedLod);
		}

		TextAsset At(int level) =>
			level == 0 ? volumeLow : level == 1 ? volumeMedium : volumeHigh;

		static int CubeEdge(byte[] bytes)
		{
			if (bytes == null || bytes.Length < 32) return 0;
			long voxels = bytes.Length / 4L;
			int edge = (int)System.Math.Round(System.Math.Pow(voxels, 1.0 / 3.0));
			return (long)edge * edge * edge == voxels ? edge : 0;
		}

		/// <summary>
		/// Tightest object-space box holding any voxel above the bake's floor,
		/// padded by one voxel so a bilinear tap at the face reads the empty
		/// neighbours instead of the clamp edge. A fully empty volume keeps
		/// the full cube - it renders nothing either way. The scan is linear
		/// in the data being uploaded anyway (~50 ms at 256^3 beside a 64 MB
		/// upload), so it is not sampled.
		/// </summary>
		void ComputeBounds(byte[] bytes, int edge)
		{
			int minX = edge, minY = edge, minZ = edge;
			int maxX = -1, maxY = -1, maxZ = -1;

			int i = 0;
			for (int z = 0; z < edge; z++)
			{
				for (int y = 0; y < edge; y++)
				{
					for (int x = 0; x < edge; x++, i += 4)
					{
						if (bytes[i + 3] < OpaqueAlpha) continue;
						if (x < minX) minX = x;
						if (x > maxX) maxX = x;
						if (y < minY) minY = y;
						if (y > maxY) maxY = y;
						if (z < minZ) minZ = z;
						if (z > maxZ) maxZ = z;
					}
				}
			}

			if (maxX < 0)
			{
				_boundsMin = FullMin;
				_boundsMax = FullMax;
				return;
			}

			minX = Mathf.Max(0, minX - 1);
			maxX = Mathf.Min(edge - 1, maxX + 1);
			minY = Mathf.Max(0, minY - 1);
			maxY = Mathf.Min(edge - 1, maxY + 1);
			minZ = Mathf.Max(0, minZ - 1);
			maxZ = Mathf.Min(edge - 1, maxZ + 1);

			float inv = 1f / edge;
			_boundsMin = new Vector3(minX * inv - 0.5f, minY * inv - 0.5f, minZ * inv - 0.5f);
			_boundsMax = new Vector3((maxX + 1) * inv - 0.5f, (maxY + 1) * inv - 0.5f,
				(maxZ + 1) * inv - 0.5f);
		}

		/// <summary>
		/// Two texels per step at every rung (32/64/128). The jitter offset is
		/// a fraction of one step, so halving the step length halves the dither
		/// grain - uniform sampling density keeps all rungs at the same noise
		/// scale. The 128-step rung is Hero only.
		/// </summary>
		static float StepsFor(int edge) =>
			edge >= 256 ? 128f : edge >= 128 ? 64f : 32f;

		void Release()
		{
			if (_renderer != null) _renderer.SetPropertyBlock(null);
			_block = null;
			_asset = null;
			_edge = 0;
			_lod = -1;
			_boundsMin = FullMin;
			_boundsMax = FullMax;
			if (_volume == null) return;
			if (Application.isPlaying) Destroy(_volume);
			else DestroyImmediate(_volume);
			_volume = null;
		}
	}
}
