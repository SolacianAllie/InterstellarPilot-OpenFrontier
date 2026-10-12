using OpenFrontier.IP.Common;
using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public abstract class TurretClass : ComponentClass
	{
		public bool IsPointDefence;

		public float ManualCombatRating = -1f;

		public float ManualDamagePerSecond = -1f;

		public float CalculatedDamagePerSecond;

		public float CalculatedCombatRating;

		public bool UsesAmmo;

		public bool AIConventionalWeapon = true;

		public float AiFireProbabilityPower = 0.5f;

		public float AIMaxTimeBeforeFire = 240f;

		public float AIMinTimeBeforeFire = 5f;

		public float AIPointDefenseEffectiveness = 1f;

		public TurretGroups DefaultTurretGroup;

		public float EnergyCost = 100f;

		public float EnergyCostPerSecond;

		public float EnergyToDamageConversion = 1f;

		public GameObject FireAudioPrefab;

		public GameObject FireChargeAudioPrefab;

		public GameObject FireLoopAudioSource;

		public float FirePrewarmTime;

		public float InaccuracyPower = 2f;

		public float MaxFiringRange = 100f;

		public float InaccuracyMultiplier = 1f;

		public float MinAppliedDamage = 0.8f;

		public float MinFiringRange;

		public bool RequiresTarget = true;

		public bool RestrictAIUsage;

		public float AutoTurretFireCooldown;

		public ShieldDamageType ShieldDamageType;

		public GameObject TurretPrefab;

		public float FireInterval => EnergyCost / EnergyChargeRate;

		public bool IsMiningLaser => ComponentBayType.BayType == BayType.Mining;

		public override bool IsWeapon => true;

		public float GetRandomDamageFromEnergy()
		{
			return EnergyCost * EnergyToDamageConversion * Random.Range(MinAppliedDamage, 1f);
		}

		public float GetMaxDamageFromEnergyCost()
		{
			return EnergyCost * EnergyToDamageConversion;
		}

		public virtual bool CanFireAt(Unit target)
		{
			return true;
		}

		public virtual float GetMinFireRequiredEnergy()
		{
			return EnergyCost;
		}

		protected override ComponentBase createComponent(GameObject target)
		{
			TurretComponent turretComponent = target.AddComponent<TurretComponent>();
			turretComponent.TurretClass = this;
			return turretComponent;
		}
	}
}
