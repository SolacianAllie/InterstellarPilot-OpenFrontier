using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine
{
	public class EngineEconomySettings : MonoBehaviour
	{
		public float FactionAIMinTradeEfficiency = 0.2f;

		public float FactionAITradeEfficiencyPower = 0.5f;

		public float StationMiscTaxRate = 0.1f;

		public float StationShipSaleTaxRate = 0.025f;

		public int ShipSaleRounding = 250;

		public int GeneralRounding = 25;

		public float EquipmentCostMultiplier = 0.87f;

		public float MiningMinPower = 0.6f;

		public float MiningMaxPower = 1.7f;

		public float CargoFactoryBayInputReserve = 0.3f;

		public float CargoFactoryBayOutputReserve = 0.8f;

		public float AITradeSearchScorePerMetreTravelled = -0.5f;

		public float AITradeEfficiencyMinCostFuzziness = 0.2f;

		public float AITradeEfficiencyMaxCostFuzziness = 1f;

		public float AITradeSearchMinPriceObscure = 0.2f;

		public float AITradeSearchMaxPriceObscure = 0.8f;

		public float AITradeSearchHighStockLevelCostDiscount = 0.5f;

		public float AITradeSearchHighStockReferenceValue = 5f;

		public float AITradeSearchLowStockLevelCostDiscount = 0.5f;

		public float AITradeSearchLowStockLevelThreshold = 0.4f;

		public float AITradeSearchHighStockLevelThreshold = 0.6f;

		public float ClearTraderCachedPricesInterval = 10f;

		public bool ApplyGoodRelationsMarkup = true;

		public bool ApplyTraderMarkup = true;

		public float AsteroidYieldQuantityMultiplier = 3f;

		public float CargoFactoryMaxBuyPrice = 1.3f;

		public float CargoFactoryMaxSellPrice = 0.9f;

		public float CargoFactoryMinBuyPrice = 1.1f;

		public float CargoFactoryMinSellPrice = 0.7f;

		public float CargoFactoryHighStock = 0.75f;

		public float CargoFactoryLowStock = 0.4f;

		public float CargoFactoryLowStockMin = 0.25f;

		public float CargoFactoryRequiredInputsMultiplier = 0.75f;

		public float CargoGoodOpinionMultiplier = 0.2f;

		public float CargoGoodOpinionPower = 2f;

		public float CargoPriceMultiplierColorAffect = 0.5f;

		public float ConsumerRevenueMultiplier = 1.7f;

		public float ConsumerProductionTimeMultiplier = 0.5f;

		public float PreferredProductionTimeCycle = 60f;

		public float ProductionTimePerCredits = 0.01f;

		public bool SetMiscPrices;

		public float TraderInitWithCargoMaxVolume = 0.6f;

		public float TraderInitWithCargoMinVolume = 0.3f;

		public float TraderInitWithCargoProbability = 0.75f;

		[FormerlySerializedAs("TraderMarkup")]
		public float TraderMarkupUpper = 0.2f;

		public float TraderMarkupLower = 0.2f;

		public bool TrimTraderCargo = true;

		public float TrimTraderCargoThresholdCargoUsage = 0.85f;

		public float TrimTraderCargoTrimLevel = 0.5f;

		public float RepairCostPerHullPoint = 0.01f;

		public float HullValuePerHullPoint = 20f;

		public float TradeIllegalCargoThreshold = -0.1f;

		public float OwnStationTradeScorePercentage = 0.15f;
	}
}
