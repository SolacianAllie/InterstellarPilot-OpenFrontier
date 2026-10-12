using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class ExploreOrder : FleetOrder
	{
		public override FleetOrderType OrderType => FleetOrderType.Explore;

		public override string GetDescription()
		{
			return "Explore";
		}

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveExploreOrder activeExploreOrder = gameObject.AddComponent<ActiveExploreOrder>();
			activeExploreOrder.ExploreObjective = this;
			return activeExploreOrder;
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
