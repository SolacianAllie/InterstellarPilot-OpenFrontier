using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Claiming;
using OpenFrontier.IP.UI.Screens.SectorMap;
using OpenFrontier.IP.UI.Screens.UniverseMap;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class ClaimUnitOrderButton : OrderButton
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
				sectorMap.SectorMapForm.Title = "Select target to claim";
				sectorMap.SectorMapForm.AllowSelectionConfirm = true;
				sectorMap.SectorMap.CustomUnitDisplayFilter = (Unit unit) => OpenFrontier.IP.UI.Screens.SectorMap.SectorMap.AlwaysShowUnitOnMap(unit) || OrderTarget.IsUnitOrdered(unit) || ClaimUnitHelper.CanClaimUnit(unit);
				sectorMap.SectorMapForm.SectorMap.CustomSelectionFilter = (SectorMapSelectionItem item) => item.Unit != null && item.Unit.Faction == null && ClaimUnitHelper.CanClaimUnit(item.Unit);
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
				OrdersHelper.OrderClaimUnit(OrderTarget, unit, stack);
				OnOrderIssued();
			}
			else
			{
				OnOrderCancelled();
			}
		}
	}
}
