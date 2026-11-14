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
	/// TWO POPULATIONS, same as the stars: the thin disk (arm-hugging --
	/// clouds ARE the arm's body) and the core bar, vertically thick, so
	/// the dust also fills the bulge's volume.
	///
	/// THE DUST SHADER: the material decides the look. Plain
	/// OpenFrontier/GalaxyBillboards gives additive puffs only -- which
	/// washes bright along sightlines through the whole disk. Assign
	/// OpenFrontier/GalaxyDust instead for the two-term model: Scatter
	/// (glow following the starlight around each cloud -- core clouds
	/// glow more) plus Occlude (a RevSub pass that EATS the glow behind
	/// the cloud -- rim dust darkens the centre's light: the Great Rift
	/// behaviour). This component bakes the per-cloud occlusion into the
	/// mesh's second UV stream (TEXCOORD2); the dust shader's Occlude
	/// pass reads it, the billboards shader ignores the stream entirely,
	/// so the mesh is shader-agnostic and swapping materials never
	/// rebuilds it.
	///
	/// Author it as a child of the Galactic Disk object, wire FitTo, layer
	/// Celestial (8) -- same as the stars.
	/// </summary>
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class GalaxyClouds : MonoBehaviour
	{
		[Header("Population")]
		[Tooltip("Cloud count. 800 is the starting budget; this layer is the fill-rate cost of the galaxy -- lower it first on weak devices. The 6000 ceiling is a sky-capture budget.")]
		[Min(0)]
		public int Count = 800;

		[Tooltip("Deterministic seed. Same seed, same clouds, every session -- integer splitmix64 decisions; placement is scenery, never sim state.")]
		public int Seed = 2;

		[Header("Distribution (unit disk; keep in step with the disk material)")]
		[Tooltip("Share of the population in the CORE: the bar's volume, vertically thick. The rest hugs the thin disk's arms.")]
		[Range(0f, 0.9f)]
		public float CoreFraction = 0.22f;

		[Tooltip("Bar length along the bar axis, in unit-disk radii. Keep in step with the stars component and the disk material's painted core.")]
		[Range(0.05f, 0.5f)]
		public float BarLength = 0.2f;

		[Tooltip("Bar width across the bar axis, in unit-disk radii.")]
		[Range(0.03f, 0.4f)]
		public float BarWidth = 0.12f;

		[Tooltip("Bar axis angle in degrees -- keep in step with the disk material's Core Rotation. 0 = bar along local Z.")]
		[Range(-180f, 180f)]
		public float BarAngle = 0f;

		[Tooltip("Vertical thickness of the core population, in disk half-thicknesses (1 half-thickness is ~3% of the galaxy radius at the default 0.06 thickness).")]
		[Range(1f, 60f)]
		public float CoreHeight = 3.5f;

		[Tooltip("Generic height of the THIN DISK population, in half-thicknesses (1 = the painted sheet's own profile). The core has its own CoreHeight.")]
		[Range(1f, 60f)]
		public float DiskHeight = 2f;

		[Tooltip("Share of the population in the HALO: sparse dust in a big round volume around the whole galaxy. Elite's dust hugs the plane, so keep this small. 0 disables.")]
		[Range(0f, 0.9f)]
		public float HaloFraction = 0.04f;

		[Tooltip("Halo radius (xz sigma), in unit-disk radii. The halo ignores the plane's radial structure by design.")]
		[Range(0.05f, 0.9f)]
		public float HaloRadius = 0.5f;

		[Tooltip("Halo height (y sigma), in half-thicknesses.")]
		[Range(1f, 120f)]
		public float HaloHeight = 8f;

		[Tooltip("Inner radius where arm snapping fades out.")]
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

		[Tooltip("Per-cloud occlusion shaping (the dust shader's Occlude pass reads it from TEXCOORD2): higher = fewer but denser dark clouds; 0 = every cloud occludes equally. The dust shader's Dust Occlusion property scales the result globally.")]
		[Range(0f, 4f)]
		public float OcclusionPower = 1.5f;

		private Mesh generatedMesh;

		private void Awake()
		{
			RebuildMesh();
			WarnIfUnanchored();
		}

		private const string MeshName = "GalaxyClouds (generated)";

		/// <summary>Hard ceiling on Count: 6000 clouds x 4 = 24000
		/// vertices, but clouds are the FILL-RATE cost of the galaxy --
		/// the ceiling is a capture-time budget, not a play budget.</summary>
		private const int MaxCount = 6000;

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
			// Per-cloud occlusion, its own UV stream (TEXCOORD2): the
			// dust shader's Occlude pass eats glow behind the quad with
			// it; the plain billboards shader never reads the stream, so
			// the mesh stays shader-agnostic.
			Vector4[] occlusion = new Vector4[count * 4];
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
					// structure. hN scaled by half x HaloHeight.
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

				// Vertical brightness weight, same split as the stars:
				// the disk's own profile on the sheet, a gentler curve in
				// the core's and halo's volume. data.w stays 0 -- dust
				// does not whiten.
				float weight = thick
					? Mathf.Exp(-hN * hN * 0.25f)
					: Mathf.Max(0f,
						(Mathf.Exp(-hN * hN * falloff) - edge) / (1f - edge));

				float size = Mathf.Lerp(sizeMin, sizeMax,
					Mathf.Pow(GalaxyDistribution.Unit(ref state), 2f));
				float brightness = Mathf.Pow(
					GalaxyDistribution.Unit(ref state), BrightnessPower);
				// Per-cloud occlusion (TEXCOORD2): the dust shader's
				// Occlude pass subtracts this much of the glow behind the
				// quad. Power-shaped: mostly wispy, a few dense dark
				// clouds; 0 = every cloud at 1.
				float occl = Mathf.Pow(
					GalaxyDistribution.Unit(ref state), OcclusionPower);

				Vector3 center = new Vector3(x, y, z);
				for (int k = 0; k < 4; k++)
				{
					int v = i * 4 + k;
					verts[v] = center;
					corners[v] = quadCorners[k];
					data[v] = new Vector4(size, brightness, weight, 0f);
					occlusion[v] = new Vector4(occl, 0f, 0f, 0f);
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

			// The check stays even though the default count sits under
			// the 16-bit ceiling: the clamp can grow without a silent
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
				// Stream 2: the dust shader's occlusion channel (see the
				// arrays above). ~16 bytes per corner at the 6000 ceiling
				// -- noise next to this layer's fill cost.
				mesh.SetUVs(2, new List<Vector4>(occlusion));
				mesh.triangles = triangles;
			}
			// Bounds SET, not recalculated -- billboard centres only; see
			// GalaxyStars. Clouds are the largest billboards, so the
			// padding matters most here. Vertical extent grows with
			// whichever population reaches highest; the halo's round
			// spread can poke past the unit disk in xz, so both extents
			// take the larger bound. All samples clamp at 4 sigma, so
			// 4 x sigma is an EXACT bound, not a guess.
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
