using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelManualRepairFleetOrder : ModelFleetOrder
	{
		public InsufficientCreditsMode InsufficientCreditsMode { get; set; }

		public ModelUnit RepairLocationUnit { get; set; }

		public override FleetOrderType OrderType => FleetOrderType.ManualRepair;
	}
}
