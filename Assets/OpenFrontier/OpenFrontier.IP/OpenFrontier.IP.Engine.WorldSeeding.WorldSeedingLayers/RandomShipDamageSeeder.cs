using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class RandomShipDamageSeeder : MonoBehaviour
	{
		public RandomShipDamageSeederSettings RandomShipDamageSeederSettings;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding random ship damage...", this, 1);
			}
			RandomShipDamageSeederSettings = world.Seeder.Settings.RandomShipDamageSeederSettings;
			foreach (Faction faction in world.Engine.Factions)
			{
				if (faction.FactionAI != null && !faction.IsPlayerFaction)
				{
					FactionSeedDamageToShips(world, faction);
				}
			}
		}

		private void FactionSeedDamageToShips(WorldBase world, Faction faction)
		{
			foreach (Fleet fleet in faction.Fleets)
			{
				foreach (NpcPilot npcPilot in fleet.NpcPilots)
				{
					if (npcPilot.CurrentUnit != null && Random.value <= RandomShipDamageSeederSettings.SpawnedShipProbabilityOfDamage)
					{
						float normalizedDamage = Mathf.Lerp(RandomShipDamageSeederSettings.SpawnedShipDamageMinDamage, RandomShipDamageSeederSettings.SpawnedShipDamageMaxDamage, Mathf.Pow(Random.value, RandomShipDamageSeederSettings.SpawnedShipDamagePower));
						npcPilot.CurrentUnit.Destructable.NormalizedDamage = normalizedDamage;
					}
				}
			}
		}
	}
}
