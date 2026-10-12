using OpenFrontier.IP.Common.Factions;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionAIStationOwner : FactionAIBase
	{
		public override FactionAIType AIType => FactionAIType.StationOwner;

		protected internal override bool AllowNewStationBuild()
		{
			if (faction.GetStationCountExcludingDefence() > 0)
			{
				return false;
			}
			if (AnyFleetBuildingStations())
			{
				return false;
			}
			return base.AllowNewStationBuild();
		}

		public override void OnNewStationConstructionStarted(Unit newStation)
		{
			base.OnNewStationConstructionStarted(newStation);
			if (!newStation.IsMinorStation())
			{
				faction.ChangeHomeSector(newStation.Sector);
			}
		}

		protected internal override void OnNewStationFullyConstructed(Unit newStation)
		{
			base.OnNewStationFullyConstructed(newStation);
			if (!newStation.IsMinorStation())
			{
				faction.ChangeHomeSector(newStation.Sector);
			}
		}
	}
}
