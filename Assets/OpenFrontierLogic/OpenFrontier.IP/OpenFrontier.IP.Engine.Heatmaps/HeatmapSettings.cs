using UnityEngine;

namespace OpenFrontier.IP.Engine.Heatmaps
{
	public class HeatmapSettings : MonoBehaviour
	{
		public int HeatmapCellSize = 100;

		public float MaxCombatRatingReferenceValue = 10f;

		public float ThreatDissipationRatePerSecond = 0.0016666667f;

		public int NewThreatCellRange = 3;

		public float HostileTargetStaleTime = 60f;
	}
}
