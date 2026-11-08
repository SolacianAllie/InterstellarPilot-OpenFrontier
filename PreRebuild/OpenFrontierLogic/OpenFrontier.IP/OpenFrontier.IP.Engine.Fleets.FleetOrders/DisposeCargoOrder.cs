using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class DisposeCargoOrder : FleetOrder
	{
		public override FleetOrderType OrderType => FleetOrderType.DisposeCargo;

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveDisposeCargoOrder activeDisposeCargoOrder = gameObject.AddComponent<ActiveDisposeCargoOrder>();
			activeDisposeCargoOrder.DisposeCargoObjective = this;
			return activeDisposeCargoOrder;
		}

		public override string GetDescription()
		{
			return "Dispose cargo";
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
