using OpenFrontier.IP.Engine.MissionObjectives;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class MissionObjectiveItemUI : ScrollListItem<MissionObjective>
	{
		public Graphic CompletedToggleGraphic;

		public Text DescriptionLabel;

		public override void Refresh()
		{
			base.Refresh();
			if (Item.Mission == null || !Item.Mission.IsValid)
			{
				return;
			}
			MissionObjectivesGrid missionObjectivesGrid = (MissionObjectivesGrid)ParentList;
			CompletedToggleGraphic.gameObject.SetActive(Item.IsComplete);
			string missionObjectiveText = Item.Mission.GetMissionObjectiveText(Item);
			if (Item.IsComplete)
			{
				if (Item.Success)
				{
					CompletedToggleGraphic.color = missionObjectivesGrid.CompletedTextColor;
					DescriptionLabel.text = $"[Completed] {missionObjectiveText}";
					DescriptionLabel.color = missionObjectivesGrid.CompletedTextColor;
				}
				else
				{
					CompletedToggleGraphic.color = missionObjectivesGrid.FailedTextColor;
					DescriptionLabel.text = $"[Failed] {missionObjectiveText}";
					DescriptionLabel.color = missionObjectivesGrid.FailedTextColor;
				}
			}
			else
			{
				DescriptionLabel.color = missionObjectivesGrid.DefaultTextColor;
				if (Item.IsOptional)
				{
					DescriptionLabel.text = $"[Optional] {missionObjectiveText}";
				}
				else
				{
					DescriptionLabel.text = missionObjectiveText;
				}
			}
		}
	}
}
