using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class FactionIntelSeeder : MonoBehaviour
	{
		public List<Unit> Exclusions;

		public FactionIntelSeederSettings FactionIntelSeederSettings;

		public List<FactionType> ExcludeFactionTypes = new List<FactionType>();

		public float IntelMultiplier = 1f;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding faction intel of other units", this, 1);
			}
			FactionIntelSeederSettings = world.Seeder.Settings.FactionIntelSeederSettings;
			SeedFactionIntel(world);
		}

		private void SeedFactionIntel(WorldBase world)
		{
			foreach (Faction faction in world.Engine.Factions)
			{
				if (faction.FactionAI != null && !ExcludeFactionTypes.Contains(faction.FactionType))
				{
					SeedFactionIntel(world, faction);
				}
			}
		}

		private void SeedFactionIntel(WorldBase world, Faction faction)
		{
			faction.DiscoverOwnUnits();
			faction.FindAndPickHomeSectorIfNull();
			WorldSeederUnitDiscovery.SeedFactionUnitIntel(faction, faction.FactionAI.HomeSector, FactionIntelSeederSettings, IntelMultiplier, Exclusions);
		}
	}
}
