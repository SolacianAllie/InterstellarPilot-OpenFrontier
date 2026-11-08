using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class PassengersScreenListItem : ScrollListItem<PassengerGroup>
	{
		public UnitPathIconsDisplay UnitPathIconsDisplay;

		public Transform CountItemHolder;

		public TextMeshProUGUI DestinationLabel;

		public TextMeshProUGUI DestinationSectorLabel;

		public Text FareLabel;

		public Text PassengersOnBoardLabel;

		public PassengersScreen PassengerInfoScreen => ((PassengersScreenItemList)ParentList).PassengerInfoScreen;

		public override void Refresh()
		{
			base.Refresh();
			if (!Item.CachedRevenue.HasValue)
			{
				Item.CacheRevenue();
			}
			RefreshPassengerCount();
			RefreshDestinationSceneLabel();
			if (DestinationLabel != null)
			{
				RefreshDestinationText();
			}
			if (FareLabel != null)
			{
				RefreshFareText();
			}
			UnitPathIconsDisplay.Unit = Item.Destination;
			PassengersOnBoardLabel.gameObject.SetActive(Item.CurrentUnit == PassengerInfoScreen.DockUI.PlayerCurrentUnit);
		}

		private void RefreshPassengerCount()
		{
			UnityObjectHelper.DestroyChildren(CountItemHolder, destroyImmediate: true);
			for (int i = 0; i < Item.PassengerCount; i++)
			{
				UnityObjectHelper.InstantiateAndGetComponent(PassengerInfoScreen.PassengerCountPrefab).transform.SetParent(CountItemHolder.transform, worldPositionStays: false);
			}
		}

		private void RefreshDestinationText()
		{
			DestinationLabel.text = GetDestinationText();
			DestinationLabel.color = PassengerInfoScreen.Eng.GetFactionHostilityColor(Item.Destination.Faction, PassengerInfoScreen.Eng.LocalPlayer.Person.Faction);
		}

		private string GetDestinationText()
		{
			if (Item.Destination != null)
			{
				string text = Item.Destination.GetDesignationOrClassAndSeries();
				if (Item.Destination.Faction != null)
				{
					text += $" ({Item.Destination.Faction.GetShortNameElseLong()})";
				}
				return text;
			}
			return "Unknown";
		}

		private void RefreshFareText()
		{
			if (FareLabel != null)
			{
				if (Item.IsValid)
				{
					FareLabel.text = TextFormattingHelper.FormatCreditsWithDashForZero(Item.CachedRevenue.GetValueOrDefault());
				}
				else
				{
					FareLabel.text = "-";
				}
			}
		}

		private void RefreshDestinationSceneLabel()
		{
			if (Item.Destination != null && Item.Destination.Sector != null && Item.CurrentUnit != null)
			{
				DestinationSectorLabel.text = UnitNamer.GetSectorNameAndDistanceForFaction(Item.CurrentUnit.Faction, EngineASX.Instance.LocalPlayerSector, Item.Destination.Sector);
			}
			else
			{
				DestinationSectorLabel.text = string.Empty;
			}
		}
	}
}
