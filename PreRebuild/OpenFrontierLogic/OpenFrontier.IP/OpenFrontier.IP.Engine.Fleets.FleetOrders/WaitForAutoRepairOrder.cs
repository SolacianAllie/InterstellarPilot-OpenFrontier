using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Fleets.ActiveOrders;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class WaitForAutoRepairOrder : FleetOrder
	{
		public float HullConditionThreshold = 0.95f;

		public float ShieldConditionThreshold = 0.95f;

		public float ComponentsConditionThreshold = 0.95f;

		public override FleetOrderType OrderType => FleetOrderType.WaitForAutoRepair;

		public override string GetDescription()
		{
			return "Wait until repaired";
		}

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveWaitForAutoRepairOrder activeWaitForAutoRepairOrder = gameObject.AddComponent<ActiveWaitForAutoRepairOrder>();
			activeWaitForAutoRepairOrder.WaitForAutoRepairOrder = this;
			return activeWaitForAutoRepairOrder;
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
