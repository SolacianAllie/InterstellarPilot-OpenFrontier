using OpenFrontier.IP.Common.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionAIEmpire : FactionAIBase
	{
		public override FactionAIType AIType => FactionAIType.Empire;

		public override bool CanBuildShipsInSector(Sector sector)
		{
			if (sector.ControllingFaction != faction)
			{
				return false;
			}
			return base.CanBuildShipsInSector(sector);
		}

		public override bool CanBuildStationInSector(Sector sector, UnitClass unitClass)
		{
			if (sector.ControllingFaction != null && sector.ControllingFaction != faction)
			{
				return false;
			}
			if (sector.ControllingFaction == null)
			{
				return unitClass.StationPurpose switch
				{
					StationPurpose.Outpost => base.CanBuildStationInSector(sector, unitClass), 
					StationPurpose.Satellite => sector.IsSectorANeighbourOfControllingFaction(faction), 
					_ => false, 
				};
			}
			return base.CanBuildStationInSector(sector, unitClass);
		}

		public override int GetPreferredCountOfUnitClass(UnitClass unitClass)
		{
			if (unitClass.StationPurpose == StationPurpose.SectorControl)
			{
				return Mathf.CeilToInt(faction.Greed * (float)EngineASX.Instance.Sectors.Count);
			}
			return base.GetPreferredCountOfUnitClass(unitClass);
		}

		public override float GetDamageMultiplierForIndirectFireFromNpcFaction(Faction sourceFaction)
		{
			switch (sourceFaction.FactionType)
			{
			case FactionType.Empire:
				return 0.35f;
			case FactionType.Bandit:
			case FactionType.Outlaw:
				return 1f;
			case FactionType.BountyHunter:
				return 0.25f;
			default:
				return base.GetDamageMultiplierForIndirectFireFromNpcFaction(sourceFaction);
			}
		}
	}
}
