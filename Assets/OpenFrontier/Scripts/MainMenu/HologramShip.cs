using UnityEngine;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// Marks a curated hologram prefab for the loading screens.
	///
	/// The folder IS the manifest: the hologram picker enumerates prefabs
	/// carrying this marker from Resources/Prefabs/Holograms - a curated
	/// prefab dropped there joins the rotation, one removed leaves it. No
	/// inspector list anywhere, and nothing in code re-authors what the
	/// editor holds (no runtime material swapping, no shader swapping)
	/// - the prefab is the hologram, curated in prefab mode.
	///
	/// Curation contract (see docs/new-ui-guide.md):
	/// - Every renderer reads a material VARIANT of the parent
	///   "Hologram Ship" material, overriding only the ship's own
	///   normal map (the shader reads _BumpMap on UV0, stock Lit's
	///   slot and keyword, so bindings survive the shader swap).
	/// - Parts that read badly as holograms (glass canopies, engine
	///   glow quads) are disabled or assigned the "Hologram Ship
	///   Accent" variant, the family's highlight shimmer.
	/// - Rings, grids and base plates are NOT children of these
	///   prefabs - they are canvas art in the loading screens, so the
	///   fleet display shares one look and ships stay pure.
	/// </summary>
	[DisallowMultipleComponent]
	public class HologramShip : MonoBehaviour
	{
	}
}
