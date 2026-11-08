using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.MessageBox;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.ActiveMission
{
	public class MissionsScreenItemListItem : ScrollListItem<Mission>
	{
		public Text FactionLabel;

		public Text GuidanceActiveLabel;

		public Text TitleLabel;

		public Text RewardLabel;

		public override void Refresh()
		{
			base.Refresh();
			MissionsScreenItemList missionsScreenItemList = (MissionsScreenItemList)ParentList;
			if (!(Item != null) || !Item.IsValid)
			{
				return;
			}
			string text = Item.CalculateTitle();
			if (Item.IsFinished)
			{
				if (Item.CompletionSuccess)
				{
					TitleLabel.text = $"[Completed] {text}";
					TitleLabel.color = missionsScreenItemList.CompletedTextColor;
				}
				else
				{
					TitleLabel.text = $"[Failed] {text}";
					TitleLabel.color = missionsScreenItemList.FailedTextColor;
				}
			}
			else
			{
				TitleLabel.text = text;
				TitleLabel.color = missionsScreenItemList.DefaultTextColor;
			}
			RefreshRewardLabel();
			GuidanceActiveLabel.gameObject.SetActive(Item.IsGuidanceActive());
			FactionLabel.text = ((Item.MissionGiverFaction != null) ? Item.MissionGiverFaction.GetShortNameElseLong() : string.Empty);
		}

		private void RefreshRewardLabel()
		{
			if (Item.IsFinished)
			{
				RewardLabel.text = string.Empty;
			}
			else if (Item.MissionRewardCredits > 0)
			{
				RewardLabel.text = TextFormattingHelper.FormatCredits(Item.MissionRewardCredits, includeSuffix: true);
			}
			else
			{
				RewardLabel.text = string.Empty;
			}
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			MissionsScreenItemList missionsScreenItemList = ParentList as MissionsScreenItemList;
			if (missionsScreenItemList != null)
			{
				if (Item != null && Item.IsValid)
				{
					UIController.Instance.ScreenNavigator.ShowActiveMissionScreen(Item);
				}
				else
				{
					UIController.Instance.ShowMessageBox("The mission has been aborted", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
				}
			}
			else
			{
				Debug.LogError("Expecting parent list to be of type MissionListUI. Actual: " + missionsScreenItemList, this);
			}
		}
	}
}
