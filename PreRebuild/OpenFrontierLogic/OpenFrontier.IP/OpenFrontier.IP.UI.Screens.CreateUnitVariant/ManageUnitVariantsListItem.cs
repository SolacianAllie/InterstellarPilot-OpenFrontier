using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.CustomUnitVariants;
using TMPro;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.CreateUnitVariant
{
	public class ManageUnitVariantsListItem : ScrollListItem<ManageUnitVariantItemWrapper>
	{
		public TextMeshProUGUI ClassLabel;

		public TextMeshProUGUI CostLabel;

		public TextMeshProUGUI NameLabel;

		public TextMeshProUGUI DescriptionLabel;

		public Image ShipImage;

		public override void Refresh()
		{
			base.Refresh();
			if (Item != null && Item.CustomUnitVariant.UnitClass != null)
			{
				RefreshIconSprite();
				RefreshNameLabel();
				RefreshDescriptionLabel();
				RefreshCostLabel();
				RefreshClassLabel();
			}
			else
			{
				NameLabel.text = null;
			}
		}

		private void RefreshClassLabel()
		{
			ClassLabel.text = ((Item.CustomUnitVariant.UnitClass.ShipType == ShipType.Normal) ? ShipBuyScreen.GetUnitClassification(Item.CustomUnitVariant.UnitClass.UnitPrefab) : string.Empty);
		}

		private void RefreshIconSprite()
		{
			ShipImage.sprite = EngineASX.Instance.EngineResources.GetUnitClassIconSpriteOrDefault(Item.CustomUnitVariant.UnitClass);
		}

		private void RefreshNameLabel()
		{
			NameLabel.text = Item.CustomUnitVariant.FullName;
		}

		private void RefreshDescriptionLabel()
		{
			DescriptionLabel.text = Item.CustomUnitVariant.Description;
		}

		private void RefreshCostLabel()
		{
			CostLabel.text = TextFormattingHelper.FormatCredits(GetCost(), includeSuffix: true);
		}

		private int GetCost()
		{
			return ShipBuyScreen.GetItemSaleCostPerUnit(EngineASX.Instance, CustomUnitVariantHelper.CalculateMoneyValue(Item.CustomUnitVariant), null, null);
		}
	}
}
