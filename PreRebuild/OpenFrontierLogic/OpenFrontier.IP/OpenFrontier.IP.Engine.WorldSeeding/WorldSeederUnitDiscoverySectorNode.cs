namespace OpenFrontier.IP.Engine.WorldSeeding
{
	public struct WorldSeederUnitDiscoverySectorNode
	{
		public Sector Sector;

		public int JumpDistance;

		public WorldSeederUnitDiscoverySectorNode(Sector sector, int jumpDistance)
		{
			this = default;
			Sector = sector;
			JumpDistance = jumpDistance;
		}
	}
}
