using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class FactionDiscoverySeedSettings : MonoBehaviour
	{
		public int MaxDiscoveryDistance = 7;

		public int MinWormholeDiscoveryDistance = 2;

		public float ProbabilityOfDiscoveringOtherFaction = 0.3f;

		public float AsteroidProbability = 0.7f;

		public float StationProbability = 0.8f;

		public float CargoProbability = 0.5f;

		public float WormholeProbability = 0.8f;
	}
}
