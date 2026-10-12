using System;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class CheatButtonUI : MonoBehaviour
	{
		private int clickCount;

		public CheatMode Mode;

		private Button button;

		private float lastClickTime;

		private void Awake()
		{
			button = GetComponent<Button>();
			if (button != null)
			{
				button.onClick.AddListener(OnClick);
			}
			else
			{
				Debug.LogError("Missing button component", this);
			}
		}

		private void Update()
		{
			if (clickCount > 0 && Time.realtimeSinceStartup > lastClickTime + 3f)
			{
				ResetClicks();
			}
		}

		private void ResetClicks()
		{
			clickCount = 0;
			lastClickTime = 0f;
		}

		private void OnClick()
		{
			lastClickTime = Time.realtimeSinceStartup;
			clickCount++;
			if (clickCount >= 10)
			{
				ActivateCheat();
			}
		}

		private void ActivateCheat()
		{
			Debug.Log("Activating Cheat: " + Enum.GetName(typeof(CheatMode), Mode));
			switch (Mode)
			{
			case CheatMode.ScenarioUnlock:
			{
				ScenarioUI scenarioUI = UIController.Instance.ScreenNavigator?.CurrentScreen as ScenarioUI;
				GameController.Instance.IgnoreScenarioPrereqs = true;
				UIController.Instance.ShowMessageBox("Scenario Prerequisites ignored");
				if (scenarioUI != null)
				{
					scenarioUI.Refresh();
				}
				break;
			}
			case CheatMode.HundredMillionCredits:
			{
				EngineASX instance = EngineASX.Instance;
				if (instance != null && instance.LocalPlayer != null)
				{
					instance.AddCreditsToPlayerFactionWithMsg(100000000, FactionTransactionType.Gift);
				}
				break;
			}
			}
			ResetClicks();
		}
	}
}
