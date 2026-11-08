using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.Models;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.ActiveOrderTypes
{
	public class ModelActiveTradeOrder : ModelActiveFleetOrder
	{
		public ModelCustomTradeRoute TradeRoute { get; set; }

		public double EndBuySellTime { get; set; }

		public double LastStateChangeTime { get; set; }

		public ActiveTradeOrderState CurrentState { get; set; }
	}
}
