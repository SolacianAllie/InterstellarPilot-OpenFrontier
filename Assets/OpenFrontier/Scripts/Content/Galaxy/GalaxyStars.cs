using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenFrontier.Galaxy
{
	/// <summary>
	/// The galaxy's discrete stars -- thousands of camera-facing points in
	/// ONE merged mesh and ONE draw call (shader
	/// OpenFrontier/GalaxyBillboards).
	///
	/// WHY: the disk shader's smooth fbm has a detail ceiling -- magnified,
	/// it is still gradients. The reference look survives close range
	/// because the galaxy resolves into DISCRETE elements: far away they
	/// blend into the painted structure, close up they separate into
	/// points. This component supplies the points; GalaxyClouds supplies
	/// the puffs; the disk stays the diffuse base.
	///
	/// Billboarding happens in the vertex shader from the camera position,
	/// so the mesh is built once (deterministically, from Seed) and never
	/// rebuilt per frame. Mesh bounds are padded by the largest size --
	/// vertices are billboard CENTRES, and unpadded bounds make
	/// edge-of-screen stars pop as their centre crosses the frustum while
	/// their quad is still visible.
	///
	/// Author it as a child of the Galactic Disk object: it shares the
	/// disk's transform scale and moves/resizes with the galaxy as one
	/// unit. Wire FitTo to the disk so thickness and vertical falloff are
	/// read from it -- one owner, no drift (the fields below are only the
	/// unwired fallback).
	///
	/// TWO POPULATIONS in the one mesh: the thin disk (arm-snapped,
	/// exponential, vertical profile matching the painted sheet) and the
	/// CORE -- a tri-axial bar plus a tighter nucleus, vertically thick.
	/// The core is where the galaxy stacks stars vertically; the thin
	/// disk's own profile spans only a fraction of its thickness, and
	/// that is correct, not a bug.
	///
	/// Layer: Celestial (8), with the rest of the celestial dressing.
	/// </summary>
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class GalaxyStars : MonoBehaviour
	{
		[Header("Population")]
		[Tooltip("Star count. 24000 default = ~96k vertices (~3.5 MB) built once, static thereafter. The 200000 ceiling (~33 MB with indices, a several-hundred-ms editor rebuild) is a SKY-CAPTURE budget: the galaxy lives only long enough to grab the cubemap, so a heavy burst is affordable. Per-frame cost is vertex shading only.")]
		[Min(0)]
		public int Count = 24000;

		[Tooltip("Deterministic seed. Same seed, same stars, every session -- integer splitmix64 decisions; placement is scenery, never sim state.")]
		public int Seed = 1;

		[Header("Distribution (unit disk; keep in step with the disk material)")]
		[Tooltip("Share of the population in the CORE: a tri-axial bar (plus a tighter nucleus) with real vertical extent. The rest is the thin disk.")]
		[Range(0f, 0.9f)]
		public float CoreFraction = 0.28f;

		[Tooltip("Bar length along the bar axis, in unit-disk radii. Match the disk material's painted core by eye.")]
		[Range(0.05f, 0.5f)]
		public float BarLength = 0.2f;

		[Tooltip("Bar width across the bar axis, in unit-disk radii.")]
		[Range(0.03f, 0.4f)]
		public float BarWidth = 0.12f;

		[Tooltip("Bar axis angle in degrees -- keep in step with the disk material's Core Rotation. 0 = bar along local Z.")]
		[Range(-180f, 180f)]
		public float BarAngle = 0f;

		[Tooltip("Vertical thickness of the core population, in disk half-thicknesses (1 half-thickness is ~3% of the galaxy radius at the default 0.06 thickness). Elite's bulge reaches ~10-20%; crank freely.")]
		[Range(1f, 60f)]
		public float CoreHeight = 3.5f;

		[Tooltip("Generic height of the THIN DISK population, in half-thicknesses (1 = the painted sheet's own profile). The core has its own CoreHeight; this stretches everything else vertically, falloff profile and all.")]
		[Range(1f, 60f)]
		public float DiskHeight = 2f;

		[Tooltip("Share of the population in the HALO: sparse stars in a big round volume around the whole galaxy -- the Elite behaviour of systems far above the disk. 0 disables.")]
		[Range(0f, 0.9f)]
		public float HaloFraction = 0.06f;

		[Tooltip("Halo radius (xz sigma), in unit-disk radii. The halo ignores the plane's radial structure by design.")]
		[Range(0.05f, 0.9f)]
		public float HaloRadius = 0.5f;

		[Tooltip("Halo height (y sigma), in half-thicknesses. 10 puts 1-sigma halo stars ~30% of the galaxy radius above the plane at the default thickness; Elite reaches way up, so crank freely.")]
		[Range(1f, 120f)]
		public float HaloHeight = 10f;

		[Tooltip("Inner radius where arm snapping fades out -- also the radius the material's Bulge Brightening uses.")]
		[Range(0.02f, 0.6f)]
		public float BulgeRadius = 0.3f;

		[Range(0.5f, 12f)]
		public float DiskFalloff = 4.5f;

		[Range(0f, 1f)]
		public float ArmWeight = 0.75f;

		[Range(1, 8)]
		public int ArmCount = 2;

		[Range(0f, 14f)]
		public float Winding = 5f;

		[Tooltip("Gaussian spread around an arm's crest, in radians per arm. Match the disk material's Arm Softness by eye.")]
		[Range(0.05f, 2f)]
		public float ArmSpread = 0.5f;

		[Header("Fit (wiring the sibling disk wins over the fields below)")]
		public GalaxyDisk FitTo;

		[Tooltip("Fallback thickness fraction when nothing is wired -- match the disk's Thickness.")]
		[Min(0.004f)]
		public float ThicknessFraction = 0.06f;

		[Range(0.5f, 6f)]
		public float VerticalFalloff = 2.2f;

		[Header("Look")]
		[Tooltip("Star size range in LOCAL units (times the transform scale). 0.001-0.004 on a 3000-radius galaxy = 3-12 unit points.")]
		public Vector2 SizeRange = new Vector2(0.001f, 0.004f);

		[Tooltip("Brightness distribution power: higher = fewer bright, many dim.")]
		[Range(0.5f, 4f)]
		public float BrightnessPower = 2f;

		private Mesh generatedMesh;

		private void Awake()
		{
			RebuildMesh();
			WarnIfUnanchored();
		}

		private const string MeshName = "GalaxyStars (generated)";

		/// <summary>Hard ceiling on Count. The galaxy renders for seconds
		/// (the sky capture window), so the ceiling is a one-shot memory
		/// budget, not a play budget: 200000 stars = 800k vertices
		/// (~29 MB) plus ~4.8 MB of indices.</summary>
		private const int MaxCount = 200000;

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
		[ContextMenu("Rebuild Star Mesh")]
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
			int count = Mathf.Clamp(Count, 0, MaxCount);
			float half = FitTo != null ? FitTo.Thickness * 0.5f : ThicknessFraction * 0.5f;
			float falloff = FitTo != null ? FitTo.VerticalFalloff : VerticalFalloff;
			float edge = Mathf.Exp(-falloff);
			float sizeMin = Mathf.Max(SizeRange.x, 0.0001f);
			float sizeMax = Mathf.Max(SizeRange.y, sizeMin);
			int coreCount = Mathf.RoundToInt(count * Mathf.Clamp01(CoreFraction));
			int haloCount = Mathf.RoundToInt(count * Mathf.Clamp01(HaloFraction));

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
				float x, y, z, hN;
				// Core and halo share the gentle vertical curve; the disk
				// keeps the sheet's own profile.
				bool thick = i < coreCount + haloCount;
				if (i < coreCount)
				{
					// Core: bar + nucleus, vertically thick. hN stays in
					// normalised units -- scaled by half x CoreHeight.
					GalaxyDistribution.SampleCore(ref state,
						BarLength, BarWidth, BarAngle, out Vector3 core);
					x = core.x;
					z = core.z;
					hN = core.y;
					y = hN * half * CoreHeight;
				}
				else if (i < coreCount + haloCount)
				{
					// Halo: sparse, round, ignoring the plane's radial
					// structure -- systems far above the disk. hN scaled
					// by half x HaloHeight.
					GalaxyDistribution.SampleHalo(ref state,
						HaloRadius, out Vector3 halo);
					x = halo.x;
					z = halo.z;
					hN = halo.y;
					y = hN * half * HaloHeight;
				}
				else
				{
					GalaxyDistribution.SampleDisk(ref state,
						DiskFalloff, ArmCount, Winding, ArmWeight, ArmSpread,
						BulgeRadius, out float radius, out float angle);
					x = radius * Mathf.Cos(angle);
					z = radius * Mathf.Sin(angle);
					hN = Mathf.Clamp(
						GalaxyDistribution.Gaussian(ref state, 0.45f), -1.5f, 1.5f);
					y = hN * half * DiskHeight;
				}

				// Vertical brightness weight. Disk stars carry the disk's
				// own normalised exp profile so they never glow outside
				// the painted sheet; core and halo carry a gentler curve
				// -- that vertical density is the point, and the sheet's
				// profile would erase it.
				float weight = thick
					? Mathf.Exp(-hN * hN * 0.25f)
					: Mathf.Max(0f,
						(Mathf.Exp(-hN * hN * falloff) - edge) / (1f - edge));

				float size = Mathf.Lerp(sizeMin, sizeMax,
					Mathf.Pow(GalaxyDistribution.Unit(ref state), 2f));
				float brightness = Mathf.Pow(
					GalaxyDistribution.Unit(ref state), BrightnessPower);

				// The bright tail pushes toward hot white (data.w): at
				// high counts the field reads as temperature variation
				// rather than one uniform tint. The shader lerps by it.
				float tint = Mathf.Clamp01((brightness - 0.6f) * 2.5f);

				Vector3 center = new Vector3(x, y, z);
				for (int k = 0; k < 4; k++)
				{
					int v = i * 4 + k;
					verts[v] = center;
					corners[v] = quadCorners[k];
					data[v] = new Vector4(size, brightness, weight, tint);
				}

				// Winding is irrelevant: the shader draws with Cull Off.
				int t = i * 6;
				int b = i * 4;
				triangles[t] = b;
				triangles[t + 1] = b + 1;
				triangles[t + 2] = b + 2;
				triangles[t + 3] = b + 2;
				triangles[t + 4] = b + 1;
				triangles[t + 5] = b + 3;
			}

			// The ceiling sits far past the 16-bit index range, so the
			// format is CHOSEN by the count, never assumed.
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
			// Bounds are SET, not recalculated: the vertices are billboard
			// centres and the quads extend past them. Unpadded bounds
			// frustum-cull edge-of-screen billboards that are still
			// on-screen -- popping with no error anywhere to explain it.
			// Vertical extent grows with whichever population reaches
			// highest (core, stretched disk, or halo); the halo's round
			// spread can also poke past the unit disk in xz, so both
			// extents take the larger bound. All samples clamp at 4
			// sigma, so 4 x sigma is an EXACT bound, not a guess.
			float yExtent = Mathf.Max(
				half + sizeMax,
				4f * half * Mathf.Max(CoreHeight,
					Mathf.Max(DiskHeight, HaloHeight)));
			float xzExtent = Mathf.Max(1f + sizeMax, 4f * HaloRadius);
			mesh.bounds = new Bounds(
				Vector3.zero,
				new Vector3(2f * xzExtent, 2f * yExtent, 2f * xzExtent));
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
