using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class TradePriceDisplay : MonoBehaviour
	{
		public bool RefreshOnUpdate = true;

		public TradePriceDisplayChangeIndicator PriceChangeIndicator;

		public Text PriceText;

		private int? oldPrice;

		public TradeType TraderTradeType = TradeType.Buy;

		public CargoTrader UnitTrader { get; set; }

		public Faction TradingFaction { get; set; }

		public CargoClass CargoClass { get; set; }

		private void Awake()
		{
			SetPriceLabelPrice(null);
			PriceChangeIndicator.gameObject.SetActive(value: false);
		}

		private void Update()
		{
			if (RefreshOnUpdate)
			{
				Refresh();
			}
		}

		public void Refresh()
		{
			if (UnitTrader != null && TradingFaction != null && CargoClass != null)
			{
				RefreshPriceLabel();
			}
			else
			{
				SetPriceLabelPrice(null);
			}
		}

		private int? GetPrice()
		{
			int price = 0;
			if (UnitTrader.GetPrice(CargoClass, TradingFaction, TraderTradeType, 1, out price))
			{
				return price;
			}
			return null;
		}

		private void SetPriceLabelPrice(int? price)
		{
			if (price.HasValue)
			{
				TradeType tradeType = ((TraderTradeType != TradeType.Buy) ? TradeType.Buy : TradeType.Sell);
				PriceText.color = TradingFaction.Engine.GetPriceColor(tradeType, CargoClass, price.Value);
				PriceText.text = TextFormattingHelper.FormatCredits(price.Value);
				return;
			}
			if (TradingFaction != null)
			{
				PriceText.color = TradingFaction.Engine.NeutralPriceLabelColor;
			}
			else
			{
				PriceText.color = Color.grey;
			}
			PriceText.text = "-";
		}

		private void RefreshPriceLabel()
		{
			int? price = GetPrice();
			if (!oldPrice.HasValue || price != oldPrice)
			{
				SetPriceLabelPrice(price);
				if (price.HasValue && oldPrice.HasValue)
				{
					PriceChangeIndicator.GoodChange = (TraderTradeType == TradeType.Sell && price.Value < oldPrice.Value) || (TraderTradeType == TradeType.Buy && price.Value > oldPrice.Value);
					PriceChangeIndicator.PositiveChange = price.Value > oldPrice.Value;
					PriceChangeIndicator.Engine = TradingFaction.Engine;
					PriceChangeIndicator.Show();
				}
				oldPrice = price;
			}
		}

		public void ResetCachedPrice()
		{
			oldPrice = null;
		}
	}
}
