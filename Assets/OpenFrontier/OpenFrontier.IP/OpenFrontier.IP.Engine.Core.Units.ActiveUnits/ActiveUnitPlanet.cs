using UnityEngine;

namespace OpenFrontier.IP.Engine.Core.Units.ActiveUnits
{
	public class ActiveUnitPlanet : MonoBehaviour
	{
		public MeshRenderer AtmoshphereRenderer;

		private Unit unit;

		private UnitPlanet planetUnit;

		public float PlanetRotation = 0.25f;

		public Transform TransformTarget;

		public Vector3 BaseRotation = Vector3.zero;

		private void Awake()
		{
			unit = GetComponentInParent<Unit>();
			if (unit != null)
			{
				planetUnit = unit.GetComponent<UnitPlanet>();
			}
			Rotate();
		}

		private void FixedUpdate()
		{
			Rotate();
		}

		public void Rotate()
		{
			if (planetUnit != null)
			{
				float num = (float)(EngineASX.Instance.ScenarioElapsedTime * (double)PlanetRotation * (double)planetUnit.RotationRateMultiplier % 360.0);
				TransformTarget.localRotation = Quaternion.Euler(planetUnit.Rotation) * Quaternion.Euler(BaseRotation.x, BaseRotation.y + num, BaseRotation.z);
			}
		}
	}
}
