using System;
using System.Collections;
using System.Collections.Generic;

namespace OpenFrontier.IP
{
	public class PriorityQueue<TValue, TPriority> : ICollection, IEnumerable, IEnumerable<PriorityQueueItem<TValue, TPriority>>
	{
		private const int DefaultCapacity = 16;

		private int capacity;

		private Comparison<TPriority> compareFunc;

		private PriorityQueueItem<TValue, TPriority>[] items;

		private int numItems;

		public int Count => numItems;

		public int Capacity
		{
			get
			{
				return items.Length;
			}
			set
			{
				SetCapacity(value);
			}
		}

		public bool IsSynchronized => false;

		public object SyncRoot => items.SyncRoot;

		public PriorityQueue()
			: this(16, (IComparer<TPriority>)Comparer<TPriority>.Default)
		{
		}

		public PriorityQueue(int initialCapacity)
			: this(initialCapacity, (IComparer<TPriority>)Comparer<TPriority>.Default)
		{
		}

		public PriorityQueue(IComparer<TPriority> comparer)
			: this(16, comparer)
		{
		}

		public PriorityQueue(int initialCapacity, IComparer<TPriority> comparer)
		{
			Init(initialCapacity, comparer.Compare);
		}

		public PriorityQueue(Comparison<TPriority> comparison)
			: this(16, comparison)
		{
		}

		public PriorityQueue(int initialCapacity, Comparison<TPriority> comparison)
		{
			Init(initialCapacity, comparison);
		}

		public void Enqueue(PriorityQueueItem<TValue, TPriority> newItem)
		{
			if (numItems == capacity)
			{
				SetCapacity(3 * Capacity / 2);
			}
			int num = numItems;
			numItems++;
			while (num > 0 && compareFunc(items[(num - 1) / 2].Priority, newItem.Priority) < 0)
			{
				items[num] = items[(num - 1) / 2];
				num = (num - 1) / 2;
			}
			items[num] = newItem;
		}

		public PriorityQueueItem<TValue, TPriority> GetItem(int index)
		{
			return items[index];
		}

		public void Enqueue(TValue value, TPriority priority)
		{
			Enqueue(new PriorityQueueItem<TValue, TPriority>(value, priority));
		}

		public bool VerifyQueue()
		{
			for (int i = 0; i < numItems / 2; i++)
			{
				int num = 2 * i + 1;
				int num2 = num + 1;
				if (compareFunc(items[i].Priority, items[num].Priority) < 0)
				{
					return false;
				}
				if (num2 < numItems && compareFunc(items[i].Priority, items[num2].Priority) < 0)
				{
					return false;
				}
			}
			return true;
		}

		public PriorityQueueItem<TValue, TPriority> Dequeue()
		{
			if (Count == 0)
			{
				throw new InvalidOperationException("The queue is empty");
			}
			return RemoveAt(0);
		}

		public void Remove(TValue item, IEqualityComparer comparer)
		{
			for (int i = 0; i < numItems; i++)
			{
				if (comparer.Equals(item, items[i].Value))
				{
					RemoveAt(i);
					break;
				}
			}
		}

		public void Remove(TValue item)
		{
			Remove(item, EqualityComparer<TValue>.Default);
		}

		public PriorityQueueItem<TValue, TPriority> Peek()
		{
			if (Count == 0)
			{
				throw new InvalidOperationException("The queue is empty");
			}
			return items[0];
		}

		public void Clear()
		{
			for (int i = 0; i < numItems; i++)
			{
				items[i] = default;
			}
			numItems = 0;
		}

		public void TrimExcess()
		{
			if ((float)numItems < 0.9f * (float)capacity)
			{
				SetCapacity(numItems);
			}
		}

		public bool Contains(TValue o)
		{
			PriorityQueueItem<TValue, TPriority>[] array = items;
			foreach (PriorityQueueItem<TValue, TPriority> priorityQueueItem in array)
			{
				if (priorityQueueItem.Value.Equals(o))
				{
					return true;
				}
			}
			return false;
		}

		public void CopyTo(PriorityQueueItem<TValue, TPriority>[] array, int arrayIndex)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (arrayIndex < 0)
			{
				throw new ArgumentOutOfRangeException("arrayIndex", "arrayIndex is less than 0.");
			}
			if (array.Rank > 1)
			{
				throw new ArgumentException("array is multidimensional.");
			}
			if (numItems != 0)
			{
				if (arrayIndex >= array.Length)
				{
					throw new ArgumentException("arrayIndex is equal to or greater than the length of the array.");
				}
				if (numItems > array.Length - arrayIndex)
				{
					throw new ArgumentException("The number of elements in the source ICollection is greater than the available space from arrayIndex to the end of the destination array.");
				}
				for (int i = 0; i < numItems; i++)
				{
					array[arrayIndex + i] = items[i];
				}
			}
		}

		public IEnumerator<PriorityQueueItem<TValue, TPriority>> GetEnumerator()
		{
			for (int i = 0; i < numItems; i++)
			{
				yield return items[i];
			}
		}

		private void Init(int initialCapacity, Comparison<TPriority> comparison)
		{
			numItems = 0;
			compareFunc = comparison;
			SetCapacity(initialCapacity);
		}

		private void SetCapacity(int newCapacity)
		{
			int num = newCapacity;
			if (num < 16)
			{
				num = 16;
			}
			if (num < numItems)
			{
				throw new ArgumentOutOfRangeException("newCapacity", "New capacity is less than Count");
			}
			capacity = num;
			if (items == null)
			{
				items = new PriorityQueueItem<TValue, TPriority>[num];
			}
			else
			{
				Array.Resize(ref items, num);
			}
		}

		private PriorityQueueItem<TValue, TPriority> RemoveAt(int index)
		{
			PriorityQueueItem<TValue, TPriority> result = items[index];
			numItems--;
			PriorityQueueItem<TValue, TPriority> priorityQueueItem = items[numItems];
			items[numItems] = default;
			if (numItems > 0 && index != numItems)
			{
				int num = index;
				int num2 = (num - 1) / 2;
				while (compareFunc(priorityQueueItem.Priority, items[num2].Priority) > 0)
				{
					items[num] = items[num2];
					num = num2;
					num2 = (num - 1) / 2;
				}
				if (num == index)
				{
					while (num < numItems / 2)
					{
						int num3 = 2 * num + 1;
						if (num3 < numItems - 1 && compareFunc(items[num3].Priority, items[num3 + 1].Priority) < 0)
						{
							num3++;
						}
						if (compareFunc(items[num3].Priority, priorityQueueItem.Priority) <= 0)
						{
							break;
						}
						items[num] = items[num3];
						num = num3;
					}
				}
				items[num] = priorityQueueItem;
			}
			return result;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void CopyTo(Array array, int index)
		{
			CopyTo((PriorityQueueItem<TValue, TPriority>[])array, index);
		}
	}
}
