using OpenFrontier.IP.Engine.CargoFactory;

namespace OpenFrontier.IP.UI.Screens.CargoFactory
{
	public class CargoFactoryProfileItemListItemUI : ScrollListItem<CargoFactoryProfileItem>
	{
		public CargoFactoryProfileItemUI ProfileItemUI;

		public override void Refresh()
		{
			base.Refresh();
			ProfileItemUI.Refresh(Item);
		}
	}
}
