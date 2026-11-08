using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.DebugInfo
{
	public struct DestructionLogItem
	{
		public int UnitId { get; set; }

		public UnitClass UnitClass { get; set; }

		public Sector Sector { get; set; }

		public Faction Faction { get; set; }

		public string FactionName { get; set; }

		public Faction AttackerFaction { get; set; }

		public string AttackerFactionName { get; set; }

		public double Timestamp { get; set; }
	}
}
