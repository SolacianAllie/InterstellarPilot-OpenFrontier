using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class FactionLeaderSeeder : MonoBehaviour
	{
		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding faction leaders...", this, 1);
			}
			foreach (Faction faction in world.Engine.Factions)
			{
				if (faction.LeaderPerson == null && faction.FactionAI != null)
				{
					faction.FactionAI.TryCreateOrPickLeaderIfNone();
				}
			}
		}
	}
}
