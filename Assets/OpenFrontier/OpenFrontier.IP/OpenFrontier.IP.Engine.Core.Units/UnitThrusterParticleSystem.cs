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
	}
}
