using System;

namespace OpenFrontier.IP.Engine
{
	[Flags]
	public enum SectorType
	{
		Unspecified = 0,
		DeepSpace = 1,
		Planet = 2,
		Asteroid = 4,
		Nebula = 8
	}
}
