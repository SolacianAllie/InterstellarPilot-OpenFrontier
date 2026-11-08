using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelEnterWormholeOrder : ModelFleetOrder
	{
		public ModelUnit TargetWormhole { get; set; }

		public override FleetOrderType OrderType => FleetOrderType.EnterWormhole;
	}
}
