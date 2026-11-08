using System.Collections.Generic;
using OpenFrontier.IP.Common;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionAISettings : MonoBehaviour
	{
		public float SectorControlLikelihood = 0.05f;

		private HashSet<int> excludedUnitIds = new HashSet<int>();

		public FactionAIPatrolSettings PatrolSettings;

		public bool IgnoreStationCreditsReserve;

		public bool AllowForeignFactionToUseDocks = true;

		public bool RepairShips = true;

		public bool UpgradeShips = true;

		public bool BuildShips = true;

		public float RepairMinHullDamage = 0.2f;

		public float RepairMinShieldDamage = 0.2f;

		public int RepairMinCreditsBeforeRepair = 2000;

		public float PreferenceToPlaceBounty = 0.5f;

		public float LargeShipPreference = 0.25f;

		public float CloakShipPreference = -0.2f;

		public int DailyIncome;

		public bool HostileWithAll;

		public int MaxGroupUnitCount = 1;

		public int MinGroupUnitCount = 1;

		public float OffensiveStance = 0.5f;

		public float PreferenceToBuildStations = 0.5f;

		public float PreferenceToBuildTurrets = 0.5f;

		public int MaxJumpDistanceFromHomeSector = -1;

		public GenderChoice PilotGender;

		public int FixedShipCount = -1;

		public int MaxStationBuildDistanceFromHomeSector = -1;

		public float PreferenceToHaveAmmo = 0.5f;

		public HashSet<int> ExcludedUnitIds => excludedUnitIds;

		public bool PreferSingleShip => FixedShipCount == 1;

		public bool IsGang => FixedShipCount > 1;
	}
}
