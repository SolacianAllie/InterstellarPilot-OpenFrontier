using System;

namespace OpenFrontier.IP.Common
{
	[Flags]
	public enum FleetCargoCollectionPreference
	{
		Nothing = 0,
		CompatibleEquipment = 1,
		IncompatibleEquipment = 2,
		TradableCargo = 4
	}
}
