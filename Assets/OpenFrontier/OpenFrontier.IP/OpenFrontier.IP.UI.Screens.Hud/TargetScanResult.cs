using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.Hud
{
	public struct TargetScanResult
	{
		public int TractorableCargoCount;

		public int HostileShipAndStationCount;

		public int NonCargoCount;

		public UnitType UnitTypeMask;

		public bool GateTargets;

		public bool StationTargets;

		public bool ShipTargets;

		public bool CargoTargets;

		public bool HostileTargets;

		public bool OwnedTargets;

		public bool HostileTargetsInCombat;

		public bool HostileTargetsInCombatTargettingPlayerFaction;
	}
}
