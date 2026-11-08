using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	public class CreateGasCloudsSeederSettings : MonoBehaviour
	{
		public List<Unit> UnitGasCloudPrefabs = new List<Unit>();

		public float Probability = 0.4f;

		public bool CreateInPlanetSectors;

		public bool CreateInAsteroidSectors;

		public float MinGateDistanceMultiplier;

		public float MaxGateDistanceMultiplier = 0.2f;

		public float MinRadius = 1500f;

		public float MaxRadius = 2500f;
	}
}
