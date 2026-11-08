using OpenFrontier.IP.Common.Factions;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionAIScavenger : FactionAIBase
	{
		public override FactionAIType AIType => FactionAIType.Scavenger;

		public override float GetDamageMultiplierForIndirectFireFromNpcFaction(Faction sourceFaction)
		{
			switch (sourceFaction.FactionType)
			{
			case FactionType.Scavenger:
			case FactionType.Bandit:
			case FactionType.Outlaw:
				return 1f;
			case FactionType.BountyHunter:
				return 0.5f;
			default:
				return base.GetDamageMultiplierForIndirectFireFromNpcFaction(sourceFaction);
			}
		}

		protected override bool ShouldApplyCargoStolenDamage(Faction thievingFaction)
		{
			if (thievingFaction.FactionType == FactionType.Empire)
			{
				return false;
			}
			return base.ShouldApplyCargoStolenDamage(thievingFaction);
		}

		public override float GetStationTypeBuildPriority(UnitClass unitClass)
		{
			return unitClass.StationPurpose switch
			{
				StationPurpose.Scrapyard => 3f, 
				StationPurpose.Repair => 2f, 
				_ => base.GetStationTypeBuildPriority(unitClass), 
			};
		}
	}
}
