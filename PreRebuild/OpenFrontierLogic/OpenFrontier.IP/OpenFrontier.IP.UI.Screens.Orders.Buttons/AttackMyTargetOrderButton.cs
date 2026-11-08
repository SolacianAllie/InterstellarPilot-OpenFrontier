namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class AttackMyTargetOrderButton : OrderButton
	{
		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable() && OrderTarget.AnyUnitArmed)
			{
				return OrdersHelper.CanOrderAttackTarget(OrderTarget, HudTarget);
			}
			return false;
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			OnOrderIssuing(out var stack);
			OrdersHelper.OrderAttackUnit(OrderTarget, HudTarget, stack);
			OnOrderIssued();
		}
	}
}
