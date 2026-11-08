using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	public class AsteroidYieldSeederSettings : MonoBehaviour
	{
		public float BaseProbabilityOfYield = 0.05f;

		public int MaxYieldIterations = 4;

		public float LowSecurityProbability = 0.6f;

		public float UnclaimedSectorProbability = 0.1f;
	}
}
