using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.ActiveOrderTypes
{
	public class ModelActiveRepairFleetOrder : ModelActiveFleetOrder
	{
		public ActiveRepairFleetOrderState RepairState { get; set; }

		public ModelUnit CurrentRepairLocationUnit { get; set; }
	}
}
