using UnityEngine;

namespace OpenFrontier.IP.Engine.Settings
{
	public class NpcTargetSearchSettings : MonoBehaviour
	{
		public float AITargetSearchInFrontScore = 4f;

		public float AITargetSearchExistingScore = 2f;

		public float AITargetSearchDistanceScore = 5f;

		public float AITargetSearchPositiveDistanceScore = 5f;

		public float AITargetSearchNegativeDistanceScore = -15f;

		public float AITargetSearchNegativeScoreLowerDistance = 500f;

		public float AITargetSearchNegativeScoreUpperDistance = 4000f;

		public float AITargetSearchTurretScore = 2.5f;

		public float AITargetSearchArmedScore = 5f;

		public float AITargetSearchArmedPilottedScore = 5f;

		public float AITargetSearchClassScore = 0.5f;

		public float AITargetSearchRecentAttacksBaseScore = 5f;

		public float AITargetSearchAttackingFleetScore = 5f;

		public float AITargetSearchAttackingFactionScore = 5f;

		public float AITargetSearchScoreRandomness = 2f;

		public float AITargetSearchLowCombatRatingNegativeScore = -9f;

		public float InterceptTargetScoreThreshold = 60f;

		public float AttackTargetScoreThreshold = 20f;
	}
}
