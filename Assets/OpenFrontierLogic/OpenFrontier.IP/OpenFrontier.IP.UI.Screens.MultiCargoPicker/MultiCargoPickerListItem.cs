using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.MultiCargoPicker
{
	public class MultiCargoPickerListItem : ScrollListItem<MultiCargoPickerItem>
	{
		public Image CargoIconImage;

		public TextMeshProUGUI NameLabel;

		public override void Refresh()
		{
			base.Refresh();
			CargoIconImage.sprite = EngineASX.Instance.EngineResources.GetCargoSpriteOrDefault(Item.CargoClass);
			NameLabel.text = GetNameText();
		}

		private string GetNameText()
		{
			if (Item.QuantityAvailable.HasValue)
			{
				return Item.CargoClass.ClassName + " (" + TextFormattingHelper.FormatCargoAmount(Item.QuantityAvailable.Value) + " " + Item.AvailableText + ")";
			}
			return Item.CargoClass.ClassName;
		}
	}
}
