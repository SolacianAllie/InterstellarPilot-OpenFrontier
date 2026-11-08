using UnityEngine;

namespace OpenFrontier.IP.Engine.CargoFactory
{
	public static class CargoFactoryCalculator
	{
		public static bool SetProfileItemQuantities(CargoFactoryProfileItem cargoFactoryProfileItem, EngineEconomySettings economySettings, float productionTimeMultiplier, out float productionTime)
		{
			float a = cargoFactoryProfileItem.CalculateMaxCostOfInputs(economySettings);
			float b = cargoFactoryProfileItem.CalculateMinOutputRevenue(economySettings);
			float num = Mathf.Max(a, b);
			float num2 = num * economySettings.ProductionTimePerCredits;
			float num3 = economySettings.PreferredProductionTimeCycle / num2 * cargoFactoryProfileItem.ProductionVolumeMultiplier * num;
			productionTime = num3 * economySettings.ProductionTimePerCredits * productionTimeMultiplier;
			if (cargoFactoryProfileItem.IsConsumer)
			{
				productionTime *= economySettings.ConsumerProductionTimeMultiplier;
			}
			float num4 = (cargoFactoryProfileItem.IsConverter ? economySettings.CargoFactoryRequiredInputsMultiplier : 1f);
			float num5 = 0f;
			foreach (CargoFactoryProfileItemInput input in cargoFactoryProfileItem.Inputs)
			{
				num5 += input.QuantityWeighting;
			}
			foreach (CargoFactoryProfileItemInput input2 in cargoFactoryProfileItem.Inputs)
			{
				float num6 = input2.QuantityWeighting / num5;
				int num7 = (input2.Quantity = Mathf.CeilToInt(num3 * num6 / (float)input2.CargoClass.BasePrice * num4));
				if (LogWrapper.LogMsgs && num7 <= 0)
				{
					Debug.LogError($"Cargo factory item at {cargoFactoryProfileItem} calculated zero or negative input quantity");
				}
			}
			float num8 = 0f;
			foreach (CargoFactoryProfileItemOutput output in cargoFactoryProfileItem.Outputs)
			{
				num8 += output.QuantityWeighting;
			}
			foreach (CargoFactoryProfileItemOutput output2 in cargoFactoryProfileItem.Outputs)
			{
				float num9 = output2.QuantityWeighting / num8;
				output2.Quantity = Mathf.CeilToInt(num3 * num9 / (float)output2.CargoClass.BasePrice * cargoFactoryProfileItem.RevenueMultiplier);
			}
			return true;
		}
	}
}
