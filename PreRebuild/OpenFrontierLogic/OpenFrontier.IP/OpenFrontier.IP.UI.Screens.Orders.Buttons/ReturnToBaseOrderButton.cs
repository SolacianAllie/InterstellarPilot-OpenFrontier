namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class ReturnToBaseOrderButton : OrderButton
	{
		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable())
			{
				if (OrderTarget != null)
				{
					return OrderTarget.AnyFleetHasHomeBase;
				}
				return false;
			}
			return false;
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			OnOrderIssuing(out var stack);
			OrdersHelper.OrderReturnToBase(OrderTarget, stack);
			OnOrderIssued();
		}
	}
}
