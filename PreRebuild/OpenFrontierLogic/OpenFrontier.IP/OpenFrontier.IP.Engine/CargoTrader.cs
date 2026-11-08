using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.CargoFactory;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	[RequireComponent(typeof(UnitComponentHolder))]
	public class CargoTrader : MonoBehaviour
	{
		private bool hasInit;

		private UnitComponentHolder unitComponents;

		public CargoTraderPriceSetter PriceSetter;

		private bool hasCachedCargoTypes;

		private HashSet<int> cachedSoldCargoTypes = new HashSet<int>();

		private HashSet<int> cachedBoughtCargoTypes = new HashSet<int>();

		private HashSet<int> cachedSoldTradableCargoTypes = new HashSet<int>();

		private HashSet<int> cachedBoughtTradableCargoTypes = new HashSet<int>();

		private Dictionary<int, CargoTraderStockLevels> cachedStockLevels;

		public UnitComponentHolder UnitComponents => unitComponents;

		public Unit Unit => unitComponents.Unit;

		public HashSet<int> SoldCargoClasses
		{
			get
			{
				CacheCargoTypesIfNeeded();
				return cachedSoldCargoTypes;
			}
		}

		public HashSet<int> SoldTradableCargoClasses
		{
			get
			{
				if (!hasCachedCargoTypes)
				{
					CacheCargoTypes();
				}
				return cachedSoldTradableCargoTypes;
			}
		}

		public HashSet<int> BoughtTradableCargoClasses
		{
			get
			{
				if (!hasCachedCargoTypes)
				{
					CacheCargoTypes();
				}
				return cachedBoughtTradableCargoTypes;
			}
		}

		public HashSet<int> BoughtCargoClasses
		{
			get
			{
				if (!hasCachedCargoTypes)
				{
					CacheCargoTypes();
				}
				return cachedBoughtCargoTypes;
			}
		}

		public bool IsConsumer
		{
			get
			{
				UnitCargoFactory component = GetComponent<UnitCargoFactory>();
				if (component != null)
				{
					return component.IsConsumer;
				}
				return false;
			}
		}

		public void Init()
		{
			if (hasInit)
			{
				return;
			}
			hasInit = true;
			EngineASX.Instance.Traders.Add(this);
			unitComponents = GetComponent<UnitComponentHolder>();
			if (unitComponents != null)
			{
				UnitCargoFactory component = unitComponents.GetComponent<UnitCargoFactory>();
				if (component != null)
				{
					component.Init();
				}
			}
			cachedStockLevels = CreatePreferredStockLevels();
		}

		public static CargoTraderStockLevels CreateStockLevels(int maxQuantity, EngineEconomySettings ec, bool bought, bool sold)
		{
			CargoTraderStockLevels result = default;
			result.HighStockMax = maxQuantity;
			result.HighStock = Mathf.RoundToInt((float)result.HighStockMax * ec.CargoFactoryHighStock);
			result.LowStock = Mathf.RoundToInt((float)maxQuantity * ec.CargoFactoryLowStock);
			result.LowStockMin = Mathf.RoundToInt((float)maxQuantity * ec.CargoFactoryLowStockMin);
			result.Bought = bought;
			result.Sold = sold;
			return result;
		}

		public Dictionary<int, CargoTraderStockLevels> GetStockLevels()
		{
			return cachedStockLevels;
		}

		private Dictionary<int, CargoTraderStockLevels> CreatePreferredStockLevels()
		{
			Dictionary<int, CargoTraderStockLevels> dictionary = new Dictionary<int, CargoTraderStockLevels>();
			foreach (CargoClass cargoClass in Unit.Engine.CargoClasses)
			{
				CargoTraderStockLevels? cargoTraderStockLevels = CreateStockLevelsForCargo(cargoClass);
				if (cargoTraderStockLevels.HasValue)
				{
					dictionary.Add(cargoClass.UniqueId, cargoTraderStockLevels.Value);
				}
			}
			return dictionary;
		}

		public int GetCargoCountOf(CargoClass cargoClass)
		{
			return Unit.GetCargoCountOf(cargoClass);
		}

		public bool HasInfiniteOf(CargoClass cargoClass)
		{
			float price = 0f;
			if ((cargoClass.IsEquipment || cargoClass.IsDeployable) && EngineASX.Instance.GameSettings.EquipmentDealerInfiniteCargo)
			{
				return GetSellPriceMultiplier(cargoClass, null, 1, out price);
			}
			return false;
		}

		public string GetCargoCountStr(CargoClass cargoClass)
		{
			if (HasInfiniteOf(cargoClass))
			{
				return TextFormattingHelper.UnlimitedCargoText;
			}
			return TextFormattingHelper.FormatCargoAmount(GetCargoCountOf(cargoClass));
		}

		public void CacheCargoTypesIfNeeded()
		{
			if (!hasCachedCargoTypes)
			{
				CacheCargoTypes();
			}
		}

		public void CacheCargoTypes()
		{
			CacheSoldCargoTypes();
			CacheBoughtCargoTypes();
			hasCachedCargoTypes = true;
		}

		private void CacheSoldCargoTypes()
		{
			cachedSoldCargoTypes.Clear();
			cachedSoldTradableCargoTypes.Clear();
			foreach (CargoClass cargoClass in unitComponents.Engine.CargoClasses)
			{
				if (IsSellerOf(cargoClass))
				{
					cachedSoldCargoTypes.Add(cargoClass.UniqueId);
					if (cargoClass.IsTraded && !cachedSoldTradableCargoTypes.Contains(cargoClass.UniqueId))
					{
						cachedSoldTradableCargoTypes.Add(cargoClass.UniqueId);
					}
				}
			}
		}

		private void CacheBoughtCargoTypes()
		{
			cachedBoughtCargoTypes.Clear();
			cachedBoughtTradableCargoTypes.Clear();
			foreach (CargoClass cargoClass in unitComponents.Engine.CargoClasses)
			{
				if (IsBuyerOf(cargoClass))
				{
					cachedBoughtCargoTypes.Add(cargoClass.UniqueId);
					if (cargoClass.IsTraded && !cachedBoughtTradableCargoTypes.Contains(cargoClass.UniqueId))
					{
						cachedBoughtTradableCargoTypes.Add(cargoClass.UniqueId);
					}
				}
			}
		}

		public bool GetPriceMultiplier(CargoClass cargoClass, Faction tradingFaction, TradeType tradeType, int tradeQuantity, out float priceMultiplier)
		{
			priceMultiplier = 1f;
			switch (tradeType)
			{
			case TradeType.Sell:
				if (!CalculatePriceMultiplier(cargoClass, tradingFaction, tradeType, tradeQuantity, out priceMultiplier))
				{
					return false;
				}
				break;
			case TradeType.Buy:
				if (!CalculatePriceMultiplier(cargoClass, tradingFaction, tradeType, tradeQuantity, out priceMultiplier))
				{
					return false;
				}
				break;
			}
			if (tradingFaction != null && tradingFaction != Unit.Faction)
			{
				priceMultiplier = unitComponents.Unit.Faction.ApplyFactionOpinionToPriceMultiplier(tradeType, tradingFaction, priceMultiplier);
			}
			return true;
		}

		public bool CalculatePriceMultiplier(CargoClass cargoClass, Faction tradingFaction, TradeType tradeType, int tradeQuantity, out float priceMultiplier)
		{
			priceMultiplier = 1f;
			if (Unit.Faction != null && Unit.Faction.WillBuyCargoType(cargoClass))
			{
				if (cargoClass.IsEquipment && (PriceSetter.SetAllEquipmentPrices || PriceSetter.SetSpecificPrices.Contains(cargoClass)))
				{
					if (tradingFaction != Unit.Faction)
					{
						priceMultiplier = Unit.Faction.MarkupPriceMultiplier(tradeType, priceMultiplier);
					}
					return true;
				}
				if (cargoClass.IsDeployable && (PriceSetter.SetAllDeployablePrices || PriceSetter.SetSpecificPrices.Contains(cargoClass)))
				{
					if (tradingFaction != Unit.Faction)
					{
						priceMultiplier = Unit.Faction.MarkupPriceMultiplier(tradeType, priceMultiplier);
					}
					return true;
				}
				int num = unitComponents.CargoBayComponent.GetCountOf(cargoClass);
				if (tradeType == TradeType.Buy)
				{
					num += tradeQuantity;
				}
				if (GetPriceMultiplierFromStockLevels(cargoClass, num, tradeType, out priceMultiplier))
				{
					priceMultiplier = Unit.Faction.MarkupPriceMultiplier(tradeType, priceMultiplier);
					return true;
				}
			}
			return false;
		}

		public int GetMaxBuyable(int maxPossibleBuyable, Faction buyerFaction, CargoClass cargoClass)
		{
			if (!HasInfiniteOf(cargoClass))
			{
				maxPossibleBuyable = Mathf.Min(maxPossibleBuyable, unitComponents.CargoBayComponent.GetFreeSpaceFor(cargoClass));
				CargoTraderStockLevels? stockLevelsForCargo = GetStockLevelsForCargo(cargoClass);
				if (!stockLevelsForCargo.HasValue || !stockLevelsForCargo.Value.Bought)
				{
					return 0;
				}
				int num = stockLevelsForCargo.Value.HighStockMax - unitComponents.CargoBayComponent.GetCountOf(cargoClass);
				if (num < 0)
				{
					num = 0;
				}
				maxPossibleBuyable = Mathf.Min(maxPossibleBuyable, num);
			}
			if (maxPossibleBuyable > 0 && unitComponents.Unit.Faction != null)
			{
				if (unitComponents.Unit.Faction == buyerFaction)
				{
					return maxPossibleBuyable;
				}
				if (GetBuyPrice(cargoClass, buyerFaction, maxPossibleBuyable, out var pricePerUnit))
				{
					return Mathf.Min(maxPossibleBuyable, unitComponents.Unit.Faction.Credits / pricePerUnit);
				}
			}
			return 0;
		}

		public bool HasSellPrice(CargoClass cargoClass)
		{
			float price = 0f;
			return GetSellPriceMultiplier(cargoClass, null, 1, out price);
		}

		public bool HasBuyPrice(CargoClass cargoClass)
		{
			float price = 0f;
			return GetBuyPriceMultiplier(cargoClass, null, 1, out price);
		}

		public bool IsSellerOf(CargoClass cargoClass)
		{
			if (cargoClass.IsEquipment && (PriceSetter.SetAllEquipmentPrices || PriceSetter.SetSpecificPrices.Contains(cargoClass)))
			{
				return true;
			}
			if (cargoClass.IsDeployable && (PriceSetter.SetAllDeployablePrices || PriceSetter.SetSpecificPrices.Contains(cargoClass)))
			{
				return true;
			}
			CargoTraderStockLevels? stockLevelsForCargo = GetStockLevelsForCargo(cargoClass);
			if (stockLevelsForCargo.HasValue)
			{
				return stockLevelsForCargo.Value.Sold;
			}
			return false;
		}

		public bool IsBuyerOf(CargoClass cargoClass)
		{
			if (cargoClass.IsEquipment && (PriceSetter.SetAllEquipmentPrices || PriceSetter.SetSpecificPrices.Contains(cargoClass)))
			{
				return true;
			}
			if (cargoClass.IsDeployable && (PriceSetter.SetAllDeployablePrices || PriceSetter.SetSpecificPrices.Contains(cargoClass)))
			{
				return true;
			}
			CargoTraderStockLevels? stockLevelsForCargo = GetStockLevelsForCargo(cargoClass);
			if (stockLevelsForCargo.HasValue)
			{
				return stockLevelsForCargo.Value.Bought;
			}
			return false;
		}

		public bool HasPrice(CargoClass cargoClass)
		{
			if (!HasBuyPrice(cargoClass))
			{
				return HasSellPrice(cargoClass);
			}
			return true;
		}

		public bool GetPriceMultiplierFromStockLevels(CargoClass cargoClass, int newQuantity, TradeType tradeType, out float priceMultiplier)
		{
			priceMultiplier = 0f;
			CargoTraderStockLevels? stockLevelsForCargo = GetStockLevelsForCargo(cargoClass);
			if (stockLevelsForCargo.HasValue)
			{
				CargoTraderStockLevels value = stockLevelsForCargo.Value;
				switch (tradeType)
				{
				case TradeType.Buy:
					if (!value.Bought)
					{
						return false;
					}
					break;
				case TradeType.Sell:
					if (!value.Sold)
					{
						return false;
					}
					break;
				}
				EngineEconomySettings economySettings = unitComponents.Engine.EconomySettings;
				float num = ((tradeType == TradeType.Buy) ? economySettings.CargoFactoryMinBuyPrice : economySettings.CargoFactoryMinSellPrice);
				float num2 = ((tradeType == TradeType.Buy) ? economySettings.CargoFactoryMaxBuyPrice : economySettings.CargoFactoryMaxSellPrice);
				float num3 = Mathf.Lerp(num, num2, 0.5f);
				if (newQuantity < value.LowStockMin)
				{
					priceMultiplier = num2;
				}
				else if (newQuantity > value.HighStockMax)
				{
					if (tradeType == TradeType.Buy)
					{
						return false;
					}
					priceMultiplier = num;
				}
				else if (newQuantity >= value.LowStockMin && newQuantity < value.LowStock)
				{
					priceMultiplier = Mathf.Lerp(num2, num3, (float)(newQuantity - value.LowStockMin) / (float)(value.LowStock - value.LowStockMin));
				}
				else if (newQuantity >= value.HighStock && newQuantity <= value.HighStockMax)
				{
					priceMultiplier = Mathf.Lerp(num3, num, (float)(newQuantity - value.HighStock) / (float)(value.HighStockMax - value.HighStock));
				}
				else
				{
					priceMultiplier = num3;
				}
				return true;
			}
			return false;
		}

		public float GetStockLevelsForCargoPercent01(CargoClass cargoClass)
		{
			CargoTraderStockLevels? stockLevelsForCargo = GetStockLevelsForCargo(cargoClass);
			return Mathf.Clamp01((float)unitComponents.CargoBayComponent.GetCountOf(cargoClass) / (float)stockLevelsForCargo.Value.HighStockMax);
		}

		public float GetStockLevelsForCargoPercent(CargoClass cargoClass)
		{
			CargoTraderStockLevels? stockLevelsForCargo = GetStockLevelsForCargo(cargoClass);
			return (float)unitComponents.CargoBayComponent.GetCountOf(cargoClass) / (float)stockLevelsForCargo.Value.HighStockMax;
		}

		public CargoTraderStockLevels? GetStockLevelsForCargo(CargoClass cargoClass)
		{
			if (cachedStockLevels.TryGetValue(cargoClass.UniqueId, out var value))
			{
				return value;
			}
			return null;
		}

		public CargoTraderStockLevels? CreateStockLevelsForCargo(CargoClass cargoClass)
		{
			UnitCargoFactory factoryComponent = Unit.Components.FactoryComponent;
			EngineEconomySettings economySettings = unitComponents.Engine.EconomySettings;
			CargoBayComponent cargoBayComponent = Unit.CargoBayComponent;
			if (factoryComponent != null)
			{
				float totalInputVolume = factoryComponent.TotalInputVolume;
				float totalOutputVolume = factoryComponent.TotalOutputVolume;
				if (factoryComponent.GetInputQuantity(cargoClass) > 0)
				{
					float num = economySettings.CargoFactoryBayInputReserve * cargoBayComponent.Capacity;
					return CreateStockLevels(Mathf.RoundToInt(factoryComponent.GetInputVolume(cargoClass) / totalInputVolume * num / cargoClass.Volume), economySettings, bought: true, sold: false);
				}
				if (factoryComponent.GetOutputQuantity(cargoClass) > 0)
				{
					float num2 = economySettings.CargoFactoryBayOutputReserve * cargoBayComponent.Capacity;
					return CreateStockLevels(Mathf.RoundToInt(factoryComponent.GetOutputVolume(cargoClass) / totalOutputVolume * num2 / cargoClass.Volume), economySettings, bought: false, sold: true);
				}
			}
			else if (PriceSetter != null && ((economySettings.SetMiscPrices && PriceSetter.SetAllPrices) || PriceSetter.SetSpecificPrices.Contains(cargoClass)))
			{
				return CreateStockLevels(100, economySettings, bought: true, sold: true);
			}
			return null;
		}

		public bool GetPrice(CargoClass cargoClass, Faction buyer, TradeType tradeType, int tradeQuantity, out int price)
		{
			price = -1;
			float priceMultiplier = 0f;
			if (GetPriceMultiplier(cargoClass, buyer, tradeType, tradeQuantity, out priceMultiplier))
			{
				price = Mathf.CeilToInt(priceMultiplier * (float)cargoClass.BasePrice);
				return true;
			}
			return false;
		}

		public bool GetBuyPrice(CargoClass cargoClass, Faction trader, int tradeQuantity, out int pricePerUnit)
		{
			return GetPrice(cargoClass, trader, TradeType.Buy, tradeQuantity, out pricePerUnit);
		}

		public bool GetSellPrice(CargoClass cargoClass, Faction trader, int tradeQuantity, out int pricePerUnit)
		{
			return GetPrice(cargoClass, trader, TradeType.Sell, tradeQuantity, out pricePerUnit);
		}

		public bool GetBuyPriceMultiplier(CargoClass cargoClass, Faction trader, int tradeQuantity, out float price)
		{
			return GetPriceMultiplier(cargoClass, trader, TradeType.Buy, tradeQuantity, out price);
		}

		public bool GetSellPriceMultiplier(CargoClass cargoClass, Faction trader, int tradeQuantity, out float price)
		{
			return GetPriceMultiplier(cargoClass, trader, TradeType.Sell, tradeQuantity, out price);
		}

		private void OnDestroy()
		{
			EngineASX instance = EngineASX.Instance;
			if (instance != null)
			{
				instance.Traders.Remove(this);
			}
		}
	}
}
