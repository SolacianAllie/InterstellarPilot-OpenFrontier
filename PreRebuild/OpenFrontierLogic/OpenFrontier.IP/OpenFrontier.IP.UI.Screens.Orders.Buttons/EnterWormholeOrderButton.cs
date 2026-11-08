using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.SectorMap;
using OpenFrontier.IP.UI.Screens.UniverseMap;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class EnterWormholeOrderButton : OrderButton
	{
		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable() && OrderedFleetIsMobile)
			{
				return OrderTarget.Faction.Intel.HasDiscoveredAnyWormholes();
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
				UIController.Instance.ScreenNavigator.ShowSectorMapScreen(selectedSector, (SectorMapScreen sectorMap) =>
				{
					sectorMap.SectorMapForm.Title = "Select Wormhole";
					sectorMap.SectorMapForm.AllowSelectionConfirm = true;
					sectorMap.SectorMapForm.SectorMap.CustomUnitDisplayFilter = (Unit unit) => OpenFrontier.IP.UI.Screens.SectorMap.SectorMap.PreferToShowUnitOnMap(unit, OrderTarget) || unit.UnitType == UnitType.Wormhole || OrderTarget.IsUnitOrdered(unit);
					sectorMap.SectorMapForm.SectorMap.CustomSelectionFilter = (SectorMapSelectionItem item) => item.Unit != null && item.Unit.UnitType == UnitType.Wormhole;
					sectorMap.SectorMapForm.SelectedCallback = SectorMapWormholePicked;
					sectorMap.RestrictNavigationAway();
				});
			}
			else
			{
				OnOrderCancelled();
			}
		}

		private void SectorMapWormholePicked(SectorMapForm sender, SectorMapSelectionItem selected)
		{
			Unit unit = selected.Unit;
			if (unit != null)
			{
				OnOrderIssuing(out var stack);
				OrdersHelper.OrderEnterWormhole(OrderTarget, unit.WormholeComponent, stack);
				OnOrderIssued();
			}
			else
			{
				OnOrderCancelled();
			}
		}
	}
}
