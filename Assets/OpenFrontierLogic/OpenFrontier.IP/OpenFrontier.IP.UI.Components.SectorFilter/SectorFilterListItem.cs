using TMPro;

namespace OpenFrontier.IP.UI.Components.SectorFilter
{
	public class SectorFilterListItem : ScrollListItem<SectorFilterItem>
	{
		public TextMeshProUGUI NameLabel;

		public override void Refresh()
		{
			base.Refresh();
			if (Item.Sector != null)
			{
				NameLabel.text = Item.Sector.Name;
			}
			else
			{
				NameLabel.text = "[All]";
			}
		}
	}
}
