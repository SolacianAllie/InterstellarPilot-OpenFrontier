using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public static class FactionAIRepairer
	{
		public static bool EvaluateFleetNeedsRepair(Faction faction, Fleet fleet)
		{
			if (!fleet.IsHomeBaseValid)
			{
				return false;
			}
			if (fleet.Sector == fleet.HomeSector && Vector3.Distance(fleet.SectorPosition, fleet.HomeSectorPosition) < 1500f)
			{
				return false;
			}
			float num = 0f;
			if (fleet.ActiveOrder != null)
			{
				num = fleet.ActiveOrder.FleetOrder.Priority;
			}
			if (!faction.FactionAI.DoesFleetHaveRepairOrder(fleet))
			{
				float num2 = GameController.Instance.GameSettings.GameplaySettings.MinDamageBeforeNpcShipNeedsRepair + faction.Aggression * 0.1f + faction.AISettings.OffensiveStance * 0.1f + num * 0.2f;
				foreach (NpcPilot npcPilot in fleet.NpcPilots)
				{
					Unit currentUnit = npcPilot.CurrentUnit;
					if (currentUnit != null && currentUnit.IsValidAndNotDestroyed && GetTotalNormalizedUnitDamage(currentUnit) > num2)
					{
						return true;
					}
				}
			}
			return false;
		}

		public static float GetTotalNormalizedUnitDamage(Unit unit)
		{
			if (unit.Components.ShieldComponent != null)
			{
				return ((1f - unit.Components.ShieldComponent.GetNormalizedShieldCharge()) * 2f + unit.Destructable.NormalizedDamage * 8f) / 10f;
			}
			return unit.Destructable.NormalizedDamage;
		}

		public static bool RepairFleet(Fleet fleet, FactionAIBase factionAI, bool forceWaitForRepair = false)
		{
			if (!fleet.IsHomeBaseValid)
			{
				return false;
			}
			fleet.ClearOrders();
			OrderReturnToBase(fleet);
			if (!forceWaitForRepair && HasEnoughCreditsToAttemptRepairs(factionAI.Faction))
			{
				OrderRepair(fleet);
				EngineASX.Instance.DebugInfo.NumTimesFleetSendForRepairs++;
			}
			else
			{
				OrderWaitForAutoRepair(fleet);
				EngineASX.Instance.DebugInfo.NumTimesFleetSendForWaitForRepair++;
			}
			fleet.AssignNextOrder();
			factionAI.OnFleetOrdered(fleet);
			return true;
		}

		private static void OrderRepair(Fleet fleet)
		{
			RepairAtNearestStationOrder repairAtNearestStationOrder = UnityObjectHelper.NewGameObject<RepairAtNearestStationOrder>();
			repairAtNearestStationOrder.InsufficientCreditsMode = ((fleet.Faction.Units.Count == 1) ? InsufficientCreditsMode.Abort : InsufficientCreditsMode.Wait);
			repairAtNearestStationOrder.AllowCombatInterception = false;
			repairAtNearestStationOrder.PreferCloak = FleetOrderCloakPreference.OnlyIfAllCanCloak;
			repairAtNearestStationOrder.AllowTimeout = true;
			repairAtNearestStationOrder.MaxDuration = 800f;
			repairAtNearestStationOrder.Priority = 0.8f;
			fleet.EnqueueOrder(repairAtNearestStationOrder);
			EngineASX.Instance.DebugInfo.NumTimesFleetSendForRepairs++;
		}

		public static bool HasEnoughCreditsToAttemptRepairs(Faction faction)
		{
			return faction.Credits - faction.CreditsReserve >= faction.AISettings.RepairMinCreditsBeforeRepair;
		}

		public static void OrderWaitForAutoRepair(Fleet fleet)
		{
			WaitForAutoRepairOrder waitForAutoRepairOrder = UnityObjectHelper.NewGameObject<WaitForAutoRepairOrder>();
			waitForAutoRepairOrder.AllowCombatInterception = true;
			waitForAutoRepairOrder.PreferCloak = FleetOrderCloakPreference.OnlyIfAllCanCloak;
			waitForAutoRepairOrder.Priority = 0.8f;
			float hullConditionThreshold = 1f - fleet.Faction.FactionAI.AISettings.OffensiveStance * 0.2f;
			waitForAutoRepairOrder.ComponentsConditionThreshold = (waitForAutoRepairOrder.HullConditionThreshold = hullConditionThreshold);
			waitForAutoRepairOrder.ShieldConditionThreshold = 0.95f - fleet.Faction.FactionAI.AISettings.OffensiveStance * 0.4f;
			fleet.EnqueueOrder(waitForAutoRepairOrder);
		}

		public static void OrderReturnToBase(Fleet fleet)
		{
			ReturnToBaseOrder returnToBaseOrder = UnityObjectHelper.NewGameObject<ReturnToBaseOrder>();
			returnToBaseOrder.AllowCombatInterception = false;
			returnToBaseOrder.PreferCloak = FleetOrderCloakPreference.OnlyIfAllCanCloak;
			returnToBaseOrder.Priority = 0.85f;
			fleet.EnqueueOrder(returnToBaseOrder);
		}
	}
}
