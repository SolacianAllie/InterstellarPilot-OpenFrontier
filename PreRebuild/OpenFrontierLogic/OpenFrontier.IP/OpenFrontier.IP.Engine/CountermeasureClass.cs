using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class CountermeasureClass : MonoBehaviour
	{
		public float Effectiveness = 0.5f;

		/// <summary>
		/// How much of the cloud's burn time it spends jamming, as a fraction.
		/// The rest of the burn it is only a heat decoy.
		/// </summary>
		/// <remarks>
		/// The burn time itself is the projectile class's MaxDuration - four
		/// seconds for an ECM I, five for an ECM II - because that is what
		/// actually takes the cloud off the board, and having the jam window
		/// derived from it means the two can never disagree.
		///
		/// Halving it is what separates the two jobs. An ECM answers a radar
		/// missile by jamming it and a heat missile by being a bigger, hotter
		/// thing than the ship, and those want different lifetimes: the jam
		/// wants to cover the window in which missiles are arriving, while the
		/// decoy wants to still be there when the second missile comes.
		///
		/// Set this to 1 for something that only ever jams, or 0 for something
		/// that is purely a decoy.
		/// </remarks>
		public float JamFraction = 0.5f;

		public float MaxDisruptionRange = 100f;

		public float MinTimeBeforeDisruption = 0.4f;

		public float Drag = 0.3f;

		/// <summary>
		/// How attractive this countermeasure's cloud is to a heat seeker.
		/// Zero means a seeker ignores it completely.
		/// </summary>
		/// <remarks>
		/// An ECM puts out a large gas cloud, and a large hot cloud is exactly
		/// what an infrared seeker is looking for. So the same puff that breaks a
		/// radar lock also pulls a heat seeker off its target, which is what
		/// makes one countermeasure answer two kinds of missile instead of one.
		///
		/// The seeker scores a cloud directly rather than as a ship, so it
		/// radiates equally in every direction - a cloud deployed between you
		/// and an incoming missile has to be the more interesting thing to it,
		/// not the less.
		///
		/// Zero is the default, so a countermeasure nobody has thought about is
		/// invisible to heat rather than accidentally a flare.
		/// </remarks>
		public float IrSignature;

		/// <summary>
		/// Whether this countermeasure disrupts every hostile missile in range
		/// regardless of which way the missile is pointing.
		/// </summary>
		/// <remarks>
		/// The roll below is scaled by the angle between the puff and the
		/// missile, so an ECM astern of a missile has a negative dot and can
		/// never succeed at all. That is right for the cheaper countermeasure -
		/// it should have to be pointed at the threat - and wrong for the
		/// expensive one, which is meant to be the guaranteed answer.
		///
		/// Ticking this drops the facing term entirely and leaves only the
		/// five-missile budget, which is a hard ceiling rather than a
		/// probability.
		/// </remarks>
		public bool AffectsAllInRange;
	}
}
