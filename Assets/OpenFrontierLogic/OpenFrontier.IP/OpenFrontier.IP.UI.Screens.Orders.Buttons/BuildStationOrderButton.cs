using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.BuildMode;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.UI.Screens.SectorMap;
using OpenFrontier.IP.UI.Screens.UniverseMap;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class BuildStationOrderButton : OrderButton
	{
		private UnitClass unitClassToBuild;

		protected override bool ShouldBeInteractable()
		{
			if (base.ShouldBeInteractable() && OrderedFleetIsMobile)
			{
				if (!(OrderTarget.SingleFleet != null))
				{
					return OrderTarget.IsSingleUnitWithoutFleet;
				}
				return true;
			}
			return false;
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			UIController.Instance.ScreenNavigator.ShowCustomBuildModeScreen(BuildModeScreenItemSelected);
		}

		private void BuildModeScreenItemSelected(BuildModeScreen buildModeScreen, UnitClass unitClass)
		{
			unitClassToBuild = unitClass;
			if (EngineASX.Instance.LocalFaction.Credits < unitClass.SaleCost)
			{
				UIController.Instance.ShowInsufficientCreditsMessageBox();
				OnOrderCancelled();
			}
			else if (!BuildModeHelper.ShowStoreScreenIfRequiresIAP(unitClass))
			{
				UIController.Instance.ScreenNavigator.ShowCustomUniverseMapScreen((UniverseMapScreen universeMap) =>
				{
					universeMap.Title = "Select sector to build in";
					universeMap.AllowSectorSelection = true;
					universeMap.EnabledSectors = OrdersHelper.GetPlayerUniverseMapPickableSectors();
					universeMap.ShowSelectedSectorInfo = false;
					universeMap.SectorSelectedCallback = SectorPicked;
					universeMap.SetSectorFromOrderTarget(OrderTarget);
					universeMap.RestrictNavigationAway();
				});
			}
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
				sectorMap.SectorMapForm.Title = "Select build location";
				sectorMap.SectorMapForm.AllowSelectionConfirm = true;
				sectorMap.SectorMapForm.SectorMap.CustomSelectionFilter = (SectorMapSelectionItem item) => item.Unit == null;
				sectorMap.SectorMapForm.SelectedCallback = SectorMapItemPicked;
				sectorMap.RestrictNavigationAway();
			});
		}

		private void SectorMapItemPicked(SectorMapForm sender, SectorMapSelectionItem selected)
		{
			if (!BuildStationValidator.CanBuild(unitClassToBuild, selected.Sector, selected.SectorPosition, out var errorMessage))
			{
				UIController.Instance.ShowMessageBox(errorMessage, MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
				return;
			}
			if (OrderTarget.SingleFleet == null && !OrderTarget.SingleUnitWithoutFleet)
			{
				OnOrderCancelled();
				return;
			}
			Fleet fleet = OrderTarget.CreateAndReturnFleets().First();
			OnOrderIssuing(out var stack);
			OrdersHelper.OrderBuildStation(fleet, unitClassToBuild, selected.Sector, selected.SectorPosition, stack);
			OnOrderIssued();
		}
	}
}
