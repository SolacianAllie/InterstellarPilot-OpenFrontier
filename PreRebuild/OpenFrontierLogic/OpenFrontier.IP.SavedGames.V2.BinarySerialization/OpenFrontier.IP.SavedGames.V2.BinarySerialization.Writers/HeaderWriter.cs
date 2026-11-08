using System;
using System.IO;
using OpenFrontier.IP.SavedGames.V2.Model;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Writers
{
	public class HeaderWriter
	{
		public void Write(BinaryWriter writer, ModelHeader header)
		{
			writer.WriteVersion(header.Version ?? new Version(1, 0, 0));
			writer.WriteVersion(header.CreatedVersion ?? new Version(1, 0, 0));
			writer.Write(header.IsAutoSave);
			writer.Write(DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));
			writer.Write(header.ScenarioInfoId);
			writer.Write(header.GlobalSaveNumber);
			writer.Write(header.SaveNumber);
			writer.Write(value: true);
			writer.WriteStringOrEmpty(header.PlayerSectorName);
			writer.WriteStringOrEmpty(header.PlayerName);
			writer.Write(header.Credits);
			writer.WriteStringOrEmpty(header.FactionName);
			writer.Write(header.NetWorth);
			writer.Write(header.Permadeath);
			writer.Write(header.SecondsElapsed);
			writer.Write(header.GameStartDate.ToString("yyyy-MM-dd HH-mm-ss"));
			writer.WriteStringOrEmpty(header.ScenarioTitle);
			writer.WriteStringOrEmpty(header.ScenarioAuthor);
			writer.WriteStringOrEmpty(header.ScenarioAuthoringTool);
			writer.WriteStringOrEmpty(header.ScenarioDescription);
		}
	}
}
