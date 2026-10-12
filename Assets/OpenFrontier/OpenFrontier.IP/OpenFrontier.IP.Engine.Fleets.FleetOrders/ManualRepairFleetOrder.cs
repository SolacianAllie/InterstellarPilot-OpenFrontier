using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class ManualRepairFleetOrder : RepairFleetOrder
	{
		public Unit SpecificRepairLocation;

		public override FleetOrderType OrderType => FleetOrderType.ManualRepair;

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveManualRepairFleetOrder activeManualRepairFleetOrder = gameObject.AddComponent<ActiveManualRepairFleetOrder>();
			activeManualRepairFleetOrder.RepairGroupObjective = (activeManualRepairFleetOrder.ManualRepairGroupObjective = this);
			return activeManualRepairFleetOrder;
		}

		public override string GetDescription()
		{
			return "Repair";
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
