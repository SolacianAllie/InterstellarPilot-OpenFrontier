using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.TraderHeatmapSeed
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class TraderHeatmapSeeder : MonoBehaviour
	{
		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding trader heatmap...", this, 1);
			}
			SeedHeatmap();
		}

		public static void SeedHeatmap()
		{
			List<Fleet> list = new List<Fleet>(1000);
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (!faction.IsBanditOrOutlaw())
				{
					list.AddRange(faction.Fleets);
				}
			}
			list.Shuffle();
			foreach (Fleet item in list)
			{
				if (item.FleetStrategy == FactionStrategy.Mine || item.FleetStrategy == FactionStrategy.Scavenge || item.FleetStrategy == FactionStrategy.Trade)
				{
					item.InvalidateCargoUsageStats();
					EngineASX.Instance.TraderHeatmapModule.ConsiderFleet(item);
				}
			}
		}
	}
}
