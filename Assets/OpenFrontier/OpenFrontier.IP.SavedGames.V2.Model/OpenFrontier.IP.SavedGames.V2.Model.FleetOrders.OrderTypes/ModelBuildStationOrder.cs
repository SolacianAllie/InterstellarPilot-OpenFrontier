using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelBuildStationOrder : ModelFleetOrder
	{
		public ModelUnitClass UnitClass { get; set; }

		public ModelSector Sector { get; set; }

		public Vec3 SectorPosition { get; set; }

		public InsufficientCreditsMode InsufficientCreditsMode { get; set; } = InsufficientCreditsMode.Abort;

		public override FleetOrderType OrderType => FleetOrderType.BuildStation;
	}
}
