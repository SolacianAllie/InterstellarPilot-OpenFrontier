#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.UI;
using OpenFrontier.IP.UI.Screens.LoadGame;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.Multiplayer.EditorTools
{
	/// <summary>
	/// Transfers the wired references from the LoadGameScreen component on
	/// MultiplayerScreen.prefab across to the MultiplayerScreen component, then
	/// removes the save-specific bits.
	///
	/// The prefab is a clone of LoadGameScreen, so its references are all correct
	/// but under the wrong field names. Doing this by hand in the inspector means
	/// moving ~12 references and remembering which to repurpose; doing it here
	/// means the mapping is written down once, checked on completion, and
	/// repeatable.
	///
	/// Safe to re-run: every assignment is idempotent.
	///
	/// Run from the menu: Tools > Multiplayer > Transfer Screen References
	/// </summary>
	public static class MultiplayerScreenReferenceTransfer
	{
		private const string PrefabPath = "Assets/GameObject/MultiplayerScreen.prefab";

		/// <summary>
		/// How long the whole operation is allowed to take.
		/// </summary>
		private const string SummaryHeading = "Transferring screen references";

		[MenuItem("Tools/Multiplayer/Transfer Screen References")]
		public static void Run()
		{
			if (!EditorUtility.DisplayDialog(
					"Multiplayer screen",
					"Finishes wiring MultiplayerScreen.prefab: assigns the host list, "
						+ "puts SavedHostItemUI on the row prefab and points the list at it. "
						+ "Safe to re-run.",
					"Continue",
					"Cancel"))
			{
				return;
			}

			GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);

			if (contents == null)
			{
				EditorUtility.DisplayDialog("Multiplayer screen", "Could not open " + PrefabPath, "OK");
				return;
			}

			try
			{
				Report report = Transfer(contents);

				if (report.Success)
				{
					PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
					AssetDatabase.Refresh();
				}

				EditorUtility.DisplayDialog(SummaryHeading, report.Describe(), "OK");
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(contents);
			}
		}

		private sealed class Report
		{
			public readonly List<string> Done = new List<string>();
			public readonly List<string> Problems = new List<string>();
			public readonly List<string> Manual = new List<string>();

			public bool Success => Problems.Count == 0;

			public string Describe()
			{
				System.Text.StringBuilder builder = new System.Text.StringBuilder();

				builder.Append(Success
					? "Done. Transferred:\n"
					: "INCOMPLETE - nothing was saved.\n");

				builder.Append("  " + (Done.Count == 0 ? "nothing" : string.Join("\n  ", Done)) + "\n");

				if (Manual.Count > 0)
				{
					builder.Append("\nStill to do by hand:\n  " + string.Join("\n  ", Manual));
				}

				if (Problems.Count > 0)
				{
					builder.Append("\nProblems:\n  " + string.Join("\n  ", Problems));
				}

				return builder.ToString();
			}

			public void Note(string what)
			{
				Done.Add(what);
			}

			/// <summary>
			/// A step this tool cannot perform. Not a fault, so it does not stop
			/// the save - but it is always reported, because a silent skip is how
			/// a screen ends up half-wired with nothing in the log.
			/// </summary>
			public void Manual_(string what)
			{
				Manual.Add(what);
			}

			public void Fail(string what)
			{
				Problems.Add(what);
			}
		}

		private static Report Transfer(GameObject contents)
		{
			Report report = new Report();

			OpenFrontier.IP.Multiplayer.UI.MultiplayerScreen target =
				contents.GetComponent<OpenFrontier.IP.Multiplayer.UI.MultiplayerScreen>();

			if (target == null)
			{
				report.Fail("no MultiplayerScreen component on the prefab - add it before running this");
				return report;
			}

			// The source is the LoadGameScreen component still sitting on the
			// prefab, holding every reference the clone inherited.
			//
			// It only ever existed to carry those references across, so its
			// absence is the finished state rather than a fault: everything it
			// provided is already transferred, and the work that cannot be
			// inherited - the list component and the row prefab - still has to
			// run. Treating a missing source as fatal would leave the tool
			// permanently unusable exactly once it was no longer needed.
			LoadGameScreen source = contents.GetComponent<LoadGameScreen>();

			if (source == null)
			{
				report.Note("LoadGameScreen already removed - nothing left to inherit");
			}
			else
			{
				TransferReferences(contents, source, target, report);
			}

			AssignItemsList(contents, target, report);

			Report itemReport = TransferItemPrefab();

			foreach (string note in itemReport.Done)
			{
				report.Note(note);
			}

			foreach (string problem in itemReport.Problems)
			{
				report.Fail(problem);
			}

			foreach (string note in itemReport.Manual)
			{
				report.Manual_(note);
			}

			// Set here rather than inside TransferItemPrefab: this component is
			// part of the open prefab contents, which the caller saves.
			AssignItemPrefab(target, report);

			Report joinCodeReport = TransferJoinCodeScreen();

			foreach (string note in joinCodeReport.Done)
			{
				report.Note(note);
			}

			foreach (string problem in joinCodeReport.Problems)
			{
				report.Fail(problem);
			}

			foreach (string note in joinCodeReport.Manual)
			{
				report.Manual_(note);
			}

			AssignJoinCodeScreenPrefab(contents, target, report);
			AssignJoinCodeButton(contents, target, report);

			return report;
		}

		/// <summary>
		/// Points the screen at the join code dialog.
		/// </summary>
		/// <remarks>
		/// The dialog lives in the multiplayer assembly, which the navigator
		/// cannot look up by type - it keys its screen table on types from the
		/// UI assembly, and naming the multiplayer type there would make the two
		/// assemblies reference each other. So the prefab is referenced
		/// directly and instantiated on demand.
		/// </remarks>
		/// <summary>
		/// Points the screen at the Join Code button.
		/// </summary>
		/// <remarks>
		/// Found by object name because it is hand-added, not inherited: the
		/// clone was made before the button existed, so there is no source
		/// component to read a reference from. Left unassigned it is simply
		/// inert - the screen's own wiring skips a null button without
		/// complaint, so the button looks present and does nothing.
		/// </remarks>
		private static void AssignJoinCodeButton(
			GameObject contents,
			OpenFrontier.IP.Multiplayer.UI.MultiplayerScreen target,
			Report report)
		{
			Transform found = contents.transform.FindDeep("ButtonIconJoinCode");

			if (found == null)
			{
				report.Manual_("no ButtonIconJoinCode in the prefab - add it beside Add Host, "
					+ "then re-run");
				return;
			}

			Button button = found.GetComponent<Button>();

			if (button == null)
			{
				report.Manual_("ButtonIconJoinCode has no Button component");
				return;
			}

			if (target.JoinCodeButton != button)
			{
				target.JoinCodeButton = button;
				report.Note("JoinCodeButton assigned");
			}
			else
			{
				report.Note("JoinCodeButton already assigned");
			}
		}

		private static void AssignJoinCodeScreenPrefab(
			GameObject contents,
			OpenFrontier.IP.Multiplayer.UI.MultiplayerScreen target,
			Report report)
		{
			const string screenPath = "Assets/GameObject/JoinCodeScreen.prefab";
			GameObject dialog = AssetDatabase.LoadAssetAtPath<GameObject>(screenPath);

			if (dialog == null)
			{
				report.Fail("could not load " + screenPath);
				return;
			}

			OpenFrontier.IP.Multiplayer.UI.JoinCodeScreen component =
				dialog.GetComponent<OpenFrontier.IP.Multiplayer.UI.JoinCodeScreen>();

			if (component == null)
			{
				report.Fail(screenPath + " has no JoinCodeScreen component");
				return;
			}

			if (target.JoinCodeScreenPrefab != component)
			{
				target.JoinCodeScreenPrefab = component;
				report.Note("JoinCodeScreenPrefab assigned");
			}
			else
			{
				report.Note("JoinCodeScreenPrefab already assigned");
			}
		}

		/// <summary>
		/// Points the list at the host row prefab, once there is a list to point.
		/// </summary>
		private static void AssignItemPrefab(
			OpenFrontier.IP.Multiplayer.UI.MultiplayerScreen target,
			Report report)
		{
			const string itemPath = "Assets/GameObject/MultiplayerGameItem.prefab";
			GameObject rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(itemPath);

			if (rowPrefab == null)
			{
				report.Fail("could not load " + itemPath);
				return;
			}

			if (target.ItemsList == null)
			{
				report.Manual_("ItemsList is still unassigned, so ItemPrefab cannot be set");
				return;
			}

			if (target.ItemsList.ItemPrefab != rowPrefab)
			{
				target.ItemsList.ItemPrefab = rowPrefab;
				report.Note("list ItemPrefab set to MultiplayerGameItem");
			}
			else
			{
				report.Note("list ItemPrefab already set");
			}
		}

		/// <summary>
		/// Points the screen at its host list, and gives the list the empty-state
		/// label the save-game list had.
		/// </summary>
		/// <remarks>
		/// ItemsList cannot be inherited from LoadGameScreen: the field is typed
		/// SavedHostItemsUI and the object carried LoadGameItemsUI. Those are
		/// different classes - both derive from ScrollList, but of different item
		/// types - so the reference cannot cross and the component has to be
		/// swapped in the inspector. Once it has been, the component itself is
		/// found here rather than asked for again.
		/// </remarks>
		private static void AssignItemsList(
			GameObject contents,
			OpenFrontier.IP.Multiplayer.UI.MultiplayerScreen target,
			Report report)
		{
			OpenFrontier.IP.Multiplayer.UI.SavedHostItemsUI[] lists =
				contents.GetComponentsInChildren<OpenFrontier.IP.Multiplayer.UI.SavedHostItemsUI>(true);

			if (lists.Length == 0)
			{
				report.Manual_("no SavedHostItemsUI on the prefab - swap LoadGameItemsUI for "
					+ "SavedHostItemsUI on ScrollRectGameList, then re-run");
				return;
			}

			if (lists.Length > 1)
			{
				report.Fail("found " + lists.Length + " SavedHostItemsUI components; expected exactly one");
				return;
			}

			OpenFrontier.IP.Multiplayer.UI.SavedHostItemsUI list = lists[0];

			if (target.ItemsList != list)
			{
				target.ItemsList = list;
				report.Note("ItemsList assigned to " + list.gameObject.name);
			}
			else
			{
				report.Note("ItemsList already assigned");
			}

			// The empty-state label is dropped along with any other data when the
			// component type is swapped in the inspector, so it is re-found by
			// name rather than inherited.
			if (list.NoItemsLabel == null)
			{
				Transform found = contents.transform.FindDeep("TextNoItems");

				if (found != null)
				{
					Text label = found.GetComponent<Text>();

					if (label != null)
					{
						list.NoItemsLabel = label;
						report.Note("NoItemsLabel assigned");
					}
					else
					{
						report.Manual_("TextNoItems has no Text component - assign NoItemsLabel by hand");
					}
				}
				else
				{
					report.Manual_("could not find TextNoItems - assign NoItemsLabel by hand");
				}
			}
			else
			{
				report.Note("NoItemsLabel already assigned");
			}
		}

		private static void TransferReferences(
			GameObject contents,
			LoadGameScreen source,
			OpenFrontier.IP.Multiplayer.UI.MultiplayerScreen target,
			Report report)
		{
			// ---- controls: reuse the existing buttons -------------------
			// The clone's buttons become join / rename / delete, and a new
			// Add Host button is created beside them.

			Assign(target, nameof(target.JoinButton), source.LoadGameButton, report,
				"JoinButton <- LoadGameButton");

			Assign(target, nameof(target.RenameHostButton), source.RenameFileButton, report,
				"RenameHostButton <- RenameFileButton");

			Assign(target, nameof(target.DeleteHostButton), source.DeleteGameConfirmButton, report,
				"DeleteHostButton <- DeleteGameConfirmButton");

			// ---- panels --------------------------------------------------

			Assign(target, nameof(target.HostInfoRoot), source.GameInfoRoot, report,
				"HostInfoRoot <- GameInfoRoot");

			Assign(target, nameof(target.MultipleItemsSelectedRoot), source.MultipleItemsSelectedRoot, report,
				"MultipleItemsSelectedRoot <- MultiSelectRoot");

			// ItemsList is assigned by AssignItemsList, which finds the component
			// on the prefab: it cannot be inherited from here, because the field
			// is typed SavedHostItemsUI and this object carries LoadGameItemsUI.

			// ---- detail labels -------------------------------------------
			// The info panel's save-specific rows are hidden rather than
			// deleted, so this stays reversible. A few are repurposed for the
			// host fields, and those keep their ROW visible.
			//
			// SaveGameFilesize is repurposed as the host status, so its row
			// stays up - but the row's own "File size" caption ("TextFileSize")
			// has to be hidden, or the status is labelled "File size" and
			// reads as nonsense. TextFileSizeValue itself is the repurposed
			// object and must not be hidden.

			Assign(target, nameof(target.HostNameLabel), source.SaveGameNameLabel, report,
				"HostNameLabel <- SaveGameNameLabel");

			Assign(target, nameof(target.HostCodeLabel), source.SaveGameFilename, report,
				"HostCodeLabel <- SaveGameFilename");

			Assign(target, nameof(target.HostAddedLabel), source.SaveGameDateLabel, report,
				"HostAddedLabel <- SaveGameDate");

			// HostStatusLabel has no natural counterpart - the save panel has
			// no "is this reachable" row. Reuse a row that is being hidden
			// anyway rather than asking the user to add a new object.
			Assign(target, nameof(target.HostStatusLabel), source.SaveGameFilesize, report,
				"HostStatusLabel <- SaveGameFilesize (repurposed)");

			// ---- hide the save-only rows ---------------------------------

			// Deactivated rather than deleted: a save file's author, version,
			// credits, pilot and sector have no meaning for a host, but leaving
			// the objects in place keeps the mapping readable and reversible.
			string[] saveOnlyRows =
			{
				"TextCredits", "TextCreditsValue",
				"TextNetWorth", "TextNetWorthValue",
				"TextPilot", "TextPilotValue",
				"TextFactionName", "TextFactionNameValue",
				"TextSaveVersion", "TextSaveVersionValue",
				"TextSaveCreatedVersionValue",
				"TextPermadeath", "TextPermadeathValue",
				"TextScenarioAuthor", "TextScenarioAuthorValue",
				"TextScenarioAuthoringTool", "TextScenarioAuthoringToolValue",
				"TextGameDate", "TextGameDateValue",
				"TextIncompatibleVersion",

				// The caption for the row repurposed as HostStatusLabel. Hidden
				// so the status is not labelled "File size", while
				// TextFileSizeValue below stays visible.
				"TextFileSize",

				// Captions for the other repurposed rows, likewise hidden.
				"TextFileName",
				"TextDate"
			};

			int hidden = 0;

			// The objects that get repurposed must stay visible.
			HashSet<GameObject> repurposed = new HashSet<GameObject>();

			foreach (Text reused in new[]
				{
					source.SaveGameFilesize,
					source.SaveGameFilename,
					source.SaveGameDateLabel,
					source.SaveGameNameLabel
				})
			{
				if (reused != null)
				{
					repurposed.Add(reused.gameObject);
				}
			}

			foreach (string rowName in saveOnlyRows)
			{
				Transform rowTransform = contents.transform.FindDeep(rowName);

				if (rowTransform == null)
				{
					continue;
				}

				GameObject row = rowTransform.gameObject;

				if (repurposed.Contains(row))
				{
					continue;
				}

				if (row.activeSelf)
				{
					row.SetActive(false);
					hidden++;
				}
			}

			if (hidden > 0)
			{
				report.Note("hid " + hidden + " save-only detail rows");
			}
			else
			{
				report.Note("save-only rows were already hidden");
			}

			// ---- remove the import button -------------------------------

			// Import is meaningless for a host: there is no file to import, a
			// host is reached over the network.
			if (source.ImportFileButton != null)
			{
				GameObject importObject = source.ImportFileButton.gameObject;
				source.ImportFileButton = null;
				UnityEngine.Object.DestroyImmediate(importObject);
				report.Note("removed the import button");
			}

			// Export goes too, for the same reasons: a host is reached over the
			// network, so there is nothing to export, and its save-file icon
			// would be misleading on a button that now adds a host.
			if (source.ExportFileButton != null)
			{
				GameObject exportObject = source.ExportFileButton.gameObject;
				source.ExportFileButton = null;
				UnityEngine.Object.DestroyImmediate(exportObject);
				report.Note("removed the export button");
			}

			// ---- create the Add Host button ------------------------------
			// Beside the delete button, which is where the row ends up once
			// import and export are gone.
			if (target.DeleteHostButton == null)
			{
				report.Fail("DeleteHostButton is unassigned, so Add Host has nowhere to go");
			}
			else
			{
				GameObject addHost = CreateAddHostButton(target.DeleteHostButton.gameObject);
				Assign(target, nameof(target.AddHostButton), addHost.GetComponent<Button>(), report,
					"AddHostButton created beside Delete");
			}

			// ---- the row prefab ---------------------------------------
			// Handled by Transfer() rather than here: the row prefab has to be
			// built whether or not the source component is still around, because
			// it cannot be inherited from it either way.

			SetText(contents, "TextNoItems",
				"No hosts yet. Use Add Host to enter a join code.", report);

			// The screen heading. It lives in the TitleBar, not on the button, so
			// it is simply renamed - the button row is icon-only, like the rest.
			//
			// This was previously mistaken for the load button's caption and
			// looked for a second label reading "Load Game", which does not
			// exist: there is only this one, and the button has no text at all.
			SetText(contents, "TextScreenTitle", "Multiplayer", report);

			// Captions in the repurposed detail rows. The values are overwritten
			// at runtime; only the row headings need to stop saying what a save
			// file is.
			SetTextByValue(contents, "Filename:", "Code:", report);
			SetTextByValue(contents, "Date:", "Added:", report);
			SetTextByValue(contents, "File size:", "Status:", report);

			// The pilot row's caption is repurposed too - the host's status lives
			// in the file-size row, and the pilot row is hidden, so nothing to do
			// beyond the hides above.

			// ---- remove the source component -----------------------------

			// The clone carried LoadGameScreen purely so its references could be
			// read. Leaving it on is not harmless: ScreenBase.awake() calls a
			// single virtual awake(), and with two components deriving from
			// ScreenBase on one GameObject only one override runs. LoadGameScreen's
			// wins, and it dereferences the import button that was just deleted -
			// a NullReferenceException every time the screen opens, with the
			// multiplayer screen's own awake never running at all.
			//
			// Removed only once every reference has actually been transferred, so
			// a failed run leaves the source intact and re-runnable.
			if (report.Success && source != null)
			{
				UnityEngine.Object.DestroyImmediate(source);
				report.Note("removed the LoadGameScreen component");
			}
			else if (source != null)
			{
				report.Manual_("LoadGameScreen left on the prefab - remove it once the "
					+ "problems above are fixed, or its awake() will throw on every open");
			}
		}

		/// <summary>
		/// Sets a label's text by its object name.
		/// </summary>
		private static void SetText(GameObject root, string objectName, string text, Report report)
		{
			Text label = FindText(root, objectName);

			if (label == null)
			{
				report.Fail("could not find a label called " + objectName);
				return;
			}

			label.text = text;
			report.Note(objectName + " text set to \"" + text + "\"");
		}

		/// <summary>
		/// Sets a label's text by its CURRENT text, since several labels are
		/// named the same and only their content distinguishes them.
		/// </summary>
		private static void SetTextByValue(GameObject root, string currentText, string replacement, Report report)
		{
			Text match = null;

			foreach (Text candidate in root.GetComponentsInChildren<Text>(includeInactive: true))
			{
				if (candidate.text == currentText)
				{
					match = candidate;
					break;
				}
			}

			if (match == null)
			{
				report.Fail("could not find a label reading \"" + currentText + "\"");
				return;
			}

			match.text = replacement;
			report.Note("\"" + currentText + "\" relabelled to \"" + replacement + "\"");
		}

		/// <summary>
		/// Builds JoinCodeScreen.prefab from the rename prompt.
		/// </summary>
		///
		/// The rename prompt is the right starting point: same shape of dialog -
		/// a title, a line of text, two buttons - and the left button becomes Copy
		/// rather than Cancel, so nothing is lost by dismissing with the wrong
		/// one. What changes is that the input field goes away: this dialog
		/// displays a code, it does not accept one.
		private static Report TransferJoinCodeScreen()
		{
			Report report = new Report();
			const string screenPath = "Assets/GameObject/JoinCodeScreen.prefab";
			const string sourcePath = "Assets/GameObject/RenameUnitScene.prefab";

			if (AssetDatabase.LoadAssetAtPath<GameObject>(screenPath) == null)
			{
				if (AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath) == null)
				{
					report.Fail("could not find " + sourcePath + " to build the join code dialog from");
					return report;
				}

				GameObject clone = UnityEngine.Object.Instantiate(
					AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath));
				PrefabUtility.SaveAsPrefabAsset(clone, screenPath);
				UnityEngine.Object.DestroyImmediate(clone);

				report.Note("cloned " + sourcePath + " to " + screenPath);
			}

			GameObject contents = PrefabUtility.LoadPrefabContents(screenPath);

			try
			{
				OpenFrontier.IP.UI.Screens.RenameUnit.RenameUnitScreen source =
					contents.GetComponent<OpenFrontier.IP.UI.Screens.RenameUnit.RenameUnitScreen>();

				if (source != null)
				{
					UnityEngine.Object.DestroyImmediate(source);
				}

				OpenFrontier.IP.Multiplayer.UI.JoinCodeScreen target =
					contents.GetComponent<OpenFrontier.IP.Multiplayer.UI.JoinCodeScreen>();

				if (target == null)
				{
					target = contents.AddComponent<OpenFrontier.IP.Multiplayer.UI.JoinCodeScreen>();
				}

				// See TransferItemPrefab: a null here means Unity had no
				// MonoScript for the type, and that failure is silent.
				if (target == null)
				{
					report.Fail(screenPath + ": Unity would not add JoinCodeScreen - check that "
						+ "JoinCodeScreen.cs exists and declares only that class.");
					return report;
				}

				target.TitleLabel = FindAnyLabel(contents, "TextScreenTitle");
				target.LegacyCodeLabel = FindLegacyLabel(contents, "LegacyTextSubTitle");
				target.CodeLabel = FindAnyLabel(contents, "LegacyTextSubTitle");
				target.OkButton = FindButton(contents, "ButtonIconOK");
				target.CopyButton = FindButton(contents, "ButtonIconCancel");
				target.EnableMultiplayerToggle = FindToggle(contents, "CheckboxEnableMultiplayer");
				target.HintLabel = FindLegacyLabel(contents, "MaxCharsText");

				List<string> unwired = new List<string>();

				if (target.TitleLabel == null) unwired.Add("TextScreenTitle (TitleLabel)");

				// Whichever kind the label turned out to be, one of these two must
				// be set - the dialog has to be able to show the code somehow.
				if (target.CodeLabel == null && target.LegacyCodeLabel == null)
				{
					unwired.Add("LegacyTextSubTitle (CodeLabel or LegacyCodeLabel)");
				}

				if (target.OkButton == null) unwired.Add("ButtonIconOK (OkButton)");
				if (target.CopyButton == null) unwired.Add("ButtonIconCancel (CopyButton)");

				// Optional rather than required: the dialog is perfectly usable
				// without it, it just means an extra trip to Options.
				if (target.EnableMultiplayerToggle == null)
				{
					report.Manual_("no CheckboxEnableMultiplayer in the dialog - the player will "
						+ "have to turn multiplayer on in Options instead");
				}

				if (unwired.Count > 0)
				{
					report.Fail(screenPath + ": could not find " + string.Join(", ", unwired));
				}
				else
				{
					report.Note("JoinCodeScreen.prefab wired");
				}

				// The input field goes, along with its placeholder. Deactivated
				// rather than deleted, so the dialog stays reversible.
				//
				// MaxCharsText is deliberately NOT in this list: it was the
				// character counter, which meant nothing once there was nothing
				// to type, and it is now the small line under the code that says
				// where to turn multiplayer off again. Its text is replaced at
				// runtime, so only the stale placeholder matters.
				foreach (string gone in new[] { "LegacyInputField", "Placeholder" })
				{
					Transform found = contents.transform.FindDeep(gone);

					if (found != null && found.gameObject.activeSelf)
					{
						found.gameObject.SetActive(false);
						report.Note("hid " + gone);
					}
				}

				Transform hint = contents.transform.FindDeep("MaxCharsText");

				if (hint != null && !hint.gameObject.activeSelf)
				{
					hint.gameObject.SetActive(true);
					report.Note("showed MaxCharsText as the hint line");
				}

				// The cancel button now copies, so it says so. Icon-only like
				// the rest of the button row, which is why this matches on the
				// label underneath rather than expecting text on the button.
				SetButtonLabel(contents, "ButtonIconCancel", "Copy", report);

				PrefabUtility.SaveAsPrefabAsset(contents, screenPath);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(contents);
			}

			return report;
		}

		private static Button FindButton(GameObject root, string objectName)
		{
			Transform found = root.transform.FindDeep(objectName);
			return found == null ? null : found.GetComponent<Button>();
		}

		private static Toggle FindToggle(GameObject root, string objectName)
		{
			Transform found = root.transform.FindDeep(objectName);
			return found == null ? null : found.GetComponent<Toggle>();
		}

		/// <summary>
		/// Finds a label, whichever of the three label kinds it turns out to be.
		/// </summary>
		/// <remarks>
		/// The prefabs are a genuine mix: the Noto Sans font pass moved most
		/// labels to TMP, but some are still legacy uGUI Text (serialised with
		/// m_FontData rather than m_fontAsset). Returns null for a legacy label,
		/// which cannot be assigned to a TMP field - see FindLegacyLabel for
		/// those, and the caller for why both are looked for.
		/// </remarks>
		private static TMPro.TMP_Text FindAnyLabel(GameObject root, string objectName)
		{
			Transform found = root.transform.FindDeep(objectName);

			if (found == null)
			{
				return null;
			}

			TMPro.TextMeshProUGUI ugui = found.GetComponent<TMPro.TextMeshProUGUI>();

			if (ugui != null)
			{
				return ugui;
			}

			TMPro.TextMeshPro world = found.GetComponent<TMPro.TextMeshPro>();

			if (world != null)
			{
				return world;
			}

			return null;
		}

		/// <summary>
		/// Finds a legacy uGUI Text, which cannot be assigned to a TMP field.
		/// </summary>
		private static Text FindLegacyLabel(GameObject root, string objectName)
		{
			Transform found = root.transform.FindDeep(objectName);
			return found == null ? null : found.GetComponent<Text>();
		}

		/// <summary>
		/// Renames a button's caption, whichever kind of label it carries.
		/// </summary>
		private static void SetButtonLabel(GameObject root, string objectName, string label, Report report)
		{
			Transform found = root.transform.FindDeep(objectName);

			if (found == null)
			{
				return;
			}

			TMPro.TextMeshProUGUI tmp = found.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);

			if (tmp != null)
			{
				tmp.text = label;
				report.Note(objectName + " labelled \"" + label + "\"");
				return;
			}

			Text legacy = found.GetComponentInChildren<Text>(true);

			if (legacy != null)
			{
				legacy.text = label;
				report.Note(objectName + " labelled \"" + label + "\"");
			}
		}

		/// <summary>
		/// Puts SavedHostItemUI on MultiplayerGameItem.prefab, wires its labels,
		/// and points the list's ItemPrefab at it.
		/// </summary>
		///
		/// The list instantiates ItemPrefab once per row, so it needs a prefab
		/// carrying the row script. There is no multiplayer equivalent to
		/// LoadGameItem.prefab, which is the template the save-game list clones.
		///
		/// MultiplayerGameItem.prefab is already that prefab - a copy of the save
		/// row with the labels renamed for a host - so all that is left is the
		/// script and the field assignments.
		///
		/// Labels are matched by object name rather than by position, so the
		/// wiring survives a re-layout of the prefab.
		///
		/// Deliberately does not touch the screen prefab: its contents are already
		/// open when this runs, and saving the asset underneath an open
		/// LoadPrefabContents would be overwritten - or would fail - when the
		/// caller saves. Pointing ItemPrefab at the result is the caller's job.
		private static Report TransferItemPrefab()
		{
			Report report = new Report();
			const string itemPath = "Assets/GameObject/MultiplayerGameItem.prefab";
			const string sourcePath = "Assets/GameObject/LoadGameItem.prefab";

			if (AssetDatabase.LoadAssetAtPath<GameObject>(itemPath) != null)
			{
				report.Note("MultiplayerGameItem.prefab already exists");
			}
			else if (AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath) == null)
			{
				report.Fail("could not find " + sourcePath + " to clone the row prefab from");
				return report;
			}
			else
			{
				// Cloned as an asset rather than instantiated in the scene, so the
				// copy is a prefab in its own right.
				GameObject sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
				GameObject clone = UnityEngine.Object.Instantiate(sourceAsset);
				PrefabUtility.SaveAsPrefabAsset(clone, itemPath);
				UnityEngine.Object.DestroyImmediate(clone);

				report.Note("cloned " + sourcePath + " to " + itemPath);
			}

			GameObject contents = PrefabUtility.LoadPrefabContents(itemPath);

		try
		{
			LoadGameItemUI source = contents.GetComponent<LoadGameItemUI>();
			OpenFrontier.IP.Multiplayer.UI.SavedHostItemUI target =
				contents.GetComponent<OpenFrontier.IP.Multiplayer.UI.SavedHostItemUI>();

			// A missing-script component left behind by an earlier run has to
			// go before anything else. It is not reachable by type - its script
			// is gone - so neither GetComponent nor DestroyImmediate can see it,
			// and Unity refuses to save a prefab that carries one ("You are
			// trying to save a Prefab with a missing script"). Found by walking
			// the components instead, which is the only way to see these.
			int orphans = RemoveMissingScripts(contents);

			if (orphans > 0)
			{
				// Re-read afterwards: the orphan counted as the row's script, so
				// anything resolved before this point may have been looking at
				// the wrong component.
				source = contents.GetComponent<LoadGameItemUI>();
				target = contents.GetComponent<OpenFrontier.IP.Multiplayer.UI.SavedHostItemUI>();

				report.Note("removed " + orphans + " missing-script component(s) from " + itemPath);
			}

			if (source != null)
			{
				UnityEngine.Object.DestroyImmediate(source);
			}

			if (target == null)
			{
				target = contents.AddComponent<OpenFrontier.IP.Multiplayer.UI.SavedHostItemUI>();
			}

			// AddComponent returns null when Unity has no MonoScript for
			// the type - which it has not if the class shares a file with
			// another MonoBehaviour, since Unity registers one per file,
			// named after the file. That failure is silent: the
			// component is not added, the prefab serialises a missing
			// script, and the row renders blank forever. Caught here
			// rather than leaving it to look like a wiring problem.
			if (target == null)
			{
				report.Fail(itemPath + ": Unity would not add SavedHostItemUI. A "
					+ "MonoBehaviour needs a file named after it - check that "
					+ "SavedHostItemUI.cs exists and declares only that class.");
				return report;
			}

			// Wired by object name, so the mapping survives a re-layout of
			// the prefab.
			target.NameLabel = FindLabel(contents, "TextHostSenario");
			target.StatusLabel = FindLabel(contents, "TextConnection");
			target.VersionLabel = FindLabel(contents, "TextHostVersion");
			target.PlayerCountLabel = FindLabel(contents, "TextHostPlayerCount");

			List<string> unwired = new List<string>();

			if (target.NameLabel == null) unwired.Add("TextHostSenario (NameLabel)");
			if (target.StatusLabel == null) unwired.Add("TextConnection (StatusLabel)");
			if (target.VersionLabel == null) unwired.Add("TextHostVersion (VersionLabel)");
			if (target.PlayerCountLabel == null) unwired.Add("TextHostPlayerCount (PlayerCountLabel)");

			if (unwired.Count > 0)
			{
				report.Fail(itemPath + ": could not find " + string.Join(", ", unwired));
			}
			else
			{
				report.Note("MultiplayerGameItem.prefab wired");
			}

			// The permadeath chip came over from the save-game row. It is
			// a plain white box with no meaning for a host, and the row
			// now dims unreachable hosts instead of badging them, so it
			// is switched off rather than left showing an empty chip.
			// "Selected" is left alone - the list owns that one.
			Transform chip = contents.transform.FindDeep("PermadeathImage");

			if (chip != null && chip.gameObject.activeSelf)
			{
				chip.gameObject.SetActive(false);
				report.Note("hid PermadeathImage");
			}

			PrefabUtility.SaveAsPrefabAsset(contents, itemPath);
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(contents);
		}

		return report;
	}

		/// <summary>
		/// Deletes every component whose script is missing, and returns how many.
		/// </summary>
		/// <remarks>
		/// These are invisible to the ordinary APIs on purpose: a component whose
		/// MonoScript is gone compares equal to null for GetComponent, so asking
		/// for it by type returns null whether or not it is there, and
		/// DestroyImmediate on a null does nothing. Walking GetComponents by
		/// type does not surface them either, so nothing but this API reaches
		/// one.
		///
		/// Unity refuses to save a prefab carrying one ("You are trying to save a
		/// Prefab with a missing script"), which is what made an earlier run of
		/// this leave a prefab that no number of re-runs could fix: the orphan
		/// survived every attempt to destroy it, and then blocked the save that
		/// would have recorded the repair.
		/// </remarks>
		private static int RemoveMissingScripts(GameObject root)
		{
			// GameObjectUtility.RemoveMonoBehavioursWithMissingScript is the API
			// for this. Its companions - ComponentUtility.RemoveComponentAtIndex
			// and DestroyImmediateImmediate - do not exist in this Unity version
			// (checked by reflection against 6000.6.4f1), so this is not a
			// stylistic choice but the only one available.
			//
			// It recurses, which is what a screen needs: the orphan can be on any
			// descendant, not just the root. It returns a count rather than a
			// bool, despite the name reading like a question.
			return GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
		}

		private static TMPro.TextMeshProUGUI FindLabel(GameObject root, string objectName)
		{
			Transform found = root.transform.FindDeep(objectName);
			return found == null ? null : found.GetComponent<TMPro.TextMeshProUGUI>();
		}

		/// <summary>
		/// Creates the Add Host button by cloning the delete button, so it
		/// matches the others in the row exactly.
		/// </summary>
		///
		/// Building it from scratch would mean guessing at the sprite, colours
		/// and helper scripts; cloning guarantees it matches. The clone is placed
		/// after the delete button in the same row, then given its own label.
		private static GameObject CreateAddHostButton(GameObject template)
		{
			GameObject created = UnityEngine.Object.Instantiate(template, template.transform.parent);
			created.name = "ButtonIconAddHost";

			Button button = created.GetComponent<Button>();

			if (button != null)
			{
				// The clone inherits the template's click listeners, which belong
				// to the delete action. Cleared so the new button starts inert and
				// the screen wires it up itself.
				button.onClick = new Button.ButtonClickedEvent();
			}

			// Name it. The row's captions are all named "Text", so the label is
			// found as the button's own child.
			Text label = created.GetComponentInChildren<Text>(includeInactive: true);

			if (label != null)
			{
				label.text = "Add Host";
			}

			return created;
		}

		private static void Assign(object target, string fieldName, UnityEngine.Object value, Report report, string description)
		{
			if (value == null)
			{
				report.Fail(description + " - source reference is null");
				return;
			}

			// Assigned through reflection so the field name is checked at runtime
			// rather than by the compiler; a rename in either script then shows
			// up as a clear failure instead of a silently unwired prefab.
			System.Reflection.FieldInfo field = target.GetType().GetField(fieldName);

			if (field == null)
			{
				report.Fail(description + " - MultiplayerScreen has no field called " + fieldName);
				return;
			}

			if (!field.FieldType.IsInstanceOfType(value) && !field.FieldType.IsAssignableFrom(value.GetType()))
			{
				report.Fail(description + " - type mismatch, field is " + field.FieldType.Name);
				return;
			}

			field.SetValue(target, value);
			report.Note(description);
		}

		private static Text FindText(GameObject root, string objectName)
		{
			Transform found = root.transform.FindDeep(objectName);
			return found == null ? null : found.GetComponent<Text>();
		}
	}

	internal static class MultiplayerTransformExtensions
	{
		/// <summary>
		/// Finds a descendant by name at any depth.
		/// </summary>
		/// <remarks>
		/// Transform.Find only searches direct children, and these objects are
		/// nested several levels deep inside the screen's layout, so a recursive
		/// search is needed.
		/// </remarks>
		public static Transform FindDeep(this Transform root, string objectName)
		{
			if (root.name == objectName)
			{
				return root;
			}

			for (int i = 0; i < root.childCount; i++)
			{
				Transform match = root.GetChild(i).FindDeep(objectName);

				if (match != null)
				{
					return match;
				}
			}

			return null;
		}
	}
}
#endif