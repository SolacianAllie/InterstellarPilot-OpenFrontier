using System.Collections.Generic;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelFactionIntel
	{
		public List<ModelSector> Sectors { get; set; } = new List<ModelSector>();

		public List<ModelUnit> Units { get; set; } = new List<ModelUnit>();

		public List<ModelUnit> EnteredWormholes { get; set; } = new List<ModelUnit>();
	}
}
