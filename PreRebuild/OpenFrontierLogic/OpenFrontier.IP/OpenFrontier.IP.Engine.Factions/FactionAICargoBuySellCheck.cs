using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public static class FactionAICargoBuySellCheck
	{
		public static bool FactionWillSellItem(FactionAIBase faction, Unit unit, CargoClass cargoClass, Faction otherFaction, out int price, out int? maxQuantity)
		{
			price = 0;
			maxQuantity = null;
			if (faction.Faction.WillTradeCargoType(cargoClass) && WillFactionTradeItem(faction, unit, cargoClass, TradeType.Sell, out maxQuantity))
			{
				price = GetCargoPrice(faction, cargoClass, otherFaction, TradeType.Sell);
				return true;
			}
			return false;
		}

		public static bool FactionWillBuyItem(FactionAIBase factionAI, Unit buyingUnit, CargoClass cargoClass, int maxQuantityForSale, Faction otherFaction, out int price, out int? resultingMaxQuantityToBuy)
		{
			price = 0;
			resultingMaxQuantityToBuy = null;
			if (factionAI.Faction.WillTradeCargoType(cargoClass) && WillFactionTradeItem(factionAI, buyingUnit, cargoClass, TradeType.Buy, out resultingMaxQuantityToBuy))
			{
				if (cargoClass.IsTraded)
				{
					Sector homeSector = null;
					Fleet fleet = buyingUnit.GetFleet();
					int a = buyingUnit.Faction.GetMaxJumpDistanceFromHomeSector();
					List<ShipHullType> hullTypes = null;
					int b = 3;
					if (fleet != null)
					{
						homeSector = fleet.HomeSectorOrFactionHomeSector;
						a = fleet.Settings.MaxJumpDistance;
						hullTypes = fleet.NpcShipHullTypes;
					}
					if (TradeSearchOperation.GetBestSellLocation(EngineASX.Instance, buyingUnit.Faction, homeSector, buyingUnit.Sector, buyingUnit.SectorPosition, Mathf.Min(a, b), 1f, cargoClass, maxQuantityForSale, 0f, null, hullTypes, ignoreDistanceCost: false, ignoreProfitability: false, out var bestTraderPriceMultiplier, out var _, out var bestTraderMaxBuyable) == null)
					{
						return false;
					}
					bestTraderMaxBuyable -= buyingUnit.GetCargoCountOf(cargoClass);
					if (bestTraderMaxBuyable <= 0)
					{
						return false;
					}
					price = factionAI.Faction.GetMarkedUpPriceAfterOpinionChange(TradeType.Buy, cargoClass.BasePrice, otherFaction, bestTraderPriceMultiplier);
					resultingMaxQuantityToBuy = bestTraderMaxBuyable;
					return true;
				}
				price = GetCargoPrice(factionAI, cargoClass, otherFaction, TradeType.Buy);
				return true;
			}
			return false;
		}

		private static int GetCargoPrice(FactionAIBase faction, CargoClass cargoClass, Faction otherFaction, TradeType aiTradeType)
		{
			float priceMultiplier = 1f;
			if (cargoClass.IsEquipment)
			{
				priceMultiplier = ((aiTradeType == TradeType.Buy) ? 1.2f : 1.5f);
			}
			return faction.Faction.GetMarkedUpPriceAfterOpinionChange(aiTradeType, cargoClass.BasePrice, otherFaction, priceMultiplier);
		}

		private static bool WillFactionTradeItem(FactionAIBase factionAI, Unit tradingUnit, CargoClass cargoClass, TradeType tradeType, out int? maxQuantity)
		{
			maxQuantity = null;
			Fleet fleet = tradingUnit.GetFleet();
			if (fleet == null)
			{
				return false;
			}
			switch (fleet.FleetStrategy)
			{
			case FactionStrategy.Scavenge:
				return true;
			case FactionStrategy.War:
			case FactionStrategy.Scout:
			case FactionStrategy.BountyHunt:
			case FactionStrategy.DealEquipment:
			case FactionStrategy.PassengerTransport:
			case FactionStrategy.Explore:
			case FactionStrategy.Escort:
				if (cargoClass.IsEquipment && tradeType == TradeType.Buy)
				{
					return WillFactionBuyEquipment(tradingUnit, cargoClass, out maxQuantity);
				}
				return false;
			case FactionStrategy.Mine:
				if (!cargoClass.IsOre)
				{
					if (cargoClass.IsEquipment && tradeType == TradeType.Buy)
					{
						return WillFactionBuyEquipment(tradingUnit, cargoClass, out maxQuantity);
					}
					return false;
				}
				return true;
			case FactionStrategy.Trade:
				if (!cargoClass.IsTraded)
				{
					if (cargoClass.IsEquipment && tradeType == TradeType.Buy)
					{
						return WillFactionBuyEquipment(tradingUnit, cargoClass, out maxQuantity);
					}
					return false;
				}
				return true;
			default:
				if (cargoClass.IsEquipment && tradeType == TradeType.Buy)
				{
					return WillFactionBuyEquipment(tradingUnit, cargoClass, out maxQuantity);
				}
				return false;
			}
		}

		private static bool WillFactionBuyEquipment(Unit tradingUnit, CargoClass cargoClass, out int? maxQuantity)
		{
			maxQuantity = null;
			if (UnitCanUseEquipment(tradingUnit, cargoClass))
			{
				float fleetEquipmentUsageWhenRearming = FactionAIRearmer.GetFleetEquipmentUsageWhenRearming(tradingUnit.GetFleet());
				int num = Mathf.RoundToInt(tradingUnit.CargoBayComponent.Capacity * fleetEquipmentUsageWhenRearming);
				float equipmentLoad = tradingUnit.CargoBayComponent.EquipmentLoad;
				float num2 = (float)num - equipmentLoad;
				if (num2 > 0f)
				{
					maxQuantity = (int)(num2 / cargoClass.Volume);
					return true;
				}
			}
			return false;
		}

		private static bool UnitCanUseEquipment(Unit unit, CargoClass cargoClass)
		{
			foreach (TurretComponent turret in unit.Components.Turrets)
			{
				ProjectileTurretComponent projectileTurretComponent = turret as ProjectileTurretComponent;
				if (projectileTurretComponent != null && projectileTurretComponent.CanUseCargoClass(cargoClass))
				{
					return true;
				}
			}
			return false;
		}

		private static int? GetRequiredCountOFEquipment(Unit unit)
		{
			return null;
		}
	}
}
