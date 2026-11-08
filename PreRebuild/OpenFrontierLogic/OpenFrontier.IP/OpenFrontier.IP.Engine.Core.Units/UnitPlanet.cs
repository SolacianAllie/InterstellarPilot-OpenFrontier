using UnityEngine;

namespace OpenFrontier.IP.Engine.Core.Units
{
	public class UnitPlanet : MonoBehaviour
	{
		public Vector3 Rotation;

		public float RotationRateMultiplier = 1f;

		public bool IsMoon => GetComponent<Moon>() != null;
	}
}
