using DigitalRubyShared;
using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Hud
{
	public class PinchZoomCamera : MonoBehaviour
	{
		[Tooltip("The zoom speed")]
		public float ZoomSpeed = 0.1f;

		[Tooltip("The threshold in units before zooming begins to happen. Start distance must change this much in order to start the gesture.")]
		[Range(0f, 3f)]
		public float ZoomThresholdUnits = 0.15f;

		public GameObject PlatformSpecificView;

		public ScaleGestureRecognizer ScaleGesture { get; private set; }

		private void OnEnable()
		{
			ScaleGesture = new ScaleGestureRecognizer
			{
				ZoomSpeed = ZoomSpeed
			};
			ScaleGesture.StateUpdated += ScaleGesture_Updated;
			ScaleGesture.ThresholdUnits = ZoomThresholdUnits;
			ScaleGesture.PlatformSpecificView = PlatformSpecificView;
			FingersScript.Instance.AddGesture(ScaleGesture);
		}

		private void OnDisable()
		{
			if (FingersScript.HasInstance)
			{
				FingersScript.Instance.RemoveGesture(ScaleGesture);
			}
		}

		private void ScaleGesture_Updated(GestureRecognizer gesture)
		{
			if (gesture.State == GestureRecognizerState.Executing && EngineASX.Instance.Hud.IsCurrentScreen)
			{
				EngineASX.Instance.HudCamera.CamUnitDistance -= ScaleGesture.ScaleDistanceDeltaY;
				EngineASX.Instance.HudCamera.CameraElevation -= ScaleGesture.ScaleDistanceDeltaY;
			}
		}
	}
}
