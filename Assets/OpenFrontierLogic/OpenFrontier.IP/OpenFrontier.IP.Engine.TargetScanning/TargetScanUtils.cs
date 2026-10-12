namespace OpenFrontier.IP.Engine.TargetScanning
{
	public class TargetScanUtils
	{
		public static bool CanPlayerScan(Unit unit)
		{
			return CanScan(unit);
		}

		public static bool CanScan(Unit unit)
		{
			if (unit == null || !unit.IsValidAndNotDestroyed)
			{
				return false;
			}
			UnitType unitType = unit.UnitType;
			if ((uint)(unitType - 1) <= 1u || unitType == UnitType.Asteroid)
			{
				return true;
			}
			return false;
		}
	}
}
