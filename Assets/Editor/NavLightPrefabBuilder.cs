using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using OpenFrontier.IP.Engine;

namespace OpenFrontier.Editor
{
	// Builds the one asset the navigation-light system needs an editor session
	// for: the shared NavLight prefab (plus its material and quad-mesh assets).
	// Menu: <b>Open Frontier / Build NavLight Prefab</b>.
	//
	// WHY A SCRIPT AND NOT HAND-WRITTEN YAML
	// AGENTS.md documents what a hand-edited prefab with a dropped field does:
	// it fails SILENTLY. Every reference here is assigned through the API and
	// saved by Unity itself, and re-running the menu never rebuilds an
	// existing prefab - it verifies the wiring and reports, so a prefetch
	// click can never detach the instances the user already placed.
	//
	// THE THREE ASSETS
	// - Mesh: NavLightQuad.asset - the persisted quad (4 verts, generous
	//   bounds for the shader's distance growth) generated FROM the runtime
	//   definition, so the authored asset and the self-heal fallback cannot
	//   drift apart.
	// - Material: Nav Lights.mat - one material on the OpenFrontier/Nav
	//   Lights shader with Enable GPU Instancing ON, because the batching
	//   contract for MaterialPropertyBlock'd chips is instancing, not the SRP
	//   Batcher (see NavLight.cs). Same rule as GpuInstancingSetup: written
	//   through the Material API, never through YAML, because the URP
	//   material postprocessor strips hand-edited material YAML.
	// - Prefab: NavLight.prefab - root NavLight + a Quad child (MeshFilter +
	//   MeshRenderer, shadows off: a chip has no business in the shadow map).
	//
	// THE BATCH THAT INSTANCING FOLDS INTO
	// All chips share this mesh and this material (texture variants aside -
	// NavLight caches a clone per flare texture, each still instanced), so a
	// sector's chips are ONE instanced draw per material.
	public static class NavLightPrefabBuilder
	{
		private const string Menu = "Open Frontier/Build NavLight Prefab";

		private const string MeshAssetPath = "Assets/OpenFrontier/Mesh/NavLightQuad.asset";
		private const string MaterialAssetPath = NavLight.NavLightMaterialAssetPath;
		private const string PrefabPath = "Assets/OpenFrontier/Prefabs/NavLight.prefab";

		[MenuItem(Menu)]
		public static void Build()
		{
			// The shader must resolve BEFORE anything is written; a missing
			// shader is an error to fix at the source, not a magenta prefab
			// to discover later in a ship.
			Shader shader = Shader.Find(NavLight.NavLightShaderName);
			if (shader == null)
			{
				Debug.LogError(
					"NavLightPrefabBuilder: shader '" + NavLight.NavLightShaderName +
					"' does not resolve. Fix or import the shader first; nothing was built.");
				return;
			}

			Mesh quadMesh = EnsureMeshAsset();
			Material material = EnsureMaterial(shader);

			GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			if (existing != null)
			{
				// Never rebuild: the prefab may already be nested inside ship
				// prefabs this session placed. Verify instead.
				Verify(existing, quadMesh, material);
				return;
			}

			GameObject root = new GameObject("NavLight");
			NavLight navLight = root.AddComponent<NavLight>();

			GameObject quad = new GameObject("Quad");
			quad.transform.SetParent(root.transform, false);
			MeshFilter meshFilter = quad.AddComponent<MeshFilter>();
			meshFilter.sharedMesh = quadMesh;
			MeshRenderer meshRenderer = quad.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterial = material;
			// A nav chip is decoration: it must not enter the shadow map at
			// any quality level.
			meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			meshRenderer.receiveShadows = false;

			// Capture the defaults for the report BEFORE anything is destroyed:
			// after SaveAsPrefabAsset the scene root (and with it the
			// component's native side) is gone, and reading a destroyed
			// component's state is exactly the "verify, don't assume" hole
			// this builder exists to close.
			string defaults = "color " + navLight.LightColor + ", pattern " +
				navLight.Pattern + ", period " + navLight.FlashPeriod +
				", size " + navLight.Size + ", intensity " + navLight.Intensity;

			// The prefab folder has to exist or SaveAsPrefabAsset fails -
			// nothing in this project has ever created Assets/OpenFrontier/Prefabs.
			EnsureFolder("Assets/OpenFrontier", "Prefabs");
			GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
			Object.DestroyImmediate(root);

			if (prefab == null)
			{
				Debug.LogError("NavLightPrefabBuilder: failed to save " + PrefabPath);
				return;
			}

			AssetDatabase.SaveAssets();
			Selection.activeObject = prefab;
			EditorGUIUtility.PingObject(prefab);

			Debug.Log(
				"NavLightPrefabBuilder: built " + PrefabPath + " (" + quadMesh.name + ", " +
				material.name + ", instancing " + material.enableInstancing + ", NavLight defaults: " +
				defaults + ")");
			Debug.Log(
				"NavLightPrefabBuilder: drag NavLight instances onto ships wherever a lamp belongs " +
				"- port red solid, starboard green solid, tail white solid, beacons red single-flash, " +
				"strobes white double-flash. Orient every chip +Z out of the hull (the gizmo's blue " +
				"line) - the shader rests the flare on that axis. Chips on one ship phase together; " +
				"ships desync.");
		}

		private static Mesh EnsureMeshAsset()
		{
			Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshAssetPath);
			if (existing != null)
			{
				return existing;
			}
			// The layout is authored by the same code the runtime self-heal
			// uses, so the two cannot disagree.
			Mesh mesh = new Mesh();
			NavLight.FillQuadMesh(mesh);
			EnsureFolder("Assets/OpenFrontier", "Mesh");
			AssetDatabase.CreateAsset(mesh, MeshAssetPath);
			return mesh;
		}

		private static Material EnsureMaterial(Shader shader)
		{
			Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialAssetPath);
			if (material == null)
			{
				// The material lives under a Resources root (runtime load in
				// NavLight.MaterialFor) - the NavLight folder itself must
				// exist before CreateAsset, and the old "OpenFrontier/Material"
				// ensure here guaranteed the WRONG folder.
				EnsureFolder("Assets/OpenFrontier/Resources", "NavLight");
				material = new Material(shader)
				{
					name = "Nav Lights"
				};
				AssetDatabase.CreateAsset(material, MaterialAssetPath);
			}
			// The instancing flag is the batching contract for MPB'd chips.
			// GpuInstancingSetup's bulk disable could have taken it away;
			// a run of this menu always brings it back.
			if (!material.enableInstancing)
			{
				material.enableInstancing = true;
				EditorUtility.SetDirty(material);
				Debug.LogWarning(
					"NavLightPrefabBuilder: " + MaterialAssetPath +
					" had Enable GPU Instancing OFF (GpuInstancingSetup's bulk disable was probably run) - turned back on.");
			}
			return material;
		}

		// Re-running the menu reports instead of rebuilding: the prefab may
		// now live nested inside ship prefabs, and a silent rebuild would
		// strand those placements.
		private static void Verify(GameObject prefab, Mesh quadMesh, Material material)
		{
			NavLight navLight = prefab.GetComponent<NavLight>();
			if (navLight == null)
			{
				Debug.LogError(
					"NavLightPrefabBuilder: " + PrefabPath +
					" exists but lost its NavLight component. Delete the prefab and re-run this menu to rebuild it.");
				return;
			}
			MeshFilter meshFilter = prefab.GetComponentInChildren<MeshFilter>(true);
			MeshRenderer meshRenderer = prefab.GetComponentInChildren<MeshRenderer>(true);
			bool meshOk = meshFilter != null && meshFilter.sharedMesh == quadMesh;
			bool materialOk = meshRenderer != null && meshRenderer.sharedMaterial == material;
			bool shadowsOff = meshRenderer == null || meshRenderer.shadowCastingMode ==
				UnityEngine.Rendering.ShadowCastingMode.Off;
			if (meshOk && materialOk && shadowsOff)
			{
				Debug.Log(
					"NavLightPrefabBuilder: " + PrefabPath + " already exists and is wired correctly " +
					"(mesh, material, instancing " + material.enableInstancing + ", shadows off). Nothing rebuilt.");
				return;
			}
			Debug.LogWarning(
				"NavLightPrefabBuilder: " + PrefabPath + " exists but the wiring drifted: mesh " +
				(meshOk ? "ok" : "MISSING/different") + ", material " + (materialOk ? "ok" : "MISSING/different") +
				", shadows " + (shadowsOff ? "off" : "ON") + ". Fix in the editor, or delete the prefab and re-run this menu to rebuild it.");
		}

		private static void EnsureFolder(string parent, string name)
		{
			if (!AssetDatabase.IsValidFolder(parent + "/" + name))
			{
				AssetDatabase.CreateFolder(parent, name);
			}
		}
	}
}
