using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Validation
{
	public static class DockedShipConsistencyValidator
	{
		public static void Validate()
		{
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
				if (unitsByType != null)
				{
					ValidateUnits(unitsByType);
				}
				List<Unit> unitsByType2 = sector.GetUnitsByType(UnitType.Ship);
				if (unitsByType2 != null)
				{
					ValidateUnits(unitsByType2);
				}
			}
		}

		private static void ValidateUnits(List<Unit> units)
		{
			foreach (Unit unit in units)
			{
				UnitHangar hangar = unit.GetHangar();
				if (!(hangar != null))
				{
					continue;
				}
				foreach (UnitComponentHolder dockedShip in hangar.DockedShips)
				{
					if (dockedShip.DockUnit != unit)
					{
						Debug.LogError($"Docked unit {dockedShip} has a different dock unit ({dockedShip.DockUnit}) from the hangar");
					}
				}
				int num = 0;
				int num2 = 0;
				foreach (UnitHangarBay bay in hangar.Bays)
				{
					if (bay.DockedUnit != null)
					{
						num++;
					}
					else
					{
						num2++;
					}
				}
				if (num != hangar.DockedUnitCount)
				{
					Debug.LogError($"Mismatch between number of units actually in hangar bays {num} and hangar docked count {hangar.DockedUnitCount}");
				}
				if (num2 != hangar.UnoccupiedBays.Count)
				{
					Debug.LogError($"Mismatch between number of unoccupied hangar bays {num2} and hangar unoccupied count {hangar.UnoccupiedBays.Count}");
				}
			}
		}
	}
}
