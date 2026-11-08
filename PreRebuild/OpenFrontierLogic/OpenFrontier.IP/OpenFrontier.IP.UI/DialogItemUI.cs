using OpenFrontier.IP.Engine.Comms;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class DialogItemUI : ScrollListItem<ICommsStageOption>
	{
		public Text TitleLabel;

		public override void Refresh()
		{
			if (Item != null)
			{
				DialogItemList dialogItemList = (DialogItemList)ParentList;
				TitleLabel.color = ((Item.NumberTimesSelected > 0) ? dialogItemList.ReadTextColor : dialogItemList.UnreadTextColor);
				TitleLabel.text = Item.OptionText;
			}
		}
	}
}
