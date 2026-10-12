using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	public class FactionSeederSettings : MonoBehaviour
	{
		public bool SeedNonBanditFactions = true;

		public bool SeedBanditFactions = true;

		public bool SeedFreelancers = true;

		public float BanditPower01 = 0.5f;

		public float NonBanditPower01 = 0.5f;

		public bool SuperchargedBanditsEnabled;

		public float GetBanditCreditsPower(float defaultPower)
		{
			return Mathf.Lerp(defaultPower * 2f, defaultPower / 2f, BanditPower01);
		}

		public float GetNonBanditCreditsPower(float defaultPower)
		{
			return Mathf.Lerp(defaultPower * 2f, defaultPower / 2f, NonBanditPower01);
		}
	}
}
