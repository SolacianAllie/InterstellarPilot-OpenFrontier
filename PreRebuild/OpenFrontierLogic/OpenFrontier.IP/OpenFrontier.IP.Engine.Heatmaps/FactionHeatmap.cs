using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Heatmaps
{
	public class FactionHeatmap : MonoBehaviour
	{
		private int heatmapCellSize = -1;

		private int cellsAcross = -1;

		private Vector3 mapPositionOffset;

		private Dictionary<int, List<HeatmapCell>> cellListsBySectorId = new Dictionary<int, List<HeatmapCell>>();

		private Dictionary<int, Dictionary<int, HeatmapCell>> cellsBySectorId = new Dictionary<int, Dictionary<int, HeatmapCell>>();

		private FactionHeatmapDissipator updater;

		private int updateRange = -1;

		public int CellsAcross => cellsAcross;

		public int CellSize => heatmapCellSize;

		public void Init()
		{
			heatmapCellSize = GameController.Instance.GameSettings.HeatmapSettings.HeatmapCellSize;
			mapPositionOffset = GetMapPositionOffset();
			cellsAcross = GameController.Instance.GameSettings.UniverseBoundsSettings.SectorSize / heatmapCellSize;
			updateRange = GameController.Instance.GameSettings.HeatmapSettings.NewThreatCellRange;
			updater = new FactionHeatmapDissipator(this);
		}

		public static Vector3 GetMapPositionOffset()
		{
			return new Vector3(GameController.Instance.GameSettings.UniverseBoundsSettings.SectorSizeOverTwo, 0f, GameController.Instance.GameSettings.UniverseBoundsSettings.SectorSizeOverTwo);
		}

		public void Update()
		{
			if (EngineASX.LoadedAndReady)
			{
				updater.Update();
			}
		}

		public void UpdateHeatmapWithHostileUnit(Unit unit)
		{
			if (!(unit.Sector == null))
			{
				float num = Mathf.Clamp01(unit.UnitClass.CombatRating / GameController.Instance.GameSettings.HeatmapSettings.MaxCombatRatingReferenceValue);
				if (num != 0f)
				{
					UpdateHeatmapHeat(unit.Sector, unit.transform.position, num, !unit.IsStatic);
				}
			}
		}

		public void UpdateHeatmapHeat(Sector sector, Vector3 worldPosition, float heat, bool expires)
		{
			GetClampedSectorCoordinatesFromWorldPosition(sector, worldPosition, out var x, out var z);
			UpdateHeatmap(sector, x, z, heat, expires);
		}

		public void UpdateHeatmap(Sector sector, int x, int z, float heat, bool expires)
		{
			for (int i = x - updateRange; i <= x + updateRange; i++)
			{
				for (int j = z - updateRange; j <= z + updateRange; j++)
				{
					if (AreCoordinatesValid(i, j))
					{
						int num = Mathf.Max(Mathf.Abs(x - i), Mathf.Abs(z - j));
						float heat2 = (float)(updateRange - num) / (float)updateRange * heat;
						UpdateWithHeat(sector, i, j, heat2, expires);
					}
				}
			}
		}

		private void UpdateWithHeat(Sector sector, int x, int z, float heat, bool expires)
		{
			int uniqueId = sector.UniqueId;
			int sectorCellId = GetSectorCellId(x, z);
			HeatmapCell value = null;
			List<HeatmapCell> list = null;
			Dictionary<int, HeatmapCell> value2 = null;
			if (!cellsBySectorId.TryGetValue(uniqueId, out value2))
			{
				list = new List<HeatmapCell>(50);
				cellListsBySectorId.Add(uniqueId, list);
				value2 = new Dictionary<int, HeatmapCell>(50);
				cellsBySectorId.Add(uniqueId, value2);
			}
			else
			{
				list = cellListsBySectorId[sector.UniqueId];
			}
			if (!value2.TryGetValue(sectorCellId, out value))
			{
				HeatmapCell heatmapCell = new HeatmapCell
				{
					CellId = sectorCellId
				};
				value = (value2[sectorCellId] = heatmapCell);
				list.Add(value);
			}
			value.Heat = Mathf.Max(value.Heat, heat);
			value.HeatExpires |= expires;
			value.LastUpdateTime = Time.time;
		}

		public bool HasHeatForSector(Sector sector)
		{
			return cellsBySectorId.ContainsKey(sector.UniqueId);
		}

		public void ExpandSectorCellId(int cellId, out int x, out int z)
		{
			z = cellId / cellsAcross;
			x = cellId - z * cellsAcross;
		}

		public int GetSectorCellId(int x, int z)
		{
			return z * cellsAcross + x;
		}

		public float GetHeat(Sector sector, Vector3 worldPosition)
		{
			if (cellsBySectorId.TryGetValue(sector.UniqueId, out var value))
			{
				GetClampedSectorCoordinatesFromWorldPosition(sector, worldPosition, out var x, out var z);
				int sectorCellId = GetSectorCellId(x, z);
				if (value.TryGetValue(sectorCellId, out var value2))
				{
					return value2.Heat;
				}
			}
			return 0f;
		}

		public float GetHeat(Sector sector, int cellX, int cellY)
		{
			if (cellsBySectorId.TryGetValue(sector.UniqueId, out var value))
			{
				int sectorCellId = GetSectorCellId(cellX, cellY);
				if (value.TryGetValue(sectorCellId, out var value2))
				{
					return value2.Heat;
				}
			}
			return 0f;
		}

		public void RemoveHeat(Sector sector, HeatmapCell cell)
		{
			Remove(sector, cell.CellId);
		}

		public void RemoveHeat(Sector sector, int x, int z)
		{
			int sectorCellId = GetSectorCellId(x, z);
			Remove(sector, sectorCellId);
		}

		private void Remove(Sector sector, int cellId)
		{
			if (cellListsBySectorId.TryGetValue(sector.UniqueId, out var value))
			{
				Dictionary<int, HeatmapCell> dictionary = cellsBySectorId[sector.UniqueId];
				HeatmapCell heatmapCell = dictionary[cellId];
				dictionary.Remove(heatmapCell.CellId);
				value.Remove(heatmapCell);
				if (dictionary.Count == 0)
				{
					cellListsBySectorId.Remove(sector.UniqueId);
					cellsBySectorId.Remove(sector.UniqueId);
				}
			}
		}

		public void Clear()
		{
			cellListsBySectorId.Clear();
			cellsBySectorId.Clear();
		}

		public void GetClampedSectorCoordinatesFromWorldPosition(Sector sector, Vector3 worldPosition, out int x, out int z)
		{
			Vector3 localPosition = worldPosition - sector.transform.position;
			GetSectorCoordinatesFromLocalPosition(localPosition, out x, out z);
		}

		public void GetSectorCoordinatesFromLocalPosition(Vector3 localPosition, out int x, out int z)
		{
			localPosition += mapPositionOffset;
			x = (int)(localPosition.x / (float)heatmapCellSize);
			z = (int)(localPosition.z / (float)heatmapCellSize);
			if (x < 0)
			{
				x = 0;
			}
			else if (x >= cellsAcross)
			{
				x = cellsAcross - 1;
			}
			if (z < 0)
			{
				z = 0;
			}
			else if (z >= cellsAcross)
			{
				z = cellsAcross - 1;
			}
		}

		public Vector3 GetCellCenterWorldPosition(Sector sector, int cellX, int cellZ)
		{
			return sector.transform.position + GetCellCenterLocalPosition(cellX, cellZ);
		}

		public Vector3 GetCellCenterLocalPosition(int cellX, int cellZ)
		{
			return -GetMapPositionOffset() + new Vector3(cellX * heatmapCellSize, 0f, cellZ * heatmapCellSize) + GetCellSizeOverTwo();
		}

		public bool AreCoordinatesValid(int x, int z)
		{
			if (x >= 0 && x < cellsAcross && z >= 0)
			{
				return z < cellsAcross;
			}
			return false;
		}

		public List<HeatmapCell> GetSectorCells(Sector sector)
		{
			if (cellListsBySectorId.TryGetValue(sector.UniqueId, out var value))
			{
				return value;
			}
			return null;
		}

		public Vector3 GetCellSizeOverTwo()
		{
			return new Vector3((float)CellSize / 2f, 0f, (float)CellSize / 2f);
		}
	}
}
