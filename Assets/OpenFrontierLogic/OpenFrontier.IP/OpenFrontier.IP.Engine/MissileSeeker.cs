using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// Works out where one missile should fly, which is the only thing that
	/// actually differs between the four seeker types.
	/// </summary>
	/// <remarks>
	/// This exists because <see cref="ProjectileClass.GuidanceType"/> was
	/// already staged on the missile classes and already drove the whole
	/// warning system - sound and flash colour per seeker - but nothing read it
	/// when flying the missile, so all four behaved identically. The types
	/// were honest labels on nothing.
	///
	/// The idea worth taking from a real seeker is what happens when the lock
	/// BREAKS. A missile here does not snap back onto the target the instant
	/// the target reappears. It goes ballistic on the last position and
	/// velocity it knew, dead-reckoning forward, and its guess wanders further
	/// off the longer it stays blind. It may or may not re-acquire before
	/// <see cref="ProjectileClass.SeekerLockPersistence"/> runs out. That is
	/// what makes a cone worth having at all: a target that turns hard enough
	/// to break the cone has bought itself something, and one that also breaks
	/// the persistence has bought itself the fight.
	///
	/// Evaluation is interval-driven rather than per-frame, on the same
	/// reasoning as the rest of this project: nothing about whether a seeker
	/// can see a target changes faster than a few times a second, and a missile
	/// is a per-frame object on a phone. Aiming is still every frame, but it
	/// aims at a point this has already decided on.
	/// </remarks>
	public class MissileSeeker
	{
		/// <summary>
		/// Used when a prefab predates <see cref="ProjectileClass.
		/// SeekerCheckInterval"/> and reads it as zero. Ten times a second,
		/// which is what the reference this was modelled on uses, and fast
		/// enough that nothing a player does outruns it.
		/// </summary>
		private const float DefaultCheckInterval = 0.1f;

		/// <summary>
		/// Half-angle of the seeker cone, as a cosine so the test is a dot
		/// product. -1 means unlimited, which is what a cone of 0 means.
		/// </summary>
		private readonly float lockDot;

		/// <summary>
		/// Half-angle needed to get the lock back, normally wider than
		/// <see cref="lockDot"/> so a missile sitting on the edge of its cone
		/// does not flicker. Having two thresholds is the entire point.
		/// </summary>
		private readonly float reacquireDot;

		private readonly Missile missile;

		/// <summary>Where the target was, the last time the seeker had it.</summary>
		private Vector3 lastKnownPosition;

		/// <summary>How fast the target was moving, the last time it was seen.</summary>
		private Vector3 lastKnownVelocity;

		/// <summary>
		/// How far the guess has wandered off the truth. Accrues only while
		/// blind and is cleared on every acquisition, so it measures how long
		/// the seeker has been guessing rather than how old the missile is.
		/// </summary>
		private Vector3 errorOffset;

		/// <summary>Seconds spent blind, against <see cref="ProjectileClass.SeekerLockPersistence"/>.</summary>
		private float blindTime;

		/// <summary>Time the previous evaluation ran, so blind time is real seconds.</summary>
		private float lastEvaluationTime;

		/// <summary>Time the next evaluation is due, in <see cref="Time.time"/>.</summary>
		private float nextEvaluationTime;

		/// <summary>
		/// True once the seeker has ever actually held the target, so a missile
		/// that launches already outside its cone is not written off before it
		/// has had a chance.
		/// </summary>
		private bool hasEverLocked;

		/// <summary>Whether the seeker can see the target right now.</summary>
		private bool hasLock;

		/// <summary>
		/// Where to fly. The real target while locked; while blind, a point
		/// dead-reckoned forward from the last thing that was seen, plus
		/// however far the guess has wandered since.
		/// </summary>
		public Vector3 AimPoint { get; private set; }

		/// <summary>
		/// The velocity to plan an intercept against. The target's own while
		/// locked, otherwise the remembered one - a blind missile is guessing
		/// where the target is going as well as where it is.
		/// </summary>
		public Vector3 AimVelocity { get; private set; }

		/// <summary>
		/// Whether the seeker can currently see the target. The warning system
		/// reads this through <see cref="Missile.IsLockedOnTo"/>, so a missile
		/// that has been blinded or shaken off stops sounding.
		/// </summary>
		public bool HasLock => hasLock;

		/// <summary>
		/// Whether this seeker has ever held the target at all. The warning
		/// needs it to tell the difference between a missile that was shaken off
		/// - no longer worth an alarm - and one that never had a lock and is
		/// still coming, which very much is.
		/// </summary>
		public bool HasEverLocked => hasEverLocked;

		public MissileSeeker(Missile missile)
		{
			this.missile = missile;
			ProjectileClass cls = missile.ProjectileClass;
			lockDot = CosineOfHalfAngle(cls.SeekerConeAngle);
			reacquireDot = CosineOfHalfAngle(cls.SeekerReacquireAngle > 0f ? cls.SeekerReacquireAngle : cls.SeekerConeAngle);
		}

		private static float CosineOfHalfAngle(float degrees)
		{
			if (degrees <= 0f)
			{
				// Unset, which is what every prefab written before the degrees
				// fields existed has. Cosine of 180 is -1 and would make the
				// cone see in every direction, which is the intent.
				return -1f;
			}
			return Mathf.Cos(Mathf.Clamp(degrees, 0f, 180f) * Mathf.Deg2Rad);
		}

		/// <summary>
		/// Brings the aim point up to date. Cheap, and runs every frame; the
		/// part that asks whether the seeker can see anything is on
		/// <see cref="ProjectileClass.SeekerCheckInterval"/>.
		/// </summary>
		public void Update(float now, float deltaTime)
		{
			if (now >= nextEvaluationTime)
			{
				float interval = missile.ProjectileClass.SeekerCheckInterval > 0f
					? missile.ProjectileClass.SeekerCheckInterval
					: DefaultCheckInterval;
				// The first evaluation has no previous one to measure against,
				// so it costs no blind time - a missile must not be written off
				// in the frame it launches.
				float elapsed = lastEvaluationTime > 0f ? now - lastEvaluationTime : 0f;
				lastEvaluationTime = now;
				nextEvaluationTime = now + interval;
				Evaluate(elapsed);
			}
			if (!hasLock)
			{
				float drift = missile.ProjectileClass.SeekerBlindDrift * deltaTime;
				lastKnownPosition += lastKnownVelocity * deltaTime;
				// The guess wanders in the combat plane. Nothing sensible moves
				// up or down here - units are Y-frozen - so the third component
				// is left at zero rather than pretending to be uncertain in 3D.
				errorOffset += new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * drift;
				AimPoint = lastKnownPosition + errorOffset;
			}
		}

		/// <summary>
		/// Asks the one question that differs per seeker: can I see it?
		/// </summary>
		/// <param name="elapsed">Real seconds since the previous evaluation.</param>
		private void Evaluate(float elapsed)
		{
			Unit target = missile.Target;
			if (target == null)
			{
				hasLock = false;
				return;
			}
			Vector3 toTarget = target.transform.position - missile.transform.position;
			float distance = toTarget.magnitude;
			if (distance <= 0.0001f)
			{
				// Close enough that the bearing is meaningless. Do not drop the
				// lock over a division by nearly zero.
				Acquire(target);
				return;
			}
			// Remember where it is and how fast it is going whether or not the
			// cone test passes. The missile was fired at this ship, so it
			// roughly knows where to start from even when it cannot see it.
			float dot = Vector3.Dot(toTarget / distance, missile.transform.forward);
			float threshold = hasLock ? lockDot : reacquireDot;
			if (dot >= threshold && SeesTarget(target, distance))
			{
				Acquire(target);
			}
			else
			{
				hasLock = false;
				blindTime += elapsed;
				// Persistence only starts mattering once the seeker has held
				// something. Otherwise a missile launched outside its own cone
				// would abandon the shot in one evaluation.
				if (hasEverLocked && blindTime >= missile.ProjectileClass.SeekerLockPersistence)
				{
					missile.LoseTargetPermanently();
				}
			}
		}

		private void Acquire(Unit target)
		{
			hasLock = true;
			hasEverLocked = true;
			blindTime = 0f;
			errorOffset = Vector3.zero;
			lastKnownPosition = target.transform.position;
			Rigidbody targetBody = target.RBody;
			lastKnownVelocity = targetBody != null ? targetBody.linearVelocity : Vector3.zero;
			AimPoint = lastKnownPosition;
			AimVelocity = lastKnownVelocity;
		}

		/// <summary>
		/// The part that is genuinely different per seeker type. The cone and
		/// the drift are shared by all of them; this is what the cone is being
		/// pointed at.
		/// </summary>
		private bool SeesTarget(Unit target, float distance)
		{
			switch (missile.ProjectileClass.GuidanceType)
			{
				case MissileGuidanceType.Optical:
					// A laser seeker rides on a designator the launcher has to
					// keep on the target, so it is the most fragile thing in the
					// game - and the only type that must never quietly fall back
					// to a blanket yes. Until the Utility Mount exists to be
					// that designator, the launcher's own lock is the best
					// answer available and the cone does the rest.
					return true;

				case MissileGuidanceType.IR:
					// Follows the target's own heat, so a cloaked ship has no
					// signature to follow. An ECM puff does nothing for it,
					// which is exactly why carrying one is a choice.
					return !target.IsFullyCloaked;

				case MissileGuidanceType.SARH:
					// Semi-active: it is riding a signal the target is putting
					// out, so the target has to be emitting - and the ECM
					// exists precisely to break that signal up. This is the one
					// seeker the ECM work actually pays off against.
					return !target.IsFullyCloaked && !missile.IsDisrupted;

				case MissileGuidanceType.ARH:
				default:
					// Its own radar. Nothing the target or its crew can do
					// about it short of outrunning the missile or putting a
					// mountain in the way.
					return true;
			}
		}

		/// <summary>
		/// Shakes the lock without ending it - the missile keeps the target and
		/// may re-acquire. For something that disrupts a seeker rather than
		/// blinding it outright.
		/// </summary>
		public void LoseLock()
		{
			hasLock = false;
		}

		/// <summary>
		/// Starts a seeker over, for a missile being initialised.
		/// </summary>
		public void Reset(Vector3 launchPosition)
		{
			hasLock = false;
			hasEverLocked = false;
			blindTime = 0f;
			lastEvaluationTime = 0f;
			nextEvaluationTime = 0f;
			errorOffset = Vector3.zero;
			lastKnownPosition = launchPosition;
			lastKnownVelocity = Vector3.zero;
			AimPoint = launchPosition;
			AimVelocity = Vector3.zero;
		}
	}
}
