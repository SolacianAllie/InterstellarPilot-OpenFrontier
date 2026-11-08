using System;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier.Unity.Utils
{
	public static class RandExtensions
	{
		public static float NextFloat(this System.Random random)
		{
			return (float)random.NextDouble();
		}

		public static float NextFloat(this System.Random random, float minValue, float maxValue)
		{
			return minValue + (float)random.NextDouble() * (maxValue - minValue);
		}

		public static Quaternion RandomQuaternion(this System.Random random)
		{
			return Quaternion.Euler(random.NextFloat() * 360f, random.NextFloat() * 360f, random.NextFloat() * 360f);
		}
	}
}
