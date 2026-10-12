using DigitalRubyShared;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Settings;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Hud
{
	public class PanCamera : MonoBehaviour
	{
		public GameObject OrbitScaleGesturePlatformSpecificView;

		private float desiredCameraAngleY;

		// Open Frontier: free-look pitch for the orbit drag (matches
		// CameraSpectator's manual orbit: clamped to +/-75 degrees).
		private const float MaxManualOrbitAngleX = 75f;

		private float desiredCameraAngleX;

		public PanGestureRecognizer OrbitPanGesture { get; private set; }

		private void OnEnable()
		{
			OrbitPanGesture = new PanGestureRecognizer();
			OrbitPanGesture.StateUpdated += PanGestureUpdated;
			OrbitPanGesture.PlatformSpecificView = OrbitScaleGesturePlatformSpecificView;
			FingersScript.Instance.AddGesture(OrbitPanGesture);
		}

		private void PanGestureUpdated(GestureRecognizer r)
		{
			if (EngineASX.LoadedAndReady && EngineASX.Instance.Hud.IsCurrentScreen)
			{
				if (r.State == GestureRecognizerState.Began)
				{
					EngineASX.Instance.Hud.CameraMode = HudCameraMode.Free;
					desiredCameraAngleY = GameController.Instance.MainCamera.transform.eulerAngles.y;
					desiredCameraAngleX = Mathf.DeltaAngle(0f, GameController.Instance.MainCamera.transform.eulerAngles.x);
				}
				if (r.State == GestureRecognizerState.Executing)
				{
					EngineASX.Instance.Hud.CameraMode = HudCameraMode.Free;
					float velocityX = r.VelocityX;
					float velocityY = r.VelocityY;
					velocityX *= RealTime.deltaTime;
					velocityY *= RealTime.deltaTime;
					HudCameraSettings hudCameraSettings = EngineASX.Instance.GameSettings.HudCameraSettings;
					float num = Mathf.Lerp(hudCameraSettings.OrbitPanGestureMinRotationSpeed, hudCameraSettings.OrbitPanGestureMaxRotationSpeed, GameController.Instance.CameraDragRotateSensitivity) * hudCameraSettings.OrbitPanGestureSpeedMultiplier;
					num *= GameController.Instance.GetCameraDragRotateSensitivityMultiplier();
					desiredCameraAngleY += DeviceInfo.PixelsToUnits(velocityX) * num;
					// Vertical drag pitches the camera (free look up/down)
					// instead of changing elevation; elevation stays on its button.
					// NEGATED to match the menu's spectator camera: that one
					// derives its pitch from where it sits around the target,
					// so an upwards swipe looks up there, while a positive
					// euler X here would look DOWN - the two were inverted
					// since elevation became pitch.
					desiredCameraAngleX = Mathf.Clamp(desiredCameraAngleX - DeviceInfo.PixelsToUnits(velocityY) * num, 0f - MaxManualOrbitAngleX, MaxManualOrbitAngleX);
					EngineASX.Instance.HudCamera.SetCameraDesiredRotation(desiredCameraAngleX, desiredCameraAngleY);
				}
			}
		}

		private void OnDisable()
		{
			if (FingersScript.HasInstance)
			{
				FingersScript.Instance.RemoveGesture(OrbitPanGesture);
			}
		}
	}
}
