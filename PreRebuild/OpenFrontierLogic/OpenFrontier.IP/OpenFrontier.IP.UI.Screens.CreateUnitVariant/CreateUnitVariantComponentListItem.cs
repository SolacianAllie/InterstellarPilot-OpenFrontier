using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.CreateUnitVariant
{
	public class CreateUnitVariantComponentListItem : ScrollListItem<ComponentClass>
	{
		public Image IconImage;

		public Text InstalledLabel;

		public Text NameLabel;

		public Text PriceLabel;

		public CreateUnitVariantComponentsScreen ParentScreen => ((CreateUnitVariantComponentList)ParentList).ParentScreen;

		public override void Refresh()
		{
			base.Refresh();
			if (Item != null)
			{
				NameLabel.text = Item.GetFriendlyName();
				UpdatePriceLabel();
				ComponentBase installedComponent = ParentScreen.CurrentBay.InstalledComponent;
				InstalledLabel.gameObject.SetActive(installedComponent != null && Item == installedComponent.ComponentClass);
				IconImage.sprite = EngineASX.Instance.EngineResources.GetComponentClassOrBaySpriteOrDefault(Item);
			}
		}

		private void Update()
		{
			if (Item != null)
			{
				UpdatePriceLabel();
			}
		}

		private void UpdatePriceLabel()
		{
			int buyPrice = ParentScreen.GetBuyPrice(Item);
			PriceLabel.text = TextFormattingHelper.FormatCredits(buyPrice, includeSuffix: true);
		}
	}
}
