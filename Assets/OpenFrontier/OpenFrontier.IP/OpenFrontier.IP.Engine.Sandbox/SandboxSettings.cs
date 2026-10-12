using UnityEngine;

namespace OpenFrontier.IP.Engine.Sandbox
{
	public class SandboxSettings : MonoBehaviour
	{
		public SandboxSectorCount[] SectorCounts;

		public SandboxSectorCount DefaultSectorCount;

		public int GenerationDefaultMaxWormholes = 5;

		public bool DefaultDiscoverAllSectors;

		public bool DefaultDiscoverEverything;

		public float GenerationDefaultSnakiness;

		public float GenerationDefaultMinSectorSize = 0.5f;

		public float GenerationDefaultMaxSectorSize = 0.7f;

		public float GenerationDefaultAsteroidDustCloudsProbability = 0.75f;

		public bool DefaultFactionSeedingBandits = true;

		public bool DefaultFactionSeedingNonBandits = true;

		public bool DefaultFactionSeedingFreelancers = true;

		public float DefaultFactionSeedingBanditPower = 0.5f;

		public float DefaultFactionSeedingNonBanditPower = 0.5f;

		public float DefaultFactionSeedingCargoVolume = 0.5f;

		public bool DefaultFactionSpawning = true;

		public bool DefaultRespawnOnDeath = true;

		public bool DefaultAllowTeleporting = true;

		public bool DefaultPermadeath;

		public bool DefaultAsteroidRespawningEnabled = true;

		public float DefaultAsteroidRespawnTime = 0.5f;

		public bool DefaultSuperchargedBanditsEnabled;

		public int DefaultFactionSeedingMinEmpireExpansion = 1;

		public int DefaultFactionSeedingMaxEmpireExpansion = 10;

		public int DefaultFactionSeedingMinEmpireFactions = 3;

		public int DefaultFactionSeedingMaxEmpireFactions = 10;

		public bool DefaultFactionSeedingGroupEmpireFactions = true;
	}
}
