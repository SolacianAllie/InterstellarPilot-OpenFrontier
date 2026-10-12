using OpenFrontier.IP.Common.Factions;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionAIBountyHunter : FactionAIBase
	{
		public override FactionAIType AIType => FactionAIType.BountyHunter;

		public override float GetDamageMultiplierForIndirectFireFromNpcFaction(Faction sourceFaction)
		{
			switch (sourceFaction.FactionType)
			{
			case FactionType.Empire:
				return 0.1f;
			case FactionType.BountyHunter:
			case FactionType.Bandit:
			case FactionType.Outlaw:
				return 1.2f;
			default:
				return base.GetDamageMultiplierForIndirectFireFromNpcFaction(sourceFaction);
			}
		}

		public override float GetStationTypeBuildPriority(UnitClass unitClass)
		{
			switch (unitClass.StationPurpose)
			{
			case StationPurpose.Equipment:
			case StationPurpose.Repair:
				return 5f;
			case StationPurpose.Shipyard:
				return 3f;
			default:
				return base.GetStationTypeBuildPriority(unitClass);
			}
		}
	}
}
