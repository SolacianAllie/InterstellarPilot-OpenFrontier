using System;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.WorldBuilding.PersonUtils
{
	[Serializable]
	public class PersonBountySeedItem
	{
		public Faction FactionPlacingBounty;

		public int Bounty;

		public Faction BountyBoardFaction;

		public float ProbabilityOfUpdatingLastKnownPosition = 1f;
	}
}
