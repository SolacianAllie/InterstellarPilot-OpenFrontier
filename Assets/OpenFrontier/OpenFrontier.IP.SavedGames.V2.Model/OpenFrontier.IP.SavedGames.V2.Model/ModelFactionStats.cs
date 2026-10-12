using System.Collections.Generic;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelFactionStats
	{
		public int TotalShipsClaimed { get; set; }

		public Dictionary<ModelUnitClass, int> UnitsDestroyedByClassId { get; set; } = new Dictionary<ModelUnitClass, int>();

		public Dictionary<ModelUnitClass, int> UnitLostByClassId { get; set; } = new Dictionary<ModelUnitClass, int>();

		public int ScratchcardsScratched { get; set; }

		public int HighestScratchcardWin { get; set; }
	}
}
