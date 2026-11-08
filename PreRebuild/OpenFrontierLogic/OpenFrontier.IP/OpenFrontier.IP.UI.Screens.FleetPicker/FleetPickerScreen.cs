using System.Collections.Generic;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.FleetPicker
{
	public class FleetPickerScreen : ScreenBase
	{
		public delegate void FleetPickedHandler(FleetPickerScreen sender, bool result, FleetPickerItem item);

		public Text TitleText;

		public List<FleetPickerItem> FleetItems = new List<FleetPickerItem>();

		public FleetPickerList ItemList;

		public Button CancelButton;

		public event FleetPickedHandler FleetPickedResult;

		protected override void awake()
		{
			base.awake();
			CancelButton.onClick.AddListener(CancelButtonClick);
		}

		private void CreateNewButtonClick()
		{
			if (FleetPickedResult != null)
			{
				FleetPickedResult(this, result: false, null);
			}
			NavigateBack();
		}

		private void CancelButtonClick()
		{
			if (FleetPickedResult != null)
			{
				FleetPickedResult(this, result: false, null);
			}
			NavigateBack();
		}

		private void ItemList_ToggleValueOn(ScrollList<FleetPickerItem> sender, ScrollListItem<FleetPickerItem> item)
		{
			if (FleetPickedResult != null)
			{
				FleetPickedResult(this, result: true, item.Item);
			}
		}

		protected override void refresh()
		{
			base.refresh();
			ItemList.SetItems(FleetItems);
			ItemList.ToggleValueOn -= ItemList_ToggleValueOn;
			ItemList.ToggleValueOn += ItemList_ToggleValueOn;
		}
	}
}
