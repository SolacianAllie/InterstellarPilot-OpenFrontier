using OpenFrontier.IP.UI.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class ScrollListItem<T_Item> : ScrollListItemBase where T_Item : class
	{
		public T_Item Item;

		private ScrollList<T_Item> parentList;

		public Toggle Toggle;

		private Button button;

		public override ScrollListBase ParentListBase => ParentList;

		public ScrollList<T_Item> ParentList
		{
			get
			{
				return parentList;
			}
			set
			{
				parentList = value;
			}
		}

		public Button Button => button;

		[ContextMenu("Refresh")]
		public virtual void Refresh()
		{
		}

		public virtual void OnActiveInList()
		{
		}

		protected virtual void awake()
		{
		}

		protected virtual void OnToggleValueChanged(bool value)
		{
			if (ParentList == null)
			{
				Debug.LogError("ItemListItem button activated but item has no ParentList!");
			}
			else
			{
				ParentList.NotifyToggleValueChanged(this, value);
				if (value)
				{
					OnToggleValueOn();
				}
			}
			OnToggleValueOn();
		}

		protected virtual void OnButtonClick()
		{
			parentList.NotifyItemButtonClick(this);
		}

		protected virtual void OnToggleValueOn()
		{
		}

		private void Awake()
		{
			if (Toggle == null)
			{
				Toggle = GetComponent<Toggle>();
			}
			button = GetComponent<Button>();
			if (Toggle != null)
			{
				Toggle.onValueChanged.AddListener(OnToggleValueChanged);
			}
			if (Button != null)
			{
				Button.onClick.AddListener(OnButtonClick);
			}
			awake();
		}

		public virtual void Tick()
		{
		}

		public virtual void CleanupOnDisable()
		{
		}
	}
}
