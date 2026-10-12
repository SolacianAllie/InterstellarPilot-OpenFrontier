using OpenFrontier.IP.Common.Factions;

namespace OpenFrontier.IP.SavedGames.V2.Model.Factions
{
	public class ModelFactionTransaction
	{
		public FactionTransactionType TransactionType { get; set; }

		public FactionTransactionTaxType TaxType { get; set; }

		public int Value { get; set; }

		public int CurrentBalance { get; set; }

		public ModelUnit LocationUnit { get; set; }

		public ModelFaction OtherFaction { get; set; }

		public ModelCargoClass RelatedCargoClass { get; set; }

		public ModelUnitClass RelatedUnitClass { get; set; }

		public double GameWorldTime { get; set; }

		public int? RelatedCount { get; set; }
	}
}
