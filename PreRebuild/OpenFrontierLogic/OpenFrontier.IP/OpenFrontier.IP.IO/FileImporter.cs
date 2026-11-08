using System;
using System.Collections;
using OpenFrontier.IP.Engine.SaveGame;
using OpenFrontier.IP.UI;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.MessageBox;
using SimpleFileBrowser;
using UnityEngine;

namespace OpenFrontier.IP.IO
{
	public static class FileImporter
	{
		private static bool? importMultipleFilesSupported;

		public static bool IsImportFileSupported()
		{
			return true;
		}

		public static bool IsMultiFileImportSupported()
		{
			if (importMultipleFilesSupported.HasValue)
			{
				return importMultipleFilesSupported.Value;
			}
			importMultipleFilesSupported = GetIsMultiFileImportSupported();
			return importMultipleFilesSupported.Value;
		}

		private static bool GetIsMultiFileImportSupported()
		{
			if (!IsImportFileSupported())
			{
				return false;
			}
			return true;
		}

		private static IEnumerator ShowFileBrowserDialogCoroutine(Action<string> callback)
		{
			yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, allowMultiSelection: false, null, null, "Choose file to import", "Import");
			Debug.Log($"FileBrowser return status: {FileBrowser.Success}");
			if (FileBrowser.Success && FileBrowser.Result != null && FileBrowser.Result.Length != 0)
			{
				callback(FileBrowser.Result[0]);
			}
		}

		private static void TryGetFileFromBrowserNative(Action<string> callback)
		{
			string binaryFileType = IOHelper.GetBinaryFileType();
			try
			{
				if (NativeFilePicker.PickFile((string path) =>
				{
					if (path != null && path.Length > 0)
					{
						callback(path);
					}
				}, new string[1] { binaryFileType }) == NativeFilePicker.Permission.Denied)
				{
					ShowBrowseForFilesPermissionError();
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				UIController.Instance.ShowError("An error occured attempting to browse for files");
			}
		}

		public static void TryGetFilesFromBrowser(MonoBehaviour caller, Action<string[]> callback)
		{
			TryGetFilesFromBrowserStandalone(caller, callback);
		}

		private static void TryGetFilesFromBrowserStandalone(MonoBehaviour caller, Action<string[]> callback)
		{
			InitStandaloneFileBrowserForSavedGameFiles();
			caller.StartCoroutine(ShowMultiFileBrowserDialogCoroutine(callback));
		}

		private static IEnumerator ShowMultiFileBrowserDialogCoroutine(Action<string[]> callback)
		{
			yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, allowMultiSelection: true, null, null, "Choose file to import", "Import");
			Debug.Log($"FileBrowser (multi) return status: {FileBrowser.Success}");
			if (FileBrowser.Success && FileBrowser.Result != null && FileBrowser.Result.Length != 0)
			{
				callback(FileBrowser.Result);
			}
		}

		private static void TryGetFilesFromBrowserNative(Action<string[]> callback)
		{
			string binaryFileType = IOHelper.GetBinaryFileType();
			try
			{
				if (NativeFilePicker.PickMultipleFiles((string[] paths) =>
				{
					if (paths != null && paths.Length != 0)
					{
						callback(paths);
					}
				}, new string[1] { binaryFileType }) == NativeFilePicker.Permission.Denied)
				{
					ShowBrowseForFilesPermissionError();
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				UIController.Instance.ShowError("An error occured attempting to browse for files");
			}
		}

		public static void ShowBrowseForFilesPermissionError()
		{
			UIController.Instance.ShowMessageBox("Permission to browse for files was denied. Please check application permissions.", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning, "No permission");
		}

		public static void TryGetFileFromBrowser(MonoBehaviour caller, Action<string> callback)
		{
			TryGetFileFromBrowserStandalone(caller, callback);
		}

		private static void TryGetFileFromBrowserStandalone(MonoBehaviour caller, Action<string> callback)
		{
			InitStandaloneFileBrowserForSavedGameFiles();
			caller.StartCoroutine(ShowFileBrowserDialogCoroutine(callback));
		}

		private static void InitStandaloneFileBrowserForSavedGameFiles()
		{
			FileBrowser.SetFilters(true, new FileBrowser.Filter("Data", "." + SaveGameUtilities.Extension));
			FileBrowser.SetDefaultFilter("." + SaveGameUtilities.Extension);
		}
	}
}
