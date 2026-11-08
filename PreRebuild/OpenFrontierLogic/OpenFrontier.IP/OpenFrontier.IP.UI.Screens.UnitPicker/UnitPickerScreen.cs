using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.UnitPicker
{
	public class UnitPickerScreen : ScreenBase
	{
		public delegate void UnitPickedHandler(UnitPickerScreen sender, bool result, UnitPickerItem item);

		public Sector LocalSector;

		public Text TitleText;

		public bool AllowNone = true;

		public List<Unit> Units = new List<Unit>();

		public UnitPickerItemList ItemList;

		public Button CancelButton;

		public event UnitPickedHandler UnitPickResult;

		protected override void awake()
		{
			base.awake();
			ItemList.ToggleValueOn += ItemList_ToggleValueOn;
			CancelButton.onClick.AddListener(CancelButtonClick);
		}

		private void CancelButtonClick()
		{
			if (UnitPickResult != null)
			{
				UnitPickResult(this, result: false, null);
			}
			NavigateBack();
		}

		private void ItemList_ToggleValueOn(ScrollList<UnitPickerItem> sender, ScrollListItem<UnitPickerItem> item)
		{
			if (UnitPickResult != null)
			{
				UnitPickResult(this, result: true, item.Item);
			}
		}

		protected override void refresh()
		{
			base.refresh();
			List<UnitPickerItem> items = GetItems();
			ItemList.SetItems(items);
		}

		private List<UnitPickerItem> GetItems()
		{
			List<UnitPickerItem> list = new List<UnitPickerItem>();
			if (AllowNone)
			{
				list.Add(new UnitPickerItem
				{
					Unit = null,
					LocalSector = LocalSector
				});
			}
			foreach (Unit unit in Units)
			{
				list.Add(new UnitPickerItem
				{
					Unit = unit,
					LocalSector = LocalSector
				});
			}
			return list;
		}
	}
}
