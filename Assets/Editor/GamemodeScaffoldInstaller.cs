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
}
