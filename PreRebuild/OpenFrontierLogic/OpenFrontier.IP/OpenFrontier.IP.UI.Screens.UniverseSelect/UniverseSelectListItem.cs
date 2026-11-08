using OpenFrontier.IP.Engine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.UniverseSelect
{
	public class UniverseSelectListItem : ScrollListItem<ScenarioInfo>
	{
		public Text DescriptionLabel;

		public Text NameLabel;

		public override void Refresh()
		{
			base.Refresh();
			NameLabel.text = Item.Title;
			DescriptionLabel.text = Item.Description;
		}
	}
}
