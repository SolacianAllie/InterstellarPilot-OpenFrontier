using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelCollectCargoOrder : ModelFleetOrder
	{
		public ModelUnit TargetUnit { get; set; }

		public override FleetOrderType OrderType => FleetOrderType.CollectCargo;
	}
}
