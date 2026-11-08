using System;

namespace OpenFrontier.IP.Engine
{
	[Flags]
	public enum ShipNameNeutraility
	{
		None = 0,
		Neutral = 1,
		Bad = 2,
		Good = 4,
		Any = Neutral | Bad | Good
	}
}
