using System.Collections.Generic;
using OpenFrontier.IP.Engine.Pathfinding;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions.Intel
{
	public static class IntelUniversePathCalculator
	{
		private struct UniversePathCalcNode
		{
			public int Jumps;

			public Sector Sector;

			public int PreviousNodeIndex;

			public int Index;

			public Wormhole Wormhole;

			public UniversePathCalcNode(Wormhole wormhole, Sector sector, int jumps, int index, int previousNodeIndex)
			{
				Wormhole = wormhole;
				Sector = sector;
				Jumps = jumps;
				PreviousNodeIndex = previousNodeIndex;
				Index = index;
			}
		}

		private static HashSet<int> visitedSectors = new HashSet<int>(20);

		private static PriorityQueue<UniversePathCalcNode, float> nodeQueue = new PriorityQueue<UniversePathCalcNode, float>(20);

		private static List<UniversePathCalcNode> nodes = new List<UniversePathCalcNode>(20);

		public static UniversePath Calculate(FactionIntel intel, Sector s1, Sector s2, bool includeUnstableWormholes = false)
		{
			if (!EngineASX.Instance.AreSectorsConnectedByStableWormholes(s1, s2))
			{
				return null;
			}
			if (!intel.IsSectorDiscovered(s1) || !intel.IsSectorDiscovered(s2))
			{
				return null;
			}
			if (s1 == s2)
			{
				return new UniversePath
				{
					Jumps = 0,
					Nodes = 
					{
						new UniversePathNode
						{
							Wormhole = null,
							Sector = s1
						}
					}
				};
			}
			visitedSectors.Clear();
			nodeQueue.Clear();
			nodes.Clear();
			UniversePathCalcNode universePathCalcNode = new UniversePathCalcNode(null, s1, 0, 0, -1);
			nodeQueue.Enqueue(universePathCalcNode, 0f);
			nodes.Add(universePathCalcNode);
			Vector3 targetPosition = s2.MapPosition;
			while (nodeQueue.Count > 0)
			{
				PriorityQueueItem<UniversePathCalcNode, float> priorityQueueItem = nodeQueue.Dequeue();
				UniversePathCalcNode universePathCalcNode2 = priorityQueueItem.Value;
				float num = 0f - priorityQueueItem.Priority;
				if (universePathCalcNode2.Sector == s2)
				{
					UniversePath universePath = new UniversePath
					{
						Jumps = universePathCalcNode2.Jumps
					};
					do
					{
						UniversePathNode item = new UniversePathNode
						{
							Sector = universePathCalcNode2.Sector,
							Wormhole = universePathCalcNode2.Wormhole
						};
						universePath.Nodes.Insert(0, item);
						universePathCalcNode2 = nodes[universePathCalcNode2.PreviousNodeIndex];
					}
					while (universePathCalcNode2.PreviousNodeIndex > -1);
					universePath.Nodes.Insert(0, new UniversePathNode
					{
						Wormhole = null,
						Sector = s1
					});
					return universePath;
				}
				Vector3 ourPosition = universePathCalcNode2.Sector.MapPosition;
				foreach (SectorNeighbour neighbour in universePathCalcNode2.Sector.Neighbours)
				{
					if ((neighbour.IsStableConnection || includeUnstableWormholes) && !visitedSectors.Contains(neighbour.Sector.UniqueId) && intel.CanTraverseIntoNeighbour(neighbour, neighbour.Sector != s2))
					{
						Vector3 proposedPosition = neighbour.Sector.MapPosition;
						int num2 = AdvPathfinder.GetSimpleCostOfMovement(ref ourPosition, ref targetPosition, ref proposedPosition) * 8;
						UniversePathCalcNode universePathCalcNode3 = new UniversePathCalcNode(neighbour.ConnectingGate, neighbour.Sector, universePathCalcNode2.Jumps + 1, nodes.Count, universePathCalcNode2.Index);
						nodeQueue.Enqueue(universePathCalcNode3, 0f - (num + (float)num2));
						nodes.Add(universePathCalcNode3);
						visitedSectors.Add(neighbour.Sector.UniqueId);
					}
				}
			}
			return null;
		}
	}
}
