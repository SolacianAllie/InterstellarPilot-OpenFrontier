using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.AsteroidDepleter
{
	public class AsteroidDepleterSeederSettings : MonoBehaviour
	{
		public float DestructionDistanceThreshold = 1500f;

		public float DepletionDistanceThresholdLower = 1501f;

		public float DepletionDistanceThresholdUpper = 1501f;

		public float MinDepletionPercentage = 0.1f;

		public float MaxDepletionPercentage = 0.5f;

		public float DepletionPercentagePower = 2f;
	}
}
