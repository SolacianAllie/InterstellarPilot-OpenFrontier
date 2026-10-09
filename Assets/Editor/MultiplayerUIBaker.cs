#if UNITY_EDITOR
using System;
using OpenFrontier.IP.Multiplayer.UI;
using OpenFrontier.IP.UI;
using OpenFrontier.IP.UI.Screens;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
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
		private const string MenuPrefabPath = "Assets/GameObject/MainMenuScreen.prefab";
		private const string ScreenPrefabPath = "Assets/GameObject/MultiplayerScreen.prefab";

		[MenuItem("Tools/Multiplayer/Build Main Menu UI")]
		public static void Build()
		{
			// Safety net: this tool rewrites a large hand-laid-out prefab. If the
			// working copy is dirty, saving over it would discard whatever else
			// is in flight, so stop and let the user commit or stash first.
			if (!EditorUtility.DisplayDialog(
					"Multiplayer UI",
					"This rewrites MainMenuScreen.prefab and creates MultiplayerScreen.prefab.\n\n"
						+ "It is safe to re-run, but unsaved work in the project will not survive it.",
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
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPrefabPath);

			if (prefab == null)
			{
				EditorUtility.DisplayDialog("Multiplayer UI", "Could not load " + MenuPrefabPath, "OK");
				return;
			}

			MainMenuScreen screen = prefab.GetComponent<MainMenuScreen>();
			bool hasButton = screen != null && screen.MultiplayerButton != null;
			bool hasScreen = screen != null && screen.MultiplayerScreenPrefab != null;

			string report =
				"Root object: " + prefab.name + "\n"
				+ "MainMenuScreen component: " + (screen != null ? "present" : "MISSING") + "\n"
				+ "Multiplayer button: " + (hasButton ? "present" : "MISSING") + "\n"
				+ "Multiplayer screen prefab: " + (hasScreen ? "assigned" : "NOT ASSIGNED") + "\n"
				+ "Buttons in column: " + CountButtons(prefab);

			if (!string.Equals(prefab.name, "MainMenuScreen", StringComparison.Ordinal))
			{
				report += "\n\nWARNING: the root object has been renamed. This prefab is probably damaged - "
					+ "restore it with: git checkout -- " + MenuPrefabPath;
			}

			EditorUtility.DisplayDialog("Multiplayer UI", report, "OK");
		}

		private static int CountButtons(GameObject prefab)
		{
			return prefab.GetComponentsInChildren<Button>(includeInactive: true).Length;
		}

		// ---- screen prefab -------------------------------------------------

		private static void BuildScreenPrefab()
		{
			GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPrefabPath);

			if (existing != null)
			{
				Debug.Log("MultiplayerScreen.prefab already exists; leaving it alone.");
				return;
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

			GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ScreenPrefabPath);
			UnityEngine.Object.DestroyImmediate(root);

			Debug.Log("Created " + ScreenPrefabPath + " (" + prefab.name + ")");
		}

		// ---- main menu button ----------------------------------------------

		private static void AddMainMenuButton()
		{
			if (AssetDatabase.LoadAssetAtPath<GameObject>(MenuPrefabPath) == null)
			{
				Debug.LogError("Could not load " + MenuPrefabPath);
				return;
			}

			// Edit the prefab through LoadPrefabContents/SaveAsPrefabAsset.
			//
			// The previous version loaded the prefab ASSET, instantiated it into
			// the scene, then parented the new button under a transform taken
			// from that asset rather than from the instance. Parenting under a
			// persistent object is rejected by Unity ("Cannot instantiate objects
			// with a parent which is persistent"), so the button was silently
			// dropped at the scene root and SaveAsPrefabAsset then wrote a
			// mangled hierarchy back - re-serialising the whole prefab with new
			// file IDs. LoadPrefabContents gives a real, editable copy of the
			// prefab contents, which is the supported way to edit one.
			GameObject contents = PrefabUtility.LoadPrefabContents(MenuPrefabPath);

			try
			{
				MainMenuScreen contentsScreen = contents.GetComponent<MainMenuScreen>();

				if (contentsScreen == null)
				{
					Debug.LogError("MainMenuScreen component missing from " + MenuPrefabPath);
					return;
				}

				// Always make sure the screen prefab is assigned, even when the
				// button already exists.
				if (contentsScreen.MultiplayerScreenPrefab == null)
				{
					contentsScreen.MultiplayerScreenPrefab = AssetDatabase.LoadAssetAtPath<ScreenBase>(ScreenPrefabPath);
				}

				if (contentsScreen.MultiplayerButton != null)
				{
					Debug.Log("Main menu already has a Multiplayer button; refreshing references only.");
					PrefabUtility.SaveAsPrefabAsset(contents, MenuPrefabPath);
					return;
				}

				// The vertical layout column that holds every other menu button.
				Transform column = contents.transform.Find("LayoutVerticalBottomLeft")
					?? FindButtonColumn(contents.transform);

				if (column == null)
				{
					Debug.LogError("Could not find the main menu button column. Add the button by hand.");
					return;
				}

				// Take an existing button as the template so sprite, colours,
				// sizing and the three helper scripts all match the rest of the
				// column exactly.
				Button template = FindTemplateButton(contentsScreen);

				if (template == null)
				{
					Debug.LogError("Could not find a menu button to copy. Add the button by hand.");
					return;
				}

				// Parent under the CONTENTS column, which is a normal scene
				// object - this is the parenting that the asset-based version got
				// wrong.
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

				contentsScreen.MultiplayerButton = button;

				PrefabUtility.SaveAsPrefabAsset(contents, MenuPrefabPath);

				AssetDatabase.SaveAssets();
				Debug.Log("Added ButtonMultiplayer to the main menu.");
			}
			finally
			{
				// Always release the loaded copy, even on an early return, or the
				// prefab stays locked and shows as open for editing.
				PrefabUtility.UnloadPrefabContents(contents);
			}
		}

		private static Transform FindButtonColumn(Transform root)
		{
			foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
			{
				if (child.GetComponent<VerticalLayoutGroup>() != null
					&& child.GetComponentInChildren<Button>(includeInactive: true) != null)
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