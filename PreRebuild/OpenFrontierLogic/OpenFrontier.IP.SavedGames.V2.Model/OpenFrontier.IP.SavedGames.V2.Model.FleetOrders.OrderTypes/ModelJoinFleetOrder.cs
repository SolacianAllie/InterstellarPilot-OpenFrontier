using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelJoinFleetOrder : ModelFleetOrder
	{
		public ModelFleet TargetFleet { get; set; }

		public override FleetOrderType OrderType => FleetOrderType.JoinFleet;
	}
}
