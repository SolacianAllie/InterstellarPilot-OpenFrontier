using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class ProjectileClass : MonoBehaviour
	{
		public float EffectiveFiringRangeStationery = 10000f;

		public float EffectiveFiringRangeAgainstMobile = 10000f;

		public bool IsWeapon = true;

		public string Name;

		public bool IsConventionalWeapon = true;

		public CargoClass AmmoClass;

		public int AmmoRequirement = 1;

		public int BurstRounds = 1;

		public float BurstTimeBetweenRounds = 0.2f;

		public bool CollisionEnabled = true;

		public bool CreateExplosionWhenExpired;

		public float Damage = 10f;

		public float DamageFuzziness = 0.1f;

		public int DefaultAmmoComplement = 3;

		public bool DestroyOnCollide;

		public float DestroyOnCollideDelayTime = 2f;

		public ExplosionClass ExplosionClass;

		public float HomingAquiredTargetThreshold = 0.5f;

		public bool HomingCanLoseTarget = true;

		public float HomingLostTargetDotThreshold = 0.25f;

		public bool IsHoming;

		/// <summary>
		/// Half-angle of the seeker cone, in degrees - how far off its nose the
		/// target has to be before this missile stops being able to see it.
		/// </summary>
		/// <remarks>
		/// This is the degrees equivalent of the two dot thresholds below, and
		/// it is the one to author: 0.5 is not obviously 60 degrees, and every
		/// missile in the game wants a different number.
		///
		/// It is a half-angle, measured from the missile's nose, so 90 means it
		/// sees 90 degrees either side of straight ahead and 180 degrees
		/// across in total.
		///
		/// It decides two things, because they are the same thing. The seeker
		/// cannot see past it - and the launching ship cannot fire the missile
		/// at a target outside it, because a missile leaves along the hull's
		/// nose and would spend the whole flight unable to acquire. So the
		/// launch arc is read from here rather than stored on the turret.
		///
		/// Zero means unset, which is what every prefab written before this
		/// field existed reads, and zero means NO cone - the missile sees in
		/// every direction exactly as it always did, and imposes no launch
		/// requirement. That is deliberate: it makes the field safe to add to
		/// existing content, and it means a missile that predates seekers keeps
		/// behaving like one.
		/// </remarks>
		public float SeekerConeAngle;

		/// <summary>
		/// The wider cone a blind missile has to get the target back inside to
		/// re-acquire. Zero means the same as <see cref="SeekerConeAngle"/>.
		/// </summary>
		/// <remarks>
		/// Without the gap a missile sitting exactly on the edge of its cone
		/// flickers between locked and blind every evaluation, and the
		/// dead-reckoning below turns that flicker into a random walk.
		/// </remarks>
		public float SeekerReacquireAngle;

		/// <summary>
		/// Seconds the seeker may stay blind before the missile gives up on the
		/// target for good. Zero means no second chances - one blind evaluation
		/// ends it, which is what every missile did before seekers existed.
		/// </summary>
		public float SeekerLockPersistence;

		/// <summary>
		/// How fast, in metres per second, the blind missile's guess at where
		/// the target is wanders away from the truth.
		/// </summary>
		/// <remarks>
		/// This is what makes a broken lock cost something. A missile that
		/// loses its cone does not snap back on: it flies on the last position
		/// and velocity it knew, and gets it steadily more wrong.
		/// </remarks>
		public float SeekerBlindDrift;

		/// <summary>
		/// How often the seeker checks whether it can see the target, in
		/// seconds. Zero falls back to the default rather than to every frame.
		/// </summary>
		public float SeekerCheckInterval;

		/// <summary>
		/// How far off the launching ship's nose the target has to be for a
		/// semi-active seeker to hold it at all, in degrees. Zero means no arc
		/// was authored and the launcher is not consulted.
		/// </summary>
		/// <remarks>
		/// Only a semi-active seeker reads this, and it is not one of the ways
		/// to beat that type - it is the premise. A semi-active missile carries
		/// no radar of its own and listens to the echo off a target the
		/// launching ship is illuminating, so the moment that ship turns away
		/// there is nothing lighting the target up and the missile goes blind
		/// whether the target is cloaked or not.
		///
		/// It used to be named SeekerCloakedHoldArc and only consulted while
		/// the contact was cloaked, on the reasoning that the launcher picture
		/// was only interesting once cloak was in play. That was wrong: it made
		/// a semi-active missile track a target its firing ship had turned right
		/// around from, which is the one thing semi-active homing cannot do. The
		/// cloak rule - that the contact must have been seen before it went -
		/// is a separate test in MissileSeeker and always was.
		///
		/// An active seeker does not consult it at all. Its own transmitter
		/// lights the target up, so the launcher can do whatever it likes, and
		/// that is the whole practical difference between the two types and the
		/// reason a ship under ARH missiles has to fight them rather than turn
		/// away.
		///
		/// Zero is the default because that is what every prefab written before
		/// this field existed reads. It is read as "do not consult the launcher"
		/// rather than as "the target must be dead ahead", so a prefab that has
		/// never heard of the field keeps the behaviour it had.
		/// </remarks>
		public float SeekerLauncherArc;

		/// <summary>
		/// How much radiated heat a ship must be putting out before an infrared
		/// seeker can hold it. Zero means every ship counts.
		/// </summary>
		/// <remarks>
		/// Venting is visibility. This is measured as what a ship is actually
		/// throwing out - what it can shed, times how much it has to shed - and
		/// not as what it is storing or as what its engines are producing.
		///
		/// Both of those were tried and both are wrong in opposite directions.
		/// The store alone makes a well-cooled hull the dimmest thing in the
		/// sector. The engine's output alone makes a ship with no radiator the
		/// brightest, because it is running flat out with nowhere to put the heat
		/// - exactly backwards, since a shut radiator means nothing escapes.
		///
		/// So the trade is the interesting one: fitting a big thermal vent makes
		/// a ship a bright, obvious target to anything looking for heat, and
		/// fitting none makes it dark and unable to run hot for long.
		///
		/// Because every thruster's ThermalLoad is its own EnginePower times one
		/// constant, hull size sets the ceiling on how hot a ship can get: a
		/// scout tops out well below what a radiator can vent, so it can never
		/// out-radiate a battleship however well vented, while a battleship
		/// saturates and becomes the brightest thing in the sector.
		///
		/// It lives on the projectile class so a missile can be tuned to how good
		/// its seeker actually be. Zero is the default because that is what every
		/// prefab written before this field existed reads.
		/// </remarks>
		public float IrEngineHeatThreshold;

		/// <summary>
		/// How much of a signature is still visible when the target is flying
		/// straight at the seeker. 1 means aspect does not matter at all, which
		/// is what every prefab written before this field existed reads.
		/// </summary>
		/// <remarks>
		/// Notching, and it is the same mechanic for every seeker. What a
		/// sensor sees depends on which way the target is pointing: from astern
		/// you see the engines, from dead ahead you see the cold nose, and
		/// beam-on is somewhere between. Flying beam-on to an incoming missile
		/// is called notching, and it is the cheapest counter there is because
		/// it costs nothing - you are already turning.
		///
		/// It works on radar and on heat at once, for the same reason it works
		/// on both in reality: it is a property of the target's shape and where
		/// its heat is, not of the sensor. And it is a continuum, which is what
		/// makes it interesting - a seeker whose threshold sits near a hull's
		/// notched signature will hold sometimes and lose sometimes, so turning
		/// is a real answer rather than a guaranteed one.
		///
		/// Half is the figure worth starting from, which is what the reference
		/// this was modelled on uses for radar.
		/// </remarks>
		public float SignatureFrontFactor = 1f;

		/// <summary>
		/// How much signature a radar seeker needs off a target to hold it.
		/// Zero means any contact at all will do.
		/// </summary>
		/// <remarks>
		/// This is what makes notching bite. A ship running away is seen from
		/// astern at full signature; one turning to meet the missile is seen
		/// bow-on at <see cref="SignatureFrontFactor"/> of that, and one holding
		/// beam-on halfway between. Set the threshold above a hull's notched
		/// signature and turning becomes a way to break a lock.
		///
		/// It is a separate field from <see cref="IrEngineHeatThreshold"/>
		/// because a radar return and a plume are different quantities with
		/// different scales, and because a good radar seeker should be able to
		/// hold a signature an IR seeker would find unremarkable.
		///
		/// Zero is the default, because that is what every prefab written before
		/// this field existed reads, and because it keeps radar seekers working
		/// exactly as they did until someone sets a number.
		/// </remarks>
		public float RadarSignatureThreshold;

		public bool IsMine;

		/// <summary>
		/// How this missile finds its target, worst to best: Optical, IR, SARH,
		/// ARH. Decides the warning the player gets, and Optical gets none.
		/// </summary>
		/// <remarks>
		/// Defaults to Optical, so every missile that predates this field is
		/// untyped and nothing changes. The order is the order - a higher value
		/// wins when several missiles are locked on at once.
		/// </remarks>
		public MissileGuidanceType GuidanceType = MissileGuidanceType.Optical;

		public float MaxMoveRange = 200f;

		public float MiningDamageMultiplier = 0.1f;

		public float MoveSpeed = 100f;

		public bool ParentExplosionToTarget;

		public GameObject ProjectilePrefab;

		public bool RotateTowardsTarget = true;

		public bool SetDamageUsingEnergyCost = true;

		public ShieldDamageType ShieldDamageType;

		public float ThrustPower = 1f;

		public float MinThrust = 0.2f;

		public bool TimedDestruction;

		public float TimedExplosionInaccuracy = 30f;

		public float TurnRate = 90f;

		public bool InheritParentVelocity;

		public float InheritParentVelocityMultiplier = 1f;

		public float InaccuracyMultiplier = 1f;

		public float MissileIntelligentGuidanceMaxDist = 200f;

		public bool MissileIntelligentGuidance = true;

		public float MinTimeBeforeGuidanceActivation = 1f;

		public float MaxTimeBeforeGuidanceActivation = 2f;

		public float MaxDuration;

		public float GetFuzzyDamage()
		{
			return Mathf.Lerp(Damage * (1f - DamageFuzziness), Damage * (1f + DamageFuzziness), Random.value);
		}

		public float GetMissileMaxSpd()
		{
			Rigidbody component = ProjectilePrefab.GetComponent<Rigidbody>();
			return UnitEngineClass.CalculateMaxSpeed(ThrustPower, component.linearDamping, component.mass);
		}
	}
}
