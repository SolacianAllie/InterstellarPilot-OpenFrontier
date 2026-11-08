using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.Intel;
using OpenFrontier.IP.Engine.Heatmaps;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Pathfinding
{
	public static class AdvPathfinder
	{
		private struct WorldPoint
		{
			public int X;

			public int Z;

			public WorldPoint(int x, int z)
			{
				this = default;
				X = x;
				Z = z;
			}
		}

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

			public int Index;

			public int CellX;

			public int CellZ;

			public float TotalCost;

			public SectorPathNode(int index, int previousNodeIndex, int cellX, int cellY, float totalCost)
			{
				this = default;
				Index = index;
				PreviousNodeIndex = previousNodeIndex;
				CellX = cellX;
				CellZ = cellY;
				TotalCost = totalCost;
			}
		}

		private static PriorityQueue<SectorPathNode, float> sectorNodeQueue = new PriorityQueue<SectorPathNode, float>();

		private static List<SectorPathNode> sectorPathNodes = new List<SectorPathNode>(100);

		private static HashSet<int> visitedSectorCellIds = new HashSet<int>(128);

		private static WorldPoint[] sectorCellMoves = new WorldPoint[8]
		{
			new WorldPoint(-1, 1),
			new WorldPoint(0, 1),
			new WorldPoint(1, 1),
			new WorldPoint(1, 0),
			new WorldPoint(1, -1),
			new WorldPoint(0, -1),
			new WorldPoint(-1, -1),
			new WorldPoint(-1, 0)
		};

		public static PathfindResult Pathfind(Sector startSector, Vector3 startSectorPosition, Sector endSector, Vector3 endSectorPosition, Faction faction, bool ignoreHeat, List<WorldNavpoint> navpoints)
		{
			FactionIntel intel = faction.Intel;
			FactionHeatmap heatmap = faction.Heatmap;
			if (heatmap != null)
			{
				_ = heatmap.CellSize;
			}
			if (startSector == endSector && (ignoreHeat || heatmap == null || !heatmap.HasHeatForSector(startSector)))
			{
				WorldNavpoint item = WorldNavpoint.FromSectorPosition(endSector, endSectorPosition);
				navpoints.Add(item);
				return PathfindResult.Success(0, Vector3.Distance(startSectorPosition, endSectorPosition));
			}
			if (startSector == endSector)
			{
				WorldNavpoint item2 = WorldNavpoint.FromSectorPosition(endSector, endSectorPosition);
				navpoints.Add(item2);
				float totalDistanceOfNavpoints = GetTotalDistanceOfNavpoints(navpoints, startSectorPosition);
				return PathfindResult.Success(0, totalDistanceOfNavpoints);
			}
			UniversePath universePath = intel.GetUniversePath(startSector, endSector);
			if (universePath == null)
			{
				return PathfindResult.Failure;
			}
			Vector3 vector = startSectorPosition;
			for (int i = 0; i < universePath.Nodes.Count; i++)
			{
				UniversePathNode universePathNode = universePath.Nodes[i];
				Sector sector = universePathNode.Sector;
				if (i > 0)
				{
					vector = universePathNode.Wormhole.GetTargetSectorPosition();
					navpoints.Add(WorldNavpoint.FromSectorPosition(sector, vector));
				}
				if (sector != endSector)
				{
					AddGateNavpoint(universePath.Nodes[i + 1].Wormhole, navpoints);
				}
				else
				{
					AddSectorPositionNavpoint(endSector, endSectorPosition, navpoints);
				}
			}
			float totalDistanceOfNavpoints2 = GetTotalDistanceOfNavpoints(navpoints, startSectorPosition);
			return PathfindResult.Success(universePath.Jumps, totalDistanceOfNavpoints2);
		}

		private static void SectorPathFind(List<WorldNavpoint> navpoints, FactionHeatmap heatmap, Sector sector, ref Vector3 currentSectorPosition, ref Vector3 targetSectorPosition)
		{
			sectorNodeQueue.Clear();
			sectorPathNodes.Clear();
			visitedSectorCellIds.Clear();
			heatmap.GetSectorCoordinatesFromLocalPosition(currentSectorPosition, out var x, out var z);
			heatmap.GetSectorCoordinatesFromLocalPosition(targetSectorPosition, out var x2, out var z2);
			int sectorCellId = heatmap.GetSectorCellId(x, z);
			int sectorCellId2 = heatmap.GetSectorCellId(x2, z2);
			float arrivalThreshold = (float)heatmap.CellSize / 2f;
			if (sectorCellId == sectorCellId2)
			{
				WorldNavpoint item = WorldNavpoint.FromSectorPosition(sector, targetSectorPosition);
				item.ArrivalThreshold = arrivalThreshold;
				navpoints.Add(item);
				return;
			}
			Vector3 movement = targetSectorPosition - currentSectorPosition;
			GetSimplifiedMovement(ref movement, out var x3, out var z3);
			QueueSectorNode(x, z, -1, 0f, heatmap);
			int num = 100000;
			int num2 = 0;
			while (sectorNodeQueue.Count > 0)
			{
				num2++;
				if (num2 >= num)
				{
					throw new Exception("Infinite loop detected in pathfinder");
				}
				SectorPathNode sectorPathNode = sectorNodeQueue.Dequeue().Value;
				WorldPoint[] array = sectorCellMoves;
				for (int i = 0; i < array.Length; i++)
				{
					WorldPoint worldPoint = array[i];
					int num3 = sectorPathNode.CellX + worldPoint.X;
					int num4 = sectorPathNode.CellZ + worldPoint.Z;
					if (!heatmap.AreCoordinatesValid(num3, num4))
					{
						continue;
					}
					int sectorCellId3 = heatmap.GetSectorCellId(num3, num4);
					if (sectorCellId3 == sectorCellId2)
					{
						int count = navpoints.Count;
						WorldNavpoint item2 = WorldNavpoint.FromSectorPosition(sector, targetSectorPosition);
						item2.ArrivalThreshold = arrivalThreshold;
						navpoints.Insert(count, item2);
						while (sectorPathNode.PreviousNodeIndex > -1)
						{
							WorldNavpoint item3 = WorldNavpoint.FromSectorPosition(sector, heatmap.GetCellCenterLocalPosition(sectorPathNode.CellX, sectorPathNode.CellZ));
							item3.ArrivalThreshold = arrivalThreshold;
							navpoints.Insert(count, item3);
							sectorPathNode = sectorPathNodes[sectorPathNode.PreviousNodeIndex];
						}
						return;
					}
					if (!visitedSectorCellIds.Contains(sectorCellId3))
					{
						float num5 = GetSimpleCostOfMovement(worldPoint.X, worldPoint.Z, x3, z3);
						num5 += heatmap.GetHeat(sector, num3, num4) * 100f;
						QueueSectorNode(num3, num4, sectorPathNode.Index, sectorPathNode.TotalCost + num5, heatmap);
					}
				}
			}
		}

		private static void QueueSectorNode(int cellX, int cellZ, int previousNodeIndex, float totalCost, FactionHeatmap heatmap)
		{
			SectorPathNode sectorPathNode = new SectorPathNode(sectorPathNodes.Count, previousNodeIndex, cellX, cellZ, totalCost);
			int sectorCellId = heatmap.GetSectorCellId(cellX, cellZ);
			sectorPathNodes.Add(sectorPathNode);
			visitedSectorCellIds.Add(sectorCellId);
			sectorNodeQueue.Enqueue(sectorPathNode, 0f - totalCost);
		}

		private static void AddSectorPositionNavpoint(Sector sector, Vector3 sectorPosition, List<WorldNavpoint> navpoints)
		{
			navpoints.Add(WorldNavpoint.FromSectorPosition(sector, sectorPosition));
		}

		private static void AddGateNavpoint(Wormhole jumpGate, List<WorldNavpoint> navpoints)
		{
			WorldNavpoint item = WorldNavpoint.FromSectorPosition(jumpGate.Sector, jumpGate.Unit.SectorPosition);
			item.TargetType = WorldNavpointTargetType.Gate;
			item.TargetSectorObject = jumpGate.Unit;
			navpoints.Add(item);
		}

		private static float GetTotalDistanceOfNavpoints(List<WorldNavpoint> navpoints, Vector3 startSectorPosition)
		{
			Sector sector = navpoints[0].Sector;
			Vector3 a = startSectorPosition;
			float num = 0f;
			foreach (WorldNavpoint navpoint in navpoints)
			{
				if (navpoint.GetTargetSector() == sector)
				{
					num += Vector3.Distance(a, navpoint.GetTargetSectorPosition());
					continue;
				}
				sector = navpoint.GetTargetSector();
				a = navpoint.GetTargetSectorPosition();
			}
			return num;
		}

		public static int GetSimpleCostOfMovement(ref Vector3 ourPosition, ref Vector3 targetPosition, ref Vector3 proposedPosition)
		{
			Vector3 vector = targetPosition - ourPosition;
			Vector3 vector2 = proposedPosition - ourPosition;
			GetSimplifiedMovement(vector.x, vector.z, out var x, out var z);
			GetSimplifiedMovement(vector2.x, vector2.z, out var x2, out var z2);
			return GetSimpleCostOfMovement(x2, z2, x, z);
		}

		public static int GetSimpleCostOfMovement(int moveX, int moveZ, int desiredMoveX, int desiredMoveZ)
		{
			return Mathf.Abs(moveX - desiredMoveX) + Mathf.Abs(moveZ - desiredMoveZ);
		}

		public static void GetSimplifiedMovement(ref Vector3 movement, out int x, out int z)
		{
			x = 0;
			z = 0;
			GetSimplifiedMovement(movement.x, movement.z, out x, out z);
		}

		public static void GetSimplifiedMovement(int moveX, int moveZ, out int x, out int z)
		{
			x = 0;
			z = 0;
			if (moveX > 0)
			{
				x = 1;
			}
			else if (moveX < 0)
			{
				x = -1;
			}
			if (moveZ > 0)
			{
				z = 1;
			}
			else if (moveZ < 0)
			{
				z = -1;
			}
		}

		public static void GetSimplifiedMovement(float moveX, float moveZ, out int x, out int z)
		{
			x = 0;
			z = 0;
			if (moveX > 0.001f)
			{
				x = 1;
			}
			else if (moveX < -0.001f)
			{
				x = -1;
			}
			if (moveZ > 0.001f)
			{
				z = 1;
			}
			else if (moveZ < -0.001f)
			{
				z = -1;
			}
		}
	}
}
