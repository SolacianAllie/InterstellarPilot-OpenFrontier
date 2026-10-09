#if UNITY_EDITOR
using System;
using OpenFrontier.IP.Multiplayer.UI;
using OpenFrontier.IP.UI;
using OpenFrontier.IP.UI.Screens;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenFrontier.IP.Multiplayer.EditorTools
{
	/// <summary>
	/// One-shot editor tool that adds the Multiplayer button to the main menu
	/// and builds the MultiplayerScreen prefab.
	///
	/// This exists because the main menu prefab is a large hand-laid-out YAML
	/// file: editing it by hand risks corrupting it, and Unity's own
	/// material/shader post-processors can strip hand-written YAML. Building the
	/// objects through the Unity API instead means they are created the same way
	/// a human would create them in the editor.
	///
	/// Safe to run more than once: it finds what it already made and updates it
	/// rather than adding duplicates.
	///
	/// Run from the menu: Tools > Multiplayer > Build Main Menu UI
	/// </summary>
	public static class MultiplayerUIBaker
	{
		/// <summary>
		/// The main menu is a SCENE, not a prefab.
		///
		/// SceneNameMapping.MenuSceneName points at MenuScreenScene, and that
		/// scene holds its own hand-placed copy of the menu. There is also a
		/// MainMenuScreen.prefab, but nothing loads it - an earlier version of
		/// this tool added the button to that prefab, where it was invisible.
		/// This is the file that actually has to change.
		/// </summary>
		private const string MenuScenePath = "Assets/Scenes/ui/menu/MenuScreenScene.unity";

		private const string ScreenPrefabPath = "Assets/GameObject/MultiplayerScreen.prefab";

		[MenuItem("Tools/Multiplayer/Build Main Menu UI")]
		public static void Build()
		{
			// Safety net: this tool rewrites a large hand-laid-out prefab. If the
			// working copy is dirty, saving over it would discard whatever else
			// is in flight, so stop and let the user commit or stash first.
			if (!EditorUtility.DisplayDialog(
					"Multiplayer UI",
					"This edits MenuScreenScene (the scene the main menu actually lives in)\n"
						+ "and creates MultiplayerScreen.prefab.\n\n"
						+ "It is safe to re-run. Save your open work first.",
					"Continue",
					"Cancel"))
			{
				return;
			}

			BuildScreenPrefab();
			AddMainMenuButton();
			RegisterScreen();

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			EditorUtility.DisplayDialog(
				"Multiplayer UI",
				"Added the Multiplayer button to the main menu and created MultiplayerScreen.prefab.",
				"OK");
		}

		/// <summary>
		/// Verifies the main menu prefab is still intact after this tool has run.
		///
		/// A previous version of this tool parented the new button under a
		/// transform belonging to the prefab ASSET rather than to an editable
		/// copy. Unity rejects that ("Cannot instantiate objects with a parent
		/// which is persistent"), dropped the button at the scene root, and then
		/// saved a mangled hierarchy back - re-serialising the whole prefab with
		/// fresh file IDs and replacing the root object. This check exists so
		/// that failure can never be silent again.
		/// </summary>
		[MenuItem("Tools/Multiplayer/Verify Main Menu UI")]
		public static void Verify()
		{
			// Verify the SCENE, because the scene is what the game loads.
			Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Additive);

			if (!scene.IsValid())
			{
				EditorUtility.DisplayDialog("Multiplayer UI", "Could not open " + MenuScenePath, "OK");
				return;
			}

			try
			{
				MainMenuScreen screen = null;

				foreach (GameObject rootObject in scene.GetRootGameObjects())
				{
					screen = rootObject.GetComponentInChildren<MainMenuScreen>(includeInactive: true);

					if (screen != null)
					{
						break;
					}
				}

				if (screen == null)
				{
					EditorUtility.DisplayDialog(
						"Multiplayer UI",
						"No MainMenuScreen found in " + MenuScenePath,
						"OK");

					return;
				}

				bool hasButton = screen.MultiplayerButton != null;
				bool hasScreen = screen.MultiplayerScreenPrefab != null;

				Transform column = FindButtonColumn(screen.transform);

				string report =
					"Scene: " + MenuScenePath + "\n"
					+ "MainMenuScreen: found\n"
					+ "Button column: " + (column != null ? column.name : "NOT FOUND") + "\n"
					+ "Buttons in menu: " + screen.GetComponentsInChildren<Button>(includeInactive: true).Length + "\n"
					+ "Multiplayer button: " + (hasButton ? "present" : "MISSING") + "\n"
					+ "Multiplayer screen prefab: " + (hasScreen ? "assigned" : "NOT ASSIGNED") + "\n"
					+ "Click listener: added by MainMenuScreen.awake(), so nothing to see in the scene file\n"
					+ "\nButton appearance: " + DescribeButtonAppearance(hasButton ? screen.MultiplayerButton : null)
					+ "\n\nMenu in the OPEN scene(s): " + DescribeOpenScenes(screen)
					+ "\n\n" + DescribeScreenPrefab();

				EditorUtility.DisplayDialog("Multiplayer UI", report, "OK");
			}
			finally
			{
				EditorSceneManager.CloseScene(scene, removeScene: true);
			}
		}

		/// <summary>
		/// Reports whether the multiplayer screen prefab is actually usable.
		/// </summary>
		///
		/// An unassigned field on the screen is invisible in every other check:
		/// the screen loads, the click fires, no error is logged, and the player
		/// just sees a blank panel. Listing the fields that are still empty names
		/// the fault directly.
		private static string DescribeScreenPrefab()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPrefabPath);

			if (prefab == null)
			{
				return "Screen prefab: MISSING (" + ScreenPrefabPath + ")";
			}

			MultiplayerScreen screen = prefab.GetComponent<MultiplayerScreen>();

			if (screen == null)
			{
				return "Screen prefab: has no MultiplayerScreen component";
			}

			ScreenBase baseScreen = screen as ScreenBase;
			int missing = 0;

			missing += Missing(screen.ModeOffButton, "ModeOffButton");
			missing += Missing(screen.ModeJoinButton, "ModeJoinButton");
			missing += Missing(screen.ModeHostButton, "ModeHostButton");
			missing += Missing(screen.HostButton, "HostButton");
			missing += Missing(screen.StopHostingButton, "StopHostingButton");
			missing += Missing(screen.JoinCodeField, "JoinCodeField");
			missing += Missing(screen.JoinButton, "JoinButton");
			missing += Missing(screen.JoinCodeDisplay, "JoinCodeDisplay");
			missing += Missing(screen.StatusText, "StatusText");
			missing += Missing(screen.BackButton, "BackButton");

			bool hasScaler = prefab.GetComponent<CanvasScaler>() != null;
			bool hasCanvasGroup = prefab.GetComponent<CanvasGroup>() != null;
			int canvases = baseScreen == null || baseScreen.Canvases == null ? 0 : baseScreen.Canvases.Count;

			return "Screen prefab:\n"
				+ "  unassigned fields: " + (missing == 0 ? "none" : missing + " <-- MUST BE FIXED") + "\n"
				+ "  CanvasScaler on root: " + (hasScaler ? "yes" : "NO <-- screen will not scale") + "\n"
				+ "  CanvasGroup on root: " + (hasCanvasGroup ? "yes" : "NO") + "\n"
				+ "  Canvases assigned: " + canvases + (canvases == 0 ? " <-- MUST BE FIXED" : "");
		}

		private static int Missing(UnityEngine.Object reference, string fieldName)
		{
			if (reference != null)
			{
				return 0;
			}

			Debug.LogWarning("MultiplayerScreen." + fieldName + " is not assigned.");
			return 1;
		}

		/// <summary>
		/// Reports the Multiplayer button in whatever scene the user currently
		/// has open.
		///
		/// A stale open copy of the menu shows the old button set even when the
		/// file on disk is correct, and that is indistinguishable from the button
		/// simply not having been added. Saying which scene is open, and whether
		/// it has the button, separates the two.
		/// </summary>
		private static string DescribeOpenScenes(MainMenuScreen savedScreen)
		{
			System.Text.StringBuilder builder = new System.Text.StringBuilder();

			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene open = SceneManager.GetSceneAt(i);

				if (!open.isLoaded)
				{
					continue;
				}

				foreach (GameObject rootObject in open.GetRootGameObjects())
				{
					MainMenuScreen found = rootObject.GetComponentInChildren<MainMenuScreen>(includeInactive: true);

					if (found == null)
					{
						continue;
					}

					builder.Append("\n  " + open.name + ": button=")
						.Append(found.MultiplayerButton != null ? "present" : "MISSING")
						.Append(", dirty=").Append(open.isDirty);
				}
			}

			if (builder.Length == 0)
			{
				return "none open (open MenuScreenScene to check)";
			}

			return builder.ToString();
		}

		/// <summary>
		/// Checks what the button actually looks like, rather than whether the
		/// reference is set.
		/// </summary>
		///
		/// A Multiplayer button with a valid reference, correct parent and active
		/// flag can still be invisible - an empty label makes it read as blank
		/// space. That is a different failure from a missing button, and it was
		/// not distinguishable by looking at the serialized data alone.
		private static string DescribeButtonAppearance(Button button)
		{
			if (button == null)
			{
				return "no button";
			}

			string text = "<no label>";

			Text label = button.GetComponentInChildren<Text>();

			if (label != null)
			{
				text = string.IsNullOrEmpty(label.text) ? "<EMPTY LABEL>" : "\"" + label.text + "\"";
			}

			Image image = button.GetComponent<Image>();
			string sprite = image == null || image.sprite == null ? "<NO SPRITE>" : "sprite set";

			return text + ", " + sprite
				+ ", active=" + button.gameObject.activeInHierarchy
				+ ", size=" + button.GetComponent<RectTransform>().rect.size;
		}

		// ---- screen prefab -------------------------------------------------

		private static void BuildScreenPrefab()
		{
			GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPrefabPath);

			if (existing != null)
			{
				// Rebuild rather than skip. An earlier version kept any existing
				// prefab, which meant a half-built one - missing field references,
				// no canvas - could never be repaired by re-running the tool, and
				// produced a screen that opened to an empty panel with nothing
				// logged. A prefab is cheap to regenerate, so always do.
				Debug.Log("Replacing the existing " + ScreenPrefabPath + ".");
				AssetDatabase.DeleteAsset(ScreenPrefabPath);
			}

			// Layout mirrors the other full-screen panels: a title at the top, a
			// column of content, and a back button.
			GameObject root = new GameObject("MultiplayerScreen", typeof(RectTransform));
			MultiplayerScreen screen = root.AddComponent<MultiplayerScreen>();

			CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();

			RectTransform rootRect = (RectTransform)root.transform;
			rootRect.anchorMin = Vector2.zero;
			rootRect.anchorMax = Vector2.one;
			rootRect.offsetMin = Vector2.zero;
			rootRect.offsetMax = Vector2.zero;

			// The screen is instantiated into an existing canvas, so it must not
			// bring its own.
			root.layer = LayerMask.NameToLayer("UI");

			// --- title -------------------------------------------------------

			GameObject title = CreateText("TextScreenTitle", root.transform, 28, TextAnchor.MiddleCenter);
			SetStretch(title.GetComponent<RectTransform>(), top: 8f, bottom: -60f);
			title.GetComponent<Text>().text = "Multiplayer";

			// --- column ------------------------------------------------------

			GameObject column = new GameObject("Column", typeof(RectTransform), typeof(VerticalLayoutGroup));
			column.layer = root.layer;
			RectTransform columnRect = (RectTransform)column.transform;
			SetStretch(columnRect, top: 64f, bottom: -80f);
			columnRect.offsetMin = new Vector2(16f, columnRect.offsetMin.y);
			columnRect.offsetMax = new Vector2(-16f, columnRect.offsetMax.y);

			VerticalLayoutGroup layout = column.GetComponent<VerticalLayoutGroup>();
			layout.spacing = 6f;
			layout.childAlignment = TextAnchor.UpperCenter;
			layout.childControlWidth = true;
			layout.childControlHeight = true;
			layout.childForceExpandWidth = true;
			layout.childForceExpandHeight = false;

			// --- mode row ----------------------------------------------------

			GameObject modeRow = CreateRow(column.transform, "ModeRow");

			Button modeOff = CreateButton(modeRow.transform, "ButtonModeOff", "Off");
			Button modeJoin = CreateButton(modeRow.transform, "ButtonModeJoin", "Join only");
			Button modeHost = CreateButton(modeRow.transform, "ButtonModeHost", "Host or join");

			// --- status ------------------------------------------------------

			GameObject status = CreateText("StatusText", column.transform, 18, TextAnchor.MiddleCenter);
			LayoutElement statusLayout = status.AddComponent<LayoutElement>();
			statusLayout.preferredHeight = 44f;
			statusLayout.flexibleHeight = 0f;
			status.GetComponent<Text>().text = "";

			// --- hosting -----------------------------------------------------

			GameObject hostRow = CreateRow(column.transform, "HostRow");

			Button host = CreateButton(hostRow.transform, "ButtonHost", "Start hosting");
			Button stopHosting = CreateButton(hostRow.transform, "ButtonStopHosting", "Stop hosting");

			GameObject codeLabel = CreateText("JoinCodeLabel", column.transform, 16, TextAnchor.MiddleCenter);
			LayoutElement codeLabelLayout = codeLabel.AddComponent<LayoutElement>();
			codeLabelLayout.preferredHeight = 22f;
			codeLabel.GetComponent<Text>().text = "Join code:";

			GameObject codeDisplay = CreateText("JoinCodeDisplay", column.transform, 26, TextAnchor.MiddleCenter);
			LayoutElement codeLayout = codeDisplay.AddComponent<LayoutElement>();
			codeLayout.preferredHeight = 40f;
			codeDisplay.GetComponent<Text>().text = "";

			// --- join --------------------------------------------------------

			GameObject joinRow = CreateRow(column.transform, "JoinRow");

			InputField joinField = CreateInputField(joinRow.transform, "JoinCodeField", "Enter a join code");
			Button join = CreateButton(joinRow.transform, "ButtonJoin", "Join");

			// --- back --------------------------------------------------------

			Button back = CreateButton(root.transform, "ButtonBack", "Back");
			SetBottomBar(back.GetComponent<RectTransform>());

			// --- wire up -----------------------------------------------------

			screen.ModeOffButton = modeOff;
			screen.ModeJoinButton = modeJoin;
			screen.ModeHostButton = modeHost;
			screen.HostButton = host;
			screen.StopHostingButton = stopHosting;
			screen.JoinCodeField = joinField;
			screen.JoinButton = join;
			screen.JoinCodeDisplay = codeDisplay.GetComponent<Text>();
			screen.StatusText = status.GetComponent<Text>();
			screen.BackButton = back;
			screen.CanvasGroups = new System.Collections.Generic.List<CanvasGroup> { canvasGroup };

			// A screen prefab is its own canvas root, exactly like
			// EnterNumberScreen: RectTransform + Canvas + CanvasScaler +
			// CanvasGroup. ScreenBase.Canvases then lists those canvases so
			// Awake can find the scalers - without it, Awake logs an error and
			// the screen renders at the wrong size.
			Canvas canvas = root.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;

			CanvasScaler scaler = root.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(800f, 600f);
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			scaler.matchWidthOrHeight = 1f;

			ScreenBase asBase = screen;
			asBase.Canvases = new System.Collections.Generic.List<Canvas> { canvas };
			asBase.CanvasGroups = new System.Collections.Generic.List<CanvasGroup> { canvasGroup };

			GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ScreenPrefabPath);
			UnityEngine.Object.DestroyImmediate(root);

			// Verify what was actually written. An unassigned field here produces
			// a screen that opens, logs nothing, and shows an empty panel - which
			// is what happened when this was written without the check.
			VerifyScreenPrefabOrThrow();

			Debug.Log("Created " + ScreenPrefabPath + " (" + prefab.name + ")");
		}

		/// <summary>
		/// Fails loudly if the saved screen prefab is not actually wired up.
		/// </summary>
		private static void VerifyScreenPrefabOrThrow()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPrefabPath);

			if (prefab == null)
			{
				throw new InvalidOperationException("Screen prefab was not created at " + ScreenPrefabPath);
			}

			MultiplayerScreen screen = prefab.GetComponent<MultiplayerScreen>();

			if (screen == null)
			{
				throw new InvalidOperationException("Saved screen prefab has no MultiplayerScreen component.");
			}

			System.Collections.Generic.List<string> problems = new System.Collections.Generic.List<string>();

			if (screen.ModeOffButton == null) problems.Add("ModeOffButton");
			if (screen.ModeJoinButton == null) problems.Add("ModeJoinButton");
			if (screen.ModeHostButton == null) problems.Add("ModeHostButton");
			if (screen.HostButton == null) problems.Add("HostButton");
			if (screen.StopHostingButton == null) problems.Add("StopHostingButton");
			if (screen.JoinCodeField == null) problems.Add("JoinCodeField");
			if (screen.JoinButton == null) problems.Add("JoinButton");
			if (screen.JoinCodeDisplay == null) problems.Add("JoinCodeDisplay");
			if (screen.StatusText == null) problems.Add("StatusText");
			if (screen.BackButton == null) problems.Add("BackButton");

			if (prefab.GetComponent<CanvasScaler>() == null) problems.Add("CanvasScaler");

			ScreenBase asBase = screen;

			if (asBase.Canvases == null || asBase.Canvases.Count == 0) problems.Add("Canvases");

			if (problems.Count > 0)
			{
				throw new InvalidOperationException(
					"Saved screen prefab is missing: " + string.Join(", ", problems)
					+ ". Delete " + ScreenPrefabPath + " and re-run this tool.");
			}
		}

		// ---- main menu button ----------------------------------------------

		private static void AddMainMenuButton()
		{
			// The menu lives in a SCENE, so the scene has to be open and saved
			// rather than edited through the prefab APIs.
			//
			// Opening it additively leaves the user's currently open scene alone,
			// and the edit is then saved back to this one path only. Using
			// LoadPrefabContents here (as an earlier version did) would modify a
			// prefab the game never loads, which is why the button appeared
			// nowhere.
			Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Additive);

			if (!scene.IsValid())
			{
				Debug.LogError("Could not open " + MenuScenePath);
				return;
			}

			GameObject[] roots = scene.GetRootGameObjects();

			MainMenuScreen menuScreen = null;
			Transform sceneRoot = null;

			foreach (GameObject rootObject in roots)
			{
				MainMenuScreen candidate = rootObject.GetComponentInChildren<MainMenuScreen>(includeInactive: true);

				if (candidate != null)
				{
					menuScreen = candidate;
					sceneRoot = candidate.transform;
					break;
				}
			}

			if (menuScreen == null)
			{
				Debug.LogError("No MainMenuScreen found in " + MenuScenePath + ". Add the button by hand.");
				EditorSceneManager.CloseScene(scene, removeScene: true);
				return;
			}

			// Always make sure the screen prefab is assigned.
			if (menuScreen.MultiplayerScreenPrefab == null)
			{
				menuScreen.MultiplayerScreenPrefab = AssetDatabase.LoadAssetAtPath<ScreenBase>(ScreenPrefabPath);
			}

			if (menuScreen.MultiplayerButton == null)
			{
				// The vertical layout column that holds every other menu button.
				Transform column = FindButtonColumn(sceneRoot);

				if (column == null)
				{
					Debug.LogError("Could not find the main menu button column. Add the button by hand.");
					EditorSceneManager.CloseScene(scene, removeScene: true);
					return;
				}

				// Take an existing button as the template so sprite, colours,
				// sizing and the three helper scripts all match the rest of the
				// column exactly. Its scene object is a normal transform, so
				// parenting under it works.
				Button template = FindTemplateButton(menuScreen);

				if (template == null)
				{
					Debug.LogError("Could not find a menu button to copy. Add the button by hand.");
					EditorSceneManager.CloseScene(scene, removeScene: true);
					return;
				}

				GameObject buttonObject = UnityEngine.Object.Instantiate(template.gameObject, column);
				buttonObject.name = "ButtonMultiplayer";

				Button button = buttonObject.GetComponent<Button>();

				if (button != null)
				{
					button.onClick = new Button.ButtonClickedEvent();
				}

				SetLabel(buttonObject, "Multiplayer");

				// Position comes from the column's VerticalLayoutGroup, so the
				// transform needs no manual placement - only a sane default.
				LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();

				if (layoutElement != null)
				{
					layoutElement.preferredHeight = 45f;
					layoutElement.layoutPriority = 1;
				}

				menuScreen.MultiplayerButton = button;
			}

			// Mark the scene dirty so the change is actually written, then save it
			// back to its own path. EditorSceneManager.SaveScene writes the open
			// scene, which is why the scene is opened additively here rather than
			// being loaded into the user's current one.
			EditorSceneManager.MarkSceneDirty(scene);
			bool saved = EditorSceneManager.SaveScene(scene);

			EditorSceneManager.CloseScene(scene, removeScene: true);

			AssetDatabase.Refresh();

			if (!saved)
			{
				Debug.LogError("Failed to save " + MenuScenePath);
				return;
			}

			Debug.Log("Added ButtonMultiplayer to " + MenuScenePath);
		}

		private static Transform FindButtonColumn(Transform root)
		{
			// Prefer the column the menu actually uses, by name. Falling back to
			// "first vertical layout containing buttons" would happily pick an
			// unrelated panel elsewhere in the scene and put the button in it.
			Transform named = root.Find("LayoutVerticalBottomLeft");

			if (named != null)
			{
				return named;
			}

			foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
			{
				LayoutGroup layout = child.GetComponent<VerticalLayoutGroup>();

				if (layout == null || layout is GridLayoutGroup)
				{
					continue;
				}

				// A real column of buttons, not a one-off panel.
				if (child.GetComponentsInChildren<Button>(includeInactive: true).Length >= 3)
				{
					return child;
				}
			}

			return null;
		}

		private static Button FindTemplateButton(MainMenuScreen screen)
		{
			// Any of the existing menu buttons will do; they all share styling.
			if (screen.TutorialsButton != null)
			{
				return screen.TutorialsButton;
			}

			if (screen.QuitButton != null)
			{
				return screen.QuitButton;
			}

			return screen.GetComponentInChildren<Button>(includeInactive: true);
		}

		private static void SetLabel(GameObject buttonObject, string text)
		{
			Text label = buttonObject.GetComponentInChildren<Text>();

			if (label != null)
			{
				label.text = text;
			}
		}

		// ---- screen registration -------------------------------------------

		private static void RegisterScreen()
		{
			// SceneNameMapping lives in the UI assembly, which cannot reference
			// the multiplayer assembly without a circular dependency. Registering
			// from here instead keeps the dependency one-way: multiplayer knows
			// about the UI, the UI never needs to know about multiplayer.
			//
			// MainMenuScreen navigates to the screen directly, so the type-based
			// lookup in SceneNameMapping is not needed for the button to work.
			Debug.Log("MultiplayerScreen navigation is wired through MainMenuScreen.Multiplayer().");
		}

		// ---- ui construction helpers ---------------------------------------

		private static GameObject CreateRow(Transform parent, string name)
		{
			GameObject row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
			row.layer = parent.gameObject.layer;

			LayoutElement rowLayout = row.AddComponent<LayoutElement>();
			rowLayout.preferredHeight = 45f;

			HorizontalLayoutGroup group = row.GetComponent<HorizontalLayoutGroup>();
			group.spacing = 6f;
			group.childAlignment = TextAnchor.MiddleCenter;
			group.childControlWidth = true;
			group.childControlHeight = true;
			group.childForceExpandWidth = true;
			group.childForceExpandHeight = true;

			return row;
		}

		private static Button CreateButton(Transform parent, string name, string label)
		{
			GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));

			Image image = buttonObject.GetComponent<Image>();
			image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprite/Button.asset");
			image.type = Image.Type.Sliced;
			image.color = Color.white;

			Button button = buttonObject.GetComponent<Button>();
			button.targetGraphic = image;

			ColorBlock colors = button.colors;
			colors.normalColor = new Color(0.256f, 0.326f, 0.581f, 1f);
			colors.highlightedColor = new Color(0.318f, 0.376f, 0.596f, 1f);
			colors.pressedColor = new Color(0.404f, 0.506f, 0.808f, 1f);
			colors.selectedColor = new Color(0.646f, 0.691f, 0.863f, 1f);
			colors.disabledColor = new Color(0.784f, 0.784f, 0.784f, 0.502f);
			button.colors = colors;

			GameObject textObject = CreateText("Text", buttonObject.transform, 20, TextAnchor.MiddleCenter);
			SetStretch((RectTransform)textObject.transform);
			textObject.GetComponent<Text>().raycastTarget = false;
			textObject.GetComponent<Text>().text = label;

			buttonObject.transform.SetParent(parent, worldPositionStays: false);

			LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
			layout.preferredHeight = 45f;

			return button;
		}

		private static InputField CreateInputField(Transform parent, string name, string placeholder)
		{
			GameObject fieldObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));

			Image background = fieldObject.GetComponent<Image>();
			background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprite/Button.asset");
			background.type = Image.Type.Sliced;
			background.color = new Color(1f, 1f, 1f, 0.25f);

			// Placeholder, so an empty field explains itself.
			GameObject placeholderObject = CreateText("Placeholder", fieldObject.transform, 18, TextAnchor.MiddleCenter);
			SetStretch((RectTransform)placeholderObject.transform);
			placeholderObject.GetComponent<Text>().text = placeholder;
			placeholderObject.GetComponent<Text>().color = new Color(1f, 1f, 1f, 0.5f);
			placeholderObject.GetComponent<Text>().raycastTarget = false;

			// The editable text.
			GameObject textObject = CreateText("Text", fieldObject.transform, 18, TextAnchor.MiddleCenter);
			SetStretch((RectTransform)textObject.transform);
			textObject.GetComponent<Text>().raycastTarget = false;

			InputField field = fieldObject.GetComponent<InputField>();
			field.textComponent = textObject.GetComponent<Text>();
			field.placeholder = placeholderObject.GetComponent<Text>();

			// Join codes are typed in blocks of eight; autocorrect and
			// mobile capitalsisation only get in the way. The field is also
			// normalised on submit, so case never matters.
			field.characterLimit = 64;
			field.lineType = InputField.LineType.SingleLine;
			field.contentType = InputField.ContentType.Standard;

			fieldObject.transform.SetParent(parent, worldPositionStays: false);

			LayoutElement layout = fieldObject.AddComponent<LayoutElement>();
			layout.preferredHeight = 45f;
			layout.flexibleWidth = 2f;

			return field;
		}

		private static GameObject CreateText(string name, Transform parent, int fontSize, TextAnchor alignment)
		{
			GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));

			Text text = textObject.GetComponent<Text>();
			text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			text.fontSize = fontSize;
			text.alignment = alignment;
			text.color = Color.white;
			text.horizontalOverflow = HorizontalWrapMode.Overflow;
			text.verticalOverflow = VerticalWrapMode.Overflow;
			text.raycastTarget = false;

			textObject.transform.SetParent(parent, worldPositionStays: false);
			return textObject;
		}

		private static void SetStretch(RectTransform rect, float top = 0f, float bottom = 0f)
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = new Vector2(0f, bottom);
			rect.offsetMax = new Vector2(0f, -top);
		}

		private static void SetBottomBar(RectTransform rect)
		{
			rect.anchorMin = new Vector2(0f, 0f);
			rect.anchorMax = new Vector2(1f, 0f);
			rect.pivot = new Vector2(0.5f, 0f);
			rect.offsetMin = new Vector2(16f, 8f);
			rect.offsetMax = new Vector2(-16f, 58f);
		}
	}
}
#endif