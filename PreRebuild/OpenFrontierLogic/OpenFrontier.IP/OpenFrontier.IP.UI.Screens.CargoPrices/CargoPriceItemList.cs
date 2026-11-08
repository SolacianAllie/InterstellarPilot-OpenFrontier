using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.CargoPrices
{
	public class CargoPriceItemList : ScrollList<CargoPriceData>
	{
		private static List<CargoPriceData> traderCache = new List<CargoPriceData>();

		public int SortDirection = 1;

		public SortPriceMode SortMode = SortPriceMode.Price;

		public CargoPricesScreen TradePricesUI;

		public TradeType TradeType = TradeType.Buy;

		public void GetTraders(CargoClass cargoClass)
		{
			traderCache.Clear();
			foreach (CargoTrader trader in Engine.Traders)
			{
				int price = 0;
				if (trader != null && trader.Unit.IsValidAndNotDestroyed && Engine.LocalPlayer.Faction.Intel.IsUnitDiscoveredOrOwned(trader.Unit) && trader.Unit.IsDockable && ((TradeType == TradeType.Buy && trader.IsBuyerOf(cargoClass)) || (TradeType == TradeType.Sell && trader.IsSellerOf(cargoClass))))
				{
					string text = "";
					text = ((!trader.HasInfiniteOf(cargoClass)) ? TextFormattingHelper.FormatCargoAmount(trader.Unit.Components.CargoBayComponent.GetCountOf(cargoClass)) : TextFormattingHelper.UnlimitedCargoText);
					bool price2 = trader.GetPrice(cargoClass, Engine.LocalFaction, TradeType, 1, out price);
					CargoPriceData cargoPriceData = new CargoPriceData();
					cargoPriceData.Trader = trader;
					cargoPriceData.Price = (price2 ? new int?(price) : ((int?)null));
					cargoPriceData.QuantityAvailableText = text;
					cargoPriceData.GateDist = EngineASX.Instance.LocalFaction.Intel.GetMinSectorJumpCount(Engine.PlayerUnit.Sector, trader.Unit.Sector);
					if (cargoPriceData.GateDist < 0)
					{
						cargoPriceData.GateDist = null;
					}
					traderCache.Add(cargoPriceData);
				}
			}
			switch (SortMode)
			{
			case SortPriceMode.Price:
				if (SortDirection == 1)
				{
					traderCache = (from e in traderCache
						orderby !e.GateDist.HasValue, !e.Price.HasValue, e.Price
						select e).ToList();
				}
				else
				{
					traderCache = (from e in traderCache
						orderby !e.GateDist.HasValue, !e.Price.HasValue
						orderby e.Price descending
						select e).ToList();
				}
				break;
			case SortPriceMode.GateDist:
				if (SortDirection == 1)
				{
					traderCache = traderCache.OrderBy((CargoPriceData e) => e.GateDist ?? 999).ToList();
				}
				else
				{
					traderCache = traderCache.OrderByDescending((CargoPriceData e) => e.GateDist ?? 999).ToList();
				}
				break;
			}
		}

		protected override void OnRefreshing()
		{
			if (TradePricesUI.CargoClass != null)
			{
				GetTraders(TradePricesUI.CargoClass);
				SetItems(traderCache);
			}
			base.OnRefreshing();
		}
	}
}
