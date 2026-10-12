using UnityEngine;

namespace OpenFrontier.IP.Engine.Core.Units.ActiveUnits
{
	public class ActiveUnitPlanetClouds : MonoBehaviour
	{
		public Transform TransformTarget;

		public float CloudRotateRateMultiplier = 1f;

		public Vector3 BaseRotation;

		private void Update()
		{
			Rotate();
		}

		public void Rotate()
		{
			if (TransformTarget != null)
			{
				float num = (float)(EngineASX.Instance.ScenarioElapsedTime * (double)CloudRotateRateMultiplier % 360.0);
				TransformTarget.localRotation = Quaternion.Euler(BaseRotation.x, num + BaseRotation.y, BaseRotation.z);
			}
		}
	}
}
