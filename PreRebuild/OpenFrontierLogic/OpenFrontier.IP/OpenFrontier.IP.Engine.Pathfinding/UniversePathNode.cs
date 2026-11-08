namespace OpenFrontier.IP.Engine.Pathfinding
{
	public struct UniversePathNode
	{
		public Wormhole Wormhole;

		public Sector Sector;

		public UniversePathNode(Sector sector, Wormhole wormhole)
		{
			this = default;
			Sector = sector;
			Wormhole = wormhole;
		}
	}
}
