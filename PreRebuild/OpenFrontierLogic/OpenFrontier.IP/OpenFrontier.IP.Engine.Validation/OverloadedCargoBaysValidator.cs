using System.Collections.Generic;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Validation
{
	public static class OverloadedCargoBaysValidator
	{
		public static void Validate()
		{
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Ship);
				if (unitsByType != null)
				{
					foreach (Unit item in unitsByType)
					{
						ValidateUnit(item);
					}
				}
				List<Unit> unitsByType2 = sector.GetUnitsByType(UnitType.Station);
				if (unitsByType2 == null)
				{
					continue;
				}
				foreach (Unit item2 in unitsByType2)
				{
					ValidateUnit(item2);
				}
			}
		}

		private static void ValidateUnit(Unit unit)
		{
			CargoBayComponent cargoBayComponent = unit.CargoBayComponent;
			if (cargoBayComponent != null && cargoBayComponent.Load > cargoBayComponent.Capacity * 1.05f)
			{
				Debug.LogError($"Unit {unit} Cargo bay is more than 5% overloaded. Load: {cargoBayComponent.Load} Capacity: {cargoBayComponent.Capacity}", unit);
			}
		}
	}
}
