using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Heatmaps
{
	public class FactionHeatmapDissipator
	{
		private readonly FactionHeatmap factionHeatmap;

		private int curSectorIndex;

		private bool isUpdating;

		private float lastSectorUpdateTime;

		private List<HeatmapCell> cellQueue = new List<HeatmapCell>(100);

		public FactionHeatmapDissipator(FactionHeatmap factionHeatmap)
		{
			this.factionHeatmap = factionHeatmap;
		}

		public void Update()
		{
			if (isUpdating)
			{
				Sector sector = EngineASX.Instance.Sectors[curSectorIndex];
				int num = Mathf.Min(cellQueue.Count, Mathf.CeilToInt((float)EngineASX.Instance.PerformanceSettings.MaxHeatmapCellsUpdatedPerSecond * Time.deltaTime));
				float threatDissipationRatePerSecond = GameController.Instance.GameSettings.HeatmapSettings.ThreatDissipationRatePerSecond;
				for (int i = 0; i < num; i++)
				{
					HeatmapCell heatmapCell = cellQueue[cellQueue.Count - 1];
					cellQueue.RemoveAt(cellQueue.Count - 1);
					if (heatmapCell.HeatExpires)
					{
						float num2 = Time.time - heatmapCell.LastUpdateTime;
						float num3 = heatmapCell.Heat - threatDissipationRatePerSecond * num2;
						if (num3 <= 0f)
						{
							factionHeatmap.RemoveHeat(sector, heatmapCell);
							continue;
						}
						heatmapCell.LastUpdateTime = Time.time;
						heatmapCell.Heat = num3;
					}
				}
				if (cellQueue.Count == 0)
				{
					isUpdating = false;
				}
			}
			else if (Time.time - lastSectorUpdateTime > GameController.Instance.GameSettings.PerformanceSettings.HeatmapDissipationUpdateFrequency)
			{
				StartUpdate();
				lastSectorUpdateTime = Time.time;
			}
		}

		private void StartUpdate()
		{
			if (EngineASX.Instance.Sectors.Count > 0)
			{
				curSectorIndex++;
				if (curSectorIndex >= EngineASX.Instance.Sectors.Count)
				{
					curSectorIndex = 0;
				}
				Sector sector = EngineASX.Instance.Sectors[curSectorIndex];
				List<HeatmapCell> sectorCells = factionHeatmap.GetSectorCells(sector);
				if (sectorCells != null && sectorCells.Count > 0)
				{
					cellQueue.AddRange(sectorCells);
				}
				isUpdating = true;
			}
		}
	}
}
