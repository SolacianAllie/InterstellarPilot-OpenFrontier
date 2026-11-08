using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class EnterMyTargetWormholeOrderButton : OrderButton
	{
		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable() && HudTarget != null && HudTarget.UnitType == UnitType.Wormhole && HudTarget.IsValidAndNotDestroyed)
			{
				return OrderedFleetIsMobile;
			}
			return false;
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			if (HudTarget != null)
			{
				OnOrderIssuing(out var stack);
				OrdersHelper.OrderEnterWormhole(OrderTarget, HudTarget.WormholeComponent, stack);
				OnOrderIssued();
			}
		}
	}
}
