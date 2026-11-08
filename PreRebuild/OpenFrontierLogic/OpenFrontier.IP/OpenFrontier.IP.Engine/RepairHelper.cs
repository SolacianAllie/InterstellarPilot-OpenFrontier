using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public static class RepairHelper
	{
		public static int GetAllComponentsRepairCost(Faction repairerFaction, Faction repairedFaction, Unit repairingUnit, Unit unit)
		{
			int num = 0;
			if (unit.Components != null)
			{
				foreach (ComponentBase unitComponent in unit.Components.UnitComponents)
				{
					num += GetComponentRepairCost(repairerFaction, repairedFaction, repairingUnit, unitComponent);
				}
			}
			return num;
		}

		public static int GetComponentRepairCost(Faction repairerFaction, Faction repairedFaction, Unit repairingUnit, ComponentBase component)
		{
			float num = (float)component.RepairCost * repairingUnit.UnitClass.RepairFacilitiesCostMultiplier;
			if (repairerFaction == repairedFaction)
			{
				num *= repairingUnit.Engine.GameSettings.OwnedStationShipRepairMultiplier;
			}
			if (num > 0f)
			{
				return ApplyMarkup(repairerFaction, repairedFaction, num);
			}
			return 0;
		}

		public static int GetHullRepairCost(Faction repairerFaction, Faction repairedFaction, Unit repairingUnit, Unit repairedUnit)
		{
			float num = (float)repairedUnit.Destructable.HullRepairCost * repairingUnit.UnitClass.RepairFacilitiesCostMultiplier;
			if (repairerFaction == repairedFaction)
			{
				num *= repairedUnit.Engine.GameSettings.OwnedStationShipRepairMultiplier;
			}
			if (num > 0f)
			{
				return ApplyMarkup(repairerFaction, repairedFaction, num);
			}
			return 0;
		}

		public static int GetCostToRechargeShield(Faction repairerFaction, Faction repairedFaction, Unit repairingUnit, Unit unit)
		{
			if (unit.Components != null && unit.Components.ShieldComponent != null)
			{
				float totalDepletedShieldPoints = unit.Components.ShieldComponent.GetTotalDepletedShieldPoints();
				float num = GetCostToRechargeShield(repairerFaction, repairedFaction, totalDepletedShieldPoints);
				num *= repairingUnit.UnitClass.RepairFacilitiesCostMultiplier;
				if (repairerFaction == repairedFaction)
				{
					num *= unit.Engine.GameSettings.OwnedStationShipRepairMultiplier;
				}
				return Mathf.RoundToInt(num);
			}
			return 0;
		}

		public static int ApplyMarkup(Faction repairerFaction, Faction repairedFaction, float cost)
		{
			if (repairerFaction != null && repairerFaction != repairedFaction)
			{
				return repairerFaction.GetMarkedUpPriceAfterOpinionChange(TradeType.Sell, cost, repairedFaction);
			}
			return Mathf.RoundToInt(cost);
		}

		public static int GetCostToRechargeShield(Faction repairerFaction, Faction repairedFaction, float damageShieldPoints)
		{
			float num = repairerFaction.Engine.GameSettings.CostToRepairShieldPoint * damageShieldPoints;
			if (repairerFaction == repairedFaction)
			{
				num *= repairerFaction.Engine.GameSettings.OwnedStationShipRepairMultiplier;
			}
			return ApplyMarkup(repairerFaction, repairedFaction, num);
		}

		public static int GetCostToRepairAllDamage(Faction repairerFaction, Faction repairedFaction, Unit repairingUnit, Unit repairedUnit)
		{
			int costToRechargeShield = GetCostToRechargeShield(repairerFaction, repairedFaction, repairingUnit, repairedUnit);
			int allComponentsRepairCost = GetAllComponentsRepairCost(repairerFaction, repairedFaction, repairingUnit, repairedUnit);
			int hullRepairCost = GetHullRepairCost(repairerFaction, repairedFaction, repairingUnit, repairedUnit);
			return costToRechargeShield + allComponentsRepairCost + hullRepairCost;
		}

		public static void RepairAll(Faction repairerFaction, Faction repairedFaction, Unit repairedUnit, Unit location)
		{
			int costToRepairAllDamage = GetCostToRepairAllDamage(repairerFaction, repairedFaction, location, repairedUnit);
			repairedUnit.Destructable.RestoreHealth();
			foreach (ComponentBase unitComponent in repairedUnit.Components.UnitComponents)
			{
				unitComponent.RestoreHealth();
			}
			repairedUnit.Components.ShieldComponent.RechargeFull();
			repairedFaction.RegisterTaxedTradeWithFaction(location, repairerFaction, -costToRepairAllDamage, FactionTransactionType.ShipRepairs);
		}
	}
}
