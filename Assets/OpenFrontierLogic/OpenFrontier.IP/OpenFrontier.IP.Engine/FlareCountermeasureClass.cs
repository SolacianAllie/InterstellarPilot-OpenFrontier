using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// A countermeasure that works by being bright rather than by jamming:
	/// it dazzles an infrared seeker instead of breaking a radar return.
	/// </summary>
	/// <remarks>
	/// This is what the IR seeker's weakness is made of. An ECM beats both
	/// radar seekers, because it breaks the signal a radar missile is listening
	/// to whether that signal is someone else's or its own; nothing in the base
	/// game could touch a heat-seeking one, which made IR strictly the safest
	/// thing to be hit by. A flare closes that: not by overpowering the seeker
	/// but by putting something else hot in the same part of the sky as the
	/// ship.
	///
	/// Which is why <see cref="AspectWeighting"/> is the interesting number
	/// here. A flare dropped anywhere near the target's bearing is very hard to
	/// tell from the target; one dropped off to the side is obviously a flare.
	/// That makes where you drop it matter, which is the whole difference
	/// between a countermeasure that is a decision and one that is a button.
	///
	/// Range is <see cref="CountermeasureClass.MaxDisruptionRange"/> rather
	/// than a field of its own - it is the same question this countermeasure
	/// asks as an ECM does, and a second range field would only be a second
	/// number to keep in step with the first.
	/// </remarks>
	public class FlareCountermeasureClass : CountermeasureClass
	{
		[Tooltip("Dazzle added per second to a seeker looking straight down the flare's bearing. Scaled up by how well aligned the flare is.")]
		public float DazzleStrength = 1f;

		[Tooltip("How much being on the target's bearing matters. 0 means a flare dazzles the same wherever it is dropped; 1 means an aligned flare counts double.")]
		public float AspectWeighting = 0.6f;

		[Tooltip("How long the flare keeps burning, in seconds.")]
		public float BurnTime = 6f;
	}
}
