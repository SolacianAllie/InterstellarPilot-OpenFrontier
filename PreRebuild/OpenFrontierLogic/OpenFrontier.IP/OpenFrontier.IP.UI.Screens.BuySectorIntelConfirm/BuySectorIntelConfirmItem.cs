using OpenFrontier.IP.Engine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.BuySectorIntelConfirm
{
	public class BuySectorIntelConfirmItem : ScrollListItem<Unit>
	{
		public UnitPathIconsDisplay UnitPathIconsDisplay;

		public Text NameLabel;

		public Image IconImage;

		public Text FactionLabel;

		public override void Refresh()
		{
			base.Refresh();
			RefreshNameLabel();
			EngineASX instance = EngineASX.Instance;
			RefreshFactionLabelColor(instance);
			RefreshIconImage();
			RefreshFactionLabel();
			UnitPathIconsDisplay.Unit = Item;
		}

		private void RefreshNameLabel()
		{
			NameLabel.text = Item.GetFriendlyNameForLocalFaction();
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
				FactionLabel.text = string.Empty;
			}
		}
	}
}
