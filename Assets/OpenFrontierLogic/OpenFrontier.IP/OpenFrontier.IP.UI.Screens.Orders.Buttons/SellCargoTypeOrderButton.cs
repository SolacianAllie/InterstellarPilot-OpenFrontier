using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.UI.Screens.MultiCargoPicker;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class SellCargoTypeOrderButton : OrderButton
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
			UIController.Instance.ScreenNavigator.ShowMultiCargoPickerScreen((MultiCargoPickerScreen screen) =>
			{
				screen.TitleLabel.text = "Select cargo types...";
				screen.Finished += Screen_Finished;
				screen.Items.AddRange(from e in OrderTarget.GetAllCargoTypes()
					orderby e.ClassName
					select new MultiCargoPickerItem
					{
						CargoClass = e
					});
			});
		}

		private void Screen_Finished(MultiCargoPickerScreen sender, MultiCargoPickerResult result)
		{
			if (result != null && result.CargoClasses.Count > 0)
			{
				OnOrderIssuing(out var stack);
				OrdersHelper.OrderSellCargo(OrderTarget, stack, (SellCargoOrder order) =>
				{
					order.SellOnlyListedCargos = true;
					order.SellCargoClasses.AddRange(result.CargoClasses);
				});
				OnOrderIssued();
			}
			else
			{
				OnOrderCancelled();
			}
		}
	}
}
