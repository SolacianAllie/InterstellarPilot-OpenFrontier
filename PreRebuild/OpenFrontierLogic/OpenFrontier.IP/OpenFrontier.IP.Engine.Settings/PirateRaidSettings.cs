using UnityEngine;

namespace OpenFrontier.IP.Engine.Settings
{
	public class PirateRaidSettings : MonoBehaviour
	{
		public float MinScoreBeforeAttack;

		public float RaidOpinionFactor = 4f;

		public float RaidCargoValueFactor = 20f;

		public float MinTimeBeforeRescan = 60f;

		public float MaxTimeBeforeRescan = 200f;

		public float MinRaidableCargoValue = 500f;

		public float BestRaidableCargoValueReference = 100000f;

		public float BestRaidableCargoValueReferenceStation = 500000f;

		public float ChanceOfConsideringRaid = 0.8f;

		public float ScoreFudge = -0.1f;

		public float RandomScoreFudge = -0.1f;
	}
}
