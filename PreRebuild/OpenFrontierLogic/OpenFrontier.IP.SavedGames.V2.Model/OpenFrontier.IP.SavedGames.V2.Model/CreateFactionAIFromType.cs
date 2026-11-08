using System;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.SavedGames.V2.Model.Factions;
using OpenFrontier.IP.SavedGames.V2.Model.Factions.FactionAITypes;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public static class CreateFactionAIFromType
	{
		public static ModelFactionAI Create(FactionAIType factionAIType)
		{
			return factionAIType switch
			{
				FactionAIType.PassengerTransport => new ModelFactionAIPassengerTransport(), 
				FactionAIType.Explorer => new ModelFactionAIExplorer(), 
				FactionAIType.BountyHunter => new ModelFactionAIBountyHunter(), 
				FactionAIType.EquipmentDealer => new ModelFactionAIEquipmentDealer(), 
				FactionAIType.StationOwner => new ModelFactionAIStationOwner(), 
				FactionAIType.Bandit => new ModelFactionAIBandit(), 
				FactionAIType.Scavenger => new ModelFactionAIScavenger(), 
				FactionAIType.Empire => new ModelFactionAINavy(), 
				FactionAIType.Trader => new ModelFactionAITrader(), 
				FactionAIType.Miner => new ModelFactionAIMiner(), 
				FactionAIType.Patroller => new ModelFactionAIPatroller(), 
				FactionAIType.StationBuilder => new ModelFactionAIStationBuilder(), 
				FactionAIType.Mercenary => new ModelFactionAIMercenary(), 
				FactionAIType.Generic => new ModelFactionAI(), 
				FactionAIType.Outlaw => new ModelFactionAIOutlaw(), 
				_ => throw new NotImplementedException($"Unknown faction AI type {(int)factionAIType}"), 
			};
		}
	}
}
