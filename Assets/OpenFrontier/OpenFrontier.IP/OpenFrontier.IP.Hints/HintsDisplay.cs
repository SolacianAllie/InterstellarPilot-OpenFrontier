using System.Collections.Generic;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Hints
{
	public class HintsDisplay : MonoBehaviour
	{
		public List<HintItem> Prefabs;

		private HintItem displayedItem;

		public Transform HintTransform;

		private void Update()
		{
			if (displayedItem == null)
			{
				displayedItem = GetNewDisplayItem();
			}
		}

		private HintItem GetNewDisplayItem()
		{
			Prefabs.TrimNulls();
			HintItem random = Prefabs.GetRandom();
			if (random != null)
			{
				HintItem hintItem = Object.Instantiate(random);
				hintItem.transform.SetParent(HintTransform, worldPositionStays: true);
				RectTransform component = hintItem.GetComponent<RectTransform>();
				component.anchorMin = Vector2.zero;
				component.anchorMax = Vector2.one;
				component.sizeDelta = Vector2.zero;
				component.anchoredPosition = Vector2.zero;
				hintItem.transform.localScale = Vector3.one;
				return hintItem;
			}
			return null;
		}
	}
}
