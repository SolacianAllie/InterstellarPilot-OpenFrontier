using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public static class AICombatProbabilityCalculator
	{
		public static float GetSimpleFleetProbabilityAgainstUnitOrUnitFleet(Fleet outFleet, Unit foreignUnit)
		{
			Fleet fleet = foreignUnit.GetFleet();
			float num = 0f;
			num = ((!(fleet != null)) ? foreignUnit.CombatRating : fleet.GetCachedSimpleCombatRating());
			return GetSimpleFleetProbabilityAgainstRating(outFleet, num);
		}

		public static float GetSimpleFleetProbabilityAgainstFleet(Fleet fleet, Fleet otherFleet)
		{
			float cachedSimpleCombatRating = otherFleet.GetCachedSimpleCombatRating();
			return GetSimpleFleetProbabilityAgainstRating(fleet, cachedSimpleCombatRating);
		}

		public static float GetSimpleFleetProbabilityAgainstRating(Fleet fleet, float enemyCombatRating)
		{
			float cachedSimpleCombatRating = fleet.GetCachedSimpleCombatRating();
			float num = 2f;
			return Mathf.Clamp(cachedSimpleCombatRating / enemyCombatRating, 0f, num) / num;
		}
	}
}
