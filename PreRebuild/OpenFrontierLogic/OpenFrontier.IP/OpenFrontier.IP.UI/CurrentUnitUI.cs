using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.RenameUnit;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class CurrentUnitUI : MonoBehaviour
	{
		public DockingBayButtonController DockingBayButtonController;

		public UnitConditionControllerUI ShipConditionController;

		public Text ShipNameText;

		public Text UnitNameText;

		public DynamicCargoTransferButtonController CargoTransferButtonController;

		public Button RenameShipButton;

		public Button ClearWaypointButton;

		public Button EquipmentButton;

		public Button ExitShipButton;

		public Text JobBoardLabel;

		public Button OrdersButton;

		public Button PassengersButton;

		public Button PilotShipButton;

		public Button RepairButton;

		public Button SellShipButton;

		public Button ShipCargoButton;

		public Button ShipComponentsButton;

		public Button ShipInfoButton;

		public Button TradeButton;

		public Button UndockButton;

		public EngineASX Eng => EngineASX.Instance;

		public DockUI DockUI => Eng.DockUI;

		public bool ShouldShowPassengerInfo()
		{
			if (DockUI.IsPlayerUnitOwnedByPlayer)
			{
				return Eng.World.Permissions.AllowDockPassengerModule;
			}
			return false;
		}

		public bool ShouldShowEquipment()
		{
			if (Eng.World.Permissions.AllowDockUIEquipment && DockUI.CanDockedShipTrade)
			{
				return DockUI.PlayerCurrentUnit.GetRootUnit().HasComponentTrader;
			}
			return false;
		}

		public bool ShouldShowShipCargo()
		{
			if (Eng.World.Permissions.AllowDockUIShipCargo && DockUI.PlayerCurrentUnit.IsOwnedByPlayer)
			{
				return DockUI.PlayerCurrentUnit.Components.CargoBayComponent != null;
			}
			return false;
		}

		public bool ShouldShowTrader()
		{
			if (Eng.World.Permissions.AllowDockUITrade)
			{
				return DockUI.CanDockedShipTradeCargo;
			}
			return false;
		}

		public bool ShouldShowSellShipButton()
		{
			if (DockUI.CanDockedShipTrade)
			{
				Unit playerCurrentUnit = DockUI.PlayerCurrentUnit;
				if (playerCurrentUnit != null && playerCurrentUnit.Components.DockUnit != null)
				{
					if (DockUI.PlayerCurrentUnit.Components.DockUnit.ShipTrader != null)
					{
						return Eng.World.Permissions.AllowDockUIShipTrader;
					}
					return false;
				}
			}
			return false;
		}

		public bool ShouldShowRepair()
		{
			if (DockUI.CanDockedShipTrade)
			{
				Unit playerCurrentUnit = DockUI.PlayerCurrentUnit;
				if (playerCurrentUnit != null && playerCurrentUnit.IsOwnedByPlayer && playerCurrentUnit.IsDocked)
				{
					bool hasRepairFacilities = playerCurrentUnit.GetDockUnit().UnitClass.HasRepairFacilities;
					return Eng.World.Permissions.AllowDockUIRepair & hasRepairFacilities;
				}
			}
			return false;
		}

		private void Awake()
		{
			ShipComponentsButton.onClick.AddListener(ShipComponentsButton_Activated);
			UndockButton.onClick.AddListener(UndockShipButton_Activated);
			if (PilotShipButton != null)
			{
				PilotShipButton.onClick.AddListener(PilotShipButton_Activated);
			}
			ShipInfoButton.onClick.AddListener(ShipInfoButton_Activated);
			ExitShipButton.onClick.AddListener(ExitShipButton_Activated);
			ClearWaypointButton.onClick.AddListener(ClearWaypointButton_Activated);
			OrdersButton.onClick.AddListener(OrdersButton_Activated);
			RenameShipButton.onClick.AddListener(RenameShipButtonClick);
			PassengersButton.onClick.AddListener(PassengersButtonClick);
			ShipCargoButton.onClick.AddListener(ShipCargoButtonClick);
			SellShipButton.onClick.AddListener(SellShipButtonClick);
			EquipmentButton.onClick.AddListener(EquipmentButtonClick);
			TradeButton.onClick.AddListener(TradeButtonClick);
			RepairButton.onClick.AddListener(RepairButtonClick);
		}

		public void Refresh()
		{
			if (Eng != null && Eng.LocalPlayer != null && Eng.LocalUnit != null && Eng.LocalUnit.IsValidAndNotDestroyed)
			{
				CargoTransferButtonController.TransferringUnit = DockUI.PlayerCurrentUnit;
				CargoTransferButtonController.Refresh();
				SellShipButton.interactable = ShouldShowSellShipButton();
				TradeButton.interactable = ShouldShowTrader();
				EquipmentButton.interactable = ShouldShowEquipment();
				RepairButton.interactable = ShouldShowRepair();
				ShipCargoButton.interactable = ShouldShowShipCargo();
				PassengersButton.interactable = ShouldShowPassengerInfo();
				ExitShipButton.interactable = CanExitCurrentShip();
				ShipInfoButton.interactable = ShouldShowShipInfo();
				ClearWaypointButton.interactable = Eng.LocalPlayer.HasCustomWaypoint;
				ShipConditionController.LocalUnit = DockUI.PlayerCurrentUnit;
				OrdersButton.interactable = ShouldShowOrdersButton();
				ShipComponentsButton.interactable = ShouldShowUnitComponentsButton();
				RenameShipButton.interactable = ShouldShowRenameUnitButton();
				DockingBayButtonController.Unit = DockUI.PlayerCurrentUnit;
				RefreshShipNameText();
				RefreshShipButtons();
			}
		}

		private bool ShouldShowUnitComponentsButton()
		{
			if (DockUI.PlayerCurrentUnit != null)
			{
				return DockUI.PlayerCurrentUnit.IsOwnedByPlayer;
			}
			return false;
		}

		private bool ShouldShowRenameUnitButton()
		{
			if (DockUI.PlayerCurrentUnit != null)
			{
				return DockUI.PlayerCurrentUnit.IsOwnedByPlayer;
			}
			return false;
		}

		private bool ShouldShowOrdersButton()
		{
			return OrdersHelper.CanPlayerOrderUnit(DockUI.PlayerCurrentUnit);
		}

		private void RefreshShipNameText()
		{
			if (UnitNameText != null && DockUI.PlayerCurrentUnit != null)
			{
				UnitNameText.text = DockUI.PlayerCurrentUnit.GetClassAndSeriesName();
			}
			if (DockUI.PlayerCurrentUnit != null)
			{
				bool flag = DockUI.PlayerCurrentUnit.UnitType == UnitType.Ship && !string.IsNullOrEmpty(DockUI.PlayerCurrentUnit.Components.ShipName);
				ShipNameText.gameObject.SetActive(flag);
				if (flag)
				{
					ShipNameText.text = $"\"{DockUI.PlayerCurrentUnit.Components.ShipName}\"";
				}
			}
		}

		private void RepairButtonClick()
		{
			if (!DockUI.PlayerCurrentUnit.RequiresRepair(1f, 1f, 1f))
			{
				UIController.Instance.ShowMessageBox("No repairs are needed.", MessageBoxButtons.Ok);
			}
			else
			{
				UIController.Instance.ScreenNavigator.ShowRepairScreen();
			}
		}

		private void TradeButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowCargoTradeScreen(DockUI.PlayerCurrentUnit, DockUI.PlayerDockUnit);
		}

		private void EquipmentButtonClick()
		{
			UnitComponentTrader componentTrader = DockUI.PlayerDockUnit.GetComponentTrader();
			if (componentTrader != null)
			{
				UIController.Instance.ScreenNavigator.ShowEquipmentTradeScreen(DockUI.PlayerCurrentUnit, DockUI.PlayerDockUnit, componentTrader);
			}
		}

		private void SellShipButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowSellShipScreen();
		}

		private void ShipCargoButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowUnitCargoScreen(Eng.PlayerUnit);
		}

		private void PassengersButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowPassengerInfoScreen(Eng.PlayerUnit, Eng.PlayerRootUnit);
		}

		private void RenameShipButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowRenameUnitScreen(Eng.PlayerUnit, RenameShipConfirmed);
		}

		private void RenameShipConfirmed(RenameUnitScreen sender, bool rename, string name)
		{
			if (rename)
			{
				Eng.PlayerUnit.Components.ShipName = name;
			}
		}

		private void OrdersButton_Activated()
		{
			UIController.Instance.ScreenNavigator.ShowOrdersScreen(Eng.PlayerUnit);
		}

		private void ShipComponentsButton_Activated()
		{
			UIController.Instance.ScreenNavigator.ShowUnitComponentsScreen(Eng.PlayerUnit);
		}

		private void ClearWaypointButton_Activated()
		{
			Eng.LocalPlayer.ClearCustomWaypoint();
			UIController.Instance.QuickMsg.AddMessage("Waypoint Removed");
			Refresh();
		}

		private void ExitShipButton_Activated()
		{
			Eng.ChangePlayerUnit(Eng.LocalPlayer.Person.CurrentUnit.Components.DockUnit);
			Eng.PlayChangeShipAudio();
		}

		private void ShipInfoButton_Activated()
		{
			UIController.Instance.ScreenNavigator.ShowUnitInfoScreen(DockUI.PlayerCurrentUnit);
		}

		private void UndockShipButton_Activated()
		{
			Eng.IsPaused = false;
			DockUI.UndockCurrentShip();
		}

		private void PilotShipButton_Activated()
		{
			Eng.IsPaused = false;
			DockUI.PilotCurrentShip();
		}

		private bool CanExitCurrentShip()
		{
			return Eng.LocalPlayer.Person.CurrentUnit.IsDocked;
		}

		private bool ShouldShowShipInfo()
		{
			return DockUI.PlayerCurrentUnit.UnitType != UnitType.Station;
		}

		private void RefreshShipButtons()
		{
			if (PilotShipButton != null)
			{
				PilotShipButton.gameObject.SetActive(DockUI.CanPilotCurrentShip());
			}
			UndockButton.interactable = DockUI.CanUndockCurrentShip();
		}
	}
}
