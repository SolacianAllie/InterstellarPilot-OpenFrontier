using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	public class FactionIntelSeederSettings : MonoBehaviour
	{
		public int MaxDiscoveryDistance = 4;

		public int MinWormholeDiscoveryDistance = 2;

		public float ProbabilityOfDiscoveringOtherFaction = 0.15f;

		public float StationProbability = 0.8f;

		public float CargoProbability = 0.5f;

		public float WormholeDiscoveryCost = 0.02f;

		public float WormholeEntryCost = 0.05f;

		public float MinBaseStaticIntel = 1.6f;

		public float MaxBaseStaticIntel = 2f;

		public float BaseStaticIntelPower = 1.5f;

		public float MinBaseNonStaticIntel = 1.6f;

		public float MaxBaseNonStaticIntel = 2f;

		public float BaseNonStaticIntelPower = 1.5f;

		public float FreelancerIntelMultiplier = 0.5f;

		public float MaxStaticIntelSectorRandomness = 10f;
	}
}
