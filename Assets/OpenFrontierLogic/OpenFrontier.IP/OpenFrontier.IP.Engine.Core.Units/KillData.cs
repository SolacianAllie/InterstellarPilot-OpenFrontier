using System.Collections.Generic;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.Core.Units
{
	public class KillData
	{
		public Faction KillerFaction { get; set; }

		public Unit KillerUnit { get; set; }

		public List<KilledPerson> KilledPeople { get; set; } = new List<KilledPerson>();

		public List<KilledUnit> KilledUnits { get; set; } = new List<KilledUnit>();

		public DamageDirectType DamageDirectType { get; set; }
	}
}
