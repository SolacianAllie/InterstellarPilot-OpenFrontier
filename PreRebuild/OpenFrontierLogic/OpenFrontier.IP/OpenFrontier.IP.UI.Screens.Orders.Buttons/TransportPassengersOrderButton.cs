namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class TransportPassengersOrderButton : OrderButton
	{
		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable())
			{
				return OrderTarget.AnyUnitCanTransportPassengers;
			}
			return false;
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			OnOrderIssuing(out var stack);
			OrdersHelper.OrderTransportPassengers(OrderTarget, stack);
			OnOrderIssued();
		}
	}
}
