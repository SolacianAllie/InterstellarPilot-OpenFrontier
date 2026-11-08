using OpenFrontier.IP.Engine.CargoFactory;

namespace OpenFrontier.IP.UI.Screens.CargoFactory
{
	public class CargoFactoryItemListItemUI : ScrollListItem<CargoFactoryItem>
	{
		public CargoFactoryProfileItemUI ProfileItemUI;

		public CargoFactoryItemStatusUI ItemStatusUI;

		public override void Refresh()
		{
			base.Refresh();
			ProfileItemUI.Refresh(Item.Profile);
			ItemStatusUI.Refresh(Item);
		}
	}
}
