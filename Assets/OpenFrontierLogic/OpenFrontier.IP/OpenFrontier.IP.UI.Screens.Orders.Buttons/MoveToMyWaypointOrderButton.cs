using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class MoveToMyWaypointOrderButton : OrderButton
	{
		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable() && OrderedFleetIsMobile && EngineASX.Instance.LocalPlayer.HasCustomWaypoint)
			{
				PlayerWaypoint value = EngineASX.Instance.LocalPlayer.WaypointController.CustomPath.Waypoint.Value;
				if (!(value.TargetUnit == null))
				{
					return !OrderTarget.IsUnitOrdered(value.TargetUnit);
				}
				return true;
			}
			return false;
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			SectorTarget sectorTarget = EngineASX.Instance.LocalPlayer.WaypointController.CustomPath.Waypoint.Value.CreateSectorTarget();
			OnOrderIssuing(out var stack);
			OrdersHelper.OrderMoveToSectorTarget(OrderTarget, sectorTarget, stack);
			OnOrderIssued();
		}
	}
}
