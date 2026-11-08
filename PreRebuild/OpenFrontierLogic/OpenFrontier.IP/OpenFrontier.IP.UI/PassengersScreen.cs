using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.MessageBox;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class PassengersScreen : EngineScreen
	{
		public UnitContextButton PassengerUnitContextButton;

		private Unit dockUnit;

		private Unit currentUnit;

		public Button EjectButton;

		public Text FreeSpaceLabel;

		public PassengersScreenItemList ItemList;

		public Image PassengerCountPrefab;

		public Button PickupButton;

		public GameObject SelectedItemRoot;

		private float lastRefreshTime;

		public Unit CurrentUnit
		{
			get
			{
				return currentUnit;
			}
			set
			{
				if (currentUnit != value)
				{
					_ = currentUnit;
					currentUnit = value;
				}
			}
		}

		public Unit DockUnit
		{
			get
			{
				return dockUnit;
			}
			set
			{
				if (dockUnit != value)
				{
					_ = dockUnit;
					dockUnit = value;
				}
			}
		}

		protected override void awake()
		{
			base.awake();
			PickupButton.onClick.AddListener(PickupButton_Activated);
			ItemList.SelectedItemChanged += ItemList_SelectedItemChanged;
			EjectButton.onClick.AddListener(EjectButton_Activated);
			RefreshSelectedItemActive();
			RefreshPickupButton();
			RefreshEjectButton();
		}

		protected override void onDestroy()
		{
			base.onDestroy();
			CurrentUnit = null;
			DockUnit = null;
		}

		protected override void refresh()
		{
			base.refresh();
			if (currentUnit != null && currentUnit.IsValidAndNotDestroyed)
			{
				PassengerManager.RemoveInvalidOrExpiredPassengerGroupsAtUnit(currentUnit);
				if (currentUnit != currentUnit.GetRootUnit())
				{
					PassengerManager.RemoveInvalidOrExpiredPassengerGroupsAtUnit(currentUnit.GetRootUnit());
				}
				ItemList.Refresh();
				DiscoverPassengerGroupDestinations();
				RefreshFreespaceLabel();
				RefreshPickupButton();
				RefreshEjectButton();
			}
			else
			{
				ItemList.ClearActiveItems();
			}
			lastRefreshTime = Time.time;
		}

		protected override void update()
		{
			base.update();
			DiscoverPassengerGroupDestinations();
			if (Time.time > lastRefreshTime + 2f)
			{
				Refresh();
			}
		}

		private void DiscoverPassengerGroupDestinations()
		{
			foreach (PassengerGroup activeItem in ItemList.ActiveItems)
			{
				if (activeItem != null && activeItem.IsValid)
				{
					EngineASX.Instance.LocalFaction.Intel.DiscoverUnit(activeItem.Destination);
				}
			}
		}

		private void RefreshFreespaceLabel()
		{
			FreeSpaceLabel.text = $"{currentUnit.Components.PassengerCount} / {currentUnit.Components.PassengerCapacity}";
		}

		private void EjectButton_Activated()
		{
			if (dockUnit != null && ItemList.FirstSelectedItem.Source == dockUnit)
			{
				ItemList.FirstSelectedItem.CurrentUnit = ItemList.FirstSelectedItem.Source;
			}
			else
			{
				int passengerCount = ItemList.FirstSelectedItem.PassengerCount;
				ItemList.FirstSelectedItem.SafeDestroy();
				if (dockUnit == null)
				{
					currentUnit.PlayEjectCargoAudioSource();
					if (Random.value < 0.125f)
					{
						for (int i = 0; i < passengerCount; i++)
						{
							AudioSource audioSource = UnityObjectHelper.InstantiateAndGetComponent(GameController.Instance.EjectPassengerAudioSource);
							audioSource.transform.position = currentUnit.transform.position;
							audioSource.PlayDelayed(Random.Range(0.25f, 1.5f));
						}
					}
				}
				if (currentUnit.GetActiveFleetOrder() is ActiveTransportPassengersOrder activeTransportPassengersOrder)
				{
					activeTransportPassengersOrder.ValidateAndClearIfInvalid();
				}
			}
			Refresh();
		}

		public void SetWaypointToItem(PassengerGroup item)
		{
			Eng.LocalPlayer.SetCustomWaypointToUnit(item.Destination);
		}

		private void ItemList_SelectedItemChanged(ScrollList<PassengerGroup> sender, PassengerGroup oldItem, PassengerGroup newItem)
		{
			RefreshSelectedItemActive();
			RefreshPickupButton();
			RefreshEjectButton();
		}

		private void RefreshSelectedItemActive()
		{
			SelectedItemRoot.gameObject.SetActive(ItemList.FirstSelectedItem != null);
			if (ItemList.FirstSelectedItem != null && ItemList.FirstSelectedItem.IsValid && ItemList.FirstSelectedItem.Destination != null)
			{
				PassengerUnitContextButton.SetUnit(ItemList.FirstSelectedItem.Destination);
			}
			else
			{
				PassengerUnitContextButton.SetUnit(null);
			}
		}

		private void PickupButton_Activated()
		{
			if (currentUnit != null)
			{
				PassengerGroup firstSelectedItem = ItemList.FirstSelectedItem;
				if (TryPickupPassengers())
				{
					SetWaypointToItem(firstSelectedItem);
					DockUI.NavigateToInitialScreen();
				}
			}
			else
			{
				Debug.LogError("Player has no owned ship", this);
			}
		}

		private bool TryPickupPassengers()
		{
			if (ItemList.FirstSelectedItem == null || !ItemList.FirstSelectedItem.IsValid)
			{
				return false;
			}
			if (currentUnit.Components.PassengerModule != null && currentUnit.Components.PassengerModule.PassengerModuleClass.PassengerCapacity > 0)
			{
				if (currentUnit.Components.FreePassengerSpace >= ItemList.FirstSelectedItem.PassengerCount)
				{
					PickupPassengers();
					return true;
				}
				string message = "Passengers cannot be picked up. Insufficient Space";
				UIController.Instance.ShowMessageBox(message, MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
			}
			else
			{
				string message2 = "Passengers cannot be picked up. Passenger Module not installed on current ship";
				UIController.Instance.ShowMessageBox(message2, MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
			}
			return false;
		}

		private void PickupPassengers()
		{
			string msg = $"{ItemList.FirstSelectedItem.PassengerCount} passengers added";
			UIController.Instance.QuickMsg.AddMessage(msg);
			EngineASX.Instance.PassengerManager.PickupPassengers(ItemList.FirstSelectedItem, currentUnit);
		}

		private void RefreshPickupButton()
		{
			PickupButton.gameObject.SetActive(ItemList.FirstSelectedItem != null && DockUI.CanUnitTrade(currentUnit) && ItemList.FirstSelectedItem.IsWaitingForPickup);
		}

		private void RefreshEjectButton()
		{
			EjectButton.gameObject.SetActive(ItemList.FirstSelectedItem != null && ItemList.FirstSelectedItem.CurrentUnit == currentUnit);
		}
	}
}
