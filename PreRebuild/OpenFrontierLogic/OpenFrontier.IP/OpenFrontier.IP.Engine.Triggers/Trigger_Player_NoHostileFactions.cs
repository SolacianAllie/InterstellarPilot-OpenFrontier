using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class Trigger_Player_NoHostileFactions : TriggerBase
	{
		public override TriggerType Type => TriggerType.Player_NoHostileFactions;

		protected override bool evaluate(EngineASX engine)
		{
			if (engine.LocalPlayer != null && engine.LocalPlayer.Faction != null)
			{
				foreach (Faction faction in engine.Factions)
				{
					if (faction != engine.LocalPlayer.Faction && faction.IsHostileTo(engine.LocalPlayer.Faction) && faction.AnyValidShipsAndStations())
					{
						return false;
					}
				}
			}
			return true;
		}
	}
}
