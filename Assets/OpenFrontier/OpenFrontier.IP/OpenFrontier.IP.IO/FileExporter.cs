using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenFrontier.IP.UI;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.MessageBox;
using SimpleFileBrowser;
using UnityEngine;

namespace OpenFrontier.IP.IO
{
	public static class FileExporter
	{
		private static bool? exportFileSupported;

		private static bool? exportMultipleFilesSupported;

		public static bool IsExportFileSupported()
		{
			if (exportFileSupported.HasValue)
			{
				return exportFileSupported.Value;
			}
			exportFileSupported = GetIsExportFileSupported();
			return exportFileSupported.Value;
		}

		private static bool GetIsExportFileSupported()
		{
			return true;
		}

		public static bool IsMultiFileExportSupported()
		{
			if (exportMultipleFilesSupported.HasValue)
			{
				return exportMultipleFilesSupported.Value;
			}
			exportMultipleFilesSupported = GetIsMultiFileExportSupported();
			return exportMultipleFilesSupported.Value;
		}

		private static bool GetIsMultiFileExportSupported()
		{
			return true;
		}

		public static void TryExportFiles(MonoBehaviour caller, IEnumerable<string> paths, Action<bool> callback = null)
		{
			TryExportFilesStandalone(caller, paths, callback);
		}

		private static void TryExportFilesStandalone(MonoBehaviour caller, IEnumerable<string> paths, Action<bool> callback)
		{
			try
			{
				caller.StartCoroutine(ShowExportFileBrowserCoroutine((string exportLocation) =>
				{
					if (!string.IsNullOrWhiteSpace(exportLocation))
					{
						try
						{
							ExportFilesStandalone(paths, exportLocation, callback);
							callback(obj: true);
							return;
						}
						catch (Exception exception2)
						{
							Debug.LogException(exception2);
							UIController.Instance.ShowError("An error occured while attempting to export the files (code: 2).");
							callback(obj: false);
							return;
						}
					}
					callback(obj: false);
				}));
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				UIController.Instance.ShowError("An error occured while attempting to export the files (code: 1).");
				callback(obj: false);
			}
		}

		private static void ExportFilesStandalone(IEnumerable<string> paths, string exportLocation, Action<bool> callback)
		{
			string[] pathsArray = paths.ToArray();
			string[] writeLocations = pathsArray.Select((string e) => Path.Combine(exportLocation, Path.GetFileName(e))).ToArray();
			if (writeLocations.Any((string e) => File.Exists(e)))
			{
				UIController.Instance.ShowMessageBox("Overwrite existing files?", MessageBoxButtons.OkCancel, (MessageBoxScreen sender, MessageBoxResult result) =>
				{
					if (result == MessageBoxResult.Ok)
					{
						for (int i = 0; i < pathsArray.Length; i++)
						{
							File.Copy(pathsArray[i], writeLocations[i], overwrite: true);
						}
						callback(obj: true);
					}
					else
					{
						callback(obj: false);
					}
				}, MessageBoxIcon.Question);
			}
			else
			{
				for (int num = 0; num < pathsArray.Length; num++)
				{
					File.Copy(pathsArray[num], writeLocations[num], overwrite: false);
				}
				callback(obj: true);
			}
		}

		private static void TryExportFilesNative(IEnumerable<string> paths, Action<bool> callback)
		{
			try
			{
				if (NativeFilePicker.ExportMultipleFiles(paths.ToArray(), (bool success) =>
				{
					if (callback != null)
					{
						callback(success);
					}
				}) == NativeFilePicker.Permission.Denied)
				{
					UIController.Instance.ShowMessageBox("Permission to access file system was denied. Please check application permissions.", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning, "No permission");
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				UIController.Instance.ShowError("An error occured while attempting to export file");
			}
		}

		public static void TryExportFile(MonoBehaviour caller, string path, Action<bool> callback = null)
		{
			TryExportFileStandalone(caller, path, callback);
		}

		private static void TryExportFileStandalone(MonoBehaviour caller, string currentPath, Action<bool> callback)
		{
			try
			{
				caller.StartCoroutine(ShowExportFileBrowserCoroutine((string exportLocation) =>
				{
					if (!string.IsNullOrWhiteSpace(exportLocation))
					{
						try
						{
							ExportFilesStandalone(new string[1] { currentPath }, exportLocation, callback);
							callback(obj: true);
							return;
						}
						catch (Exception exception2)
						{
							Debug.LogException(exception2);
							UIController.Instance.ShowError("An error occured while attempting to export the file (code: 2).");
							callback(obj: false);
							return;
						}
					}
					callback(obj: false);
				}));
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				UIController.Instance.ShowError("An error occured while attempting to export the file (code: 1).");
				callback(obj: false);
			}
		}

		private static IEnumerator ShowExportFileBrowserCoroutine(Action<string> callback)
		{
			yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Folders, allowMultiSelection: false, null, null, "Choose location to export to", "Export");
			Debug.Log($"FileBrowser return status: {FileBrowser.Success}");
			if (FileBrowser.Success && FileBrowser.Result != null && FileBrowser.Result.Length != 0)
			{
				callback(FileBrowser.Result[0]);
			}
		}

		private static void TryExportFileNative(string path, Action<bool> callback)
		{
			try
			{
				if (NativeFilePicker.ExportFile(path, (bool success) =>
				{
					if (callback != null)
					{
						callback(success);
					}
				}) == NativeFilePicker.Permission.Denied)
				{
					UIController.Instance.ShowMessageBox("Permission to access file system was denied. Please check application permissions.", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning, "No permission");
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				UIController.Instance.ShowError("An error occured while attempting to export file");
			}
		}
	}
}
