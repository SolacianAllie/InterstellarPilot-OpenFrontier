using UnityEngine;

namespace OpenFrontier
{
	/// <summary>
	/// The text scale for the development overlays.
	/// </summary>
	/// <remarks>
	/// Shared so DevHud and the music overlay cannot drift apart - they are
	/// stacked into one panel, and two overlays that disagree about how big
	/// text is stop reading as one panel.
	///
	/// It was previously Mathf.Max(2f, Screen.dpi / 160f) in both, and the
	/// floor of 2 was the problem. That floor is hit on any desktop, because
	/// Screen.dpi there is usually around 96 and 96/160 is 0.6, so every
	/// developer machine got 2x text - twice the width of the box, a long way
	/// into the screen, with the music overlay's seven lines filling a
	/// readable chunk of the viewport. The floor existed for high-DPI phone
	/// panels, where 2x is genuinely needed, and it was paid for on every
	/// platform that did not need it.
	///
	/// Scaling against ScreenDpi.Value rather than Screen.dpi is the other
	/// half. Screen.dpi returns 0 on Android foldables and in the Linux
	/// editor, and dividing 0 by anything gives 0, so a raw read collapses the
	/// whole overlay to nothing on exactly the devices where readability
	/// matters most. ScreenDpi.Value falls back through the JNI
	/// DisplayMetrics path to a constant.
	///
	/// 200 dpi is the reference, matching ScreenDpi.FallbackDpi: a device
	/// reporting the fallback draws at 1x, which is the intended desktop
	/// size. The clamps keep a 96 dpi desktop at 0.85 rather than 0.48 (too
	/// small to read) and stop a 640 dpi panel at 2.5 rather than 3.2 (large
	/// enough to cover the screen it is measuring).
	/// </remarks>
	public static class DevOverlayScale
	{
		/// <summary>DPI at which overlays draw at 1x.</summary>
		public const float ReferenceDpi = 200f;

		/// <summary>Never smaller than this, however low the reported DPI.</summary>
		public const float Min = 0.85f;

		/// <summary>Never larger than this, however high the reported DPI.</summary>
		public const float Max = 2.5f;

		/// <summary>The scale to draw overlay text and boxes at.</summary>
		public static float Value => Mathf.Clamp(ScreenDpi.Value / ReferenceDpi, Min, Max);
	}
}
