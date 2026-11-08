using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class FollowOrderButton : OrderButton
	{
		protected virtual Unit GetTargetUnit()
		{
			return null;
		}

		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable())
			{
				return OrdersHelper.CanFleetFollowTarget(OrderTarget, GetTargetUnit());
			}
			return false;
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			OnOrderIssuing(out var stack);
			OrdersHelper.OrderFollowTarget(OrderTarget, GetTargetUnit(), stack);
			OnOrderIssued();
		}
	}
}
