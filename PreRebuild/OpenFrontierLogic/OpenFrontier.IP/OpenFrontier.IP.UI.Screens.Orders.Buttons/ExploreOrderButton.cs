namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class ExploreOrderButton : OrderButton
	{
		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable())
			{
				return OrderedFleetIsMobile;
			}
			return false;
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			OnOrderIssuing(out var stack);
			OrdersHelper.OrderExplore(OrderTarget, stack);
			OnOrderIssued();
		}
	}
}
