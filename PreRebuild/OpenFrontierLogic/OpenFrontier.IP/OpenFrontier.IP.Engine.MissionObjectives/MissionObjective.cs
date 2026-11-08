using System.Collections.Generic;
using OpenFrontier.IP.UI;
using UnityEngine;

namespace OpenFrontier.IP.Engine.MissionObjectives
{
	public class MissionObjective : MonoBehaviour
	{
		public int UniqueId = -1;

		public string Description;

		public bool IsOptional;

		public int Order;

		public string Title;

		[SerializeField]
		private List<SectorTarget> relatedSceneTargets = new List<SectorTarget>();

		private EngineASX engine;

		public bool IsComplete;

		private Mission mission;

		private int objectiveIndex = -1;

		public bool ShowInJournal = true;

		public bool Success;

		private bool hasInit;

		public EngineASX Engine => engine;

		public Mission Mission => mission;

		public int ObjectiveIndex => objectiveIndex;

		public List<SectorTarget> RelatedSceneTargets => relatedSceneTargets;

		public void Init(Mission mission)
		{
			if (!hasInit)
			{
				engine = EngineASX.Instance;
				this.mission = mission;
				objectiveIndex = this.mission.Objectives.IndexOf(this);
				if (UniqueId < 0)
				{
					UniqueId = engine.GetUniqueMissionObjectiveId();
				}
				engine.RegisterMissionObjective(this);
			}
		}

		public void Complete(bool success)
		{
			if (!IsComplete)
			{
				IsComplete = true;
				MakeActive();
				Success = success;
				gameObject.SetActive(value: false);
				UnityObjectHelper.FindInParentsOrSelf<Mission>(gameObject).OnObjectiveComplete(this, Success);
			}
		}

		public void MakeActive()
		{
			if (!gameObject.activeSelf && !IsComplete)
			{
				OnNewActiveIncompleteObjective();
			}
			ShowInJournal = true;
			gameObject.SetActive(value: true);
		}

		private void OnNewActiveIncompleteObjective()
		{
			if (mission.BroadcastMessages && engine.GameSettings.ShowObjectiveMsgs)
			{
				string text = "New Objective";
				if (!string.IsNullOrEmpty(Title))
				{
					text += $" \"{Title}\"";
				}
				UIController.Instance.QuickMsg.AddMessage(text, 0f, new DocKMenuRequestData
				{
					DockMenu = DockMenuType.MissionUI,
					RelatedObject = mission
				});
			}
		}

		public void SafeDestroy()
		{
			if (engine != null)
			{
				engine.DeregisterMissionObjective(this);
				UniqueId = -1;
			}
		}

		public void AutoNameGameObject()
		{
			name = $"MissionObjective_{UniqueId}";
		}
	}
}
