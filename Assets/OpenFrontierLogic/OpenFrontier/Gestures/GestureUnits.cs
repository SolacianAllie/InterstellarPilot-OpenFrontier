using System;
using UnityEngine;

namespace OpenFrontier.Gestures
{
	/// <summary>
	/// Screen-space unit system for the gesture recognizers, measured in inches
	/// of real screen rather than pixels. Ported from the Fingers.dll
	/// DeviceInfo class with its semantics intact, because the recognizer
	/// thresholds (pan 0.2, scale 0.15, tap 0.3) are authored as inches.
	///
	/// DeviceInfo.PixelsPerInch / UnitMultiplier are the two values Fingers
	/// set up in FingersScript.Awake (Screen.dpi, or the DefaultDPI fallback
	/// with a red error). The Awake DPI fallback is exactly why
	/// FingersDpiFix existed: Screen.dpi reads 0 on Android foldables and on
	/// Linux, and 0 would leave the multiplier broken. The fix's job has
	/// moved here: GestureScript feeds OpenFrontier.ScreenDpi.Value, which
	/// already does the JNI-backed fallback with the sane default - so there
	/// is no red error and no second component to keep alive. See
	/// GestureScript.ApplyScreenScale.
	/// </summary>
	public static class GestureUnits
	{
		private static float pixelsPerInch;

		private static float unitMultiplier;

		private static float oneOverUnitMultiplier;

		public static float PixelsPerInch
		{
			get
			{
				return pixelsPerInch;
			}
			set
			{
				pixelsPerInch = value;
			}
		}

		/// <summary>
		/// Pixels per gesture unit. UnitMultiplier is clamped away from zero
		/// exactly as Fingers' setter did; this port is fed values that can
		/// not be zero (ScreenDpi floors it), so the clamp is a guard rather
		/// than the mechanism.
		/// </summary>
		public static float UnitMultiplier
		{
			get
			{
				return unitMultiplier;
			}
			set
			{
				value = Math.Max(1E-05f, value);
				unitMultiplier = value;
				oneOverUnitMultiplier = 1f / unitMultiplier;
			}
		}

		public static float CentimetersToInches(float centimeters)
		{
			return centimeters * 0.393701f;
		}

		public static float InchesToCentimeters(float inches)
		{
			return inches * 2.539998f;
		}

		public static float PixelsToUnits(float pixels)
		{
			return pixels * oneOverUnitMultiplier;
		}

		public static float UnitsToPixels(float units)
		{
			return units * unitMultiplier;
		}
	}
}
