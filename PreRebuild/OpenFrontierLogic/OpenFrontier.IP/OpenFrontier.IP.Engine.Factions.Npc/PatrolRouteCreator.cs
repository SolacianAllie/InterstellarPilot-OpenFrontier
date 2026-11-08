using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OpenFrontier.IP.Engine.Factions.Npc
{
	public static class PatrolRouteCreator
	{
		public static PatrolOrder CreatePatrolObjective(Faction faction, Sector startSector, int minPatrolSectorsCount, int maxPatrolSectorsCount, int minNodesInScene, int maxNodesInScene, int maxJumpDistFromStartSector, bool considerStationsAsNodes, Func<Unit, bool> stationNodeChecker = null)
		{
			int minInclusive = Mathf.Max(minPatrolSectorsCount, 1);
			int num = Mathf.Max(minPatrolSectorsCount, maxPatrolSectorsCount);
			int num2 = UnityEngine.Random.Range(minInclusive, num + 1);
			PatrolOrder patrolOrder = UnityObjectHelper.NewGameObject<PatrolOrder>();
			patrolOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
			patrolOrder.IsLooping = true;
			patrolOrder.IsLoop = false;
			Sector sector = startSector;
			List<Sector> list = new List<Sector>();
			for (int i = 0; i < num2; i++)
			{
				int num3 = UnityEngine.Random.Range(minNodesInScene, maxNodesInScene + 1);
				List<Vector3> possibleSectorPosition = GetPossibleSectorPosition(sector, faction, num3, considerStationsAsNodes, stationNodeChecker);
				for (int j = 0; j < num3; j++)
				{
					AIPatrolPathNode aIPatrolPathNode = new AIPatrolPathNode();
					aIPatrolPathNode.Sector = sector;
					int index = UnityEngine.Random.Range(0, possibleSectorPosition.Count);
					aIPatrolPathNode.SectorPosition = possibleSectorPosition[index];
					possibleSectorPosition.RemoveAt(index);
					patrolOrder.Nodes.Add(aIPatrolPathNode);
				}
				PopulatePatrolNeighborCache(faction, sector, list, maxJumpDistFromStartSector);
				if (list.Count <= 0)
				{
					break;
				}
				sector = list.GetRandom();
			}
			return patrolOrder;
		}

		public static List<Vector3> GetPossibleSectorPosition(Sector sector, Faction faction, int count, bool considerStationsAsNodes, Func<Unit, bool> stationNodeChecker = null)
		{
			List<Unit> list = null;
			if (considerStationsAsNodes)
			{
				list = GetKnownFriendlyStationsInSector(sector, faction, stationNodeChecker);
			}
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Wormhole);
			List<Vector3> list2 = new List<Vector3>();
			List<Unit> list3 = null;
			if (unitsByType != null)
			{
				list3 = unitsByType.Where((Unit e) => faction.Intel.IsUnitDiscoveredOrOwned(e)).ToList();
			}
			for (int num = 0; num < count; num++)
			{
				if (UnityEngine.Random.value < 0.6f && list != null && list.Count > 0)
				{
					int index = UnityEngine.Random.Range(0, list.Count);
					list2.Add(list[index].SectorPosition + Geometry.RandomXZUnitVector() * 300f);
					list.RemoveAt(index);
				}
				else if (unitsByType != null && list3.Count > 0)
				{
					int index2 = UnityEngine.Random.Range(0, list3.Count);
					Unit unit = list3[index2];
					list3.RemoveAt(index2);
					list2.Add(unit.SectorPosition + unit.transform.forward * 350f);
				}
				else
				{
					Vector3 checkSectorPosition = Maths.RandomXZDirection() * UnityEngine.Random.Range(0f, sector.GetActualGateDistance() * 0.75f);
					checkSectorPosition = PhysicsNonOverlappingPositionFinder.FindSectorPosition(sector, checkSectorPosition, 100f, GameController.Instance.StaticNonOverlappingMask);
					list2.Add(checkSectorPosition);
				}
			}
			return list2;
		}

		private static void PopulatePatrolNeighborCache(Faction faction, Sector sector, List<Sector> cache, int maxJumpDistFromStartScene)
		{
			cache.Clear();
			foreach (SectorNeighbour neighbour in sector.Neighbours)
			{
				if (!neighbour.IsStableConnection)
				{
					continue;
				}
				Sector sector2 = neighbour.Sector;
				if (faction.Intel.CanTraverseIntoNeighbour(neighbour))
				{
					bool flag = maxJumpDistFromStartScene < 0 || sector.GetJumpDistanceTo(sector2) <= maxJumpDistFromStartScene;
					if (flag && flag)
					{
						cache.Add(sector2);
					}
				}
			}
		}

		private static List<Unit> GetKnownFriendlyStationsInSector(Sector sector, Faction faction, Func<Unit, bool> stationNodeChecker = null)
		{
			return sector.GetUnitsByType(UnitType.Station)?.Where((Unit e) => e != null && e.IsDockable && e.Faction != null && faction.Intel.IsUnitDiscoveredOrOwned(e) && (stationNodeChecker == null || stationNodeChecker(e)) && !e.IsHostileToOrAlwaysHostileToTwoWay(faction) && faction.GetOpinion(e.Faction) >= 0f).ToList();
		}
	}
}
