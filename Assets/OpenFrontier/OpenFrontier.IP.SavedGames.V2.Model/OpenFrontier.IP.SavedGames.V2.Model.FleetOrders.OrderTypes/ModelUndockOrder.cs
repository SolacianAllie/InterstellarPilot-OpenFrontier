using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelUndockOrder : ModelFleetOrder
	{
		public override FleetOrderType OrderType => FleetOrderType.Undock;
	}
}
