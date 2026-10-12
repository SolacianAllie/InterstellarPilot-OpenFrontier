using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions.Bounty;
using OpenFrontier.IP.Engine.Hypersleep;
using OpenFrontier.IP.Engine.MissionSpecs;
using OpenFrontier.IP.Scratchcard;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.ActiveMission;
using OpenFrontier.IP.UI.Screens.RenameUnit;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class DockFacilitiesUI : MonoBehaviour
	{
		public Button HypersleepButton;

		public Button BuildButton;

		public UnitContextButton UnitContextButton;

		public Button CinematicButton;

		public Button RenameStationButton;

		public Button ScratchcardButton;

		public Button StationInfoButton;

		public MissionOptionsController MissionOptionsController;

		public Button CompleteMissionButton;

		public Button BountyButton;

		public Button BuySectorIntelButton;

		public Button BuyShipButton;

		public Button CargoPricesButton;

		public DockingBayButtonController DockedShipsButtonController;

		[FormerlySerializedAs("MissionSpecsButton")]
		public Button JobBoardButton;

		public Text MissionSpecsLabel;

		public Button RequestTaxiButton;

		private float lastTimeRefreshed;

		public RectTransform ButtonsLayoutRect;

		public EngineASX Eng => EngineASX.Instance;

		public DockUI DockUI => Eng.DockUI;

		private void Awake()
		{
			CargoPricesButton.onClick.AddListener(CargoPricesButtonClick);
			BuyShipButton.onClick.AddListener(BuyShipButtonClick);
			JobBoardButton.onClick.AddListener(JobBoardButtonClick);
			RequestTaxiButton.onClick.AddListener(RequestTaxiButtonClick);
			BuySectorIntelButton.onClick.AddListener(BuySectorIntelButtonClick);
			BountyButton.onClick.AddListener(BountyButtonClick);
			CompleteMissionButton.onClick.AddListener(CompleteMissionButtonClick);
			StationInfoButton.onClick.AddListener(StationInfoButtonClick);
			ScratchcardButton.onClick.AddListener(ScratchcardButtonClick);
			RenameStationButton.onClick.AddListener(RenameStationButtonClick);
			CinematicButton.onClick.AddListener(CinematicButtonClick);
			BuildButton.onClick.AddListener(BuildButtonClick);
			HypersleepButton.onClick.AddListener(HypersleepButtonClick);
		}

		private void HypersleepButtonClick()
		{
			HypersleepHelper.TryEnterHypersleep();
		}

		private void BuildButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowBuildModeScreen();
		}

		private void CompleteMissionButtonClick()
		{
			Eng.LocalPlayer.ActiveMission.ManualComplete();
		}

		private void StationInfoButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowUnitInfoScreen(DockUI.PlayerDockUnit);
		}

		private void RenameStationButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowRenameUnitScreen(DockUI.PlayerDockUnit, RenameStationConfirmed);
		}

		private void CinematicButtonClick()
		{
			if (EngineASX.Instance.PlayerUnit != null && EngineASX.Instance.PlayerUnit.IsValidAndNotDestroyed)
			{
				EngineASX.Instance.StartCustomCinematic(allowTogglePause: true);
			}
		}

		private void RenameStationConfirmed(RenameUnitScreen sender, bool rename, string name)
		{
			if (rename)
			{
				DockUI.PlayerDockUnit.UnitName = name;
			}
		}

		private void ScratchcardButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowScratchcardScreen(Eng.LocalFaction);
		}

		private void BountyButtonClick()
		{
			FactionBountyBoard nearestBountyBoardToPlayer = EngineASX.Instance.GetNearestBountyBoardToPlayer();
			if (nearestBountyBoardToPlayer != null)
			{
				UIController.Instance.ScreenNavigator.ShowBountyScreen(nearestBountyBoardToPlayer);
			}
		}

		private void BuyShipButtonClick()
		{
			if (DockUI.PlayerDockUnit.ShipTrader != null)
			{
				UIController.Instance.ScreenNavigator.ShowShipTradeScreen(DockUI.PlayerDockUnit, null);
			}
		}

		private void JobBoardButtonClick()
		{
			List<MissionSpec> jobsAtUnit = EngineASX.Instance.GetJobsAtUnit(DockUI.PlayerDockUnit);
			if (jobsAtUnit == null || !jobsAtUnit.Any())
			{
				UIController.Instance.ShowMessageBox("There are no jobs available at this time.", MessageBoxButtons.Ok);
			}
			else
			{
				UIController.Instance.ScreenNavigator.ShowJobBoardScreen();
			}
		}

		private void BuySectorIntelButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowBuySectorIntelScreen(DockUI.PlayerDockUnit, Eng.LocalPlayer);
		}

		private void RequestTaxiButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowRequestTaxiScreen();
		}

		private void Update()
		{
			if (Eng != null && Eng.LocalPlayer != null && Eng.LocalUnit != null && DockUI.PlayerSector != null)
			{
				RefreshVolatileButtonsVisible(out var buttonsChanged);
				if (buttonsChanged)
				{
					RebuildLayout();
				}
				DockedShipsButtonController.Unit = Eng.DockUI.PlayerDockUnit;
				MissionOptionsController.Mission = Eng.LocalPlayer.ActiveMission;
				if (Time.time > lastTimeRefreshed + 1f)
				{
					Refresh();
					lastTimeRefreshed = Time.time;
				}
			}
		}

		private bool ShouldShowRenameStationButton()
		{
			if (Eng.PlayerRootUnit != null && Eng.PlayerRootUnit.IsOwnedByPlayer)
			{
				return Eng.PlayerRootUnit.UnitType == UnitType.Station;
			}
			return false;
		}

		public static bool ShouldShowMissionSpecsButton()
		{
			WorldBase world = EngineASX.Instance.World;
			Unit rootUnit = EngineASX.Instance.PlayerUnit.GetRootUnit();
			if (world.Permissions.AllowDockMissionSpecs && rootUnit.UnitType == UnitType.Station && rootUnit.Faction != null && !rootUnit.IsOwnedByPlayer)
			{
				return rootUnit.Faction.ShouldFactionShowMissionSpecs;
			}
			return false;
		}

		private bool ShouldShowCompleteMissionButton()
		{
			GamePlayer localPlayer = Eng.LocalPlayer;
			if (localPlayer != null && localPlayer.ActiveMission != null && localPlayer.ActiveMission.ManualCompletionEnabled)
			{
				return localPlayer.ActiveMission.CanPlayerManualComplete();
			}
			return false;
		}

		public bool ShouldShowBuyShipButton()
		{
			return ShouldShowBuyShipButton(DockUI.PlayerDockUnit);
		}

		public static bool ShouldShowBuyShipButton(Unit dockUnit)
		{
			UnitShipTrader shipTrader = dockUnit.ShipTrader;
			if (EngineASX.Instance.World.Permissions.AllowDockUIShipTrader && shipTrader != null)
			{
				return shipTrader.GetShipItems().Count > 0;
			}
			return false;
		}

		public bool ShouldShowBountyButton()
		{
			return EngineASX.Instance.GetNearestBountyBoardToPlayer() != null;
		}

		public bool ShouldShowBuySectorIntelButton()
		{
			return false;
		}

		public bool ShouldShowScratchcardButton()
		{
			return DockUI.PlayerDockUnit.UnitClass.GetComponent<ScratchcardSeller>() != null;
		}

		public bool ShouldShowTaxiButton()
		{
			Unit playerDockUnit = DockUI.PlayerDockUnit;
			if (playerDockUnit != null)
			{
				return playerDockUnit.UnitType == UnitType.Station;
			}
			return false;
		}

		public bool ShouldShowBuildButton()
		{
			return DockUI.PlayerDockUnit.IsOwnedByPlayer;
		}

		public void Refresh()
		{
			RefreshButtons();
			BountyButton.interactable = ShouldShowBountyButton();
			UnitContextButton.SetUnit(Eng.DockUI.PlayerDockUnit);
			RefreshJobBoardLabel();
			RebuildLayout();
		}

		private void RebuildLayout()
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(ButtonsLayoutRect);
		}

		private void CargoPricesButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowCargoPricesScreen(null);
		}

		private void RefreshButtons()
		{
			JobBoardButton.gameObject.SetActive(ShouldShowMissionSpecsButton());
			BuyShipButton.gameObject.SetActive(ShouldShowBuyShipButton());
			RequestTaxiButton.gameObject.SetActive(ShouldShowTaxiButton());
			CargoPricesButton.gameObject.SetActive(Eng.World.Permissions.AllowCargoPricesButton);
			ScratchcardButton.gameObject.SetActive(ShouldShowScratchcardButton());
			RefreshVolatileButtonsVisible(out var buttonsChanged);
			BuildButton.gameObject.SetActive(ShouldShowBuildButton());
			bool active = HypersleepHelper.ShouldShowHypersleepOption();
			HypersleepButton.gameObject.SetActive(active);
			RefreshVolatileButtonsVisible(out buttonsChanged);
		}

		private void RefreshVolatileButtonsVisible(out bool buttonsChanged)
		{
			buttonsChanged = false;
			bool flag = ShouldShowBuySectorIntelButton();
			if (flag != BuySectorIntelButton.gameObject.activeSelf)
			{
				BuySectorIntelButton.gameObject.SetActive(flag);
				buttonsChanged = true;
			}
			bool flag2 = ShouldShowCompleteMissionButton();
			if (flag2 != CompleteMissionButton.gameObject.activeSelf)
			{
				CompleteMissionButton.gameObject.SetActive(flag2);
				buttonsChanged = true;
			}
		}

		private void RefreshJobBoardLabel()
		{
			if (MissionSpecsLabel != null)
			{
				int missionSpecCount = GetMissionSpecCount(DockUI.PlayerDockUnit);
				if (missionSpecCount == 0)
				{
					MissionSpecsLabel.text = "Job Board";
				}
				else
				{
					MissionSpecsLabel.text = $"Job Board ({missionSpecCount})";
				}
				MissionSpecsLabel.SetTextColorFromActiveState(missionSpecCount > 0);
			}
		}

		private int GetMissionSpecCount(Unit unit)
		{
			return EngineASX.Instance.GetJobsAtUnit(unit)?.Count ?? 0;
		}

		private UnitHangar FindHangar()
		{
			if (DockUI != null && DockUI.PlayerDockUnit != null && DockUI.PlayerDockUnit.HasHangar())
			{
				return DockUI.PlayerDockUnit.Components.HangarComponent;
			}
			return null;
		}
	}
}
