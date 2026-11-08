using System;

namespace OpenFrontier.IP.Engine.PilotRankings
{
	[Serializable]
	public class PilotRankingSystemRank
	{
		public PilotRank Rank;

		public int RequiredPilotCount;

		public int MinNumber;

		public int MaxNumber = 99999;
	}
}
