using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.AsteroidDepleter;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.AsteroidSprinkler;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.Bandits;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.CreateBars;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.FactionSpawning;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.Mining;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.RandomizedFleets;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.ScenarioOptions;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.ShipCargo;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.SkirmishUniverseGenerator;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.UnstableWormholes;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding
{
	public class WorldSeedSettings : MonoBehaviour
	{
		public AsteroidSprinklerSeederSettings AsteroidSprinklerSeederSettings;

		public AsteroidDepleterSeederSettings AsteroidDepleterSeederSettings;

		public OrderFleetsSeederSettings OrderFleetsSeederSettings;

		public SkirmishUniverseGeneratorSeederSettings SkirmishUniverseGeneratorSeederSettings;

		public MineOrderSeederSettings MineOrderSeederSettings;

		public CreateGasCloudsSeederSettings CreateGasCloudsSeederSettings;

		public WorldTraderCargoSeederSettings WorldTraderCargoSeederSettings;

		public ModdedUnitSeederSettings ModdedUnitSettings;

		public WorldDiscoverySettings DiscoverySettings;

		public WorldGeneratorSettings WorldGeneratorSettings;

		public CreateAsteroidClustersSeederSettings CreateAsteroidClustersSeederSettings;

		public CreateAsteroidsSeederSettings CreateAsteroidsSeederSettings;

		public CreateBlueprintSectorsSeederSettings CreateBlueprintSectorsSeederSettings;

		public CreatePlanetsSeederSettings CreatePlanetsSeederSettings;

		public CreateShipsSeederSettings CreateShipsSeederSettings;

		public CreateStationsSeederSettings CreateStationsSeederSettings;

		public FactionIntelSeederSettings FactionIntelSeederSettings;

		public FactionSeederSettings FactionSeederSettings;

		public RandomizedFleetSeederSettings RandomizedFleetSeederSettings;

		public RandomShipDamageSeederSettings RandomShipDamageSeederSettings;

		public SpawnPointSeederSettings SpawnPointSeederSettings;

		public UnstableWormholeSeederSettings UnstableWormholeSeederSettings;

		public CustomScenarioSeederSettings CustomScenarioSeederSettings;

		public CreateEmpireFactionsSeederSettings CreateEmpireFactionsSeederSettings;

		public AsteroidYieldSeederSettings AsteroidYieldSeederSettings;

		public AbandonedShipsSeederSettings AbandonedShipsSeederSettings;

		public BanditSprinklerSeederSettings BanditSprinklerSeederSettings;

		public CreateBarsSeederSettings CreateBarsSeederSettings;

		public FactionSpawnSettings FactionSpawnSettings;

		public ScenarioOptions ScenarioOptions;

		public TraderCargoSeederSettings ShipCargoSeederSettings;
	}
}
