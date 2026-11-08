using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public static class UnitInfoHelper
	{
		public static Color GetUnitDisplayColor(Unit unit, Faction localFaction)
		{
			if (unit.UnitType != UnitType.Wormhole)
			{
				return EngineASX.Instance.GetFactionHostilityColor(unit.Faction, localFaction);
			}
			return GetJumpGateDisplayColor(unit);
		}

		public static Color GetJumpGateDisplayColor(Unit unit)
		{
			if (GetForeignUnitDiscoveredTargetSector(unit) != null)
			{
				return EngineASX.Instance.DiscoveredSectorColor;
			}
			return EngineASX.Instance.UndiscoveredSectorColor;
		}

		public static Sector GetForeignUnitDiscoveredTargetSector(Unit unit)
		{
			Sector foreignUnitTargetSector = GetForeignUnitTargetSector(unit);
			if (foreignUnitTargetSector != null && foreignUnitTargetSector.IsDiscoveredByLocalFaction())
			{
				return foreignUnitTargetSector;
			}
			return null;
		}

		public static Sector GetForeignUnitTargetSector(Unit unit)
		{
			if (unit.UnitType == UnitType.Wormhole && unit.WormholeComponent != null && EngineASX.Instance.LocalFaction.Intel.HasWormholeBeenEntered(unit.WormholeComponent) && !unit.WormholeComponent.IsUnstable)
			{
				return unit.WormholeComponent.ActualTargetSector;
			}
			return null;
		}
	}
}
