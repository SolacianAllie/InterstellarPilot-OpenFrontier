using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	public class CreateAsteroidClustersSeederSettings : MonoBehaviour
	{
		public int MinAsteroidClusterCount = 3;

		public int MaxAsteroidClusterCount = 5;

		public float MinDistanceBetweenAsteroidClusters = 1700f;

		public float MinAsteroidClusterRadius = 2500f;

		public float MaxAsteroidClusterRadius = 4000f;

		public float AsteroidClusterRadiusPower = 1f;

		// Coin flip per cluster: belts should sometimes have a gas cloud,
		// sometimes not. (Was 0.75 - with several clusters per sector that
		// made "at least one gassy belt" a near-guarantee.)
		public float ProbabilityOfGeneratingGasCloud = 0.5f;
	}
}
