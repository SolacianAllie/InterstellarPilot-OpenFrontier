using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelRepairAtNearestStationOrder : ModelFleetOrder
	{
		public InsufficientCreditsMode InsufficientCreditsMode { get; set; }

		public override FleetOrderType OrderType => FleetOrderType.RepairAtNearest;
	}
}
