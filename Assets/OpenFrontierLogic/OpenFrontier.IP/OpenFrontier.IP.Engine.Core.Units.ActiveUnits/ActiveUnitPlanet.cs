using UnityEngine;

namespace OpenFrontier.IP.Engine.Core.Units.ActiveUnits
{
	public class ActiveUnitPlanet : MonoBehaviour
	{
		public MeshRenderer AtmoshphereRenderer;

		private Unit unit;

		private UnitPlanet planetUnit;

		public float PlanetRotation = 0.25f;

		public Transform TransformTarget;

		public Vector3 BaseRotation = Vector3.zero;

		// ---- planetary ring ----

		// The planet's radius is NOT authored. It is worked out from the
		// planet's own mesh and its transform scale -- the two things that
		// already define how big the planet is -- and then divided by the
		// ring's scale to land in the ring's OBJECT units, which is the space
		// the shader casts its shadow in. Every planet prefab already carries
		// the answer as a scale (800, 900, 910; moons at 40), so a number typed
		// here would only be a second copy of it waiting to disagree.
		//
		// Nudge the result rather than replace it. 1 is exact; below 1 makes the
		// planet read slightly smaller than it measures, which is usually what
		// you want, because an exact match puts the shadow's outer edge exactly
		// on the ring's inner rim and that looks like the ring is clipped
		// rather than shadowed.
		public float RingSizeMultiplier = 1f;

		// Optional. Left null the planet mesh is found automatically as the
		// first MeshFilter that is not the ring's. Worth setting explicitly on
		// a prefab where the atmosphere shell or another child would be found
		// first, since the shell's bounds are not the planet's.
		public MeshFilter PlanetMeshFilter;

		// Assigned per ring. Left null it is found at runtime by looking for a
		// child MeshRenderer using the ring shader, so adding a ring to a
		// planet prefab needs no wiring at all -- and rings are currently
		// scene-placed rather than in any prefab, which is the other reason
		// this is looked up rather than required.
		public MeshRenderer RingRenderer;

		// Each ring takes its planet's own Seed, so two planets get visibly
		// different dust from one shared texture. Deterministic: the same
		// planet looks the same every time, and it survives a save and reload,
		// because the seed is persisted with the unit.
		public bool SeedRingFromUnit = true;

		private static readonly int PlanetRadiusId = Shader.PropertyToID("_PlanetRadius");

		private static readonly int RingSeedId = Shader.PropertyToID("_RingSeed");

		private const string RingShaderName = "OpenFrontier/PlanetaryRing";

		// Cached because Renderer.GetPropertyBlock has no parameterless
		// overload -- it fills a block you supply -- so a fresh one has to be
		// passed each time. Reused so the two calls at spawn do not allocate.
		//
		// LAZY, and not readonly, and that is the whole point of it.
		// MaterialPropertyBlock's constructor calls the native CreateImpl,
		// and a MonoBehaviour field initializer runs during serialization --
		// which Unity forbids -- so building it in the initializer throws
		// "CreateImpl is not allowed to be called from a MonoBehaviour
		// constructor (or instance field initializer), call it in Awake or
		// Start instead". The constructor throws, so the COMPONENT never
		// finishes constructing and every field on it, not just this one,
		// is left as Unity's null default.
		//
		// It is not hypothetical and it is not specific to the ring work:
		// this threw on import of ActiveUnitPlanetJupitersCousin, because
		// the asset indexer constructs the component while indexing the
		// prefab, before any Awake could run.
		//
		// Created on first use instead. ApplyRingProperties is the only
		// reader and it returns early when there is no ring, so a planet
		// with no ring never allocates one at all.
		private MaterialPropertyBlock ringBlock;

		private void Awake()
		{
			unit = GetComponentInParent<Unit>();
			if (unit != null)
			{
				planetUnit = unit.GetComponent<UnitPlanet>();
			}
			Rotate();
			ApplyRingProperties();
		}

		private void Start()
		{
			// Unit.Init() assigns Seed -- not Awake -- so the seed does not
			// reliably exist yet by the time this component's Awake has run.
			// Applying again from Start covers the normal spawn order. It is
			// idempotent and costs one property-block write, which is not worth
			// optimising against.
			ApplyRingProperties();
		}

		private void FixedUpdate()
		{
			Rotate();
		}

		public void Rotate()
		{
			if (planetUnit != null)
			{
				float num = (float)(EngineASX.Instance.ScenarioElapsedTime * (double)PlanetRotation * (double)planetUnit.RotationRateMultiplier % 360.0);
				TransformTarget.localRotation = Quaternion.Euler(planetUnit.Rotation) * Quaternion.Euler(BaseRotation.x, BaseRotation.y + num, BaseRotation.z);
			}
		}

		/// <summary>
		/// Pushes the ring's per-planet numbers into its material.
		/// Also on the context menu, so a ring added to a prefab at edit time
		/// can be set up without entering play mode.
		/// </summary>
		[ContextMenu("Apply Ring Properties")]
		public void ApplyRingProperties()
		{
			MeshRenderer renderer = RingRenderer != null ? RingRenderer : FindRingRenderer();
			if (renderer == null)
			{
				return;
			}

			float radius = ResolvePlanetRadius(renderer);

			// Built here rather than in a field initializer -- see the note on
			// ringBlock. Awake is the earliest this can run without throwing.
			if (ringBlock == null)
			{
				ringBlock = new MaterialPropertyBlock();
			}

			// Read into the existing block rather than setting a fresh one.
			// A fresh block would wipe anything else driving this renderer, and
			// ActiveUnitFadeOutMeshController sets a fade colour on the same
			// kind of object.
			renderer.GetPropertyBlock(ringBlock);
			ringBlock.SetFloat(PlanetRadiusId, radius);

			if (SeedRingFromUnit)
			{
				ringBlock.SetFloat(RingSeedId, ResolveSeed());
			}

			renderer.SetPropertyBlock(ringBlock);

			if (Application.isPlaying && radius <= 0f)
			{
				Debug.LogWarning("Ring planet radius came out 0, so the planet shadow is off. "
					+ "Check PlanetMeshFilter, or that the ring is scaled above zero.", this);
			}
		}

		private float ResolvePlanetRadius(MeshRenderer ringRenderer)
		{
			MeshFilter filter = PlanetMeshFilter != null ? PlanetMeshFilter
				: FindPlanetMeshFilter(ringRenderer);
			if (filter == null || filter.sharedMesh == null)
			{
				return 0f;
			}

			// Radius of the planet mesh about its own origin, in the mesh's own
			// units. maxComponent, not magnitude: SphereMediumPoly's extents are
			// (10,10,10) and the length of that vector is 17.3, which is not the
			// radius. maxComponent is the radius for a sphere centred on its
			// origin, and this mesh has m_Center {0,0,0} with m_Extent
			// {10,10,10}, so it is exactly that.
			Bounds bounds = filter.sharedMesh.bounds;
			float local = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));

			// Largest axis on both transforms. A non-uniform scale would make
			// the planet an ellipsoid; taking the largest matches the extent the
			// renderer actually draws, which is the silhouette the shadow has to
			// match.
			float planetScale = LargestAxis(filter.transform.lossyScale);
			float ringScale = LargestAxis(ringRenderer.transform.lossyScale);

			// The ring is drawn in its own object space, so a radius in world
			// units means nothing to the shader. This is the whole reason the
			// shadow is cast in object space at all.
			//
			// A non-uniformly scaled ring makes the sphere the shader casts
			// become an ellipsoid in world space, which is the honest result of
			// it: an object-space sphere under a non-similarity transform is an
			// ellipsoid. Rings are expected to be scaled uniformly, so this is
			// noted rather than compensated for.
			if (ringScale < 1e-5f)
			{
				return 0f;
			}

			return local * planetScale / ringScale * RingSizeMultiplier;
		}

		private static float LargestAxis(Vector3 v)
		{
			return Mathf.Max(v.x, Mathf.Max(v.y, v.z));
		}

		private float ResolveSeed()
		{
			// -1 is Unit's "not yet initialised" sentinel, and Init() replaces
			// it with a random value. Passing -1 through untouched would give
			// every not-yet-seeded planet the identical ring, which looks like
			// the feature silently doing nothing. Deriving a stable fallback
			// from the transform keeps the rings distinct either way, and the
			// real seed replaces it on the next pass once Init has run.
			if (unit == null)
			{
				return transform.position.GetHashCode();
			}
			if (unit.Seed < 0)
			{
				return transform.position.GetHashCode();
			}
			return unit.Seed;
		}

		private MeshRenderer FindRingRenderer()
		{
			MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
			for (int i = 0; i < renderers.Length; i++)
			{
				Material material = renderers[i].sharedMaterial;
				// Compared by shader NAME, not HasProperty. HasProperty takes a
				// shader property and would never match a shader name; the
				// project identifies shaders this way elsewhere
				// (MigrationDiagnostics reports mat.shader.name).
				if (material != null && material.shader != null
					&& material.shader.name == RingShaderName)
				{
					return renderers[i];
				}
			}
			return null;
		}

		private MeshFilter FindPlanetMeshFilter(MeshRenderer ringRenderer)
		{
			// Skip the ring's own filter: it is a child like everything else,
			// and its bounds describe the belt, not the planet.
			Transform ringTransform = ringRenderer != null ? ringRenderer.transform : null;
			MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(true);
			for (int i = 0; i < filters.Length; i++)
			{
				if (ringTransform != null && filters[i].transform == ringTransform)
				{
					continue;
				}
				if (filters[i].sharedMesh != null)
				{
					return filters[i];
				}
			}
			return null;
		}
	}
}
