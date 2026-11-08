using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Fleets.ActiveOrders;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Core
{
	public static class FleetExtensions
	{
		public static string GetLeaderPilotName(this Fleet fleet)
		{
			if (fleet.Leader != null)
			{
				return fleet.Leader.Person.GetNameAndRank();
			}
			return null;
		}

		public static Person GetLeaderPilot(this Fleet fleet)
		{
			if (fleet.Leader != null)
			{
				return fleet.Leader.Person;
			}
			return null;
		}

		public static bool HasOrderToJoinFleet(this Fleet fleet)
		{
			if (fleet.ActiveOrder != null && fleet.ActiveOrder is ActiveJoinFleetOrder)
			{
				return true;
			}
			foreach (FleetOrder item in fleet.OrderQueue)
			{
				if (item != null && item is JoinFleetOrder)
				{
					return true;
				}
			}
			return false;
		}

		public static bool IsEscortingFleet(this Fleet fleet, out Fleet escortedFleet)
		{
			escortedFleet = null;
			if (fleet.ActiveOrder != null && fleet.ActiveOrder is ActiveProtectOrder activeProtectOrder && activeProtectOrder.ProtectObjective.Target.TargetFleet != null)
			{
				escortedFleet = activeProtectOrder.ProtectObjective.Target.TargetFleet;
				return true;
			}
			if (fleet.OrderQueue.Count > 0 && fleet.HasQueuedOrderToProtectFleet(out escortedFleet))
			{
				return true;
			}
			return false;
		}

		public static bool HasQueuedOrderToProtectFleet(this Fleet fleet, out Fleet escortedFleet)
		{
			escortedFleet = null;
			foreach (FleetOrder item in fleet.OrderQueue)
			{
				if (item is ProtectOrder protectOrder && protectOrder.Target.TargetFleet != null)
				{
					escortedFleet = protectOrder.Target.TargetFleet;
					return true;
				}
			}
			return false;
		}

		public static bool IsPlayerFaction(this Fleet group)
		{
			if (group.Faction != null)
			{
				return group.Faction.IsPlayerFaction;
			}
			return false;
		}

		public static bool AreRepairsNeeded(this Fleet group, bool examineShields = true, bool examineComponents = true, float threshold = 1f)
		{
			foreach (NpcPilot npcPilot in group.NpcPilots)
			{
				if (npcPilot.CurrentUnit != null && npcPilot.CurrentUnit.RequiresRepair(examineShields, examineComponents, threshold))
				{
					return true;
				}
			}
			return false;
		}

		public static bool CanAllUnitsCloak(this Fleet group)
		{
			foreach (NpcPilot npcPilot in group.NpcPilots)
			{
				UnitComponentHolder currentUnitComponents = npcPilot.CurrentUnitComponents;
				if (currentUnitComponents.PilotNpc.CurrentUnitComponents.CloakComponent == null || currentUnitComponents.IsDocked)
				{
					return false;
				}
			}
			return true;
		}

		public static void CloakAllUnitsImmediate(this Fleet group)
		{
			foreach (NpcPilot npcPilot in group.NpcPilots)
			{
				UnitComponentHolder currentUnitComponents = npcPilot.CurrentUnitComponents;
				if (currentUnitComponents.PilotNpc.CurrentUnitComponents.CloakComponent != null)
				{
					currentUnitComponents.PilotNpc.CurrentUnitComponents.CloakComponent.State = CloakState.Cloaked;
				}
			}
		}

		public static bool AreAllUnitsCloakedOrDecloaking(this Fleet group)
		{
			foreach (NpcPilot npcPilot in group.NpcPilots)
			{
				UnitComponentHolder currentUnitComponents = npcPilot.CurrentUnitComponents;
				if (currentUnitComponents.CloakState == CloakState.Cloaked || currentUnitComponents.CloakState == CloakState.Cloaking)
				{
					return true;
				}
			}
			return false;
		}

		public static int GetUnitsSaleCost(this Fleet group)
		{
			int num = 0;
			foreach (NpcPilot npcPilot in group.NpcPilots)
			{
				if (npcPilot.CurrentUnit != null)
				{
					num += npcPilot.CurrentUnit.UnitClass.SaleCost;
				}
			}
			return num;
		}

		public static bool IsHostileTo(this Fleet fleet, Unit unit)
		{
			if (fleet.Faction != null)
			{
				return fleet.Faction.IsHostileTo(unit);
			}
			return false;
		}

		public static bool IsHostileTo(this Fleet fleet, Fleet otherFleet)
		{
			if (fleet.Faction != null)
			{
				return fleet.Faction.IsHostileTo(otherFleet);
			}
			return false;
		}

		public static bool NeedsRepair(this Fleet fleet, float hullConditionThreshold, float componentsConditionThreshold, float shieldConditionThreshold)
		{
			foreach (UnitComponentHolder ship in fleet.Ships)
			{
				if (ship != null && ActiveWaitForAutoRepairOrder.DoesUnitNeedRepair(ship.Unit, hullConditionThreshold, componentsConditionThreshold, shieldConditionThreshold))
				{
					return true;
				}
			}
			return false;
		}

		public static void DumpTradeableCargoOfType(this Fleet fleet, CargoClass cargoClass)
		{
			bool flag = false;
			foreach (UnitComponentHolder ship in fleet.Ships)
			{
				if (!(ship != null) || !(ship.CargoBayComponent != null))
				{
					continue;
				}
				int cargoCountOf = ship.GetCargoCountOf(cargoClass);
				if (cargoCountOf > 0)
				{
					flag = true;
					if (fleet.Faction != null)
					{
						int creditsValue = Mathf.RoundToInt((float)(cargoCountOf * cargoClass.BasePrice) * 0.8f);
						fleet.Faction.ApplyTransaction(creditsValue, FactionTransactionType.Trade, null, ship.Unit, cargoClass, null, FactionTransactionTaxType.None, cargoCountOf);
					}
					ship.CargoBayComponent.SetCount(cargoClass, 0);
				}
			}
			if (flag)
			{
				EngineASX.Instance.DebugInfo.NumTimesTradableCargoDumped++;
			}
		}

		public static void DumpAllIncompatibleAmmo(this Fleet fleet)
		{
			bool flag = false;
			List<CargoClass> list = new List<CargoClass>(4);
			foreach (UnitComponentHolder ship in fleet.Ships)
			{
				if (!(ship != null) || !(ship.CargoBayComponent != null))
				{
					continue;
				}
				list.Clear();
				foreach (KeyValuePair<CargoClass, int> cargo in ship.CargoBayComponent.Cargos)
				{
					CargoClass key = cargo.Key;
					if (cargo.Key.IsEquipment && cargo.Value > 0 && ship.PilotNpc != null && !ship.PilotNpc.IsCargoClassCompatible(key))
					{
						flag = true;
						if (fleet.Faction != null)
						{
							int creditsValue = Mathf.RoundToInt((float)(cargo.Value * key.BasePrice) * 0.8f);
							fleet.Faction.ApplyTransaction(creditsValue, FactionTransactionType.Trade, null, ship.Unit, key, null, FactionTransactionTaxType.None, cargo.Value);
						}
						list.Add(key);
					}
				}
				foreach (CargoClass item in list)
				{
					ship.CargoBayComponent.SetCount(item, 0);
				}
			}
			if (flag)
			{
				EngineASX.Instance.DebugInfo.NumTimesFleetDumpedIncompatibleAmmo++;
			}
		}

		public static void DumpAllTradeableCargo(this Fleet fleet)
		{
			bool flag = false;
			List<CargoClass> list = new List<CargoClass>(4);
			foreach (UnitComponentHolder ship in fleet.Ships)
			{
				if (!(ship != null) || !(ship.CargoBayComponent != null))
				{
					continue;
				}
				list.Clear();
				foreach (KeyValuePair<CargoClass, int> cargo in ship.CargoBayComponent.Cargos)
				{
					CargoClass key = cargo.Key;
					if (cargo.Key.IsTraded && cargo.Value > 0)
					{
						flag = true;
						if (fleet.Faction != null)
						{
							int creditsValue = Mathf.RoundToInt((float)(cargo.Value * key.BasePrice) * 0.8f);
							fleet.Faction.ApplyTransaction(creditsValue, FactionTransactionType.Trade, null, ship.Unit, key, null, FactionTransactionTaxType.None, cargo.Value);
						}
						list.Add(key);
					}
				}
				foreach (CargoClass item in list)
				{
					ship.CargoBayComponent.SetCount(item, 0);
				}
			}
			if (flag)
			{
				EngineASX.Instance.DebugInfo.NumTimesTradableCargoDumped++;
			}
		}

		public static bool IsPlayerAutoPilotFleet(this Fleet fleet)
		{
			Unit localUnit = EngineASX.Instance.LocalUnit;
			if (localUnit != null && fleet != null)
			{
				Person pilot = localUnit.GetPilot();
				if (pilot.NpcPilot != null)
				{
					return pilot.NpcPilot.Fleet == fleet;
				}
				return false;
			}
			return false;
		}

		public static int GetCargoCountOf(this Fleet fleet, CargoClass cargoClass)
		{
			int num = 0;
			foreach (UnitComponentHolder ship in fleet.Ships)
			{
				num += ship.GetCargoCountOf(cargoClass);
			}
			return num;
		}

		public static bool HasBounty(this Fleet fleet)
		{
			foreach (UnitComponentHolder ship in fleet.Ships)
			{
				if (ship.PilotPerson.HasBounty())
				{
					return true;
				}
			}
			return false;
		}
	}
}
