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
		/// How far off the launching ship's nose a cloaked contact has to be
		/// for this seeker to keep hold of it, in degrees. Zero means it cannot
		/// hold one at all.
		/// </summary>
		/// <remarks>
		/// Only a semi-active seeker reads this. It rides the launching ship's
		/// radar picture rather than a radar of its own, so when the ship it
		/// was fired from turns away, the picture goes with it and the missile
		/// goes blind to a contact it had already found. An active seeker does
		/// not care what the launcher is doing - its own cone has already
		/// decided the question by the time cloak matters - which is the whole
		/// practical difference between the two, and the reason a ship under
		/// ARH missiles has to fight them rather than turn away.
		///
		/// Zero is the default because that is what every prefab written before
		/// this field existed reads, and zero is what cloak did to every
		/// missile in the base game.
		/// </remarks>
		public float SeekerCloakedHoldArc;

		/// <summary>
		/// How much radiated heat a ship must be putting out before an infrared
		/// seeker can hold it. Zero means every ship counts.
		/// </summary>
		/// <remarks>
		/// Measured as what the ship is putting out - which is what its engines
		/// are producing - and not as what it is storing.
		///
		/// Storing is the obvious mistake and was the first thing tried. Heat in
		/// equals heat out at steady state, so a perfectly cooled hull is
		/// emitting exactly as much as a saturated one; it is just not carrying
		/// any. Measuring the store made the ship running flat out the dimmest
		/// thing in the sector.
		///
		/// So venting does not change a signature. What thermal vents buy is the
		/// ability to run hot without cooking, and that is a separate consequence
		/// from being seen - a ship can be the brightest thing in a sector and
		/// perfectly comfortable, which is correct.
		///
		/// Because every thruster's ThermalLoad is its own EnginePower times one
		/// constant, hull size is the heat profile: a scout barely registers
		/// and a battleship lights up the sector.
		///
		/// It lives on the projectile class so a missile can be tuned to how good
		/// its seeker actually be. Zero is the default because that is what every
		/// prefab written before this field existed reads.
		/// </remarks>
		public float IrEngineHeatThreshold;

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
