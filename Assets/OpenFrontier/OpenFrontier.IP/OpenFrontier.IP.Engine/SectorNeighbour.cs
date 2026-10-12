namespace OpenFrontier.IP.Engine
{
	public struct SectorNeighbour
	{
		public Sector Sector;

		public Wormhole ConnectingGate;

		public bool IsStableConnection => !ConnectingGate.IsUnstable;

		public SectorNeighbour(Sector scene, Wormhole gate)
		{
			Sector = scene;
			ConnectingGate = gate;
		}
	}
}
