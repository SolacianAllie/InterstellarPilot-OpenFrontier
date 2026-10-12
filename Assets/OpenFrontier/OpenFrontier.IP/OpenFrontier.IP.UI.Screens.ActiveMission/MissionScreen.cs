using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.ActiveMission
{
	public class MissionScreen : EngineScreen
	{
		public Text RewardText;

		public Button AbortMissionButton;

		public Text ActivateGuidanceButtonLabel;

		public Toggle ActivateGuidanceToggle;

		public Text ActiveGuidanceLabel;

		public Button CompleteMissionButton;

		public Text FactionLabel;

		public Mission Mission;

		public GameObject MissionOptionButtonsContainer;

		public Button MissionOptionsButtonPrefab;

		public MissionObjectivesGrid ObjectivesGrid;

		public Text StageLabel;

		public Text TitleLabel;

		public MissionOptionsController MissionOptionsController;

		private float lastMissionStateChangeTime;

		protected override void awake()
		{
			base.awake();
			CompleteMissionButton.onClick.AddListener(CompleteMissionButton_Activated);
			ActivateGuidanceToggle.onValueChanged.AddListener(ActivateGuidanceToggleChanged);
			AbortMissionButton.onClick.AddListener(AbortMissionButton_Activated);
		}

		protected override void update()
		{
			base.update();
			if (IsCurrentScreen && (Mission == null || !Mission.IsValid))
			{
				NavigateBack();
			}
			else if (Mission != null && Mission.LastChangedStateTime > lastMissionStateChangeTime)
			{
				Refresh();
				RefreshMissionStateChangeTime();
			}
		}

		public void RefreshMissionStateChangeTime()
		{
			if (Mission != null)
			{
				lastMissionStateChangeTime = Mission.LastChangedStateTime;
			}
		}

		protected override void refresh()
		{
			base.refresh();
			MissionOptionsController.Mission = Mission;
			if (Mission != null)
			{
				CompleteMissionButton.interactable = Mission.ManualCompletionEnabled && Mission.CanPlayerManualComplete();
				ActivateGuidanceToggle.isOn = Mission.IsGuidanceActive();
				ActivateGuidanceButtonLabel.text = (Mission.IsGuidanceActive() ? "Guidance On" : "Guidance Off");
				if (Mission.CurrentStage != null)
				{
					StageLabel.text = Mission.CurrentStage.JournalEntry;
				}
				else
				{
					StageLabel.text = null;
				}
				TitleLabel.text = Mission.CalculateTitle();
				ObjectivesGrid.Mission = Mission;
				ObjectivesGrid.Refresh();
				FactionLabel.text = ((Mission.MissionGiverFaction != null) ? Mission.MissionGiverFaction.Name : string.Empty);
				ActiveGuidanceLabel.gameObject.SetActive(Mission.IsGuidanceActive());
				AbortMissionButton.interactable = Mission.AllowAbortMission;
				RewardText.text = ((Mission.MissionRewardCredits > 0) ? TextFormattingHelper.FormatCredits(Mission.MissionRewardCredits, includeSuffix: true) : "-");
			}
			MissionOptionsController.Refresh();
		}

		private void AbortMissionButton_Activated()
		{
			Mission.AbortMission();
			NavigateAwayWhenMissionInvalid();
		}

		private void ActivateGuidanceToggleChanged(bool on)
		{
			if (on)
			{
				Eng.LocalPlayer.ActiveMission = Mission;
			}
			else
			{
				Eng.LocalPlayer.ActiveMission = null;
			}
			Eng.LocalPlayer.WaypointController.UpdateMissionPathWaypoints();
			Eng.LocalPlayer.WaypointController.RefreshActiveMissionPaths();
			Refresh();
		}

		private void CompleteMissionButton_Activated()
		{
			if (Mission.CanPlayerManualComplete())
			{
				Mission.ManualComplete();
				NavigateAwayWhenMissionInvalid();
			}
		}

		private void NavigateAwayWhenMissionInvalid()
		{
			Eng.SetUIFromPlayerStatus();
		}

		private bool ShouldShowCompleteMissionButton()
		{
			if (!Mission.IsFinished)
			{
				return Mission.ManualCompletionEnabled;
			}
			return false;
		}
	}
}
