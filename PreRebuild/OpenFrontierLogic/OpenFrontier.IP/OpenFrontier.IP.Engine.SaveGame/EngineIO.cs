using System;
using System.IO;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.SavedGames.V2.BinarySerialization.Readers;
using OpenFrontier.IP.SavedGames.V2.BinarySerialization.Writers;
using OpenFrontier.IP.SavedGames.V2.Model;
using UnityEngine;

namespace OpenFrontier.IP.Engine.SaveGame
{
	public static class EngineIO
	{
		public static Version SaveVersion = new Version(2, 0, 43);

		public static Version MinCompatibleSaveVersion = new Version(1, 7, 21);

		public static void AutoSave(EngineASX engine)
		{
			TryDeletePreviousSaveFileWhenPermadeath(engine);
			string errorMessage;
			if (engine.World.ScenarioOptions.Permadeath)
			{
				string saveFileName = engine.GetSaveFileName();
				SaveWithValidation(engine, saveFileName, isAutoSave: false, out errorMessage);
				return;
			}
			int num = PlayerPrefs.GetInt("autosave_num", 0);
			string saveFileName2 = "AutoSave" + num;
			SaveWithValidation(engine, saveFileName2, isAutoSave: true, out errorMessage);
			num++;
			PlayerPrefs.SetInt("autosave_num", num % GameController.Instance.GameSettings.NumAutosaves);
		}

		public static bool Save(EngineASX engine, out string errorMessage)
		{
			TryDeletePreviousSaveFileWhenPermadeath(engine);
			engine.World.SaveGameCount++;
			string saveFileName = engine.GetSaveFileName();
			return SaveWithValidation(engine, saveFileName, isAutoSave: false, out errorMessage);
		}

		private static void Save(EngineASX engine, string saveFileName, bool isAutoSave, out string savePath)
		{
			savePath = null;
			engine.World.CanLoadLastSave = true;
			CreateSaveDirectoryIfRequired();
			savePath = SaveGameUtilities.GetSavePath(saveFileName, !isAutoSave);
			if (File.Exists(savePath))
			{
				SaveGameUtilities.BackupSaveFile(savePath);
			}
			Debug.Log("Start save game to: " + savePath);
			engine.World.LastLoadedData.FullSaveGamePath = savePath;
			SaveGameModelExporter saveGameModelExporter = new SaveGameModelExporter();
			SavedGame savedGame = (SavedGame)saveGameModelExporter.Export(EngineASX.Instance);
			savedGame.Header = saveGameModelExporter.ExportHeader(EngineASX.Instance, SaveVersion, isAutoSave);
			SaveGameWriter saveGameWriter = new SaveGameWriter(new HeaderWriter());
			using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(savePath)))
			{
				saveGameWriter.Write(writer, savedGame);
			}
			Debug.Log("Saving complete...");
		}

		private static void CreateSaveDirectoryIfRequired()
		{
			string saveDirectory = SaveGameUtilities.GetSaveDirectory();
			if (!Directory.Exists(saveDirectory))
			{
				Directory.CreateDirectory(saveDirectory);
			}
		}

		public static bool TryCreateSaveDirectoryIfRequired()
		{
			try
			{
				string saveDirectory = SaveGameUtilities.GetSaveDirectory();
				if (!Directory.Exists(saveDirectory))
				{
					Directory.CreateDirectory(saveDirectory);
				}
				return true;
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				return false;
			}
		}

		public static void TryDeletePreviousSaveFileWhenPermadeath(EngineASX engine)
		{
			if (engine.World.ScenarioOptions.Permadeath && !string.IsNullOrWhiteSpace(engine.World.LastLoadedData.FullSaveGamePath))
			{
				try
				{
					File.Delete(engine.World.LastLoadedData.FullSaveGamePath);
					engine.World.LastLoadedData.FullSaveGamePath = null;
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
		}

		public static bool Validate(EngineASX engine, out string errorMessage)
		{
			errorMessage = null;
			if (engine.World != null)
			{
				if (engine.World.ScenarioInfo == null)
				{
					errorMessage = "Cannot save without world scenario info";
					return false;
				}
				return true;
			}
			errorMessage = "No world assigned";
			return false;
		}

		public static bool SaveWithValidation(EngineASX engine, string saveFileName, bool isAutoSave, out string errorMessage)
		{
			errorMessage = null;
			if (Validate(engine, out var errorMessage2))
			{
				string savePath = null;
				try
				{
					Save(engine, saveFileName, isAutoSave, out savePath);
					return true;
				}
				catch (Exception ex)
				{
					if (ex is IOException)
					{
						errorMessage = "An I/O exception occured. Please ensure there is enough disk space available or retry. Contact " + GameController.Instance.SupportEmail + " for further assistance.";
					}
					Debug.LogException(new Exception("Cannot save game. Exception occured", ex));
					if (!string.IsNullOrWhiteSpace(savePath))
					{
						try
						{
							if (File.Exists(savePath))
							{
								File.Delete(savePath);
							}
						}
						catch (Exception exception)
						{
							Debug.LogException(exception);
						}
					}
				}
				return false;
			}
			errorMessage = "An internal error occured. Please contact " + GameController.Instance.SupportEmail + " for further assistance.";
			Debug.LogError("Cannot save game. Validation failed: " + errorMessage2);
			return false;
		}

		public static bool IsSaveVersionCompatible(Version version)
		{
			if (version >= MinCompatibleSaveVersion)
			{
				return version <= SaveVersion;
			}
			return false;
		}

		private static ISaveGameReader GetSaveGameReader(Version version)
		{
			if (version >= MinCompatibleSaveVersion && version <= SaveVersion)
			{
				if (version < SaveVersion)
				{
					return GetBackwardsCompatibleReader(version);
				}
				return new SaveGameReader();
			}
			return null;
		}

		private static ISaveGameImporter GetSaveGameImporter(Version version)
		{
			if (version >= MinCompatibleSaveVersion && version <= SaveVersion)
			{
				if (version < SaveVersion)
				{
					return GetBackwardsCompatibleImporter(version);
				}
				return new SaveGameModelImporter170();
			}
			return null;
		}

		private static ISaveGameImporter GetBackwardsCompatibleImporter(Version version)
		{
			if (version >= new Version(1, 7, 5))
			{
				return new SaveGameModelImporter170();
			}
			return null;
		}

		private static ISaveGameReader GetBackwardsCompatibleReader(Version version)
		{
			if (version >= new Version(2, 0, 18))
			{
				return new SaveGameReader2019();
			}
			if (version >= new Version(2, 0, 17))
			{
				return new SaveGameReader2017();
			}
			if (version >= new Version(2, 0, 11))
			{
				return new SaveGameReader2016();
			}
			if (version >= new Version(2, 0, 4))
			{
				return new SaveGameReader2004();
			}
			if (version >= new Version(2, 0, 3))
			{
				return new SaveGameReader2003();
			}
			if (version >= new Version(1, 7, 30))
			{
				return new SaveGameReader2000();
			}
			if (version >= new Version(1, 7, 26))
			{
				return new SaveGameReader1726();
			}
			if (version >= new Version(1, 7, 25))
			{
				return new SaveGameReader1725();
			}
			if (version >= new Version(1, 7, 21))
			{
				return new SaveGameReader1721();
			}
			return null;
		}

		public static void Load(string savePath, EngineASX engine)
		{
			Debug.Log($"Loading gamedata from \"{savePath}\"");
			using (BinaryReader binaryReader = new BinaryReader(File.OpenRead(savePath)))
			{
				Version version = ReadVersion(binaryReader);
				ISaveGameReader saveGameReader = GetSaveGameReader(version);
				if (saveGameReader == null)
				{
					throw new IOException("Unable to load save game. No compatible reader found");
				}
				binaryReader.BaseStream.Seek(0L, SeekOrigin.Begin);
				ISavedGame savedGame = saveGameReader.Read(binaryReader);
				(GetSaveGameImporter(version) ?? throw new IOException("Unable to load save game. No compatible reader found")).Import(savedGame, engine);
			}
			Debug.Log("Loading complete...");
		}

		public static Version ReadVersion(BinaryReader reader)
		{
			int major = reader.ReadInt32();
			int minor = reader.ReadInt32();
			int build = reader.ReadInt32();
			return new Version(major, minor, build);
		}
	}
}
