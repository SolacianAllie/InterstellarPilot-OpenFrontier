using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class TradeRouteCalculator : MonoBehaviour
	{
		private static List<TradeRoute> tradeRouteCache = new List<TradeRoute>();

		public List<TradeRouteBestLocation> BestLocationCache = new List<TradeRouteBestLocation>();

		private EngineASX engine;

		public TradeRouteSort EditorSorter = new TradeRouteSort();

		public bool recalculate;

		public TradeRouteCalculatorSortMode EditorSortMode = TradeRouteCalculatorSortMode.TotalMaxProfit;

		private static TradeRoute[] editorTradeRoutes = new TradeRoute[150];

		private static List<TradeRoute> tmp = new List<TradeRoute>();

		public TradeRoute[] EditorTradeRoutes;

		public void PopulateBestBuySellLocation(CargoClass cargoClass, TradeType tradeType, float minOrMaxPriceMultiplier, CargoTrader excludeTrader, int minQuantityAtLocation, Sector scene, int maxJumpDistance, Faction sourceFaction, float minOpinionFromSource)
		{
			BestLocationCache.Clear();
			foreach (CargoTrader trader in engine.Traders)
			{
				Unit unit = trader.Unit;
				if (!trader.Unit.IsDockable || (maxJumpDistance >= 0 && !(scene == null) && scene.GetJumpDistanceTo(trader.Unit.Sector) > maxJumpDistance) || !(unit.CargoBayComponent != null) || (!(sourceFaction == null) && !(unit.Faction == null) && (unit.Faction.IsHostileTo(sourceFaction) || !(unit.Faction.GetOpinion(sourceFaction) >= minOpinionFromSource))))
				{
					continue;
				}
				int countOf = unit.CargoBayComponent.GetCountOf(cargoClass);
				if ((tradeType != TradeType.Buy && countOf < minQuantityAtLocation) || !(trader != excludeTrader))
				{
					continue;
				}
				float priceMultiplier = 0f;
				if (trader.GetPriceMultiplier(cargoClass, null, tradeType, 1, out priceMultiplier) && ((tradeType == TradeType.Buy && priceMultiplier > minOrMaxPriceMultiplier) || (tradeType == TradeType.Sell && priceMultiplier < minOrMaxPriceMultiplier)))
				{
					for (int i = 0; i < BestLocationCache.Count && priceMultiplier > BestLocationCache[i].PricePerUnit; i++)
					{
					}
					TradeRouteBestLocation item = new TradeRouteBestLocation
					{
						PricePerUnit = priceMultiplier * (float)cargoClass.BasePrice,
						TargetLocation = trader,
						UnitsAvailable = countOf
					};
					BestLocationCache.Insert(0, item);
				}
			}
		}

		public int Recalculate(Sector scene, int maxJumpDistance, int minVolume, Faction sourceFaction, float minOpinionFromSource, bool ignoreEquipment, TradeRoute[] outputList)
		{
			tradeRouteCache.Clear();
			foreach (CargoTrader trader in engine.Traders)
			{
				Unit unit = trader.Unit;
				if (!(unit.Sector != null) || (!(sourceFaction == null) && !(unit.Faction == null) && (unit.Faction.IsHostileTo(sourceFaction) || !(unit.Faction.GetOpinion(sourceFaction) >= minOpinionFromSource))) || (!(scene == null) && maxJumpDistance >= 0 && scene.GetJumpDistanceTo(unit.Sector) > maxJumpDistance))
				{
					continue;
				}
				foreach (CargoClass cargoClass in engine.CargoClasses)
				{
					if (cargoClass.IsReserved || !cargoClass.IsTraded || (ignoreEquipment && cargoClass.IsEquipment))
					{
						continue;
					}
					float price = 0f;
					if (!trader.GetSellPriceMultiplier(cargoClass, null, 1, out price))
					{
						continue;
					}
					int cargoCountOf = unit.GetCargoCountOf(cargoClass);
					if (cargoCountOf <= 0 || !((float)cargoCountOf * cargoClass.Volume > (float)minVolume))
					{
						continue;
					}
					float num = price;
					PopulateBestBuySellLocation(cargoClass, TradeType.Buy, num, trader, 0, scene, maxJumpDistance, sourceFaction, minOpinionFromSource);
					for (int i = 0; i < BestLocationCache.Count; i++)
					{
						TradeRoute tradeRoute = new TradeRoute();
						tradeRoute.CargoClass = cargoClass;
						tradeRoute.BuyLocation = trader;
						tradeRoute.SellLocation = BestLocationCache[i].TargetLocation;
						tradeRoute.JumpDistance = trader.Unit.Sector.GetJumpDistanceTo(tradeRoute.SellLocation.Unit.Sector);
						tradeRoute.UnitsAvailable = cargoCountOf;
						tradeRoute.ProfitPerUnit = BestLocationCache[i].PricePerUnit - num * (float)cargoClass.BasePrice;
						tradeRoute.ProfitPerOneVolumeUnit = tradeRoute.ProfitPerUnit / cargoClass.Volume;
						tradeRoute.EstimatedTotalProfit = (float)unit.GetCargoCountOf(cargoClass) * tradeRoute.ProfitPerUnit;
						int j;
						for (j = 0; j < tradeRouteCache.Count && tradeRoute.ProfitPerUnit < tradeRouteCache[j].ProfitPerUnit; j++)
						{
						}
						tradeRouteCache.Insert(j, tradeRoute);
					}
				}
			}
			int num2 = Mathf.Min(outputList.Length, tradeRouteCache.Count);
			for (int k = 0; k < num2; k++)
			{
				outputList[k] = tradeRouteCache[k];
			}
			return num2;
		}

		private void Start()
		{
			EditorSorter.Calculator = this;
			engine = EngineASX.Instance;
		}

		private void Update()
		{
			if (recalculate)
			{
				int num = Recalculate(null, -1, 0, null, 0f, ignoreEquipment: true, editorTradeRoutes);
				tmp.Clear();
				for (int i = 0; i < num; i++)
				{
					tmp.Add(editorTradeRoutes[i]);
				}
				tmp.Sort(EditorSorter);
				EditorTradeRoutes = tmp.ToArray();
				recalculate = false;
			}
		}
	}
}
