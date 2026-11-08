using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionAIStrategy
	{
		private FactionStrategy primaryStrategy;

		private Dictionary<FactionStrategy, FactionAIStrategyItem> actionableStrategies = new Dictionary<FactionStrategy, FactionAIStrategyItem>(8);

		private float totalWeight;

		private FactionStrategy strategyFlags;

		private FactionStrategy actionableStrategyFlags;

		private List<FactionAIStrategyItem> strategies = new List<FactionAIStrategyItem>(4);

		public FactionStrategy PrimaryStrategy => primaryStrategy;

		public List<FactionAIStrategyItem> Strategies => strategies;

		public float TotalWeight => totalWeight;

		public FactionStrategy StrategyFlags => strategyFlags;

		public FactionStrategy ActionableStrategyFlags => actionableStrategyFlags;

		public Dictionary<FactionStrategy, FactionAIStrategyItem> ActionableStrategies => actionableStrategies;

		public void Compile()
		{
			strategyFlags = FactionStrategy.Unspecified;
			totalWeight = 0f;
			primaryStrategy = FactionStrategy.Unspecified;
			float num = 0f;
			actionableStrategyFlags = FactionStrategy.Unspecified;
			actionableStrategies.Clear();
			foreach (FactionAIStrategyItem strategy in strategies)
			{
				if (!(strategy.Weight > 0f))
				{
					continue;
				}
				totalWeight += strategy.Weight;
				strategyFlags |= strategy.Strategy;
				if (primaryStrategy == FactionStrategy.Unspecified || strategy.Weight > num)
				{
					primaryStrategy = strategy.Strategy;
					num = strategy.Weight;
				}
				if (strategy.CanAssignToFleet)
				{
					actionableStrategyFlags |= strategy.Strategy;
					if (!actionableStrategies.ContainsKey(strategy.Strategy))
					{
						actionableStrategies.Add(strategy.Strategy, strategy);
					}
				}
			}
		}

		public bool IsStrategyActionable(FactionStrategy strategy)
		{
			foreach (FactionAIStrategyItem strategy2 in strategies)
			{
				if (strategy2.Strategy == strategy && strategy2.CanAssignToFleet)
				{
					return true;
				}
			}
			return false;
		}

		public FactionAIStrategyItem? GetActionableStrategyItem(FactionStrategy strategy)
		{
			foreach (FactionAIStrategyItem strategy2 in strategies)
			{
				if (strategy2.Strategy == strategy && strategy2.CanAssignToFleet)
				{
					return strategy2;
				}
			}
			return null;
		}
	}
}
