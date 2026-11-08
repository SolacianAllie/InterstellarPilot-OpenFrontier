namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class WaitForAutoRepairOrderButton : OrderButton
	{
		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable())
			{
				return OrderTarget.AnyUnitNeedsRepair(1f, 1f, 1f);
			}
			return false;
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			OnOrderIssuing(out var stack);
			OrdersHelper.OrderWaitForAutoRepair(OrderTarget, stack);
			OnOrderIssued();
		}
	}
}
