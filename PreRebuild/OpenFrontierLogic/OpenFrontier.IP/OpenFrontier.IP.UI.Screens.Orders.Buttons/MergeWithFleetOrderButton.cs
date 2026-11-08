using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.FleetPicker;
using OpenFrontier.IP.UI.Screens.Fleets;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class MergeWithFleetOrderButton : OrderButton
	{
		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable())
			{
				return GetMergeFleetTargets().Count() > 0;
			}
			return false;
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			UIController.Instance.ScreenNavigator.ShowFleetPicker(GetMergeFleetTargets().ToList(), OnFleetPicked, allowNone: false, allowCreateNew: false, "Select fleet to merge into...");
		}

		private void OnFleetPicked(FleetPickerScreen sender, bool result, FleetPickerItem pickedFleet)
		{
			if (result && !OrderTarget.IsFleetOrdered(pickedFleet.Fleet))
			{
				OnOrderIssuing(out var stack);
				OrdersHelper.OrderMergeWithFleet(OrderTarget, pickedFleet.Fleet, stack);
				OnOrderIssued();
			}
		}

		private IEnumerable<Fleet> GetMergeFleetTargets()
		{
			return from e in FleetsHelper.GetOrderedPlayerFleets()
				where !OrderTarget.IsFleetOrdered(e) && e.Ships.Count + OrderTarget.AllUnits.Count() <= 8
				select e;
		}
	}
}
