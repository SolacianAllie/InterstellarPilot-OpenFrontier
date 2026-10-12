using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class LoadGameItemUI : ScrollListItem<EngineSaveGameHeader>
	{
		public TextMeshProUGUI DateLabel;

		public TextMeshProUGUI ScenarioNameLabel;

		public Graphic DisabledIndicator;

		public Graphic IsAutoSaveGraphic;

		public Graphic PermadeathGraphic;

		public override void Refresh()
		{
			base.Refresh();
			if (Item != null)
			{
				IsAutoSaveGraphic.enabled = Item.IsAutoSave;
				string text = Item.TimeStamp.ToString("dd-MMM-yyyy H:mm");
				DateLabel.text = text;
				PermadeathGraphic.enabled = Item.Permadeath;
				int gameWorldDay = EngineASX.Instance.DateTimeUtils.GetGameWorldDay(Item.SecondsElapsed);
				ScenarioInfo scenarioInfoById = GameController.Instance.GetScenarioInfoById(Item.ScenarioInfoId);
				string actualTitle = GetActualTitle(Item.ScenarioTitle, scenarioInfoById);
				ScenarioNameLabel.text = $"{actualTitle} (Day {gameWorldDay:N0})";
				if (scenarioInfoById == null)
				{
					Debug.LogError("Unknown scenario id: " + Item.ScenarioInfoId + " scenario is no longer valid");
				}
				DisabledIndicator.enabled = !Item.CanBeLoaded;
			}
		}

		public static string GetActualTitle(string scenarioTitle, ScenarioInfo scenarioInfo)
		{
			if (!string.IsNullOrWhiteSpace(scenarioTitle))
			{
				return scenarioTitle;
			}
			if (scenarioInfo != null)
			{
				return scenarioInfo.Title;
			}
			return "Unknown scenario";
		}
	}
}
