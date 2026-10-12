using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class FactionFullIntelSeeder : MonoBehaviour
	{
		public List<Faction> Factions = new List<Faction>();

		public bool StaticOnly;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding full intel", this, 1);
			}
			foreach (Faction faction in Factions)
			{
				if (!StaticOnly)
				{
					faction.Intel.DiscoverEverything();
				}
				else
				{
					faction.Intel.DiscoverAllSectors();
					faction.Intel.EnterAllWormholes(includeUnstable: true);
					faction.Intel.DiscoverAllUnits(staticOnly: true);
				}
				faction.OnIntelDatabaseChanged();
			}
		}
	}
}
