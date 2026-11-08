using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Controls
{
	public class ItemListSelectionButton : MonoBehaviour
	{
		public Graphic MultiSelectEnabledGraphic;

		public Toggle ToggleMultiSelectButton;

		public Button SelectAllButton;

		public Button SelectNoneButton;

		public TextMeshProUGUI SelectionCountLabel;

		public ScrollListBase ItemList;

		public PopupMenu SelectionPopupMenu;

		private bool allowMultiSelect;

		private void Awake()
		{
			ItemList.SelectedItemsChanged += ItemList_SelectedItemsChanged;
			ItemList.ItemsChanged += ItemList_ItemsChanged;
			allowMultiSelect = ItemList.CanEnableMultiSelect;
			if (!allowMultiSelect)
			{
				ToggleMultiSelectButton.gameObject.SetActive(value: false);
			}
			ToggleMultiSelectButton.onValueChanged.AddListener(ToggleMultiSelectButtonValueChanged);
			SelectNoneButton.onClick.AddListener(SelectNoneButtonClick);
			SelectAllButton.onClick.AddListener(SelectAllButtonClick);
			RefreshSelectionCount();
			SelectionPopupMenu.Opening += SelectionPopupMenu_Opening;
		}

		private bool SelectionPopupMenu_Opening(PopupMenu sender)
		{
			ToggleMultiSelectButton.gameObject.SetActive(UI.TouchInputEnabled);
			return true;
		}

		private void Update()
		{
			if (ToggleMultiSelectButton.isOn != ItemList.IsMultiSelectEnabled)
			{
				ToggleMultiSelectButton.SetIsOnWithoutNotify(ItemList.IsMultiSelectEnabled);
			}
			MultiSelectEnabledGraphic.enabled = ItemList.IsMultiSelectEnabled;
		}

		private void ItemList_ItemsChanged(ScrollListBase sender)
		{
			RefreshSelectionCount();
		}

		private void SelectNoneButtonClick()
		{
			if (UI.TouchInputEnabled)
			{
				ScrollListBase itemList = ItemList;
				bool isMultiSelectEnabled = (ToggleMultiSelectButton.isOn = true);
				itemList.IsMultiSelectEnabled = isMultiSelectEnabled;
			}
			ItemList.ClearSelection();
		}

		private void SelectAllButtonClick()
		{
			if (UI.TouchInputEnabled)
			{
				ScrollListBase itemList = ItemList;
				bool isMultiSelectEnabled = (ToggleMultiSelectButton.isOn = true);
				itemList.IsMultiSelectEnabled = isMultiSelectEnabled;
			}
			ItemList.SelectAll();
		}

		private void ToggleMultiSelectButtonValueChanged(bool value)
		{
			if (value != ItemList.IsMultiSelectEnabled)
			{
				ItemList.IsMultiSelectEnabled = value;
				ItemList.OnMultiSelectChanged();
			}
		}

		private void ItemList_SelectedItemsChanged(ScrollListBase sender)
		{
			RefreshSelectionCount();
		}

		private void RefreshSelectionCount()
		{
			SelectionCountLabel.text = $"{ItemList.SelectedItemCount:N0} / {ItemList.TotalItemCount:N0} selected";
		}
	}
}
