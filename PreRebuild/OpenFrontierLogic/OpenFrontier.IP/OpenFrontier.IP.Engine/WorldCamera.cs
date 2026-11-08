using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// Open Frontier: reliable world-camera lookup. Camera.main is NOT
	/// reliable in this project - both MenuCamera and GameCamera are tagged
	/// MainCamera, so it can return the (static) menu camera. Prefer the
	/// game's canonical camera; fall back to the HIGHEST-depth enabled
	/// camera (GameCamera renders at depth 1, MenuCamera at -1).
	/// </summary>
	public static class WorldCamera
	{
		public static Camera Resolve()
		{
			if (GameController.Instance != null && GameController.Instance.MainCamera != null && GameController.Instance.MainCamera.isActiveAndEnabled)
			{
				return GameController.Instance.MainCamera;
			}
			Camera[] array = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			Camera camera = null;
			foreach (Camera camera2 in array)
			{
				if (camera == null || camera2.depth > camera.depth)
				{
					camera = camera2;
				}
			}
			if (camera == null)
			{
				camera = Camera.main;
			}
			return camera;
		}
	}
}
