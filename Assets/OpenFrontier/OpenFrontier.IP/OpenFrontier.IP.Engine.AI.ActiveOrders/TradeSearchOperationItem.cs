namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public struct TradeSearchOperationItem
	{
		public CargoTrader BuyLocation { get; set; }

		public CargoClass CargoClass { get; set; }

		public float BaseScore { get; set; }
	}
}
