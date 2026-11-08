using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.FactionIntel
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class AllFactionsDiscoverEverythingSeeder : MonoBehaviour
	{
		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding faction full intel", this, 1);
			}
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (faction.Intel != null)
				{
					faction.Intel.DiscoverEverything();
					faction.InvalidateTraderTargets();
				}
			}
		}
	}
}
