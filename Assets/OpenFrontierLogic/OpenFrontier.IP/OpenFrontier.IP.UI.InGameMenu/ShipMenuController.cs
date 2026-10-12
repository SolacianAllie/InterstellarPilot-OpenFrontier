using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.RenameUnit;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.InGameMenu
{
	public class ShipMenuController : MonoBehaviour
	{
		public UnitContextButton UnitContextMenuButton;

		public DockingBayButtonController DockingBayButtonController;

		public Button CargoButton;

		public Button ComponentsButton;

		public Button PassengersButton;

		public Button ShipInfoButton;

		public Button UnitOrdersButton;

		public Button RenameShipButton;

		private void Awake()
		{
			PassengersButton.onClick.AddListener(PassengersButtonClick);
			ShipInfoButton.onClick.AddListener(ShipInfoButtonClick);
			ComponentsButton.onClick.AddListener(ComponentsButtonClick);
			UnitOrdersButton.onClick.AddListener(UnitOrdersButtonClick);
			CargoButton.onClick.AddListener(CargoButtonClick);
			RenameShipButton.onClick.AddListener(RenameShipButtonClick);
		}

		public void Refresh()
		{
			UnitContextMenuButton.SetUnit(EngineASX.Instance.Hud.PlayerUnit);
			RefreshVolatile();
		}

		private void Update()
		{
			RefreshVolatile();
		}

		private void RefreshVolatile()
		{
			Unit playerUnit = EngineASX.Instance.PlayerUnit;
			if (playerUnit != UnitContextMenuButton.Unit)
			{
				UnitContextMenuButton.Unit = playerUnit;
				UnitContextMenuButton.Refresh();
			}
			DockingBayButtonController.Unit = playerUnit;
			if (playerUnit != null)
			{
				PassengersButton.interactable = playerUnit.Components != null && playerUnit.Components.PassengerModule != null && playerUnit.Components.PassengerCount > 0;
				CargoButton.interactable = playerUnit.CargoBayComponent != null && !playerUnit.CargoBayComponent.IsEmpty;
				UnitOrdersButton.interactable = EngineASX.Instance.World.Permissions.AllowOrders;
			}
		}

		private void RenameShipButtonClick()
		{
			if (EngineASX.Instance.PlayerUnit.Components != null)
			{
				UIController.Instance.ScreenNavigator.ShowRenameUnitScreen(EngineASX.Instance.PlayerUnit, RenameShipConfirmed);
			}
		}

		private void RenameShipConfirmed(RenameUnitScreen sender, bool rename, string name)
		{
			if (rename)
			{
				EngineASX.Instance.PlayerUnit.Components.ShipName = name;
			}
		}

		private void CargoButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowUnitCargoScreen(EngineASX.Instance.PlayerUnit);
		}

		private void ComponentsButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowUnitComponentsScreen(EngineASX.Instance.PlayerUnit);
		}

		private void PassengersButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowPassengerInfoScreen(EngineASX.Instance.PlayerUnit, null);
		}

		private void ShipInfoButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowUnitInfoScreen(EngineASX.Instance.PlayerUnit);
		}

		private void UnitOrdersButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowOrdersScreen(EngineASX.Instance.PlayerUnit);
		}
	}
}
