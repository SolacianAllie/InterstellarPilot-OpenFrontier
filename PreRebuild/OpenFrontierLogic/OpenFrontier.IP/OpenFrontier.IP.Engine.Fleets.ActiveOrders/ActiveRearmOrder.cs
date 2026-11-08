using System.Collections.Generic;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Fleets.ActiveOrders
{
	public class ActiveRearmOrder : ActiveFleetOrder
	{
		protected bool rearmHasInsufficientCredits;

		private Unit currentRearmLocation;

		public RearmOrder RearmOrder;

		private ActiveRearmFleetOrderState state;

		private const float attemptRearmInterval = 3f;

		private float nextAttemptRearmTime;

		private int totalSpendOnRearming;

		public Unit CurrentRearmLocation
		{
			get
			{
				return currentRearmLocation;
			}
			set
			{
				if (currentRearmLocation != value)
				{
					currentRearmLocation = value;
					if (currentRearmLocation != null)
					{
						ResetTimeoutTime();
					}
					ResetTargetPosition();
				}
			}
		}

		public ActiveRearmFleetOrderState State
		{
			get
			{
				return state;
			}
			set
			{
				if (state != value)
				{
					state = value;
					switch (state)
					{
					case ActiveRearmFleetOrderState.MoveToStation:
						IsIdle = false;
						break;
					case ActiveRearmFleetOrderState.None:
						OnIdle();
						break;
					case ActiveRearmFleetOrderState.Rearming:
						OnIdle();
						break;
					}
					rearmHasInsufficientCredits = false;
				}
			}
		}

		protected override void onInit()
		{
			base.onInit();
		}

		protected override void tick(float elapsedTime)
		{
			switch (state)
			{
			case ActiveRearmFleetOrderState.None:
				if (currentRearmLocation != null && IsRearmLocationValid(currentRearmLocation))
				{
					State = ActiveRearmFleetOrderState.MoveToStation;
				}
				else
				{
					OnRearmLocationNullOrInvalid();
				}
				break;
			case ActiveRearmFleetOrderState.MoveToStation:
				if (currentRearmLocation == null || !IsRearmLocationValid(currentRearmLocation))
				{
					OnRearmLocationNullOrInvalid();
				}
				else if (fleet.AllUnitsAtDock(currentRearmLocation))
				{
					State = ActiveRearmFleetOrderState.Rearming;
				}
				break;
			case ActiveRearmFleetOrderState.Rearming:
				if (currentRearmLocation != null && IsRearmLocationValid(currentRearmLocation))
				{
					if (Time.time > nextAttemptRearmTime)
					{
						AttemptRearm();
						nextAttemptRearmTime = Time.time + 3f;
					}
				}
				else
				{
					OnRearmLocationNullOrInvalid();
				}
				break;
			}
		}

		private void AttemptRearm()
		{
			if (!fleet.AllUnitsAtDock(currentRearmLocation))
			{
				return;
			}
			int num = 0;
			for (int i = 0; i < fleet.NpcPilots.Count; i++)
			{
				NpcPilot npcPilot = fleet.NpcPilots[i];
				Unit currentUnit = npcPilot.CurrentUnit;
				if (!(currentUnit != null))
				{
					continue;
				}
				float desiredEquipmentUsage = GetDesiredEquipmentUsage01(currentUnit);
				if (desiredEquipmentUsage <= 0f)
				{
					continue;
				}
				CargoBayComponent cargoBayComponent = npcPilot.CurrentUnit.CargoBayComponent;
				float num2 = Mathf.Min(cargoBayComponent.FreeSpace, desiredEquipmentUsage * cargoBayComponent.Capacity - currentUnit.CargoBayComponent.EquipmentLoad);
				if (num2 <= 1f)
				{
					continue;
				}
				RearmHelper.PopulateRearmItems(currentUnit, num2);
				if (RearmHelper.RearmItemsQueue.Count == 0)
				{
					continue;
				}
				RearmItem value = RearmHelper.RearmItemsQueue.Dequeue().Value;
				int? costToRearmUnit = RearmHelper.GetCostToRearmUnit(currentUnit, currentRearmLocation, value);
				if (!costToRearmUnit.HasValue)
				{
					continue;
				}
				num++;
				if (IsAffordableConsideringReserve(costToRearmUnit.Value))
				{
					RearmHelper.PerformRearmAndApplyTransaction(currentUnit, currentRearmLocation, value, costToRearmUnit.Value, CreditsSource);
					fleet.InvalidateCargoUsageStats();
					totalSpendOnRearming += costToRearmUnit.Value;
					continue;
				}
				rearmHasInsufficientCredits = true;
				if (RearmOrder.InsufficientCreditsMode == InsufficientCreditsMode.Abort)
				{
					OnInvalid();
					return;
				}
			}
			if (num == 0)
			{
				OnComplete();
			}
		}

		private float GetDesiredEquipmentUsage01(Unit unit)
		{
			return GetDesiredEquipmentUsage01(RearmOrder.EquipmentUsage);
		}

		public static float GetDesiredEquipmentUsage01(float usage)
		{
			return usage;
		}

		protected virtual void OnRearmLocationNullOrInvalid()
		{
			CurrentRearmLocation = null;
			State = ActiveRearmFleetOrderState.None;
		}

		protected override void resetTargetPosition()
		{
			base.resetTargetPosition();
			if (currentRearmLocation != null && IsRearmLocationValid(currentRearmLocation))
			{
				fleet.SetTargetToDock(this, currentRearmLocation);
			}
		}

		public bool IsRearmLocationValid(Unit locationUnit)
		{
			return IsRearmLocationValid(locationUnit, fleet, fleet.NpcShipHullTypes, fleet.Faction);
		}

		public static bool IsRearmLocationValid(Unit unit, Fleet localFleet, List<ShipHullType> localFleetHullTypes, Faction localFaction)
		{
			if (unit != null && unit.IsDockable && unit.IsEquipmentTrader())
			{
				if (localFleet != null)
				{
					Fleet fleet = unit.GetFleet();
					if (fleet != null && fleet == localFleet)
					{
						return false;
					}
				}
				if (HasGoodRelationsWithRearmLocation(unit, localFaction))
				{
					if (localFleetHullTypes != null)
					{
						return unit.GetHangar().CanUnitsFitInHangarIgnoreOccupancy(localFleetHullTypes);
					}
					return true;
				}
			}
			return false;
		}

		public static bool HasGoodRelationsWithRearmLocation(Unit unit, Faction localFaction)
		{
			if (unit.Faction == localFaction)
			{
				return true;
			}
			if (unit.IsHostileToOrAlwaysHostileToTwoWay(localFaction))
			{
				return false;
			}
			if (unit.Faction.GetOpinion(localFaction) > -0.2f)
			{
				return true;
			}
			return false;
		}

		private bool HasGoodRelationsWithRearmLocation(Unit unit)
		{
			return HasGoodRelationsWithRearmLocation(unit, fleet.Faction);
		}

		public override DockedPreference GetPreferToDockWhenIdle()
		{
			if (state == ActiveRearmFleetOrderState.Rearming)
			{
				return DockedPreference.Dock;
			}
			return base.GetPreferToDockWhenIdle();
		}

		public override bool RequestUndock(NpcPilot npcPilot)
		{
			if (state == ActiveRearmFleetOrderState.Rearming)
			{
				return false;
			}
			return true;
		}
	}
}
