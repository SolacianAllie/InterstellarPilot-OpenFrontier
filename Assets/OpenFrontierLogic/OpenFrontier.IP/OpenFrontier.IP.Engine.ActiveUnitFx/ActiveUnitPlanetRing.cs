using UnityEngine;

namespace OpenFrontier.IP.Engine.ActiveUnitFx
{
	/// <summary>
	/// Randomises a planetary ring's dust tiling and pan, so that several
	/// identical ring meshes in the same view do not read as one ring stamped
	/// out four times.
	///
	/// Goes on the ring GameObject. Deliberately separate from
	/// ActiveUnitPlanet, which drives the planet-side properties on the same
	/// renderer -- both read-modify-write the property block, so whichever runs
	/// second keeps the other's values.
	/// </summary>
	public class ActiveUnitPlanetRing : MonoBehaviour
	{
		// How many times the dust texture repeats across the band's width.
		//
		// The range is 20x wide on purpose, because it is the single biggest
		// lever on how busy the ring reads: near 0.1 the texture is stretched
		// almost flat and the ring is a smooth band, near 2 it repeats twice
		// and shows its structure. Anything narrower gives rings that differ
		// only slightly, which the eye picks out as the same ring.
		public float MinRadialTiling = 0.1f;

		public float MaxRadialTiling = 2f;

		// How far around the band the texture is slid, in tiles. This is the
		// angular half of the variation: the ring meshes are all the same strip
		// repeated, so without this every ring shows its widest feature at the
		// same angle.
		//
		// A side effect worth having: these textures do NOT tile in U -- the
		// seam step measures 8.46 against an interior step of 1.31 -- so the
		// ring has one visible seam somewhere. Randomising the pan puts that
		// seam at a different angle on every ring, so the eye cannot learn
		// where it is and read it as a systematic artefact of the asset.
		public float MinPan = -1f;

		public float MaxPan = 1f;

		// Assigned per ring. Left null it is found among the children by
		// shader name, so a ring spawned in by the planet seeder needs no wiring.
		public MeshRenderer RingRenderer;

		// Randomised from the owning unit's Seed when there is one, so the same
		// planet always looks the same and a reload reproduces it exactly.
		//
		// Deliberately NOT UnityEngine.Random. The global stream is reseeded from
		// the world's CustomSeed and then consumed by the world seeder, so
		// anything that draws from it has to happen in exactly the right order
		// to reproduce. A ring spawning from a save, or from anywhere the
		// seeder did not reach, would perturb the stream for every later
		// consumer. A local generator seeded from the unit touches nothing.
		public bool RandomiseOnAwake = true;

		private const string RingShaderName = "OpenFrontier/PlanetaryRing";

		private static readonly int DustStId = Shader.PropertyToID("_DustAlbedo_ST");

		private readonly MaterialPropertyBlock ringBlock = new MaterialPropertyBlock();

		private void Awake()
		{
			if (RandomiseOnAwake)
			{
				Randomise();
			}
		}

		/// <summary>
		/// Also on the context menu, so a ring placed by hand in the editor can
		/// be re-rolled without entering play mode.
		/// </summary>
		[ContextMenu("Randomise Ring Tiling")]
		public void Randomise()
		{
			MeshRenderer renderer = RingRenderer != null ? RingRenderer : FindRingRenderer();
			if (renderer == null)
			{
				return;
			}

			Material material = renderer.sharedMaterial;
			if (material == null)
			{
				return;
			}

			// Read the material's own tiling rather than assuming 1. The
			// material is the artist's, and this only moves two of the four
			// numbers in the vector.
			Vector2 scale = material.GetTextureScale("_DustAlbedo");
			Vector2 offset = material.GetTextureOffset("_DustAlbedo");

			System.Random random = CreateRandom();

			// Y is the radial tiling -- across the band's width, which is the
			// axis with room for it.
			scale.y = Mathf.Lerp(MinRadialTiling, MaxRadialTiling, (float)random.NextDouble());
			// X of the offset is the pan, the angular slide.
			offset.x = Mathf.Lerp(MinPan, MaxPan, (float)random.NextDouble());

			// Read-modify-write. Setting a fresh block would wipe whatever
			// ActiveUnitPlanet wrote to this renderer -- the planet shadow radius
			// and the per-planet seed.
			renderer.GetPropertyBlock(ringBlock);
			ringBlock.SetVector(DustStId, new Vector4(scale.x, scale.y, offset.x, offset.y));
			renderer.SetPropertyBlock(ringBlock);
		}

		private System.Random CreateRandom()
		{
			Unit unit = GetComponentInParent<Unit>();
			if (unit != null && unit.Seed >= 0)
			{
				return new System.Random(unit.Seed);
			}
			// No unit, so no persisted seed to be reproducible against.
			// UnityEngine.Random is the only source left and is still correct
			// during seeding, where it is seeded from the world.
			return new System.Random(UnityEngine.Random.Range(0, int.MaxValue));
		}

		private MeshRenderer FindRingRenderer()
		{
			MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
			for (int i = 0; i < renderers.Length; i++)
			{
				Material material = renderers[i].sharedMaterial;
				// Compared by shader NAME, not HasProperty -- HasProperty takes
				// a shader property and would never match a shader name.
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
