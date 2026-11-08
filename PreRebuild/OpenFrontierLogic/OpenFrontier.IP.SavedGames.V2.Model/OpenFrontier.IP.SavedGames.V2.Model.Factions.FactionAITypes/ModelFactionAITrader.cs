using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;

namespace OpenFrontier.IP.SavedGames.V2.Model.Factions.FactionAITypes
{
	public class ModelFactionAITrader : ModelFactionAI
	{
		public bool TradeOnlySpecificCargoTypes { get; set; }

		public List<ModelCargoClass> TradeSpecificCargoTypes { get; set; } = new List<ModelCargoClass>();

		public override FactionAIType AIType => FactionAIType.Trader;
	}
}
