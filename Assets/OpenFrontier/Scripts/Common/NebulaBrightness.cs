using System;

namespace OpenFrontier.IP.Common
{
	[Flags]
	public enum NebulaBrightness
	{
		NONE = 0,
		VERY_DARK = 1,
		DARK = 2,
		MEDIUM = 4,
		BRIGHT = 8,
		VERY_BRIGHT = 0x10
	}
}
