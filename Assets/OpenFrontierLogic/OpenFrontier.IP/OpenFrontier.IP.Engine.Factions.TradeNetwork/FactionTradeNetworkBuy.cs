namespace OpenFrontier.IP.Engine.Factions.TradeNetwork
{
	public struct FactionTradeNetworkBuy
	{
		public CargoClass CargoClass { get; set; }

		public int BuyingQuantity { get; set; }

		public Fleet Fleet { get; set; }
	}
}
