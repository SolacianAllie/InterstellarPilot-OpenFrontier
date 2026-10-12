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

		public bool IsMine;

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
