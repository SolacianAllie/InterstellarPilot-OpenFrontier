using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenFrontier.Galaxy
{
	/// <summary>
	/// The galaxy's face and its hero stars, in ONE component:
	/// - child "Quad": a 2x2 model-unit horizontal quad carrying the
	///   BAKED disk texture (GalaxyBaker renders this component's Params
	///   into it, off the runtime path). The photo's continuous body --
	///   core glow, arm haze, dust rifts, all with correct in-bake
	///   extinction -- costs ONE additive draw.
	/// - child "Stars": ONE merged mesh of billboard quads placed by
	///   GalaxyModel.PlaceHeroStars on the SAME structures the texture
	///   painted, so the parallax layer sits exactly on the arms.
	///
	/// WHY a baked quad: the scrapped build tried to paint continuous
	/// structure with 200k points and 6000 additive blobs -- the result
	/// was haze soup with the dust switched off. Points give grain and
	/// parallax; only a texture gives the continuous, correctly-occluded
	/// body of a photo.
	///
	/// MESH LIFECYCLE: same contract as the proven dressing -- meshes are
	/// built in place (Clear + refill, never destroyed in OnValidate),
	/// named "GalaxyQuad (generated)" / "GalaxyHeroStars (generated)" so
	/// a scene reload ADOPTS the copies the last save embedded, and
	/// [ExecuteAlways] keeps the editor view live while tuning.
	///
	/// COSTS: build-once (two mesh fills), then zero per-frame work --
	/// static meshes, two draws. The hero mesh is ~17k vertices at the
	/// default 4200 stars. Mobile-shaped: the bake is editor-time; the
	/// runtime is two draw calls and one 2048-wide ASTC texture.
	/// </summary>
	[ExecuteAlways]
	public class GalaxyBackdrop : MonoBehaviour
	{
		[Header("Materials (wired by the dressing installer)")]
		[Tooltip("OpenFrontier/GalaxyBackdrop material carrying the baked disk texture. Unwired: the quad stays black and is skipped (loudly).")]
		public Material QuadMaterial;

		[Tooltip("OpenFrontier/GalaxyStars material for the hero billboards. Unwired: the star mesh is skipped (loudly).")]
		public Material StarMaterial;

		[Header("Model")]
		[Tooltip("The structure knobs. The bake tool reads THESE (scene-first), so a tuned galaxy re-bakes as tuned; keep the bake and this in sync.")]
		public GalaxyModel.Params Model = GalaxyModel.Default(1);

		[Header("Hero stars")]
		[Tooltip("Hero star count: parallax layer over the baked face. 20000 ceiling (~80k vertices, ~3 MB) -- mobile-trivial, static.")]
		[Min(0)]
		public int HeroStarCount = 4200;

		[Tooltip("Global brightness multiplier on the star mesh (the texture has its own).")]
		[Min(0f)]
		public float StarBrightness = 1f;

		[Tooltip("Global size multiplier on the star mesh.")]
		[Min(0.01f)]
		public float StarSizeScale = 1f;

		private const string QuadMeshName = "GalaxyQuad (generated)";
		private const string StarsMeshName = "GalaxyHeroStars (generated)";
		private const string QuadChildName = "Quad";
		private const string StarsChildName = "Stars";
		private const int MaxStars = 20000;

		private Mesh quadMesh;
		private Mesh starsMesh;
		private GalaxyModel.Star[] starBuffer;
		private Transform quadChild;
		private Transform starsChild;

		private void OnEnable()
		{
			EnsureChildren();
			RebuildAll();
		}

		private void OnValidate()
		{
			// Mesh refills are safe in OnValidate (no DestroyImmediate);
			// keep the editor view honest while tuning.
			EnsureChildren();
			RebuildAll();
		}

		[ContextMenu("Rebuild")]
		public void RebuildAll()
		{
			EnsureChildren();
			RebuildQuad();
			RebuildStars();
		}

		/// <summary>Create the two children (idempotent) and bind the
		/// wired materials. Children are plain scene objects: the mesh
		/// contents below are this component's business.</summary>
		private void EnsureChildren()
		{
			quadChild = EnsureChild(QuadChildName);
			starsChild = EnsureChild(StarsChildName);

			if (quadChild != null)
			{
				MeshRenderer renderer = quadChild.GetComponent<MeshRenderer>();
				if (renderer != null && QuadMaterial != null)
				{
					renderer.sharedMaterial = QuadMaterial;
				}
			}
			if (starsChild != null)
			{
				MeshRenderer renderer = starsChild.GetComponent<MeshRenderer>();
				if (renderer != null && StarMaterial != null)
				{
					renderer.sharedMaterial = StarMaterial;
				}
			}
		}

		private Transform EnsureChild(string childName)
		{
			Transform existing = transform.Find(childName);
			if (existing != null)
			{
				return existing;
			}

			GameObject child = new GameObject(childName);
			child.transform.SetParent(transform, false);
			child.AddComponent<MeshFilter>();
			child.AddComponent<MeshRenderer>();
			return child.transform;
		}

		/// <summary>The baked face: one 2x2 model-unit quad in the XZ
		/// plane, UV-mapped to the full texture (the texture's own rim
		/// fade ends the disk; additive black corners are invisible).</summary>
		private void RebuildQuad()
		{
			if (quadChild == null)
			{
				return;
			}
			MeshFilter filter = quadChild.GetComponent<MeshFilter>();
			if (filter == null)
			{
				return;
			}

			Mesh mesh = AdoptMesh(filter, QuadMeshName, ref quadMesh);
			if (mesh == null)
			{
				return;
			}

			mesh.Clear();
			Vector3[] verts =
			{
				new Vector3(-1f, 0f, -1f),
				new Vector3(1f, 0f, -1f),
				new Vector3(-1f, 0f, 1f),
				new Vector3(1f, 0f, 1f),
			};
			Vector2[] uvs =
			{
				new Vector2(0f, 0f),
				new Vector2(1f, 0f),
				new Vector2(0f, 1f),
				new Vector2(1f, 1f),
			};
			int[] tris = { 0, 2, 1, 2, 3, 1 };
			mesh.vertices = verts;
			mesh.uv = uvs;
			mesh.triangles = tris;
			mesh.bounds = new Bounds(Vector3.zero, new Vector3(2f, 0.02f, 2f));
		}

		/// <summary>One merged billboard mesh from the model's placement.</summary>
		private void RebuildStars()
		{
			if (starsChild == null)
			{
				return;
			}
			MeshFilter filter = starsChild.GetComponent<MeshFilter>();
			if (filter == null)
			{
				return;
			}

			int count = Mathf.Clamp(HeroStarCount, 0, MaxStars);
			if (starBuffer == null || starBuffer.Length != count)
			{
				starBuffer = new GalaxyModel.Star[count];
			}
			GalaxyModel.PlaceHeroStars(Model, count, starBuffer);

			Mesh mesh = AdoptMesh(filter, StarsMeshName, ref starsMesh);
			if (mesh == null)
			{
				return;
			}

			Vector3[] verts = new Vector3[count * 4];
			Vector2[] corners = new Vector2[count * 4];
			Color[] colors = new Color[count * 4];
			Vector4[] data = new Vector4[count * 4];
			int[] tris = new int[count * 6];

			Vector2[] quadCorners =
			{
				new Vector2(-1f, -1f), new Vector2(1f, -1f),
				new Vector2(-1f, 1f), new Vector2(1f, 1f),
			};

			Vector3 boundsMin = new Vector3(-1.35f, -1.1f, -1.35f);
			Vector3 boundsMax = new Vector3(1.35f, 1.1f, 1.35f);
			float brightness = Mathf.Max(StarBrightness, 0f);
			float sizeScale = Mathf.Max(StarSizeScale, 0.01f);

			for (int i = 0; i < count; i++)
			{
				GalaxyModel.Star s = starBuffer[i];
				Vector3 center = s.Pos;
				float size = s.Size * sizeScale;
				Color col = s.Col * brightness;
				// TEXCOORD1.y carries per-vertex alpha (1 today; the
				// shader's near-fade and intensity handle the rest).
				Vector4 extra = new Vector4(size, 1f, 0f, 0f);

				for (int k = 0; k < 4; k++)
				{
					int v = i * 4 + k;
					verts[v] = center;
					corners[v] = quadCorners[k];
					colors[v] = col;
					data[v] = extra;
				}

				int t = i * 6;
				int b = i * 4;
				tris[t] = b;
				tris[t + 1] = b + 1;
				tris[t + 2] = b + 2;
				tris[t + 3] = b + 2;
				tris[t + 4] = b + 1;
				tris[t + 5] = b + 3;

				// Track real bounds: position + quad reach.
				Vector3 reach = new Vector3(size, size, size);
				boundsMin = Vector3.Min(boundsMin, center - reach);
				boundsMax = Vector3.Max(boundsMax, center + reach);
			}

			if (count * 4 > 65535)
			{
				mesh.indexFormat = IndexFormat.UInt32;
			}
			mesh.Clear();
			if (count > 0)
			{
				mesh.vertices = verts;
				mesh.uv = corners;
				mesh.colors = colors;
				mesh.SetUVs(1, new List<Vector4>(data));
				mesh.triangles = tris;
			}
			mesh.bounds = new Bounds(
				(boundsMin + boundsMax) * 0.5f,
				(boundsMax - boundsMin));
		}

		/// <summary>One mesh instance REUSED across rebuilds (Clear +
		/// refill, never destroyed during OnValidate; replace-and-leak
		/// would embed an orphaned mesh per save). The marker name lets
		/// a scene reload ADOPT the copy the last save embedded.</summary>
		private Mesh AdoptMesh(MeshFilter filter, string meshName, ref Mesh cache)
		{
			Mesh mesh = cache;
			if (mesh == null)
			{
				mesh = filter.sharedMesh;
				if (mesh == null || mesh.name != meshName)
				{
					mesh = new Mesh { name = meshName };
				}
				cache = mesh;
			}
			filter.sharedMesh = mesh;
			return mesh;
		}
	}
}
