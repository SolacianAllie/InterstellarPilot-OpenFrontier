using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelDockOrder : ModelFleetOrder
	{
		public ModelUnit TargetDock { get; set; }

		public override FleetOrderType OrderType => FleetOrderType.Dock;
	}
}
