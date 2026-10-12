using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Fleets.ActiveObjectives;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class UndockOrder : FleetOrder
	{
		public override FleetOrderType OrderType => FleetOrderType.Undock;

		public override string GetDescription()
		{
			return "Undock";
		}

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			return gameObject.AddComponent<ActiveUndockOrder>();
		}
	}
}
