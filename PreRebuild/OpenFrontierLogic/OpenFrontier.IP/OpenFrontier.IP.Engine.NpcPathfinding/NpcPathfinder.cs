using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.NpcPathfinding
{
	public class NpcPathfinder
	{
		private struct SectorPathNode
		{
			public int PreviousNodeIndex;

			public int Index;

			public int CellX;

			public int CellZ;

			public float TotalCost;

			public WorldPoint Movement;

			public SectorPathNode(int index, int previousNodeIndex, int cellX, int cellY, float totalCost, WorldPoint movement)
			{
				this = default;
				Index = index;
				PreviousNodeIndex = previousNodeIndex;
				CellX = cellX;
				CellZ = cellY;
				TotalCost = totalCost;
				Movement = movement;
			}
		}

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

		private static HashSet<int> visitedSectorCellIds = new HashSet<int>(1000);

		private static PriorityQueue<SectorPathNode, float> sectorNodeQueue = new PriorityQueue<SectorPathNode, float>(1000);

		private static List<SectorPathNode> sectorPathNodes = new List<SectorPathNode>(1000);

		private const float blockedTargetCostThreshold = 0.5f;

		private static Queue<WorldPoint> queue = new Queue<WorldPoint>(20);

		public void GetPathSectorPositionsNonAlloc(Vector3 startSectorPosition, Vector3 endSectorPosition, Vector3 currentForward, Unit unit, Fleet fleet, List<Vector3> sectorPositions)
		{
			NpcPathfindingController npcPathfindingController = EngineASX.Instance.NpcPathfindingController;
			npcPathfindingController.GetCellIndicesFromSectorPosition(startSectorPosition, out var x, out var z);
			npcPathfindingController.GetCellIndicesFromSectorPosition(endSectorPosition, out var x2, out var z2);
			bool flag = npcPathfindingController.IsCellIndexInGrid(x2, z2);
			if (!npcPathfindingController.IsCellIndexInGrid(x, z))
			{
				Vector3? gridIntersectionSectorPosition = npcPathfindingController.GetGridIntersectionSectorPosition(startSectorPosition, endSectorPosition);
				if (!gridIntersectionSectorPosition.HasValue)
				{
					sectorPositions.Add(endSectorPosition);
					return;
				}
				npcPathfindingController.GetCellIndicesFromSectorPosition(gridIntersectionSectorPosition.Value, out x, out z);
				sectorPositions.Add(gridIntersectionSectorPosition.Value);
			}
			npcPathfindingController.ClampCellIndices(x, z, out x, out z);
			npcPathfindingController.ClampCellIndices(x2, z2, out x2, out z2);
			Vector3 directionOfTravel = Vector3.Normalize(endSectorPosition - startSectorPosition);
			if (npcPathfindingController.GetCost(unit, fleet, x, z, directionOfTravel) > 0.5f)
			{
				FindNearestPassableGridCell(x, z, unit, fleet, npcPathfindingController, out x, out z);
			}
			if (npcPathfindingController.GetCost(unit, fleet, x2, z2, directionOfTravel) > 0.5f)
			{
				FindNearestPassableGridCell(x2, z2, unit, fleet, npcPathfindingController, out x2, out z2);
			}
			SectorPathFind(unit, fleet, sectorPositions, EngineASX.Instance.ActiveSector, x, z, x2, z2, ref currentForward);
			if (!flag)
			{
				sectorPositions.Add(endSectorPosition);
			}
		}

		private static void FindNearestPassableGridCell(int sx, int sz, Unit unit, Fleet fleet, NpcPathfindingController npcPathfindingController, out int resultX, out int resultZ)
		{
			visitedSectorCellIds.Clear();
			queue.Clear();
			queue.Enqueue(new WorldPoint(sx, sz));
			while (queue.Count > 0)
			{
				WorldPoint worldPoint = queue.Dequeue();
				int cellIndex = npcPathfindingController.GetCellIndex(worldPoint.X, worldPoint.Z);
				visitedSectorCellIds.Add(cellIndex);
				if (npcPathfindingController.GetCost(unit, fleet, worldPoint.X, worldPoint.Z, Vector3.forward) < 0.5f)
				{
					resultX = worldPoint.X;
					resultZ = worldPoint.Z;
					return;
				}
				for (int i = 0; i < sectorCellMoves.Length; i++)
				{
					int x = sectorCellMoves[i].X;
					int z = sectorCellMoves[i].Z;
					int x2 = worldPoint.X + x;
					int z2 = worldPoint.Z + z;
					if (npcPathfindingController.IsCellIndexInGrid(x2, z2))
					{
						int cellIndex2 = npcPathfindingController.GetCellIndex(x2, z2);
						if (!visitedSectorCellIds.Contains(cellIndex2))
						{
							queue.Enqueue(new WorldPoint(x2, z2));
						}
					}
				}
			}
			resultX = sx;
			resultZ = sz;
		}

		private static void SectorPathFind(Unit unit, Fleet fleet, List<Vector3> navpoints, Sector sector, int startCellX, int startCellZ, int endCellX, int endCellZ, ref Vector3 currentForward)
		{
			NpcPathfindingController npcPathfindingController = EngineASX.Instance.NpcPathfindingController;
			sectorNodeQueue.Clear();
			sectorPathNodes.Clear();
			visitedSectorCellIds.Clear();
			int cellIndex = npcPathfindingController.GetCellIndex(startCellX, startCellZ);
			int cellIndex2 = npcPathfindingController.GetCellIndex(endCellX, endCellZ);
			if (cellIndex == cellIndex2)
			{
				return;
			}
			Vector3 cellCenterSectorPosition = npcPathfindingController.GetCellCenterSectorPosition(endCellX, endCellZ);
			WorldPoint movement = new WorldPoint(0, 0);
			GetSimplifiedMovement(ref currentForward, out movement.X, out movement.Z);
			QueueSectorNode(startCellX, startCellZ, -1, 0f, movement, npcPathfindingController);
			bool optimizeNpcPathfindingPaths = GameController.Instance.GameSettings.DebugSettings.OptimizeNpcPathfindingPaths;
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
					if (!npcPathfindingController.IsCellPassable(num3, num4))
					{
						continue;
					}
					int cellIndex3 = npcPathfindingController.GetCellIndex(num3, num4);
					if (!npcPathfindingController.IsCellIndexInGrid(num3, num4))
					{
						continue;
					}
					if (num3 == endCellX && num4 == endCellZ)
					{
						int count = navpoints.Count;
						WorldPoint worldPoint2 = worldPoint;
						while (sectorPathNode.PreviousNodeIndex > -1)
						{
							SectorPathNode sectorPathNode2 = sectorPathNodes[sectorPathNode.PreviousNodeIndex];
							if (!optimizeNpcPathfindingPaths || sectorPathNode.Movement.X != sectorPathNode2.Movement.X || sectorPathNode.Movement.Z != sectorPathNode2.Movement.Z || sectorPathNode.Movement.X != worldPoint2.X || sectorPathNode.Movement.Z != worldPoint2.Z)
							{
								Vector3 cellCenterSectorPosition2 = npcPathfindingController.GetCellCenterSectorPosition(sectorPathNode.CellX, sectorPathNode.CellZ);
								navpoints.Insert(count, cellCenterSectorPosition2);
							}
							worldPoint2 = sectorPathNode.Movement;
							sectorPathNode = sectorPathNode2;
						}
						navpoints.Add(npcPathfindingController.GetCellCenterSectorPosition(endCellX, endCellZ));
						return;
					}
					if (!visitedSectorCellIds.Contains(cellIndex3))
					{
						Vector2 vector = new Vector2(endCellX, endCellZ) - new Vector2(sectorPathNode.CellX, sectorPathNode.CellZ);
						Vector2 normalized = new Vector2(worldPoint.X, worldPoint.Z).normalized;
						float num5 = Vector2.Dot(vector.normalized, normalized);
						float pathfindBaseMoveCost = npcPathfindingController.PathfindBaseMoveCost;
						pathfindBaseMoveCost += (2f - (num5 + 1f)) / 2f * npcPathfindingController.PathfindWrongDirectionCost;
						WorldPoint movement2 = sectorPathNode.Movement;
						float num6 = npcPathfindingController.PathfindDirectionChangeCost;
						if (sectorPathNode.PreviousNodeIndex == -1)
						{
							num6 *= 8f;
						}
						pathfindBaseMoveCost += (float)Mathf.Abs(worldPoint.X - movement2.X) * num6;
						pathfindBaseMoveCost += (float)Mathf.Abs(worldPoint.Z - movement2.Z) * num6;
						int num7 = Mathf.Abs(num3 - endCellX) + Mathf.Abs(num4 - endCellZ);
						pathfindBaseMoveCost += npcPathfindingController.GetCost(unit, fleet, num3, num4, currentForward) * npcPathfindingController.PathfindBlockedCost;
						QueueSectorNode(num3, num4, sectorPathNode.Index, (float)num7 + pathfindBaseMoveCost, worldPoint, npcPathfindingController);
					}
				}
			}
			Debug.LogError($"Pathfinding failure. Total nodes visited: {visitedSectorCellIds.Count}. Start: {startCellX},{startCellZ} End: {endCellX},{endCellZ} Unit: {unit}");
		}

		private static void QueueSectorNode(int cellX, int cellZ, int previousNodeIndex, float totalCost, WorldPoint movement, NpcPathfindingController pathfindingController)
		{
			SectorPathNode sectorPathNode = new SectorPathNode(sectorPathNodes.Count, previousNodeIndex, cellX, cellZ, totalCost, movement);
			int cellIndex = pathfindingController.GetCellIndex(cellX, cellZ);
			sectorPathNodes.Add(sectorPathNode);
			visitedSectorCellIds.Add(cellIndex);
			sectorNodeQueue.Enqueue(sectorPathNode, 0f - totalCost);
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
