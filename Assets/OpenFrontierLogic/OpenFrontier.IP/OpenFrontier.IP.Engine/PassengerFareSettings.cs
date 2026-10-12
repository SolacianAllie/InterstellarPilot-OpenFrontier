using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class PassengerFareSettings : MonoBehaviour
	{
		public int BaseCredits = 150;

		public float CreditsPerDistUnit = 0.25f;

		public int Rounding = 10;

		public float MaxRandomMultiplier = 2f;

		public float RandomMultiplierPower = 8f;

		public float RewardPerPersonMultiplier = 1.75f;

		public float LowSecurityRewardBonusMultiplier = 3f;

		public float RewardForShipTargetMultiplier = 2f;
	}
}
