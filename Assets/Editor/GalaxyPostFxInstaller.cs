using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace OpenFrontier.EditorTools
{
	/// <summary>
	/// Installs the post chain ED's map relies on (findings §2.3): a global
	/// Volume with Bloom + ACES tonemapping + a gentle colour grade, and
	/// renderPostProcessing enabled on both galaxy cameras.
	///
	/// The galaxy stack is additive before this: the core and the bright
	/// sprites can exceed 1.0, but with no post-processing the camera clamps
	/// them and the map looks flat. Bloom is what turns the additive core into
	/// ED's soft glow; ACES keeps the amber core from clipping to white.
	///
	/// Idempotent, scene/asset based, and safe to re-run. The profile lives at
	/// Assets/OpenFrontier/Galaxy/GalaxyPostFX.asset; the volume is a root
	/// object named "Galaxy Post FX" in the Galaxy scene.
	/// </summary>
	public static class GalaxyPostFxInstaller
	{
		const string GalaxyScenePath = "Assets/OpenFrontier/Scenes/Galaxy.unity";
		const string ProfilePath = "Assets/OpenFrontier/Galaxy/GalaxyPostFX.asset";
		const string VolumeObjectName = "Galaxy Post FX";

		[MenuItem("OpenFrontier/Galaxy/Install Post FX (Bloom + Tonemap)")]
		public static void Install()
		{
			Scene scene = default;
			bool openedHere = false;
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene candidate = SceneManager.GetSceneAt(i);
				if (candidate.path == GalaxyScenePath) { scene = candidate; break; }
			}
			if (!scene.IsValid())
			{
				if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GalaxyScenePath) == null)
				{
					Debug.LogError($"[GalaxyPostFxInstaller] '{GalaxyScenePath}' not found - scene contract moved?");
					return;
				}
				scene = EditorSceneManager.OpenScene(GalaxyScenePath, OpenSceneMode.Additive);
				openedHere = true;
			}

			VolumeProfile profile = LoadOrCreateProfile();
			if (profile == null)
			{
				if (openedHere) EditorSceneManager.CloseScene(scene, true);
				return;
			}

			GameObject volumeGo = null;
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				if (root.name == VolumeObjectName) { volumeGo = root; break; }
			}
			if (volumeGo == null)
			{
				volumeGo = new GameObject(VolumeObjectName);
				Undo.RegisterCreatedObjectUndo(volumeGo, "Install Galaxy Post FX");
				SceneManager.MoveGameObjectToScene(volumeGo, scene);
			}

			Volume volume = volumeGo.GetComponent<Volume>();
			if (volume == null) volume = Undo.AddComponent<Volume>(volumeGo);
			Undo.RecordObject(volume, "Install Galaxy Post FX");
			volume.isGlobal = true;
			volume.priority = 0f;
			volume.weight = 1f;
			volume.sharedProfile = profile;
			EditorUtility.SetDirty(volume);

			bool changed = false;
			foreach (string cameraName in new[] { "MAP", "Galaxy Camera" })
			{
				Camera camera = FindCamera(cameraName);
				if (camera == null) continue;
				UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
				Undo.RecordObject(data, "Install Galaxy Post FX");
				if (!data.renderPostProcessing)
				{
					data.renderPostProcessing = true;
					changed = true;
				}
				EditorUtility.SetDirty(data);
			}

			EditorSceneManager.MarkSceneDirty(scene);
			EditorSceneManager.SaveScene(scene);
			AssetDatabase.SaveAssets();
			if (openedHere) EditorSceneManager.CloseScene(scene, true);

			Debug.Log($"[GalaxyPostFxInstaller] {(changed ? "enabled" : "verified")} post-processing on MAP + Galaxy Camera, " +
				$"global Volume '{VolumeObjectName}' -> '{ProfilePath}' (Bloom + ACES + color grade).");
		}

		static VolumeProfile LoadOrCreateProfile()
		{
			VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
			if (profile == null)
			{
				profile = ScriptableObject.CreateInstance<VolumeProfile>();
				profile.name = "GalaxyPostFX";
				AssetDatabase.CreateAsset(profile, ProfilePath);
			}

			Bloom bloom = GetOrAddComponent<Bloom>(profile);
			bloom.active = true;
			bloom.threshold.Override(0.9f);
			bloom.intensity.Override(0.65f);
			bloom.scatter.Override(0.72f);
			bloom.tint.Override(new Color(1f, 0.91f, 0.76f, 1f));
			bloom.highQualityFiltering.Override(true);

			Tonemapping tone = GetOrAddComponent<Tonemapping>(profile);
			tone.active = true;
			tone.mode.Override(TonemappingMode.ACES);

			ColorAdjustments color = GetOrAddComponent<ColorAdjustments>(profile);
			color.active = true;
			color.postExposure.Override(0.15f);
			color.contrast.Override(8f);
			color.saturation.Override(6f);
			color.colorFilter.Override(new Color(1f, 0.97f, 0.92f, 1f));

			Vignette vignette = GetOrAddComponent<Vignette>(profile);
			vignette.active = true;
			vignette.intensity.Override(0.18f);
			vignette.smoothness.Override(0.45f);
			vignette.color.Override(new Color(0.02f, 0.012f, 0.008f, 1f));

			EditorUtility.SetDirty(profile);
			return profile;
		}

		static T GetOrAddComponent<T>(VolumeProfile profile) where T : VolumeComponent
		{
			if (profile.TryGet(out T component)) return component;
			component = profile.Add<T>(true);
			component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
			AssetDatabase.AddObjectToAsset(component, profile);
			return component;
		}

		static Camera FindCamera(string name)
		{
			Camera[] cameras = Object.FindObjectsByType<Camera>(
				FindObjectsInactive.Include, FindObjectsSortMode.None);
			foreach (Camera camera in cameras)
				if (camera.name == name) return camera;
			return null;
		}
	}
}
