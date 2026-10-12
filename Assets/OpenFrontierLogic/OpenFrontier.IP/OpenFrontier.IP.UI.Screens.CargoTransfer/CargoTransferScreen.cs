using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.CargoTransfer
{
	public class CargoTransferScreen : EngineScreen
	{
		public CargoTransferUnitController LeftTransferController;

		public CargoTransferUnitController RightTransferController;

		public CanvasGroup TransferRightButtonsRoot;

		public CanvasGroup TransferLeftButtonsRoot;

		public Button TransferButton;

		public Button ClearTransferButton;

		private CargoTransferData currentItem;

		private float leftSideLastCargoBayChangedTime;

		private float rightSideLastCargoBayChangedTime;

		public bool NavigateBackWhenUnitInvaild;

		public CargoTransferData CurrentItem
		{
			get
			{
				return currentItem;
			}
			set
			{
				if (currentItem != value)
				{
					currentItem = value;
				}
			}
		}

		public void AddToTransfer(int count)
		{
			if (currentItem != null && count != 0)
			{
				currentItem.TransferUnits += count;
				ClampTransferQuantity();
				RefreshTransferDisplay();
			}
		}

		private void ClampTransferQuantity()
		{
			int countOf = LeftTransferController.CargoBayComponent.GetCountOf(currentItem.Item.CargoClass);
			int countOf2 = RightTransferController.CargoBayComponent.GetCountOf(currentItem.Item.CargoClass);
			currentItem.TransferUnits = Mathf.Clamp(currentItem.TransferUnits, -countOf2, countOf);
		}

		protected override void awake()
		{
			base.awake();
			LeftTransferController.ItemList.SelectedItemChanged += LeftController_SelectedItemChanged;
			RightTransferController.ItemList.SelectedItemChanged += RightController_SelectedItemChanged;
			TransferButton.onClick.AddListener(TransferButtonOnClick);
			ClearTransferButton.onClick.AddListener(ClearTransferButtonClick);
		}

		protected override void refresh()
		{
			base.refresh();
			LeftTransferController.Refresh();
			RightTransferController.Refresh();
			if (LeftTransferController.CargoBayComponent != null)
			{
				leftSideLastCargoBayChangedTime = LeftTransferController.CargoBayComponent.LastChangedTime;
			}
			if (RightTransferController.CargoBayComponent != null)
			{
				rightSideLastCargoBayChangedTime = RightTransferController.CargoBayComponent.LastChangedTime;
			}
		}

		protected override void update()
		{
			base.update();
			if (!IsCurrentScreen)
			{
				return;
			}
			if (TooFarFromTarget() && GetBackTarget() != null)
			{
				UIController.Instance.QuickMsg.AddMessage("Too far from target");
				NavigateBack();
				return;
			}
			if (NavigateBackWhenUnitInvaild && UnitsInvalid() && GetBackTarget() != null)
			{
				if (Eng.PlayerUnit != null && Eng.PlayerUnit.IsValidAndNotDestroyed)
				{
					UIController.Instance.QuickMsg.AddMessage("Cargo transfer target lost");
				}
				NavigateBack();
				return;
			}
			ClearTransferButton.interactable = currentItem != null && currentItem.TransferUnits != 0;
			TransferButton.interactable = CanDoTransfer();
			TransferLeftButtonsRoot.interactable = currentItem != null;
			TransferRightButtonsRoot.interactable = currentItem != null;
			if (LeftTransferController.CargoBayComponent != null && RightTransferController.CargoBayComponent != null && (LeftTransferController.CargoBayComponent.LastChangedTime > leftSideLastCargoBayChangedTime || RightTransferController.CargoBayComponent.LastChangedTime > rightSideLastCargoBayChangedTime))
			{
				Refresh();
			}
		}

		private bool UnitsInvalid()
		{
			if (!(LeftTransferController == null) && LeftTransferController.Unit.IsValidAndNotDestroyed && !(RightTransferController == null))
			{
				return !RightTransferController.Unit.IsValidAndNotDestroyed;
			}
			return true;
		}

		private bool TooFarFromTarget()
		{
			if (LeftTransferController.Unit != null && RightTransferController.Unit != null)
			{
				if (Eng.GameSettings.CanTransferCargoFromAnyDistance)
				{
					return false;
				}
				return Vector3.Distance(LeftTransferController.Unit.transform.position, RightTransferController.Unit.transform.position) > Eng.GameSettings.MaxTransferCargoDistance;
			}
			return false;
		}

		private bool CanDoTransfer()
		{
			if (currentItem != null && currentItem.Item != null && LeftTransferController.CanDoTransfer(currentItem))
			{
				return RightTransferController.CanDoTransfer(currentItem);
			}
			return false;
		}

		private void ClearTransferButtonClick()
		{
			CurrentItem = null;
			Refresh();
		}

		private void TransferButtonOnClick()
		{
			if (CanDoTransfer())
			{
				DoTransfer();
			}
		}

		private void DoTransfer()
		{
			LeftTransferController.DoTransfer(currentItem);
			RightTransferController.DoTransfer(currentItem);
			Refresh();
		}

		private void RightController_SelectedItemChanged(ScrollList<CargoBayItem> sender, CargoBayItem oldItem, CargoBayItem newItem)
		{
			if (sender.FirstSelectedItem != null)
			{
				CargoBayItem firstSelectedItem = sender.FirstSelectedItem;
				CurrentItem = new CargoTransferData
				{
					Item = firstSelectedItem,
					TransferUnits = 0
				};
				LeftTransferController.ItemList.FirstSelectedItem = null;
			}
			else if (LeftTransferController.ItemList.FirstSelectedItem == null && RightTransferController.ItemList.FirstSelectedItem == null)
			{
				currentItem = null;
			}
			RefreshTransferDisplay();
		}

		private void LeftController_SelectedItemChanged(ScrollList<CargoBayItem> sender, CargoBayItem oldItem, CargoBayItem newItem)
		{
			if (sender.FirstSelectedItem != null)
			{
				CargoBayItem firstSelectedItem = sender.FirstSelectedItem;
				CurrentItem = new CargoTransferData
				{
					Item = firstSelectedItem,
					TransferUnits = 0
				};
				RightTransferController.ItemList.FirstSelectedItem = null;
			}
			else if (LeftTransferController.ItemList.FirstSelectedItem == null && RightTransferController.ItemList.FirstSelectedItem == null)
			{
				currentItem = null;
			}
			RefreshTransferDisplay();
		}

		private void RefreshTransferDisplay()
		{
			LeftTransferController.RefreshTransferDisplay(currentItem);
			RightTransferController.RefreshTransferDisplay(currentItem);
		}
	}
}
