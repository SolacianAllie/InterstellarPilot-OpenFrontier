using System;
using System.Globalization;
using System.IO;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.SavedGames.V2.Model;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Readers
{
	public class HeaderReader1720 : ISavedGameHeaderReader
	{
		public ISavedGameHeader Read(BinaryReader reader)
		{
			ModelHeader modelHeader = new ModelHeader();
			modelHeader.Version = reader.ReadVersion();
			modelHeader.IsAutoSave = reader.ReadBoolean();
			modelHeader.TimeStamp = DateTime.ParseExact(reader.ReadString(), "yyyy-MM-dd HH-mm-ss", new CultureInfo("en-GB"));
			modelHeader.ScenarioInfoId = reader.ReadInt32();
			modelHeader.GlobalSaveNumber = reader.ReadInt32();
			modelHeader.SaveNumber = reader.ReadInt32();
			modelHeader.HavePlayer = reader.ReadBoolean();
			if (modelHeader.HavePlayer)
			{
				modelHeader.PlayerSectorName = reader.ReadString();
				modelHeader.PlayerName = reader.ReadString();
				modelHeader.Credits = reader.ReadInt32();
			}
			return modelHeader;
		}

		public static Version ReadVersionOnly(BinaryReader reader)
		{
			return reader.ReadVersion();
		}
	}
}
