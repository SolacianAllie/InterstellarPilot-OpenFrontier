using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.UniverseMap;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class MoveToSectorOrderButton : OrderButton
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
			UIController.Instance.ScreenNavigator.ShowCustomUniverseMapScreen((UniverseMapScreen universeMap) =>
			{
				universeMap.Title = "Select Sector";
				universeMap.AllowSectorSelection = true;
				universeMap.EnabledSectors = OrdersHelper.GetPlayerUniverseMapPickableSectors();
				universeMap.ShowSelectedSectorInfo = false;
				universeMap.SectorSelectedCallback = SectorPicked;
				universeMap.CenterOnSector(OrderTarget.Sectors.FirstOrDefault());
				universeMap.RestrictNavigationAway();
			});
		}

		private void SectorPicked(UniverseMapScreen universeMapScreen, Sector sector)
		{
			universeMapScreen.SectorSelectedCallback = null;
			if (sector != null)
			{
				OnOrderIssuing(out var stack);
				OrdersHelper.OrderMoveToSector(OrderTarget, sector, stack);
				OnOrderIssued();
			}
			else
			{
				OnOrderCancelled();
			}
		}
	}
}
