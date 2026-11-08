using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.AsteroidSprinkler
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class AsteroidSprinklerSeeder : MonoBehaviour
	{
		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Sprinkling asteroids...", this, 1);
			}
			AsteroidSprinklerSeederSettings asteroidSprinklerSeederSettings = world.Seeder.Settings.AsteroidSprinklerSeederSettings;
			SprinkleAsteroids(asteroidSprinklerSeederSettings);
		}

		private void SprinkleAsteroids(AsteroidSprinklerSeederSettings sprinklerSettings)
		{
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				if (Random.value < sprinklerSettings.SectorSprinkleProbability && SprinkleAsteroidsInSector(sector, sprinklerSettings) > 0)
				{
					EngineASX.Instance.DebugInfo.AsteroidSprinklerSeeder_NumSectorsSprinkled++;
				}
			}
		}

		private int SprinkleAsteroidsInSector(Sector sector, AsteroidSprinklerSeederSettings sprinklerSettings)
		{
			int num = 0;
			int num2 = Maths.RandomIntWithPower(sprinklerSettings.MinClusterCount, sprinklerSettings.MaxClusterCount, sprinklerSettings.ClusterCountPower);
			for (int i = 0; i < num2; i++)
			{
				Vector3? vector = FindClusterPositionInSector(sector, sprinklerSettings);
				if (vector.HasValue && CreateAsteroidsAtPosition(sector, vector.Value, sprinklerSettings) > 0)
				{
					EngineASX.Instance.DebugInfo.AsteroidSprinklerSeeder_NumAsteroidClustersCreated++;
					num++;
				}
			}
			return num;
		}

		private int CreateAsteroidsAtPosition(Sector sector, Vector3 sectorPosition, AsteroidSprinklerSeederSettings sprinklerSettings)
		{
			int num = Maths.RandomIntWithPower(sprinklerSettings.MinAsteroidsInClusterCount, sprinklerSettings.MaxAsteroidsInClusterCount, sprinklerSettings.AsteroidsInClusterPower);
			int num2 = 0;
			if (num > 0)
			{
				AsteroidType random = GameController.Instance.GameSettings.DefaultWorldSeedSettings.WorldGeneratorSettings.AsteroidTypes.GetRandom();
				if (random != null)
				{
					for (int i = 0; i < num; i++)
					{
						float num3 = Mathf.Pow(Random.value, sprinklerSettings.AsteroidDistanceFromClusterPower) * sprinklerSettings.MaxAsteroidDistanceFromCluster;
						Vector3 checkSectorPosition = sectorPosition + Geometry.RandomXZUnitVector() * num3;
						Vector3? vector = PhysicsNonOverlappingPositionFinder.FindSectorPositionOrNull(sector, checkSectorPosition, GameController.Instance.GameSettings.DefaultWorldSeedSettings.CreateAsteroidsSeederSettings.MinDistanceBetweenAsteroids, GameController.Instance.StaticNonOverlappingMask);
						if (vector.HasValue)
						{
							CreateAsteroidsSeeder.CreateAsteroid(random.AsteroidUnitPrefabs.GetRandom(), sector, vector.Value);
							EngineASX.Instance.DebugInfo.AsteroidSprinklerSeeder_NumAsteroidsCreated++;
							num2++;
						}
					}
				}
			}
			return num2;
		}

		private Vector3? FindClusterPositionInSector(Sector sector, AsteroidSprinklerSeederSettings asteroidSprinklerSeederSettings)
		{
			int num = 3;
			for (int i = 0; i < num; i++)
			{
				Vector3 vector = ((!(Random.value < asteroidSprinklerSeederSettings.ProbabilityOfClusterFringePosition)) ? sector.GetRandomSectorPositionWithinBounds(0.9f) : sector.GetRandomFringeSectorPosition());
				if (!Physics.CheckSphere(sector.ToWorldPosition(vector), asteroidSprinklerSeederSettings.MinDistanceFromOtherStaticObjects, GameController.Instance.StaticNonOverlappingMask, QueryTriggerInteraction.Collide) && !Physics.CheckSphere(sector.ToWorldPosition(vector), asteroidSprinklerSeederSettings.MaxAsteroidDistanceFromCluster, GameController.Instance.AsteroidClusterMask, QueryTriggerInteraction.Collide))
				{
					return vector;
				}
			}
			return null;
		}
	}
}
