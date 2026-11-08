using System;

namespace OpenFrontier.IP.Engine
{
	[Flags]
	public enum UnitType
	{
		None = 0,
		Any = 0xFFFF,
		Ship = 1,
		Station = 2,
		Cargo = 4,
		Wormhole = 8,
		Asteroid = 0x10,
		Debris = 0x20,
		NavBuoy = 0x40,
		Projectile = 0x80,
		Planet = 0x100,
		AsteroidCluster = 0x200,
		GasCloud = 0x400,
		Waypoint = 0x800
	}
}
