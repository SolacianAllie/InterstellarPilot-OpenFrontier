using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class AutonomousBountyHunterOrder : FleetOrder
	{
		public override FleetOrderType OrderType => FleetOrderType.AutonomousBountyHunterObjective;

		public override float AIDefaultMaxDuration => 5400f;

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveBountyHunterOrder activeBountyHunterOrder = gameObject.AddComponent<ActiveBountyHunterOrder>();
			activeBountyHunterOrder.AutonomousBountyHunterObjective = this;
			return activeBountyHunterOrder;
		}

		protected override bool CanBeStackedOnInternal()
		{
			return false;
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
