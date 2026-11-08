using System.Collections.Generic;
using OpenFrontier.IP.Engine.GasClouds;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	public class AbandonedShipsSeederSettings : MonoBehaviour
	{
		public List<GasCloudClass> SpawnInGasCloudClasses;

		public float ProbabilityOfSpawnInGasCloud = 0.8f;

		public float ProbabilityOfSpawnNearBanditStation = 0.5f;

		public float BaseProbability;

		public float LowSecurityProbability = 0.15f;

		public float FringeSectorProbability = 0.15f;

		public int MaxIterationsPerSector = 3;

		public float MaxShipHealth = 0.3f;

		public float MinShipHealth = 0.1f;

		public float MinComponentHealth = 0.05f;

		public float MaxComponentHealth = 0.4f;

		public float MinShieldHealth;

		public float MaxShieldHealth = 0.25f;

		public float ProbabilityOfShieldBeingUp = 0.4f;

		public float GateDistanceMultiplierPower = 0.4f;

		public float MaxSectorSecurity = 0.5f;

		public float MinShipHealthPower = 0.5f;

		public float MaxShipHealthPower = 2f;

		public float SmallShipFactor = 4f;

		public float ProbabilityOfAllComponentsOff = 0.75f;

		public float ProbabilityOfIndividualComponentsOff = 0.5f;

		public float MinDistanceFromOtherShipsAndStations = 2000f;
	}
}
