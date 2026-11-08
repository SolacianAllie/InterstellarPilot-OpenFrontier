using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.TargetScanning;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.InGameMenu
{
	public class TargetInGameMenu : MonoBehaviour
	{
		public UnitContextButton UnitContextMenuButton;

		public ToggleWaypointButtonController ToggleWaypointButtonController;

		public Button CargoTransferButton;

		public Button ScanTargetButton;

		public HudScreen Hud;

		public Button ClearButton;

		public Button TargetInfoButton;

		private void Awake()
		{
			ScanTargetButton.onClick.AddListener(ScanTargetButtonOnClick);
			CargoTransferButton.onClick.AddListener(CargoTransferButtonClick);
			ClearButton.onClick.AddListener(ClearButtonClick);
		}

		private void Update()
		{
			if (EngineASX.LoadedAndReady)
			{
				if (EngineASX.Instance.Hud.CurrentTarget == null)
				{
					EngineASX.Instance.Hud.InGameMenuController.Close();
				}
				else
				{
					RefreshVolatile();
				}
			}
		}

		public void Refresh()
		{
			TargetInfoButton.gameObject.SetActive(GameController.Instance.CanvasHeight01 > 0.1f);
			UnitContextMenuButton.SetUnit(Hud.CurrentTarget);
			RefreshVolatile();
		}

		private void RefreshVolatile()
		{
			if (Hud.CurrentTarget != UnitContextMenuButton.Unit)
			{
				UnitContextMenuButton.Unit = Hud.CurrentTarget;
				UnitContextMenuButton.Refresh();
			}
			ToggleWaypointButtonController.CurrentTarget = Hud.CurrentTarget;
			ScanTargetButton.interactable = TargetScanUtils.CanPlayerScan(Hud.CurrentTarget);
			CargoTransferButton.interactable = WorldHelper.CanTransferCargoToUnit(Hud.PlayerUnit, Hud.CurrentTarget);
		}

		private void CargoTransferButtonClick()
		{
			if (WorldHelper.CanTransferCargoToUnit(Hud.PlayerUnit, Hud.CurrentTarget))
			{
				UIController.Instance.ScreenNavigator.ShowCargoTransferScreen(Hud.PlayerUnit, Hud.CurrentTarget);
			}
		}

		private void ScanTargetButtonOnClick()
		{
			if (TargetScanUtils.CanPlayerScan(Hud.CurrentTarget))
			{
				Hud.Eng.OnUnitScanned(Hud.CurrentTarget, Hud.PlayerUnit);
				UIController.Instance.ScreenNavigator.ShowShipScanScreen(Hud.CurrentTarget);
			}
		}

		private void ClearButtonClick()
		{
			EngineASX.Instance.Hud.CurrentTarget = null;
			EngineASX.Instance.Hud.InGameMenuController.Close();
		}
	}
}
