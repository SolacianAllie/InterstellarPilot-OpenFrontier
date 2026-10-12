using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class SimpleListSelection : MonoBehaviour
	{
		public delegate void SelectedItemChangedHandler(SimpleListSelection sender, SimpleListSelectionItem oldItem);

		private List<SimpleListSelectionItem> items = new List<SimpleListSelectionItem>();

		private SimpleListSelectionItem selectedItem;

		public bool SelectFirstByDefault = true;

		public SimpleListSelectionItem SelectedItem
		{
			get
			{
				return selectedItem;
			}
			set
			{
				if (selectedItem != value)
				{
					SimpleListSelectionItem oldItem = selectedItem;
					selectedItem = value;
					if (selectedItem != null)
					{
						selectedItem.Awake();
					}
					if (SelectedItemChanged != null)
					{
						SelectedItemChanged(this, oldItem);
					}
				}
			}
		}

		public IEnumerable<SimpleListSelectionItem> Items => items;

		public event SelectedItemChangedHandler SelectedItemChanged;

		public void SelectFirstItem()
		{
			SelectedItem = gameObject.GetComponentInChildren<SimpleListSelectionItem>();
		}

		public void RegisterItem(SimpleListSelectionItem item)
		{
			if (!items.Contains(item))
			{
				items.Add(item);
			}
		}

		public bool DeregisterItem(SimpleListSelectionItem item)
		{
			if (items.Remove(item))
			{
				if (item == selectedItem)
				{
					SelectedItem = null;
				}
				return true;
			}
			return false;
		}

		private void Start()
		{
			if (SelectFirstByDefault)
			{
				SelectFirstItem();
			}
		}
	}
}
