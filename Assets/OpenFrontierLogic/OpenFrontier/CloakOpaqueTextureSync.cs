using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OpenFrontier
{
	/// <summary>
	/// Open Frontier: drives the render camera's opaque-texture override
	/// from CloakOpaqueTextureRequests, so the cloak shader's bent-light
	/// sample (_CameraOpaqueTexture) only costs its fullscreen copy while
	/// some hull is actually cloaking. Spawns next to ShadowQualitySync
	/// (see its Awake -- additive components, no prefab wiring, no
	/// new-script GUID dance), polls at 4 Hz, and applies only when the
	/// request state or the provided camera changes.
	///
	/// The override lives on the camera's UniversalAdditionalCameraData
	/// (requiresColorTexture, which the bool setter maps onto the On/Off
	/// override options), NOT on the pipeline asset: runtime writes on the
	/// asset re-serialize in the editor and drift (the ShadowQualitySync
	/// trap), camera data is per-camera state the pipeline re-reads every
	/// frame.
	///
	/// The menu backdrop holds no cloaks, the sector map does not render
	/// hulls, and decloaked ships carrying the cloak shader do not count:
	/// in those worlds this component is idle and the camera never pays
	/// for the copy.
	/// </summary>
	public class CloakOpaqueTextureSync : MonoBehaviour
	{
		private const float PollIntervalSeconds = 0.25f;

		private float pollTimer;

		// What the camera currently has, not what was requested: a camera
		// without the override (or a missing one) defers its apply to the
		// next poll instead of eating the state change.
		private bool appliedWants;

		// The camera that received the last apply: a REPLACED camera needs
		// the same want re-applied even though wants did not change.
		private Camera appliedCamera;

		private void Update()
		{
			pollTimer += Time.unscaledDeltaTime;
			if (pollTimer < PollIntervalSeconds)
			{
				return;
			}
			pollTimer = 0f;
			bool wantsNow = CloakOpaqueTextureRequests.Any;
			Camera provided = CloakOpaqueTextureRequests.ProvidedCamera;
			if (wantsNow == appliedWants && provided == appliedCamera)
			{
				return;
			}
			// The engine-provided render camera first (GameController hands
			// its instantiation over); Camera.main is the fallback. A
			// destroyed camera fails Unity's null check, so the next poll
			// re-resolves.
			Camera camera = CloakOpaqueTextureRequests.ProvidedCamera;
			if (camera == null)
			{
				camera = Camera.main;
			}
			if (camera == null)
			{
				return;
			}
			appliedWants = wantsNow;
			appliedCamera = provided;
			UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
			// The bool setter maps straight onto the On/Off override options.
			cameraData.requiresColorTexture = wantsNow;
		}
	}
}
