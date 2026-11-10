using UnityEngine;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// Rotates a hologram rig smoothly on unscaled time and drives the
	/// hologram shaders' clock.
	///
	/// Place on the loading rig's ship-holder node, NOT on the hologram
	/// prefab: the spin belongs to the display rig (every loading screen
	/// that shows a ship wants one), while the ships stay inert curated
	/// geometry that prefab randomisation can swap freely.
	///
	/// The clock: Unity's shader _Time follows Time.timeScale, and loading
	/// screens run while the world is frozen (the legacy LoadingScreenUI
	/// set timeScale = 0 under itself), so anything animated on _Time
	/// freezes silently right where freeze is most likely. The hologram
	/// shaders therefore read the _OpenFrontierHoloTime global, and this
	/// component is its single pusher, unscaled - the same contract the
	/// legacy LoadingWidgetUI honoured by animating on
	/// realtimeSinceStartup while the world sat paused.
	///
	/// Deliberately yaw-only with one speed: no bob, no axis menu, no
	/// easing. The first real need earns those (see docs/new-ui-guide.md,
	/// "earned, not pre-built").
	/// </summary>
	[DisallowMultipleComponent]
	public class HoloSpinner : MonoBehaviour
	{
		[Tooltip("Unscaled yaw speed of the hologram ship, degrees per second.")]
		[Min(0f)]
		public float DegreesPerSecond = 24f;

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
			transform.Rotate(0f, DegreesPerSecond * Time.unscaledDeltaTime, 0f, Space.Self);
			Shader.SetGlobalFloat(HoloTimeId, Time.unscaledTime);
		}
	}
}
