using System;
using OpenFrontier.IP.Engine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenFrontier.EditorTools
{
	/// <summary>
	/// One-shot installer for the ED-shaped galaxy renderer. It keeps the
	/// existing realtime cloud (ED has the same billboard layer) and adds
	/// the pieces the cloud alone could not provide:
	///
	///   GalaxyBackdrop  - generated band texture, plane projection, dust,
	///                     density haze, glow
	///   GalaxySkyStars  - the 3D star catalog: real positions, apparent
	///                     magnitudes from camera distance, explorable -
	///                     the perspective sky's presentation of the catalog
	///   GalaxyMapStars  - ED's StarMapStar: textured sheet sprites, the
	///                     orthographic map's presentation of the catalog
	///   Nebulae/        - the 29 real ED nebula volumes scattered across
	///                     the disc under a GalaxyNebulaField draw budget
	///   GalaxyDustCards - LocalDarkNebulaDust as instanced cards (spec §6b)
	///   GalaxySky       - the system's galactic position + the Sky Observer
	///                     anchor the future skybox capture docks at
	///   GalaxyQuality.asset - ED's GalaxyMap/GalaxyBackground ladder
	///
	/// Idempotent: components are matched by type, assets by path. Re-running
	/// repairs wiring and never duplicates the object.
	/// </summary>
	public static class GalaxyRenderInstaller
	{
		const string GalaxyScenePath = "Assets/OpenFrontier/Scenes/Galaxy.unity";
		const string AssetFolder = "Assets/OpenFrontier/Galaxy";
		const string BlueprintPath = AssetFolder + "/DefaultGalaxy.asset";
		const string MaterialPath = AssetFolder + "/GalaxyCloud.mat";
		const string MapStarMaterialPath = AssetFolder + "/GalaxyMapStar.mat";
		const string SkyStarMaterialPath = AssetFolder + "/GalaxySkyStar.mat";
		const string StarSheetPath = AssetFolder + "/GalaxyStarSprites.png";
		const string NebulaePrefabFolder = AssetFolder + "/Nebulae/Prefabs";
		const string QualityPath = AssetFolder + "/GalaxyQuality.asset";
		const string CloudShaderName = "OpenFrontier/Galaxy/Cloud";
		const string MapStarShaderName = "OpenFrontier/Galaxy/MapStar";
		const string CloudObjectName = "Galaxy Cloud";

		[MenuItem("OpenFrontier/Galaxy/Install ED Renderer")]
		public static void Install()
		{
			EnsureFolder("Assets/OpenFrontier", "Galaxy");

			GalaxyBlueprint blueprint = AssetDatabase.LoadAssetAtPath<GalaxyBlueprint>(BlueprintPath);
			if (blueprint == null)
			{
				Debug.LogError($"[GalaxyRenderInstaller] blueprint '{BlueprintPath}' not found. Run OpenFrontier/Galaxy/Install Realtime Cloud first.");
				return;
			}
			GalaxyQualitySettings quality = LoadOrCreateQuality();
			Material cloudMaterial = LoadOrCreateCloudMaterial();
			Texture2D starSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(StarSheetPath);
			Material mapStarMaterial = LoadOrCreateMapStarMaterial(starSheet);
			Material skyStarMaterial = AssetDatabase.LoadAssetAtPath<Material>(SkyStarMaterialPath);
			if (quality == null || cloudMaterial == null) return;
			quality.ApplyToShaders();

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
					Debug.LogError($"[GalaxyRenderInstaller] '{GalaxyScenePath}' not found - scene contract moved?");
					return;
				}
				scene = EditorSceneManager.OpenScene(GalaxyScenePath, OpenSceneMode.Additive);
				openedHere = true;
			}

			Transform hand = null;
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				Transform found = root.name == "hand" ? root.transform : root.transform.Find("hand");
				if (found != null) { hand = found; break; }
			}
			if (hand == null)
			{
				Debug.LogError("[GalaxyRenderInstaller] no 'hand' container in the Galaxy scene - the scene contract changed. Nothing installed.");
				if (openedHere) EditorSceneManager.CloseScene(scene, true);
				return;
			}

			GalaxyInstanceCloud cloud = null;
			foreach (Transform child in hand)
			{
				GalaxyInstanceCloud existing = child.GetComponent<GalaxyInstanceCloud>();
				if (existing != null) { cloud = existing; break; }
			}
			bool createdCloud = cloud == null;
			GameObject go;
			if (createdCloud)
			{
				go = new GameObject(CloudObjectName);
				Undo.RegisterCreatedObjectUndo(go, "Install ED Galaxy Renderer");
				go.transform.SetParent(hand, false);
				go.transform.localPosition = Vector3.zero;
				go.transform.localRotation = Quaternion.Euler(24f, 0f, 12f);
				cloud = go.AddComponent<GalaxyInstanceCloud>();
			}
			else
			{
				go = cloud.gameObject;
			}

			bool changed = createdCloud;
			Undo.RecordObject(cloud, "Wire ED Galaxy Renderer");
			if (cloud.blueprint != blueprint) { cloud.blueprint = blueprint; changed = true; }
			if (cloud.material != cloudMaterial) { cloud.material = cloudMaterial; changed = true; }
			if (cloud.quality != quality) { cloud.quality = quality; changed = true; }

			GalaxyBackdrop backdrop = go.GetComponent<GalaxyBackdrop>();
			if (backdrop == null)
			{
				backdrop = Undo.AddComponent<GalaxyBackdrop>(go);
				changed = true;
			}
			Undo.RecordObject(backdrop, "Wire ED Galaxy Renderer");
			if (backdrop.blueprint != blueprint) { backdrop.blueprint = blueprint; changed = true; }
			if (backdrop.quality != quality) { backdrop.quality = quality; changed = true; }

			GalaxySkyStars stars = go.GetComponent<GalaxySkyStars>();
			if (stars == null)
			{
				stars = Undo.AddComponent<GalaxySkyStars>(go);
				changed = true;
			}
			Undo.RecordObject(stars, "Wire ED Galaxy Renderer");
			if (stars.blueprint != blueprint) { stars.blueprint = blueprint; changed = true; }
			if (stars.quality != quality) { stars.quality = quality; changed = true; }
			if (skyStarMaterial != null && stars.material != skyStarMaterial) { stars.material = skyStarMaterial; changed = true; }

			GalaxyMapStars mapStars = go.GetComponent<GalaxyMapStars>();
			if (mapStars == null)
			{
				mapStars = Undo.AddComponent<GalaxyMapStars>(go);
				changed = true;
			}
			Undo.RecordObject(mapStars, "Wire ED Galaxy Renderer");
			if (mapStars.blueprint != blueprint) { mapStars.blueprint = blueprint; changed = true; }
			if (mapStars.quality != quality) { mapStars.quality = quality; changed = true; }
			if (mapStars.material != mapStarMaterial) { mapStars.material = mapStarMaterial; changed = true; }
			if (mapStars.spriteSheet != starSheet) { mapStars.spriteSheet = starSheet; changed = true; }

			GalaxyDustCards cards = go.GetComponent<GalaxyDustCards>();
			if (cards == null)
			{
				cards = Undo.AddComponent<GalaxyDustCards>(go);
				changed = true;
			}
			Undo.RecordObject(cards, "Wire ED Galaxy Renderer");
			if (cards.blueprint != blueprint) { cards.blueprint = blueprint; changed = true; }
			if (cards.quality != quality) { cards.quality = quality; changed = true; }

			GalaxySky sky = go.GetComponent<GalaxySky>();
			if (sky == null)
			{
				sky = Undo.AddComponent<GalaxySky>(go);
				changed = true;
			}

			PlaceNebulae(go, blueprint, ref changed);

			// Map and sky are explicit render contexts. Keep the catalog
			// sprites on the map cameras at every zoom, and the procedural
			// sky stars on the in-disc camera only.
			if (ConfigureStarLayers(FindCamera("MAP"), true)) changed = true;
			if (ConfigureStarLayers(FindCamera("Galaxy Camera"), false)) changed = true;

			if (changed)
			{
				EditorSceneManager.MarkSceneDirty(scene);
				EditorSceneManager.SaveScene(scene);
			}
			if (openedHere) EditorSceneManager.CloseScene(scene, true);

			Selection.activeGameObject = go;
			EditorGUIUtility.PingObject(go);
			Debug.Log($"[GalaxyRenderInstaller] {(changed ? "installed" : "verified")} ED renderer on '{go.name}' under hand/ " +
				$"in '{scene.path}'. Ladder '{QualityPath}' = {quality.rung} " +
				$"(band {quality.backgroundTextureSize}px, {quality.starCloudInstances} billboards, " +
				$"{quality.localDustCards} dust cards, {quality.skyStarCount} catalog stars, " +
				$"{quality.mapStarSprites} map sprites, {quality.glowTaps} glow taps). " +
				"Band bakes on enable (edit mode too); use OpenFrontier/Galaxy/Preview Map Camera " +
				"for the galaxy view, or tune the quality asset for a different rung.", go);
		}

		[MenuItem("OpenFrontier/Galaxy/Frame Galaxy (Scene View)")]
		public static void FrameGalaxyInSceneView()
		{
			GalaxyBackdrop backdrop = UnityEngine.Object.FindFirstObjectByType<GalaxyBackdrop>();
			if (backdrop == null)
			{
				Debug.LogWarning("[GalaxyRenderInstaller] no GalaxyBackdrop in the open scenes - open the Galaxy scene first.");
				return;
			}
			SceneView view = SceneView.lastActiveSceneView;
			if (view == null) return;

			float radius = backdrop.blueprint != null ? backdrop.blueprint.radius : 10000f;
			// Top-down orthographic framing of the map presentation. Star
			// presentation is by layer (30 map / 31 sky), not by camera
			// projection, so the same map sheet draws here and in perspective.
			view.LookAt(backdrop.transform.position,
				Quaternion.Euler(90f, 0f, 0f), radius * 2.6f, true, true);
			view.orthographic = true;
			view.Repaint();
		}

		/// <summary>
		/// Point the Game view at the galaxy map without entering play mode.
		/// The scene's MAP camera ships disabled and sized for a 50-unit view,
		/// so this is the missing view the galaxy look needs: a top-down
		/// orthographic camera at three disc radii, framing the whole disc.
		/// Disables the Galaxy Camera so only one base camera renders.
		/// </summary>
		[MenuItem("OpenFrontier/Galaxy/Preview Map Camera (Game View)")]
		public static void PreviewMapCamera()
		{
			GalaxyBackdrop backdrop = UnityEngine.Object.FindFirstObjectByType<GalaxyBackdrop>();
			Camera map = FindCamera("MAP");
			if (backdrop == null || map == null)
			{
				Debug.LogWarning("[GalaxyRenderInstaller] needs the Galaxy scene open (GalaxyBackdrop + a 'MAP' camera).");
				return;
			}

			float radius = backdrop.blueprint != null ? backdrop.blueprint.radius : 10000f;
			map.transform.SetPositionAndRotation(
				backdrop.transform.position + Vector3.up * radius * 3f,
				Quaternion.Euler(90f, 0f, 0f));
			map.orthographic = true;
			map.orthographicSize = radius * 1.2f;
			map.nearClipPlane = 0.3f;
			map.farClipPlane = radius * 10f;
			map.enabled = true;
			ConfigureStarLayers(map, true);

			Camera sky = FindCamera("Galaxy Camera");
			if (sky != null) sky.enabled = false;

			Selection.activeGameObject = map.gameObject;
			if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
			Debug.Log("[GalaxyRenderInstaller] MAP camera preview: ortho size " +
				$"{map.orthographicSize:0} at height {map.transform.position.y:0}.", map);
		}

		/// <summary>
		/// The map at ED's usual presentation angle: tilted far enough to
		/// read the disc as a disc, still orthographic so the map's density
		/// quad and textured StarMapStar sprites are drawn. Use this to judge
		/// the band, arms and dust lanes as one object.
		/// </summary>
		[MenuItem("OpenFrontier/Galaxy/Preview Angled Map Camera (Game View)")]
		public static void PreviewAngledMapCamera()
		{
			GalaxyBackdrop backdrop = UnityEngine.Object.FindFirstObjectByType<GalaxyBackdrop>();
			Camera map = FindCamera("MAP");
			if (backdrop == null || map == null)
			{
				Debug.LogWarning("[GalaxyRenderInstaller] needs the Galaxy scene open (GalaxyBackdrop + a 'MAP' camera).");
				return;
			}

			float radius = backdrop.blueprint != null ? backdrop.blueprint.radius : 10000f;
			Vector3 target = backdrop.transform.position;
			Quaternion orbit = Quaternion.Euler(0f, 24f, 0f);
			Vector3 position = target + orbit * new Vector3(0f, radius * 1.15f, radius * 2.1f);
			map.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position, Vector3.up));
			map.orthographic = true;
			map.orthographicSize = radius * 0.8f;
			map.nearClipPlane = 0.3f;
			map.farClipPlane = radius * 10f;
			map.enabled = true;
			ConfigureStarLayers(map, true);

			Camera sky = FindCamera("Galaxy Camera");
			if (sky != null) sky.enabled = false;

			Selection.activeGameObject = map.gameObject;
			if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
			Debug.Log("[GalaxyRenderInstaller] angled MAP preview: ortho size " +
				$"{map.orthographicSize:0} at {position}.", map);
		}

		/// <summary>
		/// The same angle in perspective. This is the view that used to turn
		/// the cloud's world-sized haze into cotton balls; the cloud now
		/// sizes sprites in screen pixels, so the two projections should
		/// agree. Map stars are layer 30 and draw at every zoom, so this
		/// view now shows ED's map sheet as well as the band and billboards.
		/// </summary>
		[MenuItem("OpenFrontier/Galaxy/Preview Angled Perspective (Game View)")]
		public static void PreviewAngledPerspectiveCamera()
		{
			GalaxyBackdrop backdrop = UnityEngine.Object.FindFirstObjectByType<GalaxyBackdrop>();
			Camera map = FindCamera("MAP");
			if (backdrop == null || map == null)
			{
				Debug.LogWarning("[GalaxyRenderInstaller] needs the Galaxy scene open (GalaxyBackdrop + a 'MAP' camera).");
				return;
			}

			float radius = backdrop.blueprint != null ? backdrop.blueprint.radius : 10000f;
			Vector3 target = backdrop.transform.position;
			Quaternion orbit = Quaternion.Euler(0f, 24f, 0f);
			Vector3 position = target + orbit * new Vector3(0f, radius * 1.2f, radius * 2.4f);
			map.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position, Vector3.up));
			map.orthographic = false;
			map.fieldOfView = 45f;
			map.nearClipPlane = 0.3f;
			map.farClipPlane = radius * 10f;
			map.enabled = true;
			ConfigureStarLayers(map, true);

			Camera sky = FindCamera("Galaxy Camera");
			if (sky != null) sky.enabled = false;

			Selection.activeGameObject = map.gameObject;
			if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
			Debug.Log("[GalaxyRenderInstaller] angled perspective preview: FOV " +
				$"{map.fieldOfView:0} at {position}.", map);
		}

		/// <summary>Back to the in-disc sky: the perspective Galaxy Camera.</summary>
		[MenuItem("OpenFrontier/Galaxy/Preview Sky Camera (Game View)")]
		public static void PreviewSkyCamera()
		{
			Camera sky = FindCamera("Galaxy Camera");
			if (sky == null)
			{
				Debug.LogWarning("[GalaxyRenderInstaller] no 'Galaxy Camera' in the open scenes.");
				return;
			}

			// The authored system sits above the plane. A dead-level camera
			// shows only the far band low in the frame and none of the 3D
			// cloud, which reads as an empty tan wall. Pitch toward the
			// galactic plane ahead so the first sky view shows the band, the
			// near starfield and the disc together. Preserve the authored
			// position and yaw; only the pitch changes.
			GalaxyBackdrop backdrop = UnityEngine.Object.FindFirstObjectByType<GalaxyBackdrop>();
			if (backdrop != null)
			{
				float radius = backdrop.blueprint != null ? backdrop.blueprint.radius : 10000f;
				Vector3 position = sky.transform.position;
				Vector3 forward = sky.transform.forward;
				forward.y = 0f;
				if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
				forward.Normalize();
				Vector3 target = new Vector3(
					position.x + forward.x * radius,
					backdrop.transform.position.y,
					position.z + forward.z * radius);
				sky.transform.LookAt(target);
			}

			sky.enabled = true;
			ConfigureStarLayers(sky, false);
			Camera map = FindCamera("MAP");
			if (map != null) map.enabled = false;

			Selection.activeGameObject = sky.gameObject;
			if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(sky.gameObject.scene);
		}

		const int MapStarLayer = 30;
		const int SkyStarLayer = 31;

		/// <summary>
		/// Star presentation is a layer contract, not a camera-distance
		/// test: map cameras see layer 30, the in-disc Galaxy Camera sees
		/// layer 31. Both masks keep every non-star layer (band, density,
		/// dust, cloud, glow) visible.
		/// </summary>
		static bool ConfigureStarLayers(Camera camera, bool map)
		{
			if (camera == null) return false;
			int mapBit = 1 << MapStarLayer;
			int skyBit = 1 << SkyStarLayer;
			int mask = map
				? (camera.cullingMask | mapBit) & ~skyBit
				: (camera.cullingMask | skyBit) & ~mapBit;
			if (mask == camera.cullingMask) return false;
			camera.cullingMask = mask;
			return true;
		}

		static Camera FindCamera(string name)
		{
			Camera[] cameras = UnityEngine.Object.FindObjectsByType<Camera>(
				FindObjectsInactive.Include, FindObjectsSortMode.None);
			foreach (Camera camera in cameras)
				if (camera.name == name) return camera;
			return null;
		}

		static GalaxyQualitySettings LoadOrCreateQuality()
		{
			GalaxyQualitySettings quality = AssetDatabase.LoadAssetAtPath<GalaxyQualitySettings>(QualityPath);
			if (quality != null) return quality;

			quality = ScriptableObject.CreateInstance<GalaxyQualitySettings>();
			BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
			bool mobile = target == BuildTarget.Android || target == BuildTarget.iOS
				|| target == BuildTarget.Switch;
			quality.ApplyPreset(mobile ? GalaxyQualityRung.Mobile : GalaxyQualityRung.Desktop);
			AssetDatabase.CreateAsset(quality, QualityPath);
			Debug.Log($"[GalaxyRenderInstaller] authored quality ladder '{QualityPath}' ({quality.rung}).");
			return quality;
		}

		static Material LoadOrCreateCloudMaterial()
		{
			Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
			if (material != null) return material;
			Shader shader = Shader.Find(CloudShaderName);
			if (shader == null)
			{
				Debug.LogError($"[GalaxyRenderInstaller] shader '{CloudShaderName}' not found - is OpenFrontier_GalaxyCloud.shader imported?");
				return null;
			}
			material = new Material(shader) { name = "GalaxyCloud" };
			material.renderQueue = 2990;
			AssetDatabase.CreateAsset(material, MaterialPath);
			Debug.Log($"[GalaxyRenderInstaller] authored material '{MaterialPath}' (renderQueue 2990).");
			return material;
		}

		static Material LoadOrCreateMapStarMaterial(Texture2D sheet)
		{
			Material material = AssetDatabase.LoadAssetAtPath<Material>(MapStarMaterialPath);
			if (material == null)
			{
				Shader shader = Shader.Find(MapStarShaderName);
				if (shader == null)
				{
					Debug.LogError($"[GalaxyRenderInstaller] shader '{MapStarShaderName}' not found - is OpenFrontier_GalaxyMapStar.shader imported?");
					return null;
				}
				material = new Material(shader) { name = "GalaxyMapStar" };
				material.renderQueue = 2991;
				AssetDatabase.CreateAsset(material, MapStarMaterialPath);
				Debug.Log($"[GalaxyRenderInstaller] authored material '{MapStarMaterialPath}' (renderQueue 2991).");
			}
			if (sheet != null)
			{
				int spriteTex = Shader.PropertyToID("_SpriteTex");
				if (material.GetTexture(spriteTex) != sheet)
				{
					material.SetTexture(spriteTex, sheet);
					EditorUtility.SetDirty(material);
				}
			}
			return material;
		}

		/// <summary>
		/// Places the 29 real ED nebula volumes under a "Nebulae" container
		/// with a GalaxyNebulaField draw budget - ED's own model, where every
		/// volume is placed and the rung decides how many draw. Positions are
		/// seeded from the blueprint and weighted by its density field, so the
		/// field follows the arms rather than filling the disc evenly. Existing
		/// children are never moved: after the first install these are ordinary
		/// scene objects and hand placement wins.
		/// </summary>
		static void PlaceNebulae(GameObject parent, GalaxyBlueprint blueprint, ref bool changed)
		{
			Transform container = parent.transform.Find("Nebulae");
			if (container == null)
			{
				var containerGo = new GameObject("Nebulae");
				Undo.RegisterCreatedObjectUndo(containerGo, "Install ED Galaxy Renderer");
				containerGo.transform.SetParent(parent.transform, false);
				container = containerGo.transform;
				changed = true;
			}
			if (container.GetComponent<GalaxyNebulaField>() == null)
			{
				Undo.AddComponent<GalaxyNebulaField>(container.gameObject);
				changed = true;
			}

			string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { NebulaePrefabFolder });
			if (guids.Length == 0)
			{
				Debug.LogWarning($"[GalaxyRenderInstaller] no nebula prefabs in '{NebulaePrefabFolder}' - the volume field stays empty.");
				return;
			}
			Array.Sort(guids, (a, b) => string.CompareOrdinal(
				AssetDatabase.GUIDToAssetPath(a), AssetDatabase.GUIDToAssetPath(b)));

			var rng = new System.Random(blueprint.seed ^ 0x4e454255); // 'NEBU'
			Vector3[] targets = ScatterNebulaPositions(blueprint, guids.Length, rng);
			for (int i = 0; i < guids.Length; i++)
			{
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
				if (prefab == null || container.Find(prefab.name) != null) continue;

				var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, container);
				Undo.RegisterCreatedObjectUndo(instance, "Install ED Galaxy Renderer");
				instance.transform.localPosition = targets[i];
				instance.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
				instance.transform.localScale *= 0.75f + 0.6f * (float)rng.NextDouble();
				changed = true;
			}
		}

		static Vector3[] ScatterNebulaPositions(GalaxyBlueprint blueprint, int count, System.Random rng)
		{
			const int Grid = 64;
			var weights = new float[Grid * Grid];
			double total = 0.0;
			for (int y = 0; y < Grid; y++)
			{
				for (int x = 0; x < Grid; x++)
				{
					float cx = (x + 0.5f) / Grid * 2f - 1f;
					float cz = (y + 0.5f) / Grid * 2f - 1f;
					float r01 = Mathf.Sqrt(cx * cx + cz * cz);
					float d = r01 >= 0.92f ? 0f : blueprint.SampleDensity(cx, cz);
					weights[y * Grid + x] = d;
					total += d;
				}
			}
			if (total <= 0.0) total = 1.0;

			var positions = new Vector3[count];
			for (int i = 0; i < count; i++)
			{
				double u = rng.NextDouble() * total;
				double acc = 0.0;
				int pick = 0;
				for (int c = 0; c < weights.Length; c++)
				{
					acc += weights[c];
					pick = c;
					if (acc >= u) break;
				}
				float cellX = (pick % Grid + (float)rng.NextDouble()) / Grid * 2f - 1f;
				float cellZ = (pick / Grid + (float)rng.NextDouble()) / Grid * 2f - 1f;
				float r01 = Mathf.Sqrt(cellX * cellX + cellZ * cellZ);

				double u1 = Math.Max(rng.NextDouble(), 1e-12);
				double u2 = rng.NextDouble();
				float y = (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2))
					* blueprint.VerticalSigma(r01) * 2.2f;

				positions[i] = new Vector3(cellX * blueprint.radius, y, cellZ * blueprint.radius);
			}
			return positions;
		}

		static void EnsureFolder(string parent, string name)
		{
			if (AssetDatabase.IsValidFolder(parent + "/" + name)) return;
			AssetDatabase.CreateFolder(parent, name);
		}
	}
}
