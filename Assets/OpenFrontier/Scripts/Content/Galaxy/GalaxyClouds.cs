using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenFrontier.Galaxy
{
	/// <summary>
	/// The galaxy's dust clouds -- a few hundred large, soft, camera-facing
	/// puffs in ONE merged mesh and ONE draw call (shader
	/// OpenFrontier/GalaxyBillboards, different material).
	///
	/// WHY: alongside GalaxyStars this is the close-range structure of the
	/// reference look. Stars give the pointillism; clouds give the arm
	/// structure depth -- the lumpy, volumetric feel that survives the
	/// camera flying into the disk, where the smooth disk shader has
	/// already run out of detail. The disk stays the diffuse base.
	///
	/// Same machinery as the stars: billboarding in the vertex shader,
	/// deterministic placement from Seed via GalaxyDistribution, bounds
	/// padded past the billboard centres, one draw call. The differences
	/// are the defaults -- fewer, larger, softer, broader arm spread --
	/// and the fill-rate reality: clouds are the expensive layer per
	/// pixel, so COUNT is the first knob down on a weak device.
	///
	/// The material's Near Fade (world units) is what keeps a puff from
	/// covering the lens when the camera sits inside the population, and
	/// keeps a sky capture clean at the destination anchor. Defaults live
	/// on the material (stars fade near; clouds fade farther out).
	///
	/// Author it as a child of the Galactic Disk object, wire FitTo, layer
	/// Celestial (8) -- same as the stars.
	/// </summary>
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class GalaxyClouds : MonoBehaviour
	{
		[Header("Population")]
		[Tooltip("Cloud count. 500 is the starting budget; this layer is the fill-rate cost of the galaxy -- lower it first on weak devices.")]
		[Min(0)]
		public int Count = 500;

		[Tooltip("Deterministic seed. Same seed, same clouds, every session -- integer splitmix64 decisions; placement is scenery, never sim state.")]
		public int Seed = 2;

		[Header("Distribution (unit disk; keep in step with the disk material)")]
		[Range(0f, 1f)]
		public float BulgeFraction = 0.25f;

		[Range(0.02f, 0.6f)]
		public float BulgeRadius = 0.3f;

		[Range(0.5f, 12f)]
		public float DiskFalloff = 4.5f;

		[Range(0f, 1f)]
		public float ArmWeight = 0.85f;

		[Range(1, 8)]
		public int ArmCount = 2;

		[Range(0f, 14f)]
		public float Winding = 5f;

		[Tooltip("Gaussian spread around an arm's crest, in radians per arm. Broader than the stars: clouds ARE the arm's body.")]
		[Range(0.05f, 2f)]
		public float ArmSpread = 1f;

		[Header("Fit (wiring the sibling disk wins over the fields below)")]
		public GalaxyDisk FitTo;

		[Tooltip("Fallback thickness fraction when nothing is wired -- match the disk's Thickness.")]
		[Min(0.004f)]
		public float ThicknessFraction = 0.06f;

		[Range(0.5f, 6f)]
		public float VerticalFalloff = 2.2f;

		[Header("Look")]
		[Tooltip("Cloud size range in LOCAL units (times the transform scale). 0.02-0.1 on a 3000-radius galaxy = 60-300 unit puffs.")]
		public Vector2 SizeRange = new Vector2(0.02f, 0.1f);

		[Tooltip("Brightness distribution power: higher = fewer bright, many dim.")]
		[Range(0.5f, 4f)]
		public float BrightnessPower = 1.5f;

		private Mesh generatedMesh;

		private void Awake()
		{
			RebuildMesh();
			WarnIfUnanchored();
		}

		private const string MeshName = "GalaxyClouds (generated)";

		/// <summary>Regenerate the population. Runs when the component is
		/// added (Reset), when a field changes in the inspector
		/// (OnValidate), and on Awake in play mode. Deterministic: the same
		/// Seed always rebuilds to the same mesh.
		///
		/// One mesh instance is REUSED across rebuilds (Clear + refill,
		/// never destroyed): Unity REFUSES DestroyImmediate during
		/// OnValidate outright, and swapping in a fresh mesh each time
		/// would embed a new orphaned copy into the scene on every save.
		/// The marker name lets a scene reload ADOPT the copy the last
		/// save embedded, so the scene carries exactly one mesh per
		/// component.</summary>
		[ContextMenu("Rebuild Cloud Mesh")]
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
			int count = Mathf.Clamp(Count, 0, 4000);
			float half = FitTo != null ? FitTo.Thickness * 0.5f : ThicknessFraction * 0.5f;
			float falloff = FitTo != null ? FitTo.VerticalFalloff : VerticalFalloff;
			float edge = Mathf.Exp(-falloff);
			float sizeMin = Mathf.Max(SizeRange.x, 0.0001f);
			float sizeMax = Mathf.Max(SizeRange.y, sizeMin);

			Vector3[] verts = new Vector3[count * 4];
			Vector2[] corners = new Vector2[count * 4];
			Vector4[] data = new Vector4[count * 4];
			int[] triangles = new int[count * 6];

			ulong state = GalaxyDistribution.Stream(Seed);
			Vector2[] quadCorners =
			{
				new Vector2(-1f, -1f), new Vector2(1f, -1f),
				new Vector2(-1f, 1f), new Vector2(1f, 1f),
			};

			for (int i = 0; i < count; i++)
			{
				GalaxyDistribution.SamplePosition(ref state,
					BulgeFraction, BulgeRadius, DiskFalloff,
					ArmCount, Winding, ArmWeight, ArmSpread,
					out float radius, out float angle);
				float cos = Mathf.Cos(angle);
				float sin = Mathf.Sin(angle);

				// Height across the disk's thickness profile, same as the
				// stars: gaussian in normalised half-thickness units, then
				// the disk's normalised exp weight, clamped at zero.
				float hNormal = Mathf.Clamp(
					GalaxyDistribution.Gaussian(ref state, 0.45f), -1.5f, 1.5f);
				float y = hNormal * half;
				float weight = Mathf.Max(0f,
					(Mathf.Exp(-hNormal * hNormal * falloff) - edge) / (1f - edge));

				float size = Mathf.Lerp(sizeMin, sizeMax,
					Mathf.Pow(GalaxyDistribution.Unit(ref state), 2f));
				float brightness = Mathf.Pow(
					GalaxyDistribution.Unit(ref state), BrightnessPower);

				Vector3 center = new Vector3(radius * cos, y, radius * sin);
				for (int k = 0; k < 4; k++)
				{
					int v = i * 4 + k;
					verts[v] = center;
					corners[v] = quadCorners[k];
					data[v] = new Vector4(size, brightness, weight, 0f);
				}

				int t = i * 6;
				int b = i * 4;
				triangles[t] = b;
				triangles[t + 1] = b + 1;
				triangles[t + 2] = b + 2;
				triangles[t + 3] = b + 2;
				triangles[t + 4] = b + 1;
				triangles[t + 5] = b + 3;
			}

			// 4000 clouds x 4 = 16000 vertices, under the 16-bit ceiling --
			// but the check stays so the clamp can grow without a silent
			// index overflow.
			if (count * 4 > 65535)
			{
				mesh.indexFormat = IndexFormat.UInt32;
			}
			if (count > 0)
			{
				mesh.vertices = verts;
				mesh.uv = corners;
				mesh.SetUVs(1, new List<Vector4>(data));
				mesh.triangles = triangles;
			}
			// Bounds SET, not recalculated -- billboard centres only; see
			// GalaxyStars. Clouds are the largest billboards, so the
			// padding matters most here.
			mesh.bounds = new Bounds(
				Vector3.zero,
				new Vector3(2f + 2f * sizeMax, 2f * (half + sizeMax), 2f + 2f * sizeMax));
		}

		private void Reset()
		{
			RebuildMesh();
		}

		private void OnValidate()
		{
			if (!Application.isPlaying && GetComponent<MeshFilter>() != null)
			{
				RebuildMesh();
			}
			WarnIfUnanchored();
		}

		/// <summary>The billboard contract: positions and sizes are LOCAL
		/// units, and the object's world scale turns them into world size.
		/// A point baked at 0.001-0.02 local units renders at a thousandth
		/// of a unit unless the transform chain carries the galaxy's scale
		/// -- and additive black on the black sky renders as NOTHING, which
		/// reads as a broken feature rather than a broken hierarchy. The
		/// wired-but-not-parented state is exactly that silent failure, so
		/// it is made loud.</summary>
		private void WarnIfUnanchored()
		{
			if (FitTo != null && transform.parent != FitTo.transform)
			{
				Debug.LogWarning(
					$"[{GetType().Name}] '{name}' wires FitTo to '{FitTo.name}' but is not its child. Baked positions/sizes are LOCAL units: without the galaxy's scale in the transform chain, a star is ~0.001 world units -- invisible. Drag this object under '{FitTo.name}' in the Hierarchy (or unwire FitTo and scale this object to the galaxy yourself).",
					this);
			}
		}
	}
}
