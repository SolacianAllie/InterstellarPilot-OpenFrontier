using System;

namespace OpenFrontier.IP.Common
{
	[Flags]
	public enum NebulaColour
	{
		NONE = 0,
		BLUE = 1,
		PINK = 2,
		PURPLE = 4,
		GREEN = 8,
		YELLOW = 0x10,
		ORANGE = 0x20,
		RED = 0x40,
		CYAN = 0x80
	}
}
