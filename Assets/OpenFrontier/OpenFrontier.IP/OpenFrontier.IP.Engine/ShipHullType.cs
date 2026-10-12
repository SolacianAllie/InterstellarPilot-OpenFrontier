using System;

namespace OpenFrontier.IP.Engine
{
	[Flags]
	public enum ShipHullType
	{
		None = 0,
		Any = 0xFFFF,
		Scout = 1,
		Fighter = 2,
		Frigate = 4,
		Destroyer = 8,
		Cruiser = 0x10,
		Battleship = 0x20,
		Station = 0x40,
		Other = 0x80
	}
}
