using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class FactionStaticIntelSeeder : MonoBehaviour
	{
		public List<Unit> UnitExclusions;

		public FactionIntelSeederSettings FactionIntelSeederSettings;

		public List<FactionType> SpecificFactionTypes = new List<FactionType>();

		[FormerlySerializedAs("ExcludeFactionTypes")]
		public List<FactionType> ExcludeSpecificFactionTypes = new List<FactionType>();

		public float IntelMultiplier = 1f;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding faction intel of sectors, planets and asteroids...", this, 1);
			}
			FactionIntelSeederSettings = world.Seeder.Settings.FactionIntelSeederSettings;
			foreach (Faction faction in world.Engine.Factions)
			{
				if (faction.FactionAI != null && (!SpecificFactionTypes.Any() || SpecificFactionTypes.Contains(faction.FactionType)) && (!ExcludeSpecificFactionTypes.Any() || !ExcludeSpecificFactionTypes.Contains(faction.FactionType)))
				{
					if (faction.FactionAI.HomeSector == null)
					{
						faction.FindAndPickHomeSectorIfNull();
					}
					WorldSeederUnitDiscovery.SeedFactionStaticIntel(faction, faction.FactionAI.HomeSector, FactionIntelSeederSettings, IntelMultiplier, UnitExclusions);
				}
			}
		}
	}
}
