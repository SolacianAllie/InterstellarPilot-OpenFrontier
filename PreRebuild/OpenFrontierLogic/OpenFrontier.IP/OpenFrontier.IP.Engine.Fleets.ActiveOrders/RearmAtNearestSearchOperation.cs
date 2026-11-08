using System.Collections.Generic;
using System.Linq;

namespace OpenFrontier.IP.Engine.Fleets.ActiveOrders
{
	public class RearmAtNearestSearchOperation : UnitSearchOperation
	{
		public List<ShipHullType> ShipHullTypes { get; set; }

		public Fleet LocalFleet { get; set; }

		protected override void QueueUnitsInSector(Sector sector)
		{
			base.QueueUnitsInSector(sector);
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item.IsEquipmentTrader())
					{
						searchItems.Enqueue(new UnitSearchOperationItem
						{
							Unit = item
						});
					}
				}
			}
			List<Unit> unitsByType2 = sector.GetUnitsByType(UnitType.Ship);
			if (unitsByType2 == null)
			{
				return;
			}
			foreach (Unit item2 in unitsByType2)
			{
				if (item2.IsEquipmentTrader())
				{
					searchItems.Enqueue(new UnitSearchOperationItem
					{
						Unit = item2
					});
				}
			}
		}

		protected override float? ScoreUnit(Unit unit, float distance)
		{
			if (!ActiveRearmOrder.IsRearmLocationValid(unit, LocalFleet, ShipHullTypes, faction))
			{
				return null;
			}
			return base.ScoreUnit(unit, distance);
		}

		public bool CanAllGroupUnitsDockAtUnitIgnoreOccupancy(Unit unit)
		{
			UnitHangar hangar = unit.GetHangar();
			if (hangar != null)
			{
				if (ShipHullTypes == null || !ShipHullTypes.Any())
				{
					return true;
				}
				return hangar.CanUnitsFitInHangarIgnoreOccupancy(ShipHullTypes);
			}
			return false;
		}
	}
}
