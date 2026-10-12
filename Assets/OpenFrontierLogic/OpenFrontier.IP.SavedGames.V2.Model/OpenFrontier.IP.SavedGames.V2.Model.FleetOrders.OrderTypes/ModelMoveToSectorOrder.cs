using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelMoveToSectorOrder : ModelFleetOrder
	{
		public ModelSector TargetSector { get; set; }

		public override FleetOrderType OrderType => FleetOrderType.MoveToSector;
	}
}
