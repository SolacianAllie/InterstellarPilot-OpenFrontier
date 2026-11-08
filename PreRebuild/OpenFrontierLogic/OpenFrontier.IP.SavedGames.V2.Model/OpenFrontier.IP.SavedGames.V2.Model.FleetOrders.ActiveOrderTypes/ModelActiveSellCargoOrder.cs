using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.ActiveOrderTypes
{
	public class ModelActiveSellCargoOrder : ModelActiveFleetOrder
	{
		public double SellExpireTime { get; set; }

		public ModelCargoClass SellCargoClass { get; set; }

		public ActiveSellCargoOrderState State { get; set; }

		public ModelUnit TraderTargetUnit { get; set; }
	}
}
