using System.Collections.Generic;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AsteroidRespawning
{
	public class AsteroidRespawner
	{
		private float nextCheckSectorTime;

		private int lastCheckSectorIndex = -1;

		public void Update()
		{
			if (!EngineASX.Instance.World.ScenarioOptions.AsteroidRespawningEnabled || Time.time < nextCheckSectorTime)
			{
				return;
			}
			double num = Maths.Lerp(GameController.Instance.GameSettings.AsteroidRespawnSettings.MinTimeBeforeAsteroidRespawnLower, GameController.Instance.GameSettings.AsteroidRespawnSettings.MinTimeBeforeAsteroidRespawnUpper, EngineASX.Instance.World.ScenarioOptions.AsteroidRespawnTime);
			if (!(Time.timeAsDouble < num) && EngineASX.Instance.Sectors.Count != 0)
			{
				nextCheckSectorTime = Time.time + 15f;
				lastCheckSectorIndex++;
				if (lastCheckSectorIndex >= EngineASX.Instance.Sectors.Count)
				{
					lastCheckSectorIndex = 0;
				}
				Sector sector = EngineASX.Instance.Sectors[lastCheckSectorIndex];
				if (EngineASX.Instance.ScenarioElapsedTime > sector.AsteroidRespawnCooldownTime)
				{
					RespawnInSector(sector);
				}
			}
		}

		private void RespawnInSector(Sector sector)
		{
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.AsteroidCluster);
			if (unitsByType != null && unitsByType.Count > 0)
			{
				List<Unit> unitsByType2 = sector.GetUnitsByType(UnitType.Asteroid);
				if (unitsByType2 == null || unitsByType2.Count <= 200)
				{
					Unit random = unitsByType.GetRandom();
					RespawnInAsteroidCluster(random);
				}
			}
		}

		private void RespawnInAsteroidCluster(Unit asteroidCluster)
		{
			if (AsteroidClusterNeedsNewAsteroids(asteroidCluster))
			{
				SpawnAsteroidInAsteroidCluster(asteroidCluster);
			}
		}

		private void SpawnAsteroidInAsteroidCluster(Unit asteroidClusterUnit)
		{
			if (!(asteroidClusterUnit.Sector == null))
			{
				CreateAsteroidsSeederSettings createAsteroidsSeederSettings = GameController.Instance.GameSettings.DefaultWorldSeedSettings.CreateAsteroidsSeederSettings;
				AsteroidCluster component = asteroidClusterUnit.GetComponent<AsteroidCluster>();
				if (CreateAsteroidsSeeder.TryGenerateAsteroid(component, component.AsteroidType.AsteroidUnitPrefabs.GetRandom(), createAsteroidsSeederSettings) != null)
				{
					asteroidClusterUnit.Sector.AsteroidRespawnCooldownTime = EngineASX.Instance.ScenarioElapsedTime + (double)Mathf.Lerp(GameController.Instance.GameSettings.AsteroidRespawnSettings.SectorRespawnCooldownTimeLower, GameController.Instance.GameSettings.AsteroidRespawnSettings.SectorRespawnCooldownTimeUpper, EngineASX.Instance.World.ScenarioOptions.AsteroidRespawnTime);
					EngineASX.Instance.DebugInfo.NumAsteroidsRespawned++;
				}
			}
		}

		private bool AsteroidClusterNeedsNewAsteroids(Unit asteroidCluster)
		{
			int num = Physics.OverlapSphereNonAlloc(asteroidCluster.transform.position, asteroidCluster.Radius * 0.8f, EngineASX.ColliderCache, GameController.Instance.AsteroidMask);
			int num2 = CreateAsteroidsSeeder.GetMinDesiredAsteroids(seederSettings: GameController.Instance.GameSettings.DefaultWorldSeedSettings.CreateAsteroidsSeederSettings, asteroidCluster: asteroidCluster.GetComponent<AsteroidCluster>());
			return num < num2;
		}
	}
}
