using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.Fleets
{
	public class FleetListItemWrapper : ScrollListItem<Fleet>
	{
		public FleetListItem ListItemDisplay;

		public override void Refresh()
		{
			base.Refresh();
			ListItemDisplay.Refresh(Item);
		}
	}
}
