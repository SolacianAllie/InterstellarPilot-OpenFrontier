using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class CreatePlanetsSeeder : MonoBehaviour
	{
		public CreatePlanetsSeederSettings CreatePlanetsSeederSettings;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Creating planets", this, 1);
			}
			CreatePlanetsSeederSettings = world.Seeder.Settings.CreatePlanetsSeederSettings;
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				if ((sector.SectorType & SectorType.Planet) != 0)
				{
					CreateScenePlanets(sector);
				}
			}
		}

		private int GetCountOfPlanetClass(PlanetClass planetClass)
		{
			int num = 0;
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Planet);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					if (item.UnitClass.PlanetClass == planetClass)
					{
						num++;
					}
				}
			}
			return num;
		}

		private Unit GetBestPlanetPrefabToSpawn()
		{
			return CreatePlanetsSeederSettings.PlanetPrefabs.OrderBy((Unit e) => (float)GetCountOfPlanetClass(e.UnitClass.PlanetClass) + Random.value * 0.2f).FirstOrDefault();
		}

		public void CreateScenePlanets(Sector sector)
		{
			int num = Random.Range(CreatePlanetsSeederSettings.MinPlanets, CreatePlanetsSeederSettings.MaxPlanets + 1);
			for (int i = 0; i < num; i++)
			{
				Unit bestPlanetPrefabToSpawn = GetBestPlanetPrefabToSpawn();
				if (!(bestPlanetPrefabToSpawn != null))
				{
					continue;
				}
				Unit unit = UnityObjectHelper.InstantiateAndGetComponent(bestPlanetPrefabToSpawn);
				unit.transform.SetParent(sector.transform, worldPositionStays: true);
				unit.transform.localPosition = GetRandomPlanetLocalPosition(sector, CreatePlanetsSeederSettings);
				float x = Random.Range(CreatePlanetsSeederSettings.MinPlanetRotationX, CreatePlanetsSeederSettings.MaxPlanetRotationX) * UnityRandomHelper.RandomSign();
				unit.GetComponent<UnitPlanet>().Rotation = new Vector3(x, Random.Range(0f, 360f), 0f);
				unit.GetComponent<Unit>().Init();
				if (!(Random.value < CreatePlanetsSeederSettings.ProbabilityOfPlanetMoon))
				{
					continue;
				}
				Unit random = CreatePlanetsSeederSettings.MoonPrefabs.GetRandom();
				if (random != null)
				{
					Unit unit2 = UnityObjectHelper.InstantiateAndGetComponent(random);
					Moon component = unit2.GetComponent<Moon>();
					if (component != null)
					{
						component.OrbitingAroundUnit = unit;
						RandomizeMoon(component, CreatePlanetsSeederSettings);
						unit2.GetComponent<Unit>().Init(autoFindParents: false);
						unit2.Sector = sector;
					}
					else
					{
						Debug.LogError("Expecting moon to have moon component", unit2.gameObject);
					}
				}
			}
		}

		public static void RandomizeMoon(Moon moonComponent, CreatePlanetsSeederSettings settings)
		{
			float y = Random.value * 360f;
			float maxMoonAngleXFromPlanet = settings.MaxMoonAngleXFromPlanet;
			float x = Random.Range(0f - maxMoonAngleXFromPlanet, maxMoonAngleXFromPlanet);
			moonComponent.OffsetFromPlanet = Quaternion.Euler(new Vector3(x, y, 0f)) * Vector3.forward * Random.Range(settings.MinMoonDistanceFromPlanet, settings.MaxMoonDistanceFromPlanet);
		}

		public static Vector3 GetRandomPlanetLocalPosition(Sector sector, CreatePlanetsSeederSettings settings)
		{
			float y = Random.Range(settings.MinPlanetPositionY, settings.MaxPlanetPositionY);
			Vector3 vector = Geometry.RandomXZUnitVector();
			float num = Random.Range(settings.MinPlanetDistance, settings.MaxPlanetDistance);
			Vector3 result = vector * num;
			result.y = y;
			return result;
		}
	}
}
