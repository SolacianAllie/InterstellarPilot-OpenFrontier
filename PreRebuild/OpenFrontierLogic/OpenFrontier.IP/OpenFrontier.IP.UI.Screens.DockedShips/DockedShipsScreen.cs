using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Comms;
using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.DockedShips
{
	public class DockedShipsScreen : EngineScreen
	{
		public UnitContextButton UnitContextButton;

		public Text TitleText;

		public bool NavigateBackWhenUnitInvaild;

		public Button CargoTransferButton;

		public Button EnterShipButton;

		private UnitHangar hangar;

		public DockedShipItemList ItemList;

		public Button OrdersButton;

		public Button UndockButton;

		public UnitHangar Hangar
		{
			get
			{
				return hangar;
			}
			set
			{
				if (hangar != value)
				{
					UnitHangar unitHangar = hangar;
					hangar = value;
					ItemList.Hangar = hangar;
					if (unitHangar != null)
					{
						unitHangar.DockedUnitsChanged -= hangar_DockedUnitsChanged;
					}
					if ((bool)hangar)
					{
						hangar.DockedUnitsChanged += hangar_DockedUnitsChanged;
					}
				}
			}
		}

		public Unit SelectedUnit
		{
			get
			{
				if (ItemList.FirstSelectedItem != null)
				{
					return ItemList.FirstSelectedItem.Unit;
				}
				return null;
			}
		}

		protected override void awake()
		{
			base.awake();
			UndockButton.onClick.AddListener(UndockButton_Activated);
			EnterShipButton.onClick.AddListener(EnterShipButton_Activated);
			OrdersButton.onClick.AddListener(OrdersButton_Activated);
			CargoTransferButton.onClick.AddListener(CargoTransferButtonClick);
			ItemList.SelectedItemChanged += ItemList_SelectedItemChanged;
			OrdersButton.gameObject.SetActive(value: true);
		}

		public bool ShouldShowCommsButton(UnitComponentHolder unit, out ICommsHandler commsHandler)
		{
			commsHandler = null;
			if (unit != null)
			{
				return CommsHelper.TryGetCommsHandlerFromUnit(unit.Unit, out commsHandler);
			}
			return false;
		}

		protected override void refresh()
		{
			base.refresh();
			ItemList.LocalFaction = Eng.LocalFaction;
			ItemList.Refresh();
			RefreshSelectedItemButtons();
			if (hangar != null)
			{
				TitleText.text = "Docked Ships - " + hangar.UnitComponents.Unit.GetFriendlyName();
			}
		}

		protected override void onDestroy()
		{
			base.onDestroy();
			Hangar = null;
		}

		public bool IsHangarValid()
		{
			if (hangar != null && hangar.UnitComponents != null)
			{
				return hangar.UnitComponents.Unit.IsValidAndNotDestroyed;
			}
			return false;
		}

		protected override void update()
		{
			base.update();
			if (!NavigateBackWhenInvalid())
			{
				RefreshSelectedItemButtons();
			}
		}

		private bool NavigateBackWhenInvalid()
		{
			if (NavigateBackWhenUnitInvaild && GetBackTarget() != null && (hangar == null || !IsHangarValid()))
			{
				if (Eng.PlayerUnit != null && Eng.PlayerUnit.IsValidAndNotDestroyed)
				{
					UIController.Instance.QuickMsg.AddMessage("Lost contact with target");
				}
				NavigateBack();
				return true;
			}
			return false;
		}

		private void RefreshCargoTransferButton()
		{
			CargoTransferButton.interactable = CanDoCargoTransfer();
		}

		private bool CanDoCargoTransfer()
		{
			if (ItemList.FirstSelectedItem != null && ItemList.FirstSelectedItem.Unit != null)
			{
				return WorldHelper.CanTransferCargoToUnit(DockUI.PlayerCurrentUnit, ItemList.FirstSelectedItem.Unit);
			}
			return false;
		}

		private void CargoTransferButtonClick()
		{
			if (CanDoCargoTransfer())
			{
				UIController.Instance.ScreenNavigator.ShowCargoTransferScreen(DockUI.PlayerCurrentUnit, ItemList.FirstSelectedItem.Unit);
			}
		}

		private void OrdersButton_Activated()
		{
			UIController.Instance.ScreenNavigator.ShowOrdersScreen(ItemList.FirstSelectedItem.Unit);
		}

		private void ItemList_SelectedItemChanged(ScrollList<UnitComponentHolder> sender, UnitComponentHolder oldItem, UnitComponentHolder newItem)
		{
			RefreshSelectedItemButtons();
		}

		private void RefreshEnterShipButton()
		{
			EnterShipButton.interactable = ItemList.FirstSelectedItem != null && WorldHelper.CanEnterUnitFromCurrentUnit(ItemList.FirstSelectedItem.Unit);
		}

		private void RefreshUndockButton()
		{
			UndockButton.interactable = ItemList.FirstSelectedItem != null && DockUI.CanUndockShip(ItemList.FirstSelectedItem);
		}

		private void RefreshOrdersButton()
		{
			OrdersButton.interactable = ItemList.FirstSelectedItem != null && HudScreen.CanShowOrdersForUnit(ItemList.FirstSelectedItem.Unit);
		}

		private void EnterShipButton_Activated()
		{
			Eng.LocalPlayer.Person.CurrentUnit = ItemList.FirstSelectedItem.Unit;
			Eng.PlayChangeShipAudio();
			Eng.SetUIFromPlayerStatus();
		}

		private void UndockButton_Activated()
		{
			Eng.IsPaused = false;
			DockUI.UndockShip(ItemList.FirstSelectedItem.Unit.Components);
		}

		private void RefreshSelectedItemButtons()
		{
			RefreshOrdersButton();
			RefreshUndockButton();
			RefreshEnterShipButton();
			RefreshCargoTransferButton();
			UnitContextButton.Unit = SelectedUnit;
			UnitContextButton.Refresh();
		}

		private void hangar_DockedUnitsChanged(UnitHangar sender, UnitHangarBay bay, UnitComponentHolder oldUnit, UnitComponentHolder newUnit)
		{
			Refresh();
		}
	}
}
