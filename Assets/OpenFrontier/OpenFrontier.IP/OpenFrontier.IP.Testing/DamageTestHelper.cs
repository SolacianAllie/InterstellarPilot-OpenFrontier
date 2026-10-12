using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Testing
{
	public class DamageTestHelper
	{
		public static void SetRandomComponentHealth()
		{
			SetUnitRandomComponentHealth(EngineASX.Instance.PlayerUnit);
		}

		public static void SetUnitRandomComponentHealth(Unit unit)
		{
			foreach (ComponentBase unitComponent in unit.Components.UnitComponents)
			{
				unitComponent.HealthNormalized = 0.001f + Random.value;
			}
		}

		public static void DamagePlayerComponentsSeverely()
		{
			DamageUnitComponentsSeverely(EngineASX.Instance.PlayerUnit);
		}

		public static void DamageUnitComponentsSeverely(Unit unit)
		{
			foreach (ComponentBase unitComponent in unit.Components.UnitComponents)
			{
				unitComponent.HealthNormalized *= 0.1f;
			}
		}

		public static void RestoreComponentHealth(Unit unit)
		{
			foreach (ComponentBase unitComponent in unit.Components.UnitComponents)
			{
				unitComponent.HealthNormalized = 1f;
			}
		}

		public static void SetUnitRandomHullHealth(Unit unit)
		{
			foreach (ComponentBase unitComponent in unit.Components.UnitComponents)
			{
				unitComponent.HealthNormalized = Random.Range(0.01f, 1f);
			}
		}

		public static void RestorePlayerUnitHull()
		{
			RestoreHull(EngineASX.Instance.PlayerUnit);
		}

		public static void DamagePlayerUnitHullSeverely()
		{
			DamageUnitHullSeverely(EngineASX.Instance.PlayerUnit);
		}

		public static void DamageUnitHullSeverely(Unit unit)
		{
			unit.Destructable.HealthNormalized *= 0.1f;
		}

		public static void RestoreHull(Unit unit)
		{
			unit.Destructable.HealthNormalized = 1f;
		}

		public static void RestorePlayerUnitShieldAndHull()
		{
			FullyRestoreUnitHealth(EngineASX.Instance.PlayerUnit);
		}

		public static void RestorePlayerUnitComponentHealth()
		{
			RestoreComponentHealth(EngineASX.Instance.PlayerUnit);
		}

		public static void FullyRestoreUnitHealth(Unit unit)
		{
			unit.Destructable.HealthNormalized = 1f;
			if (unit.Components != null)
			{
				if (unit.Components.ShieldComponent != null)
				{
					unit.Components.ShieldComponent.RechargeAllFull();
				}
				unit.Components.RestoreComponentsHealth();
			}
		}

		public static void SetPlayerShieldRandomCharge()
		{
			SetUnitShieldRandomCharge(EngineASX.Instance.PlayerUnit);
		}

		public static void Deplete(Unit unit)
		{
			ShieldComponent shieldComponent = unit.Components.ShieldComponent;
			if (shieldComponent != null)
			{
				shieldComponent.DepleteAllShields();
			}
		}

		public static void SetUnitShieldRandomCharge(Unit unit)
		{
			ShieldComponent shieldComponent = unit.Components.ShieldComponent;
			for (int i = 0; i < 6; i++)
			{
				float value = Random.value;
				shieldComponent.SetNormalizedShieldPoints(i, value);
			}
		}

		public static void DamagePlayerShieldSeverely()
		{
			DamageUnitShieldSeverely(EngineASX.Instance.PlayerUnit);
		}

		public static void DamageUnitShieldSeverely(Unit unit)
		{
			ShieldComponent shieldComponent = unit.Components.ShieldComponent;
			for (int i = 0; i < 6; i++)
			{
				shieldComponent.SetShieldsPoints(i, shieldComponent.GetShieldPoints(i) * 0.1f);
			}
		}
	}
}
