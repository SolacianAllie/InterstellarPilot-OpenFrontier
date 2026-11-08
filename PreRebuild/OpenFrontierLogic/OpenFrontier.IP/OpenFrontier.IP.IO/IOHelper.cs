using System;
using System.IO;
using OpenFrontier.IP.Engine.SaveGame;
using UnityEngine;

namespace OpenFrontier.IP.IO
{
	public class IOHelper
	{
		public static string[] ReadAndSplitTextLines(TextAsset t)
		{
			return t.text.Replace("\r\n", "\n").Split(new string[1] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
		}

		public static string GetBinaryFileType()
		{
			return NativeFilePicker.ConvertExtensionToFileType(SaveGameUtilities.Extension);
		}

		public static string MakeUniqueFileName(string writeLocation)
		{
			int num = 1;
			string directoryName = Path.GetDirectoryName(writeLocation);
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(writeLocation);
			string extension = Path.GetExtension(writeLocation);
			while (File.Exists(writeLocation))
			{
				writeLocation = Path.Combine(directoryName, $"{fileNameWithoutExtension}({num}){extension}");
				num++;
			}
			return writeLocation;
		}
	}
}
