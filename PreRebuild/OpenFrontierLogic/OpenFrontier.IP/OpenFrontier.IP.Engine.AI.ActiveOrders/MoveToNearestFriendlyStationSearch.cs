using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class MoveToNearestFriendlyStationSearch
	{
		private bool isComplete;

		private Unit bestUnit;

		private float bestDistance;

		private List<Sector> queuedSectors = new List<Sector>();

		private Fleet sourceGroup;

		private Unit nearestReferenceUnit;

		public Unit BestUnit => bestUnit;

		public bool IsComplete => isComplete;

		public void Init(Fleet sourceGroup, int maxJumpDist)
		{
			this.sourceGroup = sourceGroup;
			bestDistance = 0f;
			isComplete = false;
			bestUnit = null;
			nearestReferenceUnit = null;
			queuedSectors.Clear();
			Sector homeSectorOrCurrent = sourceGroup.GetHomeSectorOrCurrent();
			Vector3 homeSectorPositionOrCurrent = sourceGroup.GetHomeSectorPositionOrCurrent();
			nearestReferenceUnit = WorldHelper.FindNearestStationOrWormhole(homeSectorOrCurrent, homeSectorPositionOrCurrent);
			SectorFinder.FindNavigableDiscoveredSectorsWithinJumpDistanceOf(homeSectorOrCurrent, maxJumpDist, this.sourceGroup.Faction);
			foreach (SectorFinder.SectorResult result in SectorFinder.Results)
			{
				queuedSectors.Add(result.Sector);
			}
		}

		public void Search()
		{
			if (queuedSectors.Count > 0)
			{
				Sector sector = queuedSectors[0];
				queuedSectors.RemoveAt(0);
				if (sourceGroup != null && sourceGroup.Faction != null)
				{
					List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
					if (unitsByType != null)
					{
						foreach (Unit item in unitsByType)
						{
							if (item != null && item.IsValidAndNotDestroyed && sourceGroup.Faction.Intel.IsUnitDiscoveredOrOwned(item) && item.UnitClass.StationPurpose != StationPurpose.Defence && item.IsDockable && !item.IsHostileToOrAlwaysHostileToTwoWay(sourceGroup.Faction) && sourceGroup.Faction.GetOpinion(item.Faction) > -0.5f)
							{
								float? distance = EngineASX.Instance.DistanceCalculator.GetDistance(nearestReferenceUnit, item);
								if (distance.HasValue && (bestUnit == null || distance < bestDistance))
								{
									bestUnit = item;
									bestDistance = distance.Value;
								}
							}
						}
					}
				}
			}
			if (queuedSectors.Count == 0 || bestUnit != null)
			{
				isComplete = true;
			}
		}
	}
}
