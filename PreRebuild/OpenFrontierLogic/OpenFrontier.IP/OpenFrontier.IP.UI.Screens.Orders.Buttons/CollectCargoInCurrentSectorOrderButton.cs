using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class CollectCargoInCurrentSectorOrderButton : CollectCargoOrderButton
	{
		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable() && OrderedFleetIsMobile)
			{
				return OrderTarget.AnyUnitHasTractorBeam;
			}
			return false;
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			OnOrderIssuing(out var stack);
			foreach (Fleet item in OrderTarget.CreateAndReturnFleets())
			{
				OrdersHelper.OrderScavenge(item, item.Sector, stack);
			}
			OnOrderIssued();
		}
	}
}
