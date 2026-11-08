using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelScavengeOrder : ModelFleetOrder
	{
		public CollectCargoOwnerMode CollectOwnerMode { get; set; }

		public ModelSector TargetSector { get; set; }

		public override FleetOrderType OrderType => FleetOrderType.Scavenge;
	}
}
