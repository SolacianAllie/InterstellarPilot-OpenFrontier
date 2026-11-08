using OpenFrontier.IP.Engine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.UniverseScenePicker
{
	public class SectorPickerItemListItem : ScrollListItem<SectorPickerItem>
	{
		public SectorDisplayIcons SectorDisplayIcons;

		public SectorPathIconsDisplay SectorPathIconsDisplay;

		public Text NameLabel;

		public override void Refresh()
		{
			base.Refresh();
			bool flag = !Item.ShowExploredStatus || (Item.Sector != null && Item.Sector.IsDiscoveredByLocalFaction());
			if (Item.Sector != null)
			{
				if (flag)
				{
					NameLabel.text = TextFormattingHelper.GetSectorNameAndDistance(Item.Sector, EngineASX.Instance.ActiveSector);
				}
				else
				{
					NameLabel.text = Item.Sector.Name;
				}
			}
			else
			{
				NameLabel.text = "[None]";
			}
			SectorPathIconsDisplay.Sector = Item.Sector;
			SectorPathIconsDisplay.Refresh();
			if (flag)
			{
				SectorDisplayIcons.Sector = Item.Sector;
			}
			else
			{
				SectorDisplayIcons.Sector = null;
			}
			SectorDisplayIcons.Refresh();
			NameLabel.color = (flag ? EngineASX.Instance.DiscoveredSectorColor : EngineASX.Instance.UndiscoveredSectorColor);
		}
	}
}
