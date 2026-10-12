using UnityEngine;

namespace OpenFrontier
{
	/// <summary>
	/// Reliable screen DPI. Screen.dpi can return 0 (seen on Android
	/// foldables and on Linux), which silently breaks anything that scales
	/// touch thresholds by DPI. Falls back to Android's DisplayMetrics via
	/// JNI, then to a constant default. Cached; call Invalidate() when the
	/// display may have changed (fold swap).
	/// </summary>
	public static class ScreenDpi
	{
		public const float FallbackDpi = 200f;

		private static float cached = -1f;

		public static float Value
		{
			get
			{
				if (cached <= 0f)
				{
					cached = Screen.dpi;
					if (cached <= 0f)
					{
						cached = AndroidDisplayMetrics.GetDensityDpi();
					}
					if (cached <= 0f)
					{
						cached = FallbackDpi;
					}
				}
				return cached;
			}
		}

		public static void Invalidate()
		{
			cached = -1f;
		}
	}
}
