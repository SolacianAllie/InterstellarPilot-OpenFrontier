using OpenFrontier.Unity.Utils;

namespace OpenFrontier.IP.Engine.Factions
{
	public struct FactionAIShipBuildItem : IWeighted
	{
		public UnitShipTrader ShipTrader { get; set; }

		public Unit BuildLocation { get; set; }

		public UnitClass UnitClass { get; set; }

		public float Weight { get; set; }
	}
}
