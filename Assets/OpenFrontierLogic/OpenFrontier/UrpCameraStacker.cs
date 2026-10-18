using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace OpenFrontier
{
	/// <summary>
	/// The original game composites 7+ cameras via BiRP depth+clear flags.
	/// URP requires explicit camera stacking: this rebuilds the stack
	/// (sorted by camera.depth) whenever a scene loads.
	/// </summary>
	public static class UrpCameraStacker
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Register()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Rebuild()
		{
			var cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
				// The cloak backdrop copy rig rides under the real rig and
				// must never be swept into it: while cloaking survives a
				// scene load, its enabled base camera would be restacked
				// as an overlay of the real view and paint the whole
				// background into the player's screen.
				.Where(c => c.isActiveAndEnabled && !c.name.StartsWith("CloakBackdrop"))
				.OrderBy(c => c.depth)
				.ToList();
			if (cams.Count < 2)
			{
				return;
			}

			var baseData = cams[0].GetUniversalAdditionalCameraData();
			baseData.renderType = CameraRenderType.Base;
			baseData.cameraStack.Clear();
			// Post-processing on the base camera covers the whole stack
			// (URP ignores post flags on overlays). Without this the
			// volume profile (bloom etc.) never ran - effects looked dull.
			baseData.renderPostProcessing = true;

			foreach (var cam in cams.Skip(1))
			{
				if (cam == cams[0])
				{
					continue;
				}
				var data = cam.GetUniversalAdditionalCameraData();
				data.renderType = CameraRenderType.Overlay;
				baseData.cameraStack.Add(cam);
			}
			Debug.Log($"[UrpCameraStacker] stacked {cams.Count - 1} cameras onto '{cams[0].name}'");
		}

		private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			Rebuild();
		}
	}
}
