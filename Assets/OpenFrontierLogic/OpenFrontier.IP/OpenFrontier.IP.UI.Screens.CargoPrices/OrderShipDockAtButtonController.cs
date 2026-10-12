using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.CargoPrices
{
	public class OrderShipDockAtButtonController : MonoBehaviour
	{
		public Unit TargetUnit;

		public Button Button;

		private void Awake()
		{
			Button.onClick.AddListener(ButtonClick);
		}

		private void ButtonClick()
		{
			OrdersHelper.OrderDockAtTarget(OrdersHelper.FindOrCreateNpcAndFleetAndPilotShip(EngineASX.Instance.LocalUnit), TargetUnit, stack: false);
		}

		private void Update()
		{
			Button.interactable = CanOrderMoveToTarget();
		}

		private bool CanOrderMoveToTarget()
		{
			return OrdersHelper.CanOrderDockAtTarget(EngineASX.Instance.LocalUnit, TargetUnit);
		}
	}
}
