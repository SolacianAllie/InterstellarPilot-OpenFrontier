using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenFrontier.Galaxy
{
	/// <summary>
	/// The LOCAL sky: a small shell of individual stars generated around
	/// the viewpoint (shader OpenFrontier/GalaxyStars, own material with
	/// the near-fade disabled -- near stars must blaze).
	///
	/// WHY: the baked face and the hero mesh are the galaxy's FAR view.
	/// Any small neighbourhood of the population contains almost none of
	/// them: the local sky reads empty, especially looking outward past
	/// the rim where the baked quad ends. This component is the second
	/// tier of the same model -- "billions of stars" as a density the
	/// viewpoint carries with it. Elite's trick, minus the storage.
	///
	/// DETERMINISM BY CELL: the viewpoint's world position is quantised
	/// to a grid (cell edge = half the shell radius). The shell builds
	/// around the CELL CENTRE from a seed derived from the cell
	/// coordinates -- the same cell of the same seed is the same sky,
	/// every revisit, nothing stored. Crossing a cell rebuilds the shell
	/// (an authoring-time pop; the sky capture is static and never
	/// pops).
	///
	/// WIRING: object at the Galaxy root, SCALE 1 -- this shell works in
	/// WORLD units (the dressing root's x3000 scale must not touch it).
	/// Model = the GalaxyBackdrop (one owner, no drift: population mix
	/// and palette come from its Params at the cell's galaxy position).
	/// Anchor = the viewpoint (the Galaxy Camera while authoring; the
	/// M2b capture rig calls RebuildAt at the capture position).
	/// Unwired Anchor falls back to Camera.main.
	///
	/// MOBILE: one extra draw call, ~16k vertices at the default count,
	/// rebuilt only on cell crossings (throttled by MinRebuildInterval);
	/// the per-frame cost of the check is three floor calls.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class GalaxyLocalStars : MonoBehaviour
	{
		[Header("Population")]
		[Tooltip("Shell star count: individual nearby stars, ONE extra draw call. 20000 ceiling (~80k vertices, ~3 MB).")]
		[Min(0)]
		public int Count = 4000;

		[Tooltip("Deterministic base seed. The shell is keyed to the spatial CELL as well: the same cell of the same seed is the same sky, every revisit, nothing stored.")]
		public int Seed = 7;

		[Header("Shell (world units -- this object sits at scale 1)")]
		[Tooltip("How far individual stars are generated around the viewpoint. The sky rebuilds when the viewpoint crosses a cell of half this size.")]
		[Range(20f, 1000f)]
		public float ShellRadius = 260f;

		[Tooltip("Minimum seconds between rebuilds: a viewpoint dragged across many cells rebuilds at most ~4x per second, never per frame.")]
		[Range(0f, 2f)]
		public float MinRebuildInterval = 0.25f;

		[Tooltip("The viewpoint the local sky is built around. Wire the Galaxy Camera while authoring; the M2b capture rig pins the capture position via RebuildAt. Unwired: falls back to Camera.main.")]
		public Transform Anchor;

		[Header("Model (one owner, no drift)")]
		[Tooltip("The GalaxyBackdrop whose Params define the model. Required: unwired, there is nothing to sample and the shell stays empty (loudly).")]
		public GalaxyBackdrop Model;

		[Header("Look")]
		[Tooltip("Shell star size range in WORLD units. These are INDIVIDUAL stars, not buckets.")]
		public Vector2 SizeRange = new Vector2(0.5f, 2.2f);

		[Tooltip("Brightness distribution power: higher = fewer bright, many dim.")]
		[Range(0.5f, 4f)]
		public float BrightnessPower = 2.2f;

		[Tooltip("Vertical spread of the local cloud (world units) -- how thick the star cloud around the viewpoint feels.")]
		[Range(10f, 800f)]
		public float VerticalSpread = 90f;

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
		/// coverage around the viewpoint at ~0.65 x the radius.</summary>
		private const float CellFraction = 0.5f;

		/// <summary>Share of shell stars in the ROUND (halo-like)
		/// vertical spread -- the overhead sky must not be empty at the
		/// camera's height above the plane.</summary>
		private const float RoundShare = 0.3f;

		/// <summary>Nearby-star spectral variety (the local sky shows
		/// individual stars, not population averages): warm white
		/// dominant, with gold, blue-white, orange and red accents.</summary>
		private static readonly Color[] Spectra =
		{
			new Color(1.00f, 0.96f, 0.92f), // warm white
			new Color(1.00f, 0.96f, 0.92f),
			new Color(1.00f, 0.96f, 0.92f),
			new Color(1.00f, 0.88f, 0.66f), // gold
			new Color(1.00f, 0.88f, 0.66f),
			new Color(0.78f, 0.86f, 1.00f), // blue-white
			new Color(0.78f, 0.86f, 1.00f),
			new Color(1.00f, 0.72f, 0.42f), // orange
			new Color(1.00f, 0.52f, 0.40f), // red
		};

		private void Awake()
		{
			MeshFilter filter = GetComponent<MeshFilter>();
			if (filter != null)
			{
				AdoptMesh(filter);
			}
			WarnIfUnwired();
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
			WarnIfUnwired();
		}

		private float CellSize()
		{
			return Mathf.Max(ShellRadius * CellFraction, 1e-3f);
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
		/// refill, never destroyed) -- Unity refuses DestroyImmediate
		/// during OnValidate, and replace-and-leak embeds an orphaned
		/// mesh per save. The marker name lets a scene reload ADOPT the
		/// copy the last save embedded.</summary>
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

		private void FillMesh(Mesh mesh, Vector3 center)
		{
			mesh.Clear();
			int count = Mathf.Clamp(Count, 0, MaxCount);

			if (Model == null || count == 0)
			{
				// The loud version lives in WarnIfUnwired; here the
				// shell simply has no model to sample from.
				mesh.bounds = new Bounds(center, Vector3.zero);
				return;
			}

			// The local population mix: the model's face at the CELL
			// CENTRE's galaxy position -- young arms tint the local sky
			// blue where the cell sits on an arm.
			Vector3 modelScale = Model.transform.lossyScale;
			float scale = Mathf.Max(Mathf.Max(modelScale.x, modelScale.y), modelScale.z);
			GalaxyModel.Face face = GalaxyModel.EvaluateFace(
				center.x / scale, center.z / scale, Model.Model);
			float youngFrac = Mathf.Clamp01(
				face.Young / Mathf.Max(face.Young + face.OldTotal, 1e-5f));

			Vector3[] verts = new Vector3[count * 4];
			Vector2[] corners = new Vector2[count * 4];
			Color[] colors = new Color[count * 4];
			Vector4[] data = new Vector4[count * 4];
			int[] triangles = new int[count * 6];

			// Cell-keyed determinism: the same cell of the same seed is
			// the same sky, every revisit, nothing stored.
			ulong state = GalaxyModel.CellSeed(
				Seed, lastCell.x, lastCell.y, lastCell.z);
			Vector2[] quadCorners =
			{
				new Vector2(-1f, -1f), new Vector2(1f, -1f),
				new Vector2(-1f, 1f), new Vector2(1f, 1f),
			};

			float sizeMin = Mathf.Max(SizeRange.x, 0.02f);
			float sizeMax = Mathf.Max(SizeRange.y, sizeMin);
			float spectrumCount = Spectra.Length;

			for (int i = 0; i < count; i++)
			{
				// xz: uniform disc around the cell centre. The local
				// patch of any population is radially flat at this
				// scale; the model supplies the global structure at a
				// distance, this shell supplies grain up close.
				float a = GalaxyModel.Unit(ref state) * Mathf.PI * 2f;
				float rr = ShellRadius
					* Mathf.Sqrt(GalaxyModel.Unit(ref state));
				float x = center.x + Mathf.Cos(a) * rr;
				float z = center.z + Mathf.Sin(a) * rr;

				// y: a flattened cloud around the viewpoint, with a
				// rounder share so the overhead sky stays alive.
				float sigmaY = GalaxyModel.Unit(ref state) < RoundShare
					? ShellRadius * 0.6f
					: VerticalSpread;
				float y = center.y + GalaxyModel.Gaussian(ref state, sigmaY) * 0.4f;

				float size = Mathf.Lerp(sizeMin, sizeMax,
					Mathf.Pow(GalaxyModel.Unit(ref state), 2f));
				float brightness = Mathf.Pow(
					GalaxyModel.Unit(ref state), BrightnessPower) * 2.0f + 0.15f;

				// Colour: the model's population mix at this cell, then
				// a per-star spectral pick (individual stars, not
				// population averages).
				Color popColor = Color.Lerp(GalaxyModel.DiskColor, GalaxyModel.ArmColor, youngFrac);
				int spectrumIndex = (int)(GalaxyModel.Unit(ref state) * spectrumCount) % Spectra.Length;
				Color col = Color.Lerp(popColor, Spectra[spectrumIndex], 0.55f) * brightness;

				Vector3 starCenter = new Vector3(x, y, z);
				for (int k = 0; k < 4; k++)
				{
					int v = i * 4 + k;
					verts[v] = starCenter;
					corners[v] = quadCorners[k];
					colors[v] = col;
					data[v] = new Vector4(size, 1f, 0f, 0f);
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
				mesh.colors = colors;
				mesh.SetUVs(1, new List<Vector4>(data));
				mesh.triangles = triangles;
			}
			// Bounds centred on the CELL CENTRE (the stars live around
			// it), padded by the shell radius and the tallest spread.
			float extent = ShellRadius + sizeMax;
			float yExtent = ShellRadius * 0.6f + VerticalSpread + sizeMax;
			mesh.bounds = new Bounds(center,
				new Vector3(2f * extent, 2f * yExtent, 2f * extent));
		}

		/// <summary>The shell's contract, made loud: it works in WORLD
		/// units at scale 1 and samples a wired Model. Any other state
		/// is invisible additive black or a wrong-volume sample, which
		/// reads as a broken feature.</summary>
		private void WarnIfUnwired()
		{
			if (Model == null)
			{
				Debug.LogWarning(
					$"[{GetType().Name}] '{name}' has no Model -- wire the GalaxyBackdrop component. The shell samples ITS Params (one owner, no drift); unwired, there is no model and the shell stays empty.",
					this);
				return;
			}

			float maxScale = Mathf.Max(
				Mathf.Max(transform.lossyScale.x, transform.lossyScale.y),
				transform.lossyScale.z);
			if (Mathf.Abs(maxScale - 1f) > 0.01f)
			{
				Debug.LogWarning(
					$"[{GetType().Name}] '{name}' is scaled {maxScale:0.###}. The shell works in WORLD units: park it under the Galaxy root at scale 1, not inside the x3000 dressing.",
					this);
			}
		}
	}
}
