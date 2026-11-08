using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.PassengerGroups;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class PassengerManager : MonoBehaviour
	{
		private struct PassengerGroupDestination : IWeighted
		{
			public Unit Unit { get; set; }

			public float Weight { get; set; }
		}

		private static List<PassengerGroupDestination> destinationCache = new List<PassengerGroupDestination>(30);

		private Unit currentCheckUnit;

		private int currentCheckSectorIndex = -1;

		private int currentCheckUnitIndex = -1;

		private Sector currentCheckSector;

		private float nextUpdateTime;

		private static List<PassengerGroup> destructionCache = new List<PassengerGroup>(20);

		public int MaxJumpDistance = -1;

		private PassengerManagerDestinationSearch destinationSearch;

		public bool IsSearchingForDestination => destinationSearch != null;

		public static Unit GetInstantDestination(Unit currentUnit)
		{
			PassengerManagerDestinationSearch passengerManagerDestinationSearch = new PassengerManagerDestinationSearch();
			InitDestinationSearch(currentUnit, passengerManagerDestinationSearch);
			passengerManagerDestinationSearch.ProcessUntilCompletion();
			return passengerManagerDestinationSearch.BestDestinationUnit;
		}

		private static void InitDestinationSearch(Unit currentUnit, PassengerManagerDestinationSearch search)
		{
			int maxJumpDistance = Mathf.CeilToInt(Mathf.Pow(Random.value, GameController.Instance.GameSettings.PassengerGroupSettings.MaxDistancePower) * (float)GameController.Instance.GameSettings.PassengerGroupSettings.MaxDestinationJumpDistance);
			search.Initialise(currentUnit);
			search.MaxJumpDistance = maxJumpDistance;
		}

		public static int CreatePassengerGroup(Unit unit, Unit destination, bool initialSeed)
		{
			PassengerGroup passengerGroup = new PassengerGroup();
			passengerGroup.Init();
			passengerGroup.CurrentUnit = unit;
			passengerGroup.Source = passengerGroup.CurrentUnit;
			passengerGroup.Destination = destination;
			passengerGroup.ExpiryTime = (initialSeed ? (unit.Engine.ScenarioElapsedTime + (double)(unit.Engine.GameSettings.PassengersGroupExpireTime * Random.value)) : (unit.Engine.ScenarioElapsedTime + (double)unit.Engine.GameSettings.PassengersGroupExpireTime));
			int num = 1;
			if (Random.Range(0, 2) == 0)
			{
				num = Random.Range(2, 10);
			}
			passengerGroup.PassengerCount = num;
			passengerGroup.CacheRevenue();
			return num;
		}

		public static bool ShouldCreatePassengersAtUnit(Unit unit)
		{
			if (unit.Components != null && unit.Faction != null && unit.Faction.FactionTypeInfo != null && unit.Faction.FactionTypeInfo.GeneratePassengerGroups && unit.UnitType == UnitType.Station && ShouldCreatePassengersAtStationPurpose(unit.UnitClass.StationPurpose))
			{
				return unit.IsDockable;
			}
			return false;
		}

		public static bool ShouldCreatePassengersAtStationPurpose(StationPurpose stationPurpose)
		{
			switch (stationPurpose)
			{
			case StationPurpose.Refinery:
			case StationPurpose.TradeStation:
			case StationPurpose.Factory:
			case StationPurpose.Shipyard:
			case StationPurpose.Equipment:
			case StationPurpose.Repair:
			case StationPurpose.Bar:
			case StationPurpose.SectorControl:
			case StationPurpose.Outpost:
			case StationPurpose.Scrapyard:
				return true;
			default:
				return false;
			}
		}

		public static bool CanUnitBeDestination(Unit currentUnit, Unit destination)
		{
			if (currentUnit != destination && destination != null && destination.IsValidAndNotDestroyed && destination.Components != null)
			{
				return destination.IsDockable;
			}
			return false;
		}

		public static bool CanUnitBeDestinationForFaction(Faction faction, Unit destinationUnit)
		{
			if (destinationUnit.Faction != null && destinationUnit.Faction.FactionTypeInfo != null && destinationUnit.Faction.FactionTypeInfo.ReceivePassengerGroups && faction != null && faction.Intel.IsUnitDiscoveredOrOwned(destinationUnit) && !destinationUnit.IsHostileToOrAlwaysHostileToTwoWay(faction))
			{
				return !faction.IsHostileTo(destinationUnit);
			}
			return false;
		}

		private void Update()
		{
			if (!EngineASX.LoadedAndReady || EngineASX.Instance.Sectors.Count == 0)
			{
				return;
			}
			if (IsSearchingForDestination)
			{
				if (currentCheckUnit == null || !currentCheckUnit.IsValidAndNotDestroyed)
				{
					currentCheckUnit = null;
					CancelDestinationSearch();
				}
				else if (destinationSearch.HasFinished)
				{
					if (destinationSearch.BestDestinationUnit != null && CanUnitBeDestination(currentCheckUnit, destinationSearch.BestDestinationUnit) && CanUnitBeDestinationForFaction(currentCheckUnit.Faction, destinationSearch.BestDestinationUnit))
					{
						int num = CreatePassengerGroup(currentCheckUnit, destinationSearch.BestDestinationUnit, initialSeed: false);
						if (num > 0)
						{
							EngineASX.Instance.DebugInfo.PassengerGroups_NumGroupsCreated++;
							EngineASX.Instance.DebugInfo.PassengerGroups_NumPassengersCreated += num;
						}
					}
					CancelDestinationSearch();
				}
				else
				{
					destinationSearch.Process();
				}
			}
			else
			{
				if (!(Time.time > nextUpdateTime))
				{
					return;
				}
				if (currentCheckSector != null)
				{
					UpdatePassengerGroupsInSector();
				}
				else
				{
					currentCheckSectorIndex++;
					if (currentCheckSectorIndex >= EngineASX.Instance.Sectors.Count)
					{
						currentCheckSectorIndex = 0;
					}
					currentCheckSector = EngineASX.Instance.Sectors[currentCheckSectorIndex];
					currentCheckUnitIndex = -1;
					UpdatePassengerGroupsInSector();
				}
				nextUpdateTime = Time.time + GameController.Instance.GameSettings.PerformanceSettings.PassengerManagerTimeBetweenReplenish;
			}
		}

		private void UpdatePassengerGroupsInSector()
		{
			List<Unit> unitsByType = currentCheckSector.GetUnitsByType(UnitType.Station);
			if (unitsByType != null)
			{
				currentCheckUnitIndex++;
				if (currentCheckUnitIndex >= unitsByType.Count)
				{
					currentCheckSector = null;
					return;
				}
				currentCheckUnit = unitsByType[currentCheckUnitIndex];
				if (currentCheckUnit != null)
				{
					if (ShouldCreatePassengersAtUnit(currentCheckUnit) && currentCheckUnit.Components.PassengerGroups.Count < PasssengerGroupHelper.GetMaxPassengerGroupCountAtUnit(currentCheckUnit))
					{
						EngineASX.Instance.DebugInfo.PassengerGroups_NumDestinationSearchesStarted++;
						StartSearchForDestination(currentCheckUnit);
					}
					RemoveInvalidOrExpiredPassengerGroupsAtUnit(currentCheckUnit);
				}
			}
			else
			{
				currentCheckSector = null;
			}
		}

		public void InvalidateAllWaitingPassengerGroups()
		{
			foreach (PassengerGroup item in EngineASX.Instance.PassengerGroups.ToList())
			{
				if (item != null && item.IsWaitingForPickup)
				{
					item.SafeDestroy();
				}
			}
		}

		private void CancelDestinationSearch()
		{
			destinationSearch = null;
		}

		public void StartSearchForDestination(Unit currentUnit)
		{
			destinationSearch = new PassengerManagerDestinationSearch();
			InitDestinationSearch(currentUnit, destinationSearch);
		}

		public static void RemoveInvalidOrExpiredPassengerGroupsAtUnit(Unit unit)
		{
			int count = unit.Components.PassengerGroups.Count;
			if (count == 0)
			{
				return;
			}
			destructionCache.Clear();
			for (int i = 0; i < count; i++)
			{
				PassengerGroup passengerGroup = unit.Components.PassengerGroups[i];
				if (!passengerGroup.IsValid || (passengerGroup.IsWaitingForPickup && passengerGroup.WaitTimeExpired))
				{
					destructionCache.Add(passengerGroup);
				}
			}
			foreach (PassengerGroup item in destructionCache)
			{
				item.SafeDestroy();
				EngineASX.Instance.DebugInfo.PassengerGroups_NumGroupsExpired++;
			}
		}

		public void PickupPassengers(PassengerGroup passengerGroup, Unit taxiUnit)
		{
			Sector sector = passengerGroup.CurrentUnit.Sector;
			Faction faction = passengerGroup.CurrentUnit.Faction;
			passengerGroup.CurrentUnit = taxiUnit;
			if (taxiUnit.Faction != null && faction != null && faction.Intel != null && taxiUnit.Faction != null)
			{
				faction.Intel.TrySharePathWithFaction(sector, passengerGroup.Destination.Sector, taxiUnit.Faction);
				taxiUnit.Faction.Intel.ClearCachedUniversePaths();
			}
		}
	}
}
