using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.UI.Extensions;
using OpenFrontier.IP.UI.Screens.NewFleetOrder;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class OrderButton : MonoBehaviour, IOrderButton
	{
		public delegate void OrderedHandler(OrderButton sender);

		public delegate void OrderingHandler(OrderButton sender, out bool stack);

		public bool Stackable;

		private Button button;

		private NewOrderTarget orderTarget;

		public OrderButtonTargetType TargetType;

		public NewOrderTarget OrderTarget
		{
			get
			{
				return orderTarget;
			}
			set
			{
				if (orderTarget != value)
				{
					orderTarget = value;
				}
			}
		}

		public Faction OrderedUnitFaction
		{
			get
			{
				if (OrderTarget != null)
				{
					return OrderTarget.Faction;
				}
				return null;
			}
		}

		public bool OrderedFleetIsMobile => OrderTarget.AllUnitsMobile;

		public Unit HudTarget
		{
			get
			{
				if (EngineASX.Instance.Hud != null)
				{
					return EngineASX.Instance.Hud.CurrentTarget;
				}
				return null;
			}
		}

		public Unit WaypointTargetUnit => EngineASX.Instance.LocalPlayer.CustomUnitWaypoint;

		public Button Button => button;

		public bool CanStack => Stackable;

		public event OrderedHandler Ordered;

		public event OrderingHandler Ordering;

		private void Awake()
		{
			button = GetComponent<Button>();
			button.onClick.AddListener(ButtonClick);
		}

		private void Start()
		{
			OnStarted();
		}

		protected virtual void OnStarted()
		{
		}

		public void Refresh()
		{
			button.interactable = ShouldBeInteractable();
			switch (TargetType)
			{
			case OrderButtonTargetType.CurrentTarget:
			{
				Unit hudTarget = HudTarget;
				if (hudTarget != null)
				{
					string text2 = button.GetText().Replace("my target", "[" + hudTarget.GetFriendlyNameForFaction(EngineASX.Instance.LocalFaction) + "]");
					button.SetText(text2);
				}
				break;
			}
			case OrderButtonTargetType.CurrentWaypoint:
			{
				Unit waypointTargetUnit = WaypointTargetUnit;
				if (waypointTargetUnit != null)
				{
					string text = button.GetText().Replace("my waypoint", "[" + waypointTargetUnit.GetFriendlyNameForFaction(EngineASX.Instance.LocalFaction) + "]");
					button.SetText(text);
				}
				break;
			}
			}
		}

		private void ButtonClick()
		{
			if (ShouldBeInteractable())
			{
				OnButtonClick();
			}
		}

		protected void OnOrderIssuing(out bool stack)
		{
			stack = true;
			if (Ordering != null)
			{
				Ordering(this, out stack);
			}
		}

		protected void OnOrderIssued()
		{
			if (Ordered != null)
			{
				Ordered(this);
			}
		}

		protected void OnOrderCancelled()
		{
			if (Ordered != null)
			{
				Ordered(this);
			}
		}

		protected virtual void OnButtonClick()
		{
		}

		protected virtual bool ShouldBeInteractable()
		{
			if (orderTarget != null && orderTarget.IsValid)
			{
				return OrdersHelper.CanStackOrderOn(orderTarget);
			}
			return false;
		}

		protected virtual void OnOrderFleetChanged(Fleet oldOrderedFleet, Fleet orderedFleet)
		{
		}
	}
}
