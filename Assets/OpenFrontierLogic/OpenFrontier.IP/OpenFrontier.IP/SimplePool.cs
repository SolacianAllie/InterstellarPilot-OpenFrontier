using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP
{
	public class SimplePool<T> where T : Component
	{
		private List<T> activeItems = new List<T>();

		private List<T> inactiveItems = new List<T>();

		public GameObject PoolRoot;

		public T Prefab;

		public List<T> ActiveItems => activeItems;

		public void AddItemsToPool(int count)
		{
			for (int i = 0; i < count; i++)
			{
				T item = CreateItem();
				inactiveItems.Add(item);
			}
		}

		public T GetOrCreateItem()
		{
			T val = null;
			while (val == null && inactiveItems.Count > 0)
			{
				val = inactiveItems[0];
				inactiveItems.RemoveAt(0);
			}
			if (val == null)
			{
				val = CreateItem();
			}
			activeItems.Add(val);
			return val;
		}

		public void RepoolItem(T item)
		{
			if (item != null)
			{
				activeItems.Remove(item);
				inactiveItems.Add(item);
			}
		}

		private T CreateItem()
		{
			T val = UnityObjectHelper.InstantiateAndGetComponent(Prefab);
			if (PoolRoot != null)
			{
				val.transform.SetParent(PoolRoot.transform);
				val.transform.localPosition = Vector3.zero;
			}
			return val;
		}
	}
}
