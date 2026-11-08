using UnityEngine;

namespace OpenFrontier.IP.Engine.Settings
{
	public class HudCameraSettings : MonoBehaviour
	{
		public float SmoothDampTime = 0.07f;

		public float CamRelativeXAngle = 10f;

		public float CamRotationLerpRate = 9f;

		public bool AllowCameraThroughObjects = true;

		public float DestroyedUnitTargetTimeout = 10f;

		public bool LerpCameraToTarget = true;

		public float CooldownTime = 1f;

		public float OrbitPanGestureMinRotationSpeed = 1f;

		public float OrbitPanGestureMaxRotationSpeed = 2f;

		public float OrbitPanGestureSpeedMultiplier = 1f;

		public float OrbitPanGestureMinElevationSpeed = 1f;

		public float OrbitPanGestureMaxElevationSpeed = 1f;

		public float MaxManualOrbitAngleX = 75f;

		public float DefaultElevation = 0.7f;

		public float DefaultZoom = 0.7f;

		public float MouseWheelZoomRate = 1f;
	}
}
