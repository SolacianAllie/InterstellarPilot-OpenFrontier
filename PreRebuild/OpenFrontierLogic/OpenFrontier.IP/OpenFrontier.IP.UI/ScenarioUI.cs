using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Extensions;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class ScenarioUI : ScreenBase
	{
		public Text TitleText;

		public GameObject SelectedScenarioRoot;

		[FormerlySerializedAs("DiffSlider")]
		public DifficultySlider CombatDifficultySlider;

		private bool isStarted;

		public GameController.EngineLaunchSource LaunchOrigin;

		public Button PlayButton;

		public Text ScenarioDescriptionLabel;

		public ScrollRect ScenarioDescriptionScrollRect;

		public Text ScenarioFactionLabel;

		public ScenarioItemListUI ScenarioItemList;

		public Text ScenarioPilotLabel;

		public Text ScenarioSectorLabel;

		public Text ScenarioStarDateLabel;

		public Text ScenarioTitleLabel;

		public bool UseFactionShortName = true;

		public ScenarioInfo SelectedScenario
		{
			get
			{
				return ScenarioItemList.FirstSelectedItem;
			}
			set
			{
				ScenarioItemList.FirstSelectedItem = value;
			}
		}

		public static void PlayScenario(ScenarioLoadData scenarioLoadData, GameController.EngineLaunchSource launchOrigin)
		{
			GameController.Instance.LaunchOrigin = launchOrigin;
			GameController.Instance.LastAttemptedScenario = scenarioLoadData.ScenarioInfo;
			GameController.Instance.LastAttemptedScenarionCompleted = false;
			GameController.Instance.ScenarioLoader.TryLoadScenario(scenarioLoadData);
		}

		public void PlaySelectedScenario()
		{
			if (SelectedScenario != null)
			{
				OnPlayingScenario(SelectedScenario);
			}
		}

		protected override void awake()
		{
			base.awake();
			SelectedScenarioRoot.SetActive(SelectedScenario != null);
		}

		protected override void onEnable()
		{
			base.onEnable();
			UpdatePlayButtonEnabled();
			if (isStarted)
			{
				ScenarioItemList.Refresh();
			}
		}

		protected override void update()
		{
			base.update();
			UpdatePlayButtonEnabled();
		}

		protected override void start()
		{
			base.start();
			ScenarioItemList.SelectedItemChanged += ScenarioItemList_SelectedItemChanged;
			if (ScenarioItemList.FirstSelectedItem != null)
			{
				ScenarioItemList_SelectedItemChanged(ScenarioItemList, null, ScenarioItemList.FirstSelectedItem);
			}
			PlayButton.onClick.AddListener(PlaySelectedScenario);
			isStarted = true;
		}

		protected override void refresh()
		{
			base.refresh();
			ScenarioItemList.Refresh();
			SelectBestItem();
			int difficultyLevelIndex = PlayerPrefs.GetInt(GameController.Instance.CombatDifficultyLevelKey, GameController.Instance.CombatDifficultyLevels.IndexOf(GameController.Instance.CombatDifficultyLevel));
			if (CombatDifficultySlider != null)
			{
				CombatDifficultySlider.Init();
				CombatDifficultySlider.SetDifficultyLevelIndex(difficultyLevelIndex);
			}
		}

		protected override void onDisable()
		{
			base.onDisable();
			PersistDifficultySliderSetting();
		}

		private void PersistDifficultySliderSetting()
		{
			if (CombatDifficultySlider != null)
			{
				CombatDifficultySlider.SetGameDifficultyFromSliderValue();
				GameController.Instance.SaveCombatDifficultyPreference();
				PlayerPrefs.Save();
			}
		}

		protected override bool onNavigatingBack()
		{
			PersistDifficultySliderSetting();
			return base.onNavigatingBack();
		}

		protected virtual string GetDescriptionText(ScenarioInfo scenarioInfo)
		{
			ScenarioInfo uncompletedScenario = null;
			bool isUnlocked = SelectedScenario.GetIsUnlocked(out uncompletedScenario);
			string text = scenarioInfo.Description;
			if (!isUnlocked)
			{
				text += $"\n\nComplete \"{uncompletedScenario.Title}\" to unlock";
			}
			if (!string.IsNullOrEmpty(scenarioInfo.MissionLog))
			{
				text += "\n\n";
				text = text + "<b>Mission Log</b>\n\n" + scenarioInfo.MissionLog;
			}
			return text;
		}

		protected virtual void OnSelectedItemChanged(ScenarioInfo oldItem)
		{
			if (SelectedScenario != null)
			{
				RefreshScenarioDescription();
				ScenarioTitleLabel.text = SelectedScenario.Title;
				if (ScenarioFactionLabel != null)
				{
					if (UseFactionShortName)
					{
						ScenarioFactionLabel.text = StringOrUnknown((SelectedScenario.PilotFaction != null) ? SelectedScenario.PilotFaction.ShortName : null);
					}
					else
					{
						ScenarioFactionLabel.text = StringOrUnknown((SelectedScenario.PilotFaction != null) ? SelectedScenario.PilotFaction.Name : null);
					}
				}
				if (ScenarioPilotLabel != null)
				{
					ScenarioPilotLabel.text = StringOrUnknown(SelectedScenario.PilotName);
				}
				if (SelectedScenario.InitialScene != null)
				{
					ScenarioSectorLabel.text = StringOrUnknown(SelectedScenario.InitialScene.Name);
				}
				else
				{
					ScenarioSectorLabel.text = StringOrUnknown(SelectedScenario.SectorName);
				}
				ScenarioStarDateLabel.text = $"{SelectedScenario.DateYear}-{SelectedScenario.DateMonth}-{SelectedScenario.DateDay}";
			}
			ResetDescriptionScroll();
			UpdatePlayButtonEnabled();
		}

		protected void RefreshScenarioDescription()
		{
			if (ScenarioDescriptionLabel != null && SelectedScenario != null)
			{
				ScenarioDescriptionLabel.text = GetDescriptionText(SelectedScenario);
			}
		}

		protected virtual void OnPlayingScenario(ScenarioInfo scenario)
		{
			PlayScenario(GetLoadData(scenario), LaunchOrigin);
		}

		protected virtual ScenarioLoadData GetLoadData(ScenarioInfo scenario)
		{
			return new ScenarioLoadData
			{
				ScenarioInfo = scenario,
				FullSaveGamePath = null
			};
		}

		private void ScenarioItemList_SelectedItemChanged(ScrollList<ScenarioInfo> sender, ScenarioInfo oldItem, ScenarioInfo newItem)
		{
			OnSelectedItemChanged(oldItem);
			if (SelectedScenarioRoot != null)
			{
				SelectedScenarioRoot.SetActive(SelectedScenario != null);
			}
		}

		private void SelectBestItem()
		{
			if (GameController.Instance.LastAttemptedScenario != null)
			{
				Debug.Log("ScenarioUI: Last attempted scenario was " + GameController.Instance.LastAttemptedScenario, this);
				List<ScenarioInfo> activeItems = ScenarioItemList.ActiveItems;
				int num = ScenarioItemList.ActiveItems.IndexOf(GameController.Instance.LastAttemptedScenario);
				if (num > -1)
				{
					if (!GameController.Instance.LastAttemptedScenarionCompleted)
					{
						ScenarioItemList.FirstSelectedItem = activeItems[num];
					}
					else
					{
						int num2 = num + 1;
						if (num2 < activeItems.Count && activeItems[num2].GetIsUnlocked())
						{
							ScenarioItemList.FirstSelectedItem = activeItems[num2];
						}
						else
						{
							ScenarioItemList.FirstSelectedItem = activeItems[num];
						}
					}
				}
			}
			if (ScenarioItemList.FirstSelectedItem == null)
			{
				List<ScenarioInfo> source = ScenarioItemList.ActiveItems.Where((ScenarioInfo e) => !e.ComingSoon && e.GetIsUnlocked() && !e.IsCompleted()).ToList();
				if (source.Any())
				{
					ScenarioItemList.FirstSelectedItem = source.First();
				}
				else
				{
					ScenarioItemList.TrySelectFirstItem();
				}
			}
			ScenarioItemList.ScrollToSelected();
		}

		private void ResetDescriptionScroll()
		{
			if (ScenarioDescriptionScrollRect != null)
			{
				ScenarioDescriptionScrollRect.ResetScrollPosition();
			}
		}

		private void UpdatePlayButtonEnabled()
		{
			PlayButton.interactable = SelectedScenario != null && SelectedScenario.ShowInScenarioUI && !SelectedScenario.ComingSoon && (SelectedScenario.GetIsUnlocked() || GameController.Instance.IgnoreScenarioPrereqs);
		}

		private string StringOrUnknown(string str)
		{
			if (!string.IsNullOrEmpty(str))
			{
				return str;
			}
			return "Unknown";
		}
	}
}
