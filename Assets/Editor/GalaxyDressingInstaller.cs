using OpenFrontier.Engine;
using OpenFrontier.Galaxy;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
// System + UnityEngine both imported: Object is pinned explicitly
// (System.Object vs UnityEngine.Object ambiguity would fail the editor
// compile, the one gate that sees these loose scripts).
using Object = UnityEngine.Object;

namespace OpenFrontier.EditorTools
{
	/// <summary>
	/// Re-runnable: rebuilds the Galaxy scene's VISUAL dressing
	/// (hand/Dressing + Local Sky) around the pinned stage contract,
	/// baking the disk texture and authoring the three materials first.
	/// The engine side (GalaxyStageBoot, GenerationTrigger,
	/// SystemAnchor, cameras, systems/) is NEVER touched -- only the
	/// contents of hand/ are replaced.
	///
	/// The one load-bearing move: the Generation Trigger used to live
	/// under hand/Disk to inherit its scale (its legal region was the
	/// disk's transform). It is MOVED to the Galaxy root with
	/// worldPositionStays BEFORE the old dressing dies, so the component
	/// reference in GalaxyStageBoot survives AND its world-space legal
	/// region is bit-identical (collider size x world scale unchanged).
	///
	/// Every run: bake-if-missing -> author/upgrade materials (Material
	/// API, never hand-edited YAML) -> rebuild dressing -> save, CLOSE,
	/// verify against the RELOADED file. Logging records intents, disk
	/// records facts.
	/// </summary>
	public static class GalaxyDressingInstaller
	{
		private const string Tag = "[GalaxyDressingInstaller]";
		private const string ScenePath = "Assets/OpenFrontier/Scenes/Galaxy.unity";

		private const string QuadMaterialPath = "Assets/OpenFrontier/Materials/Galaxy/GalaxyBackdrop.mat";
		private const string StarMaterialPath = "Assets/OpenFrontier/Materials/Galaxy/GalaxyStars.mat";
		private const string NearMaterialPath = "Assets/OpenFrontier/Materials/Galaxy/GalaxyStarsNear.mat";
		private const string MaterialsFolder = "Assets/OpenFrontier/Materials";
		private const string GalaxyMaterialFolder = "Assets/OpenFrontier/Materials/Galaxy";

		private const string DressingName = "Dressing";
		private const string LocalSkyName = "Local Sky";
		private const float DressingScale = 3000f;

		[MenuItem("Open Frontier/Galaxy — Rebuild Dressing (bake + re-dress)")]
		public static void Install()
		{
			if (!System.IO.File.Exists(ScenePath))
			{
				Debug.LogError($"{Tag} {ScenePath} does not exist - run 'Scaffold Galaxy Scene (one-shot)' first; this tool re-dresses an existing stage.");
				return;
			}

			Scene scaffold = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
			GameObject galaxyRoot = FindSceneRoot(scaffold, "Galaxy");
			if (galaxyRoot == null)
			{
				Debug.LogError($"{Tag} No 'Galaxy' root in {ScenePath} - the scaffold contract is broken; re-scaffold from scratch instead.");
				EditorSceneManager.CloseScene(scaffold, true);
				return;
			}

			GalaxyStageBoot boot = galaxyRoot.GetComponent<GalaxyStageBoot>();
			if (boot == null)
			{
				Debug.LogError($"{Tag} 'Galaxy' root has no GalaxyStageBoot - refusing to dress a stage with no boot.");
				EditorSceneManager.CloseScene(scaffold, true);
				return;
			}

			// ---- capture what the rebuild must preserve ----
			GenerationTrigger trigger = FindComponentInScene<GenerationTrigger>(scaffold);
			BoxCollider triggerBox = trigger != null ? trigger.GetComponent<BoxCollider>() : null;
			if (trigger == null || triggerBox == null)
			{
				Debug.LogError($"{Tag} No GenerationTrigger with a BoxCollider in the scene - the boot's legal region cannot be preserved; aborting before anything is destroyed.");
				EditorSceneManager.CloseScene(scaffold, true);
				return;
			}
			// World facts recorded BEFORE any parenting move.
			Vector3 triggerWorldCenter = triggerBox.bounds.center;
			Vector3 triggerWorldSize = triggerBox.bounds.size;

			// Params: a re-run keeps the user's tuning (the OLD backdrop's
			// knobs carry into the new dressing).
			GalaxyModel.Params modelParams = GalaxyModel.Default(1);
			GalaxyBackdrop existing = FindComponentInScene<GalaxyBackdrop>(scaffold);
			if (existing != null)
			{
				modelParams = existing.Model;
				Debug.Log($"{Tag} Keeping the existing backdrop's model params (seed {modelParams.Seed}).");
			}

			Transform hand = galaxyRoot.transform.Find("hand");
			if (hand == null)
			{
				Debug.LogError($"{Tag} Galaxy/hand is missing - the scaffold contract expects it. Aborting.");
				EditorSceneManager.CloseScene(scaffold, true);
				return;
			}

			// ---- preserve the trigger, then clear the dressing ----
			trigger.transform.SetParent(galaxyRoot.transform, true); // world stays; boot's reference survives
			foreach (Transform child in new List<Transform>(hand.CastToList()))
			{
				Object.DestroyImmediate(child.gameObject);
			}
			Debug.Log($"{Tag} Old hand/ dressing cleared; Generation Trigger re-parented to the Galaxy root (world transform preserved).");

			// ---- bake (only when the texture is missing) ----
			if (!GalaxyBaker.EnsureBaked(modelParams))
			{
				Debug.LogError($"{Tag} Disk texture could not be produced - dressing would be black. Aborting.");
				EditorSceneManager.CloseScene(scaffold, true);
				return;
			}

			// ---- materials (Material API; hand-edited URP YAML is stripped by the postprocessor) ----
			Material quadMaterial = EnsureMaterial(QuadMaterialPath, "OpenFrontier/GalaxyBackdrop", mat =>
			{
				Texture2D disk = AssetDatabase.LoadAssetAtPath<Texture2D>(GalaxyBaker.TexturePath);
				if (disk != null)
				{
					mat.SetTexture("_BaseMap", disk);
				}
				mat.renderQueue = 2990;
			});
			Material starMaterial = EnsureMaterial(StarMaterialPath, "OpenFrontier/GalaxyStars", mat =>
			{
				mat.SetFloat("_NearFadeStart", 60f);
				mat.SetFloat("_NearFadeEnd", 140f);
				mat.renderQueue = 3000;
			});
			Material nearMaterial = EnsureMaterial(NearMaterialPath, "OpenFrontier/GalaxyStars", mat =>
			{
				mat.SetFloat("_NearFadeStart", 0f);
				mat.SetFloat("_NearFadeEnd", 0f); // 0 disables the fade: near stars must blaze
				mat.renderQueue = 3010;
			});
			if (quadMaterial == null || starMaterial == null || nearMaterial == null)
			{
				Debug.LogError($"{Tag} A galaxy shader did not resolve (Shader.Find null). Check 'grep -in \"shader error\" Logs/Editor.log' - a failed reimport keeps rendering the last good version silently. Aborting.");
				EditorSceneManager.CloseScene(scaffold, true);
				return;
			}

			// ---- the dressing ----
			GameObject dressing = new GameObject(DressingName);
			dressing.transform.SetParent(hand, false);
			dressing.transform.localScale = Vector3.one * DressingScale;
			GalaxyBackdrop backdrop = dressing.AddComponent<GalaxyBackdrop>();
			backdrop.QuadMaterial = quadMaterial;
			backdrop.StarMaterial = starMaterial;
			backdrop.Model = modelParams;
			backdrop.HeroStarCount = 4200;
			backdrop.RebuildAll();

			// ---- the local sky (world units, scale 1, OUTSIDE the dressing) ----
			GameObject localSky = new GameObject(LocalSkyName);
			localSky.transform.SetParent(galaxyRoot.transform, false);
			GalaxyLocalStars shell = localSky.AddComponent<GalaxyLocalStars>();
			shell.Model = backdrop;
			shell.Anchor = boot.GalaxyCamera != null ? boot.GalaxyCamera.transform : null;
			localSky.GetComponent<MeshRenderer>().sharedMaterial = nearMaterial;
			shell.RebuildAtAnchor();

			// ---- save, close, verify against the RELOADED file ----
			EditorSceneManager.MarkSceneDirty(scaffold);
			EditorSceneManager.SaveScene(scaffold, ScenePath);
			EditorSceneManager.CloseScene(scaffold, true);

			if (!VerifyAgainstReloadedFile(triggerWorldCenter, triggerWorldSize, modelParams.Seed))
			{
				return; // Loud failures already logged; do not trust-build on top.
			}

			Debug.Log($"{Tag} Dressing rebuilt and verified: hand/{DressingName} (baked quad + {backdrop.HeroStarCount} hero stars, scale x{DressingScale:0}) + '{LocalSkyName}' shell. Tune {GalaxyBaker.TexturePath} via the backdrop's Model knobs + re-run the bake menu.");
		}

		private static GameObject FindSceneRoot(Scene scene, string name)
		{
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				if (root.name == name)
				{
					return root;
				}
			}
			return null;
		}

		private static T FindComponentInScene<T>(Scene scene) where T : Component
		{
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				T component = root.GetComponentInChildren<T>(true);
				if (component != null)
				{
					return component;
				}
			}
			return null;
		}

		private static Transform[] CastToList(this Transform parent)
		{
			Transform[] children = new Transform[parent.childCount];
			for (int i = 0; i < parent.childCount; i++)
			{
				children[i] = parent.GetChild(i);
			}
			return children;
		}

		private static Material EnsureMaterial(string path, string shaderName, Action<Material> configure)
		{
			Shader shader = Shader.Find(shaderName);
			if (shader == null)
			{
				Debug.LogError($"{Tag} Shader '{shaderName}' not found - the shader failed to compile or the name drifted.");
				return null;
			}

			Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
			if (material == null)
			{
				if (!AssetDatabase.IsValidFolder(MaterialsFolder))
				{
					AssetDatabase.CreateFolder("Assets/OpenFrontier", "Materials");
				}
				if (!AssetDatabase.IsValidFolder(GalaxyMaterialFolder))
				{
					AssetDatabase.CreateFolder(MaterialsFolder, "Galaxy");
				}
				material = new Material(shader);
				configure?.Invoke(material);
				AssetDatabase.CreateAsset(material, path);
				Debug.Log($"{Tag} Created {path}.");
			}
			else
			{
				// Upgrade-in-place: re-point the shader (a re-run after a
				// shader edit) and re-apply the configuration.
				material.shader = shader;
				configure?.Invoke(material);
				EditorUtility.SetDirty(material);
			}
			return material;
		}

		private static bool VerifyAgainstReloadedFile(Vector3 triggerWorldCenter, Vector3 triggerWorldSize, int seed)
		{
			Scene reloaded = default;
			bool ok = true;
			try
			{
				reloaded = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

				GameObject galaxyRoot = FindSceneRoot(reloaded, "Galaxy");
				GalaxyStageBoot boot = galaxyRoot != null ? galaxyRoot.GetComponent<GalaxyStageBoot>() : null;
				if (boot == null)
				{
					Debug.LogError($"{Tag} RELOADED FILE: 'Galaxy' root or its GalaxyStageBoot is missing.");
					ok = false;
				}
				else if (boot.SystemsRoot == null || boot.ModesRoot == null || boot.Trigger == null || boot.GalaxyCamera == null)
				{
					Debug.LogError($"{Tag} RELOADED FILE: GalaxyStageBoot wiring did not survive the rebuild (a reference reads null).");
					ok = false;
				}

				// The legal region must be bit-identical after the move.
				GenerationTrigger trigger = boot != null ? boot.Trigger : FindComponentInScene<GenerationTrigger>(reloaded);
				BoxCollider triggerBox = trigger != null ? trigger.GetComponent<BoxCollider>() : null;
				if (triggerBox == null)
				{
					Debug.LogError($"{Tag} RELOADED FILE: GenerationTrigger or its BoxCollider is missing.");
					ok = false;
				}
				else
				{
					Vector3 center = triggerBox.bounds.center;
					Vector3 size = triggerBox.bounds.size;
					if ((center - triggerWorldCenter).magnitude > triggerWorldSize.magnitude * 0.005f
						|| (size - triggerWorldSize).magnitude > triggerWorldSize.magnitude * 0.005f)
					{
						Debug.LogError($"{Tag} RELOADED FILE: the trigger's world legal region drifted " +
							$"(center {center} -> want {triggerWorldCenter}, size {size} -> want {triggerWorldSize}). The disk-scale coupling changed under it.");
						ok = false;
					}
				}

				Transform hand = galaxyRoot != null ? galaxyRoot.transform.Find("hand") : null;
				Transform dressing = hand != null ? hand.Find(DressingName) : null;
				GalaxyBackdrop backdrop = dressing != null ? dressing.GetComponent<GalaxyBackdrop>() : null;
				if (backdrop == null)
				{
					Debug.LogError($"{Tag} RELOADED FILE: hand/{DressingName} or its GalaxyBackdrop is missing.");
					ok = false;
				}
				else
				{
					Transform quad = dressing.Find("Quad");
					Transform stars = dressing.Find("Stars");
					Mesh quadMesh = quad != null ? quad.GetComponent<MeshFilter>().sharedMesh : null;
					Mesh starsMesh = stars != null ? stars.GetComponent<MeshFilter>().sharedMesh : null;
					if (quadMesh == null || quadMesh.vertexCount == 0)
					{
						Debug.LogError($"{Tag} RELOADED FILE: the quad mesh did not survive (empty or missing).");
						ok = false;
					}
					if (starsMesh == null || starsMesh.vertexCount == 0)
					{
						Debug.LogError($"{Tag} RELOADED FILE: the hero star mesh did not survive (empty or missing).");
						ok = false;
					}
					Material quadMat = quad != null ? quad.GetComponent<MeshRenderer>().sharedMaterial : null;
					if (quadMat == null || !quadMat.HasProperty("_BaseMap") || quadMat.GetTexture("_BaseMap") == null)
					{
						Debug.LogError($"{Tag} RELOADED FILE: the quad's material or its baked texture is missing.");
						ok = false;
					}
					if (backdrop.Model.Seed != seed)
					{
						Debug.LogError($"{Tag} RELOADED FILE: the backdrop's params changed in the save (seed {backdrop.Model.Seed} vs {seed}).");
						ok = false;
					}
				}

				Transform localSky = galaxyRoot != null ? galaxyRoot.transform.Find(LocalSkyName) : null;
				GalaxyLocalStars shell = localSky != null ? localSky.GetComponent<GalaxyLocalStars>() : null;
				if (shell == null || shell.Model == null || shell.Anchor == null
					|| localSky.GetComponent<MeshFilter>().sharedMesh == null
					|| localSky.GetComponent<MeshRenderer>().sharedMaterial == null)
				{
					Debug.LogError($"{Tag} RELOADED FILE: '{LocalSkyName}' is missing, unwired or meshless.");
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
				Debug.Log($"{Tag} {ScenePath} verified against the reloaded file: boot wiring intact, legal region bit-identical, dressing + materials + texture on disk.");
			}
			return ok;
		}
	}
}
