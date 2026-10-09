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

			public bool Success => Problems.Count == 0;

			public string Describe()
			{
				System.Text.StringBuilder builder = new System.Text.StringBuilder();

				builder.Append(Success ? "Done. Transferred:\n" : "INCOMPLETE - nothing was saved.\n");
				builder.Append("  " + (Done.Count == 0 ? "nothing" : string.Join("\n  ", Done)) + "\n");

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

			Assign(target, nameof(target.ItemsList), source.ItemsList, report,
				"ItemsList <- ScrollRectGameList");

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

			// The title, so the screen does not read "Load Game".
			Text title = FindText(contents, "TextScreenTitle");
			if (title != null)
			{
				title.text = "Multiplayer";
				report.Note("title set to Multiplayer");
			}
			else
			{
				report.Fail("could not find TextScreenTitle to rename");
			}

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

			// The empty "no items" message should mention hosts, not games.
			Text noItems = FindText(contents, "TextNoItems");

			if (noItems != null)
			{
				noItems.text = "You have not added any hosts yet. Use Add Host to enter a join code.";
				report.Note("empty-list message updated");
			}

			return report;
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