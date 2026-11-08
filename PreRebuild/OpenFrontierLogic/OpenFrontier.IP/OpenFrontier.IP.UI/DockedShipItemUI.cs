using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.UI.Screens;
using TMPro;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class DockedShipItemUI : ScrollListItem<UnitComponentHolder>
	{
		public UnitConditionControllerUI UnitConditionController;

		public Text FactionLabel;

		public TextMeshProUGUI NameLabel;

		public Text OwnedShipLabel;

		public Text CurrentShipLabel;

		public Faction LocalFaction;

		public override void Refresh()
		{
			base.Refresh();
			if (Item != null)
			{
				UnitConditionController.LocalUnit = Item.Unit;
				RefreshNameLabel();
				bool isOwnedByPlayer = Item.Unit.IsOwnedByPlayer;
				OwnedShipLabel.gameObject.SetActive(isOwnedByPlayer);
				CurrentShipLabel.gameObject.SetActive(Item.Unit.IsPlayerCurrentUnit);
				FactionLabel.color = EngineASX.Instance.GetFactionHostilityColor(Item.Unit.Faction, LocalFaction);
				if (Item.Unit.Faction != null)
				{
					FactionLabel.text = Item.Unit.Faction.GetShortNameElseLong();
				}
				else
				{
					FactionLabel.text = "-";
				}
			}
			else
			{
				UnitConditionController.LocalUnit = null;
			}
		}

		private void RefreshNameLabel()
		{
			NameLabel.color = Item.Engine.GetFactionHostilityColor(Item.Unit.Faction, Item.Engine.LocalFaction);
			if (Item.Unit.IsOwnedByPlayer)
			{
				NameLabel.text = ShipListHelper.GetShipNameClassAndFleetWithEmbeddedFleetSprite(Item.Unit, shortName: false, omitFleetNameIfSingleShip: true);
			}
			else
			{
				NameLabel.text = Item.Unit.GetFriendlyName();
			}
		}
	}
}
