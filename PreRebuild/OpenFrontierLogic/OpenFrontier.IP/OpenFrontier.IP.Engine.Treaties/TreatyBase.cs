using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.Treaties
{
	public class TreatyBase
	{
		public int UniqueId { get; set; }

		public Faction FirstFaction { get; set; }

		public Faction OtherFaction { get; set; }

		public double? EndTime { get; set; }

		public TreatyType TreatyType { get; set; }

		public virtual void Init()
		{
		}

		public virtual void OnSigned()
		{
		}
	}
}
