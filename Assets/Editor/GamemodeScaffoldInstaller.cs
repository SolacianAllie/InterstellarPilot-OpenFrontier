using OpenFrontier.Gamemodes;
using OpenFrontier.MainMenu;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.EditorTools
{
	/// <summary>
	/// One-shot scaffold for the NewGame gamemode screen (run once, verify,
	/// then delete the file). Three jobs, all idempotent:
	///
	/// 1. The Development GamemodeDefinition asset under Resources/Gamemodes/
	///    - the registry auto-loads that folder, so the asset alone makes the
	///    mode appear; no registration code exists or is wanted.
	/// 2. The Gamemode Row prefab - a clone of Simple Button plus GamemodeRow
	///    and a LayoutElement height - under Resources/Prefabs/UIX/.
	/// 3. Scene wiring on the NewGame screen: GamemodeList on the Selection
	///    Menu's Content, ContentSizeFitter beside its authored
	///    VerticalLayoutGroup, and GamemodeCard on the "Gamemode Card" object.
	///
	/// The Content is resolved by EXPLICIT PATH
	/// (NewGameMenu/SelectionMenu/Scroll View/Viewport/Content), never
	/// GetComponentInChildren: the first pass of this tool used that search,
	/// found the Fullscreen Menu prefab's own outer Scroll, and built the
	/// rows under the wrong Content. It now also removes a GamemodeList it
	/// finds anywhere else under the screen (the misplaced first run).
	///
	/// Every step verifies against the RELOADED file before reporting success
	/// (the MenuScaffoldInstaller lesson: logging records intents, disk
	/// records facts). Card labels/image are deliberately left for the user
	/// - the tool does not guess the card's visual layout.
	/// </summary>
	public static class GamemodeScaffoldInstaller
	{
		private const string DefinitionPath = "Assets/OpenFrontier/Resources/Gamemodes/Development.asset";
		private const string RowPath = "Assets/OpenFrontier/Resources/Prefabs/UIX/Gamemode Row.prefab";
		private const string SimpleButtonPath = "Assets/OpenFrontier/Resources/Prefabs/UIX/Simple Button.prefab";
		private const string ContentPath = "NewGameMenu/SelectionMenu/Scroll View/Viewport/Content";
		private const string CardPath = "NewGameMenu/Gamemode Card";
		private const string Tag = "[GamemodeScaffoldInstaller]";

		[MenuItem("Open Frontier/Scaffold Gamemode Screen (one-shot)")]
		public static void Scaffold()
		{
			GameObject newGameScreen = FindScreen("NewGame");

			if (newGameScreen == null)
			{
				Debug.LogError($"{Tag} No 'NewGame' screen (a MenuScreen root named NewGame) in the open scene - open the Main Menu scene first.");
				return;
			}

			GamemodeDefinition definition = EnsureDefinition();
			GameObject rowPrefab = EnsureRowPrefab();

			if (definition == null || rowPrefab == null)
			{
				Debug.LogError($"{Tag} Asset steps failed - scene wiring skipped.");
				return;
			}

			WireScene(newGameScreen, rowPrefab);
		}

		private static GameObject FindScreen(string name)
		{
			MenuScreen[] screens = Object.FindObjectsByType<MenuScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None);

			for (int i = 0; i < screens.Length; i++)
			{
				if (screens[i].name == name)
				{
					return screens[i].gameObject;
				}
			}

			return null;
		}

		private static GamemodeDefinition EnsureDefinition()
		{
			GamemodeDefinition existing = AssetDatabase.LoadAssetAtPath<GamemodeDefinition>(DefinitionPath);

			if (existing != null)
			{
				Debug.Log($"{Tag} Definition already exists at {DefinitionPath} (Id '{existing.Id}') - left untouched.");
				return existing;
			}

			if (!AssetDatabase.IsValidFolder("Assets/OpenFrontier/Resources/Gamemodes"))
			{
				AssetDatabase.CreateFolder("Assets/OpenFrontier/Resources", "Gamemodes");
			}

			GamemodeDefinition definition = ScriptableObject.CreateInstance<GamemodeDefinition>();
			definition.Id = "core.devtest";
			definition.Title = "Development";
			definition.Description = "Internal development test mode. Visible in the editor and development builds only - the registry filters it out of release builds.";
			definition.SortOrder = 999;
			definition.DeveloperOnly = true;
			AssetDatabase.CreateAsset(definition, DefinitionPath);
			AssetDatabase.SaveAssets();

			// Verify against the reloaded file, not the in-memory object.
			GamemodeDefinition reloaded = AssetDatabase.LoadAssetAtPath<GamemodeDefinition>(DefinitionPath);
			bool ok = reloaded != null && reloaded.Id == "core.devtest" && reloaded.DeveloperOnly && reloaded.SortOrder == 999;

			if (ok)
			{
				Debug.Log($"{Tag} Created {DefinitionPath}: Id 'core.devtest', Title 'Development', SortOrder 999, DeveloperOnly - the registry picks it up automatically.");
				return reloaded;
			}

			Debug.LogError($"{Tag} Definition asset verification FAILED after creation - inspect {DefinitionPath} by hand.");
			return null;
		}

		private static GameObject EnsureRowPrefab()
		{
			GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(RowPath);

			if (existing != null)
			{
				if (existing.GetComponent<GamemodeRow>() != null)
				{
					Debug.Log($"{Tag} Row prefab already exists at {RowPath} - left untouched.");
					return existing;
				}

				Debug.LogWarning($"{Tag} {RowPath} exists but has no GamemodeRow - left untouched, wire it by hand.");
				return null;
			}

			GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SimpleButtonPath);

			if (source == null)
			{
				Debug.LogError($"{Tag} Simple Button prefab missing at {SimpleButtonPath} - cannot build the row prefab.");
				return null;
			}

			GameObject contents = PrefabUtility.LoadPrefabContents(SimpleButtonPath);

			try
			{
				contents.name = "Gamemode Row";
				contents.AddComponent<LayoutElement>().preferredHeight = contents.GetComponent<RectTransform>().sizeDelta.y;
				contents.AddComponent<GamemodeRow>();

				TextMeshProUGUI label = contents.GetComponentInChildren<TextMeshProUGUI>(true);

				if (label != null)
				{
					label.text = "Gamemode";
				}

				PrefabUtility.SaveAsPrefabAsset(contents, RowPath);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(contents);
			}

			GameObject reloaded = AssetDatabase.LoadAssetAtPath<GameObject>(RowPath);
			bool ok = reloaded != null
				&& reloaded.GetComponent<GamemodeRow>() != null
				&& reloaded.GetComponent<Button>() != null
				&& reloaded.GetComponentInChildren<TextMeshProUGUI>(true) != null;

			if (ok)
			{
				Debug.Log($"{Tag} Created {RowPath} (Simple Button clone + GamemodeRow + LayoutElement height {reloaded.GetComponent<RectTransform>().sizeDelta.y}).");
				return reloaded;
			}

			Debug.LogError($"{Tag} Row prefab verification FAILED after creation - inspect {RowPath} by hand.");
			return null;
		}

		private static void WireScene(GameObject newGameScreen, GameObject rowPrefab)
		{
			Transform contentTransform = newGameScreen.transform.Find(ContentPath);

			if (contentTransform == null)
			{
				Debug.LogError($"{Tag} No object at '{ContentPath}' under the NewGame screen - open the NewGame screen's Selection Menu scroll and wire its Content by hand.");
				return;
			}

			GameObject content = contentTransform.gameObject;

			// The first scaffold pass landed the list on the Fullscreen Menu
			// prefab's own outer Content (GetComponentInChildren found its
			// Scroll first). Remove any GamemodeList that is not on the
			// resolved Content - it is a scaffold artifact, not authoring.
			GamemodeList[] lists = newGameScreen.GetComponentsInChildren<GamemodeList>(true);

			for (int i = lists.Length - 1; i >= 0; i--)
			{
				if (lists[i].gameObject != content)
				{
					Debug.Log($"{Tag} Removed misplaced GamemodeList from '{GetPath(lists[i].transform)}' (first run wired the prefab's outer Content by mistake).");
					Object.DestroyImmediate(lists[i]);
				}
			}

			bool listAdded = EnsureComponent<GamemodeList>(content, out GamemodeList list);
			list.RowPrefab = rowPrefab.GetComponent<GamemodeRow>();

			if (listAdded)
			{
				Debug.Log($"{Tag} Added GamemodeList to '{GetPath(contentTransform)}' with the row prefab.");
			}
			else
			{
				Debug.Log($"{Tag} GamemodeList already on '{GetPath(contentTransform)}' - RowPrefab (re)assigned.");
			}

			// The Content's VerticalLayoutGroup is user-authored (spacing 10,
			// MiddleCenter, control width) - leave it exactly as it is.
			VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();

			if (layout == null)
			{
				layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
				layout.childAlignment = TextAnchor.UpperCenter;
				layout.spacing = 8f;
				layout.childControlWidth = true;
				layout.childControlHeight = false;
				layout.childForceExpandWidth = true;
				layout.childForceExpandHeight = false;
				Debug.Log($"{Tag} Added VerticalLayoutGroup to the Content (none was authored).");
			}
			else
			{
				Debug.Log($"{Tag} VerticalLayoutGroup on the Content left as authored.");
			}

			// The fitter is the piece that makes the Content grow with its
			// rows - without it a one-row list still scrolls correctly once
			// more modes exist.
			ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();

			if (fitter == null)
			{
				fitter = content.gameObject.AddComponent<ContentSizeFitter>();
				Debug.Log($"{Tag} Added ContentSizeFitter to the Content.");
			}

			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

			Transform cardTransform = newGameScreen.transform.Find(CardPath);

			if (cardTransform != null)
			{
				bool cardAdded = EnsureComponent<GamemodeCard>(cardTransform.gameObject, out GamemodeCard card);

				if (card.List != list)
				{
					card.List = list;
					Debug.Log($"{Tag} GamemodeCard on '{cardTransform.name}' now points at the Content list (was '{(cardAdded ? "unwired" : "the misplaced list")}' ). Its Title/Description/Image children stay yours - the tool does not guess the card's visual layout.");
				}
				else
				{
					Debug.Log($"{Tag} GamemodeCard already wired to the Content list - left untouched.");
				}
			}
			else
			{
				Debug.LogWarning($"{Tag} No '{CardPath}' object found under the NewGame screen - the preview card stays unwired.");
			}

			EditorUtility.SetDirty(content);
			EditorUtility.SetDirty(newGameScreen);
			AssetDatabase.SaveAssets();

			if (EditorSceneManager.GetActiveScene().name == "Main Menu")
			{
				EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
				Debug.Log($"{Tag} Saved the Main Menu scene.");
			}
			else
			{
				EditorSceneManager.MarkAllScenesDirty();
				Debug.LogWarning($"{Tag} The active scene is not 'Main Menu' - the scene is dirty, SAVE IT YOURSELF.");
			}

			// Re-read the live objects as the final check (the in-memory scene
			// is what the saved file now contains).
			Transform checkTransform = newGameScreen.transform.Find(ContentPath);
			GamemodeList check = checkTransform != null ? checkTransform.GetComponent<GamemodeList>() : null;
			bool finalOk = check != null
				&& check.RowPrefab != null
				&& checkTransform.GetComponent<ContentSizeFitter>() != null
				&& checkTransform.GetComponent<ContentSizeFitter>().verticalFit == ContentSizeFitter.FitMode.PreferredSize
				&& newGameScreen.GetComponentsInChildren<GamemodeList>(true).Length == 1;

			if (finalOk)
			{
				Debug.Log($"{Tag} DONE. Exactly one GamemodeList, on '{GetPath(checkTransform)}'. Play and open NewGame: the Development row now builds under YOUR Selection Menu Content.");
			}
			else
			{
				Debug.LogError($"{Tag} Final scene verification FAILED - inspect '{ContentPath}' by hand.");
			}
		}

		private static string GetPath(Transform transform)
		{
			string path = transform.name;
			Transform current = transform.parent;

			while (current != null)
			{
				path = current.name + "/" + path;
				current = current.parent;
			}

			return path;
		}

		private static bool EnsureComponent<T>(GameObject target, out T component) where T : Component
		{
			component = target.GetComponent<T>();

			if (component != null)
			{
				return false;
			}

			component = target.AddComponent<T>();
			Debug.Log($"{Tag} Added {typeof(T).Name} to '{target.name}'.");
			return true;
		}
	}
}
