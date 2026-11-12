using UnityEngine;

namespace OpenFrontier.Galaxy
{
	/// <summary>
	/// A procedural galactic disk -- the galaxy stage's first dressing layer
	/// and the backdrop every later sky capture photographs.
	///
	/// Division of labour, on purpose: THIS component owns geometry (the
	/// volumetric sheet -- layers, thickness, vertical profile); the
	/// MATERIAL (shader OpenFrontier/GalaxyDisk) owns the look (arms, dust,
	/// grain, palette). Nothing is duplicated across the two: the vertical
	/// falloff is baked into vertex alpha here, so the shader never needs a
	/// thickness knob to drift out of sync with the mesh.
	///
	/// The mesh is a UNIT DISK (radius 1 in local space) -- size the
	/// TRANSFORM. A main galaxy wants thousands of units; a satellite galaxy
	/// is this same component scaled down and offset. The mesh is generated
	/// and owned by this component: a foreign mesh is swapped out of the
	/// filter, never destroyed.
	///
	/// Seen from above: the classic spiral disk. Seen edge-on -- which is
	/// how a system capture sees it from INSIDE the galaxy -- the stacked
	/// layers read as a band with real thickness, where a single flat quad
	/// would collapse to a line.
	///
	/// Layer: author it on Celestial (8), with the rest of the celestial
	/// dressing. Cost: one draw call, ~900 vertices, pure-ALU fragment --
	/// mobile-trivial, and capture-friendly because there are no textures
	/// to blur at 4K capture resolution.
	/// </summary>
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class GalaxyDisk : MonoBehaviour
	{
		[Header("Geometry (local units; the mesh is a unit disk)")]
		[Tooltip("Disk planes stacked across the thickness. More = smoother edge-on band. 9 is plenty.")]
		[Min(3)]
		public int Layers = 9;

		[Tooltip("Full thickness as a fraction of the local radius. 0.06 on a unit disk reads galaxy-like.")]
		[Min(0.004f)]
		public float Thickness = 0.06f;

		[Tooltip("Shape of the brightness falloff across the thickness: 0.5 = puffy, 6 = paper-thin. Baked per layer into vertex alpha.")]
		[Range(0.5f, 6f)]
		public float VerticalFalloff = 2.2f;

		[Tooltip("Rim segments per layer. 96 is smooth at any plausible camera distance.")]
		[Min(24)]
		public int Segments = 96;

		private Mesh generatedMesh;

		private void Awake()
		{
			RebuildMesh();
		}

		private const string MeshName = "GalaxyDisk (generated)";

		/// <summary>Regenerate the sheet. Runs when the component is added
		/// (Reset), whenever a geometry field changes in the inspector
		/// (OnValidate), and on Awake in play mode -- the editor preview is
		/// always the real mesh, never a stale one.
		///
		/// One mesh instance is REUSED across rebuilds (Clear + refill,
		/// never destroyed): Unity REFUSES DestroyImmediate during
		/// OnValidate outright, and swapping in a fresh mesh each time
		/// would embed a new orphaned copy into the scene on every save.
		/// The marker name lets a scene reload ADOPT the copy the last
		/// save embedded, so the scene carries exactly one mesh per
		/// component.</summary>
		[ContextMenu("Rebuild Disk Mesh")]
		public void RebuildMesh()
		{
			MeshFilter filter = GetComponent<MeshFilter>();
			if (filter == null)
			{
				return;
			}

			Mesh mesh = generatedMesh;
			if (mesh == null)
			{
				// Adopt the embedded copy from the last save when it is
				// ours; otherwise start a new one. A foreign mesh someone
				// assigned is swapped out of the filter, never destroyed.
				mesh = filter.sharedMesh;
				if (mesh == null || mesh.name != MeshName)
				{
					mesh = new Mesh { name = MeshName };
				}
				generatedMesh = mesh;
			}
			filter.sharedMesh = mesh;
			FillMesh(mesh);
		}

		private void FillMesh(Mesh mesh)
		{
			mesh.Clear();
			int layers = Mathf.Clamp(Layers, 3, 15);
			int segments = Mathf.Clamp(Segments, 24, 256);
			float half = Mathf.Max(0.002f, Thickness) * 0.5f;
			float falloff = Mathf.Clamp(VerticalFalloff, 0.5f, 6f);
			// The exp profile is normalised to hit exactly ZERO at the outer
			// layers. Unnormalised it would still carry exp(-falloff)
			// (about 11% at 2.2) and the sheet would end in a visible hard
			// edge.
			float edge = Mathf.Exp(-falloff);

			int vertsPerLayer = segments + 2;
			Vector3[] verts = new Vector3[layers * vertsPerLayer];
			Color[] colors = new Color[layers * vertsPerLayer];
			int[] triangles = new int[layers * segments * 3];

			int vi = 0;
			int ti = 0;
			for (int layer = 0; layer < layers; layer++)
			{
				float t = (float)layer / (layers - 1);
				float y = Mathf.Lerp(-half, half, t);
				float h = y / half;
				float weight = (Mathf.Exp(-h * h * falloff) - edge) / (1f - edge);

				int centerIndex = vi;
				verts[vi] = new Vector3(0f, y, 0f);
				colors[vi] = new Color(0f, 0f, 0f, weight);
				vi++;
				for (int s = 0; s <= segments; s++)
				{
					float a = (float)s / segments * (Mathf.PI * 2f);
					verts[vi] = new Vector3(Mathf.Cos(a), y, Mathf.Sin(a));
					colors[vi] = new Color(0f, 0f, 0f, weight);
					vi++;
				}
				for (int s = 1; s <= segments; s++)
				{
					// Winding is irrelevant: the shader draws with Cull Off,
					// because the disk is seen from above, below and inside.
					triangles[ti++] = centerIndex;
					triangles[ti++] = centerIndex + s;
					triangles[ti++] = centerIndex + s + 1;
				}
			}

			mesh.vertices = verts;
			mesh.colors = colors;
			mesh.triangles = triangles;
			mesh.RecalculateBounds();
		}

		private void Reset()
		{
			// Editor: the disk appears the instant the component is added.
			RebuildMesh();
		}

		private void OnValidate()
		{
			// Live preview while tuning geometry in the inspector. Play
			// mode builds in Awake; rebuilding here as well would just burn
			// a mesh per domain reload.
			if (!Application.isPlaying && GetComponent<MeshFilter>() != null)
			{
				RebuildMesh();
			}
		}
	}
}
