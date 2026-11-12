using UnityEngine;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// Rotates a hologram rig smoothly on unscaled time. SPIN ONLY.
	///
	/// The hologram shaders' clock does NOT live here - it is pushed by
	/// <see cref="HologramShip"/> (the marker on every hologram prefab),
	/// which owns the effects. This component owns nothing but the
	/// turntable: place it on whatever node should rotate (the hologram
	/// prefab's own root is fine - the curated prefabs carry it).
	///
	/// Unscaled on purpose: loading screens run while the world is
	/// frozen (the legacy LoadingScreenUI set timeScale = 0 under
	/// itself), so a scaled spin freezes with everything else. The spin
	/// must run on the same unscaled clock the effects read.
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

		private void Update()
		{
			transform.Rotate(0f, DegreesPerSecond * Time.unscaledDeltaTime, 0f, Space.Self);
		}
	}
}
