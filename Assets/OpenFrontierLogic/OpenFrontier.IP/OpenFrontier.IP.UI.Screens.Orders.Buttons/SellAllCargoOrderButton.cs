using OpenFrontier.IP.Engine.Fleets.FleetOrders;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class SellAllCargoOrderButton : OrderButton
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
			OrdersHelper.OrderSellCargo(OrderTarget, stack, (SellCargoOrder order) =>
			{
				order.SellOnlyListedCargos = false;
				order.SellEquipment = true;
			});
			OnOrderIssued();
		}
	}
}
