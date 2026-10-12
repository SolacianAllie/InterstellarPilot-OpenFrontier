using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public static class PhysicsNonOverlappingPositionFinder
	{
		public static int NumDirectionChecks = 6;

		private const float IncrementMultiplier = 1.5f;

		public static Vector3 FindSectorPosition(Sector sector, Vector3 checkSectorPosition, float radius, LayerMask layerMask, int iterations = 4)
		{
			Vector3? vector = FindSectorPositionOrNull(sector, checkSectorPosition, radius, layerMask, iterations);
			if (vector.HasValue)
			{
				return vector.Value;
			}
			return checkSectorPosition;
		}

		public static Vector3? FindSectorPositionOrNull(Sector sector, Vector3 checkSectorPosition, float radius, LayerMask layerMask, int iterations = 4)
		{
			if (IsOverlapping(sector.ToWorldPosition(checkSectorPosition), radius, layerMask))
			{
				radius *= 1.1f;
				float num = Mathf.Max(10f, radius * 1.5f);
				int num2 = Random.Range(0, NumDirectionChecks);
				for (int i = 0; i < iterations; i++)
				{
					for (int j = 0; j < NumDirectionChecks; j++)
					{
						int num3 = Maths.WrapValue(num2 + j, 0, NumDirectionChecks);
						Vector3 vector = checkSectorPosition + Quaternion.Euler(0f, (float)num3 / (float)NumDirectionChecks * 360f, 0f) * (Vector3.forward * num);
						if (UnitOutOfBoundsValidator.IsLocalPositionWithinBounds(EngineASX.Instance.GameSettings.UniverseBoundsSettings.MaxUnitDistanceFromOriginUpperBound - radius, vector) && !IsOverlapping(sector.ToWorldPosition(vector), radius, layerMask))
						{
							return vector;
						}
					}
					num += (40f + radius) * 1.5f;
				}
				if (LogWrapper.LogMsgs)
				{
					Debug.LogWarning($"Max attempts {iterations} reached when trying to find safe deployment position in {sector.Name} at position {checkSectorPosition} with radius: {radius}");
				}
				return null;
			}
			return checkSectorPosition;
		}

		public static bool IsOverlapping(Vector3 position, float radius, LayerMask layerMask)
		{
			return Physics.CheckSphere(position, radius, layerMask, QueryTriggerInteraction.Collide);
		}
	}
}
