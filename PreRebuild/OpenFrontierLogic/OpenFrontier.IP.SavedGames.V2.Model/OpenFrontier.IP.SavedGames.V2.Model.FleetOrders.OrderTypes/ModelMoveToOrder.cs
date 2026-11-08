using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelMoveToOrder : ModelFleetOrder
	{
		public bool CompleteOnReachTarget { get; set; }

		public float ArrivalThreshold { get; set; }

		public bool MatchTargetOrientation { get; set; }

		public ModelSectorTarget Target { get; set; }

		public Vec3? PreferredRelativeVectorFromTarget { get; set; }

		public override FleetOrderType OrderType => FleetOrderType.MoveTo;
	}
}
