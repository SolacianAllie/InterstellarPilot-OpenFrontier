using OpenFrontier.IP.Engine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenFrontier.EditorTools
{
	/// <summary>
	/// Idempotent installer for the realtime galaxy cloud: authors the default
	/// blueprint + cloud material assets under Assets/OpenFrontier/Galaxy/,
	/// then drops a "Galaxy Cloud" object under the Galaxy scene's hand/
	/// container wired to both. Re-running repairs the wiring instead of
	/// duplicating (matched by component, not name).
	///
	/// The blueprint ships fully procedural (no density map) - the analytic
	/// spiral is the default look and the phase-4 painter's job is to author
	/// maps that replace it. The cloud object starts tilted (24, 0, 12) so
	/// the band crosses the sky in-system and still reads as a disc from
	/// above - that rotation is a normal authoring knob, not installer state.
	///
	/// Follows the scaffold installer's rule: saved to disk, then reported -
	/// logging records intents, disk records facts.
	/// </summary>
	public static class GalaxyCloudInstaller
	{
		const string GalaxyScenePath = "Assets/OpenFrontier/Scenes/Galaxy.unity";
		const string AssetFolder = "Assets/OpenFrontier/Galaxy";
		const string BlueprintPath = AssetFolder + "/DefaultGalaxy.asset";
		const string MaterialPath = AssetFolder + "/GalaxyCloud.mat";
		const string CloudShaderName = "OpenFrontier/Galaxy/Cloud";
		const string CloudObjectName = "Galaxy Cloud";

		[MenuItem("OpenFrontier/Galaxy/Install Realtime Cloud")]
		public static void Install()
		{
			EnsureFolder("Assets/OpenFrontier", "Galaxy");

			GalaxyBlueprint blueprint = LoadOrCreateBlueprint();
			if (blueprint == null) return;
			Material material = LoadOrCreateMaterial();
			if (material == null) return;

			// ---- locate (or open) the Galaxy scene
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
					Debug.LogError($"[GalaxyCloudInstaller] '{GalaxyScenePath}' not found - scene contract moved?");
					return;
				}
				scene = EditorSceneManager.OpenScene(GalaxyScenePath, OpenSceneMode.Additive);
				openedHere = true;
			}

			// ---- the hand/ container is the user-authoring home (scene contract)
			Transform hand = null;
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				Transform found = root.name == "hand"
					? root.transform
					: root.transform.Find("hand");
				if (found != null) { hand = found; break; }
			}
			if (hand == null)
			{
				Debug.LogError("[GalaxyCloudInstaller] no 'hand' container in the Galaxy scene - the scene contract changed. Nothing installed.");
				if (openedHere) EditorSceneManager.CloseScene(scene, true);
				return;
			}

			// ---- the cloud object: matched by component so a rename can't fork it
			GalaxyInstanceCloud cloud = null;
			foreach (Transform child in hand)
			{
				GalaxyInstanceCloud existing = child.GetComponent<GalaxyInstanceCloud>();
				if (existing != null) { cloud = existing; break; }
			}
			bool created = cloud == null;
			GameObject go;
			if (created)
			{
				go = new GameObject(CloudObjectName);
				Undo.RegisterCreatedObjectUndo(go, "Install Galaxy Cloud");
				go.transform.SetParent(hand, false);
				go.transform.localPosition = Vector3.zero;
				go.transform.localRotation = Quaternion.Euler(24f, 0f, 12f);
				cloud = go.AddComponent<GalaxyInstanceCloud>();
			}
			else
			{
				go = cloud.gameObject;
			}

			// ---- wire (and record what actually changed)
			bool changed = created;
			Undo.RecordObject(cloud, "Wire Galaxy Cloud");
			if (cloud.blueprint != blueprint) { cloud.blueprint = blueprint; changed = true; }
			if (cloud.material != material) { cloud.material = material; changed = true; }

			if (changed)
			{
				EditorSceneManager.MarkSceneDirty(scene);
				EditorSceneManager.SaveScene(scene);
			}
			if (openedHere) EditorSceneManager.CloseScene(scene, true);

			Selection.activeGameObject = go;
			EditorGUIUtility.PingObject(go);
			Debug.Log($"[GalaxyCloudInstaller] {(created ? "created" : "verified")} '{go.name}' under hand/ " +
				$"in '{scene.path}' (saved: {changed}); blueprint '{BlueprintPath}', material '{MaterialPath}'. " +
				"Reseed via the component's context menu; tune the blueprint asset for shape and palette.", go);
		}

		static GalaxyBlueprint LoadOrCreateBlueprint()
		{
			GalaxyBlueprint blueprint = AssetDatabase.LoadAssetAtPath<GalaxyBlueprint>(BlueprintPath);
			if (blueprint != null) return blueprint;
			blueprint = ScriptableObject.CreateInstance<GalaxyBlueprint>();
			AssetDatabase.CreateAsset(blueprint, BlueprintPath);
			Debug.Log($"[GalaxyCloudInstaller] authored blueprint '{BlueprintPath}' (procedural spiral, no density map).");
			return blueprint;
		}

		static Material LoadOrCreateMaterial()
		{
			Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
			if (material != null) return material;
			Shader shader = Shader.Find(CloudShaderName);
			if (shader == null)
			{
				Debug.LogError($"[GalaxyCloudInstaller] shader '{CloudShaderName}' not found - is OpenFrontier_GalaxyCloud.shader imported?");
				return null;
			}
			material = new Material(shader) { name = "GalaxyCloud" };
			material.renderQueue = 2990; // backdrop tier: under the nebulae (2999) and gameplay particles (3000)
			AssetDatabase.CreateAsset(material, MaterialPath);
			Debug.Log($"[GalaxyCloudInstaller] authored material '{MaterialPath}' (renderQueue 2990).");
			return material;
		}

		static void EnsureFolder(string parent, string name)
		{
			if (AssetDatabase.IsValidFolder(parent + "/" + name)) return;
			AssetDatabase.CreateFolder(parent, name);
		}
	}
}
