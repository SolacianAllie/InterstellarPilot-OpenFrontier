using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.WorldGeneration.Models;
using OpenFrontier.IP.Engine.WorldPopulation;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier.IP.Engine.WorldGeneration
{
	public class WorldBlueprintSectorTypeGenerator
	{
		public System.Random Random { get; }

		public WorldBlueprintSectorTypeGenerator(System.Random random)
		{
			Random = random;
		}

		public void GenerateSectorTypes(IEnumerable<SectorBlueprint> sectors, WorldGeneratorSettings settings)
		{
			DeterminePlanetSectors(sectors, settings);
			DetermineAsteroidSectors(sectors, settings);
			new WorldBlueprintSectorAsteroidTypeGenerator(Random).Generate(sectors, settings);
			SetUnspecifiedSectorsToDeepSpace(sectors);
		}

		public void DetermineAsteroidSectors(IEnumerable<SectorBlueprint> sectorBlueprints, WorldGeneratorSettings settings)
		{
			float num = settings.SectorTypeSettings.SectorTypes.Sum((WorldPopulatorSectorType e) => e.Weighting);
			WorldPopulatorSectorType worldPopulatorSectorType = settings.SectorTypeSettings.SectorTypes.FirstOrDefault((WorldPopulatorSectorType e) => e.SectorType == SectorType.Asteroid);
			int a = Mathf.Max(settings.SectorTypeSettings.MinNumberAsteroidSectors, Mathf.RoundToInt(worldPopulatorSectorType.Weighting / num * (float)sectorBlueprints.Count()));
			int num2 = sectorBlueprints.Count((SectorBlueprint e) => e.HasAsteroids);
			int num3 = sectorBlueprints.Count((SectorBlueprint e) => e.HasPlanets);
			a = Mathf.Min(a, sectorBlueprints.Count() - num3);
			for (int num4 = 0; num4 < a - num2; num4++)
			{
				SectorBlueprint bestSectorForAsteroid = GetBestSectorForAsteroid(sectorBlueprints, settings.SectorTypeSettings, Random);
				if (bestSectorForAsteroid != null)
				{
					bestSectorForAsteroid.SectorType |= SectorType.Asteroid;
				}
			}
		}

		public void DeterminePlanetSectors(IEnumerable<SectorBlueprint> sectors, WorldGeneratorSettings settings)
		{
			float num = settings.SectorTypeSettings.SectorTypes.Sum((WorldPopulatorSectorType e) => e.Weighting);
			WorldPopulatorSectorType worldPopulatorSectorType = settings.SectorTypeSettings.SectorTypes.FirstOrDefault((WorldPopulatorSectorType e) => e.SectorType == SectorType.Planet);
			int num2 = Mathf.Max(settings.SectorTypeSettings.MinNumberPlanetSectors, Mathf.RoundToInt(worldPopulatorSectorType.Weighting / num * (float)sectors.Count()));
			int num3 = sectors.Count((SectorBlueprint e) => e.HasPlanets);
			for (int num4 = 0; num4 < num2 - num3; num4++)
			{
				SectorBlueprint bestSectorForPlanet = GetBestSectorForPlanet(sectors, settings.SectorTypeSettings);
				if (bestSectorForPlanet != null)
				{
					bestSectorForPlanet.SectorType |= SectorType.Planet;
				}
			}
		}

		private static void SetUnspecifiedSectorsToDeepSpace(IEnumerable<SectorBlueprint> sectors)
		{
			foreach (SectorBlueprint item in sectors.Where((SectorBlueprint e) => e.SectorType == SectorType.Unspecified))
			{
				item.SectorType = SectorType.DeepSpace;
			}
		}

		private static SectorBlueprint GetBestSectorForPlanet(IEnumerable<SectorBlueprint> sectors, WorldPopulatorSectorTypeSettings sectorTypeSettings)
		{
			return (from e in sectors
				where !e.HasPlanets
				orderby ScoreSectorForPlanet(sectors, e) descending
				select e).FirstOrDefault();
		}

		private static float ScoreSectorForPlanet(IEnumerable<SectorBlueprint> sectors, SectorBlueprint e)
		{
			float num = 0f;
			int? sectorMinDistanceFromPlanetSector = GetSectorMinDistanceFromPlanetSector(sectors, e);
			if (sectorMinDistanceFromPlanetSector.HasValue && sectorMinDistanceFromPlanetSector < 4)
			{
				num -= (float)(4 - sectorMinDistanceFromPlanetSector.Value);
			}
			if (sectorMinDistanceFromPlanetSector < 2)
			{
				num -= 10f;
			}
			return num + (float)e.Connections.Count;
		}

		private static SectorBlueprint GetBestSectorForAsteroid(IEnumerable<SectorBlueprint> sectors, WorldPopulatorSectorTypeSettings sectorTypeSettings, System.Random random)
		{
			return (from e in sectors
				where !e.HasAsteroids
				orderby ScoreSectorForAsteroid(sectors, e, random) descending
				select e).FirstOrDefault();
		}

		private static float ScoreSectorForAsteroid(IEnumerable<SectorBlueprint> sectors, SectorBlueprint e, System.Random random)
		{
			float num = 0f;
			int? sectorMinDistanceFromPlanetSector = GetSectorMinDistanceFromPlanetSector(sectors, e);
			if (sectorMinDistanceFromPlanetSector.HasValue && sectorMinDistanceFromPlanetSector < 4)
			{
				num += (float)sectorMinDistanceFromPlanetSector.Value;
			}
			return num + random.NextFloat() * 30f;
		}

		private static int? GetSectorMinDistanceFromPlanetSector(IEnumerable<SectorBlueprint> sectors, SectorBlueprint sector)
		{
			(SectorBlueprint, int?)? tuple = FindNearestSectorBlueprint.Find(sector, sectors, (SectorBlueprint sectorBlueprint) => sectorBlueprint.HasPlanets, 4);
			if (tuple.HasValue)
			{
				return tuple.Value.Item2;
			}
			return null;
		}
	}
}
