using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.ActiveOrderTypes
{
	public class ModelActiveRearmFleetOrder : ModelActiveFleetOrder
	{
		public ActiveRearmFleetOrderState State { get; set; }

		public ModelUnit CurrentRearmLocationUnit { get; set; }
	}
}
