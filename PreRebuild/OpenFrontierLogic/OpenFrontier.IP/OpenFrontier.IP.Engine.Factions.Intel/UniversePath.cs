using System.Collections.Generic;
using OpenFrontier.IP.Engine.Pathfinding;

namespace OpenFrontier.IP.Engine.Factions.Intel
{
	public class UniversePath
	{
		public int Jumps;

		public List<UniversePathNode> Nodes = new List<UniversePathNode>(16);
	}
}
