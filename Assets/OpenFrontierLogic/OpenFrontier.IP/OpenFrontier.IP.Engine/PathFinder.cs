using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class PathFinder : MonoBehaviour
	{
		public struct PathfindResult
		{
			public int TotalJumps;

			public bool IsSuccess;

			public float TotalDistance;

			public static PathfindResult Failure => new PathfindResult
			{
				IsSuccess = false,
				TotalJumps = -1
			};

			public static PathfindResult Success(int jumps, float distance)
			{
				return new PathfindResult
				{
					IsSuccess = true,
					TotalJumps = jumps,
					TotalDistance = distance
				};
			}
		}

		private struct SectorPathNode
		{
			public int PreviousNodeIndex;

			public Sector Sector;

			public int Index;

			public int TotalJumps;

			public SectorPathNode(int index, int previousNodeIndex, Sector scene, int totalJumps)
			{
				this = default;
				Index = index;
				PreviousNodeIndex = previousNodeIndex;
				Sector = scene;
				TotalJumps = totalJumps;
			}
		}

		private static PriorityQueue<SectorPathNode, float> gateQueue = new PriorityQueue<SectorPathNode, float>();

		private static List<SectorPathNode> pathNodes = new List<SectorPathNode>(100);

		private static HashSet<int> visitedSectorIds = new HashSet<int>();

		private bool DisconnectedGraphErrorShow;

		public bool PathfindIgnoringIntel(Sector startSector, Vector3 startSectorPosition, Sector endSector, Vector3 endSectorPosition, List<WorldNavpoint> navpoints, bool includeUnstableWormholes = false)
		{
			if (startSector == endSector)
			{
				WorldNavpoint item = WorldNavpoint.FromSectorPosition(endSector, endSectorPosition);
				navpoints.Add(item);
				return true;
			}
			if (!EngineASX.Instance.AreSectorsConnectedByStableWormholes(startSector, endSector))
			{
				return false;
			}
			gateQueue.Clear();
			pathNodes.Clear();
			visitedSectorIds.Clear();
			SectorPathNode sectorPathNode = new SectorPathNode(0, -1, startSector, 0);
			pathNodes.Add(sectorPathNode);
			gateQueue.Enqueue(sectorPathNode, 0f);
			while (gateQueue.Count > 0)
			{
				PriorityQueueItem<SectorPathNode, float> priorityQueueItem = gateQueue.Dequeue();
				float priority = priorityQueueItem.Priority;
				visitedSectorIds.Add(priorityQueueItem.Value.Sector.UniqueId);
				foreach (SectorNeighbour neighbour in priorityQueueItem.Value.Sector.Neighbours)
				{
					if ((!neighbour.IsStableConnection && !includeUnstableWormholes) || visitedSectorIds.Contains(neighbour.Sector.UniqueId))
					{
						continue;
					}
					SectorPathNode sectorPathNode2 = new SectorPathNode(pathNodes.Count, priorityQueueItem.Value.Index, neighbour.Sector, priorityQueueItem.Value.TotalJumps + 1);
					pathNodes.Add(sectorPathNode2);
					if (sectorPathNode2.Sector == endSector)
					{
						navpoints.Insert(0, WorldNavpoint.FromSectorPosition(endSector, endSectorPosition));
						do
						{
							Sector sector = pathNodes[sectorPathNode2.PreviousNodeIndex].Sector;
							Wormhole connectingGate = pathNodes[sectorPathNode2.Index].Sector.GetConnectingGate(sector);
							Wormhole connectingGate2 = sector.GetConnectingGate(sectorPathNode2.Sector);
							Vector3 safeTargetSectorPosition = connectingGate2.GetSafeTargetSectorPosition(0f);
							navpoints.Insert(0, WorldNavpoint.FromSectorPosition(connectingGate.Sector, safeTargetSectorPosition));
							WorldNavpoint item2 = WorldNavpoint.FromSectorPosition(sector, connectingGate2.Unit.SectorPosition);
							item2.TargetType = WorldNavpointTargetType.Gate;
							item2.TargetSectorObject = connectingGate2.Unit;
							navpoints.Insert(0, item2);
							sectorPathNode2 = pathNodes[sectorPathNode2.PreviousNodeIndex];
						}
						while (sectorPathNode2.PreviousNodeIndex > -1);
						return true;
					}
					gateQueue.Enqueue(sectorPathNode2, priority - 1f);
				}
			}
			if (!DisconnectedGraphErrorShow)
			{
				Debug.LogWarning($"Could not calculate a path from scenes \"{startSector}\" to \"{endSector}\". Scene connections are stale and need recompiling or the world graph is disconnected", this);
				DisconnectedGraphErrorShow = true;
			}
			return false;
		}
	}
}
