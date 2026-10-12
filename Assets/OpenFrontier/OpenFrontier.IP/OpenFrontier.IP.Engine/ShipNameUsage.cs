using System;

namespace OpenFrontier.IP.Engine
{
	[Flags]
	public enum ShipNameUsage
	{
		None = 0,
		Unspecified = 1,
		MilitaryOnly = 2,
		CivilianOnly = 4,
		Any = Unspecified | MilitaryOnly | CivilianOnly
	}
}
