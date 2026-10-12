using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.FactionIntel
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class FactionIntelSeederSpecificFactions : MonoBehaviour
	{
		public List<Unit> Exclusions;

		public List<Faction> Factions;

		public float IntelMultiplier = 1f;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding faction intel of other units", this, 1);
			}
			SeedFactionIntel(world);
		}

		private void SeedFactionIntel(WorldBase world)
		{
			foreach (Faction faction in Factions)
			{
				if (faction != null && faction.FactionAI != null)
				{
					SeedFactionIntel(world, faction);
				}
			}
		}

		private void SeedFactionIntel(WorldBase world, Faction faction)
		{
			faction.DiscoverOwnUnits();
			faction.FindAndPickHomeSectorIfNull();
			WorldSeederUnitDiscovery.SeedFactionUnitIntel(faction, faction.FactionAI.HomeSector, world.Seeder.Settings.FactionIntelSeederSettings, IntelMultiplier, Exclusions);
		}
	}
}
