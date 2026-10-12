using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class EnterMyWaypointWormholeOrderButton : OrderButton
	{
		protected override bool ShouldBeInteractable()
		{
			Unit customUnitWaypoint = EngineASX.Instance.LocalPlayer.CustomUnitWaypoint;
			if (base.ShouldBeInteractable() && customUnitWaypoint != null && customUnitWaypoint.UnitType == UnitType.Wormhole && customUnitWaypoint.IsValidAndNotDestroyed)
			{
				return OrderedFleetIsMobile;
			}
			return false;
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			Unit customUnitWaypoint = EngineASX.Instance.LocalPlayer.CustomUnitWaypoint;
			if (customUnitWaypoint != null)
			{
				OnOrderIssuing(out var stack);
				OrdersHelper.OrderEnterWormhole(OrderTarget, customUnitWaypoint.WormholeComponent, stack);
				OnOrderIssued();
			}
		}
	}
}
