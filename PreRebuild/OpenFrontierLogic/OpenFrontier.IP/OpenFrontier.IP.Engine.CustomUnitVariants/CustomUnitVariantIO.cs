using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenFrontier.IP.UI;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.CreateUnitVariant;
using OpenFrontier.IP.UI.Screens.MessageBox;
using UnityEngine;

namespace OpenFrontier.IP.Engine.CustomUnitVariants
{
	public static class CustomUnitVariantIO
	{
		public static string LegacyExtension = "ipv";

		public static string Extension = "dat";

		private const string saveGamesPath = "CustomUnitVariants";

		public static string PrepareVariantForExport(string fileName)
		{
			if (Path.GetExtension(fileName) == "." + LegacyExtension)
			{
				string fullPath = GetFullPath(fileName);
				string text = Path.ChangeExtension(fileName, "." + Extension);
				string fullPath2 = GetFullPath(text);
				try
				{
					File.Move(fullPath, fullPath2);
					fileName = text;
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
			return fileName;
		}

		public static IEnumerable<CustomUnitVariant> LoadCustomVariantShips(bool ignoreInvalid = true)
		{
			IEnumerable<CustomUnitVariant> enumerable = from e in LoadCustomVariantsWithFileName()
				select e.CustomUnitVariant;
			if (ignoreInvalid)
			{
				enumerable = enumerable.Where((CustomUnitVariant e) => e.IsValid() && e.UnitClass.UnitType == UnitType.Ship);
			}
			return enumerable;
		}

		public static void DeleteCustomVariants(IEnumerable<string> fileNamesWithoutDirectory)
		{
			string saveDirectory = GetSaveDirectory();
			foreach (string item in fileNamesWithoutDirectory)
			{
				string path = Path.Combine(saveDirectory, item);
				if (File.Exists(path))
				{
					File.Delete(path);
				}
			}
		}

		public static string GetFullPath(string fileNameWithoutDirectory)
		{
			return Path.Combine(GetSaveDirectory(), fileNameWithoutDirectory);
		}

		public static bool DeleteCustomVariant(string fileNameWithoutDirectory)
		{
			string path = Path.Combine(GetSaveDirectory(), fileNameWithoutDirectory);
			if (File.Exists(path))
			{
				File.Delete(path);
				return true;
			}
			return false;
		}

		public static IEnumerable<ManageUnitVariantItemWrapper> LoadCustomVariantsWithFileName()
		{
			string saveDirectory = GetSaveDirectory();
			List<ManageUnitVariantItemWrapper> list = new List<ManageUnitVariantItemWrapper>();
			LoadAndAddVariants(saveDirectory, list, Extension);
			LoadAndAddVariants(saveDirectory, list, LegacyExtension);
			return list;
		}

		private static void LoadAndAddVariants(string saveDir, List<ManageUnitVariantItemWrapper> list, string extension)
		{
			if (!Directory.Exists(saveDir))
			{
				return;
			}
			string[] files = Directory.GetFiles(saveDir, "*." + extension);
			foreach (string text in files)
			{
				try
				{
					CustomUnitVariant customUnitVariant = LoadCustomVariant(text);
					list.Add(new ManageUnitVariantItemWrapper
					{
						CustomUnitVariant = customUnitVariant,
						FileNameWithoutDirectory = Path.GetFileName(text)
					});
				}
				catch (Exception ex)
				{
					Debug.LogError("Failed to load custom unit variant: " + ex.Message);
				}
			}
		}

		public static bool TryLoadValidCustomVariantOrError(string filePath, IEnumerable<CustomUnitVariant> existingVariants, out CustomUnitVariant customUnitVariant)
		{
			customUnitVariant = null;
			try
			{
				customUnitVariant = LoadCustomVariant(filePath);
				if (customUnitVariant != null)
				{
					if (!customUnitVariant.IsValid())
					{
						UIController.Instance.ShowMessageBox("The loaded unit variant could not be validated", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
						return false;
					}
					CustomUnitVariant c = customUnitVariant;
					if (existingVariants.Any((CustomUnitVariant e) => e.UnitClass == c.UnitClass && e.Name == c.Name))
					{
						UIController.Instance.ShowMessageBox("A custom unit variant already exists with the same name (" + c.FullName + ")", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
						return false;
					}
					return true;
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				UIController.Instance.ShowError("An error occured while attempting to load the file. Please ensure that a valid unit variant file is being loaded.");
			}
			return false;
		}

		public static CustomUnitVariant LoadCustomVariant(string filePath)
		{
			using BinaryReader reader = new BinaryReader(File.OpenRead(filePath));
			return new CustomUnitVariantReader().Read(reader);
		}

		public static string GetCustomVariantSavePath(CustomUnitVariant customUnitVariant)
		{
			return GetSavePath(customUnitVariant.FullName, Extension, ensureUnique: false);
		}

		public static bool CustomUnitVariantSaveFileExists(CustomUnitVariant customUnitVariant)
		{
			return File.Exists(GetCustomVariantSavePath(customUnitVariant));
		}

		public static void SaveCustomVariant(CustomUnitVariant customUnitVariant)
		{
			string fullName = customUnitVariant.FullName;
			TryCreateSaveDirectoryIfRequired();
			string savePath = GetSavePath(fullName, ensureUnique: false);
			if (File.Exists(savePath))
			{
				File.Delete(savePath);
			}
			SaveCustomVariantInternal(customUnitVariant, savePath);
		}

		public static bool TryCreateSaveDirectoryIfRequired()
		{
			try
			{
				string saveDirectory = GetSaveDirectory();
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

		private static void SaveCustomVariantInternal(CustomUnitVariant customUnitVariant, string savePath)
		{
			using BinaryWriter writer = new BinaryWriter(File.OpenWrite(savePath));
			new CustomUnitVariantWriter().Write(customUnitVariant, writer);
		}

		public static string GetSavePath(string saveFileName, bool ensureUnique)
		{
			return GetSavePath(saveFileName, Extension, ensureUnique);
		}

		public static string GetSavePath(string saveFileName, string extension, bool ensureUnique)
		{
			string path = saveFileName + "." + extension;
			string saveDirectory = GetSaveDirectory();
			if (ensureUnique && File.Exists(Path.Combine(saveDirectory, path)))
			{
				int num = 1;
				do
				{
					path = $"{saveFileName}({num}).{extension}";
					num++;
				}
				while (File.Exists(Path.Combine(saveDirectory, path)));
			}
			return Path.Combine(saveDirectory, path);
		}

		public static string GetSaveDirectory()
		{
			return Path.Combine(Application.persistentDataPath, "CustomUnitVariants");
		}
	}
}
