namespace OpenFrontier.IP.Engine.ComponentUpgrades
{
	public class ComponentUpgradeHelper
	{
		public static bool CanUpgradeUnit(Unit unit)
		{
			if (unit != null && unit.IsOwnedByPlayer && unit.IsStation())
			{
				return !unit.IsUnderConstructionOrDismantling;
			}
			return false;
		}
	}
}
