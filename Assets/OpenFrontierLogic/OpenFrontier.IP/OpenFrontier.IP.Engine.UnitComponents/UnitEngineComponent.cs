using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class UnitEngineComponent : ComponentBase
	{
		public UnitEngineClass EngineClass;

		private Rigidbody unitRigidBody;

		public override ComponentClass ComponentClass => EngineClass;

		public float EngineThrottle
		{
			get
			{
				return EnergySupply;
			}
			set
			{
				EnergySupply = value;
			}
		}

		public void CalculateEnergy(float deltaTime, out float actualEnergyApplied, out float actualEnergyUsed)
		{
			actualEnergyApplied = 0f;
			actualEnergyUsed = 0f;
			float num = EnergySupply;
			if (!(num <= 0f))
			{
				float maxEnergyUsage = EngineClass.GetMaxEnergyUsage(Engine.GameSettings);
				float normalizedDamage = NormalizedDamage;
				float num2 = EngineClass.EnginePower;
				if (normalizedDamage > 0f)
				{
					num2 *= 1f - normalizedDamage * Engine.GameSettings.ComponentDamageEffectSettings.DamagedEngineMaxPowerDrawEffect;
				}
				float a = num * maxEnergyUsage * deltaTime;
				actualEnergyUsed = Mathf.Min(a, UnitComponents.CapacitorCharge);
				actualEnergyApplied = actualEnergyUsed / maxEnergyUsage * num2;
				if (normalizedDamage > 0f)
				{
					actualEnergyApplied *= 1f - normalizedDamage * Engine.GameSettings.ComponentDamageEffectSettings.DamagedEngineWastedEnergyEffect;
				}
			}
		}

		public float GetCurrentMaxSpeedConsideringDamage(Unit unit)
		{
			float normalizedDamage = NormalizedDamage;
			float num = EngineClass.EnginePower;
			if (normalizedDamage > 0f)
			{
				num *= 1f - normalizedDamage * Engine.GameSettings.ComponentDamageEffectSettings.DamagedEngineMaxPowerDrawEffect;
			}
			return num / unit.UnitClass.Drag / unit.Mass;
		}

		public float CalculateRelativeForceAndReduceCapacitor(bool reduceChargeEnergy, float deltaTime)
		{
			CalculateEnergy(deltaTime, out var actualEnergyApplied, out var actualEnergyUsed);
			if (reduceChargeEnergy)
			{
				UnitComponents.CapacitorCharge -= actualEnergyUsed;
			}
			return actualEnergyApplied;
		}

		protected override void onAttachedToUnit(UnitComponentHolder unit)
		{
			base.onAttachedToUnit(unit);
			unit.EngineComponent = this;
			UpdateUnitRigidBody();
			EnergySupply = 0f;
			if (UnitComponents != null)
			{
				UnitComponents.UpdateMaxSpeed();
			}
		}

		protected override void onDetachedFromUnit(UnitComponentHolder unit)
		{
			if (UnitComponents != null)
			{
				UnitComponents.UpdateMaxSpeed();
			}
			base.onDetachedFromUnit(unit);
			unit.EngineComponent = null;
			UpdateUnitRigidBody();
		}

		protected override void onUnitActive()
		{
			base.onUnitActive();
			UpdateUnitRigidBody();
		}

		protected override void onUnitInactive()
		{
			base.onUnitInactive();
			UpdateUnitRigidBody();
		}

		public void ApplyForce()
		{
			float num = CalculateRelativeForceAndReduceCapacitor(reduceChargeEnergy: true, Time.deltaTime);
			if (num != 0f)
			{
				unitRigidBody.AddRelativeForce(Vector3.forward * num, ForceMode.Impulse);
			}
		}

		private void UpdateUnitRigidBody()
		{
			unitRigidBody = null;
			if (Unit != null)
			{
				unitRigidBody = Unit.GetComponent<Rigidbody>();
			}
		}

		public override bool RequiresRecharge()
		{
			return false;
		}
	}
}
