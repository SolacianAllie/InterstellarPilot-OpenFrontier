using UnityEngine;

namespace OpenFrontier.IP.Engine.Missions
{
	public class BreakdownMissionSettings : MonoBehaviour
	{
		public float RequiredDistanceFromBreakdownUnit = 75f;

		public float RequiredDistanceFromBase = 150f;

		public float MinDistanceFromMissionUnit = 800f;

		public int MaxJumpDistanceFromMissionGiver = 8;

		public float MaxJumpDistancePower = 4f;
	}
}
