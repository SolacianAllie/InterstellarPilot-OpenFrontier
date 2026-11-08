using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelProtectOrder : ModelMoveToOrder
	{
		public override FleetOrderType OrderType => FleetOrderType.Protect;
	}
}
