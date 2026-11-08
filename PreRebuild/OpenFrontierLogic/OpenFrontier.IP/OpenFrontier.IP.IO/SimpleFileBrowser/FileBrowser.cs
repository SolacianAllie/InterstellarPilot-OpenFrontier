using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleFileBrowser
{
	/// <summary>
	/// OpenFrontier-native replacement for the vendored SimpleFileBrowser plugin's static API.
	/// Implements exactly the surface the project uses (FileImporter/FileExporter):
	/// WaitForLoadDialog, Success, Result, SetFilters, SetDefaultFilter and the nested
	/// PickMode/Filter types, with the signatures preserved by the decompile. The dialog
	/// itself is a code-built uGUI window (FileBrowserDialogWindow); no prefab assets needed.
	/// No Unity native calls in static initialisers (AGENTS.md): everything is set lazily here.
	/// </summary>
	public static class FileBrowser
	{
		public enum PickMode
		{
			None = 0,
			Files = 1,
			Folders = 2,
			FilesAndFolders = 3
		}

		public class Filter
		{
			public string Name;
			public string[] Extensions;

			public Filter(string name, params string[] extensions)
			{
				Name = name;
				Extensions = Normalize(extensions);
			}

			private static string[] Normalize(string[] extensions)
			{
				if (extensions == null)
				{
					return new string[0];
				}
				string[] result = new string[extensions.Length];
				for (int i = 0; i < extensions.Length; i++)
				{
					result[i] = extensions[i].ToLowerInvariant();
					if (!result[i].StartsWith(".", StringComparison.Ordinal))
					{
						result[i] = "." + result[i];
					}
				}
				return result;
			}

			public bool Matches(string filename)
			{
				if (Extensions == null || Extensions.Length == 0)
				{
					return true;
				}
				string lower = filename.ToLowerInvariant();
				for (int i = 0; i < Extensions.Length; i++)
				{
					if (lower.EndsWith(Extensions[i], StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
				return false;
			}
		}

		public delegate void OnSuccess(string[] result);
		public delegate void OnCancel();

		// ---- dialog state (one dialog at a time; fields stay null until first open) ----
		private static bool _openAny;
		private static bool _done;
		private static bool _successState = false;
		private static string[] _resultState = new string[0];

		private static List<Filter> _filterList;           // SetFilters(true, ...) state; null = no filters offered
		private static bool _showAllFilesAvailable = true; // SetFilters(showAllFiles) -> whether "all files" toggle is offered
		private static Filter _activeFilter;               // selected filter; null = show all files

		public static bool Success { get; private set; }

		public static string[] Result { get; private set; }

		// ---- state the dialog window reads (same assembly, so internal is fine) ----
		internal static List<Filter> AvailableFilters
		{
			get { return _filterList; }
		}

		internal static bool ShowAllFilesAvailable
		{
			get { return _showAllFilesAvailable; }
		}

		internal static Filter ActiveFilter
		{
			get { return _activeFilter; }
		}

		public static void SetFilters(bool showAllFiles, params Filter[] filters)
		{
			_showAllFilesAvailable = showAllFiles;
			if (filters == null || filters.Length == 0)
			{
				_filterList = null;
				_activeFilter = null;
			}
			else
			{
				_filterList = new List<Filter>(filters);
				_activeFilter = _filterList[0];
			}
		}

		public static void SetDefaultFilter(string filterExtension)
		{
			if (_filterList == null || filterExtension == null)
			{
				return;
			}
			string lower = filterExtension.ToLowerInvariant();
			if (!lower.StartsWith(".", StringComparison.Ordinal))
			{
				lower = "." + lower;
			}
			for (int i = 0; i < _filterList.Count; i++)
			{
				string[] exts = _filterList[i].Extensions;
				if (exts == null)
				{
					continue;
				}
				for (int e = 0; e < exts.Length; e++)
				{
					if (string.Equals(exts[e], lower, StringComparison.OrdinalIgnoreCase))
					{
						_activeFilter = _filterList[i];
						return;
					}
				}
			}
		}

		/// <summary>Sets whether the dialog shows all files or stays on the active filter; the dialog calls this when the user toggles.</summary>
		internal static void SetShowAllFiles(bool showAll)
		{
			if (_showAllFilesAvailable)
			{
				_activeFilter = showAll ? null : (_filterList != null ? _filterList[0] : null);
			}
		}

		public static bool ShowLoadDialog(OnSuccess onSuccess, OnCancel onCancel, PickMode pickMode,
			bool allowMultiSelection = false, string initialPath = null, string initialFilename = null,
			string title = "Load", string loadButtonText = "Select")
		{
			if (!BeginOpen())
			{
				if (onCancel != null)
				{
					onCancel();
				}
				return false;
			}
			FileBrowserDialogWindow.Open(pickMode, allowMultiSelection, initialPath, initialFilename, title, loadButtonText,
				delegate (bool success, string[] paths)
				{
					EndOpen(success, paths);
					if (success)
					{
						if (onSuccess != null)
						{
							onSuccess(Result);
						}
					}
					else if (onCancel != null)
					{
						onCancel();
					}
				});
			return true;
		}

		public static IEnumerator WaitForLoadDialog(PickMode pickMode, bool allowMultiSelection = false,
			string initialPath = null, string initialFilename = null, string title = "Load", string loadButtonText = "Select")
		{
			if (!BeginOpen())
			{
				yield break; // Success/Result already reported by BeginOpen's rejection story
			}
			FileBrowserDialogWindow.Open(pickMode, allowMultiSelection, initialPath, initialFilename, title, loadButtonText, EndOpen);
			while (!_done)
			{
				yield return null;
			}
		}

		/// <summary>Rejection story: Success=false, Result empty, open flag stays free.</summary>
		private static bool BeginOpen()
		{
			if (_openAny)
			{
				Debug.LogWarning("FileBrowser: a dialog is already open; ignoring the extra request.");
				_done = true;
				_successState = false;
				_resultState = new string[0];
				Success = false;
				Result = _resultState;
				return false;
			}
			_openAny = true;
			_done = false;
			_successState = false;
			_resultState = new string[0];
			Success = false;
			Result = new string[0];
			return true;
		}

		private static void EndOpen(bool success, string[] paths)
		{
			_successState = success;
			_resultState = paths ?? new string[0];
			_done = true;
			Success = success;
			Result = _resultState;
			_openAny = false;
		}
	}
}
