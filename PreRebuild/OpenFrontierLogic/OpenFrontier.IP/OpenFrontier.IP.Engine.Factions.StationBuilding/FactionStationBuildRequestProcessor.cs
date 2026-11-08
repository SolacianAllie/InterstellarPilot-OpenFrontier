using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Factions.Intel;
using OpenFrontier.IP.Engine.Fleets.ActiveOrders;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.UI.Screens.BuildMode;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions.StationBuilding
{
	public class FactionStationBuildRequestProcessor
	{
		private class RequestFleetSearch
		{
			public float BestScore;

			public Fleet BestFleet;

			public int CurrentProcessFleetIndex;

			public FactionAIBuildStationRequest BuildStationRequest;

			public static RequestFleetSearch Start(FactionAIBuildStationRequest buildStationRequest)
			{
				return new RequestFleetSearch
				{
					BestScore = 0f,
					CurrentProcessFleetIndex = 0,
					BuildStationRequest = buildStationRequest
				};
			}
		}

		public const int MaxConcurrentBuildStationRequestsHardLimit = 3;

		private List<FactionAIBuildStationRequest> buildStationRequests = new List<FactionAIBuildStationRequest>(4);

		private readonly FactionAIBase factionAI;

		public const float CombatRatingRequiredForSectorControl = 10f;

		public const float CombatRatingRequiredForMajorStation = 8f;

		public const float CombatRatingRequiredForGeneralStation = 5f;

		public const float CombatRatingRequiredForMinorStation = 2f;

		private const float BuildStationOrderPriority = 0.6f;

		private RequestFleetSearch currentFleetSearch;

		private bool IsSearchingForFleet => currentFleetSearch != null;

		public int MaxConcurrentBuildStationRequests
		{
			get
			{
				if (factionAI.Faction.IsFreelancer)
				{
					return 1;
				}
				return Mathf.Min(factionAI.Faction.Fleets.Count, 3);
			}
		}

		public FactionStationBuildRequestProcessor(FactionAIBase factionAI)
		{
			this.factionAI = factionAI;
		}

		public void ClearRequests()
		{
			buildStationRequests.Clear();
			currentFleetSearch = null;
		}

		public void Tick()
		{
			if (IsSearchingForFleet)
			{
				ProcessFleetSearch();
			}
			else if (buildStationRequests.Count > 0)
			{
				ProcessQueue();
			}
		}

		private void ProcessFleetSearch()
		{
			if (currentFleetSearch.CurrentProcessFleetIndex >= factionAI.Faction.Fleets.Count)
			{
				OnFleetSearchFinished();
				currentFleetSearch = null;
				return;
			}
			Fleet fleet = factionAI.Faction.Fleets[currentFleetSearch.CurrentProcessFleetIndex];
			if (fleet != null)
			{
				FactionAIBuildStationRequest buildStationRequest = currentFleetSearch.BuildStationRequest;
				if (CanOrderFleetToBuildStation(fleet, buildStationRequest.UnitClass, buildStationRequest.Sector, buildStationRequest.SectorPosition, out var score) && (currentFleetSearch.BestFleet == null || score > currentFleetSearch.BestScore))
				{
					currentFleetSearch.BestFleet = fleet;
					currentFleetSearch.BestScore = score;
				}
			}
			currentFleetSearch.CurrentProcessFleetIndex++;
		}

		private void OnFleetSearchFinished()
		{
			if (currentFleetSearch.BestFleet != null && CanOrderFleetToBuildStationConsideringCurrentOrders(currentFleetSearch.BestFleet))
			{
				FactionAIBuildStationRequest buildStationRequest = currentFleetSearch.BuildStationRequest;
				if (!BuildStationValidator.CanBuild(buildStationRequest.UnitClass, buildStationRequest.Sector, buildStationRequest.SectorPosition, out var _))
				{
					EngineASX.Instance.DebugInfo.NumFactionAIRequestsToBuildStationsInvalidated++;
				}
				else if (!factionAI.Faction.IsAffordableConsideringReserve(buildStationRequest.UnitClass.SaleCost))
				{
					EngineASX.Instance.DebugInfo.NumFactionAIRequestsToBuildStationsInvalidated++;
				}
				else
				{
					OrderFleetToBuildStation(currentFleetSearch.BestFleet, buildStationRequest.UnitClass, buildStationRequest.Sector, buildStationRequest.SectorPosition);
				}
			}
			else if (factionAI.Faction.Fleets.Count > 0 && CanRequestToBuildNewStations())
			{
				FactionAIBuildStationRequest buildStationRequest2 = currentFleetSearch.BuildStationRequest;
				buildStationRequests.Add(buildStationRequest2);
			}
		}

		private void ProcessQueue()
		{
			FactionAIBuildStationRequest buildStationRequest = buildStationRequests[0];
			if (!BuildStationValidator.CanBuild(buildStationRequest.UnitClass, buildStationRequest.Sector, buildStationRequest.SectorPosition, out var _))
			{
				EngineASX.Instance.DebugInfo.NumFactionAIRequestsToBuildStationsInvalidated++;
				buildStationRequests.RemoveAt(0);
				return;
			}
			if (EngineASX.Instance.ScenarioElapsedTime > buildStationRequest.RequestTime + 60.0)
			{
				EngineASX.Instance.DebugInfo.NumFactionAIRequestsToBuildStationsTimedOut++;
				buildStationRequests.RemoveAt(0);
				return;
			}
			int num = factionAI.Faction.GetCountOfStationPurpose(buildStationRequest.UnitClass.StationPurpose);
			foreach (Fleet fleet in factionAI.Faction.Fleets)
			{
				if (fleet.ActiveOrder is ActiveBuildStationOrder activeBuildStationOrder && activeBuildStationOrder.BuildStationOrder.UnitClass.StationPurpose == buildStationRequest.UnitClass.StationPurpose)
				{
					num++;
				}
			}
			if (num >= factionAI.GetPreferredCountOfUnitClass(buildStationRequest.UnitClass))
			{
				EngineASX.Instance.DebugInfo.NumFactionAIRequestsToBuildStationsInvalidated++;
				buildStationRequests.RemoveAt(0);
			}
			else
			{
				currentFleetSearch = RequestFleetSearch.Start(buildStationRequest);
				buildStationRequests.RemoveAt(0);
			}
		}

		public void OrderFleetToBuildStation(Fleet fleet, UnitClass unitClass, Sector sector, Vector3 sectorPosition)
		{
			UniversePath universePath = fleet.Faction.Intel.GetUniversePath(fleet.Sector, sector);
			if (universePath != null)
			{
				BuildStationOrder buildStationOrder = UnityObjectHelper.NewGameObject<BuildStationOrder>();
				buildStationOrder.UnitClass = unitClass;
				buildStationOrder.Sector = sector;
				buildStationOrder.SectorPosition = sectorPosition;
				buildStationOrder.InsufficientCreditsMode = InsufficientCreditsMode.Abort;
				buildStationOrder.SetCreditsAndSetAsRestrictedSpend(fleet, unitClass.SaleCost);
				fleet.SetOrder(buildStationOrder);
				fleet.Faction.ApplyTransaction(-unitClass.SaleCost, FactionTransactionType.FleetTransfer, null, fleet.LeaderUnit);
				buildStationOrder.MaxDuration = 600f + (float)universePath.Jumps * 600f;
				buildStationOrder.TimeoutTime = 180f;
				factionAI.OnFleetOrdered(fleet);
				EngineASX.Instance.DebugInfo.NumFactionAIOrdersToBuildStation++;
			}
		}

		public bool RequestToBuildNewStation(UnitClass unitClass, Sector sector, Vector3 sectorPosition)
		{
			if (!CanRequestToBuildNewStations())
			{
				return false;
			}
			if (currentFleetSearch != null && currentFleetSearch.BuildStationRequest.UnitClass.StationPurpose == unitClass.StationPurpose)
			{
				return false;
			}
			foreach (FactionAIBuildStationRequest buildStationRequest in buildStationRequests)
			{
				if (buildStationRequest.UnitClass.StationPurpose == unitClass.StationPurpose)
				{
					return false;
				}
			}
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"Faction AI {factionAI.Faction} has approved request to build new {unitClass.GetClassAndSeriesName()} in {sector}", factionAI, 1);
			}
			buildStationRequests.Add(new FactionAIBuildStationRequest(unitClass, sector, sectorPosition, EngineASX.Instance.ScenarioElapsedTime));
			return true;
		}

		private bool CanOrderFleetToBuildStationConsideringCurrentOrders(Fleet fleet)
		{
			if (!FactionAIBase.CanOrderFleet(fleet) || !factionAI.CanOverrideFleetOrder(fleet, 0.6f))
			{
				return false;
			}
			return true;
		}

		private bool CanOrderFleetToBuildStation(Fleet fleet, UnitClass unitClass, Sector sector, Vector3 sectorPosition, out float score)
		{
			score = 0f;
			if (!CanOrderFleetToBuildStationConsideringCurrentOrders(fleet))
			{
				return false;
			}
			if (fleet.ActiveOrder is ActiveBuildStationOrder)
			{
				return false;
			}
			float fleetCombatRatingRequiredToBuildStation = GetFleetCombatRatingRequiredToBuildStation(unitClass);
			float cachedSimpleCombatRating = fleet.GetCachedSimpleCombatRating();
			score = 0f;
			if (cachedSimpleCombatRating < fleetCombatRatingRequiredToBuildStation)
			{
				if (cachedSimpleCombatRating < fleetCombatRatingRequiredToBuildStation * 0.5f)
				{
					return false;
				}
				score -= Mathf.Clamp01((fleetCombatRatingRequiredToBuildStation - cachedSimpleCombatRating) / 30f) * 5f;
			}
			else if (cachedSimpleCombatRating > fleetCombatRatingRequiredToBuildStation * 2f)
			{
				score -= Mathf.Clamp01((cachedSimpleCombatRating - fleetCombatRatingRequiredToBuildStation) / 30f) * 5f;
			}
			int jumpDistanceTo = fleet.Sector.GetJumpDistanceTo(sector);
			score -= jumpDistanceTo;
			return true;
		}

		private static float GetFleetCombatRatingRequiredToBuildStation(UnitClass unitClass)
		{
			switch (unitClass.StationPurpose)
			{
			case StationPurpose.SectorControl:
				return 10f;
			case StationPurpose.Refinery:
			case StationPurpose.TradeStation:
			case StationPurpose.Shipyard:
				return 8f;
			case StationPurpose.Factory:
			case StationPurpose.Equipment:
			case StationPurpose.Repair:
			case StationPurpose.Outpost:
			case StationPurpose.Scrapyard:
				return 5f;
			default:
				return 2f;
			}
		}

		public bool CanRequestToBuildNewStations()
		{
			return buildStationRequests.Count < MaxConcurrentBuildStationRequests;
		}
	}
}
