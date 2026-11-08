using System;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	[Serializable]
	public class AITradeRoute
	{
		public CargoTrader BuyLocation;

		public float BuyPriceMultiplier;

		public CargoClass CargoClass;

		public CargoTrader SellLocation;

		public float SellPriceMultiplier;

		public float EstimatedProfit;

		public int EstimatedQuantity;

		public override string ToString()
		{
			return $"[TradeRoute Cargo: {CargoClass} BuyLoc: {BuyLocation} SellLoc: {SellLocation}]";
		}
	}
}
