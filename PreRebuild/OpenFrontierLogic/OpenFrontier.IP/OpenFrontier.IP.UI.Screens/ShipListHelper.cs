using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens
{
	public static class ShipListHelper
	{
		public static string GetShipNameClassAndFleetWithEmbeddedFleetSprite(Unit unit, bool shortName = false, bool omitFleetNameIfSingleShip = false, bool shortFleetName = true)
		{
			string friendlyName = unit.GetFriendlyName(shortName);
			Fleet fleet = unit.GetFleet();
			if (fleet != null && (!omitFleetNameIfSingleShip || fleet.Ships.Count > 1))
			{
				return friendlyName + " <sprite index=0>" + fleet.GetFriendlyName(shortFleetName);
			}
			return friendlyName;
		}
	}
}
