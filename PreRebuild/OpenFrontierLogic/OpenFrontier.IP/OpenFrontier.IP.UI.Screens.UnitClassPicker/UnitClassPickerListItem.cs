using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.UnitClassPicker
{
	public class UnitClassPickerListItem : ScrollListItem<UnitClass>
	{
		public TextMeshProUGUI ClassLabel;

		public TextMeshProUGUI NameLabel;

		public Image ShipImage;

		public override void Refresh()
		{
			base.Refresh();
			if (Item != null)
			{
				RefreshIconSprite();
				RefreshNameLabel();
				RefreshClassLabel();
			}
			else
			{
				NameLabel.text = null;
			}
		}

		private void RefreshClassLabel()
		{
			ClassLabel.text = ((Item.ShipType == ShipType.Normal) ? ShipBuyScreen.GetUnitClassification(Item.UnitPrefab) : string.Empty);
		}

		private void RefreshIconSprite()
		{
			ShipImage.sprite = EngineASX.Instance.EngineResources.GetUnitClassIconSpriteOrDefault(Item);
		}

		private void RefreshNameLabel()
		{
			NameLabel.text = Item.GetClassAndSeriesName();
		}
	}
}
