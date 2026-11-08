using System;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier
{
	public static class Maths
	{
		public static float WrapValue(float value, float min, float max)
		{
			if (min >= max)
			{
				throw new ArgumentException("Minimum value should be smaller than the maximum");
			}
			if (value < min || value >= max)
			{
				float num = max - min;
				float num2 = (value - min) % num;
				if (num2 < 0f)
				{
					return num2 + max;
				}
				return num2 + min;
			}
			return value;
		}

		public static double Lerp(double minTimeBeforeAsteroidRespawnLower, double minTimeBeforeAsteroidRespawnUpper, double asteroidRespawnTime)
		{
			return minTimeBeforeAsteroidRespawnLower + (minTimeBeforeAsteroidRespawnUpper - minTimeBeforeAsteroidRespawnLower) * Math.Clamp(asteroidRespawnTime, 0.0, 1.0);
		}

		public static float CalculateStoppingDistance(float spd, float deceleration)
		{
			return spd * spd / (2f * deceleration) * Mathf.Sign(spd);
		}

		public static int WrapValue(int value, int min, int max)
		{
			if (min >= max)
			{
				throw new ArgumentException("Minimum value should be smaller than the maximum");
			}
			if (value < min || value >= max)
			{
				int num = max - min;
				int num2 = (value - min) % num;
				if (num2 < 0)
				{
					return num2 + max;
				}
				return num2 + min;
			}
			return value;
		}

		public static float ChangeValue(float current, float desired, float delta)
		{
			if (Mathf.Abs(current - desired) <= delta)
			{
				return desired;
			}
			if (current > desired)
			{
				return current - delta;
			}
			return current + delta;
		}

		public static Quaternion RandomYRotation()
		{
			return Quaternion.Euler(0f, UnityEngine.Random.value * 360f, 0f);
		}

		public static Vector3 RandomBoxPosition(float width, float height, float length)
		{
			return new Vector3((0f - width) / 2f + width * UnityEngine.Random.value, (0f - height) / 2f + height * UnityEngine.Random.value, (0f - length) / 2f + length * UnityEngine.Random.value);
		}

		public static float MaxComponent(ref Vector3 vector)
		{
			return Mathf.Max(Mathf.Max(vector.x, vector.y), vector.z);
		}

		public static float Sqr(float a)
		{
			return a * a;
		}

		public static Vector3 RandomXZDirection()
		{
			float f = UnityEngine.Random.value * MathF.PI * 2f;
			return new Vector3(Mathf.Sin(f), 0f, Mathf.Cos(f));
		}

		public static float Sine01(float val)
		{
			return (Mathf.Sin(val) + 1f) / 2f;
		}

		public static int RoundUpToInt(float val, int rounding)
		{
			return Mathf.CeilToInt(val / (float)rounding) * rounding;
		}

		public static int RoundToInt(float val, int rounding)
		{
			return Mathf.RoundToInt(val / (float)rounding) * rounding;
		}

		public static int RandomIntWithPower(int min, int max, float power)
		{
			return Mathf.RoundToInt(Mathf.Lerp(min, max, Mathf.Pow(UnityEngine.Random.value, power)));
		}

		public static int RandomIntWithPower(System.Random random, int min, int max, float power)
		{
			return Mathf.RoundToInt(Mathf.Lerp(min, max, Mathf.Pow((float)random.NextDouble(), power)));
		}

		public static float RandomFloatWithPower(float min, float max, float power)
		{
			return Mathf.Lerp(min, max, Mathf.Pow(UnityEngine.Random.value, power));
		}

		public static float RandomFloatWithPower(System.Random random, float min, float max, float power)
		{
			return Mathf.Lerp(min, max, Mathf.Pow((float)random.NextDouble(), power));
		}

		public static float GetDistanceIgnoreY(Vector3 p1, Vector3 p2)
		{
			return Vector2.Distance(new Vector2(p1.x, p1.z), new Vector2(p2.x, p2.z));
		}

		public static float GetDistanceIgnoreY(ref Vector3 p1, ref Vector3 p2)
		{
			return Vector2.Distance(new Vector2(p1.x, p1.z), new Vector2(p2.x, p2.z));
		}
	}
}
