using OpenFrontier.IP.Engine.WorldGeneration;
using OpenFrontier.IP.Engine.WorldGeneration.Models;
using OpenFrontier.IP.Engine.WorldPopulation;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class CreateBlueprintSectorsSeeder : MonoBehaviour
	{
		public CreateBlueprintSectorsSeederSettings CreateBlueprintSectorsSeederSettings;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding sectors and wormholes...", this, 1);
			}
			CreateBlueprintSectorsSeederSettings = world.Seeder.Settings.CreateBlueprintSectorsSeederSettings;
			WorldBlueprint worldBlueprint = GameController.Instance.WorldBlueprintToGenerate;
			if (worldBlueprint == null)
			{
				worldBlueprint = new WorldBlueprintGenerator().Generate(world.Seeder.Settings.WorldGeneratorSettings, GameController.Instance.CustomSectorNames);
			}
			SectorCreator.CreateSectorsAndWormholes(null, worldBlueprint.SectorNodes, EngineASX.Instance, CreateBlueprintSectorsSeederSettings.SectorPrefabs, CreateBlueprintSectorsSeederSettings.JumpGatePrefab, CreateBlueprintSectorsSeederSettings.SectorMapPositionScaleFudge, CreateBlueprintSectorsSeederSettings.SectorLightingSettings);
			GameController.Instance.WorldBlueprintToGenerate = null;
			EngineASX.Instance.RefreshSectorDistancesFromUniverseCenter();
		}
	}
}
