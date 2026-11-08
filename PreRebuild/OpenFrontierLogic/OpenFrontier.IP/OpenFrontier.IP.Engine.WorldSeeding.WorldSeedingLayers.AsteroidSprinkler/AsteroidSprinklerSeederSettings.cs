using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.AsteroidSprinkler
{
	public class AsteroidSprinklerSeederSettings : MonoBehaviour
	{
		public float SectorSprinkleProbability = 0.1f;

		public int MinClusterCount = 1;

		public int MaxClusterCount = 2;

		public float ClusterCountPower = 2f;

		public int MinAsteroidsInClusterCount = 1;

		public int MaxAsteroidsInClusterCount = 3;

		public float AsteroidsInClusterPower = 2f;

		public float MinDistanceFromOtherStaticObjects = 1500f;

		public float MaxAsteroidDistanceFromCluster = 1000f;

		public float AsteroidDistanceFromClusterPower = 2f;

		public float ProbabilityOfClusterFringePosition = 0.4f;
	}
}
