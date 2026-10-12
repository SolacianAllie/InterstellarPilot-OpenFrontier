using System;

namespace OpenFrontier.IP.Engine.Factions
{
	[Serializable]
	public class FactionAttitudeSetupSpecific : FactionAttitudeSetup
	{
		public Faction TargetFaction;

		protected override void apply(EngineASX engine, Faction sourceFaction, bool forceTwoWay)
		{
			if (sourceFaction != null && TargetFaction != null)
			{
				ApplyToFaction(engine, sourceFaction, TargetFaction, forceTwoWay);
			}
		}
	}
}
