using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.UniverseScenePicker
{
	public class SectorPickerScreen : ScreenBase
	{
		public delegate void ScenePickedHandler(SectorPickerScreen sender, List<SectorPickerItem> selectedItems);

		public bool AllowNone = true;

		public SectorPickerItemList ItemList;

		public Button ConfirmButton;

		public Button CancelButton;

		public Text TitleText;

		public bool ConfirmOnSelectionWhenSingleItem = true;

		public bool AutoConfirmOnSelection
		{
			get
			{
				if (ConfirmOnSelectionWhenSingleItem)
				{
					return !ItemList.IsMultiSelectEnabled;
				}
				return false;
			}
		}

		public event ScenePickedHandler ScenePicked;

		protected override void awake()
		{
			base.awake();
			ConfirmButton.onClick.AddListener(ConfirmButtonClick);
			CancelButton.onClick.AddListener(CancelButtonClick);
			ItemList.SelectedItemChanged += ItemList_SelectedItemChanged;
		}

		private void ItemList_SelectedItemChanged(ScrollList<SectorPickerItem> sender, SectorPickerItem oldItem, SectorPickerItem newItem)
		{
			if (ConfirmOnSelectionWhenSingleItem && sender != null && AutoConfirmOnSelection && ScenePicked != null)
			{
				ScenePicked(this, ItemList.SelectedItems.ToList());
			}
		}

		private void ConfirmButtonClick()
		{
			if (ScenePicked != null)
			{
				ScenePicked(this, ItemList.SelectedItems.ToList());
			}
		}

		private void CancelButtonClick()
		{
			NavigateBack();
		}

		protected override void refresh()
		{
			base.refresh();
			ItemList.Refresh();
			ConfirmButton.gameObject.SetActive(!AutoConfirmOnSelection);
		}

		public static List<SectorPickerItem> GetItemsFromScenes(List<Sector> scenes, bool allowNone)
		{
			List<SectorPickerItem> list = new List<SectorPickerItem>();
			if (allowNone)
			{
				list.Add(new SectorPickerItem
				{
					Sector = null
				});
			}
			foreach (Sector scene in scenes)
			{
				list.Add(new SectorPickerItem
				{
					Sector = scene
				});
			}
			return list;
		}
	}
}
