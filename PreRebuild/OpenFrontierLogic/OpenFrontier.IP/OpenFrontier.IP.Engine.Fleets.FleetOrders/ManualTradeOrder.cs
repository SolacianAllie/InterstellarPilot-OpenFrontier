using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class ManualTradeOrder : TradeOrder
	{
		public AITradeRoute CustomTradeRoute;

		public override FleetOrderType OrderType => FleetOrderType.ManualTrade;

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveManualTradeOrder activeManualTradeOrder = gameObject.AddComponent<ActiveManualTradeOrder>();
			activeManualTradeOrder.ManualTradeObjective = this;
			activeManualTradeOrder.TradeObjective = this;
			return activeManualTradeOrder;
		}

		public override string GetDescription()
		{
			return "Manual trade";
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
