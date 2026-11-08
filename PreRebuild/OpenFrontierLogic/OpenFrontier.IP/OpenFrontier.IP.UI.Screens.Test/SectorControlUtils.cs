using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Testing.Spawning;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Test
{
	public static class SectorControlUtils
	{
		public static void ForceControlOfSector(Sector sector, Faction faction)
		{
			if (!(sector.ControllingFaction != faction))
			{
				return;
			}
			if (sector.ControllingFaction != null)
			{
				Unit sectorHQ = sector.GetSectorHQ();
				if (sectorHQ != null)
				{
					if (sectorHQ.Faction != null)
					{
						faction.CaptureUnit(sectorHQ, silent: true);
					}
					else
					{
						faction.ClaimUnit(sectorHQ, null, silent: true);
					}
				}
				else
				{
					SpawnSectorHQ(sector, faction);
				}
			}
			else
			{
				SpawnSectorHQ(sector, faction);
			}
		}

		public static Unit SpawnSectorHQ(Sector sector, Faction faction)
		{
			Vector3 randomSafeDeploymentSectorPosition = sector.GetRandomSafeDeploymentSectorPosition(0.75f, 400f, GameController.Instance.StaticNonOverlappingMask);
			return SpawnUtils.SpawnUnit(GameController.Instance.UnitClasses.SectorHQ.UnitPrefab, sector, randomSafeDeploymentSectorPosition, faction, addCargoLoadout: true, isUnderConstruction: false, silent: true);
		}
	}
}
