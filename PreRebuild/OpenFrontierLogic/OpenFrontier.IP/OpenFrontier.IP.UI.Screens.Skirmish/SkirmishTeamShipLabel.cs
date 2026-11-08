using OpenFrontier.IP.Engine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Skirmish
{
	public class SkirmishTeamShipLabel : ScrollListItem<SkirmishShipItem>
	{
		public Image IconImage;

		public Text NameLabel;

		public override void Refresh()
		{
			base.Refresh();
			if (Item.UnitClass != null)
			{
				string text = Item.GetClassAndSeriesName();
				if (Item.IsPlayer)
				{
					text += " (You)";
				}
				NameLabel.text = text;
				IconImage.sprite = EngineASX.Instance.EngineResources.GetUnitClassIconSpriteOrDefault(Item.UnitClass);
			}
			else
			{
				NameLabel.text = string.Empty;
			}
		}
	}
}
