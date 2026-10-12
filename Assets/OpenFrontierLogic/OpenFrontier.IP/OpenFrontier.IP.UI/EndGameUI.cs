using UnityEngine;
using UnityEngine.UI;
using Input = OpenFrontier.LegacyInput;

namespace OpenFrontier.IP.UI
{
	public class EndGameUI : EngineScreen
	{
		private bool hasShownButtons;

		public GameObject ScenarioButtonsRoot;

		public Button ScenarioContinueButton;

		public Button ScenarioQuitButton;

		public Button ScenarioRetryButton;

		public Text ScenarioStatusLabel;

		public Button SkipFadeInButton;

		public GraphicColourLerp TextFadeIn;

		public void OnTextFadedIn()
		{
			TextFadeIn.Complete();
			ScenarioRetryButton.gameObject.SetActive(!Eng.World.ScenarioSuccess && !Eng.World.ScenarioOptions.Permadeath);
			hasShownButtons = true;
			ScenarioButtonsRoot.gameObject.SetActive(value: true);
			TextFadeIn.gameObject.SetActive(value: false);
		}

		protected override void awake()
		{
			base.awake();
			ScenarioButtonsRoot.gameObject.SetActive(value: false);
			ScenarioQuitButton.onClick.AddListener(ScenarioQuitButton_Pressed);
			ScenarioContinueButton.onClick.AddListener(ScenarioContinueButton_Pressed);
			ScenarioRetryButton.onClick.AddListener(ScenarioRetryButton_Activated);
		}

		protected override void refresh()
		{
			base.refresh();
			Eng.IsPaused = false;
			ScenarioContinueButton.gameObject.SetActive(Eng.World.ScenarioSuccess && Eng.World.Permissions.AllowContinueOnCompletion);
			if (Eng.World.ScenarioSuccess)
			{
				ScenarioStatusLabel.text = "Mission Accomplished";
			}
			else
			{
				ScenarioStatusLabel.text = "Mission Failed";
			}
		}

		protected override void update()
		{
			base.update();
			if (!hasShownButtons && (Input.GetMouseButtonDown(0) || TextFadeIn.IsComplete))
			{
				OnTextFadedIn();
			}
		}

		private void ScenarioContinueButton_Pressed()
		{
			Eng.World.NotifyPlayerRequireExit(continueScenario: true);
		}

		private void ScenarioQuitButton_Pressed()
		{
			Eng.World.NotifyPlayerRequireExit(continueScenario: false);
		}

		private void ScenarioRetryButton_Activated()
		{
			Eng.World.NotifyPlayerRequireRetry();
		}
	}
}
