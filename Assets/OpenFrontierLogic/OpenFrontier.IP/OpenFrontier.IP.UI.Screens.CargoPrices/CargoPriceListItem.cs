using System.Text;
using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.CargoPrices
{
	public class CargoPriceListItem : ScrollListItem<CargoPriceData>
	{
		public UnitPathIconsDisplay UnitPathIconsDisplay;

		public TextMeshProUGUI LocationLabel;

		public TextMeshProUGUI PriceLabel;

		public TextMeshProUGUI QuantityLabel;

		public TextMeshProUGUI SectorLabel;

		public bool ShortLocationName = true;

		public override void Refresh()
		{
			base.Refresh();
			EngineASX instance = EngineASX.Instance;
			CargoPriceItemList parentUI = (CargoPriceItemList)ParentList;
			SectorLabel.text = GetSectorNameAndDistance(Item.Trader.Unit.Sector, Item.GateDist);
			QuantityLabel.text = Item.QuantityAvailableText;
			RefreshLocationLabelText();
			LocationLabel.color = instance.GetFactionHostilityColor(Item.Trader.Unit.Faction, instance.LocalPlayer.Faction);
			RefreshPriceLabel(instance, parentUI);
			UnitPathIconsDisplay.Unit = Item.Trader.Unit;
		}

		public static string GetSectorNameAndDistance(Sector sector, int? gateDist)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Length = 0;
			stringBuilder.Append(sector.Name);
			if (gateDist.HasValue)
			{
				if (gateDist.Value > 0)
				{
					stringBuilder.Append(" (<sprite index= 0> ");
					stringBuilder.Concat(gateDist.Value);
					stringBuilder.Append(")");
				}
			}
			else
			{
				stringBuilder.Append(" (<sprite index= 0> -)");
			}
			return stringBuilder.ToString();
		}

		private void RefreshLocationLabelText()
		{
			string text = Item.Trader.Unit.GetFriendlyName(ShortLocationName);
			if (Item.Trader.Unit.Faction != null && Item.Trader.Unit.UnitName != Item.Trader.Unit.Faction.Name)
			{
				text += $" ({Item.Trader.Unit.Faction.GetShortNameElseLong()})";
			}
			LocationLabel.text = text;
		}

		private void RefreshPriceLabel(EngineASX engine, CargoPriceItemList parentUI)
		{
			PriceLabel.text = ((Item.Price.HasValue && Item.Price > 0) ? TextFormattingHelper.FormatCredits(Item.Price.Value) : "-");
			if (Item.Price.HasValue)
			{
				PriceLabel.color = engine.GetPriceColor((parentUI.TradeType != TradeType.Buy) ? TradeType.Buy : TradeType.Sell, parentUI.TradePricesUI.CargoClass, Item.Price.Value);
			}
			else
			{
				PriceLabel.color = Color.white;
			}
		}
	}
}
