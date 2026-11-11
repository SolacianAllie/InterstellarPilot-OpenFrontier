using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// One-time port of the ORIGINAL game's menu post-processing into URP.
///
/// Source of truth: the read-only BIRP-era asset tree in
/// PreRebuild/OpenFrontier/Scenes/ui/menu/LandingScene_Profiles/
/// "Camera Profile.asset" (PostProcessing Stack v2) + the PostProcessLayer
/// block inside LandingScene.unity. Read, translated 1:1, entered by API -
/// never hand-written YAML (volume component serialization moves with the
/// package, and a mis-written profile reads as NULL SETTINGS silently,
/// the prefab-trap shape).
///
/// What the old game had (read from disk, 2026-11-10):
///   - Bloom:            intensity 1, threshold 1, soft knee 0.5,
///                       clamp 65472 (effectively unconstrained),
///                       diffusion 7, anamorphic 0, tint white,
///                       fastMode off.
///   - ColorGrading:     HDR grading mode, tonemapper = ACES (enum 2),
///                       NOTHING else overridden - the 1300-line tail
///                       of that asset is untouched default curve keys.
///   - PostProcessLayer: antialiasingMode 0 - NO AA. Volume layers and
///                       triggers existed but only ONE global volume.
///   - Scope:            the LANDING (menu) scene ONLY. Every gameplay
///                       scene ran with zero post-processing. This port
///                       keeps that scoping: it wires the OPEN scene and
///                       nothing else.
///
/// PPv2 -> URP translation (unity.postprocessing 3.x -> URP 17.x):
///   - ColorGrading/HDR + tonemapper ACES  ->  Tonemapping.mode = ACES.
///     (URP folds "HDR grading mode" into its tonemapping; every other
///     PPv2 grading channel was default and stays unset.)
///   - Bloom.intensity 1   ->  Bloom.intensity 1.
///   - Bloom.threshold 1   ->  Bloom.threshold 1 (URP asset HDR is ON -
///     verified m_SupportsHDR 1 - so the >1 range survives as in the old
///     HDR grading path).
///   - Bloom.diffusion 7   ->  Bloom.scatter 0.7 (diffusion is a 0-10
///     band-spread; URP scatter is the same idea scaled 0-1).
///   - Bloom.clamp 65472   ->  Bloom.clamp 65472 (identical semantics AND
///     identical value - both packages picked the same sentinel).
///   - Bloom tint white    ->  Bloom.tint white.
///   - softKnee 0.5: URP bloom has no knee knob (its knee is fixed at
///     ~0.5) - behaviour carries by design, nothing to set.
///   - anamorphicRatio 0 / dirt 0 / fastMode false: URP has no
///     anamorphic or PPv2 fast path; omitted.
///   - antialiasingMode 0  ->  camera antialiasing stays at its default
///     "No anti-aliasing" (Unity 6 default) - explicitly NOT changed.
///
/// Run "Open Frontier / Install Holo Post FX" with the target scene
/// (Main Menu, Landing Scene) OPEN. It is idempotent: re-running in a
/// scene reuses the profile asset, re-checks the camera tick, and never
/// duplicates the volume. Saves the scene and prints the verified state -
/// logging records intents, disk records facts.
/// </summary>
public static class HoloPostFXInstaller
{
	const string ProfilePath = "Assets/OpenFrontier/Settings/Holo PostProcessing.asset";
	const string VolumeName = "Holo Post FX Volume";

	[MenuItem("Open Frontier/Install Holo Post FX (ACES + Bloom, open scene)")]
	public static void Install()
	{
		var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
		Debug.Log($"[HoloPostFX] installing into open scene '{scene.name}' ({scene.path})");

		var profile = BuildOrLoadProfile();

		int camerasTicked = 0;
		foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
		{
			// Base cameras only: overlay cameras inherit what the base
			// already rendered - ticking both would double-apply, and
			// overlay cameras in these scenes are anchors or holo
			// shells, none of which asked for grading.
			var data = camera.GetUniversalAdditionalCameraData();
			if (data == null || data.renderType != CameraRenderType.Base)
				continue;

			if (!data.renderPostProcessing)
			{
				data.renderPostProcessing = true;
				EditorUtility.SetDirty(data);
				Debug.Log($"[HoloPostFX] post-processing ENABLED on camera '{camera.name}' (was off)");
			}
			camerasTicked++;
		}

		if (camerasTicked == 0)
			Debug.LogWarning("[HoloPostFX] no enabled base camera in the open scene - nothing was ticked");

		EnsureGlobalVolume(profile);

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);
		Debug.Log($"[HoloPostFX] scene '{scene.name}' saved");

		Verify(profile);
	}

	static VolumeProfile BuildOrLoadProfile()
	{
		VolumeProfile profile;
		if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath) is VolumeProfile existing)
		{
			profile = existing;
			Debug.Log($"[HoloPostFX] reusing existing profile at {ProfilePath}");
		}
		else
		{
			profile = ScriptableObject.CreateInstance<VolumeProfile>();
			AssetDatabase.CreateAsset(profile, ProfilePath);
			Debug.Log($"[HoloPostFX] created profile at {ProfilePath}");
		}

		// Tonemapping: the old grading was ACES and nothing else.
		var tonemap = FindOrAdd<Tonemapping>(profile);
		tonemap.mode.overrideState = true;
		tonemap.mode.value = TonemappingMode.ACES;

		// Bloom: THE five PPv2 overrides, verbatim.
		var bloom = FindOrAdd<Bloom>(profile);
		bloom.threshold.overrideState = true;
		bloom.threshold.value = 1f;
		bloom.intensity.overrideState = true;
		bloom.intensity.value = 1f;
		bloom.scatter.overrideState = true;
		bloom.scatter.value = 0.7f;	// PPv2 diffusion 7 on a 0-10 scale.
		bloom.clamp.overrideState = true;
		bloom.clamp.value = 65472f;
		bloom.tint.overrideState = true;
		bloom.tint.value = new Color(1f, 1f, 1f, 1f);

		EditorUtility.SetDirty(profile);
		AssetDatabase.SaveAssets();
		return profile;
	}

	// VolumeProfile.Add<T> does not expose find-before-add in 17.x's
	// public surface, so components are located by component list.
	static T FindOrAdd<T>(VolumeProfile profile) where T : VolumeComponent, new()
	{
		foreach (var component in profile.components)
			if (component is T typed)
				return typed;

		var fresh = ScriptableObject.CreateInstance<T>();
		fresh.name = typeof(T).Name;
		// AddAssetToAsset keeps the profile a single serialized file -
		// sub-assets, exactly how the Volume inspector would build it.
		AssetDatabase.AddObjectToAsset(fresh, profile);
		profile.components.Add(fresh);
		return fresh;
	}

	static Volume EnsureGlobalVolume(VolumeProfile profile)
	{
		foreach (var volume in Object.FindObjectsByType<Volume>(FindObjectsInactive.Exclude))
		{
			if (volume.gameObject.name != VolumeName)
				continue;

			if (!volume.isGlobal)
			{
				volume.isGlobal = true;
				EditorUtility.SetDirty(volume);
			}
			if (volume.sharedProfile != profile)
			{
				volume.sharedProfile = profile;
				EditorUtility.SetDirty(volume);
				Debug.Log($"[HoloPostFX] existing '{VolumeName}' rebound to the ported profile");
			}
			return volume;
		}

		var go = new GameObject(VolumeName, typeof(Volume));
		var added = go.GetComponent<Volume>();
		added.isGlobal = true;
		added.priority = 1f;
		added.sharedProfile = profile;
		Debug.Log($"[HoloPostFX] created global volume '{VolumeName}' (priority 1)");
		return added;
	}

	static void Verify(VolumeProfile profile)
	{
		Debug.Log($"[HoloPostFX] VERIFY: URP asset HDR = {(UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset != null && UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset.supportsHDR ? "on" : "OFF (bloom threshold-1 will be near-blind - expected on)")}");
		// Reload straight from disk: the asset on disk is the fact.
		AssetDatabase.ImportAsset(ProfilePath);
		var fromDisk = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
		foreach (var component in fromDisk.components)
		{
			if (component is Tonemapping tm)
				Debug.Log($"[HoloPostFX] VERIFY Tonemapping: mode={tm.mode.value} overridden={tm.mode.overrideState}");
			if (component is Bloom b)
				Debug.Log($"[HoloPostFX] VERIFY Bloom: intensity={b.intensity.value} threshold={b.threshold.value} scatter={b.scatter.value} clamp={b.clamp.value} tint={b.tint.value}");
		}
	}
}
