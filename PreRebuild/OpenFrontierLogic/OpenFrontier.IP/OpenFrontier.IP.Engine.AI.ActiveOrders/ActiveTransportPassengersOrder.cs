using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveTransportPassengersOrder : ActiveFleetOrder
	{
		public AutonomousTransportPassengersOrder AutonomousTransportPassengersObjective;

		private const float searchScoreRandomness = 1f;

		private const float searchJumpDistanceScore = 10f;

		private const float searchDistanceScore = 7f;

		private const float searchPerPassengerScore = 5f;

		private ActiveTransportPassengerOrderState currentState;

		private double endBuySellTime;

		private double lastStateChangeTime;

		private float nextSearchTradeRoutes;

		private float timeBeforeSearchTradeRoutes = 10f;

		public ActiveTransportPassengersOrder TransportObjective;

		private PassengerGroup passengerGroup;

		public override bool IsValid => true;

		public ActiveTransportPassengerOrderState CurrentState
		{
			get
			{
				return currentState;
			}
			set
			{
				if (currentState != value)
				{
					lastStateChangeTime = Engine.ScenarioElapsedTime;
					currentState = value;
					OnStateChanged();
				}
			}
		}

		public PassengerGroup PassengerGroup
		{
			get
			{
				return passengerGroup;
			}
			set
			{
				if (passengerGroup != value)
				{
					passengerGroup = value;
					if (passengerGroup == null)
					{
						CurrentState = ActiveTransportPassengerOrderState.None;
					}
				}
			}
		}

		public double LastStateChangeTime
		{
			get
			{
				return lastStateChangeTime;
			}
			set
			{
				lastStateChangeTime = value;
			}
		}

		public double EndBuySellTime
		{
			get
			{
				return endBuySellTime;
			}
			set
			{
				endBuySellTime = value;
			}
		}

		protected override string GetStatusTextInternal(Faction localFaction)
		{
			switch (currentState)
			{
			case ActiveTransportPassengerOrderState.None:
				return "Finding passengers";
			case ActiveTransportPassengerOrderState.GoPickup:
			case ActiveTransportPassengerOrderState.PickingUp:
				if (passengerGroup.CurrentUnit != null)
				{
					return string.Format("Picking up {0} {1} from {2}", passengerGroup.PassengerCount, Helper.Pluralise("passenger", passengerGroup.PassengerCount), UnitNamer.GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(localFaction, fleet.Sector, passengerGroup.CurrentUnit));
				}
				break;
			case ActiveTransportPassengerOrderState.PostDeliver:
				return "Disembarking";
			case ActiveTransportPassengerOrderState.Embarking:
				return "Embarking";
			case ActiveTransportPassengerOrderState.Transporting:
			case ActiveTransportPassengerOrderState.Disembarking:
				if (passengerGroup.Destination != null)
				{
					return string.Format("Dropping off {0} {1} at {2}", passengerGroup.PassengerCount, Helper.Pluralise("passenger", passengerGroup.PassengerCount), UnitNamer.GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(localFaction, fleet.Sector, passengerGroup.Destination));
				}
				break;
			}
			return base.GetStatusTextInternal(localFaction);
		}

		public bool DisembarkPassengersIfAble()
		{
			Unit dockUnit = fleet.Leader.CurrentUnit.Components.DockUnit;
			Faction faction = fleet.Faction;
			Unit currentUnit = passengerGroup.CurrentUnit;
			if (CanDisembarkPassengers(faction, passengerGroup, dockUnit))
			{
				DisembarkPassengersOnUnit(faction, currentUnit, passengerGroup, dockUnit);
				return true;
			}
			return false;
		}

		private void DisembarkPassengersOnUnit(Faction ourFaction, Unit localUnit, PassengerGroup passengerGroup, Unit dockUnit)
		{
			DisembarkPassengers(ourFaction, localUnit, passengerGroup, dockUnit);
		}

		public static void DisembarkPassengers(Faction ourFaction, Unit localUnit, PassengerGroup passengerGroup, Unit dockUnit)
		{
			if (dockUnit != null)
			{
				int? cachedRevenueOrCalculate = passengerGroup.GetCachedRevenueOrCalculate();
				if (cachedRevenueOrCalculate.HasValue)
				{
					ourFaction.RegisterTaxedTradeWithFaction(dockUnit, dockUnit.Faction, cachedRevenueOrCalculate.Value, FactionTransactionType.PassengerFare, null, null, passengerGroup.PassengerCount);
				}
				passengerGroup.SafeDestroy();
			}
			else
			{
				Debug.LogError("No dockUnit", localUnit);
			}
		}

		public static bool CanDisembarkPassengers(Faction ourFaction, PassengerGroup passengerGroup, Unit dockUnit)
		{
			return true;
		}

		public void ValidateAndClearIfInvalid()
		{
			if (!Validate())
			{
				CurrentState = ActiveTransportPassengerOrderState.None;
				nextSearchTradeRoutes = Time.time + 2f;
			}
		}

		public bool Validate()
		{
			ActiveTransportPassengerOrderState activeTransportPassengerOrderState = currentState;
			if ((uint)(activeTransportPassengerOrderState - 1) <= 3u && (passengerGroup == null || !passengerGroup.IsValid))
			{
				return false;
			}
			return ValidatePassengerGroup();
		}

		private bool ValidatePassengerGroup()
		{
			if (passengerGroup != null)
			{
				switch (currentState)
				{
				case ActiveTransportPassengerOrderState.GoPickup:
				case ActiveTransportPassengerOrderState.PickingUp:
					if (PickupLocationIsInvalid() || DropoffLocationIsInvalid())
					{
						return false;
					}
					break;
				case ActiveTransportPassengerOrderState.Transporting:
				case ActiveTransportPassengerOrderState.Disembarking:
					if (DropoffLocationIsInvalid())
					{
						return false;
					}
					break;
				}
			}
			return true;
		}

		private bool PickupLocationIsInvalid()
		{
			if (!LocationIsInvalid(passengerGroup.CurrentUnit))
			{
				return passengerGroup.CurrentUnit.UnitType == UnitType.Ship;
			}
			return true;
		}

		private bool DropoffLocationIsInvalid()
		{
			return LocationIsInvalid(passengerGroup.Destination);
		}

		private bool LocationIsInvalid(Unit unit)
		{
			if (!(unit == null) && unit.IsDockable && !(unit.Faction == null))
			{
				return !unit.Faction.RequestDock(unit, fleet.Faction);
			}
			return true;
		}

		protected override void onInit()
		{
			base.onInit();
			FindPassengerGroup();
		}

		private double GetTradeCompletionDelay()
		{
			return GetTradeCompletionDelay(fleet, Engine);
		}

		private double GetTradeCompletionTime()
		{
			return GetTradeCompletionTime(fleet, Engine);
		}

		public static double GetTradeCompletionDelay(Fleet group, EngineASX engine)
		{
			return group.Faction.IsPlayerFaction ? engine.GameSettings.AIDockPlayerOwnedPreferredCoolDownTime : engine.GameSettings.AIDockPreferredCoolDownTime;
		}

		public static double GetTradeCompletionTime(Fleet group, EngineASX engine)
		{
			return engine.ScenarioElapsedTime + GetTradeCompletionDelay(group, engine);
		}

		protected override void tick(float elapsedTime)
		{
			ValidateAndClearIfInvalid();
			switch (currentState)
			{
			case ActiveTransportPassengerOrderState.None:
				PassengerGroup = null;
				IsIdle = true;
				SearchTradeRoutePeriodically();
				break;
			case ActiveTransportPassengerOrderState.GoPickup:
				if (!LocationHasPassengerGroup(passengerGroup.CurrentUnit, passengerGroup))
				{
					PassengerGroup = null;
					CurrentState = ActiveTransportPassengerOrderState.None;
				}
				else if (fleet.AllUnitsAtDock(passengerGroup.CurrentUnit))
				{
					CurrentState = ActiveTransportPassengerOrderState.PickingUp;
				}
				break;
			case ActiveTransportPassengerOrderState.Transporting:
				if (fleet.AllUnitsAtDock(passengerGroup.Destination))
				{
					CurrentState = ActiveTransportPassengerOrderState.Disembarking;
				}
				break;
			case ActiveTransportPassengerOrderState.PickingUp:
				if (CanTrade())
				{
					UnitComponentHolder unitComponentHolder = CanPickup(passengerGroup.CurrentUnit);
					if (unitComponentHolder != null)
					{
						PickupPassengers(unitComponentHolder);
						CurrentState = ActiveTransportPassengerOrderState.Embarking;
					}
					else
					{
						IsIdle = true;
					}
				}
				break;
			case ActiveTransportPassengerOrderState.Disembarking:
				if (CanTrade())
				{
					if (DisembarkPassengersIfAble())
					{
						CurrentState = ActiveTransportPassengerOrderState.PostDeliver;
					}
					else
					{
						IsIdle = true;
					}
				}
				break;
			case ActiveTransportPassengerOrderState.Embarking:
				if (Engine.ScenarioElapsedTime > endBuySellTime)
				{
					CurrentState = ActiveTransportPassengerOrderState.Transporting;
				}
				break;
			case ActiveTransportPassengerOrderState.PostDeliver:
				if (Engine.ScenarioElapsedTime > endBuySellTime)
				{
					CurrentState = ActiveTransportPassengerOrderState.None;
				}
				break;
			}
		}

		private void SearchTradeRoutePeriodically()
		{
			if (Time.time > nextSearchTradeRoutes)
			{
				nextSearchTradeRoutes = Time.time + timeBeforeSearchTradeRoutes;
				FindPassengerGroup();
			}
		}

		protected override void onFinalizeObjective()
		{
			base.onFinalizeObjective();
			CurrentState = ActiveTransportPassengerOrderState.None;
			passengerGroup = null;
		}

		protected override void onFleetReachedTarget()
		{
			base.onFleetReachedTarget();
			ValidateAndClearIfInvalid();
		}

		protected override void resetTargetPosition()
		{
			ValidateAndClearIfInvalid();
			switch (currentState)
			{
			case ActiveTransportPassengerOrderState.PickingUp:
				SetTargetToPickupLocation();
				break;
			case ActiveTransportPassengerOrderState.GoPickup:
				SetTargetToPickupLocation();
				break;
			case ActiveTransportPassengerOrderState.Disembarking:
				SetTargetToDropoffLocation();
				break;
			case ActiveTransportPassengerOrderState.Transporting:
				SetTargetToDropoffLocation();
				break;
			}
		}

		private void FindPassengerGroup()
		{
			if (passengerGroup != null)
			{
				return;
			}
			PassengerGroup = GetExistingValidPassengerGroups();
			if (passengerGroup == null)
			{
				int freeSpace = 0;
				if (GetUnitWithMostPassengerSpace(out freeSpace) != null)
				{
					PassengerGroup = SearchForBestPassengerGroup(freeSpace);
				}
				if (passengerGroup == null)
				{
					if (LogWrapper.LogMsgs)
					{
						LogWrapper.Log($"{fleet.name} ({fleet.Faction.ShortName}) Failed to find a passenger group", this, 1);
					}
				}
				else
				{
					CurrentState = ActiveTransportPassengerOrderState.GoPickup;
				}
			}
			else
			{
				CurrentState = ActiveTransportPassengerOrderState.Transporting;
			}
		}

		private UnitComponentHolder GetUnitWithMostPassengerSpace(out int freeSpace)
		{
			return GetUnitWithMostPassengerSpace(fleet, out freeSpace);
		}

		public static UnitComponentHolder GetUnitWithMostPassengerSpace(Fleet group, out int freeSpace)
		{
			freeSpace = 0;
			UnitComponentHolder result = null;
			foreach (NpcPilot npcPilot in group.NpcPilots)
			{
				UnitComponentHolder currentUnitComponents = npcPilot.CurrentUnitComponents;
				if (currentUnitComponents != null && currentUnitComponents.FreePassengerSpace > freeSpace)
				{
					freeSpace = currentUnitComponents.FreePassengerSpace;
					result = currentUnitComponents;
				}
			}
			return result;
		}

		private PassengerGroup GetExistingValidPassengerGroups()
		{
			foreach (PassengerGroup passengerGroup in fleet.Leader.CurrentUnit.Components.PassengerGroups)
			{
				if (passengerGroup.IsValid)
				{
					return passengerGroup;
				}
			}
			return null;
		}

		private PassengerGroup SearchForBestPassengerGroup(int freespace)
		{
			if (freespace > 0)
			{
				PassengerGroup bestTarget = null;
				if (fleet.Sector != null && fleet.Faction.Intel != null)
				{
					Sector sector = fleet.Sector;
					Vector3 sectorPosition = fleet.SectorPosition;
					Sector sector2 = sector;
					if (fleet.HomeBaseUnit != null && fleet.HomeBaseUnit.IsValidAndNotDestroyed)
					{
						sector2 = fleet.HomeBaseUnit.Sector;
					}
					float bestScore = 0f;
					int actualMaxJumpDist = GetActualMaxJumpDist();
					if (!fleet.Faction.AutopilotExcludedSectors.Contains(sector.UniqueId) && sector2.GetJumpDistanceTo(sector) <= actualMaxJumpDist)
					{
						FindBestPassengerGroupInSector(freespace, sector, sectorPosition, ref bestTarget, ref bestScore, 0, sector2, actualMaxJumpDist);
					}
					if (bestTarget == null && fleet.Faction.Intel != null)
					{
						SectorFinder.FindNavigableDiscoveredSectorsWithinJumpDistanceOf(sector2, actualMaxJumpDist, fleet.Faction);
						foreach (SectorFinder.SectorResult result in SectorFinder.Results)
						{
							Sector sector3 = result.Sector;
							if (sector3 != sector && !fleet.Faction.AutopilotExcludedSectors.Contains(sector3.UniqueId))
							{
								int jumpDistanceTo = sector.GetJumpDistanceTo(sector3);
								FindBestPassengerGroupInSector(freespace, sector3, sectorPosition, ref bestTarget, ref bestScore, jumpDistanceTo, sector2, actualMaxJumpDist);
							}
						}
					}
				}
				return bestTarget;
			}
			return null;
		}

		private bool IsPassengerGroupTargettedByOtherGroups(PassengerGroup passengerGroup)
		{
			foreach (Fleet fleet in fleet.Faction.Fleets)
			{
				if (fleet != base.fleet && fleet.ActiveOrder is ActiveTransportPassengersOrder activeTransportPassengersOrder && activeTransportPassengersOrder.passengerGroup == passengerGroup)
				{
					return true;
				}
			}
			return false;
		}

		private void FindBestPassengerGroupInSector(int passengerCapacity, Sector sector, Vector3 currentSectorPosition, ref PassengerGroup bestTarget, ref float bestScore, int jumpDistToThisSector, Sector homeSector, int maxJumpDistanceFromHomeSector)
		{
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
			if (unitsByType == null)
			{
				return;
			}
			foreach (Unit item in unitsByType)
			{
				if (item == null || item.IsMinorStation() || !(fleet.Faction.Intel != null) || !fleet.Faction.Intel.IsUnitDiscoveredOrOwned(item) || item.Components.PassengerGroups.Count <= 0 || (!fleet.Faction.TradeIllegalGoods && !item.UnitClass.Legal) || !(item.Faction != null) || LocationIsInvalid(item) || !(item.Faction.GetOpinion(fleet.Faction) > Engine.GameSettings.AIMinTradeOpinion))
				{
					continue;
				}
				float num = 0f;
				num -= (float)jumpDistToThisSector * 7f;
				if (jumpDistToThisSector == 0)
				{
					num -= Vector3.Distance(item.SectorPosition, currentSectorPosition) / Engine.World.GateDistance * 7f;
				}
				num += Random.value * 1f;
				if (!fleet.CanDockAllAtUnitIgnoreOccupancy(item))
				{
					continue;
				}
				foreach (PassengerGroup passengerGroup in item.Components.PassengerGroups)
				{
					if (!passengerGroup.IsValid || !passengerGroup.IsWaitingForPickup || LocationIsInvalid(passengerGroup.Destination) || (!fleet.Faction.TradeIllegalGoods && !passengerGroup.Destination.UnitClass.Legal) || passengerGroup.PassengerCount > passengerCapacity || IsPassengerGroupTargettedByOtherGroups(passengerGroup) || !fleet.CanDockAllAtUnitIgnoreOccupancy(passengerGroup.Destination))
					{
						continue;
					}
					int jumpDistanceTo = homeSector.GetJumpDistanceTo(passengerGroup.Destination.Sector);
					if (jumpDistanceTo >= 0 && jumpDistanceTo <= maxJumpDistanceFromHomeSector && (!(passengerGroup.Destination.Sector != sector) || !fleet.Faction.AutopilotExcludedSectors.Contains(passengerGroup.Destination.Sector.UniqueId)))
					{
						float num2 = num;
						num2 += 5f * (float)passengerGroup.PassengerCount;
						if (bestTarget == null || num2 > bestScore)
						{
							bestTarget = passengerGroup;
							bestScore = num2;
						}
					}
				}
			}
		}

		private bool CanTrade()
		{
			switch (currentState)
			{
			case ActiveTransportPassengerOrderState.PickingUp:
				if (fleet.Leader != null)
				{
					return fleet.AllUnitsAtDock(passengerGroup.CurrentUnit);
				}
				return false;
			case ActiveTransportPassengerOrderState.Disembarking:
				if (fleet.Leader != null)
				{
					return fleet.AllUnitsAtDock(passengerGroup.Destination);
				}
				return false;
			default:
				return false;
			}
		}

		private UnitComponentHolder CanPickup(Unit dockUnit)
		{
			foreach (NpcPilot npcPilot in fleet.NpcPilots)
			{
				UnitComponentHolder currentUnitComponents = npcPilot.CurrentUnitComponents;
				if (currentUnitComponents != null && currentUnitComponents.DockUnit == dockUnit && currentUnitComponents.FreePassengerSpace >= passengerGroup.PassengerCount)
				{
					return currentUnitComponents;
				}
			}
			return null;
		}

		private void PickupPassengers(UnitComponentHolder unitComponents)
		{
			EngineASX.Instance.PassengerManager.PickupPassengers(passengerGroup, unitComponents.Unit);
		}

		private bool LocationHasPassengerGroup(Unit unit, PassengerGroup passengerGroup)
		{
			if (unit != null)
			{
				return passengerGroup.CurrentUnit == unit;
			}
			return false;
		}

		private void SetTargetToDropoffLocation()
		{
			fleet.SetTargetToDock(this, passengerGroup.Destination);
		}

		private void SetTargetToPickupLocation()
		{
			fleet.SetTargetToDock(this, passengerGroup.Source);
		}

		private void OnStateChanged()
		{
			switch (currentState)
			{
			case ActiveTransportPassengerOrderState.PostDeliver:
			case ActiveTransportPassengerOrderState.Embarking:
				endBuySellTime = GetTradeCompletionTime();
				break;
			case ActiveTransportPassengerOrderState.None:
				PassengerGroup = null;
				DestroyAllGroupPassengerGroups();
				break;
			case ActiveTransportPassengerOrderState.PickingUp:
				if (passengerGroup != null)
				{
					if (!ValidatePassengerGroup())
					{
						CurrentState = ActiveTransportPassengerOrderState.None;
					}
				}
				else
				{
					Debug.LogError("Expected objective to have PassengerGroup", this);
					CurrentState = ActiveTransportPassengerOrderState.None;
				}
				break;
			case ActiveTransportPassengerOrderState.Disembarking:
				if (passengerGroup != null)
				{
					if (!ValidatePassengerGroup())
					{
						CurrentState = ActiveTransportPassengerOrderState.None;
					}
				}
				else
				{
					Debug.LogError("Expected objective to have PassengerGroup", this);
					CurrentState = ActiveTransportPassengerOrderState.None;
				}
				break;
			case ActiveTransportPassengerOrderState.GoPickup:
				if (passengerGroup != null)
				{
					if (passengerGroup.CurrentUnit == null)
					{
						Debug.LogError("AI is attempting to buy but doesn't have a Current Unit", this);
					}
				}
				else
				{
					Debug.LogError("Expected objective to have PassengerGroup", this);
				}
				break;
			case ActiveTransportPassengerOrderState.Transporting:
				if (passengerGroup != null)
				{
					if (passengerGroup.Destination == null)
					{
						Debug.LogError("AI is attempting to sell but doesn't have a Destination", this);
					}
				}
				else
				{
					Debug.LogError("Expected objective to have PassengerGroup", this);
				}
				break;
			}
			IsIdle = false;
			ResetTargetPosition();
		}

		private void DestroyAllGroupPassengerGroups()
		{
			foreach (UnitComponentHolder ship in fleet.Ships)
			{
				if (!(ship != null) || ship.PassengerGroups.Count <= 0)
				{
					continue;
				}
				foreach (PassengerGroup item in ship.PassengerGroups.ToList())
				{
					item.SafeDestroy();
				}
			}
		}

		public override void OnNpcUnableToDockAtNavpoint(NpcPilot controller, Unit unit, UnableToDockReason unableToDockReason)
		{
			if (passengerGroup != null)
			{
				switch (unableToDockReason)
				{
				case UnableToDockReason.Refused:
					if (LogWrapper.LogMsgs)
					{
						LogWrapper.Log($"Fleet {fleet} refused permission to dock. Invalidating state of ${this}", this, 1);
					}
					CurrentState = ActiveTransportPassengerOrderState.None;
					return;
				case UnableToDockReason.DockingBaysFull:
					if (LogWrapper.LogMsgs)
					{
						LogWrapper.Log($"Fleet {fleet} unable to dock as bays full. Invalidating state of ${this}", this, 1);
					}
					CurrentState = ActiveTransportPassengerOrderState.None;
					return;
				}
			}
			base.OnNpcUnableToDockAtNavpoint(controller, unit, unableToDockReason);
		}

		public override DockedPreference GetPreferToDockWhenIdle()
		{
			ActiveTransportPassengerOrderState activeTransportPassengerOrderState = currentState;
			if (activeTransportPassengerOrderState == ActiveTransportPassengerOrderState.PickingUp || (uint)(activeTransportPassengerOrderState - 4) <= 2u)
			{
				return DockedPreference.Dock;
			}
			return base.GetPreferToDockWhenIdle();
		}

		public override void OnFleetFailedToFindPathToTarget()
		{
			PassengerGroup = null;
		}

		public override bool RequestUndock(NpcPilot npcPilot)
		{
			ActiveTransportPassengerOrderState activeTransportPassengerOrderState = currentState;
			if (activeTransportPassengerOrderState == ActiveTransportPassengerOrderState.PickingUp || (uint)(activeTransportPassengerOrderState - 4) <= 2u)
			{
				return false;
			}
			return true;
		}
	}
}
