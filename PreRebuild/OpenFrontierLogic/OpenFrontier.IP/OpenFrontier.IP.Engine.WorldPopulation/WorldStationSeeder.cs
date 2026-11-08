using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldPopulation
{
	public static class WorldStationSeeder
	{
		public class StationBuild
		{
			public Sector Sector { get; set; }

			public Vector3 SectorPosition { get; set; }

			public UnitClass UnitClass { get; set; }

			public Faction Faction { get; set; }

			public float Weight { get; set; }
		}

		private struct StationSpawnSector : IWeighted
		{
			public Sector Sector { get; set; }

			public float Weight { get; set; }
		}

		private struct NewStation : IWeighted
		{
			public UnitClass UnitClass { get; set; }

			public float Weight { get; set; }
		}

		private static PriorityQueue<StationBuild, float> stationBuildCache = new PriorityQueue<StationBuild, float>(30);

		private static PriorityQueue<StationSpawnSector, float> spawnSectorPriorityQueue = new PriorityQueue<StationSpawnSector, float>(30);

		private static float MinDistanceBetweenStationsForDeployment => GameController.Instance.GameSettings.MinDistanceBetweenStations * 2f * 1.2f;

		public static Sector GetBestSectorForNewStation(EngineASX engine, Faction faction, UnitClass stationToBuild, int maxBuildJumpDistanceFromHomeSector)
		{
			spawnSectorPriorityQueue.Clear();
			SectorFinder.FindNavigableDiscoveredSectorsWithinJumpDistanceOf(faction.HomeSector, maxBuildJumpDistanceFromHomeSector, faction);
			foreach (SectorFinder.SectorResult result in SectorFinder.Results)
			{
				Sector sector = result.Sector;
				if (CanBuildStationInSector(stationToBuild, faction, sector))
				{
					float newStationSectorScore = GetNewStationSectorScore(sector, faction, stationToBuild);
					StationSpawnSector value = new StationSpawnSector
					{
						Sector = sector,
						Weight = newStationSectorScore
					};
					spawnSectorPriorityQueue.Enqueue(value, newStationSectorScore);
				}
			}
			if (spawnSectorPriorityQueue.Count > 0)
			{
				PriorityQueueItem<StationSpawnSector, float> priorityQueueItem = spawnSectorPriorityQueue.Dequeue();
				if (priorityQueueItem.Value.Sector != null)
				{
					return priorityQueueItem.Value.Sector;
				}
			}
			return null;
		}

		private static int GetSectorCountOfStationClass(Sector sector, UnitClass unitClass)
		{
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
			if (unitsByType != null)
			{
				int num = 0;
				{
					foreach (Unit item in unitsByType)
					{
						if (item != null && item.UnitClass == unitClass)
						{
							num++;
						}
					}
					return num;
				}
			}
			return 0;
		}

		public static float GetNewStationSectorScore(Sector sector, Faction faction, UnitClass unitClass)
		{
			float num = FactionHomeScenePicker.GetScoreBasedOnAdjustedSecurityType(sector, faction.FactionTypeInfo) * 5f;
			if (unitClass.StationPurpose != StationPurpose.Defence)
			{
				num -= (float)sector.GetCountOfStationPurpose(unitClass.StationPurpose);
			}
			if (sector.ContainsHostileStationsIgnoringIntel(faction))
			{
				num -= 6f;
			}
			if (!unitClass.Legal)
			{
				num -= sector.SecurityLevel * 5f;
				if (sector.IsSectorAnUncontrolledBorderSector())
				{
					num += 2.5f;
				}
			}
			switch (unitClass.StationPurpose)
			{
			case StationPurpose.Shipyard:
			case StationPurpose.Equipment:
			case StationPurpose.Repair:
			case StationPurpose.Satellite:
			case StationPurpose.Outpost:
				num -= (float)GetFactionCountOfStationPurposeInSector(faction, unitClass.StationPurpose, sector) * 5f;
				break;
			case StationPurpose.Factory:
			case StationPurpose.Scrapyard:
			{
				if (faction.FactionType == FactionType.Bandit)
				{
					num -= 4f;
				}
				int sectorCountOfStationClass = GetSectorCountOfStationClass(sector, unitClass);
				num -= (float)sectorCountOfStationClass * 2f;
				break;
			}
			case StationPurpose.Bar:
				num -= (float)sector.GetCountOfStationPurpose(unitClass.StationPurpose) * 10f;
				break;
			case StationPurpose.Refinery:
				num -= (float)sector.GetCountOfStationPurpose(StationPurpose.Refinery) * 5f;
				if (sector.HasAsteroidClusters)
				{
					num += 4f;
				}
				foreach (SectorNeighbour neighbour in sector.Neighbours)
				{
					if (neighbour.IsStableConnection && neighbour.Sector.HasAsteroidClusters)
					{
						num += 0.5f;
					}
				}
				break;
			}
			if (faction.FactionAI.HomeSector != null)
			{
				int jumpDistanceTo = sector.GetJumpDistanceTo(faction.FactionAI.HomeSector);
				float num2 = 2f;
				switch (faction.FactionType)
				{
				case FactionType.Bandit:
					num2 = Mathf.Lerp(7f, 0.5f, faction.FactionAI.AISettings.OffensiveStance);
					break;
				case FactionType.Empire:
					num2 = 0.5f;
					break;
				}
				num -= (float)jumpDistanceTo * num2;
			}
			return num + Random.value;
		}

		private static int GetFactionCountOfStationPurposeInSector(Faction faction, StationPurpose stationPurpose, Sector sector)
		{
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
			int num = 0;
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item != null && item.Faction == faction && item.UnitClass.StationPurpose == stationPurpose)
					{
						num++;
					}
				}
			}
			return num;
		}

		public static StationBuild GetNewStationBuild(EngineASX engine, Faction faction, int? availableCredits, StationPurpose? stationPurposeFilter, bool considerShipRatio, int maxBuildDistanceFromHomeSector)
		{
			stationBuildCache.Clear();
			GetPossibleStationsToBuild(engine, faction, stationPurposeFilter, availableCredits, stationBuildCache, considerShipRatio, maxBuildDistanceFromHomeSector);
			if (stationBuildCache.Count > 0)
			{
				return stationBuildCache.Dequeue().Value;
			}
			return null;
		}

		private static int GetDefensiveTurretCountAroundUnit(Unit unit, float maxDistance = 500f)
		{
			int num = Physics.OverlapSphereNonAlloc(unit.transform.position, maxDistance, EngineASX.ColliderCache, GameController.Instance.StationsMask, QueryTriggerInteraction.Collide);
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				Unit component = EngineASX.ColliderCache[i].GetComponent<Unit>();
				if (component != null && component.IsValidAndNotDestroyed && component.Faction == unit.Faction && component != unit && component.UnitClass.StationPurpose == StationPurpose.Defence)
				{
					num2++;
				}
			}
			return num2;
		}

		public static Vector3? GetNewStationSectorPosition(UnitClass unitClass, Sector sector, Faction faction)
		{
			if (!unitClass.Legal)
			{
				if (sector.SecurityLevel > 0.2f)
				{
					return GetNewStationSectorPositionAtEdgeOfSector(sector);
				}
				return GetDefaultNewStationPosition(sector);
			}
			switch (unitClass.StationPurpose)
			{
			case StationPurpose.SectorControl:
				return sector.GetRandomSafeDeploymentSectorPosition(0f, 0.2f, MinDistanceBetweenStationsForDeployment, GameController.Instance.StaticNonOverlappingMask);
			case StationPurpose.Defence:
				return GetNewDefensiveStationSectorPosition(sector, faction);
			case StationPurpose.Refinery:
				return GetNewRefinerySectorPosition(sector, faction);
			default:
				if (faction.FactionType == FactionType.Bandit)
				{
					return GetNewStationSectorPositionForBandits(sector, faction, unitClass);
				}
				return TryGroupStationWithOtherStationInSector(sector, faction, unitClass) ?? new Vector3?(GetDefaultNewStationPosition(sector));
			}
		}

		public static Vector3 GetDefaultNewStationPosition(Sector sector)
		{
			return sector.GetRandomSafeDeploymentSectorPosition(0f, 0.8f, MinDistanceBetweenStationsForDeployment, GameController.Instance.StaticNonOverlappingMask);
		}

		private static Vector3? GetNewRefinerySectorPosition(Sector sector, Faction faction)
		{
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.AsteroidCluster);
			if (unitsByType != null && unitsByType.Count > 0)
			{
				Unit bestAsteroidClusterForRefinery = GetBestAsteroidClusterForRefinery(unitsByType);
				Vector3 checkSectorPosition = bestAsteroidClusterForRefinery.SectorPosition + Geometry.RandomXZUnitVector() * bestAsteroidClusterForRefinery.Radius * 0.8f;
				return PhysicsNonOverlappingPositionFinder.FindSectorPosition(sector, checkSectorPosition, MinDistanceBetweenStationsForDeployment, GameController.Instance.StaticNonOverlappingMask);
			}
			return sector.GetRandomSafeDeploymentSectorPosition(0f, 0.6f, MinDistanceBetweenStationsForDeployment, GameController.Instance.StaticNonOverlappingMask);
		}

		private static Unit GetBestAsteroidClusterForRefinery(List<Unit> asteroidClusters)
		{
			float num = 0f;
			Unit unit = null;
			foreach (Unit asteroidCluster in asteroidClusters)
			{
				float num2 = 0f;
				num2 -= (float)AsteroidClusterRefineryCount(asteroidCluster) * 10f;
				if (unit == null || num2 > num)
				{
					unit = asteroidCluster;
					num = num2;
				}
			}
			return unit;
		}

		private static int AsteroidClusterRefineryCount(Unit asteroidClusterUnit)
		{
			int num = Physics.OverlapSphereNonAlloc(asteroidClusterUnit.transform.position, 1700f, EngineASX.ColliderCache, GameController.Instance.StationsMask, QueryTriggerInteraction.Ignore);
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				Unit component = EngineASX.ColliderCache[i].GetComponent<Unit>();
				if (component != null && component.UnitClass.StationPurpose == StationPurpose.Refinery)
				{
					num2++;
				}
			}
			return num2;
		}

		private static Vector3? GetNewDefensiveStationSectorPosition(Sector sector, Faction faction)
		{
			Unit bestStationToDefendInSector = GetBestStationToDefendInSector(faction, sector);
			if (bestStationToDefendInSector != null)
			{
				return GetNewDefensiveStationSectorPosition(bestStationToDefendInSector);
			}
			return null;
		}

		public static Vector3? GetNewDefensiveStationSectorPosition(Unit bestStationToDefend, float distanceMultiplier = 1f)
		{
			Vector3 checkSectorPosition = bestStationToDefend.SectorPosition + Geometry.RandomXZUnitVector() * Mathf.Max(50f, bestStationToDefend.UnitClass.ShieldRingRadius) * GameController.Instance.GameSettings.TurretBuildDistanceRadiusMultiplier * distanceMultiplier;
			return PhysicsNonOverlappingPositionFinder.FindSectorPositionOrNull(bestStationToDefend.Sector, checkSectorPosition, 50f, GameController.Instance.StaticNonOverlappingMask);
		}

		private static Vector3? TryGroupStationWithOtherStationInSector(Sector sector, Faction faction, UnitClass unitClass)
		{
			List<Unit> unitsByType = faction.GetUnitsByType(UnitType.Station);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item != null && item.IsValidAndNotDestroyed && item.Sector == sector && !item.IsMinorStation() && CanGroupStationsTypesTogether(unitClass, item.UnitClass))
					{
						Vector3 checkSectorPosition = item.SectorPosition + Geometry.RandomXZUnitVector() * (item.UnitClass.ShieldRingRadius + MinDistanceBetweenStationsForDeployment);
						return PhysicsNonOverlappingPositionFinder.FindSectorPosition(sector, checkSectorPosition, MinDistanceBetweenStationsForDeployment, GameController.Instance.StaticNonOverlappingMask);
					}
				}
			}
			return null;
		}

		private static Vector3? GetNewStationSectorPositionForBandits(Sector sector, Faction faction, UnitClass unitClass)
		{
			return TryGroupStationWithOtherStationInSector(sector, faction, unitClass) ?? GetNewStationSectorPositionAtEdgeOfSector(sector);
		}

		private static Vector3? GetNewStationSectorPositionAtEdgeOfSector(Sector sector, float? minDistanceFromOtherStations = null)
		{
			float num = Mathf.Min(1.1f, sector.GateDistanceMultiplier * 1.2f);
			float maxInclusive = Mathf.Min(1.1f, num * 1.2f);
			for (int i = 0; i < 8; i++)
			{
				Vector3 randomSafeDeploymentSectorPositionFromGateDistance = sector.GetRandomSafeDeploymentSectorPositionFromGateDistance(Random.Range(num, maxInclusive) * EngineASX.Instance.World.GateDistance, minDistanceFromOtherStations ?? MinDistanceBetweenStationsForDeployment, GameController.Instance.StaticNonOverlappingMask);
				if (!SectorPositionWithinAngleOfWormhole(sector, randomSafeDeploymentSectorPositionFromGateDistance))
				{
					return randomSafeDeploymentSectorPositionFromGateDistance;
				}
			}
			return null;
		}

		private static bool SectorPositionWithinAngleOfWormhole(Sector sector, Vector3 sectorLocalPosition, float maxAngle = 30f)
		{
			Quaternion a = Quaternion.LookRotation(Vector3.Normalize(sectorLocalPosition));
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Wormhole);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					Quaternion b = Quaternion.LookRotation(Vector3.Normalize(item.transform.localPosition));
					if (Mathf.Abs(Quaternion.Angle(a, b)) < maxAngle)
					{
						return true;
					}
				}
			}
			return false;
		}

		private static bool CanGroupStationsTypesTogether(UnitClass unitClass1, UnitClass unitClass2)
		{
			if ((unitClass1.StationPurpose != StationPurpose.TradeStation || unitClass2.StationPurpose != StationPurpose.TradeStation) && (unitClass1.StationPurpose != StationPurpose.Repair || unitClass2.StationPurpose != StationPurpose.Repair) && (unitClass1.StationPurpose != StationPurpose.Refinery || unitClass2.StationPurpose != StationPurpose.Refinery) && (unitClass1.StationPurpose != StationPurpose.Shipyard || unitClass2.StationPurpose != StationPurpose.Shipyard) && (unitClass1.StationPurpose != StationPurpose.Equipment || unitClass2.StationPurpose != StationPurpose.Equipment))
			{
				if (unitClass1.StationPurpose == StationPurpose.Outpost)
				{
					return unitClass2.StationPurpose != StationPurpose.Outpost;
				}
				return true;
			}
			return false;
		}

		private static Unit GetBestStationToDefend(Faction faction)
		{
			return faction.GetUnitsByType(UnitType.Station)?.Where((Unit e) => e != null && e.IsValidAndNotDestroyed && e.UnitClass.StationPurpose != StationPurpose.Defence).OrderBy((Unit e) => GetStationToDefendPriority(e)).FirstOrDefault();
		}

		private static Unit GetBestStationToDefendInSector(Faction faction, Sector sector)
		{
			return faction.GetUnitsByType(UnitType.Station)?.Where((Unit e) => e != null && e.IsValidAndNotDestroyed && e.Sector == sector && e.UnitClass.StationPurposeRequiresDefence()).OrderBy((Unit e) => GetStationToDefendPriority(e)).FirstOrDefault();
		}

		private static float GetStationToDefendPriority(Unit unit)
		{
			float stationPurposeDefensePriority = GetStationPurposeDefensePriority(unit.UnitClass.StationPurpose);
			int defensiveTurretCountAroundUnit = GetDefensiveTurretCountAroundUnit(unit);
			return stationPurposeDefensePriority - (float)defensiveTurretCountAroundUnit * 3f;
		}

		public static float GetStationPurposeDefensePriority(StationPurpose stationPurpose)
		{
			switch (stationPurpose)
			{
			case StationPurpose.SectorControl:
				return 10f;
			case StationPurpose.TradeStation:
			case StationPurpose.Shipyard:
				return 7f;
			case StationPurpose.Refinery:
				return 6f;
			case StationPurpose.Factory:
			case StationPurpose.Equipment:
			case StationPurpose.Outpost:
			case StationPurpose.Scrapyard:
				return 5f;
			case StationPurpose.Satellite:
				return 2f;
			case StationPurpose.Defence:
				return 1.5f;
			default:
				return 1f;
			}
		}

		public static bool CanBuildStationInSector(UnitClass unitClass, Faction faction, Sector sector)
		{
			if (!faction.FactionAI.CanBuildStationInSector(sector, unitClass))
			{
				return false;
			}
			switch (unitClass.StationPurpose)
			{
			case StationPurpose.SectorControl:
				if (sector.ControllingFaction != null)
				{
					return false;
				}
				if (sector.GetCountOfStationPurpose(StationPurpose.SectorControl) > 0)
				{
					return false;
				}
				return true;
			case StationPurpose.Defence:
			{
				Unit bestStationToDefend = GetBestStationToDefend(faction);
				if (bestStationToDefend != null)
				{
					return bestStationToDefend.Sector == sector;
				}
				return false;
			}
			case StationPurpose.Refinery:
				if (sector.HasAsteroidClusters || (sector.Neighbours.Any((SectorNeighbour e) => e.IsStableConnection && e.Sector.HasAsteroidClusters) && !sector.HasPlanets))
				{
					if (sector.GetCountOfStationPurpose(StationPurpose.Refinery) > 3)
					{
						return false;
					}
					return true;
				}
				return false;
			case StationPurpose.TradeStation:
				if (sector.HasPlanets)
				{
					List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
					if (unitsByType != null)
					{
						foreach (Unit item in unitsByType)
						{
							if (item != null && item.UnitClass.StationPurpose == StationPurpose.TradeStation)
							{
								return false;
							}
						}
					}
					return true;
				}
				return false;
			default:
				return true;
			}
		}

		private static float GetFactionStationPriority(Faction faction, UnitClass unitClass, Sector sector)
		{
			if (unitClass.StationPurpose == StationPurpose.Defence)
			{
				Unit bestStationToDefendInSector = GetBestStationToDefendInSector(faction, sector);
				if (bestStationToDefendInSector != null)
				{
					int defensiveTurretCountAroundUnit = GetDefensiveTurretCountAroundUnit(bestStationToDefendInSector);
					int preferredDefensiveTurretsCount = GetPreferredDefensiveTurretsCount(faction, unitClass);
					return Mathf.Max(0, preferredDefensiveTurretsCount - defensiveTurretCountAroundUnit);
				}
			}
			float num = 1f + Random.value * 4f;
			if (faction.FactionAI != null)
			{
				num += faction.FactionAI.GetStationTypeBuildPriority(unitClass);
			}
			if (faction.GetCountOfStationPurpose(unitClass.StationPurpose) == 0)
			{
				num++;
			}
			int minCountOfStationPurpose = GetMinCountOfStationPurpose(faction.Engine, unitClass.StationPurpose);
			int factionTypeCountOfUnitClass = GetFactionTypeCountOfUnitClass(faction.Engine, unitClass, faction.FactionType);
			if (factionTypeCountOfUnitClass < minCountOfStationPurpose)
			{
				num += 100f;
			}
			if (unitClass.StationPurpose == StationPurpose.Factory)
			{
				num -= (float)GetFactionTypeCountOfUnitClass(faction.Engine, unitClass, faction.FactionType) * 10f;
			}
			if (faction.FactionType == FactionType.Trader && factionTypeCountOfUnitClass == 0 && unitClass.StationPurpose == StationPurpose.Factory)
			{
				num += 30f;
			}
			return num;
		}

		private static int GetPreferredDefensiveTurretsCount(Faction faction, UnitClass unitClass)
		{
			int num = 5;
			return Mathf.RoundToInt((1f - faction.AISettings.OffensiveStance) * faction.AISettings.PreferenceToBuildTurrets * (float)num);
		}

		public static bool CanFactionHaveStation(Faction faction, UnitClass unitClass, int? availableCredits, bool considerShipRatio)
		{
			if (faction.IsPlayerFaction)
			{
				return false;
			}
			if (unitClass.StationPurpose == StationPurpose.SectorControl)
			{
				return false;
			}
			if (availableCredits.HasValue && availableCredits.Value - unitClass.AICreditsReserve < unitClass.SaleCost)
			{
				return false;
			}
			if (!faction.FactionAI.CanBuildStation(unitClass))
			{
				return false;
			}
			if (!unitClass.Legal && !faction.TradeIllegalGoods)
			{
				return false;
			}
			int preferredCountOfUnitClass = faction.FactionAI.GetPreferredCountOfUnitClass(unitClass);
			if (preferredCountOfUnitClass == 0)
			{
				return false;
			}
			if (faction.GetCountOfStationPurpose(unitClass.StationPurpose) >= preferredCountOfUnitClass)
			{
				return false;
			}
			if (unitClass.StationPurpose == StationPurpose.Defence)
			{
				Unit bestStationToDefend = GetBestStationToDefend(faction);
				if (bestStationToDefend == null || GetDefensiveTurretCountAroundUnit(bestStationToDefend) > GetPreferredDefensiveTurretsCount(faction, unitClass))
				{
					return false;
				}
			}
			else if (considerShipRatio && !DoesStationTypeSatisfyShipRatio(faction))
			{
				return false;
			}
			return true;
		}

		private static bool DoesStationTypeSatisfyShipRatio(Faction faction)
		{
			int countOfUnitType = faction.GetCountOfUnitType(UnitType.Ship);
			int validNonMinorStationCount = faction.GetValidNonMinorStationCount();
			float num = Mathf.Lerp(20f, 0.5f, faction.AISettings.PreferenceToBuildStations);
			if (validNonMinorStationCount != 0)
			{
				return (float)countOfUnitType > (float)validNonMinorStationCount * num;
			}
			return true;
		}

		private static void GetPossibleStationsToBuild(EngineASX engine, Faction faction, StationPurpose? stationPurposeFilter, int? availableCredits, PriorityQueue<StationBuild, float> stationPriorityQueue, bool considerShipRatio, int maxBuildDistanceFromHomeSector)
		{
			foreach (UnitClass item in engine.UnitClasses.Where((UnitClass e) => CanBuildStation(engine, stationPurposeFilter, e)))
			{
				if (!CanFactionHaveStation(faction, item, availableCredits, considerShipRatio))
				{
					continue;
				}
				Sector bestSectorForNewStation = GetBestSectorForNewStation(engine, faction, item, maxBuildDistanceFromHomeSector);
				if (bestSectorForNewStation != null)
				{
					Vector3? newStationSectorPosition = GetNewStationSectorPosition(item, bestSectorForNewStation, faction);
					if (newStationSectorPosition.HasValue && !HostileStationsWithinDistanceOfSectorPosition(faction, bestSectorForNewStation, newStationSectorPosition.Value))
					{
						float factionStationPriority = GetFactionStationPriority(faction, item, bestSectorForNewStation);
						StationBuild value = new StationBuild
						{
							Faction = faction,
							SectorPosition = newStationSectorPosition.Value,
							Sector = bestSectorForNewStation,
							Weight = factionStationPriority,
							UnitClass = item
						};
						stationPriorityQueue.Enqueue(value, factionStationPriority);
					}
				}
			}
		}

		private static bool HostileStationsWithinDistanceOfSectorPosition(Faction faction, Sector sector, Vector3 sectorPosition, float distance = 1500f)
		{
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item.IsValidAndNotDestroyed && item.Faction != null && faction.Intel.IsUnitDiscovered(item) && item.IsHostileToOrAlwaysHostileToTwoWay(faction) && Vector3.Distance(item.SectorPosition, sectorPosition) < distance)
					{
						return true;
					}
				}
			}
			return false;
		}

		public static bool CanBuildStation(EngineASX engine, StationPurpose? stationPurposeFilter, UnitClass unitClass)
		{
			if (unitClass.IsUsable && unitClass.SeedInSandbox && unitClass.StationPurpose != StationPurpose.SectorControl && unitClass.UnitType == UnitType.Station && unitClass.StationPurpose != StationPurpose.None)
			{
				if (stationPurposeFilter.HasValue)
				{
					return (stationPurposeFilter.Value & unitClass.StationPurpose) != 0;
				}
				return true;
			}
			return false;
		}

		private static int GetFactionTypeCountOfUnitClass(EngineASX engine, UnitClass unitClass, FactionType factionType)
		{
			if (unitClass.StationPurpose != StationPurpose.Factory)
			{
				return GetFactionTypeCountOfStationPurpose(unitClass.StationPurpose, factionType);
			}
			return GetFactionTypeCountOfUnitClass(unitClass, factionType);
		}

		public static int GetFactionTypeCountOfUnitClass(UnitClass unitClass, FactionType factionType)
		{
			List<Unit> unitsByClass = EngineASX.Instance.GetUnitsByClass(unitClass);
			if (unitsByClass != null)
			{
				int num = 0;
				{
					foreach (Unit item in unitsByClass)
					{
						if (item.Faction != null && item.Faction.FactionType == factionType)
						{
							num++;
						}
					}
					return num;
				}
			}
			return 0;
		}

		public static int GetFactionTypeCountOfStationPurpose(StationPurpose stationPurpose, FactionType factionType)
		{
			List<Unit> unitsbyStationPurpose = EngineASX.Instance.GetUnitsbyStationPurpose(stationPurpose);
			if (unitsbyStationPurpose != null)
			{
				int num = 0;
				{
					foreach (Unit item in unitsbyStationPurpose)
					{
						if (item.Faction != null && item.Faction.FactionType == factionType)
						{
							num++;
						}
					}
					return num;
				}
			}
			return 0;
		}

		public static int GetMinCountOfStationPurpose(EngineASX engine, StationPurpose stationPurpose)
		{
			switch (stationPurpose)
			{
			case StationPurpose.Defence:
				return 0;
			case StationPurpose.TradeStation:
				return 1;
			case StationPurpose.Refinery:
				return 1;
			case StationPurpose.Factory:
			case StationPurpose.Scrapyard:
				return 0;
			case StationPurpose.Shipyard:
				return 1;
			case StationPurpose.Equipment:
			case StationPurpose.Outpost:
				return 1;
			default:
				return 0;
			}
		}
	}
}
