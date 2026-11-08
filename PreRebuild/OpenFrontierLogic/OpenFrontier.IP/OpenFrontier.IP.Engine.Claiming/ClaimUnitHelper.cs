using UnityEngine;

namespace OpenFrontier.IP.Engine.Claiming
{
	public static class ClaimUnitHelper
	{
		public static bool CanClaimUnit(Unit unit)
		{
			if (IsClaimableUnitType(unit))
			{
				if (unit.UnitType == UnitType.Station && unit.UnitClass.StationPurpose == StationPurpose.SectorControl && unit.Sector.ControllingFaction != null)
				{
					return false;
				}
				if (unit.Faction == null && Time.time > unit.Components.ClaimCooldownTime)
				{
					return unit.Tractorer == null;
				}
				return false;
			}
			return false;
		}

		public static bool IsClaimableUnitType(Unit unit)
		{
			if (unit != null && unit.IsValidAndNotDestroyed)
			{
				return unit.IsStationOrShip();
			}
			return false;
		}
	}
}
