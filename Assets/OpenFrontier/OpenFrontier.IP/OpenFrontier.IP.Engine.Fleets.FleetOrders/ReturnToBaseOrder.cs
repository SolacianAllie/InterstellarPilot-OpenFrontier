using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class ReturnToBaseOrder : FleetOrder
	{
		public override FleetOrderType OrderType => FleetOrderType.RTB;

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveReturnToBaseOrder activeReturnToBaseOrder = gameObject.AddComponent<ActiveReturnToBaseOrder>();
			activeReturnToBaseOrder.RTBObjective = this;
			return activeReturnToBaseOrder;
		}

		public override string GetDescription()
		{
			return "Return to base";
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
