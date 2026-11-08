using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelExploreOrder : ModelFleetOrder
	{
		public override FleetOrderType OrderType => FleetOrderType.Explore;
	}
}
