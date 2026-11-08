using OpenFrontier.IP.Engine.WorldGeneration.Models;
using UnityEngine;

namespace OpenFrontier.IP.Engine.NpcPathfinding
{
	public class NpcPathfindingController : MonoBehaviour
	{
		public struct CellInfo
		{
			public Unit BlockingUnit;

			public Vector3 BlockingUnitVelocity;

			public Fleet BlockingFleet;

			public float Cost;

			public bool IsPassable;
		}

		public float PathfindBaseMoveCost = 1f;

		public float PathfindDirectionChangeCost = 0.1f;

		public float PathfindWrongDirectionCost = 1f;

		public float PathfindBlockedCost = 3f;

		public float BlockingUnitEdgeCost = 0.5f;

		public NpcPathfinder NpcPathfinder = new NpcPathfinder();

		public int CellSize = 32;

		public int CellsAcross = 32;

		private CellInfo[] cells;

		private Vector3 gridBottomLeftSectorPosition = Vector3.zero;

		private Vector3 gridSize = Vector3.zero;

		private Vector3 gridSizeOverTwo = Vector3.zero;

		private Vector3 cellSizeOverTwo = Vector3.zero;

		private Collider[] colliderCache;

		public int MaxColliders = 100;

		public void Init()
		{
			cells = new CellInfo[CellsAcross * CellsAcross];
			gridSize = new Vector3(CellSize * CellsAcross, 0f, CellSize * CellsAcross);
			cellSizeOverTwo = new Vector3((float)CellSize / 2f, 0f, (float)CellSize / 2f);
			gridSizeOverTwo = gridSize / 2f;
			colliderCache = new Collider[MaxColliders];
		}

		public Vector3 GetCellCenterSectorPosition(int x, int z)
		{
			return GetCellBottomLeftSectorPosition(x, z) + cellSizeOverTwo;
		}

		public Vector3 GetCellBottomLeftSectorPosition(int x, int z)
		{
			return new Vector3(x * CellSize, 0f, z * CellSize) + gridBottomLeftSectorPosition;
		}

		public void Rebuild()
		{
			for (int i = 0; i < CellsAcross; i++)
			{
				for (int j = 0; j < CellsAcross; j++)
				{
					int num = j * CellsAcross + i;
					CellInfo cellInfo = cells[num];
					cellInfo.Cost = 0f;
					cellInfo.IsPassable = true;
					cellInfo.BlockingFleet = null;
					cellInfo.BlockingUnit = null;
					cellInfo.BlockingUnitVelocity = Vector3.zero;
					cells[num] = cellInfo;
				}
			}
			if (!(EngineASX.Instance.ActiveSector == null))
			{
				Vector3 position = GameController.Instance.MainCamera.transform.position;
				Vector3 vector = new Vector3((int)(position.x / (float)CellSize) * CellSize, 0f, (int)(position.z / (float)CellSize) * CellSize);
				Vector3 vector2 = EngineASX.Instance.ActiveSector.ToLocalPosition(vector);
				gridBottomLeftSectorPosition = vector2 - gridSizeOverTwo;
				float magnitude = new Vector2(gridSize.x, gridSize.z).magnitude;
				int num2 = Physics.OverlapSphereNonAlloc(vector, magnitude, colliderCache, GameController.Instance.NonOVerlappingUnitsMask, QueryTriggerInteraction.Ignore);
				for (int k = 0; k < num2; k++)
				{
					ApplyColliderToGrid(colliderCache[k]);
				}
			}
		}

		public void ApplyColliderToGrid(Collider collider)
		{
			Unit component = collider.GetComponent<Unit>();
			if (component == null)
			{
				return;
			}
			Fleet fleet = component.GetFleet();
			_ = component.UnitClass.ShieldRingRadius;
			Bounds bounds = collider.bounds;
			bool isStatic = component.IsStatic;
			GetCellIndicesFromWorldPosition(bounds.min, out var x, out var z);
			GetCellIndicesFromWorldPosition(bounds.max, out var x2, out var z2);
			for (int i = x - 1; i <= x2 + 1; i++)
			{
				for (int j = z - 1; j <= z2 + 1; j++)
				{
					if (i <= 0 || i >= CellsAcross || j < 0 || j >= CellsAcross)
					{
						continue;
					}
					CellInfo cellInfo = cells[j * CellsAcross + i];
					if (cellInfo.IsPassable)
					{
						float num = 1f;
						bool flag = true;
						if (i < x || i > x2 || j < z || j > z2)
						{
							num = BlockingUnitEdgeCost;
						}
						else
						{
							flag = !isStatic;
						}
						if (!isStatic)
						{
							num *= Mathf.Lerp(0.25f, 0.9f, Mathf.Clamp01(component.UnitClass.ShieldRingRadius / 30f));
						}
						if (num > cellInfo.Cost || !flag)
						{
							int cellIndex = GetCellIndex(i, j);
							cells[cellIndex] = new CellInfo
							{
								BlockingUnitVelocity = ((component != null) ? component.Velocity : Vector3.zero),
								BlockingUnit = component,
								BlockingFleet = fleet,
								Cost = num,
								IsPassable = flag
							};
						}
					}
				}
			}
		}

		public bool IsCellPassable(int sx, int sz)
		{
			if (!IsCellIndexInGrid(sx, sz))
			{
				return true;
			}
			return GetCellInfo(sx, sz).IsPassable;
		}

		public bool IsCellBlocked(int sx, int sz, Unit ourUnit)
		{
			if (!IsCellIndexInGrid(sx, sz))
			{
				return false;
			}
			CellInfo cellInfo = GetCellInfo(sx, sz);
			if (cellInfo.IsPassable)
			{
				if (cellInfo.BlockingUnit != null)
				{
					return cellInfo.BlockingUnit != ourUnit;
				}
				return false;
			}
			return true;
		}

		public int GetCellIndex(int x, int z)
		{
			return z * CellsAcross + x;
		}

		public void GetCellIndicesFromWorldPosition(Vector3 position, out int x, out int z)
		{
			GetCellIndicesFromSectorPosition(EngineASX.Instance.ActiveSector.ToLocalPosition(position), out x, out z);
		}

		public void GetCellIndicesFromSectorPosition(Vector3 sectorPosition, out int x, out int z)
		{
			Vector3 vector = (sectorPosition -= gridBottomLeftSectorPosition);
			x = (int)(vector.x / (float)CellSize);
			z = (int)(vector.z / (float)CellSize);
		}

		public int? GetCellIndexFromWorldPosition(Vector3 position)
		{
			GetCellIndicesFromWorldPosition(position, out var x, out var z);
			if (IsCellIndexInGrid(x, z))
			{
				return z * CellsAcross + x;
			}
			return null;
		}

		public CellInfo GetCellInfo(int cellX, int cellZ)
		{
			int num = cellZ * CellsAcross + cellX;
			return cells[num];
		}

		public float GetCost(Unit unit, Fleet fleet, int newCellX, int newCellZ, Vector3 directionOfTravel)
		{
			if (!IsCellIndexInGrid(newCellX, newCellZ))
			{
				return 0f;
			}
			CellInfo cellInfo = GetCellInfo(newCellX, newCellZ);
			if (!cellInfo.IsPassable)
			{
				return 1f;
			}
			if (cellInfo.BlockingUnit != null && unit != cellInfo.BlockingUnit)
			{
				if (fleet != null && cellInfo.BlockingFleet == fleet)
				{
					if (fleet.MoveSpeed > 5f)
					{
						return 0f;
					}
					return 0.05f * cellInfo.Cost;
				}
				if (cellInfo.BlockingUnitVelocity != Vector3.zero)
				{
					float num = Vector3.Dot(cellInfo.BlockingUnitVelocity.normalized, directionOfTravel);
					if (Mathf.Abs(num) < 0.8f)
					{
						return 0f;
					}
					if (num > 0.8f)
					{
						return cellInfo.Cost * 0.1f;
					}
				}
				return cellInfo.Cost;
			}
			return 0f;
		}

		public bool IsCellBlocked(ref CellInfo cellInfo)
		{
			return cellInfo.BlockingUnit != null;
		}

		public bool IsCellIndexInGrid(int x, int z)
		{
			if (x >= 0 && x < CellsAcross && z >= 0)
			{
				return z < CellsAcross;
			}
			return false;
		}

		public void ClampCellIndices(int x, int z, out int resultX, out int resultZ)
		{
			resultX = x;
			resultZ = z;
			if (resultX < 0)
			{
				resultX = 0;
			}
			if (resultX >= CellsAcross)
			{
				resultX = CellsAcross - 1;
			}
			if (resultZ < 0)
			{
				resultZ = 0;
			}
			if (resultZ >= CellsAcross)
			{
				resultZ = CellsAcross - 1;
			}
		}

		public Vector3? GetGridIntersectionSectorPosition(Vector3 sectorPosition1, Vector3 sectorPosition2)
		{
			Vector2 vector = new Vector2(gridBottomLeftSectorPosition.x, gridBottomLeftSectorPosition.z);
			Vector2 vector2 = vector + new Vector2(0f, gridSize.z);
			Vector2 vector3 = vector + new Vector2(gridSize.x, gridSize.z);
			Vector2 vector4 = vector + new Vector2(gridSize.x, 0f);
			Vector3 vector5 = new Vector3(sectorPosition1.x, sectorPosition1.z);
			Vector3 vector6 = new Vector3(sectorPosition2.x, sectorPosition2.z);
			float? num = null;
			float? num2 = Line.IntersectionDist(vector5, vector6, vector, vector2);
			if (!num.HasValue || num2 < num)
			{
				num = num2;
			}
			float? num3 = Line.IntersectionDist(vector5, vector6, vector2, vector3);
			if (!num.HasValue || num3 < num)
			{
				num = num3;
			}
			float? num4 = Line.IntersectionDist(vector5, vector6, vector3, vector4);
			if (!num.HasValue || num4 < num)
			{
				num = num4;
			}
			float? num5 = Line.IntersectionDist(vector5, vector6, vector4, vector);
			if (!num.HasValue || num5 < num)
			{
				num = num5;
			}
			if (num.HasValue)
			{
				Vector2 vector7 = new Vector2(vector5.x + num.Value * (vector6.x - vector5.x), vector5.y + num.Value * (vector6.y - vector5.y));
				return new Vector3(vector7.x, 0f, vector7.y);
			}
			return null;
		}
	}
}
