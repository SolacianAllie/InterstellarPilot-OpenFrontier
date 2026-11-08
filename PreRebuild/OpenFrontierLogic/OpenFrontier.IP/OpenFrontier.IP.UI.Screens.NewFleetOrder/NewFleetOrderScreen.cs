using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.Orders;
using OpenFrontier.IP.UI.Screens.Orders.Buttons;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.NewFleetOrder
{
	public class NewFleetOrderScreen : EngineScreen
	{
		public bool StackOrder = true;

		public OrdersScreenCategory OrderCategory;

		private List<IOrderButton> orderButtons = new List<IOrderButton>(20);

		public Text FleetNameLabel;

		private NewOrderTarget orderTarget;

		public Transform OrderButtonsContainer;

		public Action OrderIssuedCallback { get; set; }

		public NewOrderTarget OrderTarget
		{
			get
			{
				return orderTarget;
			}
			set
			{
				if (orderTarget == value)
				{
					return;
				}
				orderTarget = value;
				foreach (IOrderButton orderButton in orderButtons)
				{
					orderButton.OrderTarget = orderTarget;
				}
				Refresh();
			}
		}

		protected override void awake()
		{
			base.awake();
			FindOrderButtons();
		}

		protected override void refresh()
		{
			base.refresh();
			if (orderTarget != null)
			{
				RefreshOrderButtonsEnabled();
				RefreshFleetNameText();
				RefreshVisibleOrderButtons();
			}
		}

		private void RefreshFleetNameText()
		{
			FleetNameLabel.text = GetFleetNameText(orderTarget);
		}

		public static string GetFleetNameText(NewOrderTarget orderTarget)
		{
			if (orderTarget.MixedUnitsAndFleets)
			{
				return "[Multiple targets]";
			}
			if (orderTarget.UnitsWithoutFleet.Count() == 0)
			{
				if (orderTarget.OrderedFleets.Count() < 4)
				{
					return string.Join(", ", orderTarget.OrderedFleets.Select((Fleet e) => e.GetFriendlyName()));
				}
				return "[Multiple fleets]";
			}
			if (orderTarget.UnitsWithoutFleet.Count() < 4)
			{
				return string.Join(", ", orderTarget.UnitsWithoutFleet.Select((Unit e) => e.GetFriendlyName()));
			}
			return "[Multiple ships]";
		}

		private void FindOrderButtons()
		{
			orderButtons.Clear();
			OrderButton[] componentsInChildren = OrderButtonsContainer.GetComponentsInChildren<OrderButton>();
			foreach (OrderButton orderButton in componentsInChildren)
			{
				orderButtons.Add(orderButton);
				orderButton.Ordered += OrderButton_Ordered;
				orderButton.Ordering += OrderButton_Ordering;
			}
		}

		private void OrderButton_Ordering(OrderButton sender, out bool stack)
		{
			stack = StackOrder;
		}

		public void SetCombatCategory()
		{
			SetOrderCategory(OrdersScreenCategory.Combat);
		}

		public void SetTradeCategory()
		{
			SetOrderCategory(OrdersScreenCategory.Trade);
		}

		public void SetSpecialCategory()
		{
			SetOrderCategory(OrdersScreenCategory.Special);
		}

		public void RefreshVisibleOrderButtons()
		{
			OrdersScreenButtonCategory[] componentsInChildren = OrderButtonsContainer.GetComponentsInChildren<OrdersScreenButtonCategory>(includeInactive: true);
			foreach (OrdersScreenButtonCategory ordersScreenButtonCategory in componentsInChildren)
			{
				ordersScreenButtonCategory.gameObject.SetActive(OrderCategory == ordersScreenButtonCategory.Category);
			}
		}

		public void SetNavigationCategory()
		{
			SetOrderCategory(OrdersScreenCategory.Navigation);
		}

		private void SetOrderCategory(OrdersScreenCategory category)
		{
			OrderCategory = category;
			RefreshVisibleOrderButtons();
		}

		private void OrderButton_Ordered(OrderButton sender)
		{
			if (orderTarget != null)
			{
				foreach (Fleet orderedFleet in orderTarget.OrderedFleets)
				{
					orderedFleet.AssignNextOrderIfNoCurrentOrder();
				}
			}
			if (OrderIssuedCallback != null)
			{
				OrderIssuedCallback();
			}
		}

		private int GetButtonOrder(IOrderButton button)
		{
			return ((!button.Button.interactable) ? 1000 : 0) + orderButtons.IndexOf(button);
		}

		private void RefreshOrderButtonsEnabled()
		{
			foreach (IOrderButton orderButton in orderButtons)
			{
				orderButton.Refresh();
			}
			int num = 0;
			foreach (IOrderButton item in orderButtons.OrderBy((IOrderButton e) => GetButtonOrder(e)))
			{
				item.Button.transform.SetSiblingIndex(num++);
			}
		}
	}
}
