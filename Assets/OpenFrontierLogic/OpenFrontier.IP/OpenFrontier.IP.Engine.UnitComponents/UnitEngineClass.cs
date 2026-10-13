using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class UnitEngineClass : ComponentClass
	{
		private const float costPower = 3.3f;

		private const float costPowerMinRef = 5f;

		private const float costPricePerUnitOfPower = 1200f;

		private const float costMin = 750f;

		[Tooltip("Thrust this engine produces.")]
		public float EnginePower;

		[Tooltip("Energy drawn per second while these thrusters are online.")]
		public float PowerDraw;

		[Tooltip("Ship mass these thrusters are tuned for. Heavier than this and they are sluggish.")]
		public float OptimalMass;

		[Tooltip("Heaviest ship these thrusters can still move. Past this they cannot get the ship out of a slow crawl.")]
		public float MaximumMass;

		[Tooltip("Heat per second this thruster makes just by running. Small next to a thermal weapon, but a ship under full burn for a long time still has to manage something. Zero on every legacy engine, which is why fitting one changes nothing until there is a heat system.")]
		public float ThermalLoad;

		[Tooltip("Whether this thruster can be boosted. Booster thrusters can; ordinary ones cannot.")]
		public bool CanBoost;

		[Tooltip("How much harder the thrusters push while boosting. 2 means double, which is what the Elite-style boosters use.")]
		public float BoostPowerMultiplier = 2f;

		[Tooltip("Seconds a boost lasts once it is triggered.")]
		public float BoostDuration;

		public static float CalculateMaxSpeed(float enginePower, float drag, float mass)
		{
			return enginePower / drag / mass;
		}

		public override string GetFriendlyName()
		{
			return $"{MinHullType} Engine {EnginePower:0.0}";
		}

		public float GetMaxEnergyUsage(GameSettings gameSettings)
		{
			return EnginePower * gameSettings.EnginePowerToEnergyUsage;
		}

		public float GetCurrentMaxSpeed(Unit unit)
		{
			return EnginePower / unit.UnitClass.Drag / unit.Mass * unit.Components.EnginesPowerMultiplier;
		}

		public float GetDefaultMaxSpeed(UnitClass unitClass)
		{
			return EnginePower / unitClass.Drag / unitClass.UnitPrefab.Mass;
		}

		public float CalculateMaxSpeed(Rigidbody r)
		{
			if (r != null)
			{
				return CalculateMaxSpeed(EnginePower, r.linearDamping, r.mass);
			}
			return 0f;
		}

		public float CalculateMaxSpeed(float drag, float mass)
		{
			return CalculateMaxSpeed(EnginePower, drag, mass);
		}

		protected override ComponentBase createComponent(GameObject target)
		{
			UnitEngineComponent unitEngineComponent = target.AddComponent<UnitEngineComponent>();
			unitEngineComponent.EngineClass = this;
			return unitEngineComponent;
		}

		protected override int CalculateCost()
		{
			float num = EnginePower * 0.5f;
			float num2 = (num - 5f) * 1200f;
			float num3 = Mathf.Pow(num - 5f, 3.3f);
			return (int)MinHullType * 400 + Mathf.CeilToInt((750f + num2 + num3) / 25f) * 25;
		}
	}
}
