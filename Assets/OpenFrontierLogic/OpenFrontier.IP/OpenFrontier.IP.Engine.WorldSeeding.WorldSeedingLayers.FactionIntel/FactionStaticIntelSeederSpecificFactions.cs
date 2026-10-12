using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.FactionIntel
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class FactionStaticIntelSeederSpecificFactions : MonoBehaviour
	{
		public List<Unit> UnitExclusions;

		public FactionIntelSeederSettings FactionIntelSeederSettings;

		public List<Faction> SpecificFactions = new List<Faction>();

		public float IntelMultiplier = 1f;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding faction intel of sectors, planets and asteroids...", this, 1);
			}
			FactionIntelSeederSettings = world.Seeder.Settings.FactionIntelSeederSettings;
			foreach (Faction specificFaction in SpecificFactions)
			{
				if (!(specificFaction == null) && specificFaction.FactionAI != null)
				{
					if (specificFaction.FactionAI.HomeSector == null)
					{
						specificFaction.FindAndPickHomeSectorIfNull();
					}
					WorldSeederUnitDiscovery.SeedFactionStaticIntel(specificFaction, specificFaction.FactionAI.HomeSector, FactionIntelSeederSettings, IntelMultiplier, UnitExclusions);
				}
			}
		}
	}
}
