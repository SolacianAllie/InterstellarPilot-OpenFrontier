using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.BuildModePlacement;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.billing;

namespace OpenFrontier.IP.UI.Screens.BuildMode
{
	public static class BuildModeHelper
	{
		public static void AttemptToBuildStation(UnitClass unitClass)
		{
			if (!BuildStationValidator.CanBuildInSector(unitClass, EngineASX.Instance.LocalPlayerSector, out var errorMessage))
			{
				UIController.Instance.ShowMessageBox(errorMessage, MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
			}
			else if (!CanAffordToBuild(unitClass))
			{
				UIController.Instance.ShowInsufficientCreditsMessageBox();
			}
			else if (!ShowStoreScreenIfRequiresIAP(unitClass))
			{
				NavigateToBuildPlacementScreen(unitClass);
			}
		}

		public static bool ShowStoreScreenIfRequiresIAP(UnitClass unitClass)
		{
			IPProduct requiredProduct = unitClass.RequiredProduct;
			if (ItemRequiresIAP(unitClass) && !Products.HasProduct(requiredProduct))
			{
				UIController.Instance.ScreenNavigator.ShowStoreScreen(requiredProduct);
				return true;
			}
			return false;
		}

		public static bool ItemRequiresIAP(UnitClass unitClass)
		{
			if (Products.IAPEnabled)
			{
				return unitClass.RequiredProduct != null;
			}
			return false;
		}

		private static void NavigateToBuildPlacementScreen(UnitClass unitClass)
		{
			EngineASX.Instance.TrySetHudCameraEnabled(enabled: false);
			UIController.Instance.ScreenNavigator.ShowBuildModePlacementScreen(unitClass, (BuildModePlacementScreen screen) =>
			{
				screen.RequestBuild += Screen_RequestBuild;
				screen.OnBuilt += Screen_OnBuilt;
			});
		}

		private static void Screen_OnBuilt(bool preferExit)
		{
			if (preferExit)
			{
				EngineASX.Instance.SetUIFromPlayerStatus();
			}
		}

		public static bool CanAffordToBuild(UnitClass unitClass)
		{
			return EngineASX.Instance.LocalFaction.Credits >= unitClass.SaleCost;
		}

		public static bool CanAffordToBuildMultiple(UnitClass unitClass)
		{
			return EngineASX.Instance.LocalFaction.Credits >= unitClass.SaleCost * 2;
		}

		public static bool CanBuildMultiple(UnitClass unitClass)
		{
			if (unitClass.StationPurpose == StationPurpose.SectorControl)
			{
				return false;
			}
			return true;
		}

		private static bool Screen_RequestBuild(UnitClass unitClass)
		{
			if (!CanAffordToBuild(unitClass))
			{
				UIController.Instance.ShowInsufficientCreditsMessageBox();
				return false;
			}
			return true;
		}
	}
}
