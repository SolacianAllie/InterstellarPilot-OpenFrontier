using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.IAP;
using OpenFrontier.IP.billing;
using TMPro;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.BuildMode
{
	public class BuildModeListItem : ScrollListItem<UnitClass>
	{
		public TextMeshProUGUI ClassLabel;

		public TextMeshProUGUI CostLabel;

		public TextMeshProUGUI NameLabel;

		public TextMeshProUGUI DescriptionLabel;

		public Image StationImage;

		public RequiredProductItem RequiredProductItem;

		public override void Refresh()
		{
			base.Refresh();
			if (Item != null)
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
			ClassLabel.text = GetStationClassName(Item.UnitPrefab);
		}

		private string GetStationClassName(Unit unitPrefab)
		{
			switch (unitPrefab.UnitClass.StationPurpose)
			{
			case StationPurpose.Defence:
				return "Defence";
			case StationPurpose.Factory:
			case StationPurpose.Scrapyard:
				return "Factory";
			case StationPurpose.Satellite:
				return "Satellite";
			default:
				return null;
			}
		}

		private void RefreshIconSprite()
		{
			StationImage.sprite = EngineASX.Instance.EngineResources.GetUnitClassRenderSpriteOrDefault(Item);
		}

		private void RefreshNameLabel()
		{
			NameLabel.text = Item.GetClassAndSeriesName();
		}

		private void RefreshDescriptionLabel()
		{
			DescriptionLabel.text = Item.GetShortDescriptionElseLong();
		}

		private void RefreshCostLabel()
		{
			CostLabel.text = TextFormattingHelper.FormatCredits(GetCost(), includeSuffix: true);
		}

		private int GetCost()
		{
			return Item.SaleCost;
		}

		private void RefreshAffordableLabelColor()
		{
			int cost = GetCost();
			bool flag = EngineASX.Instance.LocalPlayer.Credits >= cost;
			CostLabel.color = (flag ? GameController.Instance.GameSettings.AffordableColor : GameController.Instance.GameSettings.UnaffordableColor);
		}

		private void Update()
		{
			if (Item != null)
			{
				RefreshCostLabel();
				RefreshAffordableLabelColor();
			}
		}

		private void RefreshProductInfo()
		{
			bool flag = BuildModeHelper.ItemRequiresIAP(Item);
			RequiredProductItem.gameObject.SetActive(flag);
			if (flag)
			{
				RequiredProductItem.RequiredProducts = new List<IPProduct>();
				RequiredProductItem.RequiredProducts.Add(Item.RequiredProduct);
				RequiredProductItem.Refresh();
			}
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
		}
	}
}
