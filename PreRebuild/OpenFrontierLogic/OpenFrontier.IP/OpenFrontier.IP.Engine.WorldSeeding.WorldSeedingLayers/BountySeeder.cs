using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.Bounty;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class BountySeeder : MonoBehaviour
	{
		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding faction bounties...", this, 1);
			}
			foreach (Faction faction in world.Engine.Factions)
			{
				if (faction.IsAIFactionType)
				{
					float num = faction.AISettings.PreferenceToPlaceBounty * faction.FactionTypeInfo.ProbabilityOfSeedingBountyPlacedOnOthers;
					if (faction.IsFreelancer)
					{
						num *= 0.2f;
					}
					FactionBountySeeder.Seed(faction, num);
				}
			}
		}
	}
}
