using System.IO;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.SavedGames.V2.Model;
using OpenFrontier.IP.SavedGames.V2.Model.Factions;
using OpenFrontier.IP.SavedGames.V2.Model.Factions.FactionAITypes;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Writers.Helpers
{
	public static class FactionAIWriter
	{
		public static void Write(BinaryWriter writer, FactionAIType factionAIType, ModelFactionAI factionAI)
		{
			writer.Write(factionAI.NextUnitSpawnTime);
			writer.Write(factionAI.NumFleetsSpawned);
			writer.Write(factionAI.NumUnitsSpawned);
			writer.Write(factionAI.SpawnOnlyAtOwnedDocks);
			writer.Write(factionAI.LastBuiltUnitTime);
			writer.Write(factionAI.LastOrderedPatrolTime);
			writer.Write((int)factionAI.SpawnMode);
			writer.Write(factionAI.SpawnSectors.Count);
			foreach (ModelSector spawnSector in factionAI.SpawnSectors)
			{
				writer.WriteSectorId(spawnSector);
			}
			switch (factionAIType)
			{
			case FactionAIType.Trader:
			{
				ModelFactionAITrader modelFactionAITrader = (ModelFactionAITrader)factionAI;
				writer.Write(modelFactionAITrader.TradeOnlySpecificCargoTypes);
				writer.Write(modelFactionAITrader.TradeSpecificCargoTypes.Count);
				{
					foreach (ModelCargoClass tradeSpecificCargoType in modelFactionAITrader.TradeSpecificCargoTypes)
					{
						writer.Write((int)tradeSpecificCargoType);
					}
					break;
				}
			}
			case FactionAIType.Miner:
			case FactionAIType.Patroller:
			case FactionAIType.StationBuilder:
			case FactionAIType.Generic:
			case FactionAIType.Empire:
			case FactionAIType.Mercenary:
				break;
			}
		}
	}
}
