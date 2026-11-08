using System.Collections.Generic;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.TraderHeatmap
{
	public class TraderHeatmapModule
	{
		private List<TraderHeatmapTarget> potentialTargets = new List<TraderHeatmapTarget>(50);

		private float nextFindRandomFleetTime;

		public List<TraderHeatmapTarget> Targets => potentialTargets;

		public void Update()
		{
			if (!(Time.time < nextFindRandomFleetTime))
			{
				ConsiderFleetRandomly();
			}
		}

		private void ConsiderFleetRandomly()
		{
			float num = Mathf.Lerp(1f, 0.1f, (float)EngineASX.Instance.Sectors.Count / 256f);
			nextFindRandomFleetTime = Time.time + num;
			if (EngineASX.Instance.Sectors.Count <= 0)
			{
				return;
			}
			List<Unit> unitsByType = EngineASX.Instance.Sectors.GetRandom().GetUnitsByType(UnitType.Ship);
			if (unitsByType != null && unitsByType.Count > 0)
			{
				Unit random = unitsByType.GetRandom();
				if (random != null)
				{
					ConsiderShip(random);
				}
			}
		}

		public void ConsiderShip(Unit ship)
		{
			Fleet fleet = ship.GetFleet();
			if (fleet != null)
			{
				ConsiderFleet(fleet);
			}
		}

		public void ConsiderFleet(Fleet fleet)
		{
			if (fleet.Faction == null || fleet.Faction.IsBanditOrOutlaw())
			{
				return;
			}
			float cachedTradableCargoValue = fleet.GetCachedTradableCargoValue();
			if (cachedTradableCargoValue > 1000f && Physics.OverlapSphereNonAlloc(fleet.Sector.ToWorldPosition(fleet.SectorPosition), 1500f, EngineASX.ColliderCache, GameController.Instance.StationsMask, QueryTriggerInteraction.Collide) <= 0)
			{
				float num = Mathf.Clamp01(cachedTradableCargoValue / 50000f) * 1f;
				num -= fleet.Sector.SecurityLevel * 0.8f;
				if (fleet.Sector.ControllingFaction != null)
				{
					num -= 0.4f;
				}
				if (fleet.Sector.HasGasClouds)
				{
					num += 0.3f;
				}
				potentialTargets.Add(new TraderHeatmapTarget
				{
					Sector = fleet.Sector,
					SectorPosition = fleet.SectorPosition,
					Score = num,
					TargetFaction = fleet.Faction
				});
				if (potentialTargets.Count > GameController.Instance.GameSettings.TraderHeatmapSettings.MaxTargetsPerSector * EngineASX.Instance.Sectors.Count)
				{
					potentialTargets.RemoveRange(0, potentialTargets.Count / 2);
				}
			}
		}
	}
}
