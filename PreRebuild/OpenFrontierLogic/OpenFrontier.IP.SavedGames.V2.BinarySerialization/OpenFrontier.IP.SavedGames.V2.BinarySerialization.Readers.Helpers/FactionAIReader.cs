using System.Collections.Generic;
using System.IO;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.SavedGames.V2.Model;
using OpenFrontier.IP.SavedGames.V2.Model.Factions;
using OpenFrontier.IP.SavedGames.V2.Model.Factions.FactionAITypes;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Readers.Helpers
{
	public static class FactionAIReader
	{
		public static ModelFactionAI Read(BinaryReader reader, FactionAIType factionAIType, Dictionary<int, ModelSector> sectors)
		{
			ModelFactionAI modelFactionAI = CreateFactionAIFromType.Create(factionAIType);
			modelFactionAI.NextUnitSpawnTime = reader.ReadDouble();
			modelFactionAI.NumFleetsSpawned = reader.ReadInt32();
			modelFactionAI.NumUnitsSpawned = reader.ReadInt32();
			modelFactionAI.SpawnOnlyAtOwnedDocks = reader.ReadBoolean();
			modelFactionAI.LastBuiltUnitTime = reader.ReadDouble();
			modelFactionAI.LastOrderedPatrolTime = reader.ReadDouble();
			modelFactionAI.SpawnMode = (FactionSpawnMode)reader.ReadInt32();
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelSector modelSector = reader.ReadSector(sectors);
				if (modelSector != null)
				{
					modelFactionAI.SpawnSectors.Add(modelSector);
				}
			}
			switch (factionAIType)
			{
			case FactionAIType.Trader:
			{
				ModelFactionAITrader modelFactionAITrader = (ModelFactionAITrader)modelFactionAI;
				modelFactionAITrader.TradeOnlySpecificCargoTypes = reader.ReadBoolean();
				int num2 = reader.ReadInt32();
				for (int j = 0; j < num2; j++)
				{
					int item = reader.ReadInt32();
					modelFactionAITrader.TradeSpecificCargoTypes.Add((ModelCargoClass)item);
				}
				break;
			}
			}
			return modelFactionAI;
		}
	}
}
