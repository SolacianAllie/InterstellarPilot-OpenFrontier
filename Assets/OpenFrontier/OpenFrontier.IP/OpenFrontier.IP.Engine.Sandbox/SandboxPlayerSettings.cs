using UnityEngine;

namespace OpenFrontier.IP.Engine.Sandbox
{
	public class SandboxPlayerSettings : MonoBehaviour
	{
		public string GenerationNumSectorsIndexKey = "sandbox_num_sectors";

		public string DiscoverAllSectorsKey = "sandbox_discover_all_sectors";

		public string DiscoverEverythingKey = "sandbox_discover_everything";

		public string GenerationMaxWormholesKey = "sandbox_max_wormholes";

		public string GenerationSnakinessKey = "sandbox_snakiness";

		public string GenerationMinSectorSizeKey = "sandbox_min_sector_size";

		public string GenerationMaxSectorSizeKey = "sandbox_max_sector_size";

		public string GenerationAsteroidDustCloudsProbabilityKey = "sandbox_asteroid_dust_cloud_probability";

		public string FactionSeedingBanditFactionsKey = "sandbox_seed_bandits";

		public string FactionSeedingNonBanditFactionsKey = "sandbox_seed_factions";

		public string FactionSeedingFreelancersKey = "sandbox_seed_freelancers";

		public string FactionSeedingBanditPowerKey = "sandbox_bandit_power";

		public string FactionSeedingNonBanditPowerKey = "sandbox_non_bandit_power";

		public string FactionSeedingMinEmpireFactionsKey = "sandbox_min_empire_factions";

		public string FactionSeedingMaxEmpireFactionsKey = "sandbox_max_empire_factions";

		public string FactionSeedingMinEmpireExpansionKey = "sandbox_min_empire_expansion";

		public string FactionSeedingMaxEmpireExpansionKey = "sandbox_max_empire_expansion";

		public string FactionSeedingGroupEmpireFactionsKey = "sandbox_group_empire_factions";

		public string FactionSeedingCargoVolumeKey = "sandbox_cargo_volume";

		public string PermadeathKey = "sandbox_permadeath";

		public string AllowTeleportingKey = "sandbox_allow_teleporting";

		public string AsteroidRespawningEnabledKey = "sandbox_asteroid_respawning_enabled";

		public string AsteroidRespawnTimeKey = "sandbox_asteroid_respawn_time";

		public string SuperchargedBanditsEnabledKey = "sandbox_supercharged_bandits_enabled";

		public string FactionSpawningKey = "faction_spawning";

		public string RespawnOnDeathKey = "sandbox_respawn_on_death";

		public float GenerationAsteroidDustCloudsProbability = 0.75f;

		public int GenerationNumSectorsIndex;

		public int GenerationMaxWormholes = -1;

		public bool DiscoverAllSectors;

		public bool DiscoverEverything;

		public float GenerationSnakiness;

		public float GenerationMinSectorSize;

		public float GenerationMaxSectorSize;

		public bool FactionSeedingBanditFactions = true;

		public bool FactionSeedingNonBanditFactions = true;

		public bool FactionSeedingFreelancers = true;

		public float FactionSeedingBanditPower = 0.5f;

		public float FactionSeedingNonBanditPower = 0.5f;

		public int FactionSeedingMaxEmpireFactions = 10;

		public int FactionSeedingMinEmpireFactions = 5;

		public int FactionSeedingMinEmpireExpansion = 3;

		public int FactionSeedingMaxEmpireExpansion = 10;

		public bool FactionSeedingGroupEmpireFactions = true;

		public float FactionSeedingCargoVolume = 0.5f;

		public bool FactionSpawning = true;

		public bool RespawnOnDeath = true;

		public bool Permadeath;

		public bool AllowTeleporting = true;

		public bool AsteroidRespawningEnabled = true;

		public float AsteroidRespawnTime = 0.5f;

		public bool SuperchargedBanditsEnabled = true;
	}
}
