using OpenFrontier.IP.Common.Factions;
using OpenFrontier.Unity.Utils;

namespace OpenFrontier.IP.Engine.Factions
{
	public struct FactionAIStrategyItem : IWeighted
	{
		public FactionStrategy Strategy { get; set; }

		public float Weight { get; set; }

		public bool CanAssignToFleet { get; set; }

		public FactionAIStrategyItem(FactionStrategy strategy, float weight, bool canAssignToFleet)
		{
			this = default;
			Strategy = strategy;
			Weight = weight;
			CanAssignToFleet = canAssignToFleet;
		}
	}
}
