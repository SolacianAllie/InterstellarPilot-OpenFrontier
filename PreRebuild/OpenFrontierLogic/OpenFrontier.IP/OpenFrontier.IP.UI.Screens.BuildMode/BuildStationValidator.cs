using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.BuildMode
{
	public static class BuildStationValidator
	{
		public static float MaxDistanceFromSourcePosition = 1250f;

		private static UnitType[] unitTypesToCheck = new UnitType[3]
		{
			UnitType.Station,
			UnitType.Asteroid,
			UnitType.Wormhole
		};

		public static bool CanBuildConsideringSourcePosition(Sector buildSector, Vector3 buildSectorPosition, Sector sourceSector, Vector3 sourceSectorPosition, out string errorMessage)
		{
			errorMessage = null;
			if (buildSector != sourceSector)
			{
				errorMessage = "Cannot build in a different sector";
				return false;
			}
			if (Vector3.Distance(buildSectorPosition, sourceSectorPosition) > MaxDistanceFromSourcePosition)
			{
				errorMessage = "Cannot build - too far away";
				return false;
			}
			return true;
		}

		public static bool CanBuild(UnitClass unitClass, Sector sector, Vector3 sectorPosition, out string errorMessage)
		{
			errorMessage = null;
			if (!CanBuildInSector(unitClass, sector, out errorMessage))
			{
				return false;
			}
			if (!CanBuildAtPosition(unitClass, sector, sectorPosition, out errorMessage))
			{
				return false;
			}
			if (!CheckBlockingUnits(unitClass, sector, sectorPosition, out var blockingUnit))
			{
				if (blockingUnit != null)
				{
					errorMessage = $"Cannot build - too close to \"{blockingUnit.GetFriendlyName()}\"";
				}
				else
				{
					errorMessage = "Cannot build - too close to objects";
				}
				return false;
			}
			return true;
		}

		public static bool CanBuildAtPosition(UnitClass unitClass, Sector sector, Vector3 sectorPosition, out string errorMessage)
		{
			errorMessage = null;
			if (!UnitOutOfBoundsValidator.IsLocalPositionWithinLowerBounds(sectorPosition))
			{
				errorMessage = "Build position is out of sector bounds";
				return false;
			}
			return true;
		}

		public static bool CanBuildInSector(UnitClass unitClass, Sector sector, out string errorMessage)
		{
			errorMessage = null;
			if (unitClass.UnitType == UnitType.Station && unitClass.StationPurpose == StationPurpose.TradeStation && !sector.HasPlanets)
			{
				errorMessage = "Trade station can only be built in a Planet sector";
				return false;
			}
			if (unitClass.UnitType == UnitType.Station && unitClass.StationPurpose == StationPurpose.SectorControl)
			{
				if (sector.GetCountOfStationPurpose(StationPurpose.SectorControl) > 0)
				{
					errorMessage = "Sector HQ cannot be constructed because the sector already contains a Sector HQ";
					return false;
				}
				if (sector.ControllingFaction != null)
				{
					if (sector.ControllingFaction.IsPlayerFaction)
					{
						errorMessage = "Sector HQ cannot be constructed because you already control this sector";
						return false;
					}
					errorMessage = "Sector HQ cannot be constructed because the sector is already controlled by \"" + sector.ControllingFaction.Name + "\"";
					return false;
				}
			}
			return true;
		}

		public static bool CheckBlockingUnits(UnitClass unitClass, Sector sector, Vector3 sectorPosition, out Unit blockingUnit)
		{
			float requiredDistance = unitClass.DisplayData.Radius + unitClass.DisplayData.BuildBlockerRadius;
			blockingUnit = null;
			UnitType[] array = unitTypesToCheck;
			foreach (UnitType unitType in array)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(unitType);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					if (IsUnitBlockingDeployment(sector, sectorPosition, item, requiredDistance, unitClass.IsMinorStation() || unitClass.UnitType == UnitType.Ship))
					{
						blockingUnit = item;
						return false;
					}
				}
			}
			return true;
		}

		private static bool IsUnitBlockingDeployment(Sector deploymentSector, Vector3 deploymentSectorPosition, Unit unit, float requiredDistance, bool isDeployingMinorStation)
		{
			if (deploymentSector != unit.Sector)
			{
				return false;
			}
			float num = Vector3.Distance(unit.SectorPosition, deploymentSectorPosition);
			num -= unit.UnitClass.DisplayData.Radius;
			if (unit.UnitType == UnitType.Wormhole || (!isDeployingMinorStation && !unit.IsMinorStation()))
			{
				num -= unit.UnitClass.DisplayData.BuildBlockerRadius;
			}
			if (num < requiredDistance)
			{
				return true;
			}
			return false;
		}
	}
}
