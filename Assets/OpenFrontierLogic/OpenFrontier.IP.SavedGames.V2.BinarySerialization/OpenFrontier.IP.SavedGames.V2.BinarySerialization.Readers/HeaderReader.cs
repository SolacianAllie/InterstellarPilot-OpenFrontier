using System;
using System.Globalization;
using System.IO;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.SavedGames.V2.Model;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Readers
{
	public class HeaderReader : ISavedGameHeaderReader
	{
		public ISavedGameHeader Read(BinaryReader reader)
		{
			return new ModelHeader
			{
				Version = reader.ReadVersion(),
				CreatedVersion = reader.ReadVersion(),
				IsAutoSave = reader.ReadBoolean(),
				TimeStamp = DateTime.ParseExact(reader.ReadString(), "yyyy-MM-dd HH-mm-ss", new CultureInfo("en-GB")),
				ScenarioInfoId = reader.ReadInt32(),
				GlobalSaveNumber = reader.ReadInt32(),
				SaveNumber = reader.ReadInt32(),
				HavePlayer = reader.ReadBoolean(),
				PlayerSectorName = reader.ReadString(),
				PlayerName = reader.ReadString(),
				Credits = reader.ReadInt64(),
				FactionName = reader.ReadString(),
				NetWorth = reader.ReadInt64(),
				Permadeath = reader.ReadBoolean(),
				SecondsElapsed = reader.ReadDouble(),
				GameStartDate = DateTime.ParseExact(reader.ReadString(), "yyyy-MM-dd HH-mm-ss", new CultureInfo("en-GB")),
				ScenarioTitle = reader.ReadString(),
				ScenarioAuthor = reader.ReadString(),
				ScenarioAuthoringTool = reader.ReadString(),
				ScenarioDescription = reader.ReadString()
			};
		}

		public static Version ReadVersionOnly(BinaryReader reader)
		{
			return reader.ReadVersion();
		}
	}
}
