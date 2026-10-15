using System.Collections.Generic;
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
			// Note that lastKnownPosition is only ever written by Acquire, so a
			// seeker that has never held anything has nothing to dead-reckon
			// from and no picture to fly. That is deliberate, and the caller
			// relies on it: a missile that never saw its target is a rock.
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
		/// <summary>
		/// Whether this seeker can still hold a contact that has gone to cloak.
		/// </summary>
		/// <remarks>
		/// Cloak stops being a contact being invisible and starts being a
		/// contact being hard to keep, which is a better thing for it to be.
		/// Three things have to line up, and they are the three ways a ship
		/// should be able to break a radar lock it did not have before:
		///
		/// It must have been seen first. A missile fired at a ship that was
		/// already cloaked has no picture to hold, so it stays blind - which is
		/// what stops cloak from being a free pass against a fresh salvo.
		///
		/// And then what "keeping hold" means depends on the seeker, which is
		/// the whole practical difference between the two radar types. An
		/// active seeker carries its own radar, so by the time cloak is in
		/// play its own cone has already answered the question and the contact
		/// simply stays held - the launcher can turn away and it does not
		/// matter. A semi-active seeker is riding the launching ship's picture,
		/// so the launcher has to keep its nose on the target, and a ship that
		/// cloaks and then turns away breaks it.
		///
		/// The missile's own cone gates all of this already - by the time this
		/// is asked the contact is inside it - so only the semi-active case
		/// needs an angle of its own.
		/// </remarks>
		/// <param name="target">The contact being asked about.</param>
		/// <param name="needsLauncherArc">
		/// True for a seeker riding the launcher's picture, false for one
		/// carrying its own radar.
		/// </param>
		private bool CanHoldCloaked(Unit target, bool needsLauncherArc)
		{
			if (!target.IsFullyCloaked)
			{
				return true;
			}
			if (!hasEverLocked)
			{
				return false;
			}
			if (!needsLauncherArc)
			{
				return true;
			}
			Unit launcher = missile.Projectile.SourceUnit;
			if (launcher == null)
			{
				return false;
			}
			return Geometry.IsWithinBearing(launcher.transform, target.transform.position, missile.ProjectileClass.SeekerCloakedHoldArc);
		}

		private bool SeesTarget(Unit target, float distance)
		{
			switch (missile.ProjectileClass.GuidanceType)
			{
				case MissileGuidanceType.Optical:
					// A laser seeker sees nothing by itself. It is riding the
					// beam the launcher is pointing at the target, so the beam
					// has to still be there - which means the launching ship
					// has to still be designating that target, and the utility
					// mount has to have swung onto it. That is the whole
					// weakness of this type, and the reason it is at the bottom
					// of the order.
					//
					// Cloak still beats it, even though the designator is holding
					// a beam on a ship it has already identified. Letting the
					// laser through would make this the one seeker in the game
					// that nothing can beat except losing the mount, and it is
					// meant to be the weakest of the four.
					return !target.IsFullyCloaked && IsDesignated(target);

				case MissileGuidanceType.IR:
					// Follows the target's own heat, so a cloaked ship has no
					// signature to follow at all - there is nothing left to hold,
					// and no amount of having seen it beforehand brings the heat
					// back. A flare in the same part of the sky is
					// indistinguishable from the ship itself. An ECM puff does
					// nothing for it, which is exactly why carrying one is a
					// choice rather than an answer.
					return !target.IsFullyCloaked && !missile.IsDazzled;

				case MissileGuidanceType.SARH:
					// Semi-active: it is riding the launching ship's radar
					// picture, so the ECM beats it by breaking that picture up,
					// and cloak beats it by taking the ship out of the
					// launcher's nose. It is the only seeker with two answers.
					return !missile.IsDisrupted && CanHoldCloaked(target, needsLauncherArc: true);

				case MissileGuidanceType.ARH:
				default:
					// Active radar does not need anyone to light the target up,
					// but it is still a radar receiver on the end of a missile,
					// and a jammer does not care whose radar it is. So an ECM
					// beats this one too.
					//
					// Cloak does not, and neither does the launcher wandering
					// off - the missile's own cone is the whole story here, and
					// it is the widest cone in the game. That is what makes ARH
					// the hardest lock to shake: jammed is the only thing that
					// beats it.
					return !missile.IsDisrupted && CanHoldCloaked(target, needsLauncherArc: false);
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
		/// Whether the launching ship is still holding a beam on this target.
		/// </summary>
		/// <remarks>
		/// The launcher is resolved once and kept, because a missile does not
		/// change hands mid-flight and a launcher's bay can be blown off it
		/// without that being a reason to start looking for a different ship.
		/// Every mount on it counts: a ship with two has two beams and no reason
		/// to pick between them.
		/// </remarks>
		private bool IsDesignated(Unit target)
		{
			Unit launcher = missile.Projectile.SourceUnit;
			if (launcher == null || launcher.Components == null)
			{
				return false;
			}
			List<UtilityMountComponent> mounts = launcher.Components.UtilityMounts;
			for (int i = 0; i < mounts.Count; i++)
			{
				UtilityMountComponent mount = mounts[i];
				if (mount != null && mount.IsLasing(target))
				{
					return true;
				}
			}
			return false;
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
