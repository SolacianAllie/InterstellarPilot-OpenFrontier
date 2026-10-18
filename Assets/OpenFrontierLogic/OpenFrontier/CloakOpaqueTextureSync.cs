using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OpenFrontier
{
	/// <summary>
	/// Open Frontier: drives the main camera's opaque-texture override from
	/// CloakOpaqueTextureRequests, so the cloak shader's bent-light sample
	/// (_CameraOpaqueTexture) only costs its fullscreen copy while some
	/// hull is actually cloaking. Spawns next to ShadowQualitySync (see
	/// its Awake -- additive components, no prefab wiring, no new-script
	/// GUID dance), polls at 4 Hz, and touches the camera only when the
	/// request state changes.
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

		private void Update()
		{
			pollTimer += Time.unscaledDeltaTime;
			if (pollTimer < PollIntervalSeconds)
			{
				return;
			}
			pollTimer = 0f;
			// Refreshed on real applies only: Camera.main is tagged on
			// GameCamera/MenuCamera, and a destroyed camera fails Unity's
			// null check so the next poll re-resolves the current one.
			bool wantsNow = CloakOpaqueTextureRequests.Any;
			if (wantsNow == appliedWants)
			{
				return;
			}
			Camera main = Camera.main;
			if (main == null)
			{
				return;
			}
			appliedWants = wantsNow;
			UniversalAdditionalCameraData cameraData = main.GetUniversalAdditionalCameraData();
			// The bool setter maps straight onto the On/Off override options.
			cameraData.requiresColorTexture = wantsNow;
		}
	}
}
