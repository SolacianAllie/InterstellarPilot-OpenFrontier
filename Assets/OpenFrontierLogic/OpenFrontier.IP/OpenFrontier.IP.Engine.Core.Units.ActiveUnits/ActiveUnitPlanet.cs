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

		// The planet's radius in the RING'S OBJECT UNITS, not world units.
		//
		// The ring shader casts the shadow against a sphere sitting at the
		// ring's object origin and never sees the transform scale, so this one
		// number describes the planet correctly however the whole system is
		// scaled. Measured from the planet's own mesh it is a fixed number per
		// prefab; left at 0 the planet shadow is simply off, which is the
		// right default for every planet that has no ring.
		public float RingPlanetRadius;

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
		private readonly MaterialPropertyBlock ringBlock = new MaterialPropertyBlock();

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

			// Read into the existing block rather than setting a fresh one.
			// A fresh block would wipe anything else driving this renderer, and
			// ActiveUnitFadeOutMeshController sets a fade colour on the same
			// kind of object.
			renderer.GetPropertyBlock(ringBlock);
			ringBlock.SetFloat(PlanetRadiusId, RingPlanetRadius);

			if (SeedRingFromUnit)
			{
				ringBlock.SetFloat(RingSeedId, ResolveSeed());
			}

			renderer.SetPropertyBlock(ringBlock);
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
	}
}
