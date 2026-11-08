using System.IO;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.Models;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Writers.Helpers
{
	public static class CustomTraderRouteWriter
	{
		public static void Write(BinaryWriter writer, ModelCustomTradeRoute customTradeRoute)
		{
			writer.Write((int)customTradeRoute.CargoClass);
			writer.WriteUnitId(customTradeRoute.BuyLocation);
			writer.WriteUnitId(customTradeRoute.SellLocation);
			writer.Write(customTradeRoute.BuyPriceMultiplier);
		}
	}
}
