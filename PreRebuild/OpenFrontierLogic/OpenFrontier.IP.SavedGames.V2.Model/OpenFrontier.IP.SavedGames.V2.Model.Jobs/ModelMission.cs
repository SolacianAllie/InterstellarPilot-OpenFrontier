using System.Collections.Generic;
using OpenFrontier.IP.Common;

namespace OpenFrontier.IP.SavedGames.V2.Model.Jobs
{
	public class ModelMission
	{
		public virtual MissionType MissionType { get; }

		public int Id { get; set; }

		public string Title { get; set; }

		public bool IsPrimary { get; set; }

		public bool NotificationsEnabled { get; set; } = true;

		public int StageIndex { get; set; }

		public bool IsActive { get; set; }

		public bool IsFinished { get; set; }

		public bool CompletionSuccess { get; set; }

		public bool ShowInJournal { get; set; }

		public ModelFaction OwnerFaction { get; set; }

		public ModelFaction MissionGiverFaction { get; set; }

		public float CompletionOpinionChange { get; set; }

		public float FailureOpinionChange { get; set; }

		public double StartTime { get; set; }

		public int RewardCredits { get; set; }

		public List<ModelMissionObjective> Objectives { get; set; } = new List<ModelMissionObjective>();

		public List<ModelMissionStage> Stages { get; set; } = new List<ModelMissionStage>();
	}
}
