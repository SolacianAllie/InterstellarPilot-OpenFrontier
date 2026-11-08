using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Core;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.ActiveOrders;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveAutonomousTradeOrder : ActiveTradeOrder
	{
		public AutonomousTradeOrder AutonomousTradeObjective;

		private CompositeTradeSearchOperation searchOperation = new CompositeTradeSearchOperation();

		private bool isSearching;

		private float nextSearchTradeRoutes;

		public const float MinTradeRouteSearchInterval = 4f;

		public const float MaxTradeRouteSearchInterval = 12f;

		public float NextSearchTradeRoutes
		{
			get
			{
				return nextSearchTradeRoutes;
			}
			set
			{
				nextSearchTradeRoutes = value;
			}
		}

		public CompositeTradeSearchOperation SearchOperation => searchOperation;

		public void FindImmediateTradeRoute()
		{
			searchOperation.Initialise(this);
			searchOperation.AllowSellCompatibleEquipment = !fleet.Faction.IsPlayerFaction;
			searchOperation.ProcessUntilCompletion();
			TradeRoute = searchOperation.Result;
			isSearching = false;
			if (TradeRoute == null)
			{
				OnFailedToFindTradeRoute();
			}
		}

		private void OnFailedToFindTradeRoute()
		{
			isSearching = false;
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{fleet.name} ({fleet.Faction.ShortName}) Failed to find a trade route", this, 1);
			}
		}

		protected override void OnNoTradeRoute()
		{
			base.OnNoTradeRoute();
			if (isSearching)
			{
				if (searchOperation.HasFinished)
				{
					TradeRoute = searchOperation.Result;
					if (TradeRoute == null)
					{
						OnFailedToFindTradeRoute();
					}
					if (searchOperation.HadTradeableCargoUnableToSell && !fleet.IsOwnedByPlayer)
					{
						fleet.DumpAllTradeableCargo();
					}
					isSearching = false;
				}
			}
			else if (Time.time > nextSearchTradeRoutes)
			{
				nextSearchTradeRoutes = Time.time + Random.Range(4f, 12f);
				StartSearchForBestTradeRoute();
			}
		}

		private void Update()
		{
			if (IsValid && isSearching && !searchOperation.HasFinished && fleet != null)
			{
				searchOperation.Process(Time.deltaTime);
			}
		}

		protected override string GetStatusTextInternal(Faction localFaction)
		{
			if (isSearching)
			{
				return $"Finding trade route - {GetSearchPercentageComplete():P0}";
			}
			return base.GetStatusTextInternal(localFaction);
		}

		public float GetSearchPercentageComplete()
		{
			if (isSearching)
			{
				return searchOperation.GetSearchPercentageComplete();
			}
			return 0f;
		}

		public void StartSearchForBestTradeRoute()
		{
			searchOperation.Initialise(this);
			isSearching = true;
		}

		public void ProcessTradeSearchUntilCompletion()
		{
			searchOperation.ProcessUntilCompletion();
			isSearching = false;
		}

		private AITradeRoute SearchForBestTradeRouteImmediate()
		{
			StartSearchForBestTradeRoute();
			searchOperation.ProcessUntilCompletion();
			AITradeRoute result = searchOperation.Result;
			isSearching = false;
			return result;
		}

		protected override void onFinalizeObjective()
		{
			base.onFinalizeObjective();
			TradeRoute = null;
			CurrentState = ActiveTradeOrderState.None;
		}

		public override float ScoreCargoToCollect(NpcPilot aIUnitController, Unit cargoUnit, CargoClass cargoClass, int availableQuantity, CargoOwnership cargoOwnership)
		{
			if (AutonomousTradeObjective.TradeOnlySpecificCargoTypes)
			{
				if (AutonomousTradeObjective.TradeSpecificCargoTypes.Contains(cargoClass))
				{
					return float.MaxValue;
				}
				return float.MinValue;
			}
			return base.ScoreCargoToCollect(aIUnitController, cargoUnit, cargoClass, availableQuantity, cargoOwnership);
		}

		public override void OnFleetFailedToFindPathToTarget()
		{
			TradeRoute = null;
		}
	}
}
