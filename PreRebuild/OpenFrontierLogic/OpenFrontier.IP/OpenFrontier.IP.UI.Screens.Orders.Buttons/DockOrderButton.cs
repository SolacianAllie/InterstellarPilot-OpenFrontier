using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class DockOrderButton : OrderButton
	{
		protected virtual Unit GetTargetUnit()
		{
			return null;
		}

		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable())
			{
				return OrdersHelper.CanOrderDockAtTarget(OrderTarget, GetTargetUnit());
			}
			return false;
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			OnOrderIssuing(out var stack);
			OrdersHelper.OrderDockAtTarget(OrderTarget, GetTargetUnit(), stack);
			OnOrderIssued();
		}
	}
}
