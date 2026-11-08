using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.FleetFormations;
using OpenFrontier.IP.UI.Screens.FleetFormations;
using OpenFrontier.IP.UI.Screens.RenameUnit;
using OpenFrontier.IP.UI.Screens.UniverseMap;
using OpenFrontier.Unity.Utils;
using TMPro;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.FactionSettings
{
	public class FactionSettingsScreen : EngineScreen
	{
		public Toggle TradeIllegalCargoToggle;

		public Button PreferredFormationButton;

		public Button RandomizePilotTitleButton;

		public TextMeshProUGUI PreferredFormationLabel;

		public Image PreferredFormationImage;

		public Button RenamePilotButton;

		public Button RenamePilotTitleButton;

		public Button AutopilotExcludeSectorsButton;

		private Faction faction;

		public Button RenameFactionButton;

		public Button RenameFactionShortNameButton;

		public Text PilotTitleText;

		public Text PilotNameText;

		public Text FactionNameText;

		public Text FactionShortNameText;

		public Faction Faction
		{
			get
			{
				return faction;
			}
			set
			{
				faction = value;
			}
		}

		protected override void awake()
		{
			base.awake();
			RenamePilotButton.onClick.AddListener(RenamePilotButtonClick);
			RenamePilotTitleButton.onClick.AddListener(RenamePilotTitleButtonClick);
			RenameFactionButton.onClick.AddListener(RenameFactionButtonClick);
			RenameFactionShortNameButton.onClick.AddListener(RenameFactionShortNameButtonClick);
			AutopilotExcludeSectorsButton.onClick.AddListener(AutopilotExcludeSectorsButtonClick);
			PreferredFormationButton.onClick.AddListener(PreferredFormationButtonClick);
			RandomizePilotTitleButton.onClick.AddListener(RandomizePilotTitleButtonClick);
			TradeIllegalCargoToggle.onValueChanged.AddListener(TradeIllegalCargoToggle_ValueChanged);
		}

		private void TradeIllegalCargoToggle_ValueChanged(bool value)
		{
			if (faction != null)
			{
				faction.TradeIllegalGoods = value;
			}
		}

		protected override void refresh()
		{
			base.refresh();
			if (faction != null)
			{
				FactionNameText.text = faction.Name;
				FactionShortNameText.text = faction.ShortName;
				if (faction.PreferredFormationStyle != null)
				{
					PreferredFormationImage.sprite = faction.PreferredFormationStyle.Sprite;
					PreferredFormationLabel.text = faction.PreferredFormationStyle.Name;
				}
				else
				{
					PreferredFormationLabel.text = "[None]";
				}
			}
			if (Eng.LocalPlayer != null)
			{
				PilotNameText.text = Eng.LocalPlayer.Person.CustomName;
				PilotTitleText.text = Eng.LocalPlayer.Person.CustomTitle;
			}
			TradeIllegalCargoToggle.onValueChanged.RemoveAllListeners();
			TradeIllegalCargoToggle.isOn = faction.TradeIllegalGoods;
			TradeIllegalCargoToggle.onValueChanged.AddListener(TradeIllegalCargoToggle_ValueChanged);
			AutopilotExcludeSectorsButton.interactable = Eng.LocalFaction.Intel.DiscoveredSectorIds.Count > 1;
		}

		private void AutopilotExcludeSectorsButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowCustomUniverseMapScreen((UniverseMapScreen universeMap) =>
			{
				universeMap.Title = "Select sectors to exclude navigation...";
				universeMap.AllowSectorSelection = true;
				universeMap.AllowSectorSelectionPick = false;
				universeMap.ShowSelectedSectorInfo = false;
				universeMap.SelectingSectorItem = (UniverseMapItemUI item) =>
				{
					EngineASX.Instance.LocalFaction.ToggleAllowSectorNavigation(item.Sector);
					item.Refresh();
					return false;
				};
				universeMap.RestrictNavigationAway();
			});
		}

		private void PreferredFormationButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowFleetFormationStylePickerScreen(EngineASX.Instance.LocalFaction.PreferredFormationStyle, (FleetFormationStylePickerScreen picker, FleetFormationStyle newStyle) =>
			{
				EngineASX.Instance.LocalFaction.PreferredFormationStyle = newStyle;
				if (EngineASX.Instance.CachedFleetSettingsController.HasDefaultFleetSettings)
				{
					EngineASX.Instance.CachedFleetSettingsController.DefaultFleetSettings.FormationId = newStyle.UniqueId;
				}
				picker.NavigateBack();
				Refresh();
			});
		}

		private void RandomizePilotTitleButtonClick()
		{
			EngineASX.Instance.LocalPlayer.Person.CustomTitle = GameController.Instance.GameSettings.RandomTitles.Where((string e) => e != EngineASX.Instance.LocalPlayer.Person.CustomTitle).GetRandom();
			Refresh();
		}

		private void OnRenamePilotConfirmed(RenameUnitScreen sender, bool rename, string newName)
		{
			if (rename)
			{
				Eng.LocalPlayer.Person.CustomName = newName;
				Eng.LocalPlayer.Person.RefreshName();
				Refresh();
			}
		}

		private void OnRenamePilotTitleConfirmed(RenameUnitScreen sender, bool rename, string newName)
		{
			if (rename)
			{
				Eng.LocalPlayer.Person.CustomTitle = newName;
				Refresh();
			}
		}

		private void OnRenameFactionConfirmed(RenameUnitScreen sender, bool rename, string newName)
		{
			if (rename)
			{
				faction.Name = newName;
				faction.ClearGeneratedName();
				Refresh();
			}
		}

		private void OnRenameFactionShortNameConfirmed(RenameUnitScreen sender, bool rename, string newName)
		{
			if (rename)
			{
				faction.ShortName = newName;
				faction.ClearGeneratedName();
				Refresh();
			}
		}

		private void RenamePilotButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowRenameScreen(Eng.LocalPlayer.Person.CustomName, Eng.GameSettings.PilotNameMinChars, Eng.GameSettings.PilotNameMaxChars, OnRenamePilotConfirmed);
		}

		private void RenamePilotTitleButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowRenameScreen(Eng.LocalPlayer.Person.CustomTitle, Eng.GameSettings.PilotTitleMinChars, Eng.GameSettings.PilotTitleMaxChars, OnRenamePilotTitleConfirmed);
		}

		private void RenameFactionButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowRenameScreen(faction.Name, Eng.GameSettings.FactionMinNameChars, Eng.GameSettings.FactionMaxLongNameChars, OnRenameFactionConfirmed);
		}

		private void RenameFactionShortNameButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowRenameScreen(faction.ShortName, Eng.GameSettings.FactionMinShortNameChars, Eng.GameSettings.FactionMaxShortNameChars, OnRenameFactionShortNameConfirmed);
		}
	}
}
