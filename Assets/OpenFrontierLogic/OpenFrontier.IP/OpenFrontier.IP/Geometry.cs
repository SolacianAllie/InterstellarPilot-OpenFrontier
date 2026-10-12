using System;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier.IP
{
	public static class Geometry
	{
		public static float GetTurn(float relativeBearing, float turnRate)
		{
			float num = Mathf.Abs(relativeBearing);
			if (turnRate > num)
			{
				return num * Mathf.Sign(relativeBearing);
			}
			return turnRate * Mathf.Sign(relativeBearing);
		}

		public static float WrapRadiansForBearing(float radians)
		{
			return Maths.WrapValue(radians, -MathF.PI, MathF.PI);
		}

		public static float WrapDegreesForBearing(float degrees)
		{
			return Maths.WrapValue(degrees, -180f, 180f);
		}

		public static float? CalculateProjectileHeading(float bearingToTarget, float angleAtBow, float targetSpeed, float projectileSpeed)
		{
			if (targetSpeed < 0f || projectileSpeed < 0f)
			{
				throw new ArgumentOutOfRangeException("Invalid Speed");
			}
			if (targetSpeed == 0f)
			{
				return bearingToTarget;
			}
			float num = targetSpeed / (projectileSpeed / Mathf.Sin(angleAtBow));
			if (num > 1f || num < -1f)
			{
				return null;
			}
			float num2 = Mathf.Asin(num);
			return WrapRadiansForBearing(bearingToTarget + num2);
		}

		public static bool GetInteceptPosition(ref Vector3 sourcePosition, float sourceSpd, ref Vector3 target, ref Vector3 targetVelocity, out Vector3 interception)
		{
			interception = sourcePosition;
			float num = Mathf.Pow(targetVelocity.x, 2f) + Mathf.Pow(targetVelocity.y, 2f) + Mathf.Pow(targetVelocity.z, 2f) - Mathf.Pow(sourceSpd, 2f);
			float num2 = 2f * (targetVelocity.x * (target.x - sourcePosition.x) + targetVelocity.y * (target.y - sourcePosition.y) + targetVelocity.z * (target.z - sourcePosition.z));
			float num3 = Maths.Sqr(target.x - sourcePosition.x) + Maths.Sqr(target.y - sourcePosition.y) + Maths.Sqr(target.z - sourcePosition.z);
			float num4 = Mathf.Pow(num2, 2f) - 4f * num * num3;
			if (num4 == 0f)
			{
				return true;
			}
			if (num4 < 0f)
			{
				return false;
			}
			float num5 = Mathf.Sqrt(num4);
			float num6 = 2f * num;
			float num7 = (0f - num2 + num5) / num6;
			float num8 = (0f - num2 - num5) / num6;
			float num9 = ((num7 < num8) ? num8 : num7);
			if (num9 > 0f)
			{
				interception.x = num9 * targetVelocity.x + target.x;
				interception.y = num9 * targetVelocity.y + target.y;
				interception.z = num9 * targetVelocity.z + target.z;
				return true;
			}
			return false;
		}

		public static void RotateVectorRandomlyXY(ref Vector3 directionVector, float minAngle, float maxAngle)
		{
			Quaternion quaternion = Quaternion.Euler(UnityEngine.Random.Range(minAngle, maxAngle), UnityEngine.Random.Range(minAngle, maxAngle), 0f);
			directionVector = quaternion * directionVector;
		}

		public static void RotateVectorRandomlyY(ref Vector3 directionVector, float minAngle, float maxAngle)
		{
			Quaternion quaternion = Quaternion.Euler(0f, UnityEngine.Random.Range(minAngle, maxAngle), 0f);
			directionVector = quaternion * directionVector;
		}

		public static Quaternion RandomYRotation()
		{
			return Quaternion.Euler(0f, UnityEngine.Random.value * 360f, 0f);
		}

		public static Vector3 RandomXZUnitVector()
		{
			return Quaternion.Euler(0f, UnityEngine.Random.value * 360f, 0f) * Vector3.forward;
		}

		public static Vector3 RandomXZUnitVector(System.Random random)
		{
			return Quaternion.Euler(0f, random.NextFloat() * 360f, 0f) * Vector3.forward;
		}
	}
}
