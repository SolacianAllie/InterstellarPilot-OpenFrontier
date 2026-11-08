using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.PilotRankings
{
	public class PilotRankingSystem : MonoBehaviour
	{
		public int UniqueId = -1;

		public string Name;

		public List<PilotRankingSystemRank> Ranks;
	}
}
