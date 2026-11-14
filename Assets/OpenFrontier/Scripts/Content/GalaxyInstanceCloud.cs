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
	/// Cost model: N sprites of 2 triangles. Default 12k instances = 24k
	/// triangles, ~768 KB of instance data - trivial on a phone; 48k is the
	/// deliberate PC ceiling (GalaxyBlueprint.MaxStarCount).
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

		const string CloudShaderName = "OpenFrontier/Galaxy/Cloud";
		const int FloatsPerInstance = 8; // pos.xyz + size | color.rgba
		const int InstanceStride = 32;   // two float4s

		static readonly int LocalToWorldId = Shader.PropertyToID("_LocalToWorld");
		static readonly int InstanceDataId = Shader.PropertyToID("_InstanceData");

		Mesh _quad;
		ComputeBuffer _instanceBuffer;
		ComputeBuffer _argsBuffer;
		MaterialPropertyBlock _mpb;
		Material _fallbackMaterial;
		float[] _instanceFloats;
		Vector3[] _positions;
		Color[] _colors;
		float[] _sizes;
		int _capacity; // instance count the buffers were built for
		bool _warnedNoBlueprint;
		bool _warnedNoShader;

		void OnEnable()
		{
			EnsureSetup();
			if (blueprint != null) Reseed(blueprint.seed);
		}

		void OnDisable()
		{
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
			_mpb.SetMatrix(LocalToWorldId, transform.localToWorldMatrix);
			_mpb.SetBuffer(InstanceDataId, _instanceBuffer);

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
			int count = Mathf.Clamp(blueprint.starCount, 0, GalaxyBlueprint.MaxStarCount);
			if (count != _capacity) RebuildBuffers(count);
			if (_capacity <= 0) return;

			int written = blueprint.Generate(seed, _positions, _colors, _sizes);
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
				_fallbackMaterial = new Material(shader);
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
