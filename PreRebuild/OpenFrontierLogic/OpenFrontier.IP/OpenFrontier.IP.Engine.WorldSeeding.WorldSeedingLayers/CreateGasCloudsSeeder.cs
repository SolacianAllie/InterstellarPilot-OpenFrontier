using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class CreateGasCloudsSeeder : MonoBehaviour
	{
		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Creating gas clouds...", this, 1);
			}
			CreateGasCloudsSeederSettings createGasCloudsSeederSettings = world.Seeder.Settings.CreateGasCloudsSeederSettings;
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				if ((!sector.HasPlanets || createGasCloudsSeederSettings.CreateInPlanetSectors) && (!sector.HasAsteroidClusters || createGasCloudsSeederSettings.CreateInAsteroidSectors) && Random.value < createGasCloudsSeederSettings.Probability)
				{
					Unit random = createGasCloudsSeederSettings.UnitGasCloudPrefabs.GetRandom();
					if (random != null)
					{
						float radius = Random.Range(createGasCloudsSeederSettings.MinRadius, createGasCloudsSeederSettings.MaxRadius);
						float num = Random.Range(createGasCloudsSeederSettings.MinGateDistanceMultiplier, createGasCloudsSeederSettings.MaxGateDistanceMultiplier) * sector.GateDistanceMultiplier;
						Vector3 localPosition = Geometry.RandomXZUnitVector() * world.GateDistance * num;
						CreateAsteroidClustersSeeder.CreateGasCloud(sector, radius, localPosition, random);
					}
				}
			}
		}
	}
}
