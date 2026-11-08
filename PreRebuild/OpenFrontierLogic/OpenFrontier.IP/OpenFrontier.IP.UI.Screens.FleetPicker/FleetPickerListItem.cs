using OpenFrontier.IP.UI.Screens.Fleets;

namespace OpenFrontier.IP.UI.Screens.FleetPicker
{
	public class FleetPickerListItem : ScrollListItem<FleetPickerItem>
	{
		public FleetListItem ListItemDisplay;

		public override void Refresh()
		{
			base.Refresh();
			ListItemDisplay.Refresh(Item.Fleet);
		}
	}
}
