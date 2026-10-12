using System.Collections.Generic;
using OpenFrontier.IP.Engine.Core.Units;
using UnityEngine;

namespace OpenFrontier.IP.Engine.GasClouds
{
	public static class GasCloudHelper
	{
		private static Collider[] cache = new Collider[1];

		public static UnitGasCloud GetGasCloudAtWorldPosition(Sector sector, Vector3 position)
		{
			if (sector == null)
			{
				return null;
			}
			if (sector.GetUnitsByType(UnitType.GasCloud) != null && Physics.OverlapSphereNonAlloc(position, 1f, cache, GameController.Instance.UnitGasCloudMask, QueryTriggerInteraction.Collide) > 0)
			{
				UnitGasCloud component = cache[0].GetComponent<UnitGasCloud>();
				if (component != null && component.Unit != null && component.Unit.Sector == sector)
				{
					return component;
				}
			}
			return null;
		}

		public static void FindGasCloudForUnitsInSector(Sector sector)
		{
			if (sector.GetUnitsByType(UnitType.GasCloud) != null)
			{
				FindGasCloudForUnitsOfType(sector, UnitType.Ship);
				FindGasCloudForUnitsOfType(sector, UnitType.Station);
				FindGasCloudForUnitsOfType(sector, UnitType.Cargo);
				FindGasCloudForUnitsOfType(sector, UnitType.Wormhole);
				FindGasCloudForUnitsOfType(sector, UnitType.Asteroid);
			}
		}

		public static void FindGasCloudForUnitsOfType(Sector sector, UnitType unitType)
		{
			List<Unit> unitsByType = sector.GetUnitsByType(unitType);
			if (unitsByType == null)
			{
				return;
			}
			foreach (Unit item in unitsByType)
			{
				item.UpdateGasCloud();
			}
		}
	}
}
