using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;

namespace OpenFrontier.IP.Engine.Fleets.ActiveOrders
{
	public class CompositeTradeSearchOperation
	{
		private CompositeTradeSearchState currentState;

		private AITradeRoute tradeRouteResult;

		private bool hasFinished;

		private ActiveAutonomousTradeOrder tradeOrder;

		private Fleet fleet;

		private TradeSearchOperation tradeSearchOperation;

		private bool hadTradeableCargoUnableToSell;

		public TradeSearchOptions TradeSearchOptions { get; set; }

		public TradeSearchFleetStats FleetStats { get; private set; }

		public bool AllowSellCompatibleEquipment { get; set; } = true;

		public bool HadTradeableCargoUnableToSell => hadTradeableCargoUnableToSell;

		public AITradeRoute Result => tradeRouteResult;

		public bool HasFinished => hasFinished;

		public void Initialise(ActiveAutonomousTradeOrder order)
		{
			currentState = CompositeTradeSearchState.SearchingExistingTradeableCargo;
			hasFinished = false;
			tradeRouteResult = null;
			hadTradeableCargoUnableToSell = false;
			tradeOrder = order;
			fleet = tradeOrder.Fleet;
			if (FleetStats == null)
			{
				FleetStats = TradeSearchFleetStats.Create(fleet);
			}
			else
			{
				FleetStats.Update(fleet);
			}
			if (TradeSearchOptions == null)
			{
				TradeSearchOptions = new TradeSearchOptions();
			}
			currentState = CompositeTradeSearchState.SearchingExistingTradeableCargo;
		}

		public void ProcessUntilCompletion()
		{
			while (!hasFinished)
			{
				Process(0.1f);
			}
		}

		public void Process(float elapsedTime)
		{
			switch (currentState)
			{
			case CompositeTradeSearchState.SearchingExistingTradeableCargo:
			{
				int actualMaxJumpDist3 = tradeOrder.GetActualMaxJumpDist();
				AITradeRoute tradeRouteFromExistingTradeableCargo = ExistingCargoTradeSearchOperation.GetTradeRouteFromExistingTradeableCargo(fleet, FleetStats, actualMaxJumpDist3, out hadTradeableCargoUnableToSell);
				if (tradeRouteFromExistingTradeableCargo != null)
				{
					Finish(tradeRouteFromExistingTradeableCargo);
				}
				else if (!StartSearchForNewTradeRouteIfAble())
				{
					currentState = CompositeTradeSearchState.SearchingExistingIncompatibleEquipment;
				}
				break;
			}
			case CompositeTradeSearchState.SearchingExistingIncompatibleEquipment:
			{
				int actualMaxJumpDist = tradeOrder.GetActualMaxJumpDist();
				AITradeRoute tradeRouteFromExistingIncompatibleEquipment = ExistingCargoTradeSearchOperation.GetTradeRouteFromExistingIncompatibleEquipment(fleet, FleetStats, actualMaxJumpDist);
				if (tradeRouteFromExistingIncompatibleEquipment != null)
				{
					Finish(tradeRouteFromExistingIncompatibleEquipment);
				}
				else
				{
					currentState = CompositeTradeSearchState.SearchingExistingCompatibleEquipment;
				}
				break;
			}
			case CompositeTradeSearchState.SearchingExistingCompatibleEquipment:
			{
				int actualMaxJumpDist2 = tradeOrder.GetActualMaxJumpDist();
				AITradeRoute tradeRouteFromExistingCompatibleEquipment = ExistingCargoTradeSearchOperation.GetTradeRouteFromExistingCompatibleEquipment(fleet, FleetStats, actualMaxJumpDist2);
				if (tradeRouteFromExistingCompatibleEquipment != null)
				{
					Finish(tradeRouteFromExistingCompatibleEquipment);
				}
				else
				{
					Finish(null);
				}
				break;
			}
			case CompositeTradeSearchState.SearchingTradeRoutes:
				if (!tradeSearchOperation.HasFinished)
				{
					tradeSearchOperation.Process(elapsedTime);
				}
				if (tradeSearchOperation.HasFinished)
				{
					Finish(tradeSearchOperation.Result);
				}
				break;
			}
		}

		private bool StartSearchForNewTradeRouteIfAble()
		{
			if (CanSearchForNewTradeRoute())
			{
				currentState = CompositeTradeSearchState.SearchingTradeRoutes;
				tradeSearchOperation = new TradeSearchOperation
				{
					FleetStats = FleetStats,
					TradeSearchOptions = TradeSearchOptions
				};
				tradeSearchOperation.InitialiseAndStartSearch(tradeOrder);
				return true;
			}
			return false;
		}

		private bool CanSearchForNewTradeRoute()
		{
			return FleetStats.FleetFreeCargoSpace >= 1f;
		}

		public float GetSearchPercentageComplete()
		{
			switch (currentState)
			{
			case CompositeTradeSearchState.SearchingExistingTradeableCargo:
				return 0f;
			case CompositeTradeSearchState.SearchingTradeRoutes:
				if (tradeSearchOperation != null)
				{
					return tradeSearchOperation.GetSearchPercentageComplete();
				}
				break;
			}
			return 0f;
		}

		private void Finish(AITradeRoute tradeRoute)
		{
			tradeRouteResult = tradeRoute;
			hasFinished = true;
			tradeSearchOperation = null;
			currentState = CompositeTradeSearchState.None;
		}
	}
}
