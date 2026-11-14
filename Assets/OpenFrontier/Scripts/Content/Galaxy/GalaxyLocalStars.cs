using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenFrontier.Galaxy
{
	/// <summary>
	/// The LOCAL sky: a small shell of individual stars generated around
	/// the viewpoint, sampled from the sibling GalaxyStars' model at the
	/// local density (shader OpenFrontier/GalaxyBillboards, own
	/// material).
	///
	/// WHY: the global mesh is the galaxy's FAR view -- a few tens of
	/// thousands of weighted points standing in for billions of model
	/// stars. Inside the population, any small neighbourhood contains
	/// almost none of them: the local sky reads empty away from the
	/// core. This component is the second tier of the SAME model --
	/// "billions of stars" as a density function (GalaxyDistribution's
	/// samplers), rendered through a bounded budget that follows the
	/// viewpoint. Elite's trick, minus the storage.
	///
	/// DETERMINISM BY CELL: the viewpoint's galaxy-local position is
	/// quantised to a grid (cell edge = half the shell radius). The
	/// shell builds around the CELL CENTRE from a seed derived from the
	/// cell coordinates -- the same cell of the same seed is the same
	/// sky, every revisit, nothing stored. Crossing a cell rebuilds the
	/// shell (an authoring-time pop; the sky capture is static and
	/// never pops).
	///
	/// HANDOFF: the global mesh's near fade removes its stars close to
	/// the camera while this shell supplies the near field -- the two
	/// tiers compose into one sky. Give the shell its OWN material
	/// (duplicate the stars material): Near Fade ~0/40, Proximity
	/// Boost on (near stars blaze), Radial Dimming low (rim regions
	/// stay bright -- the point of the shell). Local positions need no
	/// arm snapping: the shader's inter-arm band shading supplies the
	/// arm contrast at any radius.
	///
	/// WIRING: child of the disk at its ORIGIN, scale 1 (the cell grid
	/// is this object's local frame), ModelSource = the GalaxyStars
	/// component (the shell samples ITS distribution knobs; one owner,
	/// no drift), Anchor = the viewpoint (the Galaxy Camera while
	/// authoring; the M2b capture rig calls RebuildAt at the capture
	/// position). Unwired Anchor falls back to Camera.main. Layer:
	/// Celestial (8), with the rest of the celestial dressing.
	///
	/// MOBILE: one extra draw call, ~16k vertices at the default count,
	/// rebuilt only on cell crossings (throttled by MinRebuildInterval);
	/// the per-frame cost of the check is one matrix multiply and three
	/// floor calls.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class GalaxyLocalStars : MonoBehaviour
	{
		[Header("Population")]
		[Tooltip("Shell star count: individual nearby stars, ONE extra draw call. 20000 ceiling (~80k vertices, ~3 MB) is trivial next to the global mesh.")]
		[Min(0)]
		public int Count = 4000;

		[Tooltip("Deterministic base seed. The shell is keyed to the spatial CELL as well: the same cell of the same seed is the same sky, every revisit, nothing stored.")]
		public int Seed = 3;

		[Header("Shell")]
		[Tooltip("Shell radius in LOCAL units (x the transform scale): how far individual stars are generated around the viewpoint. 0.08 on a 3000-scale galaxy = a 240-unit sphere. The sky rebuilds when the viewpoint crosses a cell of half this size.")]
		[Range(0.01f, 0.5f)]
		public float ShellRadius = 0.08f;

		[Tooltip("Minimum seconds between rebuilds: a viewpoint dragged across many cells rebuilds at most ~10x per second, never per frame.")]
		[Range(0f, 2f)]
		public float MinRebuildInterval = 0.1f;

		[Tooltip("The viewpoint the local sky is built around. Wire the Galaxy Camera while authoring; the M2b capture rig pins the capture position via RebuildAt. Unwired: falls back to Camera.main.")]
		public Transform Anchor;

		[Tooltip("Fallback thickness fraction when the model source's own FitTo is unwired -- match the disk's Thickness.")]
		[Min(0.004f)]
		public float ThicknessFraction = 0.06f;

		[Header("Model (read from the wired sibling -- one owner, no drift)")]
		[Tooltip("The GalaxyStars component whose distribution knobs define the model. Required: unwired, there is nothing to sample and the shell stays empty (loudly).")]
		public GalaxyStars ModelSource;

		[Header("Look")]
		[Tooltip("Shell star size range in LOCAL units. These are INDIVIDUAL stars, not buckets: 0.0004-0.0015 on a 3000-radius galaxy = 1.2-4.5 unit points.")]
		public Vector2 SizeRange = new Vector2(0.0004f, 0.0015f);

		[Tooltip("Brightness distribution power: higher = fewer bright, many dim.")]
		[Range(0.5f, 4f)]
		public float BrightnessPower = 2f;

		private Mesh generatedMesh;
		private Camera mainCamera;
		private Vector3Int lastCell;
		private bool hasCell;
		private float lastBuildTime = -999f;

		private const string MeshName = "GalaxyLocalStars (generated)";

		/// <summary>Hard ceiling on Count: 20000 stars x 4 = 80000
		/// vertices (~2.9 MB) -- the shell is one draw call around one
		/// viewpoint, nothing more.</summary>
		private const int MaxCount = 20000;

		/// <summary>The cell grid edge as a fraction of the shell
		/// radius: half the radius per cell step keeps the guaranteed
		/// coverage around the viewpoint at ~0.65 x the radius (the
		/// worst-case corner offset is sqrt(2)/2 x cell) while
		/// re-rolling at most every half radius of travel.</summary>
		private const float CellFraction = 0.5f;

		/// <summary>The thin disk population's vertical sigma, in
		/// half-thickness units -- the same shape the global sampler
		/// uses (and the same constant LocalPopulationWeights
		/// normalises by).</summary>
		private const float DiskSigmaH = 0.45f;

		private void Awake()
		{
			MeshFilter filter = GetComponent<MeshFilter>();
			if (filter != null)
			{
				AdoptMesh(filter);
			}
			WarnIfUnanchored();
		}

		private void Update()
		{
			if (!isActiveAndEnabled || !TryResolveAnchor(out Vector3 world))
			{
				return;
			}

			float cs = CellSize();
			Vector3 local = transform.InverseTransformPoint(world);
			Vector3Int cell = new Vector3Int(
				Mathf.FloorToInt(local.x / cs),
				Mathf.FloorToInt(local.y / cs),
				Mathf.FloorToInt(local.z / cs));

			if (hasCell && cell == lastCell)
			{
				return;
			}
			// The interval backstop throttles flights through many
			// cells; a FIRST build (no cell yet) always runs.
			if (hasCell
				&& Time.realtimeSinceStartup - lastBuildTime < MinRebuildInterval)
			{
				return;
			}
			RebuildAt(world);
		}

		private void Reset()
		{
			RebuildAtAnchor();
		}

		private void OnValidate()
		{
			if (!Application.isPlaying
				&& TryResolveAnchor(out Vector3 world))
			{
				RebuildAt(world);
			}
			WarnIfUnanchored();
		}

		private float CellSize()
		{
			return Mathf.Max(ShellRadius * CellFraction, 1e-4f);
		}

		private bool TryResolveAnchor(out Vector3 world)
		{
			if (Anchor != null)
			{
				world = Anchor.position;
				return true;
			}
			// Camera.main is a lookup -- lazy, refreshed only when the
			// cached one is gone (Unity's overloaded null covers
			// destroyed).
			if (mainCamera == null)
			{
				mainCamera = Camera.main;
			}
			if (mainCamera != null)
			{
				world = mainCamera.transform.position;
				return true;
			}
			world = default;
			return false;
		}

		/// <summary>Rebuild around the resolved anchor (menu item for a
		/// manual nudge while authoring).</summary>
		[ContextMenu("Rebuild At Anchor")]
		public void RebuildAtAnchor()
		{
			if (TryResolveAnchor(out Vector3 world))
			{
				RebuildAt(world);
			}
			else
			{
				Debug.LogWarning(
					$"[{GetType().Name}] '{name}' has no anchor to build around: wire Anchor (the Galaxy Camera) or tag a camera MainCamera.",
					this);
			}
		}

		/// <summary>Rebuild the shell around a world-space position.
		/// The M2b capture rig's entry point: pin the capture position,
		/// build once, capture the cubemap. The shell is quantised to
		/// the CELL containing the position and built around that
		/// cell's centre -- the same cell always yields the same
		/// stars.</summary>
		public void RebuildAt(Vector3 worldPosition)
		{
			MeshFilter filter = GetComponent<MeshFilter>();
			if (filter == null)
			{
				return;
			}

			float cs = CellSize();
			Vector3 local = transform.InverseTransformPoint(worldPosition);
			lastCell = new Vector3Int(
				Mathf.FloorToInt(local.x / cs),
				Mathf.FloorToInt(local.y / cs),
				Mathf.FloorToInt(local.z / cs));
			hasCell = true;
			lastBuildTime = Time.realtimeSinceStartup;

			Mesh mesh = AdoptMesh(filter);
			Vector3 centerLocal = new Vector3(
				(lastCell.x + 0.5f) * cs,
				(lastCell.y + 0.5f) * cs,
				(lastCell.z + 0.5f) * cs);
			FillMesh(mesh, centerLocal);
		}

		/// <summary>One mesh instance REUSED across rebuilds (Clear +
		/// refill, never destroyed) -- same lifecycle contract as the
		/// siblings: Unity refuses DestroyImmediate during OnValidate,
		/// and replace-and-leak embeds an orphaned mesh per save. The
		/// marker name lets a scene reload ADOPT the copy the last save
		/// embedded.</summary>
		private Mesh AdoptMesh(MeshFilter filter)
		{
			Mesh mesh = generatedMesh;
			if (mesh == null)
			{
				mesh = filter.sharedMesh;
				if (mesh == null || mesh.name != MeshName)
				{
					mesh = new Mesh { name = MeshName };
				}
				generatedMesh = mesh;
			}
			filter.sharedMesh = mesh;
			return mesh;
		}

		private void FillMesh(Mesh mesh, Vector3 centerLocal)
		{
			mesh.Clear();
			int count = Mathf.Clamp(Count, 0, MaxCount);
			float half = ModelSource != null && ModelSource.FitTo != null
				? ModelSource.FitTo.Thickness * 0.5f
				: ThicknessFraction * 0.5f;

			if (ModelSource == null)
			{
				// The loud version lives in WarnIfUnanchored; here the
				// shell simply has no model to sample from.
				mesh.bounds = new Bounds(centerLocal, Vector3.zero);
				return;
			}

			// The local population mix: the model's density at the CELL
			// CENTRE (the stars' origin), mass-weighted per population.
			float hCenter = centerLocal.y / half;
			float diskMass = Mathf.Max(0f,
				1f - ModelSource.CoreFraction - ModelSource.HaloFraction);
			GalaxyDistribution.LocalPopulationWeights(
				new Vector2(centerLocal.x, centerLocal.z), hCenter,
				ModelSource.DiskFalloff, ModelSource.DiskHeight,
				ModelSource.VerticalFalloff,
				ModelSource.BarLength, ModelSource.BarWidth,
				ModelSource.BarAngle, ModelSource.CoreHeight,
				ModelSource.HaloRadius, ModelSource.HaloHeight,
				diskMass, ModelSource.CoreFraction, ModelSource.HaloFraction,
				out float wDisk, out float wCore, out float wHalo);
			float total = wDisk + wCore + wHalo;

			// Far from every population the weights underflow to zero:
			// an empty local sky is the CORRECT answer out there.
			if (count == 0 || total <= 0f)
			{
				mesh.bounds = new Bounds(centerLocal, Vector3.zero);
				return;
			}

			float sizeMin = Mathf.Max(SizeRange.x, 0.00005f);
			float sizeMax = Mathf.Max(SizeRange.y, sizeMin);
			float vFall = ModelSource.VerticalFalloff;
			float vEdge = Mathf.Exp(-vFall);
			float sigmaYMax = Mathf.Max(DiskSigmaH * ModelSource.DiskHeight,
				Mathf.Max(ModelSource.CoreHeight, ModelSource.HaloHeight));

			Vector3[] verts = new Vector3[count * 4];
			Vector2[] corners = new Vector2[count * 4];
			Vector4[] data = new Vector4[count * 4];
			int[] triangles = new int[count * 6];

			// Cell-keyed determinism: the same cell of the same seed is
			// the same sky, every revisit, nothing stored.
			ulong state = GalaxyDistribution.CellSeed(
				Seed, lastCell.x, lastCell.y, lastCell.z);
			Vector2[] quadCorners =
			{
				new Vector2(-1f, -1f), new Vector2(1f, -1f),
				new Vector2(-1f, 1f), new Vector2(1f, 1f),
			};

			for (int i = 0; i < count; i++)
			{
				// Population by local density share.
				float pick = GalaxyDistribution.Unit(ref state) * total;
				float sigmaH;
				float hMul;
				bool diskPop;
				if (pick < wDisk)
				{
					diskPop = true;
					sigmaH = DiskSigmaH * ModelSource.DiskHeight;
					hMul = ModelSource.DiskHeight;
				}
				else if (pick < wDisk + wCore)
				{
					diskPop = false;
					sigmaH = ModelSource.CoreHeight;
					hMul = ModelSource.CoreHeight;
				}
				else
				{
					diskPop = false;
					sigmaH = ModelSource.HaloHeight;
					hMul = ModelSource.HaloHeight;
				}

				// xz: uniform disc around the cell centre. The local
				// patch of any population is radially flat at this
				// scale; the shader's band and presence terms supply
				// the global structure for free.
				float a = GalaxyDistribution.Unit(ref state) * Mathf.PI * 2f;
				float rr = ShellRadius
					* Mathf.Sqrt(GalaxyDistribution.Unit(ref state));
				float x = centerLocal.x + Mathf.Cos(a) * rr;
				float z = centerLocal.z + Mathf.Sin(a) * rr;

				// y: the population's vertical gaussian around the cell
				// centre, in absolute local units.
				float y = centerLocal.y
					+ GalaxyDistribution.Gaussian(ref state, sigmaH) * half;

				// Vertical brightness weight, the global rule: the
				// disk's curve runs from the PLANE (through the
				// DiskHeight stretch), core and halo from their own
				// sigma units.
				float hN = (y / half) / Mathf.Max(hMul, 0.05f);
				float weight = diskPop
					? Mathf.Max(0f,
						(Mathf.Exp(-hN * hN * vFall) - vEdge) / (1f - vEdge))
					: Mathf.Exp(-hN * hN * 0.25f);

				float size = Mathf.Lerp(sizeMin, sizeMax,
					Mathf.Pow(GalaxyDistribution.Unit(ref state), 2f));
				float brightness = Mathf.Pow(
					GalaxyDistribution.Unit(ref state), BrightnessPower);
				// Same bright-tail whitening as the global stars.
				float tint = Mathf.Clamp01((brightness - 0.6f) * 2.5f);

				Vector3 center = new Vector3(x, y, z);
				for (int k = 0; k < 4; k++)
				{
					int v = i * 4 + k;
					verts[v] = center;
					corners[v] = quadCorners[k];
					data[v] = new Vector4(size, brightness, weight, tint);
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
			// Bounds centred on the CELL CENTRE (the stars live around
			// it), padded by the largest size and the tallest
			// population's 4-sigma reach. Exact: every sample clamps at
			// 4 sigma.
			float yExtent = 4f * half * sigmaYMax + sizeMax;
			float xzExtent = ShellRadius + sizeMax;
			mesh.bounds = new Bounds(centerLocal,
				new Vector3(2f * xzExtent, 2f * yExtent, 2f * xzExtent));
		}

		/// <summary>The shell's contract, made loud: it must be a child
		/// of the disk at its origin (scale contract -- LOCAL units),
		/// sampling a wired ModelSource. Any other state is invisible
		/// additive black or a wrong-volume sample, which reads as a
		/// broken feature.</summary>
		private void WarnIfUnanchored()
		{
			if (ModelSource == null)
			{
				Debug.LogWarning(
					$"[{GetType().Name}] '{name}' has no ModelSource -- wire the GalaxyStars component. The shell samples ITS distribution knobs (one owner, no drift); unwired, there is no model and the shell stays empty.",
					this);
				return;
			}

			Transform disk = ModelSource.FitTo != null
				? ModelSource.FitTo.transform
				: null;
			if (disk == null)
			{
				Debug.LogWarning(
					$"[{GetType().Name}] '{name}' -> ModelSource '{ModelSource.name}' has no FitTo wired, so the disk's thickness is unknown; falling back to ThicknessFraction ({ThicknessFraction}).",
					this);
			}
			else if (transform.parent != disk)
			{
				Debug.LogWarning(
					$"[{GetType().Name}] '{name}' is not a child of '{disk.name}'. The shell's cell grid and sizes are LOCAL units: without the galaxy's scale in the transform chain the local sky is invisible. Drag this object under '{disk.name}'.",
					this);
			}
			else if (transform.localPosition.magnitude > 0.001f
				|| Mathf.Abs(transform.localScale.x - 1f) > 0.001f)
			{
				Debug.LogWarning(
					$"[{GetType().Name}] '{name}' must sit at the disk's origin, scale 1: its cell grid is its own local frame, so an offset or scaled shell samples the wrong volume.",
					this);
			}
		}
	}
}
