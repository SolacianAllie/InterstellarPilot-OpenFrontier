using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class ScenarioInfo : MonoBehaviour
	{
		public bool ComingSoon;

		public int DateDay = 1;

		public int DateMonth = 1;

		public int DateYear = 2236;

		public int DateMinute;

		public int DateHour;

		[TextArea(3, 6)]
		public string Description;

		public Sector InitialScene;

		public bool IsTutorial;

		[TextArea(3, 6)]
		public string MissionLog;

		public string Objectives;

		public Faction PilotFaction;

		public string PilotName = "Unknown";

		public List<ScenarioInfo> Prereqs = new List<ScenarioInfo>();

		public string SectorName;

		public bool ShowCompletionData = true;

		public bool ShowCompletionDifficultyLevels = true;

		public bool ShowInListWhenLocked = true;

		public bool ShowInScenarioUI = true;

		public string TargetSceneName;

		public string Title;

		public int UniqueId = -1;

		public string GetPlayerPrefsKey()
		{
			return $"scenario_{UniqueId}";
		}

		public string GetPlayerPrefsCompleteCountKey()
		{
			return $"scenario_complete_count_{UniqueId}";
		}

		public void SetCompleted(int completionCombatDifficultyLevel)
		{
			int num = PlayerPrefs.GetInt(GetPlayerPrefsCompleteCountKey(), 0);
			num++;
			PlayerPrefs.SetInt(GetPlayerPrefsCompleteCountKey(), num);
			int completionDifficultyLevel = GetCompletionDifficultyLevel();
			if (completionCombatDifficultyLevel > completionDifficultyLevel)
			{
				PlayerPrefs.SetInt(GetPlayerPrefsKey(), completionCombatDifficultyLevel);
			}
			PlayerPrefs.Save();
		}

		public int GetCompletedCount()
		{
			return PlayerPrefs.GetInt(GetPlayerPrefsCompleteCountKey(), 0);
		}

		public bool GetIsUnlocked(out ScenarioInfo uncompletedScenario)
		{
			uncompletedScenario = null;
			if (GameController.Instance.IgnoreScenarioPrereqs)
			{
				return true;
			}
			for (int i = 0; i < Prereqs.Count; i++)
			{
				ScenarioInfo scenarioInfo = Prereqs[i];
				if (scenarioInfo != null && !scenarioInfo.IsCompleted())
				{
					uncompletedScenario = scenarioInfo;
					return false;
				}
			}
			return true;
		}

		public bool GetIsUnlocked()
		{
			if (Debug.isDebugBuild && GameController.Instance.AllScenariosUnlocked)
			{
				return true;
			}
			ScenarioInfo uncompletedScenario = null;
			return GetIsUnlocked(out uncompletedScenario);
		}

		public bool IsCompleted()
		{
			int completionDifficultyLevel = -1;
			return IsCompleted(out completionDifficultyLevel);
		}

		public int GetCompletionDifficultyLevel()
		{
			return PlayerPrefs.GetInt(GetPlayerPrefsKey(), -1);
		}

		public bool IsCompleted(out int completionDifficultyLevel)
		{
			completionDifficultyLevel = GetCompletionDifficultyLevel();
			return completionDifficultyLevel > -1;
		}

		public DateTime GetScenarioStartDateTime()
		{
			return new DateTime(DateYear, DateMonth, DateDay, DateHour, DateMinute, 0);
		}
	}
}
