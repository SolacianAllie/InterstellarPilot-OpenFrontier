namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class UndockOrderButton : OrderButton
	{
		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable())
			{
				return OrderTarget.AnyUnitDocked;
			}
			return false;
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			OnOrderIssuing(out var stack);
			OrdersHelper.OrderUndock(OrderTarget, stack);
			OnOrderIssued();
		}
	}
}
