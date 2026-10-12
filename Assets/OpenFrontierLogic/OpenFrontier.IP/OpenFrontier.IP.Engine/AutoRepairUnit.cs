using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class AutoRepairUnit : MonoBehaviour
	{
		private Unit unit;

		private float nextCheckRequiresRepair;

		private bool requiresRepair;

		public void Init(Unit unit)
		{
			this.unit = unit;
		}

		public void Tick(float elapsedTime)
		{
			if (requiresRepair)
			{
				AutoRepair(elapsedTime);
			}
			else if (Time.time > nextCheckRequiresRepair)
			{
				requiresRepair = unit.Destructable.IsHullDamaged || unit.Components.AnyComponentDamaged();
				nextCheckRequiresRepair = Time.time + 10f;
			}
		}

		private void AutoRepair(float deltaTime)
		{
			float currentHealth = unit.Destructable.CurrentHealth;
			bool flag = false;
			if (currentHealth < unit.UnitClass.maxHealth)
			{
				unit.Destructable.CurrentHealth += unit.Engine.GameSettings.AutoRepairSettings.AutoRepairHealthPointsRestored * deltaTime;
				flag = true;
				if (currentHealth > unit.UnitClass.maxHealth)
				{
					unit.Destructable.CurrentHealth = unit.UnitClass.maxHealth;
				}
			}
			else if (unit.Components.UnitComponents != null)
			{
				float num = unit.Engine.GameSettings.AutoRepairSettings.AutoRepairComponentHealthPointsRestored * deltaTime;
				foreach (ComponentBase unitComponent in unit.Components.UnitComponents)
				{
					if (unitComponent.HealthNormalized < 1f)
					{
						unitComponent.HealthPoints += num;
						flag = true;
					}
				}
			}
			if (!flag)
			{
				requiresRepair = false;
			}
		}
	}
}
