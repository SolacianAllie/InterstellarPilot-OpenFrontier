using System;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.SaveGame;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.GameMode;
using OpenFrontier.IP.UI.Screens.LoadGame;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.UI.Screens.Skirmish;
using OpenFrontier.IP.billing;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class MainMenuScreen : ScreenBase
	{
		public Button QuitButton;

		public Button TutorialsButton;

		public Button StoreButton;

		public Button NewsButton;

		public Button ContinueGameButton;

		private EngineSaveGameHeader firstSaveGameHeader;

		public Button LoadGameButton;

		public Button MultiplayerButton;

		/// <summary>
		/// The multiplayer screen prefab.
		///
		/// Typed as <see cref="ScreenBase"/> rather than the concrete
		/// multiplayer screen on purpose. The multiplayer assembly depends on
		/// this one, so naming its types here would make the two assemblies
		/// reference each other - which Unity forbids. Holding the prefab as a
		/// screen keeps the dependency pointing one way only.
		/// </summary>
		public ScreenBase MultiplayerScreenPrefab;

		public SkirmishSetupScreen SkirmishSetupUI;

		public Text VersionText;

		private const float canLoadGameRefreshInterval = 1f;

		private float lastCanLoadGameCheckTime;

		protected override void awake()
		{
			base.awake();
			StoreButton.gameObject.SetActive(Products.IAPEnabled);
			TutorialsButton.onClick.AddListener(TutorialsButtonClick);
			QuitButton.onClick.AddListener(() =>
			{
				Application.Quit();
			});
		}

		protected override void ApplyFadingState()
		{
			if (EngineASX.LoadedAndReady && TimeSinceLastEnabled > 0.4f)
			{
				base.ApplyFadingState();
			}
		}

		private void TutorialsButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowTutorialsScreen();
		}

		public void PromptForContinueGame()
		{
			UIController.Instance.ShowMessageBox("Continue last saved game?", MessageBoxButtons.OkCancel, MessageBoxScreen_Dismissed, MessageBoxIcon.Question);
		}

		public void Credits()
		{
			UIController.Instance.ScreenNavigator.NavigateToScreen<CreditsUI>();
		}

		public void LoadOptions()
		{
			UIController.Instance.ScreenNavigator.NavigateToScreen<OptionsScreen>();
		}

		public void NewGame()
		{
			UIController.Instance.ScreenNavigator.NavigateToScreen<GameModeScreen>();
		}

		public void LoadGame()
		{
			UIController.Instance.ScreenNavigator.NavigateToScreen<LoadGameScreen>();
		}

		/// <summary>
		/// Opens the multiplayer screen, instantiating it from
		/// <see cref="MultiplayerScreenPrefab"/>.
		///
		/// Instantiated here rather than looked up by type in the navigator's
		/// screen table, because that table is keyed on types from this assembly
		/// and the multiplayer screen belongs to an assembly that depends on this
		/// one. Naming it here would make the assemblies reference each other.
		/// </summary>
		public void Multiplayer()
		{
			if (MultiplayerScreenPrefab == null)
			{
				Debug.LogError("Multiplayer is unavailable: no multiplayer screen is assigned.");
				return;
			}

			ScreenBase screen = Instantiate(MultiplayerScreenPrefab);

			if (screen == null)
			{
				Debug.LogError("Multiplayer is unavailable: the multiplayer screen could not be created.");
				return;
			}

			// The navigator does this for its own screens; a directly
			// instantiated one needs it called explicitly.
			screen.Awake();

			UIController.Instance.ScreenNavigator.NavigateToScreen(screen);
		}

		public void StoryScene()
		{
			UIController.Instance.ScreenNavigator.NavigateToScreen<StoreScreen>();
		}

		public void InstantAction()
		{
			SkirmishSetupScreen.PlayRandomSkirmish();
		}

		public void ContinueGame()
		{
			try
			{
				if (SaveGameUtilities.CanFindHeaders(out firstSaveGameHeader) && firstSaveGameHeader.CanBeLoaded)
				{
					GameController.Instance.ScenarioLoader.TryLoadScenario(firstSaveGameHeader);
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				UIController.Instance.ShowError("An error occured while attempting to load the saved game. Please contact " + GameController.Instance.SupportEmail + " for further assistance");
			}
		}

		protected override void start()
		{
			base.start();
			GameController.Instance.ScenarioLoader.LoadingScenario += ScenarioLoader_LoadingScenario;
		}

		private static void ApplyLaunchOrigin()
		{
			switch (GameController.Instance.LaunchOrigin)
			{
			case GameController.EngineLaunchSource.BattlesUI:
				UIController.Instance.ScreenNavigator.ShowBattlesScreen();
				break;
			case GameController.EngineLaunchSource.TutorialsUI:
				UIController.Instance.ScreenNavigator.ShowTutorialsScreen();
				break;
			case GameController.EngineLaunchSource.ScenariosUI:
				UIController.Instance.ScreenNavigator.ShowScenariosScreen();
				break;
			case GameController.EngineLaunchSource.Skirmish:
				UIController.Instance.ScreenNavigator.NavigateToScreen<SkirmishSetupScreen>();
				break;
			}
			GameController.Instance.LaunchOrigin = GameController.EngineLaunchSource.Unspecified;
		}

		private void RefreshVersionText()
		{
			// Open Frontier: merged version display (the old alpha watermark
			// is retired) - one label, project-driven number + stage tag.
			VersionText.text = $"Version: {Versioning.Version}{Versioning.StageTag}";
		}

		protected override void onMadeCurrentPanel(bool navigatedForward)
		{
			base.onMadeCurrentPanel(navigatedForward);
			Refresh();
		}

		protected override void refresh()
		{
			base.refresh();
			if (GameController.Instance.GameSaveEnabled)
			{
				RefreshCanLoadGame();
			}
			else
			{
				ContinueGameButton.gameObject.SetActive(value: false);
				LoadGameButton.gameObject.SetActive(value: false);
			}
			ApplyLaunchOrigin();
			RefreshVersionText();
		}

		protected override void update()
		{
			if (GameController.Instance.GameSaveEnabled && Time.realtimeSinceStartup > lastCanLoadGameCheckTime + 1f)
			{
				RefreshCanLoadGame();
				lastCanLoadGameCheckTime = Time.realtimeSinceStartup;
			}
		}

		private void RefreshCanLoadGame()
		{
			try
			{
				bool flag = SaveGameUtilities.CanFindHeaders(out firstSaveGameHeader);
				ContinueGameButton.interactable = flag && firstSaveGameHeader.CanBeLoaded;
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				ContinueGameButton.interactable = false;
				LoadGameButton.interactable = true;
			}
		}

		protected override void onDestroy()
		{
			base.onDestroy();
			if (GameController.Instance != null && GameController.Instance.ScenarioLoader != null)
			{
				GameController.Instance.ScenarioLoader.LoadingScenario -= ScenarioLoader_LoadingScenario;
			}
		}

		protected override bool onNavigatingBack()
		{
			Debug.LogWarning("Quitting game", this);
			Application.Quit();
			return true;
		}

		private void MessageBoxScreen_Dismissed(MessageBoxScreen sender, MessageBoxResult result)
		{
			if (result == MessageBoxResult.Ok)
			{
				ContinueGame();
			}
		}

		private void ScenarioLoader_LoadingScenario(ScenarioLoaderUI sender, ScenarioLoadData loadData)
		{
			if (EngineASX.Instance != null && EngineASX.Instance.ActiveSectorData != null)
			{
				EngineASX.Instance.ActiveSectorData.gameObject.SetActive(value: false);
			}
		}
	}
}
