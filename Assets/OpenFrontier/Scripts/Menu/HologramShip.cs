using UnityEngine;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// Marks a curated hologram prefab for the loading screens AND
	/// drives the hologram shaders' clock.
	///
	/// MARKER - the folder IS the manifest: the hologram picker
	/// enumerates prefabs carrying this marker from
	/// Resources/Prefabs/Holograms - a curated prefab dropped there
	/// joins the rotation, one removed leaves it. No inspector list
	/// anywhere, and nothing in code re-authors what the editor holds
	/// (no runtime material swapping, no shader swapping) - the prefab
	/// is the hologram, curated in prefab mode.
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
	///
	/// THE CLOCK: Unity's shader _Time follows Time.timeScale, and
	/// loading screens run while the world is frozen (the legacy
	/// LoadingScreenUI set timeScale = 0 under itself), so anything
	/// animated on _Time freezes silently right where freeze is most
	/// likely. The hologram shaders therefore read the
	/// _OpenFrontierHoloTime global, and every instance of this
	/// component - one per hologram prefab, so one per on-screen ship -
	/// pushes it every frame from Time.unscaledTime. Multiple pushers
	/// are harmless (same-frame writes of the same unscaled clock);
	/// no instance anywhere means the global stays 0 and the drift
	/// stops visibly. The spin itself is separate: HoloSpinner owns
	/// the turntable, this owns the effects.
	/// </summary>
	[DisallowMultipleComponent]
	public class HologramShip : MonoBehaviour
	{
		/// <summary>
		/// Shader global the hologram shaders read instead of _Time (name
		/// must match OpenFrontier/Hologram Ship's TIME CONTRACT).
		///
		/// Resolved lazily, never as a static field initializer: a static
		/// initializer is type initialization, which Unity can stage during
		/// serialization where native calls are forbidden - the AGENTS.md
		/// "no Unity native calls in static constructors" trap would poison
		/// the entire type off one eager Shader.PropertyToID. First use is
		/// a runtime Update, so lazy costs nothing.
		/// </summary>
		public static int HoloTimeId
		{
			get
			{
				if (!holoTimeIdKnown)
				{
					holoTimeId = Shader.PropertyToID("_OpenFrontierHoloTime");
					holoTimeIdKnown = true;
				}
				return holoTimeId;
			}
		}

		private static int holoTimeId;
		private static bool holoTimeIdKnown;

		private void Update()
		{
			Shader.SetGlobalFloat(HoloTimeId, Time.unscaledTime);
		}
	}
}
