using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Scenarios;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class FactionSeeder : MonoBehaviour
	{
		public FactionSeederSettings FactionSeederSettings;

		public List<FactionType> SpecificFactionTypes = new List<FactionType>();

		public List<FactionType> ExcludeSpecificFactionTypes = new List<FactionType>();

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding factions...", this, 1);
			}
			FactionSeederSettings = world.Seeder.Settings.FactionSeederSettings;
			foreach (FactionSpawnerSpawnType item in EngineASX.Instance.FactionSpawner.Settings.FactionTypes.Where((FactionSpawnerSpawnType e) => e.SeedOnNewGame).ToList())
			{
				if ((SpecificFactionTypes.Any() && !SpecificFactionTypes.Contains(item.TypeInfo.FactionType)) || (ExcludeSpecificFactionTypes.Any() && ExcludeSpecificFactionTypes.Contains(item.TypeInfo.FactionType)) || (item.IsFreelancer && !world.Seeder.Settings.FactionSeederSettings.SeedFreelancers) || (item.TypeInfo.FactionType != FactionType.Bandit && !item.IsFreelancer && !world.Seeder.Settings.FactionSeederSettings.SeedNonBanditFactions) || (item.TypeInfo.FactionType == FactionType.Bandit && !world.Seeder.Settings.FactionSeederSettings.SeedBanditFactions))
				{
					continue;
				}
				int requiredFactionCountOfType = FactionSpawner.GetRequiredFactionCountOfType(item);
				float seedCountMultiplier = GetSeedCountMultiplier(item);
				requiredFactionCountOfType = Mathf.CeilToInt((float)requiredFactionCountOfType * seedCountMultiplier);
				for (int num = 0; num < requiredFactionCountOfType; num++)
				{
					Sector bestHomeSectorForNewFactionType = FactionSpawner.GetBestHomeSectorForNewFactionType(item.TypeInfo, !item.IsFreelancer);
					if (bestHomeSectorForNewFactionType != null)
					{
						SeedFaction(item, bestHomeSectorForNewFactionType);
					}
				}
			}
		}

		private float GetSeedCountMultiplier(FactionSpawnerSpawnType spawnType)
		{
			if (spawnType.SeedCountMultiplier >= 0f)
			{
				return spawnType.SeedCountMultiplier;
			}
			if (spawnType.TypeInfo.FactionType == FactionType.Bandit)
			{
				return Mathf.Lerp(spawnType.MinSeedCountMultiplier, spawnType.MaxSeedCountMultiplier, FactionSeederSettings.BanditPower01);
			}
			return Random.Range(spawnType.MinSeedCountMultiplier, spawnType.MaxSeedCountMultiplier);
		}

		private Faction SeedFaction(FactionSpawnerSpawnType spawnType, Sector homeSector)
		{
			Faction faction = EngineASX.Instance.FactionSpawner.Spawn(spawnType);
			if (faction != null)
			{
				faction.WasSeeded = true;
				faction.ChangeHomeSector(homeSector);
				faction.SpawnType = spawnType;
				SetSeededFactionCredits(spawnType, faction);
			}
			return faction;
		}

		public void SetSeededFactionCredits(FactionSpawnerSpawnType spawnType, Faction faction)
		{
			SetSeededFactionCredits(spawnType, faction, FactionSeederSettings);
		}

		public static void SetSeededFactionCredits(FactionSpawnerSpawnType spawnType, Faction faction, FactionSeederSettings factionSeederSettings)
		{
			float power = ((faction.FactionType == FactionType.Bandit) ? factionSeederSettings.GetBanditCreditsPower(spawnType.SeedCreditsPower) : factionSeederSettings.GetNonBanditCreditsPower(spawnType.SeedCreditsPower));
			int minSeedCredits = spawnType.MinSeedCredits;
			int maxSeedCredits = spawnType.MaxSeedCredits;
			SetFactionCredits(faction, spawnType, power, minSeedCredits, maxSeedCredits);
		}

		public static void SetSeededFactionCredits(FactionSpawnerSpawnType spawnType, Faction faction, float creditsPower)
		{
			int minSeedCredits = spawnType.MinSeedCredits;
			int maxSeedCredits = spawnType.MaxSeedCredits;
			SetFactionCredits(faction, spawnType, creditsPower, minSeedCredits, maxSeedCredits);
		}

		public static void SetFactionCredits(Faction faction, FactionSpawnerSpawnType spawnType, float power, int min, int max)
		{
			int num = Mathf.CeilToInt(Mathf.Lerp(min, max, Mathf.Pow(Random.value, power)));
			if (spawnType.MaxCreditsPerControllingFactionOwnedSectors > 0 && faction.HomeSector != null)
			{
				Faction controllingFaction = faction.HomeSector.ControllingFaction;
				if (controllingFaction != null)
				{
					num += Maths.RandomIntWithPower(spawnType.MinCreditsPerControllingFactionOwnedSectors, spawnType.MinCreditsPerControllingFactionOwnedSectors, power) * controllingFaction.ControlledSectorCount;
				}
			}
			faction.Credits = num;
		}
	}
}
