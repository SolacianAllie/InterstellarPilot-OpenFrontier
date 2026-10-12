using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelRearmAtNearestFleetOrder : ModelFleetOrder
	{
		public InsufficientCreditsMode InsufficientCreditsMode { get; set; }

		public float EquipmentCargoUsage { get; set; } = 0.25f;

		public override FleetOrderType OrderType => FleetOrderType.RearmAtNearest;
	}
}
