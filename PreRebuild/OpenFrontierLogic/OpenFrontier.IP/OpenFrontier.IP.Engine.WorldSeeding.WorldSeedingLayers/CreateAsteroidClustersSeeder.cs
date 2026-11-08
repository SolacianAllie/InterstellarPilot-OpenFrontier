using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class CreateAsteroidClustersSeeder : MonoBehaviour
	{
		private struct Point
		{
			public int X;

			public int Y;
		}

		public CreateAsteroidClustersSeederSettings CreateAsteroidClustersSeederSettings;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Creating asteroid clusters...", this, 1);
			}
			CreateAsteroidClustersSeederSettings = world.Seeder.Settings.CreateAsteroidClustersSeederSettings;
			foreach (Sector item in EngineASX.Instance.Sectors.Where((Sector e) => (e.SectorType & SectorType.Asteroid) != 0))
			{
				CreateSectorAsteroidClusters(item);
			}
		}

		public void CreateSectorAsteroidClusters(Sector sector)
		{
			AsteroidCluster asteroidClusterPrefab = GetAsteroidClusterPrefab(sector.AsteroidType);
			if (asteroidClusterPrefab != null)
			{
				CreateSectorAsteroidClusters(sector, asteroidClusterPrefab);
			}
		}

		private AsteroidCluster GetAsteroidClusterPrefab(AsteroidType asteroidType)
		{
			if (asteroidType != null)
			{
				return asteroidType.AsteroidClusterPrefabs.GetRandom();
			}
			return null;
		}

		public void CreateSectorAsteroidClusters(Sector sector, AsteroidCluster asteroidClusterPrefab)
		{
			int randomAsteroidClusterCount = GetRandomAsteroidClusterCount();
			for (int i = 0; i < randomAsteroidClusterCount; i++)
			{
				AsteroidCluster asteroidCluster = TryGenerateAsteroidCluster(sector, asteroidClusterPrefab);
				if (asteroidCluster != null && Random.value < CreateAsteroidClustersSeederSettings.ProbabilityOfGeneratingGasCloud)
				{
					// Roll gas from the SECTOR's assigned type, not the
					// cluster prefab's: variants share one cluster prefab
					// whose back-reference always points at the original
					// (gassy) type - reading it here silently defeated the
					// NoGas variants. An empty list returns null = clean.
					Unit random = sector.AsteroidType.GasCloudPrefabs.GetRandom();
					if (random != null)
					{
						CreateGasCloud(sector, asteroidCluster.Unit.Radius, asteroidCluster.transform.localPosition, random);
					}
				}
			}
		}

		public static void CreateGasCloud(Sector sector, float radius, Vector3 localPosition, Unit randomGasCloudPrefab)
		{
			Unit unit = UnityObjectHelper.InstantiateAndGetComponent(randomGasCloudPrefab, sector.transform);
			unit.transform.localPosition = localPosition;
			unit.Radius = radius;
			unit.Init(autoFindParents: false);
			UnitGasCloud component = unit.GetComponent<UnitGasCloud>();
			component.Init();
			component.ApplyRadius();
			unit.SetSector(sector, updateGameObjectParent: false);
		}

		private int GetRandomAsteroidClusterCount()
		{
			return Random.Range(CreateAsteroidClustersSeederSettings.MinAsteroidClusterCount, CreateAsteroidClustersSeederSettings.MaxAsteroidClusterCount + 1);
		}

		private AsteroidCluster TryGenerateAsteroidCluster(Sector sector, AsteroidCluster asteroidClusterPrefab)
		{
			float num = Maths.RandomFloatWithPower(CreateAsteroidClustersSeederSettings.MinAsteroidClusterRadius, CreateAsteroidClustersSeederSettings.MaxAsteroidClusterRadius, CreateAsteroidClustersSeederSettings.AsteroidClusterRadiusPower);
			int num2 = 6;
			for (int i = 0; i < num2; i++)
			{
				Vector3? vector = TryGenerateAsteroidClusterSectorPosition(sector, num);
				if (vector.HasValue)
				{
					AsteroidCluster asteroidCluster = UnityObjectHelper.InstantiateAndGetComponent(asteroidClusterPrefab);
					asteroidCluster.Unit.Radius = num;
					asteroidCluster.transform.SetParent(sector.transform, worldPositionStays: true);
					asteroidCluster.transform.localPosition = vector.Value;
					asteroidCluster.GetComponent<Unit>().Init();
					return asteroidCluster;
				}
			}
			return null;
		}

		private Vector3? TryGenerateAsteroidClusterSectorPosition(Sector sector, float clusterRadius)
		{
			float maxUnitDistanceFromOriginLowerBound = GameController.Instance.GameSettings.UniverseBoundsSettings.MaxUnitDistanceFromOriginLowerBound;
			maxUnitDistanceFromOriginLowerBound -= 200f;
			Vector3 vector = Vector3.zero;
			maxUnitDistanceFromOriginLowerBound -= clusterRadius;
			if (maxUnitDistanceFromOriginLowerBound > 0f)
			{
				vector = Geometry.RandomXZUnitVector() * Random.Range(0f, maxUnitDistanceFromOriginLowerBound);
			}
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.AsteroidCluster);
			if (unitsByType != null && unitsByType.Any())
			{
				foreach (Unit item in unitsByType)
				{
					float num = CreateAsteroidClustersSeederSettings.MinDistanceBetweenAsteroidClusters + clusterRadius + item.Radius;
					if (Vector3.Distance(item.SectorPosition, vector) < num)
					{
						return null;
					}
				}
			}
			return vector;
		}
	}
}
