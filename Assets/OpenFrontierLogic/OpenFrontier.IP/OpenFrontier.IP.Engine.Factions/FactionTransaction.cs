using OpenFrontier.IP.Common.Factions;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionTransaction
	{
		public double GameWorldTime { get; set; }

		public FactionTransactionType TransactionType { get; set; }

		public int Value { get; set; }

		public int CurrentBalance { get; set; }

		public Unit Location { get; set; }

		public Faction OtherFaction { get; set; }

		public CargoClass RelatedCargoClass { get; set; }

		public UnitClass RelatedUnitClass { get; set; }

		public FactionTransactionTaxType TaxType { get; set; }

		public int? RelatedCount { get; set; }
	}
}
