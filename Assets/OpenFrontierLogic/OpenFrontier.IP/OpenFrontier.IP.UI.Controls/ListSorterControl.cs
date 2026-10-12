using OpenFrontier.IP.Common;
using TMPro;
using UnityEngine;

namespace OpenFrontier.IP.UI.Controls
{
	public class ListSorterControl : MonoBehaviour
	{
		public SortOrderButton SortDirectionButton;

		public TMP_Dropdown SortOptionsDropdown;

		public ListSorter TargetListSorter;

		private void Awake()
		{
			SortOptionsDropdown.ClearOptions();
			SortOptionsDropdown.AddOptions(TargetListSorter.GetSortOptionNames());
			SortDirectionButton.Toggle.onValueChanged.AddListener(SortDirectionToggleValueChanged);
			SortOptionsDropdown.value = TargetListSorter.GetSortMode();
			SortOptionsDropdown.onValueChanged.AddListener(SortDropdownValueChanged);
		}

		private void SortDirectionToggleValueChanged(bool value)
		{
			if (TargetListSorter != null)
			{
				TargetListSorter.SortOrder = SortDirectionButton.SortMode;
				TargetListSorter.Apply();
			}
		}

		private void SortDropdownValueChanged(int value)
		{
			if (value >= 0)
			{
				SortDirectionButton.Toggle.onValueChanged.RemoveListener(SortDirectionToggleValueChanged);
				SortDirectionButton.SortMode = Sort.Ascending;
				SortDirectionButton.Toggle.onValueChanged.AddListener(SortDirectionToggleValueChanged);
				TargetListSorter.SortOrder = Sort.Ascending;
				TargetListSorter.SetSortMode(value);
				TargetListSorter.Apply();
			}
		}
	}
}
