using OpenFrontier.IP.UI.Screens.EnterSlider;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class RearmAtNearestOrderButton : OrderButton
	{
		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			RearmOrderHelper.ShowEquipmentUsagePrompt((EnterSliderIngameScreen handler, bool enteredValue, float newValue) =>
			{
				if (enteredValue)
				{
					OnOrderIssuing(out var stack);
					OrdersHelper.OrderRearmAtNearest(OrderTarget, newValue, stack);
					OnOrderIssued();
				}
			});
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
