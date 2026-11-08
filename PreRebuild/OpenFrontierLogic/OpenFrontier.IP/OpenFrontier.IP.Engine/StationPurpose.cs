namespace OpenFrontier.IP.Engine
{
	public enum StationPurpose
	{
		None = 0,
		Refinery = 1,
		TradeStation = 2,
		Factory = 4,
		Shipyard = 8,
		Equipment = 0x10,
		Repair = 0x20,
		Defence = 0x40,
		RESERVED = 0x80,
		Satellite = 0x100,
		Bar = 0x200,
		SectorControl = 0x400,
		Outpost = 0x800,
		Scrapyard = 0x1000
	}
}
