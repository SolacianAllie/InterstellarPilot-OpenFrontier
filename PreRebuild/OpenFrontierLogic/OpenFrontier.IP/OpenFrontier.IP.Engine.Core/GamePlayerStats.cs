using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Core
{
	public class GamePlayerStats : MonoBehaviour
	{
		private HashSet<int> visitedSectorIds = new HashSet<int>(20);

		public long TotalBountyClaimed { get; set; }

		public int ShipsMinedToDeath { get; set; }

		public int SectorsVisitedCount => visitedSectorIds.Count;

		public IEnumerable<int> VisistedSectorIds => visitedSectorIds;

		internal void AddVisitedSector(Sector sector)
		{
			if (!visitedSectorIds.Contains(sector.UniqueId))
			{
				visitedSectorIds.Add(sector.UniqueId);
			}
		}

		public void ClearVisitedSectors()
		{
			visitedSectorIds.Clear();
		}
	}
}
