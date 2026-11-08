using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class AsteroidYieldSeeder : MonoBehaviour
	{
		public AsteroidYieldSeederSettings AsteroidYieldSeederSettings;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding asteroid yield...", this, 1);
			}
			AsteroidYieldSeederSettings = world.Seeder.Settings.AsteroidYieldSeederSettings;
			SeedAsteroidYields(AsteroidYieldSeederSettings);
		}

		public static void SeedAsteroidYields(AsteroidYieldSeederSettings settings)
		{
			int maxYieldIterations = settings.MaxYieldIterations;
			if (maxYieldIterations <= 0)
			{
				return;
			}
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Asteroid);
				float baseProbabilityOfYield = settings.BaseProbabilityOfYield;
				baseProbabilityOfYield += (1f - sector.AdjustedSecurityLevel01) * settings.LowSecurityProbability;
				if (sector.ControllingFaction == null)
				{
					baseProbabilityOfYield += settings.UnclaimedSectorProbability;
				}
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					for (int i = 0; i < maxYieldIterations; i++)
					{
						TryYieldForAsteroid(item.Asteroid, sector, baseProbabilityOfYield);
					}
				}
			}
		}

		private static void TryYieldForAsteroid(Asteroid asteroid, Sector sector, float prob)
		{
			if (Random.value < prob)
			{
				asteroid.YieldCargo(null, null, applyForces: false, applyEffects: false, setExpiryTime: false);
			}
		}
	}
}
