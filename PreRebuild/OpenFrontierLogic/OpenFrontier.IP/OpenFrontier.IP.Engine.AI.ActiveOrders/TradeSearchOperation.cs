using System.Collections.Generic;
using OpenFrontier.IP.Engine.Core;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.Intel;
using OpenFrontier.IP.Engine.Fleets.ActiveOrders;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.Engine.Pathfinding;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class TradeSearchOperation
	{
		private CargoClass bestCargoClass;

		private CargoTrader bestBuyLocation;

		private CargoTrader bestSellLocation;

		private float bestBuyPrice;

		private float bestSellPrice;

		private float bestTraderScore = float.MinValue;

		private int bestEstimatedQuantity;

		private static List<WorldNavpoint> pathFindCache = new List<WorldNavpoint>(30);

		private HashSet<int> tradeSpecificCargoTypes;

		private bool hasFinished;

		private AITradeRoute tradeRouteResult;

		private ActiveAutonomousTradeOrder tradeObjective;

		private Fleet fleet;

		private Queue<TradeSearchOperationItem> searchItems = new Queue<TradeSearchOperationItem>(100);

		private int searchItemsInitialCount;

		private Sector homeSector;

		private int maxJumpDistanceFromHomeSector = -1;

		private Faction ourFaction;

		private bool hasObtainedTradeLock = true;

		public TradeSearchOptions TradeSearchOptions { get; set; }

		public TradeSearchFleetStats FleetStats { get; set; }

		public AITradeRoute Result => tradeRouteResult;

		public bool HasFinished => hasFinished;

		public void Initialise(ActiveAutonomousTradeOrder objective)
		{
			hasFinished = false;
			bestCargoClass = null;
			bestBuyLocation = null;
			bestSellLocation = null;
			bestBuyPrice = 0f;
			bestTraderScore = float.MinValue;
			bestSellPrice = 0f;
			tradeRouteResult = null;
			searchItemsInitialCount = 0;
			bestEstimatedQuantity = 0;
			tradeObjective = objective;
			fleet = tradeObjective.Fleet;
			ourFaction = fleet.Faction;
			hasObtainedTradeLock = TryGetTradeNetworkLock();
			if (fleet.IsHomeBaseValid)
			{
				homeSector = fleet.HomeSector;
			}
			else
			{
				homeSector = fleet.Sector;
			}
			maxJumpDistanceFromHomeSector = tradeObjective.GetActualMaxJumpDist();
			if (FleetStats == null)
			{
				FleetStats = TradeSearchFleetStats.Create(fleet);
			}
			if (TradeSearchOptions == null)
			{
				TradeSearchOptions = new TradeSearchOptions();
			}
		}

		private bool TryGetTradeNetworkLock()
		{
			if (ourFaction.TradeNetwork != null && tradeObjective != null)
			{
				if (ourFaction.TradeNetwork.IsLocked(tradeObjective))
				{
					return false;
				}
				ourFaction.TradeNetwork.ObtainLock(tradeObjective);
			}
			return true;
		}

		public void InitialiseAndStartSearch(ActiveAutonomousTradeOrder objective)
		{
			Initialise(objective);
			PopulateSearchItems();
			if (searchItems.Count == 0)
			{
				hasFinished = true;
			}
		}

		public void PopulateSearchItems()
		{
			searchItems.Clear();
			Faction faction = fleet.Faction;
			if (faction != null)
			{
				PopulateSpecificCargoTypesCache();
				foreach (CargoTrader validTraderTarget in faction.GetValidTraderTargets())
				{
					if (!IsValidBuyLocation(validTraderTarget))
					{
						continue;
					}
					Unit unit = validTraderTarget.Unit;
					_ = unit.Faction;
					if (unit.Sector != homeSector)
					{
						UniversePath universePath = fleet.Faction.Intel.GetUniversePath(homeSector, unit.Sector);
						if (universePath == null || universePath.Jumps > maxJumpDistanceFromHomeSector)
						{
							continue;
						}
					}
					if (!CanAllGroupUnitsDockAtUnitIgnoreOccupancy(fleet.NpcShipHullTypes, unit))
					{
						continue;
					}
					foreach (int soldTradableCargoClass in validTraderTarget.SoldTradableCargoClasses)
					{
						CargoClass cargoClassById = EngineASX.Instance.GetCargoClassById(soldTradableCargoClass);
						if ((tradeSpecificCargoTypes == null || tradeSpecificCargoTypes.Contains(soldTradableCargoClass)) && CanTradeCargoClass(cargoClassById))
						{
							TradeSearchOperationItem item = new TradeSearchOperationItem
							{
								BuyLocation = validTraderTarget,
								CargoClass = cargoClassById
							};
							searchItems.Enqueue(item);
						}
					}
				}
			}
			searchItemsInitialCount = searchItems.Count;
		}

		private bool CanTradeCargoClass(CargoClass cargoClass)
		{
			if (tradeObjective.AutonomousTradeObjective.TradeOnlySpecificCargoTypes)
			{
				if (tradeSpecificCargoTypes != null)
				{
					return tradeSpecificCargoTypes.Contains(cargoClass.UniqueId);
				}
				return false;
			}
			return ourFaction.WillTradeCargoType(cargoClass);
		}

		public float GetSearchPercentageComplete()
		{
			if (searchItemsInitialCount == 0)
			{
				return 1f;
			}
			return 1f - (float)searchItems.Count / (float)searchItemsInitialCount;
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
			int tradeSearchesCount = GetTradeSearchesCount(elapsedTime);
			for (int i = 0; i < tradeSearchesCount; i++)
			{
				if (searchItems.Count > 0)
				{
					ProcessNextItem();
					continue;
				}
				OnNoMoreSearchItems();
				break;
			}
		}

		private int GetTradeSearchesCount(float elapsedTime)
		{
			if (fleet.IsPlayerFaction())
			{
				return Mathf.CeilToInt(EngineASX.Instance.PerformanceSettings.AITradeSearchSearchesPerSecondPlayer * elapsedTime);
			}
			return Mathf.CeilToInt(EngineASX.Instance.PerformanceSettings.AITradeSearchSearchesPerSecond * elapsedTime);
		}

		private void ProcessNextItem()
		{
			if (!hasObtainedTradeLock)
			{
				hasObtainedTradeLock = TryGetTradeNetworkLock();
				return;
			}
			TradeSearchOperationItem tradeSearchOperationItem = searchItems.Dequeue();
			CargoTrader buyLocation = tradeSearchOperationItem.BuyLocation;
			if (!IsValidBuyLocation(tradeSearchOperationItem.BuyLocation))
			{
				return;
			}
			Unit unit = tradeSearchOperationItem.BuyLocation.Unit;
			CargoClass cargoClass = tradeSearchOperationItem.CargoClass;
			if (ourFaction == null)
			{
				return;
			}
			pathFindCache.Clear();
			AdvPathfinder.PathfindResult pathfindResult = AdvPathfinder.Pathfind(fleet.Sector, fleet.SectorPosition, unit.Sector, unit.SectorPosition, ourFaction, ignoreHeat: false, pathFindCache);
			if (!pathfindResult.IsSuccess)
			{
				return;
			}
			float baseScore = 0f;
			if (!TradeSearchOptions.IgnoreDistanceCost)
			{
				baseScore = pathfindResult.TotalDistance * FleetStats.DistanceScorePerMetre;
			}
			tradeSearchOperationItem.BaseScore = baseScore;
			EngineEconomySettings economySettings = fleet.Engine.EconomySettings;
			int actualMaxJumpDist = tradeObjective.GetActualMaxJumpDist();
			List<ShipHullType> npcShipHullTypes = fleet.NpcShipHullTypes;
			float num = Mathf.Lerp(economySettings.AITradeEfficiencyMaxCostFuzziness, economySettings.AITradeEfficiencyMinCostFuzziness, ourFaction.TradeEfficiency);
			float baseScore2 = tradeSearchOperationItem.BaseScore;
			int num2 = tradeSearchOperationItem.BuyLocation.UnitComponents.CargoBayComponent.GetCountOf(cargoClass);
			if (num2 == 0)
			{
				return;
			}
			if (ourFaction.TradeNetwork != null)
			{
				num2 -= ourFaction.TradeNetwork.GetQuantityOfCargoClassBeingBoughtAt(fleet, unit, cargoClass);
			}
			int minBuyQuantity = tradeObjective.GetMinBuyQuantity(cargoClass, FleetStats.FleetTotalCargoSpace);
			if (num2 < minBuyQuantity)
			{
				return;
			}
			int maxPurchasable = GetMaxPurchasable(cargoClass, num2, FleetStats.FleetTotalCargoSpace);
			float priceMultiplier = 0f;
			if (!buyLocation.GetPriceMultiplier(cargoClass, fleet.Faction, TradeType.Sell, minBuyQuantity, out priceMultiplier))
			{
				return;
			}
			float num3 = (TradeSearchOptions.IgnoreProfitability ? 100f : GetObscuredPrice(economySettings, cargoClass, ourFaction.TradeEfficiency));
			float num4 = priceMultiplier * (float)maxPurchasable * num3;
			if (FleetStats.FleetFreeCargoSpace > 0f)
			{
				float num5 = Mathf.Clamp((float)num2 * cargoClass.Volume / FleetStats.FleetFreeCargoSpace, 0f, economySettings.AITradeSearchHighStockReferenceValue) / economySettings.AITradeSearchHighStockReferenceValue;
				float num6 = 1f - num5 * economySettings.AITradeSearchHighStockLevelCostDiscount;
				num4 *= num6;
			}
			if (unit.Faction == ourFaction)
			{
				num4 *= 1f - economySettings.OwnStationTradeScorePercentage;
			}
			num4 *= 1f + Random.value * num;
			baseScore2 -= num4;
			float bestTraderPriceMultiplier = 0f;
			float num7 = 0f;
			CargoTrader cargoTrader = GetBestSellLocation(fleet.Engine, ourFaction, homeSector, buyLocation.Unit.Sector, buyLocation.Unit.SectorPosition, actualMaxJumpDist, FleetStats.DistanceScorePerMetre, cargoClass, maxPurchasable, priceMultiplier, buyLocation, npcShipHullTypes, TradeSearchOptions.IgnoreDistanceCost, TradeSearchOptions.IgnoreProfitability, out bestTraderPriceMultiplier, out num7, out var bestTraderMaxBuyable);
			if (cargoTrader != null)
			{
				baseScore2 += num7;
				if (baseScore2 > bestTraderScore)
				{
					bestBuyPrice = priceMultiplier * (float)cargoClass.BasePrice;
					bestSellPrice = bestTraderPriceMultiplier * (float)cargoClass.BasePrice;
					bestSellLocation = cargoTrader;
					bestBuyLocation = buyLocation;
					bestTraderScore = baseScore2;
					bestCargoClass = cargoClass;
					bestEstimatedQuantity = bestTraderMaxBuyable;
				}
			}
		}

		public static int GetMaxPurchasable(CargoClass cargoClass, int quantityAvailable, float fleetCargoSpace)
		{
			return Mathf.Min((int)(fleetCargoSpace / cargoClass.Volume), quantityAvailable);
		}

		private void OnNoMoreSearchItems()
		{
			hasFinished = true;
			if (bestCargoClass != null)
			{
				tradeRouteResult = new AITradeRoute
				{
					SellPriceMultiplier = bestSellPrice / (float)bestCargoClass.BasePrice,
					BuyPriceMultiplier = bestBuyPrice / (float)bestCargoClass.BasePrice,
					BuyLocation = bestBuyLocation,
					SellLocation = bestSellLocation,
					CargoClass = bestCargoClass,
					EstimatedProfit = bestTraderScore,
					EstimatedQuantity = bestEstimatedQuantity
				};
			}
		}

		private bool IsValidBuyLocation(CargoTrader trader)
		{
			if (trader != null && trader.Unit.Faction != null)
			{
				return trader.SoldTradableCargoClasses.Count > 0;
			}
			return false;
		}

		private void PopulateSpecificCargoTypesCache()
		{
			if (tradeObjective.AutonomousTradeObjective.TradeOnlySpecificCargoTypes)
			{
				if (tradeSpecificCargoTypes == null)
				{
					tradeSpecificCargoTypes = new HashSet<int>(8);
				}
				else
				{
					tradeSpecificCargoTypes.Clear();
				}
				{
					foreach (CargoClass tradeSpecificCargoType in tradeObjective.AutonomousTradeObjective.TradeSpecificCargoTypes)
					{
						if (tradeSpecificCargoType != null)
						{
							tradeSpecificCargoTypes.Add(tradeSpecificCargoType.UniqueId);
						}
					}
					return;
				}
			}
			tradeSpecificCargoTypes = null;
		}

		public static CargoTrader GetBestSellLocation(EngineASX engine, Faction ourFaction, Sector homeSector, Sector currentSector, Vector3 currentSectorPosition, int maxJumpDist, float distanceScorePerMetre, CargoClass cargoClass, int quantityToSell, float minPriceMultiplier, CargoTrader buyLocation, List<ShipHullType> hullTypes, bool ignoreDistanceCost, bool ignoreProfitability, out float bestTraderPriceMultiplier, out float bestTraderScore, out int bestTraderMaxBuyable)
		{
			CargoTrader cargoTrader = null;
			bestTraderScore = 0f;
			bestTraderPriceMultiplier = 0f;
			bestTraderMaxBuyable = 0;
			EngineEconomySettings economySettings = engine.EconomySettings;
			if (ourFaction == null)
			{
				return null;
			}
			float num = (ignoreProfitability ? 100f : GetObscuredPrice(economySettings, cargoClass, ourFaction.TradeEfficiency));
			foreach (CargoTrader validTraderTarget in ourFaction.GetValidTraderTargets())
			{
				if (!validTraderTarget.BoughtCargoClasses.Contains(cargoClass.UniqueId))
				{
					continue;
				}
				Unit unit = validTraderTarget.Unit;
				if (!unit.IsValidAndNotDestroyed || validTraderTarget == buyLocation)
				{
					continue;
				}
				int maxBuyable = validTraderTarget.GetMaxBuyable(quantityToSell, ourFaction, cargoClass);
				if (maxBuyable <= 0)
				{
					continue;
				}
				float priceMultiplier = 0f;
				if (!validTraderTarget.GetPriceMultiplier(cargoClass, ourFaction, TradeType.Buy, maxBuyable, out priceMultiplier) || priceMultiplier < minPriceMultiplier)
				{
					continue;
				}
				if (unit.Sector != homeSector)
				{
					if (maxJumpDist < 0)
					{
						maxJumpDist = 999;
					}
					UniversePath universePath = ourFaction.Intel.GetUniversePath(homeSector, unit.Sector);
					if (universePath == null || universePath.Jumps > maxJumpDist)
					{
						continue;
					}
				}
				pathFindCache.Clear();
				AdvPathfinder.PathfindResult pathfindResult = AdvPathfinder.Pathfind(currentSector, currentSectorPosition, unit.Sector, unit.SectorPosition, ourFaction, ignoreHeat: false, pathFindCache);
				if (!pathfindResult.IsSuccess)
				{
					continue;
				}
				float num2 = 0f;
				if (!ignoreDistanceCost)
				{
					num2 += pathfindResult.TotalDistance * distanceScorePerMetre;
				}
				if (hullTypes != null && !CanAllGroupUnitsDockAtUnitIgnoreOccupancy(hullTypes, unit))
				{
					continue;
				}
				float num3 = priceMultiplier * num * (float)maxBuyable;
				if (unit.Faction == ourFaction)
				{
					num3 *= 1f + economySettings.OwnStationTradeScorePercentage;
				}
				if (cargoClass.IsTraded)
				{
					float stockLevelsForCargoPercent = validTraderTarget.GetStockLevelsForCargoPercent(cargoClass);
					float aITradeSearchLowStockLevelCostDiscount = economySettings.AITradeSearchLowStockLevelCostDiscount;
					if (stockLevelsForCargoPercent < aITradeSearchLowStockLevelCostDiscount)
					{
						float num4 = 1f + (1f - stockLevelsForCargoPercent / aITradeSearchLowStockLevelCostDiscount) * economySettings.AITradeSearchLowStockLevelCostDiscount;
						num3 *= num4;
					}
				}
				num2 += num3 * cargoClass.AITradeSearchProfitabilityFudge;
				if (cargoTrader == null || num2 > bestTraderScore)
				{
					bestTraderPriceMultiplier = priceMultiplier;
					cargoTrader = validTraderTarget;
					bestTraderScore = num2;
					bestTraderMaxBuyable = maxBuyable;
				}
			}
			return cargoTrader;
		}

		public static float GetObscuredPrice(EngineEconomySettings economySettings, CargoClass cargoClass, float tradeEfficiency)
		{
			float t = Mathf.Lerp(economySettings.AITradeSearchMaxPriceObscure, economySettings.AITradeSearchMinPriceObscure, tradeEfficiency);
			return Mathf.Lerp(cargoClass.BasePrice, 100f, t);
		}

		public static bool CanAllGroupUnitsDockAtUnit(List<ShipHullType> groupHullTypes, Unit unit)
		{
			UnitHangar hangar = unit.GetHangar();
			if (hangar != null)
			{
				return hangar.CanUnitsFitInHangar(groupHullTypes);
			}
			return false;
		}

		public static bool CanAllGroupUnitsDockAtUnitIgnoreOccupancy(List<ShipHullType> groupHullTypes, Unit unit)
		{
			UnitHangar hangar = unit.GetHangar();
			if (hangar != null)
			{
				return hangar.CanUnitsFitInHangarIgnoreOccupancy(groupHullTypes);
			}
			return false;
		}
	}
}
