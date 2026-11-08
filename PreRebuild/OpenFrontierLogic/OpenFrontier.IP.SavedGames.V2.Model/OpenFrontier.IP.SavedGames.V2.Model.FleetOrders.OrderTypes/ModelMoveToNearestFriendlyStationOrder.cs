using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelMoveToNearestFriendlyStationOrder : ModelFleetOrder
	{
		public bool CompleteOnReachTarget { get; set; }

		public override FleetOrderType OrderType => FleetOrderType.MoveToNearestFriendlyStation;
	}
}
