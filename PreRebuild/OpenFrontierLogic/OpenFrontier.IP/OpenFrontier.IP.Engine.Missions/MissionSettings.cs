using UnityEngine;

namespace OpenFrontier.IP.Engine.Missions
{
	public class MissionSettings : MonoBehaviour
	{
		public CourierMissionSettings CourierMissionSettings;

		public BreakdownMissionSettings BreakdownMissionSettings;

		public float CompletionOpinionChangeReferenceValue = 350000f;

		public float CompletionMinOpinionChange = 0.005f;

		public float MissionProfitabilityPower = 2f;

		public float MissionsMaxRewardRandomness = 3f;
	}
}
