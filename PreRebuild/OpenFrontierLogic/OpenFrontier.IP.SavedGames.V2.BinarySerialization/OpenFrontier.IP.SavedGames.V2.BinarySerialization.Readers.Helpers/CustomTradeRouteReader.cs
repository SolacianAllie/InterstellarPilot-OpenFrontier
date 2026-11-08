using System.Collections.Generic;
using System.IO;
using OpenFrontier.IP.SavedGames.V2.Model;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.Models;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Readers.Helpers
{
	public static class CustomTradeRouteReader
	{
		public static ModelCustomTradeRoute Read(BinaryReader reader, Dictionary<int, ModelUnit> units)
		{
			ModelCustomTradeRoute modelCustomTradeRoute = new ModelCustomTradeRoute();
			int cargoClass = reader.ReadInt32();
			int key = reader.ReadInt32();
			int key2 = reader.ReadInt32();
			float buyPriceMultiplier = reader.ReadSingle();
			modelCustomTradeRoute.CargoClass = (ModelCargoClass)cargoClass;
			modelCustomTradeRoute.BuyLocation = units.GetValueOrDefault(key);
			modelCustomTradeRoute.SellLocation = units.GetValueOrDefault(key2);
			modelCustomTradeRoute.BuyPriceMultiplier = buyPriceMultiplier;
			return modelCustomTradeRoute;
		}
	}
}
