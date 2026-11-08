using OpenFrontier.IP.Engine.Fleets.FleetOrders;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveManualTradeOrder : ActiveTradeOrder
	{
		public ManualTradeOrder ManualTradeObjective;

		protected override void onInit()
		{
			base.onInit();
			TradeRoute = ManualTradeObjective.CustomTradeRoute;
		}

		protected override void tick(float elapsedTime)
		{
			base.tick(elapsedTime);
			if (TradeRoute == null)
			{
				if (IsCustomTradeRouteValid())
				{
					TradeRoute = ManualTradeObjective.CustomTradeRoute;
				}
				else
				{
					OnInvalid("Trade route is no longer valid");
				}
			}
		}

		private bool IsCustomTradeRouteValid()
		{
			if (ManualTradeObjective.CustomTradeRoute == null || ManualTradeObjective.CustomTradeRoute.CargoClass == null)
			{
				return false;
			}
			if (LocationIsInvalid(ManualTradeObjective.CustomTradeRoute.BuyLocation) || LocationIsInvalid(ManualTradeObjective.CustomTradeRoute.SellLocation))
			{
				return false;
			}
			return true;
		}
	}
}
