using System.Collections.Generic;
using System.IO;
using OpenFrontier.IP.SavedGames.V2.Model;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Readers.Helpers
{
	public static class FleetSpawnParamsReader
	{
		public static ModelFleetSpawnParams Read(BinaryReader reader, Dictionary<int, ModelFaction> factions, Dictionary<int, ModelSector> sectors, Dictionary<int, ModelUnit> units)
		{
			ModelFleetSpawnParams modelFleetSpawnParams = new ModelFleetSpawnParams();
			modelFleetSpawnParams.TargetSector = reader.ReadSector(sectors);
			modelFleetSpawnParams.TargetPosition = reader.ReadVec3();
			modelFleetSpawnParams.TargetDockUnit = reader.ReadUnit(units);
			modelFleetSpawnParams.FleetResourceName = reader.ReadString();
			modelFleetSpawnParams.Faction = reader.ReadFaction(factions);
			modelFleetSpawnParams.ShipDesignation = reader.ReadString();
			modelFleetSpawnParams.HomeSector = reader.ReadSector(sectors);
			modelFleetSpawnParams.HomeBaseUnit = reader.ReadUnit(units);
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelFleetSpawnParamsItem item = ReadFleetSpawnParamsItem(reader);
				modelFleetSpawnParams.Items.Add(item);
			}
			return modelFleetSpawnParams;
		}

		public static ModelFleetSpawnParamsItem ReadFleetSpawnParamsItem(BinaryReader reader)
		{
			return new ModelFleetSpawnParamsItem
			{
				UnitClass = (ModelUnitClass)reader.ReadInt32(),
				PilotResourceName = reader.ReadString(),
				ShipName = reader.ReadString()
			};
		}
	}
}
