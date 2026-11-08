using System.Collections.Generic;
using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveRepairAtNearestStationOrder : ActiveRepairFleetOrder
	{
		private bool isSearching;

		private PriorityQueue<Sector, int> queuedSearchSectors = new PriorityQueue<Sector, int>();

		protected override void tick(float elapsedTime)
		{
			base.tick(elapsedTime);
			if (CurrentRepairLocation == null)
			{
				IsIdle = true;
				if (!isSearching)
				{
					StartSearch();
					isSearching = true;
				}
				else if (queuedSearchSectors.Count > 0)
				{
					Unit unit = SearchInSector(queuedSearchSectors.Dequeue().Value);
					if (unit != null)
					{
						isSearching = false;
						CurrentRepairLocation = unit;
					}
				}
			}
			else
			{
				IsIdle = false;
			}
		}

		private Unit SearchInSector(Sector searchSector)
		{
			Unit result = null;
			float num = float.MinValue;
			Unit unit = WorldHelper.FindNearestStationOrWormhole(fleet.Sector, fleet.SectorPosition);
			if (unit != null)
			{
				List<Unit> unitsByType = searchSector.GetUnitsByType(UnitType.Station);
				if (unitsByType != null)
				{
					foreach (Unit item in unitsByType)
					{
						if (!item.UnitClass.HasRepairFacilities || !IsRepairLocationValid(item))
						{
							continue;
						}
						float? distance = EngineASX.Instance.DistanceCalculator.GetDistance(unit, item);
						if (distance.HasValue)
						{
							float num2 = 0f - distance.Value;
							if (num2 > num)
							{
								num = num2;
								result = item;
							}
						}
					}
				}
			}
			return result;
		}

		private void StartSearch()
		{
			queuedSearchSectors.Clear();
			SectorFinder.FindNavigableDiscoveredSectorsWithinJumpDistanceOf(fleet.GetHomeSectorOrCurrent(), GetActualMaxJumpDist(), fleet.Faction);
			foreach (SectorFinder.SectorResult result in SectorFinder.Results)
			{
				int jumpDistanceTo = result.Sector.GetJumpDistanceTo(fleet.Sector);
				queuedSearchSectors.Enqueue(result.Sector, -jumpDistanceTo);
			}
		}

		public override void OnFleetFailedToFindPathToTarget()
		{
			CurrentRepairLocation = null;
			RepairState = ActiveRepairFleetOrderState.None;
		}
	}
}
