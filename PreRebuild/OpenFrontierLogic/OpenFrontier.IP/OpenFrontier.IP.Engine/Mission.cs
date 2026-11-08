using System.Collections.Generic;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.Comms;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.MissionObjectives;
using OpenFrontier.IP.UI;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class Mission : MonoBehaviour, IUnique
	{
		public bool BroadcastMessages = true;

		public bool IsPrimary;

		public string Title;

		public float CompletionOpinionChange;

		private bool completionSuccess;

		public MissionStage CurrentStage;

		private EngineASX engine;

		public Faction MissionGiverFaction;

		private Faction ownerFaction;

		public float FailureOpinionChange;

		private bool isFinished;

		public int MissionRewardCredits;

		public List<MissionObjective> Objectives = new List<MissionObjective>();

		public List<MissionStage> Stages = new List<MissionStage>();

		public bool ShowInJournal = true;

		private double startTime;

		private float lastChangedStateTime;

		[SerializeField]
		private int uniqueId = -1;

		private static HashSet<int> addedUnitsFromObjectives = new HashSet<int>(8);

		public virtual MissionType MissionType => MissionType.Custom;

		public bool DynamicMission => MissionType != MissionType.Custom;

		public EngineASX Engine
		{
			get
			{
				return engine;
			}
			private set
			{
				if (!(engine != value))
				{
					return;
				}
				EngineASX engineASX = engine;
				engine = value;
				if (engineASX != null)
				{
					engineASX.DeregisterMission(this);
				}
				if (engine != null)
				{
					if (UniqueId < 0)
					{
						UniqueId = engine.GetUniqueMissionId();
					}
					engine.RegisterMission(this);
				}
			}
		}

		public virtual bool ManualCompletionEnabled => false;

		public bool IsFinished
		{
			get
			{
				return isFinished;
			}
			set
			{
				isFinished = value;
			}
		}

		public double StartTime
		{
			get
			{
				return startTime;
			}
			set
			{
				startTime = value;
			}
		}

		public bool CompletionSuccess
		{
			get
			{
				return completionSuccess;
			}
			set
			{
				completionSuccess = value;
			}
		}

		public WorldBase World
		{
			get
			{
				if (engine != null)
				{
					return engine.World;
				}
				return null;
			}
		}

		public int UniqueId
		{
			get
			{
				return uniqueId;
			}
			set
			{
				uniqueId = value;
			}
		}

		public virtual bool AllowAbortMission => DynamicMission;

		public float LastChangedStateTime => lastChangedStateTime;

		public virtual bool IsValid => engine != null;

		public Faction OwnerFaction
		{
			get
			{
				return ownerFaction;
			}
			set
			{
				ownerFaction = value;
			}
		}

		public void Init()
		{
			FindEngine();
			if (Objectives == null)
			{
				Objectives = new List<MissionObjective>();
			}
			if (Stages == null)
			{
				Stages = new List<MissionStage>();
			}
			OnInit();
		}

		public void RecordStartTime()
		{
			startTime = engine.ScenarioElapsedTime;
		}

		public void InitObjectives()
		{
			foreach (MissionObjective objective in Objectives)
			{
				if (objective == null)
				{
					Debug.LogError($"{this}: Mission contains a null objective", this);
				}
				else
				{
					objective.Init(this);
				}
			}
		}

		protected virtual void OnInit()
		{
		}

		public void FindEngine()
		{
			Engine = EngineASX.Instance;
		}

		public void UpdateObjectiveState()
		{
			if (isFinished || !World.CanCompleteMission(this))
			{
				return;
			}
			int num = 0;
			for (int i = 0; i < Objectives.Count; i++)
			{
				if (!Objectives[i].IsComplete)
				{
					num++;
				}
			}
			if (!isFinished && num == 0)
			{
				Finish(accomplished: true);
			}
		}

		public void OnObjectiveComplete(MissionObjective objective, bool success)
		{
			RegisterStateChange();
			World.OnMissionObjectiveComplete(this, objective, success);
			if (!success && !objective.IsOptional)
			{
				Finish(accomplished: false);
			}
		}

		[ContextMenu("ForceAccomplished")]
		public void ForceAccomplished()
		{
			Finish(accomplished: true);
		}

		[ContextMenu("ForceFailed")]
		public void ForceFailed()
		{
			Finish(accomplished: false);
		}

		public void Finish(bool accomplished)
		{
			OnFinishing(accomplished);
			RegisterStateChange();
			completionSuccess = accomplished;
			isFinished = true;
			gameObject.SetActive(value: false);
			World.OnMissionComplete(this, completionSuccess);
			DialogBase[] componentsInChildren = GetComponentsInChildren<DialogBase>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].AllowDialogToShow = false;
			}
			if (IsGuidanceActive())
			{
				engine.LocalPlayer.ActiveMission = null;
			}
			if (completionSuccess)
			{
				if (CompletionOpinionChange != 0f)
				{
					MissionGiverFaction.ChangeOpinion(engine.LocalPlayer.Faction, CompletionOpinionChange);
				}
			}
			else if (FailureOpinionChange != 0f)
			{
				MissionGiverFaction.ChangeOpinion(engine.LocalPlayer.Faction, FailureOpinionChange);
			}
			if (DynamicMission)
			{
				CleanDestroy();
			}
		}

		protected virtual void OnFinishing(bool accomplished)
		{
		}

		public virtual bool CanPlayerManualComplete()
		{
			return false;
		}

		public virtual void ManualComplete()
		{
			Finish(accomplished: true);
		}

		public void ChangeStage(MissionStage stage)
		{
			MissionStage currentStage = CurrentStage;
			CurrentStage = stage;
			if (currentStage != null)
			{
				currentStage.gameObject.SetActive(value: false);
			}
			if (CurrentStage != null)
			{
				CurrentStage.gameObject.SetActive(value: true);
				if (CurrentStage.CompletesMission && !isFinished && World.CanCompleteMission(this))
				{
					Finish(CurrentStage.MissionSuccess);
				}
			}
		}

		public void MakeActive()
		{
			if (gameObject.activeSelf)
			{
				Debug.LogWarning("Trying to activate a mission that is already active", this);
				return;
			}
			RecordStartTime();
			ShowInJournal = true;
			gameObject.SetActive(value: true);
			if (Engine.GameSettings.ShowMissionMsgs && BroadcastMessages)
			{
				string text = "Mission Started";
				string text2 = CalculateTitle();
				if (!string.IsNullOrEmpty(text2))
				{
					text += $" \"{text2}\"";
				}
				UIController.Instance.QuickMsg.AddMessage(text, 0f, new DocKMenuRequestData
				{
					DockMenu = DockMenuType.MissionUI,
					RelatedObject = this
				});
			}
		}

		public virtual string CalculateTitle()
		{
			return Title;
		}

		public virtual void AddWaypointsToList(List<PlayerWaypoint> waypoints)
		{
			AddWaypointsFromObjectives(waypoints);
		}

		private void AddWaypointsFromObjectives(List<PlayerWaypoint> waypoints)
		{
			addedUnitsFromObjectives.Clear();
			foreach (MissionObjective objective in Objectives)
			{
				if (objective != null && !objective.IsComplete && objective.gameObject.activeSelf)
				{
					AddWaypointsFromObjective(waypoints, objective);
				}
			}
		}

		private static void AddWaypointsFromObjective(List<PlayerWaypoint> waypoints, MissionObjective objective)
		{
			foreach (SectorTarget relatedSceneTarget in objective.RelatedSceneTargets)
			{
				if (!(relatedSceneTarget.GetTargetSector() != null))
				{
					continue;
				}
				if (relatedSceneTarget.TargetUnit != null)
				{
					if (!addedUnitsFromObjectives.Contains(relatedSceneTarget.TargetUnit.UniqueId) && relatedSceneTarget.TargetUnit.IsValidAndNotDestroyed)
					{
						PlayerWaypoint item = PlayerWaypoint.FromUnit(relatedSceneTarget.TargetUnit);
						waypoints.Add(item);
						addedUnitsFromObjectives.Add(relatedSceneTarget.TargetUnit.UniqueId);
					}
				}
				else if (relatedSceneTarget.TargetFleet != null)
				{
					foreach (NpcPilot npcPilot in relatedSceneTarget.TargetFleet.NpcPilots)
					{
						if (npcPilot != null)
						{
							Unit currentUnit = npcPilot.CurrentUnit;
							if (currentUnit != null && currentUnit.IsValidAndNotDestroyed && !addedUnitsFromObjectives.Contains(currentUnit.UniqueId))
							{
								PlayerWaypoint item2 = PlayerWaypoint.FromUnit(currentUnit);
								waypoints.Add(item2);
								addedUnitsFromObjectives.Add(currentUnit.UniqueId);
							}
						}
					}
				}
				else
				{
					PlayerWaypoint item3 = PlayerWaypoint.FromSectorPosition(relatedSceneTarget.GetTargetSector(), relatedSceneTarget.GetTargetSectorPosition());
					waypoints.Add(item3);
				}
			}
		}

		public bool IsGuidanceActive()
		{
			if (!IsValid)
			{
				return false;
			}
			return engine.LocalPlayer.ActiveMission == this;
		}

		public void MakeActiveMission()
		{
			engine.LocalPlayer.ActiveMission = this;
		}

		public virtual void GetMissionOptions(List<MissionOption> options)
		{
		}

		public virtual string GetMissionObjectiveText(MissionObjective objective)
		{
			if (!string.IsNullOrEmpty(objective.Description))
			{
				return objective.Description;
			}
			return objective.Title;
		}

		[ContextMenu("Abort Mission")]
		public void AbortMission()
		{
			Finish(accomplished: false);
			OnAbortedMission();
			if (MissionGiverFaction != null && CompletionOpinionChange != 0f)
			{
				MissionGiverFaction.ChangeOpinion(EngineASX.Instance.LocalFaction, (0f - CompletionOpinionChange) * GameController.Instance.GameSettings.AbortMissionPenaltyMultiplier);
			}
		}

		protected virtual void update()
		{
		}

		protected virtual void OnAbortedMission()
		{
		}

		private void OnDestroy()
		{
			Engine = null;
		}

		private void Update()
		{
			if (EngineASX.LoadedAndReady && !AbortIfInvalid())
			{
				update();
			}
		}

		public void RegisterStateChange()
		{
			lastChangedStateTime = Time.time;
		}

		public void CleanDestroy()
		{
			foreach (MissionObjective objective in Objectives)
			{
				objective.SafeDestroy();
			}
			Engine = null;
			Object.Destroy(gameObject);
		}

		public bool AbortIfInvalid()
		{
			if (!isFinished && !IsValid)
			{
				AbortMission();
				return true;
			}
			return false;
		}

		public void AutoNameGameObject()
		{
			name = $"Mission_{uniqueId}";
		}
	}
}
