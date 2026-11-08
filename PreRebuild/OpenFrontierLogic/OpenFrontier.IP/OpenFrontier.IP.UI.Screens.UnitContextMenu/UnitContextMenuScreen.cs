using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Comms;
using OpenFrontier.IP.Engine.ComponentUpgrades;
using OpenFrontier.IP.Engine.Factions.Bounty;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.UI.Screens.RenameUnit;
using OpenFrontier.IP.UI.Screens.SectorMap;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.UnitContextMenu
{
	public class UnitContextMenuScreen : EngineScreen
	{
		public TextMeshProUGUI PassengersLabel;

		public Transform PassengersLabelRoot;

		public Transform HullSliderRoot;

		public HullSlider HullSlider;

		public Transform CapacitorSliderRoot;

		public CapacitorSlider CapacitorSlider;

		public Transform CargoSliderRoot;

		public CargoUsageSlider CargoSlider;

		public Button PayBountyButton;

		public Image UnitRenderImage;

		public UnitConditionControllerUI UnitConditionController;

		public Transform DestructableRoot;

		public Transform OwnedUnitRoot;

		public Button ScanUnitButton;

		public Transform PilotRoot;

		public TextMeshProUGUI PilotNameLabel;

		public Button RenamePilotButton;

		public UnitPathIconsDisplay UnitPathIconsDisplay;

		public FactionContextButton FactionContextButton;

		public Button PassengersButton;

		public Button UpgradeButton;

		public TextMeshProUGUI UnitNameLabel;

		public Button RenameButton;

		public Button OrdersButton;

		public Button SetWaypointButton;

		public Button EnterUnitButton;

		public Button ViewUnitButton;

		public Button ShowCargoButton;

		public Button InfoButton;

		public Button SelfDestructButton;

		public Button DismantleButton;

		public Button CargoTransferButton;

		public Button BuyShipButton;

		public Unit Unit;

		public Transform FactionRoot;

		public DockingBayButtonController DockingBayButtonController;

		public Button ComponentsButton;

		public Button CommsButton;

		protected override void awake()
		{
			base.awake();
			SetWaypointButton.onClick.AddListener(ToggleWaypoint);
			InfoButton.onClick.AddListener(ShowShipInfo);
			OrdersButton.onClick.AddListener(ShowOrders);
			EnterUnitButton.onClick.AddListener(EnterUnitButtonClick);
			ViewUnitButton.onClick.AddListener(ViewUnitButtonClick);
			ShowCargoButton.onClick.AddListener(ShowCargoButtonClick);
			RenameButton.onClick.AddListener(RenameButtonClick);
			SelfDestructButton.onClick.AddListener(SelfDestructButtonClick);
			ComponentsButton.onClick.AddListener(ComponentsButtonClick);
			CargoTransferButton.onClick.AddListener(CargoTransferButtonClick);
			CommsButton.onClick.AddListener(CommsButtonClick);
			UpgradeButton.onClick.AddListener(UpgradeButtonClick);
			PassengersButton.onClick.AddListener(PassengersButtonClick);
			DismantleButton.onClick.AddListener(DismantleButtonClick);
			BuyShipButton.onClick.AddListener(BuyShipButtonClick);
			RenamePilotButton.onClick.AddListener(RenamePilotButtonClick);
			ScanUnitButton.onClick.AddListener(ScanUnitButtonClick);
			PayBountyButton.onClick.AddListener(PayBountyButtonClick);
			FactionRoot.gameObject.SetActive(value: false);
			PilotRoot.gameObject.SetActive(value: false);
		}

		protected override void refresh()
		{
			base.refresh();
			RefreshFaction();
			RefreshPilot();
			if (Unit != null)
			{
				UnitNameLabel.text = Unit.GetFriendlyNameForLocalFaction();
				RefreshCommonButtons();
				OwnedUnitRoot.gameObject.SetActive(Unit.IsOwnedByPlayer);
				if (Unit.IsOwnedByPlayer)
				{
					RefreshOwnedUnitButtons();
				}
				DestructableRoot.gameObject.SetActive(Unit.Destructable != null && Unit.UnitType != UnitType.Asteroid);
				UnitRenderImage.sprite = Unit.GetRenderSprite();
				UnitConditionController.LocalUnit = Unit;
				UnitConditionController.Tick();
				bool flag = Unit.Components != null && Unit.Components.CargoBayComponent != null;
				CargoSliderRoot.gameObject.SetActive(flag);
				if (flag)
				{
					CargoSlider.RefreshFromCargoBayComponent(Unit.CargoBayComponent);
				}
				bool flag2 = Unit.Components != null && Unit.Components.Capacitor != null;
				CapacitorSliderRoot.gameObject.SetActive(flag2);
				if (flag2)
				{
					CapacitorSlider.Capacitor = Unit.Components.Capacitor;
					CapacitorSlider.Refresh();
				}
				bool flag3 = Unit.Destructable != null && Unit.UnitType != UnitType.Asteroid;
				HullSliderRoot.gameObject.SetActive(flag3);
				if (flag3)
				{
					HullSlider.Unit = Unit;
					HullSlider.Refresh();
				}
				RefreshPassengersText();
			}
			UnitPathIconsDisplay.Unit = Unit;
		}

		private void RefreshCommonButtons()
		{
			RefreshBuyShipButtonInteractable();
			RefreshScanUnitButtonInteractable();
			RefreshWaypointButtonEnabled();
			RefreshInfoButtonEnabled();
			RefreshViewButtonInteractable();
			DockingBayButtonController.Unit = Unit;
			CommsButton.gameObject.SetActive(ShouldShowCommsButton(Unit, out var _));
		}

		private void RefreshOwnedUnitButtons()
		{
			RefreshPayBountyButton();
			RefreshSelfDestructButtonInteractable();
			RefreshRenameButtonEnabled();
			RefreshRenamePilotButtonEnabled();
			RefreshShipComponentsButtonInteractable();
			RefreshOrdersButtonEnabled();
			RefreshCargoButtonInteractable();
			RefreshEnterUnitButtonInteractable();
			RefreshUpgradeButtonInteractable();
			RefreshPassengersButtonInteractable();
			ShowCargoButton.SetTextColorFromActiveState(Unit != null && Unit.HasAnyCargo());
			CargoTransferButton.gameObject.SetActive(CanDoCargoTransfer());
			SelfDestructButton.gameObject.SetActive(Unit != null && Unit.CouldBeSelfDestructed());
			DismantleButton.gameObject.SetActive(Unit != null && Unit.CouldBeDismantled());
			if (Unit.CouldBeDismantled())
			{
				DismantleButton.gameObject.SetActive(Unit.CanDismantleNow(EngineASX.Instance.LocalFaction));
			}
		}

		private void RefreshPassengersText()
		{
			bool flag = Unit.Components != null && Unit.Components.PassengerCapacity > 0;
			PassengersLabelRoot.gameObject.SetActive(flag);
			if (flag)
			{
				PassengersLabel.text = GetPassengersText();
			}
		}

		private string GetPassengersText()
		{
			if (Unit.Components != null)
			{
				return $"{Unit.Components.PassengerCount} / {Unit.Components.PassengerCapacity}";
			}
			return "-";
		}

		private void RefreshPayBountyButton()
		{
			PayBountyButton.gameObject.SetActive(Unit.UnitPilotHasBounty());
		}

		private void PayBountyButtonClick()
		{
			List<FactionBountyItem> bounties = EngineASX.Instance.GetBountiesOnPerson(Unit.GetPilot());
			if (bounties == null)
			{
				return;
			}
			int totalCost = 0;
			foreach (FactionBountyItem item in bounties)
			{
				if (item.IsValid)
				{
					totalCost += item.Bounty;
				}
			}
			if (EngineASX.Instance.LocalFaction.Credits >= totalCost)
			{
				if (totalCost <= 5000)
				{
					OnPayOffBountyItems(bounties, totalCost);
					return;
				}
				UIController.Instance.ShowMessageBox($"Are you sure you want to pay {totalCost:N0} credits to remove the bounty?", MessageBoxButtons.OkCancel, (MessageBoxScreen screen, MessageBoxResult result) =>
				{
					if (result == MessageBoxResult.Ok)
					{
						OnPayOffBountyItems(bounties, totalCost);
					}
				}, MessageBoxIcon.Question, "Pay off bounty");
			}
			else
			{
				UIController.Instance.ShowInsufficientCreditsMessageBox();
			}
		}

		private void OnPayOffBountyItems(List<FactionBountyItem> bounties, int totalCost)
		{
			EngineASX.Instance.CreditsAnimation.AllowCreditBeepAudio = true;
			FactionBountyItem[] array = bounties.ToArray();
			foreach (FactionBountyItem item in array)
			{
				BountyHelper.PayOffBountyItem(EngineASX.Instance, item, EngineASX.Instance.LocalFaction);
			}
			EngineASX.Instance.RaiseExchangedCreditsMessage(-totalCost);
			if (IsCurrentScreen)
			{
				NavigateBack();
			}
		}

		private void RefreshScanUnitButtonInteractable()
		{
			ScanUnitButton.gameObject.SetActive(ShouldScanUnitButtonBeInteractable(out var _));
		}

		private bool ShouldScanUnitButtonBeInteractable(out Unit scanner)
		{
			return WorldHelper.CanPlayerScanUnit(Unit, out scanner);
		}

		private void ScanUnitButtonClick()
		{
			if (ShouldScanUnitButtonBeInteractable(out var scanner))
			{
				EngineASX.Instance.OnUnitScanned(Unit, scanner);
				UIController.Instance.ScreenNavigator.ShowShipScanScreen(Unit);
			}
		}

		private void RefreshBuyShipButtonInteractable()
		{
			BuyShipButton.gameObject.SetActive(CanBuyShipFrom(Unit));
		}

		private bool CanBuyShipFrom(Unit unit)
		{
			if (unit != null && unit.Faction != null && unit.IsDockable && DockFacilitiesUI.ShouldShowBuyShipButton(unit))
			{
				return unit.Faction.RequestDock(unit, EngineASX.Instance.LocalFaction);
			}
			return false;
		}

		private void BuyShipButtonClick()
		{
			if (!(EngineASX.Instance.LocalUnit.GetDockUnit() == Unit))
			{
				Unit.Faction.GetOrCreateAttitude(EngineASX.Instance.LocalFaction);
				if (Unit.Faction != EngineASX.Instance.LocalFaction && !Unit.Faction.RequestDock(Unit, EngineASX.Instance.LocalFaction))
				{
					UIController.Instance.ShowMessageBox("The ship dealer has refused to trade with you", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
					return;
				}
			}
			if (Unit.ShipTrader != null)
			{
				UIController.Instance.ScreenNavigator.ShowShipTradeScreen(Unit, null);
			}
		}

		private void RefreshPassengersButtonInteractable()
		{
			PassengersButton.gameObject.SetActive(Unit != null && Unit.IsOwnedByPlayer && Unit.IsStationOrShip() && !Unit.IsUnderConstructionOrDismantling && Unit.Components.PassengerCount > 0);
		}

		private void RefreshUpgradeButtonInteractable()
		{
			UpgradeButton.gameObject.SetActive(ComponentUpgradeHelper.CanUpgradeUnit(Unit));
		}

		private bool CanDoCargoTransfer()
		{
			if (Unit != null)
			{
				return WorldHelper.CanTransferCargoToUnit(EngineASX.Instance.LocalUnit, Unit);
			}
			return false;
		}

		private void CargoTransferButtonClick()
		{
			if (CanDoCargoTransfer())
			{
				UIController.Instance.ScreenNavigator.ShowCargoTransferScreen(EngineASX.Instance.LocalUnit, Unit);
			}
		}

		private void PassengersButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowPassengerInfoScreen(Unit, null);
		}

		private void RefreshShipComponentsButtonInteractable()
		{
			ComponentsButton.gameObject.SetActive(Unit != null && Unit.IsOwnedByPlayer && Unit.Components != null && Unit.IsStationOrShip() && !Unit.IsUnderConstructionOrDismantling && Unit.UnitClass.ShipType != ShipType.Container);
		}

		private void SelfDestructButtonClick()
		{
			UIController.Instance.ShowMessageBox("Are you sure?", MessageBoxButtons.OkCancel, SelfDestructButtonClickConfirm, MessageBoxIcon.Question, "Self-Destruct");
		}

		private void SelfDestructButtonClickConfirm(MessageBoxScreen sender, MessageBoxResult result)
		{
			if (result == MessageBoxResult.Ok)
			{
				Unit.Destructable.KillUnitAsPlayer();
				if (IsCurrentScreen)
				{
					NavigateBack();
				}
			}
		}

		private void DismantleButtonClick()
		{
			UIController.Instance.ShowMessageBox("Are you sure?", MessageBoxButtons.OkCancel, DismantleButtonClickConfirm, MessageBoxIcon.Question, "Dismantle");
		}

		private void DismantleButtonClickConfirm(MessageBoxScreen sender, MessageBoxResult result)
		{
			if (result == MessageBoxResult.Ok)
			{
				Unit.Components.StartDismantle();
				if (IsCurrentScreen)
				{
					NavigateBack();
				}
			}
		}

		private void CommsButtonClick()
		{
			ICommsHandler commsHandler = null;
			if (!(Unit == null) && ShouldShowCommsButton(Unit, out commsHandler))
			{
				CommsHelper.OpenCommsWithHandler(commsHandler, EngineASX.Instance.LocalFaction);
			}
		}

		public bool ShouldShowCommsButton(Unit unit, out ICommsHandler commsHandler)
		{
			return WorldHelper.ShouldShowCommsButton(unit, out commsHandler);
		}

		private void ComponentsButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowUnitComponentsScreen(Unit);
		}

		private void RefreshSelfDestructButtonInteractable()
		{
			SelfDestructButton.gameObject.SetActive(EngineASX.Instance.CanPlayerSelfDestructUnit(Unit));
		}

		private void RefreshFaction()
		{
			bool flag = Unit != null && Unit.CanHaveFaction;
			if (flag)
			{
				FactionContextButton.Faction = Unit.Faction;
				FactionContextButton.Refresh();
			}
			FactionRoot.gameObject.SetActive(flag);
		}

		private void RefreshPilot()
		{
			bool flag = Unit != null && Unit.UnitClass.IsPilottable;
			if (flag)
			{
				Person pilot = Unit.GetPilot();
				string text = ((pilot != null) ? pilot.GetNameAndTitleOrFullRank(shortName: false) : "-");
				if (pilot != null)
				{
					string pilotExtraInfoText = pilot.GetPilotExtraInfoText();
					if (!string.IsNullOrEmpty(pilotExtraInfoText))
					{
						text = text + " (" + pilotExtraInfoText + ")";
					}
				}
				PilotNameLabel.text = text;
			}
			PilotRoot.gameObject.SetActive(flag);
		}

		private void RefreshRenameButtonEnabled()
		{
			RenameButton.gameObject.SetActive(EngineASX.Instance.CanPlayerRenameUnit(Unit));
		}

		private void RefreshRenamePilotButtonEnabled()
		{
			RenamePilotButton.gameObject.SetActive(EngineASX.Instance.CanPlayerRenameUnitNpcPilot(Unit));
		}

		private void RefreshViewButtonInteractable()
		{
			ViewUnitButton.gameObject.SetActive(Unit != null && EngineASX.Instance.World.Permissions.AllowDockUIMap && EngineASX.Instance.LocalFaction.Intel.IsUnitDiscovered(Unit, float.MaxValue) && !UIController.Instance.ScreenNavigator.IsScreenOfTypeInNavigationStack<SectorMapScreen>());
		}

		private void RefreshCargoButtonInteractable()
		{
			ShowCargoButton.gameObject.SetActive(WorldHelper.CanPlayerViewCargo(Unit));
		}

		private void RefreshEnterUnitButtonInteractable()
		{
			EnterUnitButton.gameObject.SetActive(Unit != null && WorldHelper.CanEnterUnitFromCurrentUnit(Unit));
		}

		private void RefreshInfoButtonEnabled()
		{
			InfoButton.gameObject.SetActive(Unit != null && Unit.CanShowUnitInfo());
		}

		private void RefreshWaypointButtonEnabled()
		{
			SetWaypointButton.gameObject.SetActive(WorldHelper.AllowToggleWaypoint(Unit));
		}

		private void RefreshOrdersButtonEnabled()
		{
			OrdersButton.gameObject.SetActive(OrdersHelper.CanPlayerOrderUnit(Unit));
		}

		private void ShowCargoButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowUnitCargoScreen(Unit);
		}

		private void EnterUnitButtonClick()
		{
			WorldHelper.EnterUnit(Unit);
		}

		private void ViewUnitButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowSectorMapScreenForUnitRespectingIntel(Unit, null, Vector3.zero);
		}

		public void ToggleWaypoint()
		{
			if (Unit != null)
			{
				Eng.LocalPlayer.ToggleCustomWaypoint(Unit);
			}
		}

		public void ShowOrders()
		{
			if (Unit != null)
			{
				UIController.Instance.ScreenNavigator.ShowOrdersScreen(Unit);
			}
		}

		public void ShowShipInfo()
		{
			if (Unit != null)
			{
				UIController.Instance.ScreenNavigator.ShowUnitInfoScreen(Unit);
			}
		}

		private void RenameButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowRenameUnitScreen(Unit, null);
		}

		private void RenamePilotButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowRenameScreen(Unit.GetPilot().Name, GameController.Instance.GameSettings.PilotNameMinChars, GameController.Instance.GameSettings.PilotNameMaxChars, OnRenamePilot);
		}

		private void OnRenamePilot(RenameUnitScreen handler, bool rename, string newName)
		{
			if (rename)
			{
				Person pilot = Unit.GetPilot();
				pilot.ClearGeneratedName();
				pilot.CustomName = newName;
				pilot.RefreshName();
			}
		}

		private void UpgradeButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowEquipmentTradeScreen(Unit, null, null);
		}
	}
}
