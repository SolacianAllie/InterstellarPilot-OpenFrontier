using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.CargoFactory
{
	[Serializable]
	public class CargoFactoryProfileItem
	{
		[SerializeField]
		private float productionTime;

		public List<CargoFactoryProfileItemInput> Inputs = new List<CargoFactoryProfileItemInput>();

		public List<CargoFactoryProfileItemOutput> Outputs = new List<CargoFactoryProfileItemOutput>();

		public float ProfitPerCycle = -1f;

		public float ProfitPerMinute = -1f;

		public float RevenueMultiplier = 1f;

		public float ProductionTimeMultiplier = 1f;

		public float ProductionVolumeMultiplier = 1f;

		public float ProductionTime
		{
			get
			{
				return productionTime;
			}
			set
			{
				productionTime = value;
			}
		}

		public bool IsConverter
		{
			get
			{
				if (Inputs.Count > 0)
				{
					return Outputs.Count > 0;
				}
				return false;
			}
		}

		public bool IsConsumer
		{
			get
			{
				if (Outputs.Count == 0)
				{
					return Inputs.Count > 0;
				}
				return false;
			}
		}

		public int GetBaseCostOfInputs()
		{
			int num = 0;
			foreach (CargoFactoryProfileItemInput input in Inputs)
			{
				num += input.CargoClass.BasePrice;
			}
			return num;
		}

		public int GetBaseCostOfOutputs()
		{
			int num = 0;
			foreach (CargoFactoryProfileItemOutput output in Outputs)
			{
				num += output.CargoClass.BasePrice;
			}
			return num;
		}

		public float CalculateMinProfitPerCycle(EngineEconomySettings ec)
		{
			float num = 0f;
			if (IsConsumer)
			{
				foreach (CargoFactoryProfileItemInput input in Inputs)
				{
					num += (float)input.CargoClass.BasePrice * ec.ConsumerRevenueMultiplier * RevenueMultiplier * (float)input.Quantity;
				}
			}
			else
			{
				foreach (CargoFactoryProfileItemOutput output in Outputs)
				{
					num += CalculateItemMinOutputRevenue(ec, output) * (float)output.Quantity;
				}
			}
			float num2 = 0f;
			foreach (CargoFactoryProfileItemInput input2 in Inputs)
			{
				num2 += CalculateItemMaxInputCost(ec, input2) * (float)input2.Quantity;
			}
			return num - num2;
		}

		public float CalculateMaxCostOfInputs(EngineEconomySettings ec)
		{
			float num = 0f;
			foreach (CargoFactoryProfileItemInput input in Inputs)
			{
				num += CalculateItemMaxInputCost(ec, input);
			}
			return num;
		}

		public float CalculateMinOutputRevenue(EngineEconomySettings ec)
		{
			float num = 0f;
			foreach (CargoFactoryProfileItemOutput output in Outputs)
			{
				num += CalculateItemMinOutputRevenue(ec, output);
			}
			return num;
		}

		private float CalculateItemMinOutputRevenue(EngineEconomySettings ec, CargoFactoryProfileItemOutput output)
		{
			return (float)output.CargoClass.BasePrice * ec.CargoFactoryMinSellPrice;
		}

		private float CalculateItemMaxInputCost(EngineEconomySettings ec, CargoFactoryProfileItemInput input)
		{
			return (float)input.CargoClass.BasePrice * ec.CargoFactoryMaxBuyPrice;
		}
	}
}
