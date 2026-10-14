using UnityEngine;

namespace OpenFrontier.IP.Engine.Sandbox
{
	public class SandboxSettings : MonoBehaviour
	{
		public SandboxSectorCount[] SectorCounts;

		public SandboxSectorCount DefaultSectorCount;

		/// <summary>
		/// Lowest and highest sector counts across the SectorCounts presets. The
		/// sandbox size slider is continuous between these rather than stepping
		/// through the presets, so the presets only define the bounds now.
		/// </summary>
		public int MinSectorCount
		{
			get
			{
				if (SectorCounts == null || SectorCounts.Length == 0)
				{
					return (DefaultSectorCount != null) ? DefaultSectorCount.MinCount : 0;
				}
				int min = int.MaxValue;
				foreach (SandboxSectorCount c in SectorCounts)
				{
					if (c.MinCount < min)
					{
						min = c.MinCount;
					}
				}
				return min;
			}
		}

		public int MaxSectorCount
		{
			get
			{
				if (SectorCounts == null || SectorCounts.Length == 0)
				{
					return (DefaultSectorCount != null) ? DefaultSectorCount.MaxCount : 0;
				}
				int max = int.MinValue;
				foreach (SandboxSectorCount c in SectorCounts)
				{
					if (c.MaxCount > max)
					{
						max = c.MaxCount;
					}
				}
				return max;
			}
		}

		/// <summary>Default absolute sector count, not an index into SectorCounts.</summary>
		public int DefaultNumSectors
		{
			get
			{
				return (DefaultSectorCount != null) ? DefaultSectorCount.MinCount : 0;
			}
		}

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
