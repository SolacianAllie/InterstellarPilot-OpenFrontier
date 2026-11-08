using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class OrderFleetsSeeder : MonoBehaviour
	{
		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Ordering fleets...", this, 1);
			}
			foreach (Faction faction in world.Engine.Factions)
			{
				if (faction.FactionAI != null && !faction.IsPlayerFaction)
				{
					OrderFleets(world, faction, faction.Fleets.Where((Fleet e) => e.FleetStrategy != FactionStrategy.Escort));
					OrderFleets(world, faction, faction.Fleets.Where((Fleet e) => e.FleetStrategy == FactionStrategy.Escort));
				}
			}
		}

		private static void OrderFleets(WorldBase world, Faction faction, IEnumerable<Fleet> fleets)
		{
			foreach (Fleet fleet in fleets)
			{
				if (!world.FleetsBeforeSeed.Contains(fleet.UniqueId) && Random.value < world.Seeder.Settings.OrderFleetsSeederSettings.ProbabilityOfOrderingOnSeed && fleet.IdleAndNoObjectives && FactionAIBase.CanOrderFleet(fleet))
				{
					faction.FactionAI.AssignOrdersToIdleFleet(fleet);
				}
			}
		}
	}
}
