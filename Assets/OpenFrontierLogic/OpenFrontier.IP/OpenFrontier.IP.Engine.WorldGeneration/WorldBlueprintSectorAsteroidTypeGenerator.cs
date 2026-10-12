using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.WorldGeneration.Models;
using OpenFrontier.Unity.Utils;

namespace OpenFrontier.IP.Engine.WorldGeneration
{
	public class WorldBlueprintSectorAsteroidTypeGenerator
	{
		public Random Random { get; }

		public WorldBlueprintSectorAsteroidTypeGenerator(Random random)
		{
			Random = random;
		}

		public void Generate(IEnumerable<SectorBlueprint> sectors, WorldGeneratorSettings settings)
		{
			foreach (SectorBlueprint sector in sectors)
			{
				if (sector.HasAsteroids)
				{
					sector.AsteroidType = GetAsteroidType(sectors, settings);
				}
			}
		}

		private AsteroidType GetAsteroidType(IEnumerable<SectorBlueprint> sectors, WorldGeneratorSettings settings)
		{
			AsteroidType asteroidType = GetMissingAsteroidTypes(sectors, settings).GetIEnumerableRandom();
			if (asteroidType == null)
			{
				asteroidType = GetRandomAsteroidTypeForSector(sectors, settings);
			}
			return asteroidType;
		}

		private AsteroidType GetRandomAsteroidTypeForSector(IEnumerable<SectorBlueprint> sectors, WorldGeneratorSettings settings)
		{
			AsteroidType asteroidType = null;
			float num = float.MaxValue;
			int numAsteroidSectors = GetNumAsteroidSectors(sectors);
			float num2 = settings.AsteroidTypes.Sum((AsteroidType e) => e.PreferredWeighting);
			foreach (AsteroidType asteroidType2 in settings.AsteroidTypes)
			{
				float num3 = (float)GetSectorCountOfAsteroidType(sectors, asteroidType2) / (float)numAsteroidSectors + Random.NextFloat() * settings.AsteroidTypeWeightRandomness - asteroidType2.PreferredWeighting / num2;
				if (asteroidType == null || num3 < num)
				{
					num = num3;
					asteroidType = asteroidType2;
				}
			}
			return asteroidType;
		}

		private int GetNumAsteroidSectors(IEnumerable<SectorBlueprint> sectors)
		{
			return sectors.Count((SectorBlueprint e) => e.HasAsteroids);
		}

		private IEnumerable<AsteroidType> GetMissingAsteroidTypes(IEnumerable<SectorBlueprint> sectors, WorldGeneratorSettings settings)
		{
			foreach (AsteroidType asteroidType in settings.AsteroidTypes)
			{
				if (GetSectorCountOfAsteroidType(sectors, asteroidType) == 0)
				{
					yield return asteroidType;
				}
			}
		}

		private int GetSectorCountOfAsteroidType(IEnumerable<SectorBlueprint> sectors, AsteroidType asteroidType)
		{
			int num = 0;
			foreach (SectorBlueprint sector in sectors)
			{
				if (DoesSectorHaveAsteroidType(sector, asteroidType))
				{
					num++;
				}
			}
			return num;
		}

		private bool DoesSectorHaveAsteroidType(SectorBlueprint sector, AsteroidType asteroidType)
		{
			return sector.AsteroidType == asteroidType;
		}
	}
}
