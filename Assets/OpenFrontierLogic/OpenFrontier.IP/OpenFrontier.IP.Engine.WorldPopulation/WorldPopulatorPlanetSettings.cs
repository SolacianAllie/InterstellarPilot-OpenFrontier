using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldPopulation
{
	public class WorldPopulatorPlanetSettings : MonoBehaviour
	{
		public List<Unit> PlanetPrefabs;

		public List<Unit> MoonPrefabs;

		public int MinPlanets = 1;

		public int MaxPlanets = 1;

		public float ProbabilityOfPlanetMoon = 1f;

		public float MinPlanetPositionY = -0.2f;

		public float MaxPlanetPositionY = 0.2f;

		public float MinPlanetDistance = 12f;

		public float MaxPlanetDistance = 30f;

		public float MinMoonDistanceFromPlanet = 20f;

		public float MaxMoonDistanceFromPlanet = 20f;

		public float MaxMoonAngleXFromPlanet = 30f;
	}
}
