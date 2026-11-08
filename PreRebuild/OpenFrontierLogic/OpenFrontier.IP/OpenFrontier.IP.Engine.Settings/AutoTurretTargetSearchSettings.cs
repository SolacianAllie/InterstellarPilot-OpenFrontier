using UnityEngine;

namespace OpenFrontier.IP.Engine.Settings
{
	public class AutoTurretTargetSearchSettings : MonoBehaviour
	{
		public float PreferredTurretTargetScore = 10f;

		public float ReferenceMaxDistance = 1000f;

		public float DistanceScore = 10f;
	}
}
