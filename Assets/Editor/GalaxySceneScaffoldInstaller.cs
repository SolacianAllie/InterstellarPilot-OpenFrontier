using OpenFrontier.Engine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenFrontier.EditorTools
{
	/// <summary>
	/// One-shot: authors the pinned Galaxy scene contract
	/// (docs/engine-plan.md, "Galaxy scene contract") as
	/// Assets/OpenFrontier/Scenes/Galaxy.unity, wires the engine components
	/// to each other, and adds the scene to the build profile's scene list
	/// (runtime LoadScene requires it — the same gate the stub scene hit).
	/// Built additively beside whatever the user has open, saved, CLOSED,
	/// then verified against the RELOADED file — the house rule: logging
	/// records intents, disk records facts.
	///
	/// What the user still authors by hand (the tool only builds the
	/// skeleton): stars and dressing under "hand/", per-gamemode dressing
	/// under "modes/<gamemode-id>/", the Generation Trigger's real shape
	/// (the starter box is a placeholder to resize), and further anchors.
	/// </summary>
	public static class GalaxySceneScaffoldInstaller
	{
		private const string Tag = "[GalaxySceneScaffoldInstaller]";
		private const string ScenePath = "Assets/OpenFrontier/Scenes/Galaxy.unity";

		[MenuItem("Open Frontier/Scaffold Galaxy Scene (one-shot)")]
		public static void Install()
		{
			if (System.IO.File.Exists(ScenePath))
			{
				Debug.LogError($"{Tag} {ScenePath} already exists - the scaffold is one-shot and never overwrites. Delete the scene first if you want it rebuilt.");
				return;
			}

			Debug.Log($"{Tag} Building the pinned galaxy scene contract additively...");

			// EmptyScene: no camera, no light — everything authored below.
			// Additive beside whatever the user has open; closed after save.
			Scene scaffold = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

			GameObject galaxyRoot = BuildGalaxyRoot(scaffold);
			GenerationTrigger trigger = BuildGenerationTrigger(scaffold);
			Transform systemsRoot = BuildSystemsRoot(scaffold);
			Camera galaxyCamera = BuildCamera(scaffold, "Galaxy Camera", orthographic: false, enabled: true);
			BuildCamera(scaffold, "MAP", orthographic: true, enabled: false); // consumed later; disabled = no rogue URP base camera

			// Wire the boot to its own stage (inspector-visible references —
			// the scene contract's load-bearing links are editable, not
			// name-lookup magic).
			GalaxyStageBoot boot = galaxyRoot.GetComponent<GalaxyStageBoot>();
			boot.SystemsRoot = systemsRoot;
			boot.ModesRoot = galaxyRoot.transform.Find("modes");
			boot.Trigger = trigger;
			boot.GalaxyCamera = galaxyCamera;

			EditorSceneManager.SaveScene(scaffold, ScenePath);
			EditorSceneManager.CloseScene(scaffold, true);

			if (!VerifyAgainstReloadedFile())
			{
				return; // Loud failures already logged; do not trust-build on top.
			}

			AddToBuildSettings();
		}

		private static GameObject BuildGalaxyRoot(Scene scaffold)
		{
			GameObject root = new GameObject("Galaxy");
			SceneManager.MoveGameObjectToScene(root, scaffold);
			root.AddComponent<GalaxyStageBoot>();

			foreach (string childName in new[] { "hand", "Generated", "modes" })
			{
				GameObject child = new GameObject(childName);
				child.transform.SetParent(root.transform, false);
			}

			return root;
		}

		private static GenerationTrigger BuildGenerationTrigger(Scene scaffold)
		{
			GameObject triggerObject = new GameObject("Generation Trigger");
			SceneManager.MoveGameObjectToScene(triggerObject, scaffold);
			GenerationTrigger trigger = triggerObject.AddComponent<GenerationTrigger>();

			// Starter shape only — the user owns the real region; resize in
			// the inspector. A modest box, centred at the origin.
			BoxCollider box = triggerObject.AddComponent<BoxCollider>();
			box.size = new Vector3(120f, 40f, 120f);

			return trigger;
		}

		private static Transform BuildSystemsRoot(Scene scaffold)
		{
			GameObject systems = new GameObject("systems");
			SceneManager.MoveGameObjectToScene(systems, scaffold);

			// One starter anchor so the 2a chain is testable the moment the
			// scene exists — clearly the user's to move, rename or delete.
			GameObject anchor = new GameObject("Devtest");
			anchor.transform.SetParent(systems.transform, false);
			SystemAnchor systemAnchor = anchor.AddComponent<SystemAnchor>();
			systemAnchor.AnchorId = "core.devtest.alpha";
			systemAnchor.GamemodeIds = new[] { "core.devtest" };
			anchor.transform.localPosition = new Vector3(0f, 0f, 150f);

			return systems.transform;
		}

		private static Camera BuildCamera(Scene scaffold, string name, bool orthographic, bool enabled)
		{
			GameObject cameraObject = new GameObject(name);
			SceneManager.MoveGameObjectToScene(cameraObject, scaffold);
			Camera camera = cameraObject.AddComponent<Camera>();
			camera.orthographic = orthographic;
			camera.enabled = enabled;

			// The black-skybox contract: flat black, everything else is
			// scene objects. Far plane 100000 per the backdrop discipline.
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = Color.black;
			camera.farClipPlane = 100000f;

			if (orthographic)
			{
				camera.orthographicSize = 50f;
			}

			return camera;
		}

		private static bool VerifyAgainstReloadedFile()
		{
			Scene reloaded = default;
			bool ok = true;
			try
			{
				reloaded = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

				GameObject root = GameObject.Find("Galaxy");
				if (root == null || root.GetComponent<GalaxyStageBoot>() == null)
				{
					Debug.LogError($"{Tag} RELOADED FILE: 'Galaxy' root or its GalaxyStageBoot is missing.");
					ok = false;
				}
				else
				{
					GalaxyStageBoot boot = root.GetComponent<GalaxyStageBoot>();
					if (boot.SystemsRoot == null || boot.ModesRoot == null || boot.Trigger == null || boot.GalaxyCamera == null)
					{
						Debug.LogError($"{Tag} RELOADED FILE: GalaxyStageBoot wiring did not survive the save (a reference read null).");
						ok = false;
					}
					else
					{
						foreach (string childName in new[] { "hand", "Generated", "modes" })
						{
							if (root.transform.Find(childName) == null)
							{
								Debug.LogError($"{Tag} RELOADED FILE: Galaxy/{childName} is missing.");
								ok = false;
							}
						}
					}
				}

				if (GameObject.Find("Generation Trigger") == null)
				{
					Debug.LogError($"{Tag} RELOADED FILE: 'Generation Trigger' is missing.");
					ok = false;
				}

				if (GameObject.Find("systems") == null)
				{
					Debug.LogError($"{Tag} RELOADED FILE: 'systems' is missing.");
					ok = false;
				}

				Camera map = GameObject.Find("MAP") != null ? GameObject.Find("MAP").GetComponent<Camera>() : null;
				if (map == null)
				{
					Debug.LogError($"{Tag} RELOADED FILE: 'MAP' camera is missing.");
					ok = false;
				}
				else if (map.enabled || !map.orthographic)
				{
					// An enabled MAP today is a rogue URP base camera (the
					// mid-scene-camera trap) — the disabled state is the
					// contract until the sector map consumes it.
					Debug.LogError($"{Tag} RELOADED FILE: MAP must be DISABLED and orthographic - refusing to report success.");
					ok = false;
				}

				Camera galaxyCamera = GameObject.Find("Galaxy Camera") != null ? GameObject.Find("Galaxy Camera").GetComponent<Camera>() : null;
				if (galaxyCamera == null)
				{
					Debug.LogError($"{Tag} RELOADED FILE: 'Galaxy Camera' is missing.");
					ok = false;
				}
				else if (galaxyCamera.clearFlags != CameraClearFlags.SolidColor || galaxyCamera.backgroundColor != Color.black)
				{
					Debug.LogError($"{Tag} RELOADED FILE: Galaxy Camera must clear SOLID BLACK (the skybox contract).");
					ok = false;
				}
			}
			finally
			{
				if (reloaded.IsValid())
				{
					EditorSceneManager.CloseScene(reloaded, true);
				}
			}

			if (ok)
			{
				Debug.Log($"{Tag} {ScenePath} verified against the reloaded file - the pinned contract is on disk. Author your stars, dressing and the trigger's real shape; a starter 'Devtest' anchor (core.devtest.alpha) is under systems/.");
			}

			return ok;
		}

		private static void AddToBuildSettings()
		{
			List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
			if (scenes.Exists(s => s.path == ScenePath))
			{
				Debug.Log($"{Tag} {ScenePath} already in the build profile's scene list - left untouched.");
				return;
			}

			scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
			EditorBuildSettings.scenes = scenes.ToArray();

			// Verify against the RELOADED settings, not the array just written.
			bool listed = false;
			foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
			{
				if (scene.path == ScenePath && scene.enabled)
				{
					listed = true;
					break;
				}
			}

			if (listed)
			{
				Debug.Log($"{Tag} {ScenePath} added to the build profile's scene list and verified on reload - launches may route through it.");
			}
			else
			{
				Debug.LogError($"{Tag} RELOADED BUILD SETTINGS DO NOT LIST {ScenePath} - do not trust any success line above this one.");
			}
		}
	}
}
