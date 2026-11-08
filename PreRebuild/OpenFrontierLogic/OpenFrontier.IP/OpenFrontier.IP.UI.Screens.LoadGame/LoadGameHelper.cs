using System;
using System.Collections.Generic;
using System.IO;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.SaveGame;
using OpenFrontier.IP.IO;
using OpenFrontier.IP.UI.Screens.MessageBox;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.LoadGame
{
	public static class LoadGameHelper
	{
		public static bool TryImportSaveGameHeaders(IEnumerable<EngineSaveGameHeader> headers)
		{
			EngineIO.TryCreateSaveDirectoryIfRequired();
			foreach (EngineSaveGameHeader header in headers)
			{
				try
				{
					string savePath = SaveGameUtilities.GetSavePath(Path.GetFileNameWithoutExtension(header.FullPath), ensureUnique: true);
					File.Copy(header.FullPath, savePath);
					header.FullPath = savePath;
				}
				catch (Exception innerException)
				{
					Debug.LogException(new Exception("Failed to import save game header into local directory", innerException));
					return false;
				}
			}
			return true;
		}

		public static void GetSaveGameHeaders(MonoBehaviour caller, Action<IEnumerable<EngineSaveGameHeader>> successfulCallback)
		{
			if (FileImporter.IsMultiFileImportSupported())
			{
				GetMultipleSaveGameHeaders(caller, successfulCallback);
				return;
			}
			GetSaveGameHeader(caller, (EngineSaveGameHeader item) =>
			{
				successfulCallback(new EngineSaveGameHeader[1] { item });
			});
		}

		private static void GetSaveGameHeader(MonoBehaviour caller, Action<EngineSaveGameHeader> successfulCallback)
		{
			FileImporter.TryGetFileFromBrowser(caller, (string filePath) =>
			{
				EngineSaveGameHeader saveGameHeaderFromFilePath = GetSaveGameHeaderFromFilePath(filePath);
				if (saveGameHeaderFromFilePath != null)
				{
					successfulCallback(saveGameHeaderFromFilePath);
				}
			});
		}

		private static void GetMultipleSaveGameHeaders(MonoBehaviour caller, Action<IEnumerable<EngineSaveGameHeader>> successfulCallback)
		{
			FileImporter.TryGetFilesFromBrowser(caller, (string[] filePaths) =>
			{
				if (filePaths.Length != 0)
				{
					if (filePaths.Length == 1)
					{
						EngineSaveGameHeader saveGameHeaderFromFilePath = GetSaveGameHeaderFromFilePath(filePaths[0]);
						if (saveGameHeaderFromFilePath != null)
						{
							successfulCallback(new EngineSaveGameHeader[1] { saveGameHeaderFromFilePath });
						}
					}
					else
					{
						List<EngineSaveGameHeader> list = new List<EngineSaveGameHeader>(filePaths.Length * 2);
						for (int i = 0; i < filePaths.Length; i++)
						{
							EngineSaveGameHeader saveGameHeaderFromFilePath2 = GetSaveGameHeaderFromFilePath(filePaths[i]);
							if (saveGameHeaderFromFilePath2 == null)
							{
								return;
							}
							list.Add(saveGameHeaderFromFilePath2);
						}
						successfulCallback(list);
					}
				}
			});
		}

		private static EngineSaveGameHeader GetSaveGameHeaderFromFilePath(string path)
		{
			Debug.Log("Attempting to load header from custom path...");
			EngineSaveGameHeader engineSaveGameHeader = null;
			try
			{
				engineSaveGameHeader = SaveGameUtilities.LoadHeader(path);
				if (engineSaveGameHeader.SaveVersion < EngineIO.MinCompatibleSaveVersion)
				{
					UIController.Instance.ShowMessageBox($"Cannot load the selected save file \"{Path.GetFileName(path)}\" as it was created in incompatible version {engineSaveGameHeader.SaveVersion}", MessageBoxButtons.Ok, null, MessageBoxIcon.Error, "Load save file");
					return null;
				}
				return engineSaveGameHeader;
			}
			catch (Exception innerException)
			{
				Debug.LogException(new Exception("Failed to read save game header at path \"" + path + "\"", innerException));
				UIController.Instance.ShowMessageBox("It was not possible to load the file \"" + Path.GetFileName(path) + "\". Please ensure that a valid save file was selected or contact " + GameController.Instance.SupportEmail, MessageBoxButtons.Ok, null, MessageBoxIcon.Error, "Load save file");
			}
			return null;
		}

		private static void TryImportAndThenLoadGameFromCustomPath(string path)
		{
			EngineSaveGameHeader saveGameHeaderFromFilePath = GetSaveGameHeaderFromFilePath(path);
			if (saveGameHeaderFromFilePath != null)
			{
				try
				{
					GameController.Instance.ScenarioLoader.TryLoadScenario(saveGameHeaderFromFilePath);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
					UIController.Instance.ShowMessageBox("An error occured loading the saved game. Please contact " + GameController.Instance.SupportEmail, MessageBoxButtons.Ok, null, MessageBoxIcon.Error, "Load save file");
				}
			}
		}
	}
}
