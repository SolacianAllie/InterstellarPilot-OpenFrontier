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
		/// <summary>
		/// Last time an evaluation ran. Kept for the blind-time reset on a fresh
		/// seeker and nothing else - the interval itself is derived from
		/// <see cref="ProjectileClass.SeekerCheckInterval"/>, not from the gap
		/// between two runs.
		/// </summary>
		private float lastEvaluationTime;

		/// <summary>Time the next evaluation is due, in <see cref="Time.time"/>.</summary>
		private float nextEvaluationTime;

		/// <summary>
		/// True once the seeker has ever actually held the target, so a missile
		/// that launches already outside its cone is not written off before it
		/// has had a chance.
		/// </summary>
		private bool hasEverLocked;

		/// <summary>
		/// Whether the seeker was handed the target's position at launch. False
		/// for SARH, and false when there was no target to read.
		/// </summary>
		private bool hasInitialFix;

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
		/// How much of its target's signature this seeker is currently getting,
		/// as a fraction, because of the angle it is viewing the target from.
		/// </summary>
		/// <remarks>
		/// The input to <see cref="Notch"/> rather than anything read on its
		/// own, which is why it is private. One while locked on a target
		/// running away, falling towards the seeker's front factor as the
		/// target turns to present its nose.
		/// </remarks>
		private float LockQuality { get; set; } = 1f;

		/// <summary>
		/// Whether this seeker has ever held the target at all. The warning
		/// needs it to tell the difference between a missile that was shaken off
		/// - no longer worth an alarm - and one that never had a lock and is
		/// still coming, which very much is.
		/// </summary>
		/// <summary>Whether the seeker has ever actually held the target at all.</summary>
		public bool HasEverLocked => hasEverLocked;

		/// <summary>
		/// Whether this seeker has anything at all to fly at - either it can
		/// see the target, or it was handed a position to start from.
		/// </summary>
		/// <remarks>
		/// False means the missile is a rock. That is a real state and not an
		/// error: a semi-active missile whose launcher had no lock on the target
		/// genuinely does not know the target exists, and the honest thing for
		/// it to do is fly straight until it expires. Without this the seeker
		/// would steer at the un-seeded aim point, which is the position the
		/// missile was fired from - so it would turn around to go back where it
		/// came from.
		/// </remarks>
		public bool HasPicture => hasLock || hasInitialFix;

		public MissileSeeker(Missile missile)
		{
			this.missile = missile;
			ProjectileClass cls = missile.ProjectileClass;
			lockDot = CosineOfHalfAngle(cls.SeekerConeAngle);
			reacquireDot = CosineOfHalfAngle(cls.SeekerReacquireAngle > 0f ? cls.SeekerReacquireAngle : cls.SeekerConeAngle);
		}

		/// <summary>
		/// The most a seeker can ever see, as a half-angle.
		/// </summary>
		/// <remarks>
		/// Ninety degrees is the forward half-plane: the missile sees straight
		/// ahead and abeam, and nothing at all behind its own beam. That is a
		/// hard rule rather than a matter of taste, and it is clamped here so a
		/// prefab cannot break it by being authored badly.
		///
		/// It matters because of what a homing missile does with something it
		/// can see. Give it more than ninety and it will see a target astern
		/// and simply turn around to go after it, which turns every wide-cone
		/// missile into a U-turn seeker that will chase things it was never
		/// launched at. Ninety keeps the cone describing what is in front of
		/// the missile, which is the only question a seeker is being asked.
		///
		/// The trade is at the ceiling: a missile at exactly ninety has no room
		/// for a wider re-acquire angle, so the hysteresis in
		/// <see cref="ProjectileClass.SeekerReacquireAngle"/> flattens to
		/// nothing for the widest seekers. A wide cone and a hysteresis gap
		/// cannot both be had at ninety degrees.
		/// </remarks>
		public const float MaxConeHalfAngle = 90f;

		private static float CosineOfHalfAngle(float degrees)
		{
			if (degrees <= 0f)
			{
				// Unset, which is what every prefab written before the degrees
				// fields existed has. Cosine of 180 is -1 and would make the
				// cone see in every direction, which is the intent.
				return -1f;
			}
			return Mathf.Cos(Mathf.Min(degrees, MaxConeHalfAngle) * Mathf.Deg2Rad);
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
				lastEvaluationTime = now;
				nextEvaluationTime = now + interval;
				Evaluate();
			}
			if (!hasLock)
			{
				float drift = missile.ProjectileClass.SeekerBlindDrift * deltaTime;
				// Dead-reckon, but only for as long as the seeker would still be
				// willing to wait for the target to come back. After that the aim
				// point stops advancing and simply holds where the track ended.
				//
				// Without that bound a missile which never actually acquires -
				// and there is a whole class of those, because a target that
				// never radiates is never acquired - extrapolates the target's
				// launch velocity for the missile's entire flight. The aim point
				// recedes faster than the missile can follow, it curves after it
				// forever, and it leaves the engagement in a wide arc while
				// pointed confidently at a place the target was never going to be.
				blindTime += deltaTime;
				if (blindTime <= missile.ProjectileClass.SeekerLockPersistence)
				{
					lastKnownPosition += lastKnownVelocity * deltaTime;
				}
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
		private void Evaluate()
		{
			Unit target = missile.Target;
			if (target == null)
			{
				// Nothing to hunt for. This has to be checked before the hunt
				// rather than after it, because a missile that has given up has a
				// null target and the hunt would read that as "the current target
				// radiates nothing" and latch onto the first ship it walked past,
				// resurrecting a missile that had already finished.
				hasLock = false;
				return;
			}
			if (missile.ProjectileClass.GuidanceType == MissileGuidanceType.IR
				&& blindTime < missile.ProjectileClass.SeekerLockPersistence)
			{
				HuntForHotterContact();
				target = missile.Target;
				if (target == null)
				{
					hasLock = false;
					return;
				}
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
			// Note that lastKnownPosition is only ever written by Acquire or by
			// the launch fix, so a seeker with neither has nothing to dead-reckon
			// from and no picture to fly. That is deliberate, and the caller
			// relies on it: a missile that never saw its target and was never
			// told where it was is a rock.
			float dot = Vector3.Dot(toTarget / distance, missile.transform.forward);
			float threshold = hasLock ? lockDot : reacquireDot;
			if (dot >= threshold && SeesTarget(target, distance))
			{
				Acquire(target);
			}
			else
			{
				hasLock = false;
			}
			// Blind time is accumulated per frame in Update rather than here, so
			// that it is real seconds and does not depend on the seeker's own
			// evaluation interval.
			//
			// This deliberately no longer requires the seeker to have held
			// something first. A missile that launched with only a fix and never
			// acquired - which is every shot at a target that never radiates -
			// used to have no way to give up at all, and flew the whole engagement
			// chasing a dead-reckoned point. It flies to where the target was, the
			// target is not there, and it goes straight on. That is the honest
			// outcome and it is what the other three seekers already did.
			if (!hasLock && blindTime >= missile.ProjectileClass.SeekerLockPersistence)
			{
				missile.LoseTargetPermanently();
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
			RecordLockQuality(target);
		}

		/// <summary>
		/// Notes how much of its target's signature the seeker is getting, so a
		/// countermeasure attacking this missile knows how weak the lock is.
		/// </summary>
		/// <remarks>
		/// Recomputed on every acquisition rather than every frame. The aspect
		/// changes as the target turns, but a countermeasure only rolls four
		/// times a second and only reads this then, so a per-evaluation refresh
		/// is plenty and keeps it off the per-frame path.
		/// </remarks>
		private void RecordLockQuality(Unit target)
		{
			float signature = RadiatedHeat(target);
			if (signature <= 0f)
			{
				LockQuality = 1f;
				return;
			}
			float seen = SignatureFrom(target, missile.transform.position, missile.ProjectileClass);
			LockQuality = seen >= signature ? 1f : Mathf.Clamp01(seen / signature);
			Notch = BeamOnliness(target) * (1f - LockQuality);
		}

		/// <summary>
		/// How side-on the target is holding to this missile: one beam-on, zero
		/// dead astern or dead bow-on.
		/// </summary>
		/// <remarks>
		/// Notching is a side-on activity and this is the shape of it. The ship
		/// turns its beam to the missile and deploys into the weakened lock, and
		/// that pairing is the whole counter - neither half is much on its own.
		///
		/// It is deliberately NOT the same curve as the signature. A signature
		/// falls off steadily from astern to bow-on, because a seeker looking at
		/// a ship's nose sees less of its engines than one looking into its
		/// exhaust. But the notch peaks at the beam and is worth nothing at either
		/// end, because a ship flying straight at a missile is not evading it -
		/// and a ship running flat out is not evading it either. Presenting the
		/// flank is.
		///
		/// So flying bow-on dims you the most and helps the countermeasure
		/// least. That looks odd until you remember it also means flying directly
		/// at an incoming missile.
		/// </remarks>
		private float BeamOnliness(Unit target)
		{
			float sternFirst = SternFirstAspect(target, missile.transform.position);
			// SternFirstAspect is 1 astern, 0.5 beam-on, 0 bow-on, so this is a
			// triangle peaking at the middle.
			return 1f - Mathf.Abs(sternFirst - 0.5f) * 2f;
		}

		/// <summary>
		/// How well notched this seeker is: side-on to the missile and losing
		/// most of the signature as a result. Zero when either is untrue.
		/// </summary>
		/// <remarks>
		/// Read by a countermeasure, which is the other half of the manoeuvre.
		/// A notched lock is a lock worth attacking, and a lock is only worth
		/// attacking if the target actually turned side-on to earn the weakness.
		/// </remarks>
		public float Notch { get; private set; }

		/// <summary>
		/// The part that is genuinely different per seeker type. The cone and
		/// the drift are shared by all of them; this is what the cone is being
		/// pointed at.
		/// </summary>
		/// <summary>
		/// Whether a contact that has gone to cloak can still be held, which
		/// means it must have been seen before it went.
		/// </summary>
		/// <remarks>
		/// Cloak stops being a contact being invisible and starts being a
		/// contact being hard to keep, which is a better thing for it to be. A
		/// missile fired at a ship that was already cloaked has no picture to
		/// hold, so it stays blind - which is what stops cloak from being a free
		/// pass against a fresh salvo.
		///
		/// This is deliberately only the cloak half of the question. Whether the
		/// launcher is still pointing at the contact is a separate test, and it
		/// used to be folded into this one behind a cloak check - which meant it
		/// was only ever asked while the target was cloaked. A semi-active
		/// missile therefore tracked perfectly well with the firing ship turned
		/// right around, which is the one thing a semi-active missile cannot do:
		/// it has no radar of its own, so with the launcher looking elsewhere
		/// there is nothing lighting the target up and nothing to reflect off
		/// it.
		/// </remarks>
		private bool CanHoldCloaked(Unit target)
		{
			return !target.IsFullyCloaked || hasEverLocked;
		}

		/// <summary>
		/// Whether the ship that fired this missile still has its nose on the
		/// target, which is the whole of what a semi-active seeker rides.
		/// </summary>
		/// <remarks>
		/// Semi-active homing is not a weaker version of active homing, it is a
		/// different arrangement: the missile carries no transmitter and no
		/// receiver of its own worth the name, and instead listens to the echo
		/// off a target that the launching ship is illuminating. The moment the
		/// launching ship turns away that illumination stops, and the missile is
		/// a rock that happens to still be flying towards where the target was.
		///
		/// Nothing rotates an ActiveTurret in this game - the comment on
		/// <see cref="TurretComponent.RequiredLaunchAngle"/> says so, and a
		/// homing missile launches along the hull's own rotation. So the firing
		/// ship's transform is the antenna, and there is no separate turret
		/// transform to be reading instead.
		///
		/// This is also what stops a fire-and-forget tactic working. The launch
		/// itself is already gated on the ship's nose being on the target, since
		/// the required launch angle is the missile's own seeker cone - but that
		/// is only the instant of firing. A ship that turns hard immediately
		/// afterwards used to launch a SARH it then had no way to shake, which
		/// is the cheapest possible answer to the type.
		/// </remarks>
		private bool LauncherIlluminates(Unit target)
		{
			Unit launcher = missile.Projectile.SourceUnit;
			if (launcher == null || launcher.transform == null)
			{
				return false;
			}
			float arc = missile.ProjectileClass.SeekerLauncherArc;
			if (arc <= 0f)
			{
				// No arc authored, so this seeker type was not written to need
				// one. Held rather than blocked, because the field predates every
				// seeker in the game and reading it as zero must not silently
				// turn every semi-active missile into a rock.
				return true;
			}
			return Geometry.IsWithinBearing(launcher.transform, target.transform.position, arc);
		}

		/// <summary>
		/// Whether a ship is putting out enough radiated heat for a seeker to
		/// hold it.
		/// </summary>
		/// <remarks>
		/// Venting is visibility. What escapes is the lesser of what the engines
		/// are putting out and what the ship can get rid of - a ship cannot shed
		/// heat it has not made, and cannot make heat faster than it can leave.
		/// Hull size sets the ceiling on that, because every thruster's
		/// ThermalLoad is its own EnginePower times one constant.
		///
		/// The stored heat is deliberately NOT part of this, and getting that
		/// wrong is what produced two separate backwards results in a row.
		///
		/// Multiplying stored heat by the vent rate says a well-vented ship is
		/// invisible, because it has vented itself down to nothing and has nothing
		/// left to throw out - so fitting a better vent would make you harder to
		/// see, which is precisely backwards.
		///
		/// Using the engine's output alone says the opposite and just as wrongly:
		/// a ship with no radiator at all is running flat out with nowhere to put
		/// the heat, and becomes the brightest thing in the sector - also
		/// backwards, since a shut radiator means nothing escapes.
		///
		/// Neither needs the stored figure because it is clamped to 1 and lags
		/// the engine by seconds. Reading it would also mean a seeker could not
		/// see a ship that was plainly running its engines, which is not how a
		/// plume works.
		///
		/// This is where Silent Running belongs when it is built. It has to force
		/// the radiator shut rather than merely stop asking for vents, because
		/// stopping only drops this to the safety net - which is a very good vent
		/// and not a stealth mode at all.
		/// </remarks>
		private bool HasIrSignature(Unit target)
		{
			float threshold = missile.ProjectileClass.IrEngineHeatThreshold;
			if (threshold <= 0f)
			{
				// Nothing has asked for heat to matter, so every ship counts.
				return true;
			}
			return SignatureFrom(target, missile.transform.position, missile.ProjectileClass) >= threshold;
		}

		/// <summary>
		/// Whether a radar seeker can still hold a contact, which is where
		/// notching bites hardest.
		/// </summary>
		/// <remarks>
		/// A radar return is aspect-dependent in the same way a plume is, so a
		/// ship turning beam-on weakens the lock. It is deliberately a separate
		/// test from the disruption one rather than a multiplier on it, because
		/// the two failing together is the interesting case: notching on its own
		/// may leave a signature just above the seeker's threshold, and an ECM
		/// on its own may disrupt that particular missile, but a ship doing both
		/// is what actually breaks a lock that neither would have.
		/// </remarks>
		private bool HasRadarContact(Unit target)
		{
			float threshold = missile.ProjectileClass.RadarSignatureThreshold;
			if (threshold <= 0f)
			{
				return true;
			}
			return SignatureFrom(target, missile.transform.position, missile.ProjectileClass) >= threshold;
		}

		/// <summary>
		/// How much heat a ship is putting out into space right now.
		/// </summary>
		/// <remarks>
		/// This is what the engines are producing, not what the ship is storing,
		/// and the difference is not a detail. Heat in equals heat out at steady
		/// state, so a ship that is venting perfectly is emitting exactly as much
		/// as it is making - and a ship that is storing a great deal is emitting
		/// the same, just while it holds it. Reading the store would make a
		/// well-cooled hull the dimmest thing in the sector, which is backwards:
		/// the cool hull is the one running its engines flat out.
		///
		/// Which is also why venting does not change a signature. What vents buy
		/// is the ability to run hot without cooking - the stored heat, and so
		/// eventually the overheat - and that is a different consequence from
		/// being seen. A ship can be the brightest thing in a sector and perfectly
		/// comfortable, and that is correct.
		///
		/// This is where Silent Running belongs. Shutting the radiator stops
		/// heat escaping, so what the engines produce stops being emitted and only
		/// accumulates - which is a hard zero here rather than a second parallel
		/// check the other three seekers never hear about.
		///
		/// Hull size is the profile, and it comes straight from the engine: every
		/// thruster's ThermalLoad is its own EnginePower times one constant, so a
		/// scout barely registers and a battleship lights up the sector.
		/// </remarks>
		public static float RadiatedHeat(Unit unit)
		{
			if (unit == null)
			{
				return 0f;
			}
			UnitEngineComponent engine = unit.Components != null ? unit.Components.EngineComponent : null;
			if (engine == null)
			{
				return 0f;
			}
			// Engines shut: no plume, and AddFlightHeat is never reached either,
			// because a shut engine produces no force to push with.
			if (engine.EngineThrottle <= 0f)
			{
				return 0f;
			}
			// Thruster size, scaled by how hard the engines are being pushed.
			// Nothing here depends on the thermal vents, because no ship in the
			// game has a vent bay to fit one in - every hull radiated identically
			// and the seeker could not tell a scout from a battleship.
			return unit.ThrusterHeat * engine.EngineThrottle;
		}

		/// <summary>
		/// What a seeker actually gets from a ship, once aspect is taken into
		/// account.
		/// </summary>
		/// <remarks>
		/// This is notching, and it is the reason a target that turns is harder
		/// to hold. The signature a ship radiates is what is coming out of its
		/// engines; what a seeker can see is how much of that is facing it.
		/// Astern you are looking into the thrusters and see all of it. Dead
		/// ahead you are looking at the cold nose and see
		/// <see cref="ProjectileClass.SignatureFrontFactor"/> of it. Beam-on,
		/// which is where a pilot notching an incoming missile puts themselves,
		/// is halfway between.
		///
		/// The factor is read off the seeker's own class rather than the
		/// target's, because it is a property of the seeker: how good it is at
		/// resolving a weak return. A laser seeker holding a designation cares
		/// very little what aspect the ship is at - somebody already told it
		/// where the ship is - while a heat seeker looking for a plume has to
		/// actually see one.
		/// </remarks>
		private static float SignatureFrom(Unit target, Vector3 seekerPosition, ProjectileClass cls)
		{
			float signature = RadiatedHeat(target);
			float frontFactor = FrontFactorFor(signature, cls);
			if (signature <= 0f || frontFactor >= 1f)
			{
				return signature;
			}
			return signature * Mathf.Lerp(frontFactor, 1f, SternFirstAspect(target, seekerPosition));
		}

		/// <summary>
		/// How much of a signature survives being viewed bow-on, for a hull of
		/// this size.
		/// </summary>
		/// <remarks>
		/// The falloff scales with hull size, and that is the difference between
		/// notching being a counter and notching being a rounding error.
		///
		/// A fighter is a plume and a nose: there is very little of it that
		/// stops being visible when it turns, because most of what it gives off
		/// is the exhaust and you can see exhaust from most angles. A battleship
		/// is a wall of machinery with its engines at the back of it, and turning
		/// that broadside puts a great deal between the seeker and the heat.
		///
		/// So the small end of the ladder barely changes with aspect and the
		/// large end changes enormously - the Creon reads 13.76 astern and 3.44
		/// bow-on, while a Small hull reads 1.72 and 1.64. That is what makes a
		/// large ship genuinely better seen from behind rather than merely
		/// proportionally better, which is all a flat multiplier ever gave.
		///
		/// The brackets are the two ends of the real thruster ladder: nothing at
		/// or below a single small thruster is treated as omnidirectional, and
		/// anything at or above the largest hull gets the seeker's full falloff.
		/// </remarks>
		private static float FrontFactorFor(float signature, ProjectileClass cls)
		{
			if (cls.SignatureFrontFactor >= 1f)
			{
				return 1f;
			}
			float sizeT = Mathf.InverseLerp(OmnidirectionalBelow, FullyDirectionalAbove, signature);
			return Mathf.Lerp(1f, cls.SignatureFrontFactor, sizeT);
		}

		/// <summary>
		/// Zero when the target is flying straight at the seeker, one when it is
		/// running away and the seeker is looking into its engines.
		/// </summary>
		private static float SternFirstAspect(Unit target, Vector3 seekerPosition)
		{
			Vector3 toSeeker = seekerPosition - target.transform.position;
			toSeeker.y = 0f;
			if (toSeeker.sqrMagnitude <= 0.000001f)
			{
				return 1f;
			}
			Vector3 facing = target.transform.forward;
			facing.y = 0f;
			if (facing.sqrMagnitude <= 0.000001f)
			{
				return 1f;
			}
			facing.Normalize();
			toSeeker.Normalize();
			// +1 when the ship is flying straight at the seeker, so the seeker is
			// looking at its nose; -1 when it is running away.
			return (1f - Vector3.Dot(facing, toSeeker)) * 0.5f;
		}

		/// <summary>Signature at or below which a hull barely changes with aspect.</summary>
		public const float OmnidirectionalBelow = 1f;

		/// <summary>Signature at or above which a hull gets the seeker's full falloff.</summary>
		public const float FullyDirectionalAbove = 12f;

		/// <summary>
		/// Points the missile at something hotter, if there is anything hotter
		/// inside the cone.
		/// </summary>
		/// <remarks>
		/// Deliberately not gated on having lost the target. An infrared seeker
		/// that only started hunting once it had been shaken would be a seeker
		/// you could stop by breaking a lock rather than one you have to outrun,
		/// and the whole point of locking onto the hottest thing available is
		/// that it is a continuous comparison rather than a recovery path.
		///
		/// The switch goes through the projectile's own Target setter, which
		/// rebuilds the target collider and rigidbody, so the missile keeps
		/// hitting what it is now aimed at. That also means the warning system
		/// follows: a missile that has gone over to your escort is reported as
		/// locking onto your escort.
		/// </remarks>
		private void HuntForHotterContact()
		{
			Unit hotter = FindHottestContact();
			if (hotter != null)
			{
				SetTarget(hotter);
			}
		}

		/// <summary>
		/// Finds the hottest thing this seeker can see, and returns null when
		/// nothing beats the target it already has.
		/// </summary>
		/// <remarks>
		/// A heat seeker cannot tell a hostile from a friendly, and pretending
		/// otherwise would be the one thing a seeker genuinely cannot do. So this
		/// looks at every ship and station in the sector, with no faction test
		/// anywhere in it, and takes the strongest signature it finds.
		///
		/// The one thing it will not consider is the ship that fired it. That
		/// exclusion used to be written against Projectile.Unit, which is the
		/// missile's OWN unit rather than the launcher's, so it excluded the
		/// missile - which is not in any of these lists anyway - and let the
		/// launcher straight through. The rule that stops a heat seeker locking
		/// its own ship has therefore never once run.
		///
		/// Which is why fast fighters shot themselves with the Lancer. Nothing
		/// about launch velocity is involved. A signature is thruster heat times
		/// throttle, so the harder a ship is accelerating the hotter it looks,
		/// and a fighter running flat out is the brightest thing in its own
		/// neighbourhood. The missile has just left that fighter's nose, so it is
		/// inside the cone, at point blank range, looking straight at the single
		/// hottest object in the sector - and it locks on. The faster the fighter,
		/// the brighter it is and the more certain the lock.
		///
		/// The cone and the ranking are unchanged by this. It is cone-limited,
		/// meaning something: turn away from the hot ship and there is nothing
		/// to lock. It runs on the seeker's own evaluation rather than only on
		/// losing the target, so a seeker that was locked onto something modest
		/// will notice a battleship lighting up in front of it and change its
		/// mind - several times, if the ranking changes.
		///
		/// Choosing the outright maximum each pass rather than the first thing
		/// better than the current target is what keeps that from flapping: the
		/// answer is always the same argmax, so the only way to oscillate is for
		/// two ships' heat to genuinely cross over, and stored heat moves
		/// smoothly enough that that is rare.
		///
		/// The scan is two lists off the sector - ships and stations - and each
		/// unit is rejected on squared distance before anything else is asked of
		/// it, so the common case is a handful of comparisons. Only infrared
		/// missiles ever do this, and only over the cone.
		/// </remarks>
		private Unit FindHottestContact()
		{
			Unit best = null;
			Vector3 nose = missile.transform.forward;
			Vector3 self = missile.transform.position;
			float bestHeat = SignatureOf(missile.Target, self);
			Unit launcher = missile.Projectile.SourceUnit;
			Sector sector = missile.Projectile.Unit != null ? missile.Projectile.Unit.Sector : null;
			if (sector == null)
			{
				return null;
			}
			float threshold = missile.ProjectileClass.IrEngineHeatThreshold;
			// -1 means the cone is unlimited, in which case the test below is
			// skipped entirely rather than compared against a cosine of -1 that
			// would only ever be true by accident.
			float coneDot = reacquireDot;
			bool coneLimited = coneDot > -1f;
			for (int type = 0; type < 3; type++)
			{
				List<Unit> units = sector.GetUnitsByType(type == 0 ? UnitType.Ship
					: type == 1 ? UnitType.Station : UnitType.Projectile);
				if (units == null)
				{
					continue;
				}
				// Re-read the count every pass. A unit can be destroyed while
				// this runs - an explosion does not wait for a seeker - and the
				// list is not a snapshot, so caching the count would read past
				// the end rather than merely miss somebody.
				for (int i = 0; i < units.Count; i++)
				{
					Unit candidate = units[i];
					if (candidate == null || candidate == missile.Projectile.Unit || candidate == launcher || candidate == best)
					{
						continue;
					}
					// A countermeasure puff is a Unit of type Other and it has no
					// engines, so without this the hunt would score it at zero and
					// sail past the very thing it is meant to be attracted to.
					Countermeasure cloud = candidate.CountermeasureCloud;
					if (cloud == null)
					{
						// The Projectile list is walked only for puffs; anything
						// else in it is a shot already in the air.
						if (type == 2 || candidate.IsFullyCloaked || candidate.IsDocked)
						{
							continue;
						}
					}
					else if (cloud.IrSignature <= 0f)
					{
						// An expended or spent puff is just a fading gas cloud.
						continue;
					}
					Vector3 offset = candidate.transform.position - self;
					float distanceSq = offset.sqrMagnitude;
					if (distanceSq <= 0.0001f)
					{
						continue;
					}
					if (Vector3.Dot(offset, nose) <= 0f)
					{
						// Behind the missile. Cheap reject first: with a
						// ninety degree ceiling this is more than half of a
						// sector's worth of candidates.
						continue;
					}
					if (coneLimited && Vector3.Dot(offset / Mathf.Sqrt(distanceSq), nose) < coneDot)
					{
						continue;
					}
					float heat = SignatureOf(candidate, self);
					if (heat <= threshold || heat <= bestHeat)
					{
						continue;
					}
					bestHeat = heat;
					best = candidate;
				}
			}
			return best;
		}

		/// <summary>
		/// What a candidate is worth to this seeker: its own signature if it is
		/// a countermeasure puff, otherwise its aspect-adjusted ship signature.
		/// </summary>
		/// <remarks>
		/// A gas cloud radiates the same in every direction, because it is a
		/// cloud and not a ship with a nose. Running it through the aspect
		/// calculation would make an ECM deployed behind you harder for an IR
		/// seeker to notice than one deployed in front, which is precisely
		/// backwards - the whole point of throwing one is to put it between you
		/// and the missile.
		/// </remarks>
		private float SignatureOf(Unit candidate, Vector3 seekerPosition)
		{
			Countermeasure cloud = candidate != null ? candidate.CountermeasureCloud : null;
			if (cloud != null)
			{
				return cloud.IrSignature;
			}
			return SignatureFrom(candidate, seekerPosition, missile.ProjectileClass);
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
					// Follows the engines, so a cloaked ship has no signature
					// left to follow and a cold one has nothing hot enough to
					// follow either. A flare in the same part of the sky is
					// indistinguishable from the exhaust. An ECM puff does
					// nothing for it, which is exactly why carrying one is a
					// choice rather than an answer.
					return !target.IsFullyCloaked && !missile.IsDazzled && HasIrSignature(target);

				case MissileGuidanceType.SARH:
					// Semi-active: it has no radar of its own and rides the
					// launching ship's, so the ECM beats it by breaking that
					// picture up, the launcher beating it by turning away, cloak
					// beats it by taking the ship out of that picture, and
					// notching beats it by turning the ship edge-on to the radar.
					//
					// Four answers, which makes it the most fragile radar lock in
					// the game despite the middling cone. The launcher arc is not
					// one of the answers - it is the premise. Everything else
					// only gets asked once the ship is still pointing at the
					// target at all.
					return !missile.IsDisrupted && HasRadarContact(target)
						&& LauncherIlluminates(target) && CanHoldCloaked(target);

				case MissileGuidanceType.ARH:
				default:
					// Active radar does not need anyone to light the target up,
					// but it is still a radar receiver on the end of a missile,
					// and a jammer does not care whose radar it is. So an ECM
					// beats this one too.
					//
					// It is notched as well - an active radar still has to resolve
					// a return, and a ship beam-on is a poor one. What ARH does
					// not suffer from is the launcher's picture: the missile can
					// hold a cloaked contact and does not care what the firing
					// ship is doing, because it is lighting the target up itself.
					return !missile.IsDisrupted && HasRadarContact(target) && CanHoldCloaked(target);
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
		/// Re-aims this missile at a different ship, keeping the picture it
		/// already had if it can.
		/// </summary>
		/// <remarks>
		/// The last known position is carried over when the new contact is
		/// somewhere the seeker has never been, so that a missile switching to a
		/// target it cannot yet see still has a direction to fly rather than
		/// dropping back to the un-seeded aim point - which is the position it
		/// was fired from.
		/// </remarks>
		public void SetTarget(Unit newTarget)
		{
			if (newTarget == null || newTarget == missile.Target)
			{
				return;
			}
			missile.Projectile.Target = newTarget;
			lastKnownPosition = newTarget.transform.position;
			Rigidbody newBody = newTarget.RBody;
			lastKnownVelocity = newBody != null ? newBody.linearVelocity : Vector3.zero;
			AimPoint = lastKnownPosition;
			AimVelocity = lastKnownVelocity;
			errorOffset = Vector3.zero;
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
		/// <remarks>
		/// Most seeker types are handed the target's position and velocity the
		/// moment they are fired, and treat that as their picture until they can
		/// do better. It is not a lock and it is not guidance - it is simply
		/// where the ship was at the instant the trigger was pulled, which the
		/// firing ship knew and can pass on.
		///
		/// This is what makes a bay rotated away from the target survivable. A
		/// launcher's direction and the pilot's chosen target are not the same
		/// thing - a bay can be turned to face aft - so a missile can leave the
		/// tube pointing well away from what it was fired at. Without an initial
		/// position it would have to fly blind until it worked out where to go,
		/// and with a tight cone it might never work it out at all. With one it
		/// flies to where the target was, and re-locks whenever the target comes
		/// back inside the cone - which, if the target has not moved, is
		/// immediately.
		///
		/// Two kinds of missile do not get one, and
		/// <see cref="TakeInitialFix"/> is where that is decided. A round that
		/// turns to bear on its target at launch is already pointing the right
		/// way. And SARH is riding the launcher's radar picture, so it has to
		/// earn its lock honestly rather than be handed a position the firing
		/// ship never had.
		/// </remarks>
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
			hasInitialFix = TakeInitialFix();
		}

		/// <summary>
		/// Reads the target's position and velocity as the starting picture.
		/// False when this seeker type is not entitled to one, or there is no
		/// target to read.
		/// </summary>
		/// <remarks>
		/// The test is RotateTowardsTarget, and it is not a new flag because
		/// that field already says the thing this needs to know. A round with
		/// it set is aimed at its target as it leaves the tube, so it is already
		/// pointing the right way and has nothing to be told. A round without it
		/// is fired along the launcher - which is the four guided missiles, and
		/// is exactly the case where a bay turned away from the target sends it
		/// out pointing the wrong way and it needs the firing ship's knowledge
		/// to get going.
		///
		/// So the Small Rocket is the exception the data already describes: it
		/// is the one round that turns to bear on its target at launch, which
		/// is why it needs no fix, and why with no beam on the target it flies
		/// exactly as it always did.
		/// </remarks>
		private bool TakeInitialFix()
		{
			if (missile.ProjectileClass.RotateTowardsTarget)
			{
				return false;
			}
			if (missile.ProjectileClass.GuidanceType == MissileGuidanceType.SARH)
			{
				// A semi-active seeker is riding the launching ship's radar
				// picture; if the launcher has no lock then the missile has no
				// idea the target exists. Handing it a free position anyway
				// would give away the hardest counter to SARH - an ECM, or a
				// shot from a launcher that was itself looking elsewhere.
				return false;
			}
			Unit target = missile.Target;
			if (target == null || target.transform == null)
			{
				return false;
			}
			lastKnownPosition = target.transform.position;
			Rigidbody targetBody = target.RBody;
			lastKnownVelocity = targetBody != null ? targetBody.linearVelocity : Vector3.zero;
			AimPoint = lastKnownPosition;
			AimVelocity = lastKnownVelocity;
			return true;
		}
	}
}
