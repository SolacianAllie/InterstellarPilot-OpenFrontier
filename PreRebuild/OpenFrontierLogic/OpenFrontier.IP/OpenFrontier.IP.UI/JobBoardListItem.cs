using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.MissionSpecs;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class JobBoardListItem : ScrollListItem<MissionSpec>
	{
		public Text CreditsRewardLabel;

		public Text TitleLabel;

		public override void Refresh()
		{
			base.Refresh();
			TitleLabel.text = Item.CalculateMissionSpecTitle();
			int profitCredits = Item.ProfitCredits;
			if (profitCredits > 0)
			{
				CreditsRewardLabel.text = $"{TextFormattingHelper.FormatCredits(profitCredits)} cr";
			}
			else
			{
				CreditsRewardLabel.text = "-";
			}
		}
	}
}
