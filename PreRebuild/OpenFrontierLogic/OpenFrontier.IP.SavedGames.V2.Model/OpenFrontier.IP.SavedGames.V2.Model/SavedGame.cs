using System.Collections.Generic;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs;
using OpenFrontier.IP.SavedGames.V2.Model.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class SavedGame : ISavedGame
	{
		public List<ModelDitchedUnit> DitchedUnitsToBeCleanedUp = new List<ModelDitchedUnit>(10);

		public ModelHeader Header { get; set; }

		public List<ModelPerson> People { get; } = new List<ModelPerson>(1000);

		public List<ModelSector> Sectors { get; } = new List<ModelSector>(128);

		public List<ModelFaction> Factions { get; } = new List<ModelFaction>(1000);

		public List<ModelSectorPatrolPath> PatrolPaths { get; } = new List<ModelSectorPatrolPath>(20);

		public List<ModelUnit> Units { get; } = new List<ModelUnit>(2000);

		public List<ModelFleet> Fleets { get; } = new List<ModelFleet>(1000);

		public List<ModelFleetSpawner> FleetSpawners { get; } = new List<ModelFleetSpawner>(10);

		public ModelPlayer Player { get; set; }

		public List<ModelMission> Missions { get; } = new List<ModelMission>(200);

		public ModelUnit CurrentHudTarget { get; set; }

		public ModelScenarioData ScenarioData { get; set; }

		public List<ModelMoon> Moons { get; } = new List<ModelMoon>(40);

		public ModelSeedOptions SeedOptions { get; set; }

		public List<ModelTriggerGroup> TriggerGroups { get; set; } = new List<ModelTriggerGroup>();

		public ModelEngineData EngineData { get; set; }

		public List<ModelPlayerUnitFleetSettings> PlayerUnitFleetSettingItems { get; set; } = new List<ModelPlayerUnitFleetSettings>(8);

		public ModelPlayerFleetSettingsCombined PlayerDefaultFleetSettings { get; set; }

		public List<ModelTractorerDataItem> TractorerDataItems { get; set; } = new List<ModelTractorerDataItem>();
	}
}
