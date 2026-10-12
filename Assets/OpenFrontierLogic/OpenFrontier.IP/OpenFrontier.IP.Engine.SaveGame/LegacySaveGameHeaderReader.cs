using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace OpenFrontier.IP.Engine.SaveGame
{
	public static class LegacySaveGameHeaderReader
	{
		public static bool ReadHeader162(BinaryReader reader, EngineSaveGameHeader header)
		{
			try
			{
				header.SaveVersion = reader.ReadVersion();
				header.IsAutoSave = reader.ReadBoolean();
				string s = reader.ReadString();
				header.TimeStamp = DateTime.ParseExact(s, SaveGameUtilities.HeaderDateFormat, CultureInfo.CurrentCulture.DateTimeFormat);
				header.ScenarioInfoId = reader.ReadInt32();
				header.GlobalSaveNumber = reader.ReadInt32();
				header.SaveNumber = reader.ReadInt32();
				if (reader.ReadBoolean())
				{
					header.SceneName = reader.ReadString();
					header.PilotName = reader.ReadString();
					header.Credits = reader.ReadInt32();
				}
				return true;
			}
			catch (Exception)
			{
				Debug.LogWarning("Failed to read header");
			}
			return false;
		}

		public static bool ReadHeader160(BinaryReader reader, EngineSaveGameHeader header)
		{
			try
			{
				header.SaveVersion = reader.ReadVersion();
				header.IsAutoSave = reader.ReadBoolean();
				double d = reader.ReadDouble();
				header.TimeStamp = DateTime.FromOADate(d);
				header.ScenarioInfoId = reader.ReadInt32();
				header.GlobalSaveNumber = reader.ReadInt32();
				header.SaveNumber = reader.ReadInt32();
				if (reader.ReadBoolean())
				{
					header.SceneName = reader.ReadString();
					header.PilotName = reader.ReadString();
					header.Credits = reader.ReadInt32();
				}
				return true;
			}
			catch (Exception)
			{
				Debug.LogWarning("Failed to read header");
			}
			return false;
		}
	}
}
