using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.UI
{
	public class StatGeneratorUI<T> : MonoBehaviour
	{
		private struct ComponentStat
		{
			public string Header;

			public string Value;
		}

		public const int MaxStatsDisplayed = 20;

		private static int statCounter;

		public GameObject HeadersContainer;

		public T Item;

		public Text LabelPrefab;

		public Text LabelValuePrefab;

		public bool ReverseOrder;

		private ComponentStat[] statsCache = new ComponentStat[20];

		public GameObject ValuesContainer;

		private bool isRefreshing;

		private List<Text> labelHeaderCache = new List<Text>();

		private List<Text> labelValueCache = new List<Text>();

		public bool IsRefreshing => isRefreshing;

		private void ReturnItemsToCache(GameObject root, List<Text> cache)
		{
			Text[] componentsInChildren = root.GetComponentsInChildren<Text>(includeInactive: true);
			foreach (Text item in componentsInChildren)
			{
				if (!cache.Contains(item))
				{
					cache.Add(item);
				}
			}
		}

		public void Refresh()
		{
			isRefreshing = true;
			statCounter = 0;
			ReturnItemsToCache(HeadersContainer, labelHeaderCache);
			ReturnItemsToCache(ValuesContainer, labelValueCache);
			refresh();
			if (ReverseOrder)
			{
				for (int num = statCounter - 1; num >= 0; num--)
				{
					CreateLabelsFromStat(statsCache[num].Header, statsCache[num].Value, num);
				}
			}
			else
			{
				for (int i = 0; i < statCounter; i++)
				{
					CreateLabelsFromStat(statsCache[i].Header, statsCache[i].Value, i);
				}
			}
			DeactivateCachedLabels(HeadersContainer, labelHeaderCache);
			DeactivateCachedLabels(ValuesContainer, labelValueCache);
			isRefreshing = false;
		}

		private void DeactivateCachedLabels(GameObject root, List<Text> cache)
		{
			Text[] componentsInChildren = root.GetComponentsInChildren<Text>(includeInactive: true);
			foreach (Text text in componentsInChildren)
			{
				text.gameObject.SetActive(!cache.Contains(text));
			}
		}

		public void AddStat(string header, float val)
		{
			AddStat(header, val, 0);
		}

		public void AddStat(string header, float val, int decimalPlaces)
		{
			string text = "#,#0";
			if (decimalPlaces > 0)
			{
				text += ".";
				for (int i = 0; i < decimalPlaces; i++)
				{
					text += "0";
				}
			}
			AddStat(header, Math.Round(val, decimalPlaces).ToString(text));
		}

		public void AddStat(string header, string val)
		{
			if (statCounter < 20)
			{
				ComponentStat componentStat = new ComponentStat
				{
					Header = header,
					Value = val
				};
				statsCache[statCounter++] = componentStat;
			}
		}

		private Text GetOrCreateHeaderLabel()
		{
			return GetOrCreateLabel(LabelPrefab.gameObject, HeadersContainer.transform, labelHeaderCache);
		}

		private Text GetOrCreateValueLabel()
		{
			return GetOrCreateLabel(LabelValuePrefab.gameObject, ValuesContainer.transform, labelValueCache);
		}

		private Text GetOrCreateLabel(GameObject prefab, Transform root, List<Text> cache)
		{
			Text text = null;
			if (cache.Count > 0)
			{
				text = cache[0];
				cache.RemoveAt(0);
			}
			if (text == null)
			{
				text = UnityEngine.Object.Instantiate(prefab.gameObject).GetComponent<Text>();
				text.transform.SetParent(root);
				text.transform.localScale = prefab.transform.localScale;
			}
			return text;
		}

		public void CreateLabelsFromStat(string header, string value, int index)
		{
			Text orCreateHeaderLabel = GetOrCreateHeaderLabel();
			Text orCreateValueLabel = GetOrCreateValueLabel();
			orCreateHeaderLabel.text = header;
			orCreateValueLabel.text = value;
		}

		protected virtual void refresh()
		{
		}
	}
}
