using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.MessageBox;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Fleets
{
	public class FleetsHelper
	{
		public static IEnumerable<Fleet> GetPlayerFleets(bool includeSingleShips = true)
		{
			return EngineASX.Instance.LocalFaction.Fleets.Where((Fleet e) => ShouldShowFleet(e, includeSingleShips));
		}

		public static IEnumerable<Fleet> GetOrderedPlayerFleets(bool includeSingleShips = true)
		{
			Sector localSector = EngineASX.Instance.ActiveSector;
			Vector3 localPosition = EngineASX.Instance.LocalUnit.SectorPosition;
			return from e in GetPlayerFleets(includeSingleShips)
				orderby GetDistanceOfFleetFromLocalUnit(e, localSector, localPosition), e.Ships.Count, e.Name
				select e;
		}

		public static float GetDistanceOfFleetFromLocalUnit(Fleet fleet, Sector localSector, Vector3 localPosition)
		{
			int jumpDistanceToAdjustedForDisconnected = fleet.Sector.GetJumpDistanceToAdjustedForDisconnected(localSector);
			if (jumpDistanceToAdjustedForDisconnected == 0)
			{
				return Vector3.Distance(localPosition, fleet.SectorPosition);
			}
			return (float)jumpDistanceToAdjustedForDisconnected * 20000f;
		}

		public static bool ShouldShowFleet(Fleet fleet, bool showSingleShips = true)
		{
			if (fleet != null && fleet.IsValid && fleet.LeaderUnit != null && fleet.Ships.Count > 0)
			{
				if (!showSingleShips)
				{
					return fleet.Ships.Count > 1;
				}
				return true;
			}
			return false;
		}

		public static bool PlayerHasAnyVisibleFleets(bool includeSingleShips = true)
		{
			foreach (Fleet fleet in EngineASX.Instance.LocalFaction.Fleets)
			{
				if (ShouldShowFleet(fleet, includeSingleShips))
				{
					return true;
				}
			}
			return false;
		}

		public static void ViewFleet(Fleet fleet)
		{
			if (fleet.LeaderUnit != null)
			{
				UIController.Instance.ScreenNavigator.ShowSectorMapScreenShowingUnit(fleet.LeaderUnit);
			}
			else
			{
				UIController.Instance.ScreenNavigator.ShowSectorMapScreenShowingSectorPosition(fleet.Sector, fleet.SectorPosition);
			}
		}

		public static void TooManyShipsInFleetMessage()
		{
			UIController.Instance.ShowMessageBox($"Too many ships (Max: {8})", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
		}

		public static bool ShouldShowFleetContextMenu(Unit unit)
		{
			Fleet fleet = unit.GetFleet();
			if (fleet == null)
			{
				return false;
			}
			return fleet.Ships.Count > 1;
		}

		public static bool ShouldShowFleetContextMenu(Fleet fleet)
		{
			if (fleet == null)
			{
				return false;
			}
			return fleet.Ships.Count > 1;
		}
	}
}
