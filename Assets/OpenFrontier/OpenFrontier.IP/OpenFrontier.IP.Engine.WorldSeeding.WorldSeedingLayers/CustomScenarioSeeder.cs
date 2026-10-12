using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class CustomScenarioSeeder : MonoBehaviour
	{
		public CustomScenarioSeederSettings CustomScenarioSeederOptions;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding custom scenario...", this, 1);
			}
			CustomScenarioSeederOptions = world.Seeder.Settings.CustomScenarioSeederSettings;
			if (CustomScenarioSeederOptions.SeedTraderCargoEnabled)
			{
				UnityObjectHelper.NewGameObject<WorldTraderCargoSeeder>(transform.parent).SeedLayer(world);
				CustomScenarioSeederOptions.SeedTraderCargoEnabled = false;
			}
			if (CustomScenarioSeederOptions.SeedAbandonedShips)
			{
				CustomScenarioSeederOptions.SeedAbandonedShips = false;
			}
			if (CustomScenarioSeederOptions.SeedFactionIntel)
			{
				UnityObjectHelper.NewGameObject<FactionIntelSeeder>(transform.parent).SeedLayer(world);
				CustomScenarioSeederOptions.SeedFactionIntel = false;
			}
			if (CustomScenarioSeederOptions.SeedPassengerGroups)
			{
				UnityObjectHelper.NewGameObject<CreatePassengersSeeder>(transform.parent).SeedLayer(world);
				CustomScenarioSeederOptions.SeedPassengerGroups = false;
			}
		}
	}
}
