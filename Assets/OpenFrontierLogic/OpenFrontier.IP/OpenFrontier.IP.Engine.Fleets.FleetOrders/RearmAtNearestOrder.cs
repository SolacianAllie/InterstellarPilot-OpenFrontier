using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.ActiveOrders;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class RearmAtNearestOrder : RearmOrder
	{
		public override FleetOrderType OrderType => FleetOrderType.RearmAtNearest;

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveRearmAtNearestOrder activeRearmAtNearestOrder = gameObject.AddComponent<ActiveRearmAtNearestOrder>();
			activeRearmAtNearestOrder.RearmOrder = this;
			return activeRearmAtNearestOrder;
		}

		public override string GetDescriptionForFaction(Faction faction, Sector currentSector)
		{
			return "Rearm at nearest";
		}
	}
}
