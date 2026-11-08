using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelReturnToBaseOrder : ModelFleetOrder
	{
		public override FleetOrderType OrderType => FleetOrderType.RTB;
	}
}
