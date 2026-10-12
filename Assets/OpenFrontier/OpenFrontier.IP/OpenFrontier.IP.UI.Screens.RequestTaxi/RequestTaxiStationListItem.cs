using OpenFrontier.IP.Engine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.RequestTaxi
{
	public class RequestTaxiStationListItem : ScrollListItem<Unit>
	{
		public Text FactionLabel;

		public Text StationNameLabel;

		public Image IconImage;

		public override void Refresh()
		{
			base.Refresh();
			RefreshNameLabel();
			EngineASX instance = EngineASX.Instance;
			RefreshFactionLabelColor(instance);
			RefreshIconImage();
			RefreshFactionLabel();
		}

		private void RefreshNameLabel()
		{
			StationNameLabel.text = Item.GetFriendlyName();
		}

		private void RefreshIconImage()
		{
			IconImage.sprite = EngineASX.Instance.EngineResources.GetUnitClassIconSpriteOrDefault(Item.UnitClass);
		}

		private void RefreshFactionLabelColor(EngineASX engine)
		{
			FactionLabel.color = engine.GetFactionHostilityColor(Item.Faction, engine.LocalPlayer.Person.Faction);
		}

		private void RefreshFactionLabel()
		{
			if (Item.Faction != null)
			{
				FactionLabel.text = Item.Faction.GetShortNameElseLong();
			}
			else
			{
				FactionLabel.text = "-";
			}
		}
	}
}
