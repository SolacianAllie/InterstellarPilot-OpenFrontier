using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.ActiveMission
{
	public class MissionOptionsController : MonoBehaviour
	{
		private float lastMissionStateChangeTime;

		public GameObject MissionOptionButtonsContainer;

		public Button MissionOptionsButtonPrefab;

		private Mission mission;

		private List<MissionOption> currentOptions = new List<MissionOption>();

		public Button CompleteMissionButton;

		public Mission Mission
		{
			get
			{
				return mission;
			}
			set
			{
				if (mission != value)
				{
					mission = value;
					Refresh();
				}
			}
		}

		public void Update()
		{
			if (Mission != null)
			{
				if (Mission.LastChangedStateTime > lastMissionStateChangeTime)
				{
					Refresh();
					RefreshMissionStateChangeTime();
				}
			}
			else if (currentOptions.Count > 0)
			{
				Refresh();
			}
		}

		public void RefreshMissionStateChangeTime()
		{
			if (Mission != null)
			{
				lastMissionStateChangeTime = Mission.LastChangedStateTime;
			}
		}

		public void Refresh()
		{
			Button[] componentsInChildren = MissionOptionButtonsContainer.GetComponentsInChildren<Button>(includeInactive: true);
			foreach (Button button in componentsInChildren)
			{
				if (button != CompleteMissionButton)
				{
					Object.DestroyImmediate(button.gameObject);
				}
			}
			if (mission != null)
			{
				currentOptions = new List<MissionOption>(8);
				Mission.GetMissionOptions(currentOptions);
				for (int j = 0; j < currentOptions.Count; j++)
				{
					MissionOption missionOption = currentOptions[j];
					AddMissionOptionButton(missionOption);
				}
			}
		}

		private void AddMissionOptionButton(MissionOption missionOption)
		{
			Button button = UnityObjectHelper.InstantiateAndGetComponent(MissionOptionsButtonPrefab);
			button.transform.SetParent(MissionOptionButtonsContainer.transform);
			button.GetComponentInChildren<Text>().text = missionOption.Title;
			button.onClick.AddListener(() =>
			{
				MissionOptionButtonActivated(missionOption);
			});
			button.transform.localScale = Vector3.one;
		}

		private void MissionOptionButtonActivated(MissionOption missionOption)
		{
			string methodName = missionOption.MethodName;
			Mission.SendMessage(methodName);
			Refresh();
		}
	}
}
