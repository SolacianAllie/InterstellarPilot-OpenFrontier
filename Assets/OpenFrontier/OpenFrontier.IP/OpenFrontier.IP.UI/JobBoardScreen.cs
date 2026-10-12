using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.MissionSpecs;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.MessageBox;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class JobBoardScreen : EngineScreen
	{
		public Button AcceptMissionButton;

		public Text DescriptionLabel;

		[FormerlySerializedAs("MissionSpecList")]
		public JobBoardList JobBoardList;

		public GameObject SelectedItemRoot;

		protected override void awake()
		{
			base.awake();
			AcceptMissionButton.onClick.AddListener(OnAcceptMissionButton_Activated);
			JobBoardList.SelectedItemChanged += OnJobBoardList_SelectedItemChanged;
		}

		protected override void update()
		{
			base.update();
			if (EngineASX.LoadedAndReady)
			{
				RefreshWhenJobsInvalid();
			}
		}

		private void RefreshWhenJobsInvalid()
		{
			if (AnyJobsInvalid())
			{
				Refresh();
			}
		}

		private bool AnyJobsInvalid()
		{
			foreach (MissionSpec activeItem in JobBoardList.ActiveItems)
			{
				if (activeItem == null || activeItem.Engine == null || !activeItem.IsValid())
				{
					return true;
				}
			}
			return false;
		}

		protected override void refresh()
		{
			base.refresh();
			JobBoardList.Refresh();
			RefreshButtons();
			RefreshDescription();
			SelectedItemRoot.gameObject.SetActive(JobBoardList.FirstSelectedItem != null);
		}

		private void Engine_MissionSpecsChanged(EngineASX sender)
		{
			Refresh();
		}

		private void OnJobBoardList_SelectedItemChanged(ScrollList<MissionSpec> sender, MissionSpec oldItem, MissionSpec newItem)
		{
			if (newItem != null)
			{
				newItem.OnDisplayedToPlayer();
			}
			RefreshButtons();
			RefreshDescription();
			SelectedItemRoot.gameObject.SetActive(JobBoardList.FirstSelectedItem != null);
		}

		private void RefreshButtons()
		{
			AcceptMissionButton.gameObject.SetActive(JobBoardList.FirstSelectedItem != null);
			AcceptMissionButton.interactable = JobBoardList.FirstSelectedItem != null && JobBoardList.FirstSelectedItem.CanAcceptMission();
		}

		private void RefreshDescription()
		{
			DescriptionLabel.text = ((JobBoardList.FirstSelectedItem != null && JobBoardList.FirstSelectedItem.IsValid()) ? JobBoardList.FirstSelectedItem.CalculateBrief() : string.Empty);
		}

		private void OnAcceptMissionButton_Activated()
		{
			if (!(JobBoardList.FirstSelectedItem != null))
			{
				return;
			}
			if (Eng.Missions.Count((Mission e) => !e.IsFinished && e.DynamicMission) < Eng.GameSettings.MaxDynamicMissions)
			{
				Mission mission = JobBoardList.FirstSelectedItem.CreateMission(EngineASX.Instance.LocalFaction);
				JobBoardList.FirstSelectedItem.SafeDestroy();
				UIController.Instance.QuickMsg.AddMessage("Mission accepted - " + mission.CalculateTitle());
				if (Eng.LocalPlayer.ActiveMission == null)
				{
					mission.MakeActiveMission();
				}
				Eng.SetUIFromPlayerStatus();
			}
			else
			{
				UIController.Instance.ShowMessageBox("Cannot accept anymore missions", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
			}
		}
	}
}
