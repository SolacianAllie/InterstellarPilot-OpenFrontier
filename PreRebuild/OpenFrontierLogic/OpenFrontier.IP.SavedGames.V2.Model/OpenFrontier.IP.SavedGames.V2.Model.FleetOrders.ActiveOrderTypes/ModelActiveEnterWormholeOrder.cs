using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.ActiveOrderTypes
{
	public class ModelActiveEnterWormholeOrder : ModelActiveFleetOrder
	{
		public EnterWormholeState State { get; set; }
	}
}
