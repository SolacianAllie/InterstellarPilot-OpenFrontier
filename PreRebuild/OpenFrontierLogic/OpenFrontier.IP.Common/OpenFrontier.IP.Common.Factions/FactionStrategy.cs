using System;

namespace OpenFrontier.IP.Common.Factions
{
	[Flags]
	public enum FactionStrategy
	{
		Unspecified = 0,
		Any = 0xFFFF,
		War = 1,
		Scout = 2,
		Trade = 4,
		Scavenge = 8,
		Mine = 0x10,
		BountyHunt = 0x20,
		DealEquipment = 0x40,
		PassengerTransport = 0x80,
		Explore = 0x100,
		Escort = 0x200
	}
}
