using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Hud
{
	public class HudScanner : MonoBehaviour
	{
		private static List<Unit> units = new List<Unit>(30);

		public void PopulateSceneUnitCache(LayerMask layerMask, List<HudScannerUnit> unitCache)
		{
			Unit playerUnit = EngineASX.Instance.PlayerUnit;
			unitCache.Clear();
			if (playerUnit == null || playerUnit.Faction == null)
			{
				return;
			}
			Sector sector = playerUnit.Sector;
			Faction faction = playerUnit.Faction;
			if (!(sector != null))
			{
				return;
			}
			units.Clear();
			faction.Intel.GetDiscoveredUnitIdsInSectorNonAlloc(sector, units, GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTimeWhenPlayerPilotting);
			foreach (Unit unit in units)
			{
				if (unit != null && unit != playerUnit && unit.ActiveUnit != null && unit.IsTargettable(faction))
				{
					unitCache.Add(new HudScannerUnit(unit, Maths.GetDistanceIgnoreY(unit.SectorPosition, playerUnit.SectorPosition)));
				}
			}
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Waypoint);
			if (unitsByType == null)
			{
				return;
			}
			foreach (Unit item in unitsByType)
			{
				unitCache.Add(new HudScannerUnit(item, Maths.GetDistanceIgnoreY(item.SectorPosition, playerUnit.SectorPosition)));
			}
		}

		private static void AddedOwnedAlliedOrDiscoveredUnitIfNeeded(LayerMask layerMask, List<Unit> unitCache, Unit localUnit, Faction localFaction, Unit targetUnit)
		{
			if (targetUnit != null && targetUnit != localUnit && UnityObjectHelper.LayerMaskContainsLayer(ref layerMask, targetUnit.gameObject.layer) && targetUnit.IsTargettable(localFaction))
			{
				if (targetUnit.IsOwnedByPlayerOrAllied())
				{
					unitCache.Add(targetUnit);
				}
				else if (targetUnit.IsStatic && localFaction.Intel.IsUnitDiscoveredOrOwned(targetUnit))
				{
					unitCache.Add(targetUnit);
				}
			}
		}
	}
}
