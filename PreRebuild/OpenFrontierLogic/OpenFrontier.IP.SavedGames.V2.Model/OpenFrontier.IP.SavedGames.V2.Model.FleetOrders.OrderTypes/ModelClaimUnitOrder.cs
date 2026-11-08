using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelClaimUnitOrder : ModelFleetOrder
	{
		public ModelUnit Unit { get; set; }

		public override FleetOrderType OrderType => FleetOrderType.ClaimUnit;
	}
}
