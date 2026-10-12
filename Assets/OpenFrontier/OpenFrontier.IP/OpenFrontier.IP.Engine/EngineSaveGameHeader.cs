using System;
using OpenFrontier.IP.Engine.SaveGame;

namespace OpenFrontier.IP.Engine
{
	public class EngineSaveGameHeader
	{
		public long Credits;

		public long NetWorth;

		public string FullPath;

		public long FileSize;

		public int GlobalSaveNumber;

		public bool IsAutoSave;

		public string PilotName;

		public string FactionName;

		public int SaveNumber;

		public Version SaveVersion;

		public Version CreatedVersion;

		public int ScenarioInfoId = -1;

		public string SceneName;

		public DateTime TimeStamp;

		public bool Permadeath;

		public double SecondsElapsed;

		public DateTime GameStartDate;

		public string ScenarioTitle;

		public string ScenarioAuthor;

		public string ScenarioAuthoringTool;

		public string ScenarioDescription;

		public bool IsOlderVersion => SaveVersion < EngineIO.SaveVersion;

		public bool IsCurrentVersion => SaveVersion == EngineIO.SaveVersion;

		public bool CanBeLoaded => EngineIO.IsSaveVersionCompatible(SaveVersion);
	}
}
