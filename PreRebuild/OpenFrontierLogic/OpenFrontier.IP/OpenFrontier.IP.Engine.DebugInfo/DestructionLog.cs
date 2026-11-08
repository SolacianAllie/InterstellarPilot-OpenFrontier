using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.DebugInfo
{
	public class DestructionLog
	{
		private List<DestructionLogItem> logItems = new List<DestructionLogItem>();

		public List<DestructionLogItem> LogItems => logItems;

		public void Log(Unit unit, Faction attackerFaction)
		{
			logItems.Add(new DestructionLogItem
			{
				UnitId = unit.UniqueId,
				UnitClass = unit.UnitClass,
				Faction = unit.Faction,
				Sector = unit.Sector,
				FactionName = ((unit.Faction != null) ? unit.Faction.name : null),
				AttackerFaction = attackerFaction,
				AttackerFactionName = ((attackerFaction != null) ? attackerFaction.name : null),
				Timestamp = EngineASX.Instance.ScenarioElapsedTime
			});
		}
	}
}
