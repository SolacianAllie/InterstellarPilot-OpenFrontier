using UnityEngine;

namespace OpenFrontier.IP.Engine.Npcs
{
	public class NpcPiilotArrivalSettings : MonoBehaviour
	{
		public float AIArrive_StoppingDistMultiplier = 1.15f;

		public float AIArrive_MassFudge = 1.5f;

		public float AIArrive_StoppingPower = 32f;

		public float AIArrive_TurnThrottlePower = 16f;

		public float AIArrive_MinArrivalDistance = 40f;

		public float AIArrive_ArrivalDistanceRadiusMultiplier = 2.5f;

		public float AIArrive_DefaultThrottleDotThreshold = 0.6f;

		public float AIArrive_SlowThrottleDotThreshold = 0.8f;
	}
}
