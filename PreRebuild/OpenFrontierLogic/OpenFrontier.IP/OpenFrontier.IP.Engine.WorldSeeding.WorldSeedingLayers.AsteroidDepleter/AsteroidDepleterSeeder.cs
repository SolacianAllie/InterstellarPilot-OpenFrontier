using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.AsteroidDepleter
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class AsteroidDepleterSeeder : MonoBehaviour
	{
		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Depleting asteroids...", this, 1);
			}
			AsteroidDepleterSeederSettings asteroidDepleterSeederSettings = world.Seeder.Settings.AsteroidDepleterSeederSettings;
			DepleteAsteroids(asteroidDepleterSeederSettings);
		}

		private void DepleteAsteroids(AsteroidDepleterSeederSettings depletionSettings)
		{
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				DepleteAsteroidsInSector(sector, depletionSettings);
			}
		}

		private void DepleteAsteroidsInSector(Sector sector, AsteroidDepleterSeederSettings depletionSettings)
		{
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Asteroid);
			List<Unit> unitsByType2 = sector.GetUnitsByType(UnitType.Station);
			if (unitsByType == null || unitsByType2 == null)
			{
				return;
			}
			foreach (Unit item in unitsByType2)
			{
				if (item.UnitClass.StationPurpose == StationPurpose.Refinery)
				{
					DepleteAsteroidsAroundStation(item, depletionSettings);
				}
			}
		}

		private void DepleteAsteroidsAroundStation(Unit station, AsteroidDepleterSeederSettings depletionSettings)
		{
			int num = Physics.OverlapSphereNonAlloc(station.transform.position, depletionSettings.DepletionDistanceThresholdUpper, EngineASX.ColliderCache, GameController.Instance.AsteroidMask, QueryTriggerInteraction.Collide);
			for (int i = 0; i < num; i++)
			{
				Asteroid component = EngineASX.ColliderCache[i].GetComponent<Asteroid>();
				if (Vector3.Distance(station.transform.position, component.transform.position) < depletionSettings.DestructionDistanceThreshold)
				{
					EngineASX.Instance.DebugInfo.AsteroidDepleterSeeder_NumAsteroidsDestroyed++;
					component.GetComponent<Unit>().SafeDestroy();
				}
				else if (component.RemainingYield > 0)
				{
					float num2 = Maths.RandomFloatWithPower(depletionSettings.MinDepletionPercentage, depletionSettings.MaxDepletionPercentage, depletionSettings.DepletionPercentagePower);
					component.RemainingYield = Mathf.RoundToInt((float)component.RemainingYield * num2);
					EngineASX.Instance.DebugInfo.AsteroidDepleterSeeder_NumAsteroidsDepleted++;
				}
			}
		}
	}
}
