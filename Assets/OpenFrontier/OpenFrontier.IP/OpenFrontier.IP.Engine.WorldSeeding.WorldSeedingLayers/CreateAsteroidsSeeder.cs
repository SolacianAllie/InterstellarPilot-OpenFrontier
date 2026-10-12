using System.Collections.Generic;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class CreateAsteroidsSeeder : MonoBehaviour
	{
		public CreateAsteroidsSeederSettings CreateAsteroidsSeederSettings;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Creating asteroids...", this, 1);
			}
			CreateAsteroidsSeederSettings = world.Seeder.Settings.CreateAsteroidsSeederSettings;
			CreateAsteroids();
		}

		public void CreateAsteroids()
		{
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.AsteroidCluster);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					PopulateAsteroidsAroundCluster(item.GetComponent<AsteroidCluster>());
				}
			}
		}

		public void PopulateAsteroidsAroundCluster(AsteroidCluster asteroidCluster)
		{
			Asteroid random = asteroidCluster.AsteroidType.AsteroidUnitPrefabs.GetRandom();
			if (random != null)
			{
				PopulateAsteroidsAroundCluster(asteroidCluster, random);
				return;
			}
			Debug.LogWarningFormat(this, "Cannot create asteroids in asteroid cluster {0}. No prefabs for asteroid type defined", asteroidCluster);
		}

		public void PopulateAsteroidsAroundCluster(AsteroidCluster asteroidCluster, Asteroid asteroidPrefab)
		{
			int minDesiredAsteroids = GetMinDesiredAsteroids(asteroidCluster, CreateAsteroidsSeederSettings);
			int maxDesiredAsteroids = GetMaxDesiredAsteroids(asteroidCluster, CreateAsteroidsSeederSettings);
			int num = Random.Range(minDesiredAsteroids, maxDesiredAsteroids + 1);
			for (int i = 0; i < num; i++)
			{
				TryGenerateAsteroid(asteroidCluster, asteroidPrefab, CreateAsteroidsSeederSettings);
			}
		}

		public static int GetMinDesiredAsteroids(AsteroidCluster asteroidCluster, CreateAsteroidsSeederSettings seederSettings)
		{
			return Mathf.CeilToInt((float)seederSettings.MinAsteroidCount * (asteroidCluster.Unit.Radius / seederSettings.AsteroidCountReferenceRadius));
		}

		public static int GetMaxDesiredAsteroids(AsteroidCluster asteroidCluster, CreateAsteroidsSeederSettings seederSettings)
		{
			return Mathf.CeilToInt((float)seederSettings.MaxAsteroidCount * (asteroidCluster.Unit.Radius / seederSettings.AsteroidCountReferenceRadius));
		}

		public static Asteroid TryGenerateAsteroid(AsteroidCluster asteroidCluster, Asteroid asteroidPrefab, CreateAsteroidsSeederSettings seederSettings)
		{
			Vector3? vector = TryGenerateAsteroidSectorPosition(asteroidCluster, seederSettings);
			if (vector.HasValue)
			{
				return CreateAsteroid(asteroidPrefab, asteroidCluster.Unit.Sector, vector.Value);
			}
			return null;
		}

		public static Asteroid CreateAsteroid(Asteroid prefab, Sector sector, Vector3 sectorPosition)
		{
			Asteroid asteroid = UnityObjectHelper.InstantiateAndGetComponent(prefab);
			asteroid.transform.SetParent(sector.transform, worldPositionStays: true);
			asteroid.transform.localPosition = sectorPosition;
			asteroid.transform.localRotation = Random.rotationUniform;
			asteroid.GetComponent<Unit>().Init();
			return asteroid;
		}

		public static Vector3? TryGenerateAsteroidSectorPosition(AsteroidCluster asteroidCluster, CreateAsteroidsSeederSettings createAsteroidsSeederSettings)
		{
			float num = Random.Range(0f, asteroidCluster.Unit.Radius * 0.9f);
			Vector3 checkSectorPosition = asteroidCluster.Unit.SectorPosition + Geometry.RandomXZUnitVector() * num;
			return PhysicsNonOverlappingPositionFinder.FindSectorPositionOrNull(asteroidCluster.Unit.Sector, checkSectorPosition, createAsteroidsSeederSettings.MinDistanceBetweenAsteroids, GameController.Instance.NonOVerlappingUnitsMask);
		}
	}
}
