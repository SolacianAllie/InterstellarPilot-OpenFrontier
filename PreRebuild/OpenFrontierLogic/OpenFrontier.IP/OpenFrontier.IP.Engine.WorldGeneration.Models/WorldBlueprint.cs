using System.Collections.Generic;

namespace OpenFrontier.IP.Engine.WorldGeneration.Models
{
	public class WorldBlueprint
	{
		public List<SectorBlueprint> SectorNodes { get; set; } = new List<SectorBlueprint>(16);
	}
}
