using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelPatrolPathOrder : ModelFleetOrder
	{
		public int PathDirection { get; set; }

		public bool IsLooping { get; set; }

		public ModelSectorPatrolPath PatrolPath { get; set; }

		public override FleetOrderType OrderType => FleetOrderType.PatrolPath;
	}
}
