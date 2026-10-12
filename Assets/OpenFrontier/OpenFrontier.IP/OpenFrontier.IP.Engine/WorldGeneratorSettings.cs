using System.Collections.Generic;
using OpenFrontier.IP.Engine.WorldPopulation;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class WorldGeneratorSettings : MonoBehaviour
	{
		public float LineOvershootDistance = 60f;

		public WorldPopulatorSectorTypeSettings SectorTypeSettings;

		public int SeedValue = -1;

		[SerializeField]
		private bool snapNodesEnabled = true;

		[SerializeField]
		private int minNumScenes = 8;

		[SerializeField]
		private int maxNumScenes = 10;

		[SerializeField]
		private float snakiness = 0.2f;

		[SerializeField]
		private float minGateDistanceMultiplier = 0.75f;

		[SerializeField]
		private float maxGateDistanceMultiplier = 1.25f;

		[SerializeField]
		private int maxConnections = 4;

		[SerializeField]
		private float minAngleBetweenGates = 22.5f;

		[SerializeField]
		private float minDistanceBetweenSectors = 100f;

		[SerializeField]
		private float maxDistanceBetweenSectors = 150f;

		[SerializeField]
		private float minSectorSnapRadius = 75f;

		[SerializeField]
		private float maxSectorSnapRadius = 130f;

		[SerializeField]
		private float minSectorDistance = 10f;

		public List<AsteroidType> AsteroidTypes;

		public float AsteroidTypeWeightRandomness = 0.2f;

		public int MinNumScenes
		{
			get
			{
				return minNumScenes;
			}
			set
			{
				minNumScenes = value;
			}
		}

		public int MaxNumScenes
		{
			get
			{
				return maxNumScenes;
			}
			set
			{
				maxNumScenes = value;
			}
		}

		public float Snakiness
		{
			get
			{
				return snakiness;
			}
			set
			{
				snakiness = value;
			}
		}

		public float MinSectorSnapRadius
		{
			get
			{
				return minSectorSnapRadius;
			}
			set
			{
				minSectorSnapRadius = value;
			}
		}

		public float MaxSectorSnapRadius
		{
			get
			{
				return maxSectorSnapRadius;
			}
			set
			{
				maxSectorSnapRadius = value;
			}
		}

		public float MinGateDistanceMultiplier
		{
			get
			{
				return minGateDistanceMultiplier;
			}
			set
			{
				minGateDistanceMultiplier = value;
			}
		}

		public float MaxGateDistanceMultiplier
		{
			get
			{
				return maxGateDistanceMultiplier;
			}
			set
			{
				maxGateDistanceMultiplier = value;
			}
		}

		public int MaxConnections
		{
			get
			{
				return maxConnections;
			}
			set
			{
				maxConnections = value;
			}
		}

		public float MinAngleBetweenGates
		{
			get
			{
				return minAngleBetweenGates;
			}
			set
			{
				minAngleBetweenGates = value;
			}
		}

		public float MaxDistanceBetweenSectors
		{
			get
			{
				return maxDistanceBetweenSectors;
			}
			set
			{
				maxDistanceBetweenSectors = value;
			}
		}

		public float MinDistanceBetweenSectors
		{
			get
			{
				return minDistanceBetweenSectors;
			}
			set
			{
				minDistanceBetweenSectors = value;
			}
		}

		public float MinSectorDistance
		{
			get
			{
				return minSectorDistance;
			}
			set
			{
				minSectorDistance = value;
			}
		}

		public bool SnapNodesEnabled
		{
			get
			{
				return snapNodesEnabled;
			}
			set
			{
				snapNodesEnabled = value;
			}
		}
	}
}
