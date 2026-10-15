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
		/// Zero means unset, which is what every prefab written before this
		/// field existed reads, and zero means NO cone - the missile sees in
		/// every direction exactly as it always did. That is deliberate: it
		/// makes the field safe to add to existing content, and it means a
		/// missile that predates seekers keeps behaving like one.
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
		/// How much engine heat a ship must be putting out before an infrared
		/// seeker can hold it, as a throttle level. Zero means every ship
		/// radiates enough.
		/// </summary>
		/// <remarks>
		/// An IR seeker does not look at a ship, it looks at the engines -
		/// they are the hot part and everything else is roughly the
		/// temperature of space. So the signature is throttle, not speed: a
		/// ship coasting at full speed with its engines shut has gone cold,
		/// and a ship holding station under a burn is still lit up.
		///
		/// That asymmetry is deliberate. Keying the check on speed instead
		/// would mean a ship could sit still with its engines running and be
		/// invisible to a heat seeker, which is not a thing a heat seeker can
		/// be fooled by. The counterplay to IR is to cut throttle, and that is
		/// a decision with a cost rather than a free one.
		///
		/// It lives on the projectile class rather than on the seeker so a
		/// missile can be tuned to how good its seeker actually is. Zero is
		/// the default because that is what every prefab written before this
		/// field existed reads, and zero is what IR did before engines were
		/// part of the question.
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
