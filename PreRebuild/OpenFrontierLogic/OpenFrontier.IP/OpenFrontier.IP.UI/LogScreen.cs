using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions.Bounty;
using OpenFrontier.IP.UI.PlayerStats;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.Fleets;
using OpenFrontier.IP.UI.Screens.TopPilots;
using OpenFrontier.IP.UI.Screens.UniverseMap;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class LogScreen : EngineScreen
	{
		private float lastTimeUpdatedStats;

		public Button FactionSettingsButton;

		public Button TopPilotsButton;

		public Button BountyBoardButton;

		public Button GodModeButton;

		public Button CargoPricesButton;

		public Button TransactionsButton;

		public PlayerStatGenerator PlayerStatGenerator;

		public Button FactionsButton;

		public Text LevelLabel;

		public Button MessagesButton;

		public Button MissionsButton;

		public Button PropertyButton;

		public Button SectorMapButton;

		public Button UniverseMapButton;

		public Button FleetsButton;

		public Text XpLabel;

		public bool ShouldShowMissions()
		{
			return Eng.World.Permissions.AllowDockUIMissions;
		}

		public bool ShouldShowUniverseMap()
		{
			return Eng.World.Permissions.AllowDockUIMap;
		}

		public bool ShouldShowSectorMap()
		{
			return Eng.World.Permissions.AllowDockUIMap;
		}

		public bool ShouldShowFactionInfo()
		{
			return Eng.World.Permissions.AllowDockUIFactionInfo;
		}

		protected override void awake()
		{
			base.awake();
			PropertyButton.onClick.AddListener(PropertyButton_Activated);
			SectorMapButton.onClick.AddListener(ShowSectorMapButtonActivated);
			UniverseMapButton.onClick.AddListener(ShowUniverseMapButtonActivated);
			MessagesButton.onClick.AddListener(MessagesButtonClick);
			FactionsButton.onClick.AddListener(FactionsButtonClick);
			MissionsButton.onClick.AddListener(MissionsButtonClick);
			TransactionsButton.onClick.AddListener(TransactionsButtonClick);
			CargoPricesButton.onClick.AddListener(CargoPricesButtonClick);
			BountyBoardButton.onClick.AddListener(BountyButtonClick);
			FleetsButton.onClick.AddListener(FleetsButtonClick);
			TopPilotsButton.onClick.AddListener(TopPilotsButtonClick);
			FactionSettingsButton.onClick.AddListener(FactionSettingsButtonClick);
			GodModeButton.onClick.AddListener(GodModeButtonClick);
		}

		private void FleetsButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowFleetsScreen();
		}

		private void BountyButtonClick()
		{
			FactionBountyBoard nearestBountyBoardToPlayer = EngineASX.Instance.GetNearestBountyBoardToPlayer();
			if (nearestBountyBoardToPlayer != null)
			{
				UIController.Instance.ScreenNavigator.ShowBountyScreen(nearestBountyBoardToPlayer);
			}
		}

		private void FactionSettingsButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowFactionSettingsScreen(EngineASX.Instance.LocalFaction);
		}

		protected override void refresh()
		{
			base.refresh();
			UniverseMapButton.gameObject.SetActive(ShouldShowUniverseMap());
			SectorMapButton.gameObject.SetActive(ShouldShowSectorMap());
			FactionsButton.gameObject.SetActive(ShouldShowFactionInfo());
			MissionsButton.gameObject.SetActive(ShouldShowMissions());
			MessagesButton.gameObject.SetActive(Eng.AllowUiMessagesNavigation());
			PropertyButton.gameObject.SetActive(ShouldShowPropertyButton());
			FactionSettingsButton.gameObject.SetActive(ShouldShowFactionSettingsButton());
			RefreshXpLabel();
			RefreshLevelText();
			CargoPricesButton.gameObject.SetActive(Eng.World.Permissions.AllowCargoPricesButton);
			if (PlayerStatGenerator != null)
			{
				PlayerStatGenerator.Item = Eng.LocalPlayer;
				PlayerStatGenerator.Refresh();
				lastTimeUpdatedStats = RealTime.time;
			}
			TopPilotsButton.interactable = TopPilotsScreen.GetTopPilots(1).Any();
			GodModeButton.gameObject.SetActive(GameController.Instance.ShouldShowGodModeButton && EngineASX.Instance.World.Permissions.AllowGodMode);
			RefreshVolatile();
		}

		private void GodModeButtonClick()
		{
			if (GameController.Instance.IsGodModePurchased)
			{
				UIController.Instance.ScreenNavigator.ShowGodModeScreen();
			}
			else
			{
				UIController.Instance.ScreenNavigator.ShowStoreScreen();
			}
		}

		protected override void update()
		{
			base.update();
			if (RealTime.time > lastTimeUpdatedStats + 0.25f)
			{
				PlayerStatGenerator.Refresh();
				lastTimeUpdatedStats = RealTime.time;
				RefreshVolatile();
			}
		}

		private void RefreshVolatile()
		{
			TopPilotsButton.interactable = TopPilotsScreen.GetTopPilots(1).Any();
			FleetsButton.interactable = FleetsHelper.PlayerHasAnyVisibleFleets();
			BountyBoardButton.interactable = EngineASX.Instance.GetNearestBountyBoardToPlayer() != null;
		}

		private void CargoPricesButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowCargoPricesScreen(null);
		}

		private void TransactionsButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowTransactionsScreen(Eng.LocalFaction);
		}

		private void MissionsButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowMissionsScreen();
		}

		private void FactionsButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowFactionsScreen();
		}

		private void PropertyButton_Activated()
		{
			UIController.Instance.ScreenNavigator.ShowPropertyScreen();
		}

		private void TopPilotsButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowTopPilotsScreen();
		}

		private void MessagesButtonClick()
		{
			ShowMessagesIfAny();
		}

		private void ShowMessagesIfAny()
		{
			if (PlayerHasDisplaybleMessages())
			{
				UIController.Instance.ScreenNavigator.ShowMessagesScreen();
			}
			else
			{
				ShowNoMessagesPopup();
			}
		}

		private void ShowNoMessagesPopup()
		{
			UIController.Instance.ShowMessageBox("You do not have any messages", MessageBoxButtons.Ok);
		}

		private bool PlayerHasDisplaybleMessages()
		{
			return PlayerMessagesHelper.HasAnyMessages();
		}

		private void ShowUniverseMapButtonActivated()
		{
			UIController.Instance.ScreenNavigator.ShowCustomUniverseMapScreen((UniverseMapScreen screen) =>
			{
				screen.AutoSelectPlayerSector();
				screen.CenterOnPlayerSector();
			});
		}

		private void ShowSectorMapButtonActivated()
		{
			UIController.Instance.ScreenNavigator.ShowSectorMapScreenWhenPilotting();
		}

		private void RefreshLevelText()
		{
			if (LevelLabel != null)
			{
				LevelLabel.text = "Lvl " + Eng.LocalPlayer.Level;
			}
		}

		private bool ShouldShowFactionSettingsButton()
		{
			return Eng.World.Permissions.AllowFactionSettings;
		}

		private bool ShouldShowPropertyButton()
		{
			return Eng.World.Permissions.AllowPropertyScreen;
		}

		private void RefreshXpLabel()
		{
			if (XpLabel != null)
			{
				XpLabel.text = CalculateXpLabelText();
			}
		}

		private string CalculateXpLabelText()
		{
			return string.Format("XP {0} / {1}", Eng.LocalPlayer.XP, Eng.LocalPlayer.CanLevelUp() ? TextFormattingHelper.FormatNumber(Eng.LocalPlayer.CalculateRemainingLevelUpXP()) : "-");
		}
	}
}
