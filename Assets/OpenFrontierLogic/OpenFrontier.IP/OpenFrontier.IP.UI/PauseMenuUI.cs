using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.SaveGame;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.LoadGame;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.billing;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class PauseMenuUI : EngineScreen
	{
		public Button StoreButton;

		public Button LoadGameButton;

		public Button OptionsButton;

		public Button QuitButton;

		public Button ResumeButton;

		public Button SaveGameButton;

		private bool? wasPaused;

		public bool? WasPaused
		{
			get
			{
				return wasPaused;
			}
			set
			{
				wasPaused = value;
			}
		}

		protected override void awake()
		{
			base.awake();
			SaveGameButton.onClick.AddListener(SaveGameButton_Activated);
			LoadGameButton.onClick.AddListener(LoadGameButton_Activated);
			OptionsButton.onClick.AddListener(OptionsButton_Activated);
			QuitButton.onClick.AddListener(QuitButton_Activated);
			ResumeButton.onClick.AddListener(ResumeButton_Activated);
		}

		protected override void refresh()
		{
			base.refresh();
			if (EngineASX.LoadedAndReady)
			{
				StoreButton.gameObject.SetActive(Products.IAPEnabled);
				RefreshSaveButtonInteractable();
				LoadGameButton.interactable = !Eng.World.ScenarioOptions.Permadeath;
			}
		}

		private void RefreshSaveButtonInteractable()
		{
			SaveGameButton.interactable = AllowSave();
		}

		protected override bool onNavigatingBack()
		{
			Eng.IsPaused = wasPaused.Value;
			return base.onNavigatingBack();
		}

		private void ResumeButton_Activated()
		{
			NavigateBack();
		}

		private void MessageBox_Dismissed(MessageBoxScreen sender, MessageBoxResult result)
		{
			if (result == MessageBoxResult.Ok)
			{
				if (Eng.World.ScenarioOptions.Permadeath)
				{
					Eng.AutoSaveIfPossible();
				}
				UI.QuitToMainMenu();
			}
		}

		private bool AllowSave()
		{
			if (Eng.World.ScenarioOptions.Permadeath)
			{
				return false;
			}
			if (Eng.World.Permissions.AllowSaving)
			{
				if (Eng.GameSettings.SaveOnlyWhenDocked && Eng.LocalPlayer.Person.CurrentUnit.UnitType != UnitType.Station)
				{
					return Eng.LocalPlayer.Person.CurrentUnit.IsDocked;
				}
				return true;
			}
			return false;
		}

		private void QuitButton_Activated()
		{
			string text = "Are you sure you want to quit?";
			if (Eng.World.ScenarioOptions.Permadeath)
			{
				text += " All progress will be saved.";
			}
			UIController.Instance.ShowMessageBox(text, MessageBoxButtons.OkCancel, MessageBox_Dismissed, MessageBoxIcon.Question);
		}

		private void OptionsButton_Activated()
		{
			UIController.Instance.ScreenNavigator.NavigateToScreen<OptionsScreen>();
		}

		private void LoadGameButton_Activated()
		{
			if (Eng.World.ScenarioOptions.Permadeath)
			{
				UIController.Instance.ShowMessageBox("Loading is not permitted in this scenario", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
			}
			else
			{
				UIController.Instance.ScreenNavigator.NavigateToScreen<LoadGameScreen>();
			}
		}

		private void SaveGameButton_Activated()
		{
			EngineASX instance = EngineASX.Instance;
			MessageBoxIcon icon = MessageBoxIcon.Ok;
			string text = "Game saved OK";
			if (!EngineIO.Save(instance, out var errorMessage))
			{
				text = "Game save failed";
				if (!string.IsNullOrWhiteSpace(errorMessage))
				{
					text = text + ": " + errorMessage;
				}
				icon = MessageBoxIcon.Error;
			}
			UIController.Instance.ShowMessageBox(text, MessageBoxButtons.Ok, null, icon);
		}
	}
}
