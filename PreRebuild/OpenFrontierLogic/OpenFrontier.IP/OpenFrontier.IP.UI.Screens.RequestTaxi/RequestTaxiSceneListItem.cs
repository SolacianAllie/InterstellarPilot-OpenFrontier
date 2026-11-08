using OpenFrontier.IP.Engine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.RequestTaxi
{
	public class RequestTaxiSceneListItem : ScrollListItem<Sector>
	{
		public Text SceneNameLabel;

		public SectorDisplayIcons SectorDisplayIcons;

		public SectorPathIconsDisplay SectorPathIconsDisplay;

		public override void Refresh()
		{
			base.Refresh();
			SceneNameLabel.text = TextFormattingHelper.GetSectorNameAndDistance(Item, EngineASX.Instance.ActiveSector);
			SectorDisplayIcons.Sector = Item;
			SectorDisplayIcons.Refresh();
			SectorPathIconsDisplay.Sector = Item;
			SectorPathIconsDisplay.Refresh();
		}
	}
}
