using System.Collections.Generic;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelPlayerStats
	{
		public List<ModelSector> SectorsVisited { get; set; } = new List<ModelSector>(64);

		public long TotalBountyClaimed { get; set; }

		public int ShipsMinedToDeath { get; set; }
	}
}
