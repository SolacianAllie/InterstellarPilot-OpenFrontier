using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class MoveToUnitOrderButton : OrderButton
	{
		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable() && OrderedFleetIsMobile && GetTargetUnit() != null)
			{
				return !OrderTarget.IsUnitOrdered(GetTargetUnit());
			}
			return false;
		}

		protected virtual Unit GetTargetUnit()
		{
			return null;
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			SectorTarget sectorTarget = new SectorTarget
			{
				TargetUnit = GetTargetUnit(),
				HadSceneObject = true
			};
			OnOrderIssuing(out var stack);
			OrdersHelper.OrderMoveToSectorTarget(OrderTarget, sectorTarget, stack);
			OnOrderIssued();
		}
	}
}
