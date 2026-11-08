using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// How a missile threat should look on the HUD.
	/// </summary>
	/// <remarks>
	/// Lives here rather than on the warning controller because the compass
	/// needs it per missile and the warning controller needs it for the shared
	/// lock indicator, and the compass is engine-side. One table, so the box
	/// around a missile icon and the sound it is paired with cannot drift
	/// apart - they are meant to be the same piece of information.
	///
	/// This replaced a single shared SpriteSineColor that the whole HUD read
	/// from. That could only ever show one colour at a time, taken from the
	/// worst missile on the scope, so every missile icon was painted with the
	/// same threat regardless of what was actually chasing the player.
	/// </remarks>
	public static class MissileThreatFlash
	{
		/// <summary>Quietest tier first: a redder flash means better guided.</summary>
		public static readonly Color Optical = new Color(1f, 1f, 1f, 1f);

		public static readonly Color Infrared = new Color(1f, 0.85f, 0.4f, 1f);

		public static readonly Color SemiActiveRadar = new Color(1f, 0.55f, 0.2f, 1f);

		public static readonly Color ActiveRadar = new Color(1f, 0.2f, 0.15f, 1f);

		/// <summary>The bright end of the pulse, so it reads as a flash.</summary>
		public static readonly Color Peak = Color.white;

		/// <summary>Pulses per second while a missile is still far off.</summary>
		public const float SpeedDistant = 1.5f;

		/// <summary>Pulses per second once inside the closing-in distance.</summary>
		public const float SpeedClosingIn = 6f;

		public static Color ColorFor(MissileGuidanceType type)
		{
			switch (type)
			{
				case MissileGuidanceType.IR: return Infrared;
				case MissileGuidanceType.SARH: return SemiActiveRadar;
				case MissileGuidanceType.ARH: return ActiveRadar;
				default: return Optical;
			}
		}

		/// <summary>
		/// Pulse rate for a missile at a given range. Ramps up over the last
		/// stretch so the flash quickens as it closes rather than snapping
		/// between two speeds.
		/// </summary>
		/// <param name="distance">Range to the missile, in metres.</param>
		/// <param name="closingInDistance">Where the warning considers it close.</param>
		public static float SpeedFor(float distance, float closingInDistance)
		{
			if (closingInDistance <= 0f || distance >= closingInDistance)
			{
				return SpeedDistant;
			}

			if (distance <= 0f)
			{
				return SpeedClosingIn;
			}

			// 0 at the edge of closing range, 1 right on top of the player.
			float t = 1f - distance / closingInDistance;
			return Mathf.Lerp(SpeedDistant, SpeedClosingIn, t * t);
		}

		/// <summary>
		/// The colour to paint an indicator with right now.
		/// </summary>
		/// <remarks>
		/// Phase comes from the clock rather than per-item state, so items with
		/// different speeds drift in and out of step instead of pulsing as one
		/// solid block - which is what makes a salvo look like several threats
		/// rather than one.
		/// </remarks>
		public static Color Pulse(MissileGuidanceType type, float speed, float elapsedTime)
		{
			return Color.Lerp(ColorFor(type), Peak, Maths.Sine01(elapsedTime * speed));
		}
	}
}
