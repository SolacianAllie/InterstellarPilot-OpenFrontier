using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.SectorMap;
using OpenFrontier.IP.UI.Screens.UniverseMap;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class RepairAtOrderButton : OrderButton
	{
		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable())
			{
				return OrderTarget.AllUnitsCanDock;
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
				Sector sector = OrderTarget.Sectors.FirstOrDefault();
				if (sector != null)
				{
					universeMap.SelectSector(sector);
					universeMap.CenterOnSector(sector);
				}
				universeMap.RestrictNavigationAway();
			});
		}

		private void SectorPicked(UniverseMapScreen sender, Sector selectedSector)
		{
			if (selectedSector != null)
			{
				ShowSectorMap(selectedSector);
			}
			else
			{
				OnOrderCancelled();
			}
		}

		private void ShowSectorMap(Sector selectedSector)
		{
			UIController.Instance.ScreenNavigator.ShowSectorMapScreen(selectedSector, (SectorMapScreen sectorMap) =>
			{
				sectorMap.SectorMapForm.Title = "Select repair location";
				sectorMap.SectorMapForm.AllowSelectionConfirm = true;
				sectorMap.SectorMapForm.SectorMap.CustomUnitDisplayFilter = (Unit unit) => OpenFrontier.IP.UI.Screens.SectorMap.SectorMap.AlwaysShowUnitOnMap(unit) || OrdersHelper.CanOrderRepairAtTarget(OrderTarget, unit) || OrderTarget.IsUnitOrdered(unit);
				sectorMap.SectorMapForm.SectorMap.CustomSelectionFilter = (SectorMapSelectionItem item) => item.Unit != null && OrdersHelper.CanOrderRepairAtTarget(OrderTarget, item.Unit);
				sectorMap.SectorMapForm.SelectedCallback = SectorMapItemPicked;
				sectorMap.RestrictNavigationAway();
			});
		}

		private void SectorMapItemPicked(SectorMapForm sender, SectorMapSelectionItem selected)
		{
			Unit unit = selected.Unit;
			if (unit != null)
			{
				OnOrderIssuing(out var stack);
				OrdersHelper.OrderRepairAt(OrderTarget, unit, stack);
				OnOrderIssued();
			}
			else
			{
				OnOrderCancelled();
			}
		}
	}
}
