using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.UI.Controls;
using OpenFrontier.IP.UI.Screens.FleetOrders;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.UI.Screens.Orders;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens
{
	public class OrdersScreen : EngineScreen
	{
		public Button FleetCargoButton;

		public UnitContextButton UnitContextButton;

		public OrdersScreenShipList OrdersScreenShipList;

		public Button ClearOrdersButton;

		public Button NewOrderButton;

		public bool NavigateBackWhenFleetInvalid;

		[FormerlySerializedAs("UnitNameLabel")]
		public Text FleetNameLabel;

		public Text CurrentLocationLabel;

		private static float ordersButtonAndTextUpdateFrequency = 1f;

		private float nextUpdateOrdersButtonAndText;

		public Transform FleetOrdersTransform;

		public FleetActiveOrderListItem FleetActiveOrderListItem;

		public FleetOrderListItem FleetOrderListItemPrefab;

		private List<FleetOrderListItem> fleetOrderListItems = new List<FleetOrderListItem>();

		public TextMeshProUGUI NoOrdersText;

		private Fleet orderedFleet;

		public Button FleetSettingsButton;

		public Fleet OrderedFleet
		{
			get
			{
				return orderedFleet;
			}
			set
			{
				if (orderedFleet != value)
				{
					orderedFleet = value;
					Refresh();
				}
			}
		}

		public bool OrderedFleetIsValid
		{
			get
			{
				if (orderedFleet != null && orderedFleet.IsValid)
				{
					return orderedFleet.LeaderUnit != null;
				}
				return false;
			}
		}

		protected override void awake()
		{
			base.awake();
			for (int i = 0; i < EngineASX.Instance.GameSettings.MaxPlayerFleetStackedOrders; i++)
			{
				FleetOrderListItem fleetOrderListItem = UnityObjectHelper.InstantiateAndGetComponent(FleetOrderListItemPrefab, FleetOrdersTransform);
				fleetOrderListItem.transform.localScale = Vector3.one;
				fleetOrderListItems.Add(fleetOrderListItem);
				fleetOrderListItem.DeleteFleetOrderEvent += NewFleetOrderListItem_DeleteFleetOrderEvent;
			}
			FleetActiveOrderListItem.DeleteFleetOrderEvent += FleetActiveOrderListItem_DeleteFleetOrderEvent;
			NewOrderButton.onClick.AddListener(NewOrderButtonClick);
			ClearOrdersButton.onClick.AddListener(ClearOrdersButtonClick);
			FleetSettingsButton.onClick.AddListener(FleetSettingsButtonClick);
			FleetCargoButton.onClick.AddListener(FleetCargoButtonClick);
		}

		private void FleetCargoButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowFleetCargoScreen(orderedFleet);
		}

		private void ClearOrdersButtonClick()
		{
			// Through OrdersHelper, not straight to the fleet.
			//
			// This called orderedFleet.ClearOrders() itself, which on a client
			// emptied a queue nobody was reading: the orders being run are the
			// host's, in the host's fleet. The screen went empty, the ship
			// carried on being flown by the host, and because the host only
			// hands a ship back when its fleet runs out of orders, cancelling
			// locally meant the player never got the ship back at all.
			//
			// OrderStop sends a Stop, the host stops the fleet, that clears
			// its orders, and that is what returns the ship.
			if (orderedFleet == null)
			{
				return;
			}

			Unit leader = orderedFleet.LeaderUnit;

			if (leader == null || leader.IsDestroyed)
			{
				// No ship to name. The host cannot be told what to stop, so
				// saying so beats clearing a local queue that was never the
				// one running.
				Debug.LogWarning("OrdersScreen: cannot cancel - this fleet has no"
					+ " ship to tell the host about. Nothing was cancelled.");

				return;
			}

			OrdersHelper.OrderStop(leader);

			orderedFleet.ClearOrders();

			Refresh();
		}

		private void FleetSettingsButtonClick()
		{
			if (orderedFleet != null)
			{
				UIController.Instance.ScreenNavigator.ShowFleetSettingsScreen(orderedFleet);
			}
		}

		private void NewOrderButtonClick()
		{
			if (orderedFleet.OrderQueue.Count >= EngineASX.Instance.GameSettings.MaxPlayerFleetStackedOrders)
			{
				UIController.Instance.ShowMessageBox("Cannot stack any more orders", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
				return;
			}
			UIController.Instance.ScreenNavigator.ShowNewFleetOrderScreen(orderedFleet, () =>
			{
				UIController.Instance.ScreenNavigator.NavigateBackTo(this);
				Refresh();
			});
		}

		private void NewFleetOrderListItem_DeleteFleetOrderEvent(FleetOrderListItem sender, FleetOrder fleetOrder)
		{
			if (orderedFleet != null && orderedFleet.IsValid && orderedFleet.OrderQueue.Contains(fleetOrder))
			{
				orderedFleet.OrderQueue.Remove(fleetOrder);
				fleetOrder.SafeDestroy(orderedFleet);
			}
			Refresh();
		}

		private void FleetActiveOrderListItem_DeleteFleetOrderEvent(FleetActiveOrderListItem sender, ActiveFleetOrder activeFleetOrder)
		{
			if (orderedFleet != null && orderedFleet.IsValid && orderedFleet.ActiveOrder == activeFleetOrder)
			{
				orderedFleet.ActiveOrder.SafeDestroy();
			}
			Refresh();
		}

		protected override void refresh()
		{
			base.refresh();
			if (orderedFleet != null && orderedFleet.IsValid)
			{
				RefreshFleetNameText();
				RefreshCurrentLocationText();
				RefreshFleetOrderListItems();
				PopulateShipList();
				RefreshContextButton();
				OrdersScreenShipList.SelectedItemsChanged -= ShipList_SelectedItemsChanged;
				OrdersScreenShipList.SelectedItemsChanged += ShipList_SelectedItemsChanged;
			}
		}

		private void PopulateShipList()
		{
			IOrderedEnumerable<Unit> items = from e in orderedFleet.Ships
				select e.Unit into e
				orderby e.IsFleetLeader() descending, e.CombatRating descending
				select e;
			OrdersScreenShipList.SetItems(items);
		}

		private void RefreshFleetOrderListItems()
		{
			FleetActiveOrderListItem.gameObject.SetActive(orderedFleet.ActiveOrder != null);
			FleetActiveOrderListItem.ActiveFleetOrder = orderedFleet.ActiveOrder;
			if (orderedFleet.ActiveOrder != null)
			{
				FleetActiveOrderListItem.Refresh();
			}
			for (int i = 0; i < EngineASX.Instance.GameSettings.MaxPlayerFleetStackedOrders; i++)
			{
				bool flag = i < orderedFleet.OrderQueue.Count;
				fleetOrderListItems[i].gameObject.SetActive(flag);
				if (flag)
				{
					fleetOrderListItems[i].Fleet = orderedFleet;
					fleetOrderListItems[i].FleetOrder = orderedFleet.OrderQueue[i];
					fleetOrderListItems[i].Refresh();
				}
			}
			NoOrdersText.gameObject.SetActive(!orderedFleet.HasAnyOrders);
		}

		protected override void update()
		{
			base.update();

			// A client with no fleet is not lost contact with a target.
			//
			// This used to navigate back out of the screen, which is how a
			// client lost the orders menu entirely: the menu builds a fleet
			// only so it has something to show, a client is not allowed to
			// build one, and the screen read the result as its target having
			// gone away. The ship was right there. The menu was refusing to
			// open for a reason that was true and irrelevant.
			//
			// So it stays open, says why there is nothing to show, and lets
			// the player give an order - which goes to the host, which is
			// where it was always going.
			if (orderedFleet == null && Authority.IsThisCopyAClient)
			{
				if (Time.time > nextUpdateOrdersButtonAndText)
				{
					ShowNoLocalFleet();
					nextUpdateOrdersButtonAndText = Time.time + ordersButtonAndTextUpdateFrequency;
				}

				return;
			}

			if (NavigateBackWhenFleetInvalid && !OrderedFleetIsValid && GetBackTarget() != null)
			{
				if (Eng.PlayerUnit != null && Eng.PlayerUnit.IsValidAndNotDestroyed)
				{
					UIController.Instance.QuickMsg.AddMessage("Lost contact with target");
				}
				NavigateBack();
			}
			else if (orderedFleet != null)
			{
				if (Time.time > nextUpdateOrdersButtonAndText)
				{
					RefreshCurrentLocationText();
					RefreshFleetOrderListItems();
					PopulateShipList();
					nextUpdateOrdersButtonAndText = Time.time + ordersButtonAndTextUpdateFrequency;
				}
				ClearOrdersButton.interactable = orderedFleet.HasAnyOrders;
				NewOrderButton.interactable = OrdersHelper.CanStackOrderOn(orderedFleet);
				OrdersScreenShipList.TickVisibleItems();
			}
		}

		private void ShowNoLocalFleet()
		{
			ClearOrdersButton.interactable = false;
			NewOrderButton.interactable = Eng.PlayerUnit != null
				&& Eng.PlayerUnit.IsValidAndNotDestroyed;

			FleetActiveOrderListItem.gameObject.SetActive(false);

			for (int i = 0; i < fleetOrderListItems.Count; i++)
			{
				fleetOrderListItems[i].gameObject.SetActive(false);
			}

			NoOrdersText.gameObject.SetActive(true);

			// Said once per opening rather than every refresh, because this
			// text is the screen's explanation and a log line per frame is not
			// an explanation to anybody.
			if (!saidNoLocalFleet)
			{
				saidNoLocalFleet = true;

				Debug.Log("Multiplayer: orders are held by the host for this ship."
					+ " An order given here is sent to it, and what it decides comes"
					+ " back to be shown.");
			}
		}

		private bool saidNoLocalFleet;

		private void RefreshFleetNameText()
		{
			FleetNameLabel.text = orderedFleet.GetFriendlyName();
		}

		private void RefreshCurrentLocationText()
		{
			CurrentLocationLabel.text = OrderedFleet.GetSectorAndLocationText();
		}

		protected override void onDisable()
		{
			base.onDisable();
			OrdersScreenShipList.CleanupOnDisable();
		}

		private void RefreshContextButton()
		{
			UnitContextButton.SetUnit(OrdersScreenShipList.SingleSelectedItem);
		}

		private void ShipList_SelectedItemsChanged(ScrollListBase sender)
		{
			RefreshContextButton();
		}
	}
}
