using OpenFrontier.IP.Common;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelFactionCustomSettings
	{
		public bool PreferSingleShip { get; set; }

		public bool BuildShips { get; set; } = true;

		public bool RepairShips { get; set; }

		public bool UpgradeShips { get; set; }

		public float RepairMinHullDamage { get; set; }

		public int RepairMinCreditsBeforeRepair { get; set; }

		public float PreferenceToPlaceBounty { get; set; }

		public float LargeShipPreference { get; set; }

		public float CloakShipPreference { get; set; }

		public int DailyIncome { get; set; }

		public bool HostileWithAll { get; set; }

		public int MinFleetUnitCount { get; set; }

		public int MaxFleetUnitCount { get; set; }

		public float OffensiveStance { get; set; }

		public bool AllowOtherFactionToUseDocks { get; set; }

		public float PreferenceToBuildTurrets { get; set; }

		public float PreferenceToBuildStations { get; set; }

		public bool IgnoreStationCreditsReserve { get; set; }

		public GenderChoice PilotGender { get; set; }

		public int MaxJumpDistanceFromHomeSector { get; set; } = -1;

		public int MaxStationBuildDistanceFromHomeSector { get; set; } = -1;

		public int FixedShipCount { get; set; }

		public float SectorControlLikelihood { get; set; }

		public float PreferenceToHaveAmmo { get; set; } = 0.5f;
	}
}
