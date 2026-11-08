using UnityEngine;

namespace OpenFrontier.IP.Engine.Core.Units.ActiveUnits
{
	public class ActiveUnitSatellite : MonoBehaviour
	{
		public ActiveUnit ActiveUnit;

		public Transform RotateTarget;

		public float RotationRate = 2f;

		private void Update()
		{
			if (ActiveUnit.LastDistanceFromCamera < 1500f && ActiveUnit.Unit != null && !ActiveUnit.Unit.IsUnderConstruction)
			{
				RotateTarget.transform.Rotate(Vector3.up, RotationRate * Time.deltaTime);
			}
		}
	}
}
