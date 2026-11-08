using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class AttackFleetOrder : FleetOrder
	{
		public float AttackPriority = 8f;

		public Fleet Target;

		public override FleetOrderType OrderType => FleetOrderType.AttackGroup;

		public override bool IsOffensive => true;

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveAttackFleetOrder activeAttackFleetOrder = gameObject.AddComponent<ActiveAttackFleetOrder>();
			activeAttackFleetOrder.AttackGroupObjective = this;
			return activeAttackFleetOrder;
		}

		public override string GetDescription()
		{
			return "Attack fleet";
		}
	}
}
