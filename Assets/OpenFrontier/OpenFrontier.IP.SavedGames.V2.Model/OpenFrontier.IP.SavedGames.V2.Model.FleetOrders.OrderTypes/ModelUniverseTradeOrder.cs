using System.Collections.Generic;
using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelUniverseTradeOrder : ModelFleetOrder
	{
		public int MinBuyQuantity { get; set; }

		public float MinBuyCargoPercentage { get; set; }

		public bool TradeOnlySpecificCargoClasses { get; set; }

		public List<ModelCargoClass> TradeSpecificCargoClasses { get; set; } = new List<ModelCargoClass>();

		public override FleetOrderType OrderType => FleetOrderType.AutonomousTrade;
	}
}
