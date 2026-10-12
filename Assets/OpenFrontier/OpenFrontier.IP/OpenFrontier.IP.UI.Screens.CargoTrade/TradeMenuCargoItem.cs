using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.CargoTrade
{
	public class TradeMenuCargoItem : ScrollListItem<CargoClass>
	{
		public Image CargoImage;

		public Text CargoItemNameLabel;

		public Text CompatibleEquipmentLabel;

		public Text DockCargoAmountLabel;

		public Text PlayerCargoAmountLabel;

		public TradePriceDisplay BuyTradePriceDisplay;

		public TradePriceDisplay SellTradePriceDisplay;

		public BuySellMaxButton BuyMaxButton;

		public BuySellMaxButton SellMaxButton;

		public CargoTradeScreen TradeUI => ((TradeItemListUI)ParentList).TradeMenuUI;

		public override void OnActiveInList()
		{
			base.OnActiveInList();
			BuyTradePriceDisplay.ResetCachedPrice();
			SellTradePriceDisplay.ResetCachedPrice();
		}

		public override void Refresh()
		{
			base.Refresh();
			if (Item != null)
			{
				Faction faction = TradeUI.Eng.LocalPlayer.Faction;
				Unit dockUnit = TradeUI.DockUnit;
				CargoTrader component = dockUnit.GetComponent<CargoTrader>();
				BuyMaxButton.CargoClass = (SellMaxButton.CargoClass = Item);
				BuyMaxButton.DockUnit = (SellMaxButton.DockUnit = dockUnit);
				BuyMaxButton.PlayerUnit = (SellMaxButton.PlayerUnit = TradeUI.DockedUnit);
				if (CompatibleEquipmentLabel != null)
				{
					CompatibleEquipmentLabel.gameObject.SetActive(TradeUI.IsCargoClassCompatible(Item));
				}
				RefreshPrices(component, faction);
				CargoItemNameLabel.text = Item.ClassName;
				RefreshCargoCounts();
				CargoImage.sprite = TradeUI.Eng.EngineResources.GetCargoSpriteOrDefault(Item);
			}
			else
			{
				DockCargoAmountLabel.text = "-";
				CargoItemNameLabel.text = null;
				PlayerCargoAmountLabel.text = "-";
			}
		}

		private void RefreshCargoCounts()
		{
			DockCargoAmountLabel.text = TradeUI.DockUnit.Components.CargoTrader.GetCargoCountStr(Item);
			int countOf = TradeUI.DockedUnit.CargoBayComponent.GetCountOf(Item);
			if (countOf > 0)
			{
				PlayerCargoAmountLabel.text = countOf.ToString();
			}
			else
			{
				PlayerCargoAmountLabel.text = "-";
			}
		}

		private void RefreshPrices(CargoTrader trader, Faction playerFaction)
		{
			BuyTradePriceDisplay.UnitTrader = trader;
			BuyTradePriceDisplay.TradingFaction = playerFaction;
			BuyTradePriceDisplay.CargoClass = Item;
			SellTradePriceDisplay.UnitTrader = trader;
			SellTradePriceDisplay.TradingFaction = playerFaction;
			SellTradePriceDisplay.CargoClass = Item;
		}

		private void Update()
		{
			Refresh();
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			TryShowItem();
		}

		public void TryShowItem()
		{
			if (TradeUI != null)
			{
				UIController.Instance.ScreenNavigator.ShowCargoTradeItemScreen(TradeUI.DockedUnit, TradeUI.DockUnit, Item);
			}
		}
	}
}
