using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.SectorMap;
using OpenFrontier.IP.UI.Screens.UniverseMap;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class CollectCargoOrderButton : OrderButton
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
			UIController.Instance.ScreenNavigator.ShowCustomUniverseMapScreen((UniverseMapScreen universeMap) =>
			{
				universeMap.Title = "Select Sector";
				universeMap.AllowSectorSelection = true;
				universeMap.EnabledSectors = OrdersHelper.GetPlayerUniverseMapPickableSectors();
				universeMap.ShowSelectedSectorInfo = false;
				universeMap.SectorSelectedCallback = SectorPicked;
				universeMap.SetSectorFromOrderTarget(OrderTarget);
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
				sectorMap.SectorMapForm.Title = "Select cargo to collect";
				sectorMap.SectorMapForm.AllowSelectionConfirm = true;
				sectorMap.SectorMap.CustomUnitDisplayFilter = (Unit unit) => OpenFrontier.IP.UI.Screens.SectorMap.SectorMap.PreferToShowUnitOnMap(unit, OrderTarget) || OrdersHelper.CanOrderCollectTarget(OrderTarget, unit) || OrderTarget.IsUnitOrdered(unit);
				sectorMap.SectorMap.CustomSelectionFilter = (SectorMapSelectionItem item) => OrdersHelper.CanOrderCollectTarget(OrderTarget, item.Unit);
				sectorMap.SectorMapForm.SelectedCallback = SectorMapItemPicked;
				sectorMap.RestrictNavigationAway();
			});
		}

		private void SectorMapItemPicked(SectorMapForm sender, SectorMapSelectionItem selected)
		{
			if (selected.Unit != null)
			{
				OnOrderIssuing(out var stack);
				OrdersHelper.OrderCollectCargo(OrderTarget, selected.Unit, stack);
				OnOrderIssued();
			}
			else
			{
				OnOrderCancelled();
			}
		}
	}
}
