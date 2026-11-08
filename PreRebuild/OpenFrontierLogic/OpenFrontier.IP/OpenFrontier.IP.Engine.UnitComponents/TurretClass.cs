using OpenFrontier.IP.Common;
using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public abstract class TurretClass : ComponentClass
	{
		[Tooltip("This weapon defends its own ship rather than engaging at range.")]
		public bool IsPointDefence;

		[Tooltip("Would override the worked-out combat rating. Nothing reads it at the moment - the calculated value is used instead.")]
		public float ManualCombatRating = -1f;

		[Tooltip("The same for damage. Also unread; the calculated value is what counts.")]
		public float ManualDamagePerSecond = -1f;

		[Tooltip("Damage per second, worked out from this weapon's own numbers.")]
		public float CalculatedDamagePerSecond;

		[Tooltip("How dangerous this weapon is, worked out from its stats. This is what feeds the ship's combat rating.")]
		public float CalculatedCombatRating;

		[Tooltip("Draws on an ammunition supply rather than only on energy.")]
		public bool UsesAmmo;

		[Tooltip("AI ships may treat this as an ordinary weapon and fire it at range.")]
		public bool AIConventionalWeapon = true;

		[Tooltip("Shapes how eagerly the AI fires this. Higher means sooner and more often.")]
		public float AiFireProbabilityPower = 0.5f;

		[Tooltip("Longest the AI will wait before firing this at a target.")]
		public float AIMaxTimeBeforeFire = 240f;

		[Tooltip("Shortest the AI will wait before firing this at a target.")]
		public float AIMinTimeBeforeFire = 5f;

		[Tooltip("How well this weapon covers the ship it is fitted to against incoming missiles.")]
		public float AIPointDefenseEffectiveness = 1f;

		[Tooltip("Orders this weapon in the turret list, so related weapons sit together.")]
		public TurretGroups DefaultTurretGroup;

		[Tooltip("Energy one shot costs.")]
		public float EnergyCost = 100f;

		[Tooltip("Extra energy drawn per second while this weapon is firing.")]
		public float EnergyCostPerSecond;

		[Tooltip("How much of the energy spent becomes damage. Shown as Efficiency in the info panel.")]
		public float EnergyToDamageConversion = 1f;

		[Tooltip("One-shot sound played as the weapon fires.")]
		public GameObject FireAudioPrefab;

		[Tooltip("Sound played while the weapon is charging up.")]
		public GameObject FireChargeAudioPrefab;

		[Tooltip("Looping sound played for as long as the weapon fires.")]
		public GameObject FireLoopAudioSource;

		[Tooltip("Delay between deciding to fire and the shot actually leaving the barrel.")]
		public float FirePrewarmTime;

		[Tooltip("Shapes how loosely this weapon groups its shots. Higher is tighter.")]
		public float InaccuracyPower = 2f;

		[Tooltip("How far this weapon can reach at all.")]
		public float MaxFiringRange = 100f;

		[Tooltip("Scales how far off target this weapon shoots. Higher is less accurate.")]
		public float InaccuracyMultiplier = 1f;

		[Tooltip("Smallest share of its damage a hit can do, so a graze still registers.")]
		public float MinAppliedDamage = 0.8f;

		[Tooltip("Closest this weapon can hit. Anything nearer is ignored.")]
		public float MinFiringRange;

		[Tooltip("Needs something to shoot at, and will not fire at empty space.")]
		public bool RequiresTarget = true;

		[Tooltip("AI ships will not use this weapon except after a delay.")]
		public bool RestrictAIUsage;

		[Tooltip("How long an auto-firing turret waits between range checks.")]
		public float AutoTurretFireCooldown;

		[Tooltip("Which kind of shield damage this does, for shields that resist one type and not another.")]
		public ShieldDamageType ShieldDamageType;

		[Tooltip("Optional model for the weapon. Leave empty and the component builds its own.")]
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
