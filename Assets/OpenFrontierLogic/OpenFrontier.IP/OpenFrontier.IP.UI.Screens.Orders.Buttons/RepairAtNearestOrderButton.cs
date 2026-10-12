namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class RepairAtNearestOrderButton : OrderButton
	{
		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			OnOrderIssuing(out var stack);
			OrdersHelper.OrderRepairAtNearest(OrderTarget, stack);
			OnOrderIssued();
		}

		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable())
			{
				return OrderTarget.AllUnitsCanDock;
			}
			return false;
		}
	}
}
