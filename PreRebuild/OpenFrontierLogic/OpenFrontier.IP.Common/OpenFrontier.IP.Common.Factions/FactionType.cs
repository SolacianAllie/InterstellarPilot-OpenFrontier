namespace OpenFrontier.IP.Common.Factions
{
	public enum FactionType
	{
		None = 0,
		Trader = 1,
		Scavenger = 2,
		Miner = 4,
		BountyHunter = 8,
		StationBuilder = 0x10,
		Empire = 0x20,
		Bandit = 0x40,
		PassengerTransport = 0x80,
		Explorer = 0x100,
		EquipmentDealer = 0x200,
		Mercenary = 0x400,
		Outlaw = 0x800,
		Security = 0x1000,
		Bar = 0x2000,
		Player = 0x4000,
		Generic = 0x8000
	}
}
