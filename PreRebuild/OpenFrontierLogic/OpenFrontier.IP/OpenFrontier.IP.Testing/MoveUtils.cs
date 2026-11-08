using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Testing
{
	public static class MoveUtils
	{
		public static bool CanMoveUnitToSector(Unit targetUnit, Sector sector, out string errorMessage)
		{
			if (targetUnit == null || !targetUnit.IsValidAndNotDestroyed)
			{
				errorMessage = "Target unit is invalid";
				return false;
			}
			if (!CanMoveUnit(targetUnit))
			{
				errorMessage = "It is not possible to move the unit";
				return false;
			}
			if (sector != targetUnit.Sector && !CanChangeTargetSector(targetUnit))
			{
				errorMessage = "It is not possible to move the unit into a different sector";
				return false;
			}
			errorMessage = null;
			return true;
		}

		public static bool CanMoveUnit(Unit unit)
		{
			switch (unit.UnitType)
			{
			case UnitType.Ship:
			case UnitType.Station:
			case UnitType.Cargo:
			case UnitType.Wormhole:
			case UnitType.Asteroid:
			case UnitType.NavBuoy:
				return true;
			default:
				return false;
			}
		}

		public static bool CanChangeTargetSector(Unit unit)
		{
			switch (unit.UnitType)
			{
			case UnitType.Ship:
			case UnitType.Cargo:
			case UnitType.Asteroid:
			case UnitType.NavBuoy:
				return true;
			case UnitType.Station:
				return unit.UnitClass.StationPurpose != StationPurpose.SectorControl;
			default:
				return false;
			}
		}

		public static void OnUnitMoved(Unit unit, Sector previousSector)
		{
			if (unit.UnitType == UnitType.Station || unit.UnitType == UnitType.Wormhole)
			{
				EngineASX.Instance.DistanceCalculator.StationDistanceCache.Clear();
			}
			foreach (Fleet fleet in EngineASX.Instance.Fleets)
			{
				if (fleet.NavTarget.IsActive && fleet.NavTarget.TargetSectorObject == unit && fleet.ActiveOrder != null)
				{
					fleet.ActiveOrder.ResetTargetPosition();
				}
			}
			if (!(previousSector != unit.Sector) || !FactionIntel.IsStaticOrTreatAsStatic(unit) || !unit.IsDiscoverableType)
			{
				return;
			}
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (faction != null && faction.IsValidInGame && faction.Intel != null && faction.Intel.IsUnitDiscovered(unit, float.MaxValue))
				{
					FactionIntel.DiscoveredUnitData? discoveryData = faction.Intel.GetDiscoveryData(unit, float.MaxValue);
					if (discoveryData.HasValue)
					{
						faction.Intel.DiscoverUnit(unit);
						faction.Intel.OverrideTimeOfDiscovery(unit, discoveryData.Value.TimeOfDiscovery);
					}
				}
			}
		}
	}
}
