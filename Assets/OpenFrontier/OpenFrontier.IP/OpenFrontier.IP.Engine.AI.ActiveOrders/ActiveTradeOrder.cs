using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Core;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveTradeOrder : ActiveFleetOrder
	{
		private ActiveTradeOrderState currentState;

		private double endBuySellTime;

		private double lastStateChangeTime;

		public TradeOrder TradeObjective;

		private AITradeRoute tradeRoute;

		public override bool IsValid => true;

		public ActiveTradeOrderState CurrentState
		{
			get
			{
				return currentState;
			}
			set
			{
				if (currentState == value)
				{
					return;
				}
				ActiveTradeOrderState activeTradeOrderState = currentState;
				if ((uint)(activeTradeOrderState - 1) <= 1u || activeTradeOrderState == ActiveTradeOrderState.PostBuy)
				{
					if ((uint)(value - 1) > 1u && value != ActiveTradeOrderState.PostBuy)
					{
						DeregisterFromTradeNetwork();
					}
				}
				else if ((uint)(value - 1) <= 1u || value == ActiveTradeOrderState.PostBuy)
				{
					RegisterOnTradeNetwork();
				}
				lastStateChangeTime = Engine.ScenarioElapsedTime;
				currentState = value;
				OnStateChanged();
			}
		}

		public AITradeRoute TradeRoute
		{
			get
			{
				return tradeRoute;
			}
			set
			{
				if (tradeRoute == value)
				{
					return;
				}
				DeregisterFromTradeNetwork();
				tradeRoute = value;
				if (tradeRoute != null)
				{
					if (tradeRoute.BuyLocation != null)
					{
						CurrentState = ActiveTradeOrderState.GoBuy;
					}
					else if (tradeRoute.SellLocation != null)
					{
						CurrentState = ActiveTradeOrderState.GoSell;
					}
				}
			}
		}

		public double LastStateChangeTime
		{
			get
			{
				return lastStateChangeTime;
			}
			set
			{
				lastStateChangeTime = value;
			}
		}

		public double EndBuySellTime
		{
			get
			{
				return endBuySellTime;
			}
			set
			{
				endBuySellTime = value;
			}
		}

		private void DeregisterFromTradeNetwork()
		{
			if (fleet != null && fleet.Faction != null && fleet.Faction.TradeNetwork != null)
			{
				fleet.Faction.TradeNetwork.RemoveFleetBuyingCargo(fleet);
			}
		}

		public static void ExchangeCargo(EngineASX engine, Unit localUnit, Unit dockUnit, CargoClass cargoClass, int units)
		{
			if (units != 0)
			{
				localUnit.CargoBayComponent.AddToCargoIfFits(cargoClass, -units);
				dockUnit.CargoBayComponent.AddToCargoIfFits(cargoClass, units);
				EngineASX.Instance.OnFactionTraded(localUnit.Faction, dockUnit.Faction, cargoClass, Mathf.Abs(units));
			}
		}

		protected override string GetStatusTextInternal(Faction localFaction)
		{
			switch (currentState)
			{
			case ActiveTradeOrderState.None:
				return "Finding trade route";
			case ActiveTradeOrderState.GoBuy:
			case ActiveTradeOrderState.Buying:
				return $"Buy {tradeRoute.CargoClass.GetShortNameElseLong()} at {UnitNamer.GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(localFaction, fleet.Sector, tradeRoute.BuyLocation.Unit, colourByHostility: true, shortName: true)}";
			case ActiveTradeOrderState.PostSell:
			{
				string text = "Cargo";
				if (tradeRoute != null && tradeRoute.CargoClass != null)
				{
					text = tradeRoute.CargoClass.GetShortNameElseLong();
				}
				return "Transfer " + text + " to dock";
			}
			case ActiveTradeOrderState.PostBuy:
			{
				string text2 = "Cargo";
				if (tradeRoute != null && tradeRoute.CargoClass != null)
				{
					text2 = tradeRoute.CargoClass.GetShortNameElseLong();
				}
				return "Transfer " + text2 + " to ship";
			}
			case ActiveTradeOrderState.GoSell:
			case ActiveTradeOrderState.Selling:
				if (tradeRoute == null || tradeRoute.SellLocation == null || tradeRoute.SellLocation.Unit == null)
				{
					return "Sell";
				}
				return $"Sell {tradeRoute.CargoClass.GetShortNameElseLong()} at {UnitNamer.GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(localFaction, fleet.Sector, tradeRoute.SellLocation.Unit, colourByHostility: true, shortName: true)}";
			default:
				return base.GetStatusTextInternal(localFaction);
			}
		}

		public int? GetBuyPrice(CargoTrader unitTrader)
		{
			int pricePerUnit = 0;
			if (unitTrader.GetSellPrice(tradeRoute.CargoClass, fleet.Faction, 1, out pricePerUnit))
			{
				return pricePerUnit;
			}
			return null;
		}

		public int GetAffordableCargoCount(CargoTrader unitTrader)
		{
			if (fleet.Faction.IsPlayerFaction || fleet.Engine.World.AITradePurchaseRequiresCredits)
			{
				if (fleet.Faction == unitTrader.Unit.Faction)
				{
					return int.MaxValue;
				}
				int? buyPrice = GetBuyPrice(unitTrader);
				if (buyPrice.HasValue)
				{
					if (buyPrice > 0)
					{
						return Credits / buyPrice.Value;
					}
					return int.MaxValue;
				}
				return 0;
			}
			return int.MaxValue;
		}

		public int GetMaxPurchasableCargo(CargoClass cargoClass, CargoTrader unitTrader)
		{
			int cargoCountOf = unitTrader.GetCargoCountOf(tradeRoute.CargoClass);
			return Mathf.Min(cargoCountOf, fleet.GetTotalCapacityForCargoCount(cargoClass), GetAffordableCargoCount(unitTrader));
		}

		public void BuyMaxCargo()
		{
			CargoTrader buyLocation = tradeRoute.BuyLocation;
			if (!(buyLocation != null))
			{
				return;
			}
			Unit unit = buyLocation.Unit;
			int num = GetMaxPurchasableCargo(tradeRoute.CargoClass, buyLocation);
			if (num <= 0)
			{
				return;
			}
			int? buyPrice = GetBuyPrice(buyLocation);
			if (!buyPrice.HasValue)
			{
				return;
			}
			int num2 = -buyPrice.Value * num;
			if (fleet.Faction != buyLocation.Unit.Faction)
			{
				fleet.Faction.RegisterNormalTradeWithFaction(buyLocation.Unit, buyLocation.Unit.Faction, num2, FactionTransactionType.Trade, tradeRoute.CargoClass, null, num, CreditsSource);
			}
			int num3 = 0;
			while (num > 0 && num3 < fleet.NpcPilots.Count)
			{
				Unit currentUnit = fleet.NpcPilots[num3].CurrentUnit;
				if (currentUnit != null)
				{
					int num4 = Mathf.Min(num, currentUnit.CargoBayComponent.GetFreeSpaceFor(tradeRoute.CargoClass));
					ExchangeCargo(Engine, currentUnit, unit, tradeRoute.CargoClass, -num4);
					num -= num4;
				}
				num3++;
			}
			if (fleet.LeaderUnit != null)
			{
				fleet.LeaderUnit.TryPlayCargoDoorAudio();
			}
			fleet.InvalidateCargoUsageStats();
		}

		public bool SellAllCargoIfAble()
		{
			CargoClass cargoClass = tradeRoute.CargoClass;
			Unit dockUnit = fleet.Leader.CurrentUnit.Components.DockUnit;
			Faction faction = fleet.Faction;
			int cargoCount = fleet.GetCargoCount(cargoClass);
			if (CanSellCargo(faction, cargoClass, cargoCount, tradeRoute.BuyPriceMultiplier, dockUnit, out var maxSellable))
			{
				foreach (NpcPilot npcPilot in fleet.NpcPilots)
				{
					Unit currentUnit = npcPilot.CurrentUnit;
					if (currentUnit != null)
					{
						int cargoCountOf = currentUnit.GetCargoCountOf(cargoClass);
						int num = Mathf.Min(maxSellable, cargoCountOf);
						if (num > 0)
						{
							maxSellable -= num;
							SellCargoOnUnit(faction, currentUnit, cargoClass, num, dockUnit);
						}
					}
				}
				fleet.InvalidateCargoUsageStats();
				return true;
			}
			return false;
		}

		private void SellCargoOnUnit(Faction faction, Unit localUnit, CargoClass cargoClass, int quantity, Unit dockUnit)
		{
			SellCargo(faction, localUnit, cargoClass, quantity, dockUnit, CreditsSource);
		}

		public static void SellCargo(Faction ourFaction, Unit localUnit, CargoClass cargoClass, int quantity, Unit dockUnit, ICreditsSource creditsSource)
		{
			CargoTrader cargoTrader = dockUnit.Components.CargoTrader;
			if (cargoTrader != null)
			{
				int pricePerUnit = 0;
				cargoTrader.GetBuyPrice(cargoClass, ourFaction, quantity, out pricePerUnit);
				int num = pricePerUnit * quantity;
				if (ourFaction != cargoTrader.Unit.Faction)
				{
					ourFaction.RegisterNormalTradeWithFaction(cargoTrader.Unit, cargoTrader.Unit.Faction, num, FactionTransactionType.Trade, cargoClass, null, quantity, creditsSource);
				}
				ExchangeCargo(ourFaction.Engine, localUnit, dockUnit, cargoClass, quantity);
			}
			else
			{
				Debug.LogError("AIActiveTradeObjective: No UnitTrader", localUnit);
			}
		}

		public static bool CanSellCargo(Faction ourFaction, CargoClass cargoClass, int quantity, float buyPriceMultiplier, Unit dockUnit, out int maxSellable)
		{
			maxSellable = 0;
			if (dockUnit.Faction != null && (ourFaction.Engine.World.AITradePurchaseRequiresCredits || dockUnit.Faction.IsPlayerFaction))
			{
				maxSellable = dockUnit.Components.CargoTrader.GetMaxBuyable(quantity, ourFaction, cargoClass);
				if (maxSellable > 0)
				{
					int pricePerUnit = 0;
					if (dockUnit.Components.CargoTrader.GetBuyPrice(cargoClass, ourFaction, maxSellable, out pricePerUnit) && (float)pricePerUnit > buyPriceMultiplier)
					{
						return dockUnit.CargoBayComponent.CanChangeCargo(cargoClass, quantity);
					}
				}
				return false;
			}
			return true;
		}

		public bool ValidateTradeRouteAndClearIfInvalid()
		{
			if (!ValidateTradeRoute(checkProfitability: false, updateNewPrices: false))
			{
				CurrentState = ActiveTradeOrderState.None;
				return false;
			}
			return true;
		}

		private bool IsTradeRouteProfitable(out float newBuyPriceMultiplier, out float newSellPriceMultiplier)
		{
			newBuyPriceMultiplier = 0f;
			newSellPriceMultiplier = 0f;
			int countOf = tradeRoute.BuyLocation.UnitComponents.CargoBayComponent.GetCountOf(tradeRoute.CargoClass);
			if (QuantityFulfilsMinOrder(tradeRoute.CargoClass, countOf, fleet.GetCachedTotalCargoCapacity()) && tradeRoute.BuyLocation.GetSellPriceMultiplier(tradeRoute.CargoClass, fleet.Faction, TradeObjective.MinBuyQuantity, out newBuyPriceMultiplier) && tradeRoute.SellLocation.GetBuyPriceMultiplier(tradeRoute.CargoClass, fleet.Faction, TradeObjective.MinBuyQuantity, out newSellPriceMultiplier))
			{
				return true;
			}
			return false;
		}

		protected bool ValidateTradeRoute(bool checkProfitability = true, bool updateNewPrices = true)
		{
			if (tradeRoute != null)
			{
				if (tradeRoute.CargoClass == null)
				{
					return false;
				}
				switch (currentState)
				{
				case ActiveTradeOrderState.GoBuy:
				case ActiveTradeOrderState.Buying:
					if (BuyLocationIsInvalid() || SellLocationIsInvalid())
					{
						return false;
					}
					if (checkProfitability)
					{
						float newBuyPriceMultiplier = 0f;
						float newSellPriceMultiplier = 0f;
						if (!IsTradeRouteProfitable(out newBuyPriceMultiplier, out newSellPriceMultiplier))
						{
							return false;
						}
						if (updateNewPrices)
						{
							tradeRoute.BuyPriceMultiplier = newBuyPriceMultiplier;
							tradeRoute.SellPriceMultiplier = newSellPriceMultiplier;
						}
					}
					break;
				case ActiveTradeOrderState.GoSell:
				case ActiveTradeOrderState.Selling:
					if (SellLocationIsInvalid())
					{
						return false;
					}
					if (checkProfitability)
					{
						float price = 0f;
						if (!tradeRoute.SellLocation.GetBuyPriceMultiplier(tradeRoute.CargoClass, fleet.Faction, TradeObjective.MinBuyQuantity, out price) || price <= tradeRoute.BuyPriceMultiplier)
						{
							return false;
						}
					}
					break;
				}
			}
			return true;
		}

		private bool BuyLocationIsInvalid()
		{
			return LocationIsInvalid(tradeRoute.BuyLocation);
		}

		private bool SellLocationIsInvalid()
		{
			return LocationIsInvalid(tradeRoute.SellLocation);
		}

		protected bool LocationIsInvalid(CargoTrader cargoTrader)
		{
			if (cargoTrader == null)
			{
				return true;
			}
			Unit unit = cargoTrader.Unit;
			if (!(unit == null) && unit.IsDockable && !(unit.Faction == null))
			{
				return !unit.Faction.RequestDock(unit, fleet.Faction);
			}
			return true;
		}

		private double GetTradeCompletionDelay()
		{
			return GetTradeCompletionDelay(fleet, Engine);
		}

		private double GetTradeCompletionTime()
		{
			return GetTradeCompletionTime(fleet, Engine);
		}

		public static double GetTradeCompletionDelay(Fleet group, EngineASX engine)
		{
			return group.Faction.IsPlayerFaction ? engine.GameSettings.AIDockPlayerOwnedPreferredCoolDownTime : engine.GameSettings.AIDockPreferredCoolDownTime;
		}

		public static double GetTradeCompletionTime(Fleet group, EngineASX engine)
		{
			return engine.ScenarioElapsedTime + GetTradeCompletionDelay(group, engine);
		}

		protected override void tick(float elapsedTime)
		{
			if (tradeRoute != null)
			{
				if (!ValidateTradeRoute())
				{
					TradeRoute = null;
					CurrentState = ActiveTradeOrderState.None;
					return;
				}
				switch (currentState)
				{
				case ActiveTradeOrderState.None:
					TradeRoute = null;
					IsIdle = true;
					break;
				case ActiveTradeOrderState.GoBuy:
					if (!LocationHasMinBuyQuantity(tradeRoute.BuyLocation, tradeRoute.CargoClass))
					{
						TradeRoute = null;
						CurrentState = ActiveTradeOrderState.None;
					}
					else if (fleet.AllUnitsAtDock(tradeRoute.BuyLocation.Unit))
					{
						CurrentState = ActiveTradeOrderState.Buying;
					}
					break;
				case ActiveTradeOrderState.GoSell:
					if (fleet.AllUnitsAtDock(tradeRoute.SellLocation.Unit))
					{
						CurrentState = ActiveTradeOrderState.Selling;
					}
					break;
				case ActiveTradeOrderState.Buying:
					if (CanTrade())
					{
						if (CanBuy())
						{
							BuyMaxCargo();
							CurrentState = ActiveTradeOrderState.PostBuy;
						}
						else
						{
							IsIdle = true;
						}
					}
					break;
				case ActiveTradeOrderState.Selling:
					if (CanTrade())
					{
						if (SellAllCargoIfAble())
						{
							CurrentState = ActiveTradeOrderState.PostSell;
						}
						else
						{
							IsIdle = true;
						}
					}
					break;
				case ActiveTradeOrderState.PostBuy:
					if (Engine.ScenarioElapsedTime > endBuySellTime)
					{
						if (TradeRoute != null && TradeRoute.SellLocation != null && tradeRoute.SellLocation.Unit.IsValidAndNotDestroyed)
						{
							CurrentState = ActiveTradeOrderState.GoSell;
						}
						else
						{
							CurrentState = ActiveTradeOrderState.None;
						}
					}
					break;
				case ActiveTradeOrderState.PostSell:
					if (Engine.ScenarioElapsedTime > endBuySellTime)
					{
						CurrentState = ActiveTradeOrderState.None;
					}
					break;
				}
			}
			else
			{
				OnNoTradeRoute();
			}
		}

		protected virtual void OnNoTradeRoute()
		{
		}

		protected override void onFinalizeObjective()
		{
			base.onFinalizeObjective();
			CurrentState = ActiveTradeOrderState.None;
			tradeRoute = null;
		}

		protected override void onFleetReachedTarget()
		{
			base.onFleetReachedTarget();
			ValidateTradeRouteAndClearIfInvalid();
			switch (currentState)
			{
			case ActiveTradeOrderState.GoBuy:
				CurrentState = ActiveTradeOrderState.Buying;
				break;
			case ActiveTradeOrderState.GoSell:
				CurrentState = ActiveTradeOrderState.Selling;
				break;
			}
		}

		protected override void resetTargetPosition()
		{
			if (tradeRoute != null && ValidateTradeRoute())
			{
				switch (currentState)
				{
				case ActiveTradeOrderState.Buying:
					SetTargetToBuyLocation();
					break;
				case ActiveTradeOrderState.GoBuy:
					SetTargetToBuyLocation();
					break;
				case ActiveTradeOrderState.Selling:
					SetTargetToSellLocation();
					break;
				case ActiveTradeOrderState.GoSell:
					SetTargetToSellLocation();
					break;
				}
			}
		}

		private bool CanTrade()
		{
			switch (currentState)
			{
			case ActiveTradeOrderState.Buying:
				if (fleet.Leader != null)
				{
					return fleet.AllUnitsAtDock(tradeRoute.BuyLocation.Unit);
				}
				return false;
			case ActiveTradeOrderState.Selling:
				if (fleet.Leader != null)
				{
					return fleet.AllUnitsAtDock(tradeRoute.SellLocation.Unit);
				}
				return false;
			default:
				return false;
			}
		}

		private bool CanBuy()
		{
			int maxPurchasableCargo = GetMaxPurchasableCargo(tradeRoute.CargoClass, tradeRoute.BuyLocation);
			return QuantityFulfilsMinOrder(tradeRoute.CargoClass, maxPurchasableCargo, fleet.CalculateTotalCargoCapacity());
		}

		public int GetMinBuyQuantity(CargoClass cargoClass, float fleetCargoCapacity)
		{
			return Mathf.Max(TradeObjective.MinBuyQuantity, (int)(TradeObjective.MinBuyCargoPercentage * fleetCargoCapacity / cargoClass.Volume));
		}

		public bool QuantityFulfilsMinOrder(CargoClass cargoClass, int quantity, float fleetTotalCargoCapacity)
		{
			return quantity >= GetMinBuyQuantity(cargoClass, fleetTotalCargoCapacity);
		}

		public bool LocationHasMinBuyQuantity(CargoTrader trader, CargoClass cargoClass)
		{
			int cargoCountOf = trader.GetCargoCountOf(tradeRoute.CargoClass);
			return QuantityFulfilsMinOrder(cargoClass, cargoCountOf, fleet.CalculateTotalCargoCapacity());
		}

		private void SetTargetToSellLocation()
		{
			fleet.SetTargetToDock(this, tradeRoute.SellLocation.Unit);
		}

		private void SetTargetToBuyLocation()
		{
			fleet.SetTargetToDock(this, tradeRoute.BuyLocation.Unit);
		}

		private void OnStateChanged()
		{
			switch (currentState)
			{
			case ActiveTradeOrderState.PostSell:
			case ActiveTradeOrderState.PostBuy:
				endBuySellTime = GetTradeCompletionTime();
				break;
			case ActiveTradeOrderState.None:
				TradeRoute = null;
				break;
			case ActiveTradeOrderState.Buying:
				if (!ValidateTradeRoute())
				{
					CurrentState = ActiveTradeOrderState.None;
				}
				break;
			case ActiveTradeOrderState.Selling:
				if (!ValidateTradeRoute())
				{
					CurrentState = ActiveTradeOrderState.None;
				}
				break;
			case ActiveTradeOrderState.GoBuy:
				if (tradeRoute != null && tradeRoute.BuyLocation == null)
				{
					Debug.LogError("AI is attempting to buy but doesn't have a BuyLocation", this);
				}
				break;
			case ActiveTradeOrderState.GoSell:
				if (tradeRoute.SellLocation == null)
				{
					Debug.LogError("AI is attempting to sell but doesn't have a SellLocation", this);
				}
				break;
			}
			IsIdle = false;
			ResetTargetPosition();
		}

		private void RegisterOnTradeNetwork()
		{
			if (fleet != null && fleet.Faction != null && fleet.Faction.TradeNetwork != null)
			{
				fleet.Faction.TradeNetwork.RegisterBuyingCargo(fleet, tradeRoute.BuyLocation.Unit, tradeRoute.CargoClass, tradeRoute.EstimatedQuantity);
			}
		}

		public override void OnNpcUnableToDockAtNavpoint(NpcPilot controller, Unit unit, UnableToDockReason unableToDockReason)
		{
			if (currentState != ActiveTradeOrderState.None)
			{
				switch (unableToDockReason)
				{
				case UnableToDockReason.Refused:
					if (LogWrapper.LogMsgs)
					{
						LogWrapper.Log($"Fleet {fleet} refused permission to dock. Invalidating state of ${this}", this, 1);
					}
					CurrentState = ActiveTradeOrderState.None;
					return;
				case UnableToDockReason.DockingBaysFull:
					if (LogWrapper.LogMsgs)
					{
						LogWrapper.Log($"Fleet {fleet} unable to dock as bays full. Invalidating state of ${this}", this, 1);
					}
					CurrentState = ActiveTradeOrderState.None;
					return;
				}
			}
			base.OnNpcUnableToDockAtNavpoint(controller, unit, unableToDockReason);
		}

		public override DockedPreference GetPreferToDockWhenIdle()
		{
			ActiveTradeOrderState activeTradeOrderState = currentState;
			if (activeTradeOrderState == ActiveTradeOrderState.Buying || (uint)(activeTradeOrderState - 4) <= 2u)
			{
				return DockedPreference.Dock;
			}
			return DockedPreference.Undock;
		}

		public override bool RequestUndock(NpcPilot npcPilot)
		{
			ActiveTradeOrderState activeTradeOrderState = currentState;
			if (activeTradeOrderState == ActiveTradeOrderState.Buying || (uint)(activeTradeOrderState - 4) <= 2u)
			{
				return false;
			}
			return true;
		}
	}
}
