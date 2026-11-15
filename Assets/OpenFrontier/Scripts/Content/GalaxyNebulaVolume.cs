using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// One placed 3D nebula. ED's volumes ship as 64^3 RGBA32 .bytes files
	/// (1 MB each); this component uploads one into a Texture3D
	/// and hands it to the renderer through a MaterialPropertyBlock, so the
	/// shared NebulaVolume.mat stays untouched and nothing runtime-created is
	/// serialized into the scene. The shader does the raymarch; there is no
	/// per-frame work here.
	///
	/// ExecuteAlways so the nearest editor view already shows the volume
	/// instead of the template's empty box. OnDisable tears the texture down.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(MeshRenderer))]
	public sealed class GalaxyNebulaVolume : MonoBehaviour
	{
		/// <summary>64^3 RGBA32 raw bytes, x fastest then y then z.</summary>
		public TextAsset volumeData;

		/// <summary>Edge length of the baked volume. Must match volumeData.</summary>
		[Min(8)] public int size = 64;

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

		void Build()
		{
			if (volumeData == null) return;

			_renderer = GetComponent<Renderer>();
			if (_renderer == null) return;

			int expected = size * size * size * 4;
			byte[] bytes = volumeData.bytes;
			if (bytes == null || bytes.Length != expected)
			{
				Debug.LogError($"[GalaxyNebulaVolume] '{name}': '{volumeData.name}' is " +
					$"{(bytes == null ? 0 : bytes.Length)} bytes, expected {expected} " +
					$"for {size}^3 RGBA32.", this);
				return;
			}

			_volume = new Texture3D(size, size, size, TextureFormat.RGBA32, false, true)
			{
				name = volumeData.name,
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Bilinear,
				hideFlags = HideFlags.HideAndDontSave
			};
			_volume.SetPixelData(bytes, 0);
			_volume.Apply(false, true);

			if (_block == null) _block = new MaterialPropertyBlock();
			_block.SetTexture("_Volume", _volume);
			_renderer.SetPropertyBlock(_block);
		}

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
