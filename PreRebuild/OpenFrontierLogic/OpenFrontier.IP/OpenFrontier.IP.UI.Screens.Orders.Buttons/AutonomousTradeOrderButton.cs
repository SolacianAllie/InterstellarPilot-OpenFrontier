namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class AutonomousTradeOrderButton : OrderButton
	{
		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable() && OrderTarget.AllUnitsMobile)
			{
				return OrderTarget.AllUnitsCanDock;
			}
			return false;
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			OnOrderIssuing(out var stack);
			OrdersHelper.OrderAutonomousTrade(OrderTarget, stack);
			OnOrderIssued();
		}
	}
}
