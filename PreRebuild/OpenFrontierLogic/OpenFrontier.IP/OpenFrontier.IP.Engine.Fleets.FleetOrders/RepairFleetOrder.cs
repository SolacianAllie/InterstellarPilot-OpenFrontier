using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class RepairFleetOrder : FleetOrder
	{
		public InsufficientCreditsMode InsufficientCreditsMode;

		public override FleetOrderType OrderType => FleetOrderType.RepairAtNearest;

		public override bool CanSpendCredits => true;

		public override string GetDescription()
		{
			return "Repair";
		}

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveRepairFleetOrder activeRepairFleetOrder = gameObject.AddComponent<ActiveRepairFleetOrder>();
			activeRepairFleetOrder.RepairGroupObjective = this;
			return activeRepairFleetOrder;
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
