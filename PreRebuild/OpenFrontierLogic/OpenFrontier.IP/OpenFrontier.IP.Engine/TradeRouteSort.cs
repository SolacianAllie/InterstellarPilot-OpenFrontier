using System.Collections.Generic;

namespace OpenFrontier.IP.Engine
{
	public class TradeRouteSort : IComparer<TradeRoute>
	{
		public TradeRouteCalculator Calculator;

		public int Compare(TradeRoute x, TradeRoute y)
		{
			return Calculator.EditorSortMode switch
			{
				TradeRouteCalculatorSortMode.TotalMaxProfit => y.EstimatedTotalProfit.CompareTo(x.EstimatedTotalProfit), 
				TradeRouteCalculatorSortMode.UnitsAvailable => y.UnitsAvailable.CompareTo(x.UnitsAvailable), 
				_ => y.ProfitPerUnit.CompareTo(x.ProfitPerUnit), 
			};
		}
	}
}
