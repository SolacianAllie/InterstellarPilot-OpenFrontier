using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Controls;
using OpenFrontier.IP.UI.Screens.FleetPicker;
using OpenFrontier.IP.UI.Screens.Fleets;
using OpenFrontier.IP.UI.Screens.NewFleetOrder;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Property
{
	public class PropertyUnitPopupMenu : MonoBehaviour
	{
		public UnitContextButton UnitContextButton;

		public PropertyScreen PropertyScreen;

		public Button EnterButton;

		public Button AssignToFleetButton;

		public Button RemoveFromFleetButton;

		public Button NewOrderButton;

		public Button OrdersButton;

		public Button ClearOrdersButton;

		public Button ToggleWaypointButton;

		public Button InfoButton;

		public Button ViewButton;

		public Button ViewCargoButton;

		private PopupMenu popupMenu;

		public PlayerPropertyList PropertyList => PropertyScreen.PropertyList;

		private void Awake()
		{
			popupMenu = GetComponent<PopupMenu>();
			popupMenu.Opening += PopupMenu_Opening;
			AssignToFleetButton.onClick.AddListener(AssignToFleetButtonClick);
			RemoveFromFleetButton.onClick.AddListener(RemoveFromFleetButtonClick);
			NewOrderButton.onClick.AddListener(NewOrderButtonClick);
			ClearOrdersButton.onClick.AddListener(ClearOrdersButtonClick);
			EnterButton.onClick.AddListener(EnterButtonClick);
			ToggleWaypointButton.onClick.AddListener(ToggleWaypointButtonClick);
			InfoButton.onClick.AddListener(InfoButtonClick);
			ViewButton.onClick.AddListener(ViewButtonClick);
			ViewCargoButton.onClick.AddListener(ViewCargoButtonClick);
			OrdersButton.onClick.AddListener(OrdersButtonClick);
		}

		private bool PopupMenu_Opening(PopupMenu sender)
		{
			RefreshClearOrdersButtonEnabled();
			RefreshNewOrderButtonEnabled();
			RefreshAssignToFleetButtonEnabled();
			RefreshRemoveFromFleetButtonEnabled();
			RefreshEnterUnitButtonEnabled();
			RefreshToggleWaypointButtonEnabled();
			RefreshInfoButtonEnabled();
			RefreshViewButtonEnabled();
			RefreshViewCargoButtonEnabled();
			RefreshOrdersButtonEnabled();
			UnitContextButton.SetUnit(PropertyList.SingleSelectedItem);
			return true;
		}

		private void RefreshOrdersButtonEnabled()
		{
			OrdersButton.gameObject.SetActive(ShouldShowOrdersButton());
		}

		private void RefreshInfoButtonEnabled()
		{
			InfoButton.gameObject.SetActive(ShouldShowInfoButton());
		}

		private void RefreshViewButtonEnabled()
		{
			ViewButton.gameObject.SetActive(ShouldShowViewButton());
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

		private void RefreshAssignToFleetButtonEnabled()
		{
			AssignToFleetButton.gameObject.SetActive(ShouldShowAssignToFleetButton());
		}

		private void RefreshRemoveFromFleetButtonEnabled()
		{
			RemoveFromFleetButton.gameObject.SetActive(ShouldShowRemoveFromFleetButton());
		}

		private bool ShouldShowViewCargoButton()
		{
			return WorldHelper.CanPlayerViewCargo(PropertyList.SingleSelectedItem);
		}

		private bool ShouldShowAssignToFleetButton()
		{
			if (PropertyList.SelectedItems.Count == 0)
			{
				return false;
			}
			if (!PropertyList.SelectedItems.All((Unit e) => OrdersHelper.CanPlayerOrderUnit(e) && e.GetFleet() == null))
			{
				return false;
			}
			return true;
		}

		private void RefreshClearOrdersButtonEnabled()
		{
			ClearOrdersButton.gameObject.SetActive(ShouldShowClearOrdersButton());
		}

		private void RefreshNewOrderButtonEnabled()
		{
			NewOrderButton.gameObject.SetActive(ShouldShowNewOrderButton());
		}

		private bool ShouldShowNewOrderButton()
		{
			if (PropertyList.SelectedItems.Count > 0)
			{
				return AllSelectedItemsCanBeOrdered();
			}
			return false;
		}

		private bool AllSelectedItemsCanBeOrdered()
		{
			foreach (Unit selectedItem in PropertyList.SelectedItems)
			{
				if (!OrdersHelper.CanPlayerOrderUnit(selectedItem))
				{
					return false;
				}
			}
			return true;
		}

		private bool ShouldShowRemoveFromFleetButton()
		{
			if (PropertyList.SelectedItems.Count == 0)
			{
				return false;
			}
			return PropertyList.SelectedItems.Any((Unit e) => e.GetFleet() != null && e.GetFleet().Ships.Count > 1);
		}

		private bool ShouldShowClearOrdersButton()
		{
			foreach (Unit selectedItem in PropertyList.SelectedItems)
			{
				if (selectedItem != null)
				{
					Fleet fleet = selectedItem.GetFleet();
					if (fleet != null && fleet.HasAnyOrders)
					{
						return true;
					}
				}
			}
			return false;
		}

		private void OrdersButtonClick()
		{
			if (ShouldShowOrdersButton())
			{
				UIController.Instance.ScreenNavigator.ShowOrdersScreen(PropertyList.FirstSelectedItem);
			}
		}

		private void ViewCargoButtonClick()
		{
			if (ShouldShowViewCargoButton())
			{
				UIController.Instance.ScreenNavigator.ShowUnitCargoScreen(PropertyList.SingleSelectedItem);
			}
		}

		private void ViewButtonClick()
		{
			if (ShouldShowViewButton())
			{
				UIController.Instance.ScreenNavigator.ShowSectorMapScreenForUnitRespectingIntel(PropertyList.SingleSelectedItem, null, Vector3.zero);
			}
		}

		private void InfoButtonClick()
		{
			if (ShouldShowInfoButton())
			{
				UIController.Instance.ScreenNavigator.ShowUnitInfoScreen(PropertyList.SingleSelectedItem);
			}
		}

		private void ToggleWaypointButtonClick()
		{
			if (ShouldShowToggleWaypointButton())
			{
				EngineASX.Instance.LocalPlayer.ToggleCustomWaypoint(PropertyList.SingleSelectedItem);
			}
		}

		private bool ShouldShowOrdersButton()
		{
			if (PropertyList.SelectedItemCount == 0)
			{
				return false;
			}
			foreach (Unit selectedItem in PropertyList.SelectedItems)
			{
				if (!OrdersHelper.CanPlayerOrderUnit(selectedItem))
				{
					return false;
				}
			}
			NewOrderTarget newOrderTarget = NewOrderTarget.CreateFromUnits(PropertyList.SelectedItems);
			if (!newOrderTarget.IsSingleFleet)
			{
				return newOrderTarget.IsSingleUnitWithoutFleet;
			}
			return true;
		}

		private void EnterButtonClick()
		{
			if (ShouldShowEnterButton())
			{
				WorldHelper.EnterUnit(PropertyList.SingleSelectedItem);
			}
		}

		private bool ShouldShowViewButton()
		{
			return PropertyList.SingleSelectedItem != null;
		}

		private bool ShouldShowInfoButton()
		{
			return PropertyList.SingleSelectedItem != null;
		}

		private bool ShouldShowToggleWaypointButton()
		{
			return WorldHelper.AllowSetWaypointToUnit(PropertyList.SingleSelectedItem);
		}

		private bool ShouldShowEnterButton()
		{
			if (PropertyList.SingleSelectedItem != null)
			{
				return WorldHelper.CanEnterUnitFromCurrentUnit(PropertyList.SingleSelectedItem);
			}
			return false;
		}

		private void ClearOrdersButtonClick()
		{
			if (!ShouldShowClearOrdersButton())
			{
				return;
			}
			List<Fleet> list = new List<Fleet>();
			foreach (Unit selectedItem in PropertyList.SelectedItems)
			{
				Fleet fleet = selectedItem.GetFleet();
				if (fleet != null && !list.Contains(fleet))
				{
					list.Add(fleet);
				}
			}
			foreach (Fleet item in list)
			{
				item.ClearOrders();
			}
			PropertyScreen.Refresh();
		}

		public void NewOrderButtonClick()
		{
			if (ShouldShowNewOrderButton() && PropertyList.SelectedItems.Count > 0)
			{
				NewOrderTarget newOrderTarget = NewOrderTarget.CreateFromUnits(PropertyList.SelectedItems);
				UIController.Instance.ScreenNavigator.ShowNewFleetOrderScreen(newOrderTarget, () =>
				{
					UIController.Instance.ScreenNavigator.NavigateBackTo(PropertyScreen);
				});
			}
		}

		private void RemoveFromFleetButtonClick()
		{
			if (!ShouldShowRemoveFromFleetButton())
			{
				return;
			}
			foreach (Unit item in PropertyList.SelectedItems.Where((Unit e) => e.GetFleet() != null && e.GetFleet().Ships.Count > 1).ToList())
			{
				NpcPilot npcPilot = item.NpcPilot;
				if (npcPilot.Fleet != null)
				{
					if (EngineASX.Instance.LocalUnit.GetFleet() == npcPilot.Fleet && EngineASX.Instance.PlayerUnitAutoPilotEnabled)
					{
						EngineASX.Instance.LocalUnit.Components.PilotPerson = null;
					}
					npcPilot.Fleet = null;
				}
			}
			PropertyScreen.Refresh();
		}

		private void AssignToFleetButtonClick()
		{
			if (ShouldShowAssignToFleetButton())
			{
				if (GetUnitsToAssignToFleet().Count > 8)
				{
					FleetsHelper.TooManyShipsInFleetMessage();
				}
				else
				{
					UIController.Instance.ScreenNavigator.ShowFleetPicker(FleetsHelper.GetOrderedPlayerFleets().ToList(), AssignToFleetPicked, allowNone: false, allowCreateNew: true);
				}
			}
		}

		private List<Unit> GetUnitsToAssignToFleet()
		{
			return PropertyList.SelectedItems.Where((Unit e) => OrdersHelper.CanPlayerOrderUnit(e) && e.GetFleet() == null).ToList();
		}

		private void AssignToFleetPicked(FleetPickerScreen sender, bool result, FleetPickerItem fleetToAssignTo)
		{
			if (result)
			{
				List<Unit> unitsToAssignToFleet = GetUnitsToAssignToFleet();
				PropertyScreen.AssignUnitsToFleet(unitsToAssignToFleet, fleetToAssignTo);
				sender.NavigateBack();
				PropertyScreen.Refresh();
			}
		}
	}
}
