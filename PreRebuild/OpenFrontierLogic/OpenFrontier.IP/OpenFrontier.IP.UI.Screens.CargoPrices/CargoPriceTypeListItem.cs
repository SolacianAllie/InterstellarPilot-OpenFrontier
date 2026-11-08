using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.CargoPrices
{
	public class CargoPriceTypeListItem : ScrollListItem<CargoPriceTypeData>
	{
		public Image Icon;

		public TextMeshProUGUI NameLabel;

		public Color TextColorWithPrice = Color.white;

		public Color TextColorWithoutPrices = Color.grey;

		public override void Refresh()
		{
			base.Refresh();
			RefreshNameLabelText();
			RereshNameLabelColor();
			RefreshIconSprite();
		}

		private void RereshNameLabelColor()
		{
			NameLabel.color = (Item.HasPrices ? TextColorWithPrice : TextColorWithoutPrices);
		}

		private void RefreshNameLabelText()
		{
			NameLabel.text = Item.CargoClass.ClassName;
		}

		private void RefreshIconSprite()
		{
			Icon.sprite = EngineASX.Instance.EngineResources.GetCargoSpriteOrDefault(Item.CargoClass);
		}
	}
}
