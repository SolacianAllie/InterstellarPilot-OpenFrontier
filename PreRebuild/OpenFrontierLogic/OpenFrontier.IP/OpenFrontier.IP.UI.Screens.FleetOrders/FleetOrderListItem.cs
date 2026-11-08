using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.FleetOrders
{
	public class FleetOrderListItem : MonoBehaviour
	{
		public delegate void DeleteFleetOrderHandler(FleetOrderListItem sender, FleetOrder fleetOrder);

		public Graphic RequeueGraphic;

		public Graphic LimitedSpendGraphic;

		public Graphic MaxDurationGraphic;

		public TextMeshProUGUI OrderLabel;

		public Button DeleteButton;

		public Button SettingsButton;

		public FleetOrder FleetOrder { get; set; }

		public Fleet Fleet { get; set; }

		public event DeleteFleetOrderHandler DeleteFleetOrderEvent;

		public void Awake()
		{
			DeleteButton.onClick.AddListener(DeleteButtonClick);
			SettingsButton.onClick.AddListener(() =>
			{
				if (FleetOrder != null)
				{
					UIController.Instance.ScreenNavigator.ShowFleetOrderSettingsScreen(Fleet, FleetOrder);
				}
			});
		}

		private void DeleteButtonClick()
		{
			if (DeleteFleetOrderEvent != null)
			{
				DeleteFleetOrderEvent(this, FleetOrder);
			}
		}

		public void Refresh()
		{
			RefreshOrderLabel(FleetOrder);
			RequeueGraphic.gameObject.SetActive(FleetOrder.CompletionMode == FleetOrderCompletionMode.Requeue);
			LimitedSpendGraphic.gameObject.SetActive(!FleetOrder.HasUnlimitedSpend);
			MaxDurationGraphic.gameObject.SetActive(FleetOrder.MaxDuration > 0f);
		}

		private void RefreshOrderLabel(FleetOrder fleetOrder)
		{
			OrderLabel.text = OrdersHelper.GetFleetOrderText(fleetOrder, EngineASX.Instance.LocalFaction, (Fleet != null) ? Fleet.Sector : null);
		}
	}
}
