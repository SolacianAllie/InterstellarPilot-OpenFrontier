using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.ActiveOrders;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class ManualRearmOrder : RearmOrder
	{
		public Unit SpecificRearmLocation;

		public override FleetOrderType OrderType => FleetOrderType.ManualRearm;

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveManualRearmOrder activeManualRearmOrder = gameObject.AddComponent<ActiveManualRearmOrder>();
			activeManualRearmOrder.RearmOrder = (activeManualRearmOrder.ManualRearmOrder = this);
			return activeManualRearmOrder;
		}

		public override string GetDescriptionForFaction(Faction faction, Sector currentSector)
		{
			if (faction != null && SpecificRearmLocation != null && SpecificRearmLocation.Sector != null)
			{
				SpecificRearmLocation.GetFriendlyName();
				return "Rearm at " + UnitNamer.GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(faction, currentSector, SpecificRearmLocation);
			}
			return "Rearm";
		}
	}
}
