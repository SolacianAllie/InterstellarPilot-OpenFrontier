using System;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Core.Units
{
	[Serializable]
	public class UnitThrusterParticleSystem
	{
		public float MinEmissionRate;

		public ParticleSystem ParticleSystemPrefab;

		public float DrawDistanceMultiplier = 1f;

		/// <summary>
		/// The particle prefab's authored startSize, logged down so it can be
		/// added up. Zero for anything that is not a thruster.
		/// </summary>
		/// <remarks>
		/// This is where the infrared seeker's picture of a ship comes from, and
		/// it is logged down rather than read live because startSize belongs to
		/// a ParticleSystemMainModule on a prefab asset, which cannot be
		/// interrogated at runtime without instantiating it. The values are real
		/// - 0.5 for the micro thruster up to 5 for the huge one - so hull size
		/// is a measurement rather than a naming convention.
		///
		/// Zero for a smoke effect, deliberately. Smoke is not an engine, and
		/// counting it wrecks the ordering completely: the large and very large
		/// thrusters both carry a smoke puff, and including it made VeryLarge
		/// read cooler than Large and OrangeLarge hotter than Huge.
		/// </remarks>
		public float HeatSize;
	}
}
