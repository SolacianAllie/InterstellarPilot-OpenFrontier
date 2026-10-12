using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.UI.Screens.SectorMap;
using OpenFrontier.IP.UI.Screens.UniverseMap;

namespace OpenFrontier.IP.UI.Screens.Orders
{
	public static class ChangeFleetHomeBase
	{
		private static IEnumerable<Fleet> changingFleets;

		private static Action callback;

		public static void Change(IEnumerable<Fleet> fleets, Action cb)
		{
			changingFleets = fleets;
			callback = cb;
			UIController.Instance.ScreenNavigator.ShowCustomUniverseMapScreen((UniverseMapScreen universeMap) =>
			{
				universeMap.Title = "Select Sector";
				universeMap.AllowSectorSelection = true;
				universeMap.EnabledSectors = OrdersHelper.GetPlayerUniverseMapPickableSectorsWithEnabledNavigation();
				universeMap.ShowSelectedSectorInfo = false;
				universeMap.SectorSelectedCallback = SectorPicked;
				universeMap.SelectSector(fleets.First().Sector);
				universeMap.CenterOnSector(fleets.First().Sector);
				universeMap.RestrictNavigationAway();
			});
		}

		private static void SectorPicked(UniverseMapScreen sender, Sector selectedSector)
		{
			if (selectedSector != null)
			{
				ShowSectorMap(selectedSector);
			}
		}

		private static void ShowSectorMap(Sector selectedSector)
		{
			UIController.Instance.ScreenNavigator.ShowSectorMapScreen(selectedSector, (SectorMapScreen sectorMap) =>
			{
				sectorMap.SectorMapForm.Title = "Select home base";
				sectorMap.SectorMapForm.AllowSelectionConfirm = true;
				sectorMap.SectorMapForm.SectorMap.CustomUnitDisplayFilter = (Unit unit) => OpenFrontier.IP.UI.Screens.SectorMap.SectorMap.AlwaysShowUnitOnMap(unit) || unit.IsOwnedByPlayer || CanUnitBeHomeBase(unit, EngineASX.Instance.LocalFaction);
				sectorMap.SectorMapForm.SectorMap.CustomSelectionFilter = (SectorMapSelectionItem item) => item.Unit == null || (!changingFleets.Contains(item.Unit.GetFleet()) && CanUnitBeHomeBase(item.Unit, EngineASX.Instance.LocalFaction));
				sectorMap.SectorMapForm.SelectedCallback = SectorMapItemPicked;
				sectorMap.RestrictNavigationAway();
			});
		}

		private static void SectorMapItemPicked(SectorMapForm sender, SectorMapSelectionItem selected)
		{
			Unit unit = selected.Unit;
			if (unit != null)
			{
				foreach (Fleet changingFleet in changingFleets)
				{
					changingFleet.SetHomeBaseToUnit(unit);
				}
			}
			else
			{
				foreach (Fleet changingFleet2 in changingFleets)
				{
					changingFleet2.SetHomeBaseToSectorPosition(selected.Sector, selected.SectorPosition);
				}
			}
			callback();
		}

		private static bool CanUnitBeHomeBase(Unit unit, Faction playerFaction)
		{
			if (unit != null && unit.IsDockable && unit.Faction != null && (unit.IsStatic || EngineASX.Instance.GameSettings.AllowFleetToSetHomeBaseToUnownedUnalliedCapitalShip || unit.IsOwnedByPlayerOrAllied()))
			{
				if (!(unit.Faction == playerFaction))
				{
					if (unit.Faction.GetOpinion(playerFaction) > -0.5f)
					{
						return !unit.Faction.IsHostileToOrAlwaysHostileTo(playerFaction);
					}
					return false;
				}
				return true;
			}
			return false;
		}
	}
}
