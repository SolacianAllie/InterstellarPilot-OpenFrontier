using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.Orders.PatrolOrderCreator;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class PatrolOrderButton : OrderButton
	{
		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable())
			{
				return OrderedFleetIsMobile;
			}
			return false;
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			UIController.Instance.ScreenNavigator.ShowPatrolOrderCreatorScreen((PatrolOrderCreatorScreen patrolOrderCreatorScreen) =>
			{
				patrolOrderCreatorScreen.OnPatrolRouteConfirmed += OnPatrolRouteConfirmed;
				patrolOrderCreatorScreen.SelectSector(OrderTarget.Sectors.FirstOrDefault());
			});
		}

		private void OnPatrolRouteConfirmed(IEnumerable<SectorTarget> patrolNodes, bool isLoop, bool repeat)
		{
			OnOrderIssuing(out var stack);
			OrdersHelper.OrderPatrol(OrderTarget, patrolNodes, isLoop, repeat, stack);
			OnOrderIssued();
		}
	}
}
