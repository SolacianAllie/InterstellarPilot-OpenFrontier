using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.Testing
{
	public static class UnitMassUtils
	{
		public const float MinMass = 0.05f;

		public const float MaxMass = 1000f;

		public static void ChangeUnitMassWithMultiplier(Unit unit, float multiplier)
		{
			unit.Mass = Mathf.Clamp(unit.Mass * multiplier, 0.05f, 1000f);
			if (unit.RBody != null)
			{
				unit.RBody.mass = unit.Mass;
			}
		}
	}
}
