using System.Collections.Generic;
using OpenFrontier.IP.Avatars;
using OpenFrontier.IP.Engine.PilotRankings;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Scenarios
{
	public class FactionSpawnerSpawnType : MonoBehaviour
	{
		public List<AvatarProfile> PersonAvatarProfiles = new List<AvatarProfile>();

		public double MinTimeBeforeSpawn;

		public bool SeedOnNewGame = true;

		public bool CanSpawnDuringGame = true;

		public bool ShouldHaveBountyBoard;

		public float SeedCountMultiplier = 1f;

		public float MinSeedCountMultiplier = 0.5f;

		public float MaxSeedCountMultiplier = 1f;

		public float MinPreferenceToPlaceBounty;

		public float MaxPreferenceToPlaceBounty = 1f;

		[FormerlySerializedAs("CreditsPower")]
		public float SeedCreditsPower = 2f;

		[FormerlySerializedAs("MinCredits")]
		public int MinSeedCredits = 5000;

		[FormerlySerializedAs("MaxCredits")]
		public int MaxSeedCredits = 30000;

		public float SpawnCreditsPower = 2f;

		public int MinSpawnCredits = 5000;

		public int MaxSpawnCredits = 30000;

		public float MaxSpendOfSeededStations = 0.5f;

		public int MinCreditsPerControllingFactionOwnedSectors;

		public int MaxCreditsPerControllingFactionOwnedSectors;

		public int MinSectorsBeforeSpawn;

		public float SpawnPriority = 1f;

		[FormerlySerializedAs("NumFactionsPerScene")]
		public float NumFactionsPerSector = 1f;

		public FactionSpawnNumFactionsCountType NumFactionsCountType;

		public FactionTypeInfo TypeInfo;

		public int MinShipsInGroup = 1;

		public int MaxShipsInGroup = 1;

		public float MinVirtue;

		public float MaxVirtue = 1f;

		public float VirtueRandomPower = 1f;

		public float MinAggresion;

		public float MaxAggresion = 1f;

		public float AggressionRandomPower = 1f;

		public string CustomUnitDesignation;

		public float LargeShipPreferenceLower = 0.25f;

		public float LargeShipPreferenceUpper = 0.5f;

		public float LargeShipPreferencePower = 1f;

		public float CloakShipPreferenceLower = 1f;

		public float CloakShipPreferenceUpper = 1f;

		public float CloakShipPreferencePower = 1f;

		public int MinFixedShipCount = -1;

		public int MaxFixedShipCount = -1;

		public bool BuildShips = true;

		public bool IsBarOwner;

		public int MaxJumpDistanceFromHomeSector = -1;

		public int MaxStationBuildDistanceFromHomeSector = -1;

		public float MinStaticIntel = 1f;

		public float MaxStaticIntel = 1f;

		public float MinNonStaticIntel = 1f;

		public float MaxNonStaticIntel = 1f;

		public float MinPreferenceToBuildStations;

		public float MaxPreferenceToBuildStations = 1f;

		public int MinDailyIncome;

		public int MaxDailyIncome;

		public float SectorControlLikelihood = 0.05f;

		public PilotRankingSystem PilotRankingSystem;

		public List<PilotRankingSystem> PilotRankingSystems;

		public float MinOffensiveStance = 0.25f;

		public float MaxOffensiveStance = 1f;

		public float OffensiveStancePower = 1f;

		public float MinNpcCombatEfficiency = 0.2f;

		public float MaxNpcCombatEfficiency = 1f;

		public float MinPreferenceToHaveAmmo = 0.25f;

		public float MaxPreferenceToHaveAmmo = 0.75f;

		public float PreferenceToHaveAmmoPower = 1f;

		public bool IsFreelancer
		{
			get
			{
				if (MinFixedShipCount == 1)
				{
					return MaxFixedShipCount == 1;
				}
				return false;
			}
		}
	}
}
