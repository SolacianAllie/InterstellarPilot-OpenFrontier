using System;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	public class TradeRoute
	{
		public CargoTrader BuyLocation;

		public CargoClass CargoClass;

		public float EstimatedTotalProfit;

		public int JumpDistance;

		public float ProfitPerOneVolumeUnit;

		public float ProfitPerUnit;

		public CargoTrader SellLocation;

		public int UnitsAvailable;

		public override string ToString()
		{
			return $"[TradeRoute Cargo: {CargoClass} BuyLoc: {BuyLocation} SellLoc: {SellLocation}]";
		}
	}
}
