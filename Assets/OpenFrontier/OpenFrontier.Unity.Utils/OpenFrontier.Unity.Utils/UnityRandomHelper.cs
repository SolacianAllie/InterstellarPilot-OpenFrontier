using UnityEngine;

namespace OpenFrontier.Unity.Utils
{
	public static class UnityRandomHelper
	{
		public static bool RandBoolean()
		{
			return Random.Range(0, 2) == 0;
		}

		public static float RandomSign()
		{
			if (Random.Range(0, 2) == 0)
			{
				return 1f;
			}
			return -1f;
		}

		public static int RandomRangeWithPower(int min, int max, float power)
		{
			return Mathf.RoundToInt(Mathf.Lerp(min, max, Mathf.Pow(Random.value, power)));
		}

		public static Quaternion RandomYRotation()
		{
			return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
		}
	}
}
