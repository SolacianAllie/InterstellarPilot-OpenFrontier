using OpenFrontier.IP.Engine.Fleets.FleetFormations;
using TMPro;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.FleetFormations
{
	public class FleetFormationStyleItem : ScrollListItem<FleetFormationStyle>
	{
		public TextMeshProUGUI Label;

		public Image FormationImage;

		public override void Refresh()
		{
			base.Refresh();
			Label.text = Item.Name;
			FormationImage.sprite = Item.Sprite;
		}
	}
}
