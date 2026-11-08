using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelAttackTargetOrder : ModelFleetOrder
	{
		public ModelUnit TargetUnit { get; set; }

		public float AttackPriority { get; set; }

		public override FleetOrderType OrderType => FleetOrderType.AttackTarget;
	}
}
