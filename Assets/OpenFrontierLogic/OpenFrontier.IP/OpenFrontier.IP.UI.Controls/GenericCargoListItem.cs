using OpenFrontier.IP.Engine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Controls
{
	public class GenericCargoListItem : ScrollListItem<CargoBayItem>
	{
		public Image Icon;

		public Text NameLabel;

		public Text QuantityLabel;

		public Text TotalVolumeLabel;

		public override void Refresh()
		{
			base.Refresh();
			QuantityLabel.text = TextFormattingHelper.FormatNumber(Item.Quantity);
			NameLabel.text = Item.CargoClass.ClassName;
			TotalVolumeLabel.text = TextFormattingHelper.FormatCargoVolume(Item.Load);
			Icon.sprite = EngineASX.Instance.EngineResources.GetCargoSpriteOrDefault(Item.CargoClass);
		}
	}
}
