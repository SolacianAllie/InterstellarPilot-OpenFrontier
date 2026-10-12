using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Comms;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.UI.Screens.NewFleetOrder;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Controls
{
	public class UnitPopupMenu : MonoBehaviour
	{
		public Button MoreButton;

		public Button EnterButton;

		public Button OrdersButton;

		public Button ToggleWaypointButton;

		public Button InfoButton;

		public Button ViewCargoButton;

		public Button CommsButton;

		public Button ScanButton;

		public Button OrderDockAtButton;

		public Button OrderMoveToButton;

		private PopupMenu popupMenu;

		public Unit Unit { get; set; }

		private void Awake()
		{
			popupMenu = GetComponent<PopupMenu>();
			popupMenu.Opening += PopupMenu_Opening;
			EnterButton.onClick.AddListener(EnterButtonClick);
			ToggleWaypointButton.onClick.AddListener(ToggleWaypointButtonClick);
			InfoButton.onClick.AddListener(InfoButtonClick);
			ViewCargoButton.onClick.AddListener(ViewCargoButtonClick);
			OrdersButton.onClick.AddListener(OrdersButtonClick);
			MoreButton.onClick.AddListener(MoreButtonClick);
			ScanButton.onClick.AddListener(ScanButtonClick);
			CommsButton.onClick.AddListener(CommsButtonClick);
			OrderDockAtButton.onClick.AddListener(OrderDockAtButtonClick);
			OrderMoveToButton.onClick.AddListener(OrderFlyToButtonClick);
		}

		private bool PopupMenu_Opening(PopupMenu sender)
		{
			RefreshEnterUnitButtonEnabled();
			RefreshToggleWaypointButtonEnabled();
			RefreshInfoButtonEnabled();
			RefreshViewCargoButtonEnabled();
			RefreshOrdersButtonEnabled();
			RefreshMoreButtonEnabled();
			RefreshCommsButtonEnabled();
			RefreshScanButtonEnabled();
			RefreshOrderDockAtButtonEnabled();
			RefreshOrderMoveToButtonEnabled();
			return true;
		}

		private void RefreshOrderMoveToButtonEnabled()
		{
			OrderMoveToButton.gameObject.SetActive(ShouldShowOrderMoveToButton());
		}

		private void RefreshMoreButtonEnabled()
		{
			MoreButton.gameObject.SetActive(ShouldShowMoreButton());
		}

		private void RefreshOrdersButtonEnabled()
		{
			OrdersButton.gameObject.SetActive(ShouldShowOrdersButton());
		}

		private void RefreshInfoButtonEnabled()
		{
			InfoButton.gameObject.SetActive(ShouldShowInfoButton());
		}

		private void RefreshViewCargoButtonEnabled()
		{
			ViewCargoButton.gameObject.SetActive(ShouldShowViewCargoButton());
		}

		private void RefreshToggleWaypointButtonEnabled()
		{
			ToggleWaypointButton.gameObject.SetActive(ShouldShowToggleWaypointButton());
		}

		private void RefreshEnterUnitButtonEnabled()
		{
			EnterButton.gameObject.SetActive(ShouldShowEnterButton());
		}

		private void RefreshCommsButtonEnabled()
		{
			CommsButton.gameObject.SetActive(ShouldShowCommsButton(out var _));
		}

		private void RefreshScanButtonEnabled()
		{
			ScanButton.gameObject.SetActive(ShouldShowScanButton(out var _));
		}

		private void RefreshOrderDockAtButtonEnabled()
		{
			OrderDockAtButton.gameObject.SetActive(ShouldShowOrderDockAtButton());
		}

		private bool ShouldShowViewCargoButton()
		{
			return WorldHelper.CanPlayerViewCargo(Unit);
		}

		private void OrdersButtonClick()
		{
			if (ShouldShowOrdersButton())
			{
				UIController.Instance.ScreenNavigator.ShowOrdersScreen(Unit);
			}
		}

		private void ViewCargoButtonClick()
		{
			if (ShouldShowViewCargoButton())
			{
				UIController.Instance.ScreenNavigator.ShowUnitCargoScreen(Unit);
			}
		}

		private void InfoButtonClick()
		{
			if (ShouldShowInfoButton())
			{
				UIController.Instance.ScreenNavigator.ShowUnitInfoScreen(Unit);
			}
		}

		private void ToggleWaypointButtonClick()
		{
			if (ShouldShowToggleWaypointButton())
			{
				EngineASX.Instance.LocalPlayer.ToggleCustomWaypoint(Unit);
			}
		}

		private bool ShouldShowOrdersButton()
		{
			return OrdersHelper.CanPlayerOrderUnit(Unit);
		}

		private void EnterButtonClick()
		{
			if (ShouldShowEnterButton())
			{
				WorldHelper.EnterUnit(Unit);
			}
		}

		private bool ShouldShowInfoButton()
		{
			return WorldHelper.CanShowUnitInfo(Unit);
		}

		private bool ShouldShowToggleWaypointButton()
		{
			return WorldHelper.AllowToggleWaypoint(Unit);
		}

		private bool ShouldShowEnterButton()
		{
			if (Unit != null)
			{
				return WorldHelper.CanEnterUnitFromCurrentUnit(Unit);
			}
			return false;
		}

		private bool ShouldShowMoreButton()
		{
			return WorldHelper.CanShowUnitContext(Unit);
		}

		public bool ShouldShowCommsButton(out ICommsHandler commsHandler)
		{
			return WorldHelper.ShouldShowCommsButton(Unit, out commsHandler);
		}

		public bool ShouldShowScanButton(out Unit scanner)
		{
			return WorldHelper.CanPlayerScanUnit(Unit, out scanner);
		}

		private bool ShouldShowOrderDockAtButton()
		{
			return OrdersHelper.CanOrderDockAtTarget(EngineASX.Instance.PlayerUnit, Unit);
		}

		private bool ShouldShowOrderMoveToButton()
		{
			if (OrdersHelper.CanPlayerOrderUnit(EngineASX.Instance.PlayerUnit) && Unit != null && Unit.IsValidAndNotDestroyed)
			{
				return Unit != EngineASX.Instance.PlayerUnit;
			}
			return false;
		}

		private void MoreButtonClick()
		{
			if (ShouldShowMoreButton())
			{
				UIController.Instance.ScreenNavigator.ShowUnitContextMenuScreen(Unit);
			}
		}

		private void ScanButtonClick()
		{
			if (ShouldShowScanButton(out var scanner))
			{
				EngineASX.Instance.OnUnitScanned(Unit, scanner);
				UIController.Instance.ScreenNavigator.ShowShipScanScreen(Unit);
			}
		}

		private void CommsButtonClick()
		{
			if (ShouldShowCommsButton(out var commsHandler))
			{
				CommsHelper.OpenCommsWithHandler(commsHandler, EngineASX.Instance.LocalFaction);
			}
		}

		private void OrderDockAtButtonClick()
		{
			if (ShouldShowOrderDockAtButton())
			{
				if (Unit.Faction != EngineASX.Instance.LocalFaction && !Unit.Faction.RequestDock(Unit, EngineASX.Instance.LocalFaction))
				{
					UIController.Instance.ShowMessageBox("Docking permission denied", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
					return;
				}
				OrdersHelper.OrderDockAtTarget(NewOrderTarget.CreateFromUnits(new Unit[1] { EngineASX.Instance.PlayerUnit }), Unit, stack: false);
			}
		}

		private void OrderFlyToButtonClick()
		{
			if (ShouldShowOrderMoveToButton())
			{
				OrdersHelper.OrderMoveToSectorTarget(NewOrderTarget.CreateFromUnits(new Unit[1] { EngineASX.Instance.PlayerUnit }), SectorTarget.FromUnit(Unit), stack: false);
			}
		}
	}
}
