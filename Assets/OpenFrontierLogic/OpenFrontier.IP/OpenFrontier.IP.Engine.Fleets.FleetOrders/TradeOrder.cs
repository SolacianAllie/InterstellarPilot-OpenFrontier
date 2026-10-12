using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class TradeOrder : FleetOrder
	{
		public float MinBuyCargoPercentage;

		public int MinBuyQuantity = 1;

		public override FleetOrderType OrderType => FleetOrderType.Trade;

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveTradeOrder activeTradeOrder = gameObject.AddComponent<ActiveTradeOrder>();
			activeTradeOrder.TradeObjective = this;
			return activeTradeOrder;
		}

		public override string GetDescription()
		{
			return "Trade";
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
