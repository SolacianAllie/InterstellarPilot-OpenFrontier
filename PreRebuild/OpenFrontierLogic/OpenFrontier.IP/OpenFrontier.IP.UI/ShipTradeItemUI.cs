using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.IAP;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.ShipTrader;
using OpenFrontier.IP.billing;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class ShipTradeItemUI : ScrollListItem<ShipTraderItemWrapper>
	{
		public Text ClassLabel;

		public Text CostLabel;

		public Text NameLabel;

		public Text DescriptionLabel;

		public Image ShipImage;

		public RequiredProductItem RequiredProductItem;

		public ShipTradeScreen ShipTradeUI => ((ShipTradeItemListUI)ParentList).ShipTradeUI;

		public override void Refresh()
		{
			base.Refresh();
			if (Item != null && Item.UnitClass != null)
			{
				RefreshIconSprite();
				RefreshNameLabel();
				RefreshDescriptionLabel();
				RefreshCostLabel();
				RefreshAffordableLabelColor();
				RefreshClassLabel();
				RefreshProductInfo();
			}
			else
			{
				NameLabel.text = null;
			}
		}

		private void RefreshClassLabel()
		{
			ClassLabel.text = ((Item.UnitClass.ShipType == ShipType.Normal) ? ShipBuyScreen.GetUnitClassification(Item.UnitClass.UnitPrefab) : string.Empty);
		}

		private void RefreshIconSprite()
		{
			ShipImage.sprite = EngineASX.Instance.EngineResources.GetUnitClassRenderSpriteOrDefault(Item.UnitClass);
		}

		private void RefreshNameLabel()
		{
			NameLabel.text = Item.GetClassAndSeriesName();
		}

		private void RefreshDescriptionLabel()
		{
			DescriptionLabel.text = Item.GetDescription();
		}

		private void RefreshCostLabel()
		{
			CostLabel.text = TextFormattingHelper.FormatCredits(GetCost(), includeSuffix: true);
		}

		private int GetCost()
		{
			return Item.GetCost(ShipTradeUI.ShipTraderFaction, ShipTradeUI.Eng.LocalFaction);
		}

		private void RefreshAffordableLabelColor()
		{
			int cost = GetCost();
			bool flag = ShipTradeUI.Eng.LocalPlayer.Credits >= cost;
			CostLabel.color = (flag ? ShipTradeUI.Eng.GameSettings.AffordableColor : ShipTradeUI.Eng.GameSettings.UnaffordableColor);
		}

		private void Update()
		{
			if (Item != null && Item.UnitClass != null)
			{
				RefreshCostLabel();
				RefreshAffordableLabelColor();
			}
		}

		private void RefreshProductInfo()
		{
			bool requiresIap = Item.RequiresIap;
			RequiredProductItem.gameObject.SetActive(requiresIap);
			if (requiresIap)
			{
				RequiredProductItem.RequiredProducts = new List<IPProduct>();
				RequiredProductItem.RequiredProducts.Add(Item.RequiredProduct);
				RequiredProductItem.Refresh();
			}
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			if (ShipTradeUI != null)
			{
				ShipTradeUI.AttemptToBuyShip(Item);
			}
		}
	}
}
