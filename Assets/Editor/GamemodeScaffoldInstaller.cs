using System.Collections.Generic;
using System.IO;
using OpenFrontier.Gamemodes;
using OpenFrontier.MainMenu;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
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
	///    and a LayoutElement height - under Resources/Prefabs/UIX/. The
	///    background Image is handed to SelectableImageColor (the Button's
	///    targetGraphic released - one owner per graphic) so the CHOSEN row's
	///    bar stays lit, not just its label; GamemodeRow drives both twins.
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
	/// records facts). Card visuals wire by child NAME (Title / Description
	/// / Image) when those children exist; anything unmatched is logged,
	/// never guessed - and only ever fills a still-null field, so hand
	/// wiring always wins.
	///
	/// 4. Every SelectableImageColor in the scene: the hands-on greys
	///    (Highlighted / Pressed / Selected) -> 0, icons stay colored while
	///    the card is interacted with. Selected alone was not enough: with a
	///    mouse present the module keeps the card under the resting cursor
	///    hovered, so a controller-focused card resolved Highlighted
	///    (authored grey) regardless of the Selected value. Rest stays as
	///    authored (these cards author colored-at-rest).
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

			// Before WireScene: its scene save must capture this too.
			EnsureInteractionColours();
			WireScene(newGameScreen, rowPrefab);
			VerifyInteractionColours();
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
					Debug.Log($"{Tag} Row prefab already exists at {RowPath} - checking its background ownership.");
					return EnsureBackgroundOwner(existing);
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
				return EnsureBackgroundOwner(reloaded);
			}

			Debug.LogError($"{Tag} Row prefab verification FAILED after creation - inspect {RowPath} by hand.");
			return null;
		}

		/// <summary>
		/// The chosen row's BAR must stay lit, not just its label - and the
		/// Button's built-in ColorTint cannot express a persistent state, so
		/// its background ownership moves to SelectableImageColor (which
		/// carries the same SetChosen the label twin has). One owner per
		/// graphic: the Button releases its targetGraphic and keeps
		/// transitioning nothing, while the twin covers every state it used
		/// to. The twin's ColorBlock is copied from the Button's so the bar
		/// reads exactly as authored before the handover.
		/// </summary>
		private static GameObject EnsureBackgroundOwner(GameObject rowPrefab)
		{
			Button button = rowPrefab.GetComponent<Button>();
			Image background = rowPrefab.GetComponent<Image>();
			SelectableImageColor imageColor = rowPrefab.GetComponent<SelectableImageColor>();

			if (button == null || background == null)
			{
				Debug.LogWarning($"{Tag} Row prefab has no Button or background Image - the chosen tint stays label-only.");
				return rowPrefab;
			}

			if (imageColor != null && button.targetGraphic == null && imageColor.Target == background)
			{
				Debug.Log($"{Tag} Row prefab background already owned by SelectableImageColor - left untouched.");
				return rowPrefab;
			}

			GameObject contents = PrefabUtility.LoadPrefabContents(RowPath);

			try
			{
				imageColor = contents.GetComponent<SelectableImageColor>();

				if (imageColor == null)
				{
					imageColor = contents.AddComponent<SelectableImageColor>();
				}

				imageColor.ImageColors = contents.GetComponent<Button>().colors;
				imageColor.Target = contents.GetComponent<Image>();
				contents.GetComponent<Button>().targetGraphic = null;

				PrefabUtility.SaveAsPrefabAsset(contents, RowPath);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(contents);
			}

			GameObject reloaded = AssetDatabase.LoadAssetAtPath<GameObject>(RowPath);
			SelectableImageColor check = reloaded != null ? reloaded.GetComponent<SelectableImageColor>() : null;
			bool ok = check != null
				&& check.Target == reloaded.GetComponent<Image>()
				&& reloaded.GetComponent<Button>().targetGraphic == null;

			if (ok)
			{
				Debug.Log($"{Tag} Row prefab upgraded: SelectableImageColor owns the background (Button's color block copied, targetGraphic released) - the CHOSEN row's bar stays lit.");
				return reloaded;
			}

			Debug.LogError($"{Tag} Row prefab upgrade verification FAILED - inspect {RowPath} by hand.");
			return reloaded != null ? reloaded : null;
		}

		private static void EnsureInteractionColours()
		{
			SelectableImageColor[] tints = Object.FindObjectsByType<SelectableImageColor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			int fixedCount = 0;

			for (int i = 0; i < tints.Length; i++)
			{
				SelectableImageColor tint = tints[i];
				bool changed = false;

				// All three hands-on states colored. Fixing Selected alone did
				// not change what the user saw: with a mouse present the
				// module keeps the card under the resting cursor hovered, so
				// the controller-focused card resolved Highlighted (authored
				// grey) and stayed grey regardless of the Selected value.
				if (tint.GreyscaleHighlighted != 0f)
				{
					tint.GreyscaleHighlighted = 0f;
					changed = true;
				}

				if (tint.GreyscalePressed != 0f)
				{
					tint.GreyscalePressed = 0f;
					changed = true;
				}

				if (tint.GreyscaleSelected != 0f)
				{
					tint.GreyscaleSelected = 0f;
					changed = true;
				}

				if (!changed)
				{
					continue;
				}

				EditorUtility.SetDirty(tint);
				fixedCount++;
				Debug.Log($"{Tag} '{GetPath(tint.transform)}': interaction greys (hover/press/selected) -> 0, the icon stays colored whenever its card is active.");
			}

			if (fixedCount > 0)
			{
				AssetDatabase.SaveAssets();
			}
			else
			{
				Debug.Log($"{Tag} Every SelectableImageColor already colors its hands-on states - left untouched.");
			}
		}

		private static void VerifyInteractionColours()
		{
			// The scene is saved by now, so the live objects are the file.
			SelectableImageColor[] tints = Object.FindObjectsByType<SelectableImageColor>(FindObjectsInactive.Include, FindObjectsSortMode.None);

			for (int i = 0; i < tints.Length; i++)
			{
				SelectableImageColor tint = tints[i];

				if (tint.GreyscaleHighlighted != 0f || tint.GreyscalePressed != 0f || tint.GreyscaleSelected != 0f)
				{
					Debug.LogError($"{Tag} Final verification FAILED: '{GetPath(tint.transform)}' still greys a hands-on state.");
					return;
				}
			}

			Debug.Log($"{Tag} Interaction colours verified: icons stay colored on hover, press and selected.");
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
					Debug.Log($"{Tag} GamemodeCard on '{cardTransform.name}' now points at the Content list (was {(cardAdded ? "unwired" : "the misplaced list")}).");
				}
				else
				{
					Debug.Log($"{Tag} GamemodeCard already wired to the Content list - left untouched.");
				}

				WireCardVisuals(cardTransform, card);
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
				Debug.Log($"{Tag} DONE. Exactly one GamemodeList, on '{GetPath(checkTransform)}'. Play and open NewGame: the Development row builds under YOUR Selection Menu Content, and the card mirrors it with no click - the list preselects the first visible mode on open.");
			}
			else
			{
				Debug.LogError($"{Tag} Final scene verification FAILED - inspect '{ContentPath}' by hand.");
			}
		}

		private static void WireCardVisuals(Transform cardTransform, GamemodeCard card)
		{
			TextMeshProUGUI[] labels = cardTransform.GetComponentsInChildren<TextMeshProUGUI>(true);

			for (int i = 0; i < labels.Length; i++)
			{
				TextMeshProUGUI label = labels[i];

				if (label.name == "Title" && card.Title == null)
				{
					card.Title = label;
					Debug.Log($"{Tag} Card Title wired to the '{label.name}' label.");
				}
				else if (label.name == "Description" && card.Description == null)
				{
					card.Description = label;
					Debug.Log($"{Tag} Card Description wired to the '{label.name}' label.");
				}
			}

			// The card root itself carries a background Image - only a child
			// actually named "Image" is the preview, never the root's.
			Image[] images = cardTransform.GetComponentsInChildren<Image>(true);

			for (int i = 0; i < images.Length; i++)
			{
				Image image = images[i];

				if (image.name == "Image" && image.transform != cardTransform && card.Image == null)
				{
					card.Image = image;
					Debug.Log($"{Tag} Card Image wired to the '{image.name}' child (it hides itself until a selection has a PreviewImage).");
				}
			}

			if (card.Title == null || card.Description == null || card.Image == null)
			{
				Debug.LogWarning($"{Tag} Card wiring incomplete - Title {(card.Title != null ? "wired" : "NOT FOUND")}, Description {(card.Description != null ? "wired" : "NOT FOUND")}, Image {(card.Image != null ? "wired" : "NOT FOUND")}. Wire the missing ones in the Inspector, or rename the child labels to 'Title' / 'Description' / 'Image'.");
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

	/// <summary>
	/// One-shot scaffold for the landing scene (run once, verify, then delete
	/// the file together with the gamemode scaffold). Idempotent:
	///
	/// 1. Places the user's authored "Loading Screen" prefab
	///    (Resources/Prefabs/UIX/ - own ortho camera, holo ships, tips UI)
	///    into the open "Landing Scene" and activates the INSTANCE (the
	///    prefab asset ships deactivated for editing comfort).
	/// 2. Guarantees the boot conductor: LandingBoot lives at the SCENE ROOT,
	///    never under UI - the first build parented it under the generated
	///    canvas and that canvas' retirement destroyed it (no swap, caught in
	///    the saved scene). Missing = recreated; parented = moved to root.
	/// 3. Wires the ship lottery: LoadingShipPicker goes on the prefab root
	///    (through the sanctioned LoadPrefabContents flow) with every direct
	///    child named *HOLO as a candidate - one is shown per enable, uniform
	///    random, re-rolled. Adding a ship = drag it in and re-run this tool
	///    (the rescan grows the array). Also fixes a LoadingGUI scale-0 slip
	///    while there (the whole authored UI was collapsed).
	/// 4. Wires the tips: LoadingTips on the prefab's 'Tips' node, labels
	///    filled by child name (Title / Description) only when still null,
	///    starter tips seeded only while the pool is empty - authored tips
	///    always win on a re-run. One tip per enable, uniform random.
	/// 5. Retires the scaffold's own generated UI ("Landing Canvas" and its
	///    backdrop/title/progress bar) plus the scene's loose camera - the
	///    authored screen replaces both. LandingBoot releases its fill
	///    reference (the authored screen has no progress widget) and its
	///    minimum beat moves 0.5 -&gt; 1.5s so the random ship gets a visible
	///    moment at 24 deg/s.
	/// 6. Rewrites EditorBuildSettings to the real scenes, landing first
	///    (build index 0 = the boot scene, the original game's flow). Every
	///    entry whose path no longer exists is dropped - the whole
	///    pre-migration list dangles since the 2026-11-08 rebuild move; any
	///    real extra scene is kept behind the two.
	///
	/// Verifies the live objects after the save, same as the gamemode
	/// scaffold. Requires the Landing Scene to be the ACTIVE scene - the
	/// tool edits and saves it.
	/// </summary>
	public static class LandingScaffoldInstaller
	{
		private const string LandingScenePath = "Assets/OpenFrontier/Scenes/Landing Scene.unity";
		private const string MenuScenePath = "Assets/OpenFrontier/Scenes/Main Menu.unity";
		private const string LoadingScreenPrefabPath = "Assets/OpenFrontier/Resources/Prefabs/UIX/Loading Screen.prefab";
		private const string Tag = "[LandingScaffoldInstaller]";

		[MenuItem("Open Frontier/Scaffold Landing Scene (one-shot)")]
		public static void Scaffold()
		{
			if (EditorSceneManager.GetActiveScene().name != "Landing Scene")
			{
				Debug.LogError($"{Tag} The ACTIVE scene is not 'Landing Scene' - open it (File > Open Scene), then run this again.");
				return;
			}

			EnsureBootSceneList();
			GameObject loadingScreen = EnsureLoadingScreenInstance();
			EnsureBootConductor();
			EnsureShipLottery(loadingScreen);
			EnsureTips(loadingScreen);
			SwapToAuthoredScreen(loadingScreen);
			SaveAndVerify(loadingScreen);
		}

		private static GameObject EnsureLoadingScreenInstance()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LoadingScreenPrefabPath);

			if (prefab == null)
			{
				Debug.LogError($"{Tag} No loading screen prefab at {LoadingScreenPrefabPath} - cannot place it.");
				return null;
			}

			// Match by prefab source, not name: a renamed instance is still
			// the same screen (and an unsaved manual placement is found too).
			GameObject instance = null;

			foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
			{
				if (PrefabUtility.GetCorrespondingObjectFromSource(root) == prefab)
				{
					instance = root;
					break;
				}
			}

			if (instance == null)
			{
				instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, EditorSceneManager.GetActiveScene());
				Debug.Log($"{Tag} Placed the Loading Screen prefab in the scene.");
			}
			else
			{
				Debug.Log($"{Tag} Loading screen already placed in the scene - left in place.");
			}

			// The prefab asset ships deactivated (editing comfort); the boot
			// scene needs the screen visible from frame zero, so the INSTANCE
			// is activated - an override; the asset keeps its own state.
			if (!instance.activeSelf)
			{
				instance.SetActive(true);
				Debug.Log($"{Tag} Activated the Loading Screen instance (the prefab asset itself stays deactivated).");
			}

			return instance;
		}

		private static void EnsureShipLottery(GameObject loadingScreen)
		{
			if (loadingScreen == null)
			{
				return;
			}

			LoadingShipPicker existing = loadingScreen.GetComponent<LoadingShipPicker>();
			bool hasCandidates = existing != null
				&& existing.ships != null
				&& existing.ships.Length > 0
				&& System.Array.Exists(existing.ships, ship => ship != null);

			int holoChildren = 0;

			for (int i = 0; i < loadingScreen.transform.childCount; i++)
			{
				if (loadingScreen.transform.GetChild(i).name.EndsWith("HOLO", System.StringComparison.Ordinal))
				{
					holoChildren++;
				}
			}

			// Drift = the picker exists but the prefab grew new *HOLO children
			// since the array was wired - the rescan grows the array.
			bool arrayDrifted = existing != null && existing.ships != null && existing.ships.Length != holoChildren;

			// A scale-0 canvas collapses the whole authored UI - that reads as
			// a slip, not a hide (a hide is m_IsActive), so it is fixed here.
			Transform gui = loadingScreen.transform.Find("LoadingGUI");
			bool guiCollapsed = gui != null && gui.localScale == Vector3.zero;

			if (hasCandidates && !arrayDrifted && !guiCollapsed)
			{
				Debug.Log($"{Tag} Ship lottery already wired ({holoChildren} *HOLO children) and the GUI stands - left untouched.");
				return;
			}

			// The lottery belongs on the prefab (every placement gets it), so
			// the edit goes through the sanctioned prefab flow.
			GameObject contents = PrefabUtility.LoadPrefabContents(LoadingScreenPrefabPath);

			try
			{
				LoadingShipPicker picker = contents.GetComponent<LoadingShipPicker>();

				if (picker == null)
				{
					picker = contents.AddComponent<LoadingShipPicker>();
					Debug.Log($"{Tag} Added LoadingShipPicker to the Loading Screen prefab root.");
				}

				List<Transform> candidates = new List<Transform>();

				for (int i = 0; i < contents.transform.childCount; i++)
				{
					Transform child = contents.transform.GetChild(i);

					if (child.name.EndsWith("HOLO", System.StringComparison.Ordinal))
					{
						candidates.Add(child);
					}
				}

				picker.ships = candidates.ToArray();
				Debug.Log($"{Tag} Ship lottery wired: {candidates.Count} candidate(s) ({string.Join(", ", candidates.ConvertAll(ship => ship.name))}) - direct children named *HOLO.");

				Transform prefabGui = contents.transform.Find("LoadingGUI");

				if (prefabGui != null && prefabGui.localScale == Vector3.zero)
				{
					prefabGui.localScale = Vector3.one;
					Debug.Log($"{Tag} LoadingGUI scale was 0 - set to 1 (the authored UI was collapsed to nothing).");
				}

				PrefabUtility.SaveAsPrefabAsset(contents, LoadingScreenPrefabPath);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(contents);
			}

			// Verify against the RELOADED prefab, not the in-memory object.
			GameObject reloaded = AssetDatabase.LoadAssetAtPath<GameObject>(LoadingScreenPrefabPath);
			LoadingShipPicker check = reloaded != null ? reloaded.GetComponent<LoadingShipPicker>() : null;
			bool ok = check != null && check.ships != null && check.ships.Length > 0 && check.ships[0] != null;

			if (ok)
			{
				Debug.Log($"{Tag} Ship lottery verified on the reloaded prefab.");
			}
			else
			{
				Debug.LogError($"{Tag} Ship lottery verification FAILED - inspect the prefab by hand.");
			}
		}

		private static void EnsureBootConductor()
		{
			// The conductor must be a SCENE ROOT object: the first scaffold
			// build parented it under the generated canvas, and retiring that
			// canvas destroyed it with the UI - the boot sequence never ran
			// and the landing never handed over (caught in the saved scene:
			// zero LandingBoot references after the swap run).
			LandingBoot boot = Object.FindAnyObjectByType<LandingBoot>();

			if (boot == null)
			{
				new GameObject("Landing Boot", typeof(LandingBoot));
				Debug.Log($"{Tag} Created 'Landing Boot' with LandingBoot at the scene root.");
				return;
			}

			if (boot.transform.parent != null)
			{
				string formerParent = boot.transform.parent.name;
				boot.transform.SetParent(null);
				Debug.Log($"{Tag} Moved LandingBoot from '{formerParent}' to the scene root - a conductor under any UI subtree dies with that UI's cleanup.");
			}
			else
			{
				Debug.Log($"{Tag} LandingBoot already at the scene root - left in place.");
			}
		}

		private static void EnsureTips(GameObject loadingScreen)
		{
			if (loadingScreen == null)
			{
				return;
			}

			Transform tipsTransform = loadingScreen.transform.Find("LoadingGUI/Tips");
			LoadingTips existing = tipsTransform != null ? tipsTransform.GetComponent<LoadingTips>() : null;

			bool wired = existing != null
				&& existing.titleLabel != null
				&& existing.descriptionLabel != null
				&& existing.tips != null
				&& existing.tips.Length > 0;

			if (wired)
			{
				Debug.Log($"{Tag} LoadingTips already wired with authored tips - left untouched.");
				return;
			}

			// The tip system belongs on the prefab (every placement gets it),
			// so the edit goes through the sanctioned prefab flow.
			GameObject contents = PrefabUtility.LoadPrefabContents(LoadingScreenPrefabPath);

			try
			{
				Transform prefabTips = contents.transform.Find("LoadingGUI/Tips");

				if (prefabTips == null)
				{
					Debug.LogError($"{Tag} No 'LoadingGUI/Tips' node in the prefab - wire the tip labels by hand.");
					return;
				}

				LoadingTips tips = prefabTips.GetComponent<LoadingTips>();

				if (tips == null)
				{
					tips = prefabTips.gameObject.AddComponent<LoadingTips>();
					Debug.Log($"{Tag} Added LoadingTips to the prefab's 'Tips' node.");
				}

				// Labels fill only still-null fields - hand wiring always wins.
				TMP_Text[] labels = prefabTips.GetComponentsInChildren<TMP_Text>(true);

				for (int i = 0; i < labels.Length; i++)
				{
					TMP_Text label = labels[i];

					if (label.name == "Title" && tips.titleLabel == null)
					{
						tips.titleLabel = label;
						Debug.Log($"{Tag} LoadingTips titleLabel wired to 'Title'.");
					}
					else if (label.name == "Description" && tips.descriptionLabel == null)
					{
						tips.descriptionLabel = label;
						Debug.Log($"{Tag} LoadingTips descriptionLabel wired to 'Description'.");
					}
				}

				// Starter content only while the pool is empty - user-authored
				// tips always win on a re-run.
				if (tips.tips == null || tips.tips.Length == 0)
				{
					tips.tips = new LoadingTips.Tip[]
					{
						new LoadingTips.Tip { Title = "Welcome aboard", Description = "Open Frontier is under construction - the fleet grows with every build." },
						new LoadingTips.Tip { Title = "Gamemodes", Description = "The NewGame screen lists every registered gamemode. Development is the built-in test mode." },
						new LoadingTips.Tip { Title = "The holo fleet", Description = "Every boot rolls a random ship onto this turntable. Today: Ranger and Overlord." },
					};
					Debug.Log($"{Tag} Seeded 3 starter tips (edit or replace them in the Inspector - LoadingTips on the 'Tips' node).");
				}

				PrefabUtility.SaveAsPrefabAsset(contents, LoadingScreenPrefabPath);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(contents);
			}

			// Verify against the RELOADED prefab, not the in-memory object.
			GameObject reloaded = AssetDatabase.LoadAssetAtPath<GameObject>(LoadingScreenPrefabPath);
			Transform reloadedTips = reloaded != null ? reloaded.transform.Find("LoadingGUI/Tips") : null;
			LoadingTips check = reloadedTips != null ? reloadedTips.GetComponent<LoadingTips>() : null;
			bool ok = check != null && check.titleLabel != null && check.descriptionLabel != null && check.tips != null && check.tips.Length > 0;

			if (ok)
			{
				Debug.Log($"{Tag} Tips verified on the reloaded prefab: labels wired, {check.tips.Length} tip(s) in the pool.");
			}
			else
			{
				Debug.LogError($"{Tag} Tips verification FAILED - inspect the prefab by hand.");
			}
		}

		private static void SwapToAuthoredScreen(GameObject loadingScreen)
		{
			// The conductor survives the swap; its references to the generated
			// UI do not.
			LandingBoot boot = Object.FindAnyObjectByType<LandingBoot>();

			if (boot == null)
			{
				Debug.LogError($"{Tag} No LandingBoot in the scene - the boot conductor is missing.");
				return;
			}

			// The authored screen has no progress widget: release the fill
			// reference BEFORE the canvas it points at is destroyed.
			if (boot.progressFill != null)
			{
				boot.progressFill = null;
				Debug.Log($"{Tag} LandingBoot.progressFill released - the authored screen has no progress widget. Wire one later if you add one; until then the boot paces the handover on the minimum beat alone.");
			}

			// Half a second shows nothing of a turntable at 24 deg/s.
			if (boot.minimumDisplaySeconds != 1.5f)
			{
				boot.minimumDisplaySeconds = 1.5f;
				Debug.Log($"{Tag} LandingBoot minimum beat 0.5 -> 1.5s - the random ship deserves a visible moment. Tune on the LandingBoot component.");
			}

			// The scaffold's generated UI is retired wholesale: the authored
			// screen brings its own canvas, camera and ships.
			GameObject generatedCanvas = GameObject.Find("Landing Canvas");

			if (generatedCanvas != null)
			{
				Object.DestroyImmediate(generatedCanvas);
				Debug.Log($"{Tag} Removed the scaffold's generated 'Landing Canvas' (replaced by the authored Loading Screen).");
			}

			// The scene's loose camera loses to the prefab's LoadingCam; two
			// base cameras would double-render the frame.
			GameObject looseCamera = GameObject.Find("Camera");

			if (looseCamera != null)
			{
				Object.DestroyImmediate(looseCamera);
				Debug.Log($"{Tag} Removed the scene's loose 'Camera' - the Loading Screen prefab brings its own (LoadingCam); two base cameras would double-render.");
			}
		}

		private static void SaveAndVerify(GameObject loadingScreen)
		{
			if (loadingScreen == null)
			{
				return;
			}

			EditorUtility.SetDirty(loadingScreen);
			EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
			Debug.Log($"{Tag} Saved the Landing Scene.");

			// Re-read the live objects as the final check (the in-memory scene
			// is what the saved file now contains).
			LoadingShipPicker picker = loadingScreen.GetComponent<LoadingShipPicker>();
			bool ok = loadingScreen.activeSelf
				&& PrefabUtility.GetCorrespondingObjectFromSource(loadingScreen) == AssetDatabase.LoadAssetAtPath<GameObject>(LoadingScreenPrefabPath)
				&& picker != null
				&& picker.ships != null
				&& picker.ships.Length > 0
				&& GameObject.Find("Landing Canvas") == null
				&& GameObject.Find("Camera") == null;

			LandingBoot boot = Object.FindAnyObjectByType<LandingBoot>();
			ok = ok && boot != null && boot.progressFill == null && boot.menuSceneName == "Main Menu";

			EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
			ok = ok && buildScenes.Length >= 2
				&& buildScenes[0].path == LandingScenePath
				&& buildScenes[1].path == MenuScenePath;

			if (ok)
			{
				Debug.Log($"{Tag} DONE. The landing boots through YOUR Loading Screen: a random *HOLO ship on the turntable (one roll per show), your tips UI, then the menu. The scaffold's generated UI and the scene's loose camera are gone; the Main Menu scene still plays standalone.");
			}
			else
			{
				Debug.LogError($"{Tag} Final verification FAILED - inspect the Landing Scene by hand.");
			}
		}

		private static void EnsureBootSceneList()
		{
			List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
			int dangling = 0;

			// Keep only entries whose path still exists on disk (skipping the
			// two managed here - they are re-added in the right order below).
			// The pre-migration build list dangles wholesale: every path died
			// in the 2026-11-08 rebuild move.
			for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
			{
				EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];

				if (scene.path == LandingScenePath || scene.path == MenuScenePath)
				{
					continue;
				}

				if (!string.IsNullOrEmpty(scene.path) && File.Exists(scene.path))
				{
					scenes.Add(scene);
				}
				else
				{
					dangling++;
				}
			}

			// Landing first: build index 0 is the default boot scene, exactly
			// the original game's flow.
			scenes.Insert(0, new EditorBuildSettingsScene(LandingScenePath, true));
			scenes.Insert(1, new EditorBuildSettingsScene(MenuScenePath, true));
			EditorBuildSettings.scenes = scenes.ToArray();

			if (dangling > 0)
			{
				Debug.Log($"{Tag} Dropped {dangling} dangling build-scene entries (dead pre-migration paths).");
			}

			Debug.Log($"{Tag} Build scene list: index 0 = Landing Scene (boot), index 1 = Main Menu{(scenes.Count > 2 ? $", {scenes.Count - 2} other scene(s) kept" : "")}.");
		}
	}

	/// <summary>
	/// One-shot pass that puts ButtonSounds on every menu button: each prefab
	/// under Resources/Prefabs/UIX/ (Simple Button, Gamemode Row, Fullscreen
	/// Menu - anything carrying a Button, so future button prefabs are
	/// covered by a re-run) and every Button in the open scene. Idempotent:
	/// buttons already carrying the component are left alone.
	///
	/// ButtonSounds owns NO AudioSource component - every blip spawns a
	/// transient one-shot player that dies after the clip, so sounds outlive
	/// the screens they were born on. The first version of this tool relied
	/// on a RequireComponent AudioSource; any empty one still sitting on a
	/// button is stripped here (an AudioSource with a clip assigned by hand
	/// is left alone).
	///
	/// Clips stay NULL by design - sound is authored data, assigned in the
	/// Inspector (hover + click); this pass installs the machinery only and
	/// lists the available UIX clips so they are one Inspector drag away.
	/// Verified against the RELOADED prefabs and the live scene.
	/// </summary>
	public static class ButtonSoundInstaller
	{
		private const string UixFolder = "Assets/OpenFrontier/Resources/Prefabs/UIX";
		private const string AudioClipFolder = "Assets/OpenFrontier/AudioClip/UIX";
		private const string Tag = "[ButtonSoundInstaller]";

		[MenuItem("Open Frontier/Add Button Sounds (one-shot)")]
		public static void Install()
		{
			int prefabButtons = 0;
			int prefabEdited = 0;

			foreach (string prefabPath in Directory.GetFiles(UixFolder, "*.prefab", SearchOption.AllDirectories))
			{
				string path = prefabPath.Replace('\\', '/');
				GameObject contents = PrefabUtility.LoadPrefabContents(path);
				bool changed = false;

				try
				{
					foreach (Button button in contents.GetComponentsInChildren<Button>(true))
					{
						prefabButtons++;

						if (EnsureOnButton(button, $"'{path}'"))
						{
							changed = true;
							prefabEdited++;
						}
					}

					if (changed)
					{
						PrefabUtility.SaveAsPrefabAsset(contents, path);
					}
				}
				finally
				{
					PrefabUtility.UnloadPrefabContents(contents);
				}
			}

			int sceneButtons = 0;
			int sceneEdited = 0;

			foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				sceneButtons++;

				if (EnsureOnButton(button, "Scene"))
				{
					EditorUtility.SetDirty(button);
					sceneEdited++;
				}
			}

			Scene activeScene = EditorSceneManager.GetActiveScene();

			if (sceneEdited > 0)
			{
				if (!string.IsNullOrEmpty(activeScene.path))
				{
					EditorSceneManager.SaveScene(activeScene);
					Debug.Log($"{Tag} Saved the open scene '{activeScene.name}'.");
				}
				else
				{
					Debug.LogWarning($"{Tag} The open scene is untitled - save it yourself to keep the added components.");
				}
			}

			AssetDatabase.SaveAssets();

			// Verify against the RELOADED state - the saved file is the truth.
			bool ok = true;

			foreach (string prefabPath in Directory.GetFiles(UixFolder, "*.prefab", SearchOption.AllDirectories))
			{
				string path = prefabPath.Replace('\\', '/');
				GameObject reloaded = AssetDatabase.LoadAssetAtPath<GameObject>(path);

				if (reloaded == null)
				{
					continue;
				}

				foreach (Button button in reloaded.GetComponentsInChildren<Button>(true))
				{
					if (button.GetComponent<ButtonSounds>() == null)
					{
						Debug.LogError($"{Tag} Verification FAILED: '{path}' '{GetPath(button.transform)}' has no ButtonSounds.");
						ok = false;
						continue;
					}

					AudioSource stray = button.GetComponent<AudioSource>();

					if (stray != null && stray.clip == null)
					{
						Debug.LogError($"{Tag} Verification FAILED: '{path}' '{GetPath(button.transform)}' still carries an empty AudioSource.");
						ok = false;
					}
				}
			}

			foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				if (button.GetComponent<ButtonSounds>() == null)
				{
					Debug.LogError($"{Tag} Verification FAILED: scene '{GetPath(button.transform)}' has no ButtonSounds.");
					ok = false;
					continue;
				}

				AudioSource stray = button.GetComponent<AudioSource>();

				if (stray != null && stray.clip == null)
				{
					Debug.LogError($"{Tag} Verification FAILED: scene '{GetPath(button.transform)}' still carries an empty AudioSource.");
					ok = false;
				}
			}

			if (ok)
			{
				string[] clipFiles = Directory.GetFiles(AudioClipFolder);
				List<string> clipNames = new List<string>();

				foreach (string clipFile in clipFiles)
				{
					if (clipFile.EndsWith(".meta"))
					{
						continue;
					}

					clipNames.Add(Path.GetFileName(clipFile));
				}

				Debug.Log($"{Tag} DONE. {prefabEdited}/{prefabButtons} prefab button(s) and {sceneEdited}/{sceneButtons} scene button(s) gained ButtonSounds" +
					$"{(prefabEdited == 0 && sceneEdited == 0 ? " - every button already carried it" : "")}. " +
					$"Assign the clips in the Inspector (Hover Clip / Click Clip); null plays nothing. " +
					$"Available UIX clips: {(clipNames.Count > 0 ? string.Join(", ", clipNames) : "none")}");
			}
		}

		/// <summary>
		/// Puts ButtonSounds on one button if missing, and strips the empty
		/// AudioSource the first tool version's RequireComponent added (the
		/// one-shot now spawns its sources; an unclipped one on a button is
		/// tool debris - a hand-wired clip is left alone). Returns true when
		/// anything changed.
		/// </summary>
		private static bool EnsureOnButton(Button button, string context)
		{
			bool changed = false;

			if (button.GetComponent<ButtonSounds>() == null)
			{
				button.gameObject.AddComponent<ButtonSounds>();
				Debug.Log($"{Tag} {context}: ButtonSounds added to '{GetPath(button.transform)}'.");
				changed = true;
			}

			AudioSource stray = button.GetComponent<AudioSource>();

			if (stray != null && stray.clip == null)
			{
				Object.DestroyImmediate(stray);
				Debug.Log($"{Tag} {context}: removed the empty AudioSource from '{GetPath(button.transform)}' - one-shot sources are spawned now.");
				changed = true;
			}

			return changed;
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
	}
}
