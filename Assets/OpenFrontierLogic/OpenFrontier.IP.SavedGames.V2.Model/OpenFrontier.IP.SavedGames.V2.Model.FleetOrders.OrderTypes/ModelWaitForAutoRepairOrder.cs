using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelWaitForAutoRepairOrder : ModelFleetOrder
	{
		public float HullConditionThreshold { get; set; } = 0.95f;

		public float ShieldConditionThreshold { get; set; } = 0.95f;

		public float ComponentsConditionThreshold { get; set; } = 0.95f;

		public override FleetOrderType OrderType => FleetOrderType.WaitForAutoRepair;
	}
}
