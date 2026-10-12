using System;
using OpenFrontier.IP.Common;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelHeader : ISavedGameHeader
	{
		public Version Version { get; set; }

		public Version CreatedVersion { get; set; }

		public bool IsAutoSave { get; set; }

		public DateTime TimeStamp { get; set; }

		public int ScenarioInfoId { get; set; }

		public int GlobalSaveNumber { get; set; }

		public int SaveNumber { get; set; }

		public bool HavePlayer { get; set; }

		public string PlayerSectorName { get; set; }

		public string PlayerName { get; set; }

		public long Credits { get; set; }

		public long NetWorth { get; set; }

		public string FactionName { get; set; }

		public bool Permadeath { get; set; }

		public DateTime GameStartDate { get; set; }

		public double SecondsElapsed { get; set; }

		public string ScenarioTitle { get; set; }

		public string ScenarioAuthor { get; set; }

		public string ScenarioAuthoringTool { get; set; }

		public string ScenarioDescription { get; set; }
	}
}
