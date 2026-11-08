using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class ScenarioSelectButtonUI : ScrollListItem<ScenarioInfo>
	{
		public TextMeshProUGUI ComingSoonText;

		public Image[] CompletionImages;

		public bool IsScenarioPlayable = true;

		public Text Label;

		public bool LoadEngine = true;

		public GameObject LockedObject;

		public Text TutorialLabel;

		public override void Refresh()
		{
			base.Refresh();
			if (!(Item != null))
			{
				return;
			}
			if (ComingSoonText != null)
			{
				ComingSoonText.enabled = Item.ComingSoon;
			}
			TutorialLabel.gameObject.SetActive(Item.IsTutorial);
			bool isUnlocked = Item.GetIsUnlocked();
			LockedObject.gameObject.SetActive(!isUnlocked);
			Toggle.interactable = isUnlocked && !Item.ComingSoon;
			bool flag = false;
			Label.text = Item.Title;
			int completionDifficultyLevel = -1;
			if (isUnlocked)
			{
				flag = Item.IsCompleted(out completionDifficultyLevel);
			}
			if (flag && Item.ShowCompletionData)
			{
				if (!Item.ShowCompletionDifficultyLevels)
				{
					completionDifficultyLevel = 0;
				}
				for (int i = 0; i < CompletionImages.Length; i++)
				{
					if (i <= completionDifficultyLevel)
					{
						CompletionImages[i].gameObject.SetActive(value: true);
						CompletionImages[i].color = GameController.Instance.CompletionColors[completionDifficultyLevel];
					}
					else
					{
						CompletionImages[i].gameObject.SetActive(value: false);
					}
				}
			}
			else
			{
				for (int j = 0; j < CompletionImages.Length; j++)
				{
					CompletionImages[j].gameObject.SetActive(value: false);
				}
			}
		}

		private void OnEnable()
		{
			Refresh();
		}
	}
}
