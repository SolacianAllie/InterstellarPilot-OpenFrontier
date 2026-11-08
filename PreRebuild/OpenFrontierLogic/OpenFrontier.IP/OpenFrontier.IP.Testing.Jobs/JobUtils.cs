using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.MissionSpecs;
using UnityEngine;

namespace OpenFrontier.IP.Testing.Jobs
{
	public static class JobUtils
	{
		public static void RecalculateAllJobRewards()
		{
			foreach (MissionSpec job in EngineASX.Instance.Jobs)
			{
				if (job != null)
				{
					float profitability = Mathf.Pow(Random.value, GameController.Instance.GameSettings.MissionSettings.MissionProfitabilityPower);
					job.RewardCredits = job.CalculateRewardCredits(profitability);
					job.ProfitCredits = job.CalculateProfitCredits(profitability);
				}
			}
		}

		public static void RecalculateAllJobRewardsLowProfit()
		{
			foreach (MissionSpec job in EngineASX.Instance.Jobs)
			{
				if (job != null)
				{
					job.RewardCredits = job.CalculateRewardCredits(0f);
					job.ProfitCredits = job.CalculateProfitCredits(0f);
				}
			}
		}
	}
}
