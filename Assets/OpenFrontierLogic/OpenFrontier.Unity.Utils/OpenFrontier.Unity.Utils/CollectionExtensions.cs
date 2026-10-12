using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier.Unity.Utils
{
	public static class CollectionExtensions
	{
		public static TValue GetItemOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
		{
			if (dictionary.TryGetValue(key, out var value))
			{
				return value;
			}
			return default;
		}

		public static T ItemOrDefault<T>(this IList<T> list, int index)
		{
			if (index > -1 && index < list.Count)
			{
				return list[index];
			}
			return default;
		}

		public static void TrimNulls<T>(this IList<T> list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (object.Equals(list[i], null))
				{
					list.RemoveAt(i);
					i--;
				}
			}
		}

		public static T GetNextItem<T>(this IList<T> list, int step, T currentItem)
		{
			if (list.Count > 0)
			{
				if (currentItem == null || list.Count == 1)
				{
					return list[0];
				}
				int num = list.IndexOf(currentItem);
				num += step;
				if (num < 0 || num >= list.Count)
				{
					num = Maths.WrapValue(num, 0, list.Count);
				}
				return list[num];
			}
			return default;
		}

		public static T GetRandom<T>(this IEnumerable<T> list)
		{
			if (list != null)
			{
				int num = list.Count();
				if (num > 0)
				{
					return list.ElementAt(UnityEngine.Random.Range(0, num));
				}
			}
			return default;
		}

		public static T GetRandom<T>(this IEnumerable<T> list, System.Random random)
		{
			if (list != null)
			{
				int num = list.Count();
				if (num > 0)
				{
					return list.ElementAt(random.Next(0, num));
				}
			}
			return default;
		}

		public static T GetIEnumerableRandom<T>(this IEnumerable<T> collection)
		{
			if (collection != null)
			{
				int num = collection.Count();
				if (num > 0)
				{
					int num2 = 0;
					int num3 = UnityEngine.Random.Range(0, num);
					foreach (T item in collection)
					{
						if (num3 == num2)
						{
							return item;
						}
						num2++;
					}
				}
			}
			return default;
		}

		public static T GetRandom<T>(this IList<T> list)
		{
			if (list != null && list.Count > 0)
			{
				return list[UnityEngine.Random.Range(0, list.Count)];
			}
			return default;
		}

		public static T GetRandom<T>(this IList<T> list, int max)
		{
			if (list != null && list.Count > 0)
			{
				return list[UnityEngine.Random.Range(0, max)];
			}
			return default;
		}

		public static int IndexOf<T>(this IList<T> list, T item)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (object.Equals(list[i], item))
				{
					return i;
				}
			}
			return -1;
		}

		public static void Shuffle<T>(this IList<T> list)
		{
			List<T> list2 = list.ToList();
			int count = list.Count;
			for (int i = 0; i < count; i++)
			{
				int index = UnityEngine.Random.Range(0, list2.Count);
				list[i] = list2[index];
				list2.RemoveAt(index);
			}
		}

		public static void Shuffle<T>(this IList<T> list, System.Random random)
		{
			List<T> list2 = list.ToList();
			int count = list.Count;
			for (int i = 0; i < count; i++)
			{
				int index = random.Next(0, list2.Count);
				list[i] = list2[index];
				list2.RemoveAt(index);
			}
		}

		public static T GetRandomWeighted<T>(this IList<T> arr, int count = -1) where T : IWeighted
		{
			return GetRandomWeightedIndex(arr, count);
		}

		public static T GetRandomWeightedIndex<T>(IList<T> arr, int count = -1) where T : IWeighted
		{
			int num = ((count > -1) ? count : arr.Count);
			if (arr != null && num > 0)
			{
				if (num == 1)
				{
					return arr[0];
				}
				float num2 = 0f;
				for (int i = 0; i < num; i++)
				{
					num2 += arr[i].Weight;
				}
				float num3 = UnityEngine.Random.value * num2;
				float num4 = 0f;
				int j;
				for (j = 0; j < num - 1 && num3 > num4 + arr[j].Weight; j++)
				{
					num4 += arr[j].Weight;
				}
				return arr[j];
			}
			return default;
		}

		public static T GetRandomWeighted<T>(this IList<T> arr, System.Random random, int count = -1) where T : IWeighted
		{
			return GetRandomWeightedIndex(arr, random, count);
		}

		public static T GetRandomWeightedIndex<T>(IList<T> arr, System.Random random, int count = -1) where T : IWeighted
		{
			int num = ((count > -1) ? count : arr.Count);
			if (arr != null && num > 0)
			{
				if (num == 1)
				{
					return arr[0];
				}
				float num2 = 0f;
				for (int i = 0; i < num; i++)
				{
					num2 += arr[i].Weight;
				}
				float num3 = (float)random.NextDouble() * num2;
				float num4 = 0f;
				int j;
				for (j = 0; j < num - 1 && num3 > num4 + arr[j].Weight; j++)
				{
					num4 += arr[j].Weight;
				}
				return arr[j];
			}
			return default;
		}

		public static int GetRandomWeightedIndex(this IList<float> arr)
		{
			if (arr != null && arr.Count > 0)
			{
				if (arr.Count == 1)
				{
					return 0;
				}
				float num = 0f;
				for (int i = 0; i < arr.Count; i++)
				{
					num += arr[i];
				}
				float num2 = UnityEngine.Random.value * num;
				float num3 = 0f;
				int j;
				for (j = 0; j < arr.Count - 1 && num2 > num3 + arr[j]; j++)
				{
					num3 += arr[j];
				}
				return j;
			}
			return -1;
		}

		public static int GetRandomWeightedIndex(this float[] arr)
		{
			if (arr != null && arr.Length != 0)
			{
				if (arr.Length == 1)
				{
					return 0;
				}
				float num = 0f;
				for (int i = 0; i < arr.Length; i++)
				{
					num += arr[i];
				}
				float num2 = UnityEngine.Random.value * num;
				float num3 = 0f;
				int j;
				for (j = 0; j < arr.Length - 1 && num2 > num3 + arr[j]; j++)
				{
					num3 += arr[j];
				}
				return j;
			}
			return -1;
		}
	}
}
