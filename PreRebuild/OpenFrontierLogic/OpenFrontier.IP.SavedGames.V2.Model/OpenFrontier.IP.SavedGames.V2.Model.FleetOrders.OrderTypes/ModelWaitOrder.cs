using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelWaitOrder : ModelFleetOrder
	{
		public float WaitTime { get; set; }

		public override FleetOrderType OrderType => FleetOrderType.Wait;
	}
}
