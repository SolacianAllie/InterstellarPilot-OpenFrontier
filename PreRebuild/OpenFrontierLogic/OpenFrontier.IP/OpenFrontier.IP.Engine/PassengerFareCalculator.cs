using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public static class PassengerFareCalculator
	{
		public static int CalculateFare(Unit source, Unit destination, int passengerCount, PassengerFareSettings settings)
		{
			float num = 0f;
			float? distance = EngineASX.Instance.DistanceCalculator.GetDistance(source, destination);
			float num2 = (float)passengerCount * settings.RewardPerPersonMultiplier;
			if (distance.HasValue)
			{
				num = (distance.Value * settings.CreditsPerDistUnit + (float)settings.BaseCredits) * num2;
			}
			else
			{
				Debug.LogErrorFormat("Could not determine passenger fare. Could not determine distance from unit {0} to unit {1}", source, destination);
				num = (float)settings.BaseCredits * num2;
			}
			if (destination.Sector.SecurityLevel < 0.5f)
			{
				float num3 = 1f - destination.Sector.SecurityLevel / 0.5f;
				num += num * num3 * settings.LowSecurityRewardBonusMultiplier;
			}
			if (destination.IsShip())
			{
				num *= settings.RewardForShipTargetMultiplier;
			}
			float t = Mathf.Pow(Random.value, settings.RandomMultiplierPower);
			float num4 = Mathf.Lerp(1f, settings.MaxRandomMultiplier, t);
			return Maths.RoundToInt(num * num4, settings.Rounding);
		}
	}
}
