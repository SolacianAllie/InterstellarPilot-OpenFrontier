using UnityEngine;

namespace OpenFrontier.IP.Engine.Settings
{
	public class DitchUnitSettings : MonoBehaviour
	{
		public float MaxHullThreshold = 0.3f;

		public float ProbabilityMultiplier = 0.3f;

		public float ProbabilityOfKill = 0.5f;

		public bool PlayerNpcCanDitchShip;

		public double TimeOfPersistingDitchedShip = 20000.0;
	}
}
