using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public struct DistressCall
	{
		public Unit SenderUnit { get; set; }

		public Faction SenderFaction { get; set; }

		public Fleet SenderGroup { get; set; }

		public float TimeOfCall { get; set; }

		public Sector Sector { get; set; }

		public Vector3 SectorPosition { get; set; }

		public Faction AttackingFaction { get; internal set; }
	}
}
