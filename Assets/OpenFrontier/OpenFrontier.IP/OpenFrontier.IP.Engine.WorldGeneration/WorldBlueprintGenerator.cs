using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine.WorldGeneration.Models;

namespace OpenFrontier.IP.Engine.WorldGeneration
{
	public class WorldBlueprintGenerator
	{
		public WorldBlueprint Generate(WorldGeneratorSettings worldGeneratorSettings, List<string> customSectorNames = null)
		{
			Random random = null;
			random = ((worldGeneratorSettings.SeedValue <= -1) ? new Random() : new Random(worldGeneratorSettings.SeedValue));
			WorldBlueprint worldBlueprint = new WorldBlueprint();
			WorldBlueprintSectorGenerator worldBlueprintSectorGenerator = new WorldBlueprintSectorGenerator(random);
			worldBlueprintSectorGenerator.UseRandomSectorNames = customSectorNames == null;
			worldBlueprintSectorGenerator.CustomSectorNames = customSectorNames;
			worldBlueprint.SectorNodes = worldBlueprintSectorGenerator.Generate(worldGeneratorSettings);
			new WorldBlueprintSectorTypeGenerator(random).GenerateSectorTypes(worldBlueprint.SectorNodes, worldGeneratorSettings);
			new WorldBlueprintSectorAsteroidTypeGenerator(random).Generate(worldBlueprint.SectorNodes, worldGeneratorSettings);
			return worldBlueprint;
		}
	}
}
