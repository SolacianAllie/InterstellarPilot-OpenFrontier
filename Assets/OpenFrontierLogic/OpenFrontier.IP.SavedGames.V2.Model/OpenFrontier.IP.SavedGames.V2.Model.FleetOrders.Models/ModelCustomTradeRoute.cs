namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.Models
{
	public class ModelCustomTradeRoute
	{
		public ModelCargoClass CargoClass { get; set; }

		public ModelUnit BuyLocation { get; set; }

		public ModelUnit SellLocation { get; set; }

		public float BuyPriceMultiplier { get; set; }
	}
}
