using System.IO;
using OpenFrontier.IP.SavedGames.V2.Model;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Writers.Helpers
{
	public static class FleetSpawnerWriter
	{
		public static void Write(BinaryWriter writer, ModelFleetSpawner fleetSpawner)
		{
			writer.WriteStringOrEmpty(fleetSpawner.Name);
			writer.WriteVec3(fleetSpawner.Position);
			writer.WriteVec4(fleetSpawner.Rotation);
			writer.Write(fleetSpawner.InitialSpawnTimeRandomness);
			writer.Write(fleetSpawner.SpawnTimeRandomness);
			writer.WriteStringOrEmpty(fleetSpawner.ShipDesignation);
			writer.WriteStringOrEmpty(fleetSpawner.ShipName);
			writer.WriteStringOrEmpty(fleetSpawner.NamePrefix);
			writer.Write(fleetSpawner.SpawnCounter);
			writer.Write(fleetSpawner.RespawnWhenNoObjectives);
			writer.Write(fleetSpawner.RespawnWhenNoPilots);
			writer.Write(fleetSpawner.AllowRespawnInActiveScene);
			writer.WriteUnitId(fleetSpawner.FleetHomeBase);
			writer.WriteSectorId(fleetSpawner.FleetHomeSector);
			writer.WriteFactionId(fleetSpawner.OwnerFaction);
			writer.WriteSectorId(fleetSpawner.Sector);
			writer.WriteUnitId(fleetSpawner.SpawnDock);
			writer.Write(fleetSpawner.NextSpawnTime);
			writer.Write(fleetSpawner.MinTimeBeforeSpawn);
			writer.Write(fleetSpawner.MaxTimeBeforeSpawn);
			writer.Write(fleetSpawner.MinGroupUnitCount);
			writer.Write(fleetSpawner.MaxGroupUnitCount);
			writer.WriteFleetId(fleetSpawner.SpawnedFleet);
			writer.Write(fleetSpawner.UnitClasses.Count);
			foreach (ModelUnitClass unitClass in fleetSpawner.UnitClasses)
			{
				writer.Write((int)unitClass);
			}
			writer.Write(fleetSpawner.PilotResourceNames.Count);
			foreach (string pilotResourceName in fleetSpawner.PilotResourceNames)
			{
				writer.WriteStringOrEmpty(pilotResourceName);
			}
			writer.WriteStringOrEmpty(fleetSpawner.FleetResourceName);
			writer.Write(fleetSpawner.Orders.Count);
			foreach (ModelFleetOrder order in fleetSpawner.Orders)
			{
				FleetOrdersWriter.WriteOrder(writer, order);
			}
		}
	}
}
