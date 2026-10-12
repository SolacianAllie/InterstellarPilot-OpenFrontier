using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class NpcPilotBehaviourCountermeasureSettings : MonoBehaviour
	{
		public float MinMissileLifetimeLower = 0.25f;

		public float MaxMissileLifetimeUpper = 1.5f;

		public bool DeploymentEnabled = true;

		public float MinDeployCooldownTime = 2f;

		public float MaxDeployCooldownTime = 6f;

		public float MaxDeploymentRangeUpper = 280f;

		public float MaxDeploymentRangeLower = 180f;

		public float MinDeploymentProbability = 0.5f;

		public float MaxDeploymentProbability = 1f;

		public float UpdateInterval = 1f;

		public bool UpdateEnabled = true;
	}
}
