using System;
using UnityEngine;

namespace OpenFrontier.IP.Engine.CargoFactory
{
	[Serializable]
	public class CargoFactoryProfileItemInput : CargoFactoryProfileItemComponent
	{
		public int CalculateConsumerRevenue(EngineEconomySettings economySettings, float multiplier = 1f)
		{
			return Mathf.RoundToInt((float)(Mathf.Abs(Quantity) * CargoClass.BasePrice) * economySettings.ConsumerRevenueMultiplier * multiplier);
		}
	}
}
