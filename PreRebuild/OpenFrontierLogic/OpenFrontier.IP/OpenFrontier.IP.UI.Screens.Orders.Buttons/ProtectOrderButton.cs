using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class ProtectOrderButton : OrderButton
	{
		protected virtual Unit GetTargetUnit()
		{
			return null;
		}

		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable())
			{
				return OrdersHelper.CanOrderProtectTarget(OrderTarget, GetTargetUnit());
			}
			return false;
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			SectorTarget target = new SectorTarget
			{
				TargetUnit = GetTargetUnit(),
				HadSceneObject = true
			};
			OnOrderIssuing(out var stack);
			OrdersHelper.OrderProtectTarget(OrderTarget, target, stack);
			OnOrderIssued();
		}
	}
}
