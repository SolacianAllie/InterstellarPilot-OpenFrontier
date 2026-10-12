using UnityEngine;

namespace OpenFrontier.IP.Engine.FactionOpinionNeutralizer
{
	public class FactionOpinionNeutralizerSettings : MonoBehaviour
	{
		public float TimeBeforeOpinionNeutralization = 60f;

		public float OpinionChangePerHour = 0.01f;

		public int ProcessedAttitudesPerSecond = 240;
	}
}
