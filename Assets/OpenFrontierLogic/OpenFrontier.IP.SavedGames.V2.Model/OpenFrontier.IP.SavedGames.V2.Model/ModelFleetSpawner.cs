using System.Collections.Generic;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelFleetSpawner
	{
		public string Name { get; set; }

		public Vec3 Position { get; set; }

		public Vec4 Rotation { get; set; }

		public float InitialSpawnTimeRandomness { get; set; }

		public float SpawnTimeRandomness { get; set; }

		public string ShipDesignation { get; set; }

		public string ShipName { get; set; }

		public string NamePrefix { get; set; }

		public int SpawnCounter { get; set; }

		public bool RespawnWhenNoObjectives { get; set; }

		public bool RespawnWhenNoPilots { get; set; }

		public bool AllowRespawnInActiveScene { get; set; }

		public ModelUnit FleetHomeBase { get; set; }

		public ModelSector FleetHomeSector { get; set; }

		public ModelFaction OwnerFaction { get; set; }

		public ModelSector Sector { get; set; }

		public ModelUnit SpawnDock { get; set; }

		public double NextSpawnTime { get; set; }

		public float MinTimeBeforeSpawn { get; set; }

		public float MaxTimeBeforeSpawn { get; set; }

		public int MinGroupUnitCount { get; set; }

		public int MaxGroupUnitCount { get; set; }

		public ModelFleet SpawnedFleet { get; set; }

		public List<ModelUnitClass> UnitClasses { get; set; } = new List<ModelUnitClass>();

		public List<string> PilotResourceNames { get; set; } = new List<string>();

		public string FleetResourceName { get; set; }

		public List<ModelFleetOrder> Orders { get; set; } = new List<ModelFleetOrder>();
	}
}
