using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.Models;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelManualTradeOrder : ModelFleetOrder
	{
		public int MinBuyQuantity { get; set; }

		public float MinBuyCargoPercentage { get; set; }

		public ModelCustomTradeRoute CustomTradeRoute { get; set; }

		public override FleetOrderType OrderType => FleetOrderType.ManualTrade;
	}
}
