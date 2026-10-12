using DigitalRubyShared;

namespace OpenFrontier.IP.UI
{
	public static class GestureRecognizerExtensions
	{
		public static bool IsRightMouseButton(this GestureRecognizer gestureRecognizer)
		{
			if (gestureRecognizer is TapGestureRecognizer { TapTouches: not null } tapGestureRecognizer && tapGestureRecognizer.TapTouches.Count > 0 && tapGestureRecognizer.TapTouches[0].PlatformSpecificTouch is int num)
			{
				return num == 1;
			}
			return false;
		}
	}
}
