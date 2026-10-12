using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class AsteroidClass : MonoBehaviour
	{
		public List<Transform> RandomYieldPositions = new List<Transform>();

		public AsteroidType AsteroidType;

		public float ProbabilityOfYield = 0.2f;

		public int MaxYieldQuantity = 1;

		public int MinYieldQuantity = 1;

		public int MinTotalYield = 1000;

		public int MaxTotalYield = 2000;

		public ExplosionClass YieldExplosionClass;

		public List<AsteroidYieldItem> AsteroidYieldItems;
	}
}
