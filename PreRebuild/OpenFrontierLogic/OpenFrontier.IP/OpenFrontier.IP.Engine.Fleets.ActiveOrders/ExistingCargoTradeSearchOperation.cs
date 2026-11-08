using System.Collections.Generic;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;

namespace OpenFrontier.IP.Engine.Fleets.ActiveOrders
{
	public class ExistingCargoTradeSearchOperation
	{
		public static AITradeRoute GetTradeRouteFromExistingTradeableCargo(Fleet fleet, TradeSearchFleetStats fleetStats, int maxJumpDist, out bool hadTradeableCargoUnableToSell)
		{
			hadTradeableCargoUnableToSell = false;
			float cachedMinMoveSpeed = fleet.GetCachedMinMoveSpeed();
			float distanceScorePerMetreTravelled = fleet.GetDistanceScorePerMetreTravelled(cachedMinMoveSpeed);
			List<ShipHullType> npcShipHullTypes = fleet.NpcShipHullTypes;
			int estimatedQuantity = 0;
			float num = float.MinValue;
			float sellPriceMultiplier = 0f;
			CargoClass cargoClass = null;
			CargoTrader cargoTrader = null;
			bool flag = false;
			if (fleet.GetCachedTradableCargoLoad() >= 1f)
			{
				foreach (UnitComponentHolder ship in fleet.Ships)
				{
					if (!(ship != null) || !(ship.CargoBayComponent != null) || !(ship.CargoBayComponent.TradableCargoLoad > 0f))
					{
						continue;
					}
					foreach (KeyValuePair<CargoClass, int> cargo in ship.CargoBayComponent.Cargos)
					{
						CargoClass key = cargo.Key;
						if (cargo.Key.IsTraded && cargo.Value > 0)
						{
							flag = true;
							float bestTraderPriceMultiplier = 0f;
							float bestTraderScore = 0f;
							CargoTrader bestSellLocation = TradeSearchOperation.GetBestSellLocation(fleet.Engine, fleet.Faction, fleet.GetHomeSectorOrCurrent(), fleet.Sector, fleet.SectorPosition, maxJumpDist, distanceScorePerMetreTravelled, key, cargo.Value, 0.75f, null, npcShipHullTypes, ignoreDistanceCost: false, ignoreProfitability: false, out bestTraderPriceMultiplier, out bestTraderScore, out var bestTraderMaxBuyable);
							if (bestSellLocation != null && bestTraderScore > num)
							{
								cargoTrader = bestSellLocation;
								num = bestTraderScore;
								sellPriceMultiplier = bestTraderPriceMultiplier;
								cargoClass = cargo.Key;
								estimatedQuantity = bestTraderMaxBuyable;
							}
						}
					}
				}
			}
			if (cargoTrader != null)
			{
				return new AITradeRoute
				{
					BuyLocation = null,
					SellLocation = cargoTrader,
					CargoClass = cargoClass,
					SellPriceMultiplier = sellPriceMultiplier,
					BuyPriceMultiplier = 0f,
					EstimatedProfit = num,
					EstimatedQuantity = estimatedQuantity
				};
			}
			hadTradeableCargoUnableToSell = flag;
			return null;
		}

		public static AITradeRoute GetTradeRouteFromExistingIncompatibleEquipment(Fleet fleet, TradeSearchFleetStats fleetStats, int maxJumpDist)
		{
			float cachedMinMoveSpeed = fleet.GetCachedMinMoveSpeed();
			float distanceScorePerMetreTravelled = fleet.GetDistanceScorePerMetreTravelled(cachedMinMoveSpeed);
			List<ShipHullType> npcShipHullTypes = fleet.NpcShipHullTypes;
			float num = float.MinValue;
			float sellPriceMultiplier = 0f;
			int estimatedQuantity = 0;
			CargoClass cargoClass = null;
			CargoTrader cargoTrader = null;
			if (fleet.GetCachedIncompatibleEquipmentValue() >= 1f)
			{
				foreach (UnitComponentHolder ship in fleet.Ships)
				{
					if (!(ship != null) || !(ship.CargoBayComponent != null))
					{
						continue;
					}
					NpcPilot pilotNpc = ship.PilotNpc;
					if (pilotNpc == null || !(ship.CargoBayComponent.EquipmentLoad > 0f))
					{
						continue;
					}
					foreach (KeyValuePair<CargoClass, int> cargo in ship.CargoBayComponent.Cargos)
					{
						CargoClass key = cargo.Key;
						if (cargo.Key.IsEquipment && cargo.Value > 0 && !pilotNpc.IsCargoClassCompatible(key))
						{
							float bestTraderPriceMultiplier = 0f;
							float bestTraderScore = 0f;
							CargoTrader bestSellLocation = TradeSearchOperation.GetBestSellLocation(fleet.Engine, fleet.Faction, fleet.GetHomeSectorOrCurrent(), fleet.Sector, fleet.SectorPosition, maxJumpDist, distanceScorePerMetreTravelled, key, cargo.Value, 0.75f, null, npcShipHullTypes, ignoreDistanceCost: false, ignoreProfitability: false, out bestTraderPriceMultiplier, out bestTraderScore, out var bestTraderMaxBuyable);
							if (bestSellLocation != null && bestTraderScore > num)
							{
								cargoTrader = bestSellLocation;
								num = bestTraderScore;
								sellPriceMultiplier = bestTraderPriceMultiplier;
								cargoClass = cargo.Key;
								estimatedQuantity = bestTraderMaxBuyable;
							}
						}
					}
				}
			}
			if (cargoTrader != null)
			{
				return new AITradeRoute
				{
					BuyLocation = null,
					SellLocation = cargoTrader,
					CargoClass = cargoClass,
					SellPriceMultiplier = sellPriceMultiplier,
					BuyPriceMultiplier = 0f,
					EstimatedQuantity = estimatedQuantity,
					EstimatedProfit = num
				};
			}
			return null;
		}

		public static AITradeRoute GetTradeRouteFromExistingCompatibleEquipment(Fleet fleet, TradeSearchFleetStats fleetStats, int maxJumpDist)
		{
			float cachedMinMoveSpeed = fleet.GetCachedMinMoveSpeed();
			float distanceScorePerMetreTravelled = fleet.GetDistanceScorePerMetreTravelled(cachedMinMoveSpeed);
			List<ShipHullType> npcShipHullTypes = fleet.NpcShipHullTypes;
			float num = float.MinValue;
			float sellPriceMultiplier = 0f;
			int estimatedQuantity = 0;
			CargoClass cargoClass = null;
			CargoTrader cargoTrader = null;
			if (fleet.GetCachedCompatibleEquipmentLoad() >= 1f)
			{
				foreach (UnitComponentHolder ship in fleet.Ships)
				{
					if (!(ship != null) || !(ship.CargoBayComponent != null))
					{
						continue;
					}
					NpcPilot pilotNpc = ship.PilotNpc;
					if (pilotNpc == null || !(ship.CargoBayComponent.EquipmentLoad > 0f))
					{
						continue;
					}
					foreach (KeyValuePair<CargoClass, int> cargo in ship.CargoBayComponent.Cargos)
					{
						CargoClass key = cargo.Key;
						if (cargo.Key.IsEquipment && cargo.Value > 0 && pilotNpc.IsCargoClassCompatible(key))
						{
							float bestTraderPriceMultiplier = 0f;
							float bestTraderScore = 0f;
							CargoTrader bestSellLocation = TradeSearchOperation.GetBestSellLocation(fleet.Engine, fleet.Faction, fleet.GetHomeSectorOrCurrent(), fleet.Sector, fleet.SectorPosition, maxJumpDist, distanceScorePerMetreTravelled, key, cargo.Value, 0.75f, null, npcShipHullTypes, ignoreDistanceCost: false, ignoreProfitability: false, out bestTraderPriceMultiplier, out bestTraderScore, out var bestTraderMaxBuyable);
							if (bestSellLocation != null && bestTraderScore > num)
							{
								cargoTrader = bestSellLocation;
								num = bestTraderScore;
								sellPriceMultiplier = bestTraderPriceMultiplier;
								cargoClass = cargo.Key;
								estimatedQuantity = bestTraderMaxBuyable;
							}
						}
					}
				}
			}
			if (cargoTrader != null)
			{
				return new AITradeRoute
				{
					BuyLocation = null,
					SellLocation = cargoTrader,
					CargoClass = cargoClass,
					SellPriceMultiplier = sellPriceMultiplier,
					BuyPriceMultiplier = 0f,
					EstimatedProfit = num,
					EstimatedQuantity = estimatedQuantity
				};
			}
			return null;
		}
	}
}
