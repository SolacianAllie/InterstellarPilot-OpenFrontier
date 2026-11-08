using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class MoveToNearestFriendlyStationOrder : FleetOrder
	{
		public bool CompleteOnReachTarget = true;

		public override FleetOrderType OrderType => FleetOrderType.MoveToNearestFriendlyStation;

		public override string GetDescription()
		{
			return "Move";
		}

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveMoveToNearestFriendlyStationOrder activeMoveToNearestFriendlyStationOrder = gameObject.AddComponent<ActiveMoveToNearestFriendlyStationOrder>();
			activeMoveToNearestFriendlyStationOrder.MoveToNearestFriendlyStationObjective = this;
			return activeMoveToNearestFriendlyStationOrder;
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
