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
	/// 'quality' is the test/tuning slider: 0 low, 1 medium, 2 high. Build
	/// falls back to the nearest asset present, so a prefab may carry only
	/// part of the ladder. Raymarch steps scale with the edge (32/64/128,
	/// two texels per step at every rung) - a 256^3 volume sampled 32
	/// times would throw the new detail away.
	///
	/// ExecuteAlways so the nearest editor view already shows the volume
	/// instead of the template's empty box. OnDisable tears the texture down.
	/// A quality switch is a rebuild - old Texture3D destroyed, new one
	/// uploaded - so expect an upload hitch for the high LOD on device.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(MeshRenderer))]
	public sealed class GalaxyNebulaVolume : MonoBehaviour
	{
		public TextAsset volumeLow;      // 64^3,  1 MB
		public TextAsset volumeMedium;   // 128^3, 8 MB
		public TextAsset volumeHigh;     // 256^3, 64 MB

		/// <summary>0 = 64^3, 1 = 128^3, 2 = 256^3.</summary>
		[Range(0, 2)] public int quality;

		/// <summary>
		/// Per-instance optical multipliers; the shared NebulaVolume.mat stays
		/// the neutral base (1/1/4) and these ride the MaterialPropertyBlock
		/// next to _Steps. ED's named volumes carry the game's own per-volume
		/// density gain baked into the data (ed_gain in the bake tool), stored
		/// as density/peak with this field restoring the peak, so they march
		/// at absorption 1 with brightness 4/gain to hold the emission level;
		/// the generic bakes keep their hand normalisation and compensate
		/// with absorption 12 instead.
		/// </summary>
		public float density = 1f;
		public float absorption = 1f;
		public float brightness = 4f;

		Renderer _renderer;
		MaterialPropertyBlock _block;
		Texture3D _volume;

		void OnEnable()
		{
			Build();
		}

		void OnDisable()
		{
			Release();
		}

		void OnValidate()
		{
			if (!isActiveAndEnabled) return;
			Release();
			Build();
		}

		/// <summary>Switch the uploaded LOD at runtime (rebuilds the texture).</summary>
		public void SetQuality(int level)
		{
			quality = Mathf.Clamp(level, 0, 2);
			if (!isActiveAndEnabled) return;
			Release();
			Build();
		}

		void Build()
		{
			TextAsset asset = Pick();
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
			_block.SetFloat("_Steps", StepsFor(edge));
			_block.SetFloat("_Density", Mathf.Max(density, 0f));
			_block.SetFloat("_Absorption", Mathf.Max(absorption, 0f));
			_block.SetFloat("_Brightness", Mathf.Max(brightness, 0f));
			_renderer.SetPropertyBlock(_block);
		}

		/// <summary>Requested rung, else the nearest one that has an asset.</summary>
		TextAsset Pick()
		{
			int wanted = Mathf.Clamp(quality, 0, 2);
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
		/// Two texels per step at every rung (32/64/128). The jitter offset is
		/// a fraction of one step, so halving the step length halves the dither
		/// grain - uniform sampling density keeps all rungs at the same noise
		/// scale. The 128-step rung is desktop/testing only.
		/// </summary>
		static float StepsFor(int edge) =>
			edge >= 256 ? 128f : edge >= 128 ? 64f : 32f;

		void Release()
		{
			if (_renderer != null) _renderer.SetPropertyBlock(null);
			_block = null;
			if (_volume == null) return;
			if (Application.isPlaying) Destroy(_volume);
			else DestroyImmediate(_volume);
			_volume = null;
		}
	}
}
