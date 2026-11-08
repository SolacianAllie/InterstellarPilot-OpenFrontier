using OpenFrontier.IP.Engine;
using OpenFrontier.Unity.Utils;

namespace OpenFrontier.IP.Scenarios
{
	public static class FactionObjectiveAssigner
	{
		private class WeightedUnit : IWeighted
		{
			public float Weight { get; set; }

			public Unit Unit { get; set; }
		}
	}
}
