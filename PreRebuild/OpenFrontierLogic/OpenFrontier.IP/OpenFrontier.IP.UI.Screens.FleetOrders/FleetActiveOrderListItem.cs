using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.FleetOrders
{
	public class FleetActiveOrderListItem : MonoBehaviour
	{
		public delegate void DeleteFleetOrderHandler(FleetActiveOrderListItem sender, ActiveFleetOrder activeFleetOrder);

		public Graphic RequeueGraphic;

		public Graphic LimitedSpendGraphic;

		public Graphic MaxDurationGraphic;

		public TextMeshProUGUI OrderLabel;

		public TextMeshProUGUI OrderStatusLabel;

		public Button DeleteButton;

		public Button SettingsButton;

		public ActiveFleetOrder ActiveFleetOrder { get; set; }

		public event DeleteFleetOrderHandler DeleteFleetOrderEvent;

		public void Awake()
		{
			DeleteButton.onClick.AddListener(DeleteButtonClick);
			SettingsButton.onClick.AddListener(() =>
			{
				if (ActiveFleetOrder != null && ActiveFleetOrder.FleetOrder != null)
				{
					UIController.Instance.ScreenNavigator.ShowFleetOrderSettingsScreen(ActiveFleetOrder.Fleet, ActiveFleetOrder.FleetOrder);
				}
			});
		}

		private void DeleteButtonClick()
		{
			if (DeleteFleetOrderEvent != null)
			{
				DeleteFleetOrderEvent(this, ActiveFleetOrder);
			}
		}

		public void Refresh()
		{
			if (ActiveFleetOrder != null)
			{
				OrderLabel.text = OrdersHelper.GetFleetOrderText(ActiveFleetOrder.FleetOrder, EngineASX.Instance.LocalFaction, ActiveFleetOrder.Fleet.Sector);
				OrderStatusLabel.text = ActiveFleetOrder.GetStatusText(EngineASX.Instance.LocalFaction);
				RequeueGraphic.gameObject.SetActive(ActiveFleetOrder.FleetOrder.CompletionMode == FleetOrderCompletionMode.Requeue);
				LimitedSpendGraphic.gameObject.SetActive(!ActiveFleetOrder.FleetOrder.HasUnlimitedSpend);
				MaxDurationGraphic.gameObject.SetActive(ActiveFleetOrder.FleetOrder.MaxDuration > 0f);
			}
		}
	}
}
