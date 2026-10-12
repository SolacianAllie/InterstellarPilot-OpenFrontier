namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class MineAsteroidsOrderButton : OrderButton
	{
		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable())
			{
				if (OrderedFleetIsMobile)
				{
					return OrderTarget.AnyUnitHasMiningEquipment;
				}
				return false;
			}
			return false;
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			OnOrderIssuing(out var stack);
			OrdersHelper.OrderMineAsteroids(OrderTarget, stack);
			OnOrderIssued();
		}
	}
}
