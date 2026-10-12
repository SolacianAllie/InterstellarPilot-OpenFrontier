using System.Collections.Generic;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelSectorPatrolPath
	{
		public int Id { get; set; }

		public ModelSector Sector { get; set; }

		public bool IsLoop { get; set; }

		public List<ModelSectorPatrolPathNode> Nodes { get; set; } = new List<ModelSectorPatrolPathNode>();
	}
}
