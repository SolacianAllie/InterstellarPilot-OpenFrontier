using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.ActiveOrderTypes
{
	public class ModelActiveUniversePassengerTransportOrder : ModelActiveFleetOrder
	{
		public ModelPassengerGroup PassengerGroup { get; set; }

		public double EndBuySellTime { get; set; }

		public double LastStateChangeTime { get; set; }

		public ActiveTransportPassengerOrderState CurrentState { get; set; }
	}
}
