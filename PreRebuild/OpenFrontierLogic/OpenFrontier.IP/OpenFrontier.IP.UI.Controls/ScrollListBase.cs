using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.UI.Controls
{
	public class ScrollListBase : MonoBehaviour
	{
		public delegate void SelectedItemsChangedHandler(ScrollListBase sender);

		public delegate void ItemsChangedHandler(ScrollListBase sender);

		public bool CanEnableMultiSelect = true;

		[FormerlySerializedAs("AllowSelectMultiple")]
		public bool IsMultiSelectEnabled;

		public virtual int SelectedItemCount => 0;

		public virtual int TotalItemCount => 0;

		public event SelectedItemsChangedHandler SelectedItemsChanged;

		public event ItemsChangedHandler ItemsChanged;

		public virtual void ClearSelection()
		{
		}

		public virtual void SelectAll()
		{
		}

		protected void RaiseSelectedItemsChanged()
		{
			if (SelectedItemsChanged != null)
			{
				SelectedItemsChanged(this);
			}
		}

		protected void RaiseItemsChanged()
		{
			if (ItemsChanged != null)
			{
				ItemsChanged(this);
			}
		}

		protected virtual void OnSelectedItemsChanged()
		{
		}

		public virtual void OnMultiSelectChanged()
		{
		}

		public virtual void DeselectAllExcept(ScrollListItemBase item)
		{
		}
	}
}
