using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class AutonomousRoamLocationsOrder : FleetOrder
	{
		public override FleetOrderType OrderType => FleetOrderType.AutonomousRoamLocationsObjective;

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveRoamLocationsOrder activeRoamLocationsOrder = gameObject.AddComponent<ActiveRoamLocationsOrder>();
			activeRoamLocationsOrder.AutonomousRoamLocationsObjective = this;
			return activeRoamLocationsOrder;
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
