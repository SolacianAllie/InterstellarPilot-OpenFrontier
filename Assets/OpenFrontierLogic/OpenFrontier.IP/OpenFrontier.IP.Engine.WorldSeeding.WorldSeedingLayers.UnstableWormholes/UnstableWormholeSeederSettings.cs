using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.UnstableWormholes
{
	public class UnstableWormholeSeederSettings : MonoBehaviour
	{
		public float UnstableWormholeToSectorRatio = 0.2f;

		public UnitClass WormholeUnitClass;

		public float MinGateDistanceMultiplier = 1.1f;

		public float MaxGateDistanceMultiplier = 1.5f;

		public float MinDistanceFromOtherWormholesAndStations = 800f;

		public int MinDesignationRandomValue = 156;

		public int MaxDesignationRandomValue = 804;

		public float ProbabilityOfGreekAlphabetSuffix = 0.7f;
	}
}
