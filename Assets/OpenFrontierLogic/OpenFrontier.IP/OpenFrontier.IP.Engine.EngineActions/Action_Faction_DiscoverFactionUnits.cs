using System.Collections.Generic;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Faction_DiscoverFactionUnits : EngineAction
	{
		public Faction SourceFaction;

		public List<Faction> TargetFactions = new List<Faction>();

		public override ActionType Type => ActionType.Faction_DiscoverFactionUnits;

		public override void Execute()
		{
			base.Execute();
			if (!(SourceFaction != null))
			{
				return;
			}
			for (int i = 0; i < TargetFactions.Count; i++)
			{
				Faction faction = TargetFactions[i];
				if (!(faction != null))
				{
					continue;
				}
				for (int j = 0; j < faction.Units.Count; j++)
				{
					Unit unit = faction.Units[j];
					if (unit != null && unit.IsDiscoverableType)
					{
						SourceFaction.Intel.DiscoverUnit(unit);
					}
				}
			}
		}
	}
}
