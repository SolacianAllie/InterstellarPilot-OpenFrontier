using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public static class TraderCargoTrimmer
	{
		public static bool Trim(CargoTrader trader)
		{
			bool result = false;
			if (trader.Unit.CargoBayComponent.UsagePercent > EngineASX.Instance.EconomySettings.TrimTraderCargoThresholdCargoUsage)
			{
				foreach (KeyValuePair<int, CargoTraderStockLevels> stockLevel in trader.GetStockLevels())
				{
					CargoClass cargoClassById = trader.Unit.Engine.GetCargoClassById(stockLevel.Key);
					int count = Mathf.RoundToInt((float)trader.UnitComponents.CargoBayComponent.GetCountOf(cargoClassById) * EngineASX.Instance.EconomySettings.TrimTraderCargoTrimLevel);
					trader.UnitComponents.CargoBayComponent.SetCount(cargoClassById, count);
					result = true;
				}
			}
			return result;
		}
	}
}
