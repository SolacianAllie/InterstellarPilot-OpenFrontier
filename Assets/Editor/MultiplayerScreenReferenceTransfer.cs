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
					"Transfers the LoadGameScreen references on MultiplayerScreen.prefab across to "
						+ "MultiplayerScreen, removes the import button and hides the save-only labels.",
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

			// The source is the LoadGameScreen component still sitting on the
			// prefab, holding every reference the clone inherited.
			LoadGameScreen source = contents.GetComponent<LoadGameScreen>();

			if (source == null)
			{
				report.Fail("no LoadGameScreen component on the prefab - add one, or restore the prefab");
				return report;
			}

			OpenFrontier.IP.Multiplayer.UI.MultiplayerScreen target =
				contents.GetComponent<OpenFrontier.IP.Multiplayer.UI.MultiplayerScreen>();

			if (target == null)
			{
				report.Fail("no MultiplayerScreen component on the prefab - add it before running this");
				return report;
			}

			// ---- controls: reuse the existing buttons -------------------
			// The clone's four bottom buttons become join / add / rename /
			// delete. The import button has no equivalent and goes away.

			Assign(target, nameof(target.JoinButton), source.LoadGameButton, report,
				"JoinButton <- LoadGameButton");

			Assign(target, nameof(target.AddHostButton), source.ExportFileButton, report,
				"AddHostButton <- ExportFileButton");

			Assign(target, nameof(target.RenameHostButton), source.RenameFileButton, report,
				"RenameHostButton <- RenameFileButton");

			Assign(target, nameof(target.DeleteHostButton), source.DeleteGameConfirmButton, report,
				"DeleteHostButton <- DeleteGameConfirmButton");

			// ---- panels --------------------------------------------------

			Assign(target, nameof(target.HostInfoRoot), source.GameInfoRoot, report,
				"HostInfoRoot <- GameInfoRoot");

			Assign(target, nameof(target.MultipleItemsSelectedRoot), source.MultipleItemsSelectedRoot, report,
				"MultipleItemsSelectedRoot <- MultiSelectRoot");

			// ItemsList cannot be transferred: the field is typed SavedHostItemsUI and
			// the object carries LoadGameItemsUI. Those are different classes -
			// both derive from ScrollList, but of different item types - so the
			// reference cannot be assigned across. The component has to be
			// swapped in the inspector, and the field re-assigned afterwards.
			//
			// Reported rather than silently skipped, because leaving it is what
			// stops the screen working, but it is a manual step rather than a
			// fault with this prefab.
			if (target.ItemsList == null)
			{
				report.Manual_("ItemsList still needs SavedHostItemsUI - swap the component on "
					+ "ScrollRectGameList, then re-run to assign it");
			}
			else
			{
				report.Note("ItemsList already assigned");
			}

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

			return report;
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