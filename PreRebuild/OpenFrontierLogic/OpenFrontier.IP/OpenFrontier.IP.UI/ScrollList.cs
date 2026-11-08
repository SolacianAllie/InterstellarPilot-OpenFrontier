using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Controls;
using OpenFrontier.IP.UI.Extensions;
using OpenFrontier.IP.UI.Screens;
using UnityEngine;
using UnityEngine.UI;
using Input = OpenFrontier.LegacyInput;

namespace OpenFrontier.IP.UI
{
	public class ScrollList<T_Item> : ScrollListBase where T_Item : class
	{
		public delegate void ItemClickedHandler(ScrollList<T_Item> sender, ScrollListItem<T_Item> item);

		public delegate void ToggleValueOnHandler(ScrollList<T_Item> sender, ScrollListItem<T_Item> item);

		public delegate void SelectedItemChangedHandler(ScrollList<T_Item> sender, T_Item oldItem, T_Item newItem);

		public bool ForceRefreshAllItems;

		public bool LegacyUpdate = true;

		public int VisibleItemRangeFudge = 2;

		public AudioClip ItemClickAudioClip;

		private static List<T_Item> itemCache = new List<T_Item>();

		private List<T_Item> activeItems = new List<T_Item>();

		public bool AutoSelectFirstItem = true;

		public bool ClearOnDisable;

		private EngineASX engine;

		public Graphic FilterWidget;

		public Vector3 FilterWidgetOffsetFromHeader = new Vector3(80f, 0f, 0f);

		public int InitialPoolSize = 10;

		private bool isAwake;

		private bool isStale;

		public IComparer<T_Item> ItemComparer;

		public GameObject ItemContainer;

		public GameObject ItemPrefab;

		private List<ScrollListItem<T_Item>> uiItemPool = new List<ScrollListItem<T_Item>>(24);

		private List<bool> refreshedItems = new List<bool>(24);

		public Text NoItemsLabel;

		public bool RefreshOnEnable;

		public bool ResetScrollPositionOnEnable;

		[SerializeField]
		protected ScrollRect scrollRect;

		private List<T_Item> selectedItems = new List<T_Item>();

		public bool SelectionEnabled = true;

		public bool UsePool = true;

		public ToggleGroup ToggleGroup;

		private bool suppressToggleValueChanged;

		private ToggleGroupButtonSoundsController toggleGroupButtonSoundsController;

		public List<T_Item> ActiveItems => activeItems;

		public EngineASX Engine => engine;

		public int SelectedIndex => activeItems.IndexOf(FirstSelectedItem);

		public T_Item SingleSelectedItem
		{
			get
			{
				if (selectedItems.Count == 1)
				{
					return selectedItems[0];
				}
				return null;
			}
		}

		public T_Item FirstSelectedItem
		{
			get
			{
				if (selectedItems.Count > 0)
				{
					return selectedItems[0];
				}
				return null;
			}
			set
			{
				if (object.Equals(FirstSelectedItem, value))
				{
					return;
				}
				if (value != null && !activeItems.Contains(value))
				{
					if (LogWrapper.LogMsgs)
					{
						Debug.LogError("Attempting to select item that is not in the list", this);
					}
					value = null;
				}
				T_Item firstSelectedItem = FirstSelectedItem;
				selectedItems.Clear();
				if (value != null)
				{
					selectedItems.Add(value);
				}
				ActivateSelectedButtons();
				OnSelectedItemsChanged();
				RaiseSelectedItemsChanged();
				if (SelectedItemChanged != null)
				{
					SelectedItemChanged(this, firstSelectedItem, FirstSelectedItem);
				}
			}
		}

		public List<T_Item> SelectedItems => selectedItems;

		public List<ScrollListItem<T_Item>> UIItemPool => uiItemPool;

		public ScrollListItem<T_Item> SelectedUIItem
		{
			get
			{
				if (SelectedIndex > -1)
				{
					return UIItemPool[SelectedIndex];
				}
				return null;
			}
		}

		public override int SelectedItemCount => selectedItems.Count;

		public override int TotalItemCount => activeItems.Count;

		public event SelectedItemChangedHandler SelectedItemChanged;

		public event ItemClickedHandler ItemClicked;

		public event ToggleValueOnHandler ToggleValueOn;

		public ScrollListItem<T_Item> TryGetPooledListItem(T_Item item)
		{
			if (UsePool && activeItems.Count < uiItemPool.Count)
			{
				return uiItemPool[activeItems.Count];
			}
			ScrollListItem<T_Item> scrollListItem = CreateItem(item);
			uiItemPool.Add(scrollListItem);
			refreshedItems.Add(item: false);
			return scrollListItem;
		}

		public void MarkAllItemsAsNeedRefresh()
		{
			for (int i = 0; i < activeItems.Count; i++)
			{
				refreshedItems[i] = false;
			}
		}

		public void Update()
		{
			if (LegacyUpdate)
			{
				Tick();
			}
		}

		public void Tick()
		{
			if (!isStale)
			{
				isStale = DetermineIsStale();
			}
			if (isStale)
			{
				Refresh();
				isStale = false;
			}
			update();
		}

		public void TickItems()
		{
			for (int i = 0; i < activeItems.Count; i++)
			{
				uiItemPool[i].Tick();
			}
		}

		public void ClearActiveItems()
		{
			SetItems(new T_Item[0]);
		}

		public void Add(T_Item item)
		{
			List<T_Item> list = activeItems.ToList();
			list.Add(item);
			SetItems(list);
		}

		public void AddRange(IEnumerable<T_Item> items)
		{
			List<T_Item> list = activeItems.ToList();
			list.AddRange(items);
			SetItems(list);
		}

		public void Remove(T_Item item)
		{
			List<T_Item> list = activeItems.ToList();
			list.Remove(item);
			SetItems(list);
		}

		public void RemoveAt(int index)
		{
			List<T_Item> list = activeItems.ToList();
			list.RemoveAt(index);
			SetItems(list);
		}

		public virtual void SetItems(IEnumerable<T_Item> items)
		{
			T_Item firstSelectedItem = FirstSelectedItem;
			itemCache.Clear();
			if (items != null)
			{
				itemCache.AddRange(items);
			}
			if (ItemComparer != null)
			{
				itemCache.Sort(ItemComparer);
			}
			int count = activeItems.Count;
			activeItems.Clear();
			List<T_Item> list = null;
			if (SelectionEnabled)
			{
				list = selectedItems.ToList();
			}
			if (ItemContainer != null)
			{
				foreach (T_Item item in itemCache)
				{
					GetListItemForObject(item);
					activeItems.Add(item);
				}
				MarkAllItemsAsNeedRefresh();
			}
			if (count != activeItems.Count)
			{
				DeactivateUnusedPoolItems();
			}
			if (SelectionEnabled)
			{
				for (int i = 0; i < list.Count; i++)
				{
					if (activeItems.Contains(list[i]))
					{
						list.RemoveAt(i);
						i--;
					}
				}
				while (list.Count > 0)
				{
					SetItemSelected(list[0], selected: false);
					list.RemoveAt(0);
				}
				if (FirstSelectedItem == null && AutoSelectFirstItem)
				{
					TrySelectFirstItem();
				}
			}
			ActivateSelectedButtons();
			RefreshNoItemsLabel();
			if (firstSelectedItem != null && ActiveItems.IndexOf(firstSelectedItem) == -1)
			{
				OnLostActiveItem();
			}
			RaiseItemsChanged();
			if (ForceRefreshAllItems)
			{
				RefreshAllItems();
				return;
			}
			if (gameObject.activeInHierarchy)
			{
				UIController.Instance.StartCoroutine(StartVisibleItemRefreshCoroutine());
				return;
			}
			Debug.LogWarning("Not possible to refresh only visible items as this gameObject is not currently active", this);
			RefreshAllItems();
		}

		private IEnumerator StartVisibleItemRefreshCoroutine()
		{
			yield return null;
			RefreshVisibleItemsIfRequired();
		}

		protected virtual void OnLostActiveItem()
		{
			ResetScrollPosition();
		}

		public virtual void ResetScrollPosition()
		{
			if (scrollRect != null && scrollRect.verticalScrollbar != null)
			{
				scrollRect.verticalScrollbar.ResetScrollValue();
			}
		}

		[ContextMenu("Refresh")]
		public void Refresh()
		{
			if (!isAwake)
			{
				Awake();
			}
			OnRefreshing();
			RefreshNoItemsLabel();
			ActivateSelectedButtons();
		}

		public T_Item SwitchItem(T_Item curItem, int movement)
		{
			int num = activeItems.IndexOf(curItem);
			num += movement;
			num = Maths.WrapValue(num, 0, activeItems.Count);
			if (num >= 0 && num < activeItems.Count)
			{
				return activeItems[num];
			}
			return null;
		}

		public void NotifyItemButtonClick(ScrollListItem<T_Item> item)
		{
			TryPlayClickAudioClip();
			if (ItemClicked != null)
			{
				ItemClicked(this, item);
			}
		}

		private void TryPlayClickAudioClip()
		{
			if (ItemClickAudioClip != null && GameController.Instance.PlayButtonSounds)
			{
				AudioHelper.PlaySound(ItemClickAudioClip);
			}
		}

		public void NotifyToggleValueChanged(ScrollListItem<T_Item> item, bool isOn)
		{
			if (suppressToggleValueChanged)
			{
				return;
			}
			if (SelectionEnabled)
			{
				if (IsMultiSelectEnabled || (CanEnableMultiSelect && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))))
				{
					SetItemSelected(item.Item, isOn);
				}
				else if ((isOn && selectedItems.Count != 1) || FirstSelectedItem != item.Item)
				{
					FirstSelectedItem = item.Item;
				}
				else if (!isOn && FirstSelectedItem == item.Item && ToggleGroup != null && ToggleGroup.allowSwitchOff)
				{
					FirstSelectedItem = null;
				}
			}
			else if (item.Toggle != null && ToggleGroup == null)
			{
				item.Toggle.isOn = false;
			}
			if (isOn)
			{
				TryPlayClickAudioClip();
				if (ToggleValueOn != null)
				{
					ToggleValueOn(this, item);
				}
			}
		}

		public ScrollListItem<T_Item> GetActiveListItem(T_Item item)
		{
			for (int i = 0; i < activeItems.Count; i++)
			{
				if (object.Equals(activeItems[i], item))
				{
					return uiItemPool[i];
				}
			}
			return null;
		}

		public void ScrollToSelected()
		{
			if (scrollRect != null && FirstSelectedItem != null)
			{
				float normalizedPosition = Mathf.Clamp01((float)activeItems.IndexOf(FirstSelectedItem) / (float)activeItems.Count);
				scrollRect.ScrollToPosition(normalizedPosition);
			}
		}

		public void SelectFirstItem()
		{
			FirstSelectedItem = ActiveItems[0];
		}

		public void SetItemSelected(T_Item item, bool selected)
		{
			if (IsItemSelected(item) != selected)
			{
				T_Item firstSelectedItem = FirstSelectedItem;
				if (selected)
				{
					selectedItems.Add(item);
				}
				else
				{
					selectedItems.Remove(item);
				}
				ActivateSelectedButtons();
				OnSelectedItemsChanged();
				if (FirstSelectedItem != firstSelectedItem && SelectedItemChanged != null)
				{
					SelectedItemChanged(this, firstSelectedItem, FirstSelectedItem);
				}
				RaiseSelectedItemsChanged();
			}
		}

		public override void ClearSelection()
		{
			if (selectedItems.Count > 0)
			{
				selectedItems.Clear();
				OnSelectedItemsChanged();
				RaiseSelectedItemsChanged();
			}
		}

		public void SetSelectedItems(IEnumerable<T_Item> items)
		{
			selectedItems.Clear();
			selectedItems.AddRange(items);
			SuppressToggleSounds();
			try
			{
				OnSelectedItemsChanged();
				RaiseSelectedItemsChanged();
			}
			finally
			{
				ResumeToggleSounds();
			}
		}

		public override void SelectAll()
		{
			if (SelectedItemCount < TotalItemCount)
			{
				selectedItems.Clear();
				selectedItems.AddRange(activeItems);
				SuppressToggleSounds();
				try
				{
					OnSelectedItemsChanged();
					RaiseSelectedItemsChanged();
				}
				finally
				{
					ResumeToggleSounds();
				}
			}
		}

		public void AddToSelection(IEnumerable<T_Item> selectedFleets)
		{
			if (!CanEnableMultiSelect)
			{
				Debug.LogError("Multi select not enabled");
				return;
			}
			IsMultiSelectEnabled = true;
			bool flag = false;
			T_Item firstSelectedItem = FirstSelectedItem;
			foreach (T_Item selectedFleet in selectedFleets)
			{
				if (!activeItems.Contains(selectedFleet))
				{
					Debug.LogError("Specified item is not in the list");
				}
				else if (!IsItemSelected(selectedFleet))
				{
					selectedItems.Add(selectedFleet);
					flag = true;
				}
			}
			if (flag)
			{
				if (FirstSelectedItem != firstSelectedItem && SelectedItemChanged != null)
				{
					SelectedItemChanged(this, firstSelectedItem, FirstSelectedItem);
				}
				ActivateSelectedButtons();
				OnSelectedItemsChanged();
				RaiseSelectedItemsChanged();
			}
		}

		public bool IsItemSelected(T_Item item)
		{
			return selectedItems.Contains(item);
		}

		protected virtual void awake()
		{
		}

		protected virtual void start()
		{
		}

		protected virtual bool DetermineIsStale()
		{
			return false;
		}

		protected virtual void update()
		{
		}

		public void TickVisibleItems()
		{
			GetVisibleItemRange(out var startIndex, out var count);
			for (int i = 0; i < count; i++)
			{
				int num = startIndex + i;
				if (num < uiItemPool.Count)
				{
					uiItemPool[num].Tick();
				}
			}
		}

		public void RefreshVisibleItemsIfRequired()
		{
			if (scrollRect == null || scrollRect.verticalScrollbar == null || scrollRect.content.rect.height == 0f)
			{
				RefreshAllItems();
				return;
			}
			GetVisibleItemRange(out var startIndex, out var count);
			for (int i = 0; i < count; i++)
			{
				int num = startIndex + i;
				if (num >= 0 && num < activeItems.Count && ItemNeedsRefresh(num))
				{
					uiItemPool[num].Refresh();
					MarkItemAsRefreshed(num, refreshed: true);
				}
			}
		}

		private void MarkItemAsRefreshed(int index, bool refreshed)
		{
			refreshedItems[index] = refreshed;
		}

		private bool ItemNeedsRefresh(int index)
		{
			return !refreshedItems[index];
		}

		public void RefreshVisibleItems()
		{
			if (scrollRect == null || scrollRect.verticalScrollbar == null || scrollRect.content.rect.height == 0f)
			{
				RefreshAllItems();
				return;
			}
			GetVisibleItemRange(out var startIndex, out var count);
			for (int i = 0; i < count; i++)
			{
				int num = startIndex + i;
				if (num >= 0 && num < activeItems.Count)
				{
					uiItemPool[num].Refresh();
				}
			}
		}

		public void GetVisibleItemRange(out int startIndex, out int count)
		{
			startIndex = 0;
			count = 0;
			if (activeItems.Count <= 0 || !(scrollRect != null) || !(scrollRect.verticalScrollbar != null))
			{
				return;
			}
			float height = scrollRect.content.rect.height;
			if (height != 0f)
			{
				float height2 = ((RectTransform)transform).rect.height;
				int count2 = activeItems.Count;
				float num = height / (float)count2;
				count = Mathf.CeilToInt(height2 / num) + VisibleItemRangeFudge;
				float value = scrollRect.content.offsetMax.y / scrollRect.content.rect.height;
				startIndex = (int)(Mathf.Clamp01(value) * (float)count2);
				startIndex = Mathf.Clamp(startIndex, 0, activeItems.Count - 1);
				int num2 = activeItems.Count - startIndex;
				if (count > num2)
				{
					count = num2;
				}
			}
		}

		private List<T_Item> GetVisibleItems()
		{
			List<T_Item> list = new List<T_Item>();
			GetVisibleItemRange(out var startIndex, out var count);
			for (int i = startIndex; i < count; i++)
			{
				list.Add(activeItems[startIndex + i]);
			}
			return list;
		}

		protected virtual bool IsItemFiltered(T_Item item)
		{
			return true;
		}

		protected virtual void OnRefreshing()
		{
			RefreshVisibleItems();
		}

		public void RefreshAllItems()
		{
			for (int i = 0; i < activeItems.Count; i++)
			{
				uiItemPool[i].Refresh();
			}
		}

		protected override void OnSelectedItemsChanged()
		{
			for (int i = 0; i < activeItems.Count; i++)
			{
				if (uiItemPool[i].Toggle != null)
				{
					uiItemPool[i].Toggle.SetIsOnWithoutNotify(IsItemSelected(activeItems[i]));
				}
			}
		}

		private void CreatePool()
		{
			AddToPool(InitialPoolSize);
		}

		private void AddToPool(int count)
		{
			for (int i = 0; i < InitialPoolSize; i++)
			{
				AddItemToPool(CreateItem(null));
			}
		}

		private void AddItemToPool(ScrollListItem<T_Item> item)
		{
			uiItemPool.Add(item);
			refreshedItems.Add(item: false);
		}

		private void DeactivateUnusedPoolItems()
		{
			for (int i = activeItems.Count; i < uiItemPool.Count; i++)
			{
				uiItemPool[i].gameObject.SetActive(value: false);
			}
		}

		public void Awake()
		{
			if (isAwake)
			{
				return;
			}
			if (scrollRect == null)
			{
				scrollRect = GetComponent<ScrollRect>();
				if (scrollRect == null)
				{
					scrollRect = UnityObjectHelper.FindInParentsOrSelf<ScrollRect>(gameObject);
				}
			}
			if (scrollRect != null)
			{
				scrollRect.onValueChanged.AddListener(ScrollRectValueChanged);
			}
			engine = EngineASX.Instance;
			if (UsePool)
			{
				CreatePool();
			}
			DeactivateUnusedPoolItems();
			awake();
			isAwake = true;
			if (ToggleGroup != null && IsMultiSelectEnabled)
			{
				Debug.LogError("Cannot select multiple when using toggle group");
			}
			toggleGroupButtonSoundsController = GetComponent<ToggleGroupButtonSoundsController>();
			if (toggleGroupButtonSoundsController == null)
			{
				toggleGroupButtonSoundsController = gameObject.AddComponent<ToggleGroupButtonSoundsController>();
			}
		}

		private void ScrollRectValueChanged(Vector2 arg0)
		{
			RefreshVisibleItemsIfRequired();
		}

		private void Start()
		{
			start();
		}

		private void OnDisable()
		{
			CleanupOnDisable();
		}

		private void OnEnable()
		{
			if (RefreshOnEnable)
			{
				Debug.LogError(name + ": Relying on OnEnable to call is no longer guaranteed");
				Refresh();
			}
			if (ResetScrollPositionOnEnable)
			{
				ResetScrollPosition();
			}
		}

		public void TrySelectFirstItem()
		{
			if (activeItems.Count > 0)
			{
				FirstSelectedItem = activeItems[0];
			}
		}

		private void ActivateSelectedButtons()
		{
			try
			{
				suppressToggleValueChanged = true;
				bool allowSwitchOff = false;
				if (ToggleGroup != null)
				{
					allowSwitchOff = ToggleGroup.allowSwitchOff;
					ToggleGroup.allowSwitchOff = true;
				}
				SuppressToggleSounds();
				for (int i = 0; i < uiItemPool.Count; i++)
				{
					if (uiItemPool[i].Toggle != null)
					{
						uiItemPool[i].Toggle.isOn = IsItemSelected(uiItemPool[i].Item);
					}
				}
				if (ToggleGroup != null)
				{
					ToggleGroup.allowSwitchOff = allowSwitchOff;
				}
			}
			finally
			{
				suppressToggleValueChanged = false;
				ResumeToggleSounds();
			}
		}

		private void SuppressToggleSounds()
		{
			if (toggleGroupButtonSoundsController != null)
			{
				toggleGroupButtonSoundsController.PlayToggleSounds = false;
			}
		}

		private void ResumeToggleSounds()
		{
			if (toggleGroupButtonSoundsController != null)
			{
				toggleGroupButtonSoundsController.PlayToggleSounds = true;
			}
		}

		private void RefreshNoItemsLabel()
		{
			if (NoItemsLabel != null)
			{
				NoItemsLabel.gameObject.SetActive(activeItems.Count == 0);
			}
		}

		private ScrollListItem<T_Item> GetListItemForObject(T_Item c)
		{
			ScrollListItem<T_Item> scrollListItem = TryGetPooledListItem(c);
			if (!scrollListItem.gameObject.activeSelf)
			{
				scrollListItem.gameObject.SetActive(value: true);
			}
			scrollListItem.Item = c;
			scrollListItem.OnActiveInList();
			return scrollListItem;
		}

		public virtual GameObject GetItemPrefab(T_Item item)
		{
			return ItemPrefab;
		}

		private ScrollListItem<T_Item> CreateItem(T_Item item)
		{
			GameObject gameObject = Object.Instantiate(GetItemPrefab(item));
			ScrollListItem<T_Item> component = gameObject.GetComponent<ScrollListItem<T_Item>>();
			if (component != null)
			{
				component.ParentList = this;
				if (component.Toggle != null)
				{
					component.Toggle.group = ToggleGroup;
				}
				if (ItemContainer != null)
				{
					gameObject.transform.SetParent(ItemContainer.transform, worldPositionStays: false);
				}
				gameObject.transform.localPosition = Vector3.zero;
				gameObject.transform.localScale = Vector3.one;
			}
			else
			{
				Debug.LogError($"Error creating item for list {name}. Item prefab \"{ItemPrefab}\" did not have expected component type", this);
			}
			return component;
		}

		public virtual void CleanupOnDisable()
		{
			for (int i = 0; i < activeItems.Count; i++)
			{
				uiItemPool[i].CleanupOnDisable();
			}
			if (ClearOnDisable)
			{
				Debug.LogWarning(name + ": Relying on OnDisable to call is no longer guaranteed", this);
				activeItems.Clear();
				DeactivateUnusedPoolItems();
			}
			if (scrollRect != null)
			{
				scrollRect.velocity = Vector2.zero;
			}
		}

		public override void OnMultiSelectChanged()
		{
			if (IsMultiSelectEnabled || selectedItems.Count <= 1)
			{
				return;
			}
			if (AutoSelectFirstItem)
			{
				T_Item firstSelectedItem = FirstSelectedItem;
				selectedItems.Clear();
				if (firstSelectedItem != null)
				{
					selectedItems.Add(firstSelectedItem);
				}
				OnSelectedItemsChanged();
				RaiseSelectedItemsChanged();
			}
			else
			{
				ClearSelection();
			}
		}

		public override void DeselectAllExcept(ScrollListItemBase item)
		{
			base.DeselectAllExcept(item);
			foreach (T_Item item2 in selectedItems.ToList())
			{
				if ((object)GetActiveListItem(item2) != item)
				{
					SetItemSelected(item2, selected: false);
				}
			}
		}
	}
}
