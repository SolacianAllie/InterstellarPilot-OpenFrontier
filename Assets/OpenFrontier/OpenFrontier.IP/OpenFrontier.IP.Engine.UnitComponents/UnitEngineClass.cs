using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class UnitEngineClass : ComponentClass
	{
		private const float costPower = 3.3f;

		private const float costPowerMinRef = 5f;

		private const float costPricePerUnitOfPower = 1200f;

		private const float costMin = 750f;

		public float EnginePower;

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
			return EnginePower / unit.UnitClass.Drag / unit.Mass;
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
