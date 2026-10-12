using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.SectorMap
{
	public class SectorMapLabelDrawer : MonoBehaviour
	{
		public SectorMap SectorMap;

		public TextMeshProUGUI LabelPrefab;

		private List<TextMeshProUGUI> pool = new List<TextMeshProUGUI>();

		private List<TextMeshProUGUI> activeLabels = new List<TextMeshProUGUI>();

		public Transform LabelHolderTransform;

		public Vector3 LabelOffsetFudge = Vector3.zero;

		public void Tick()
		{
			ClearLabels();
			if (!SectorMap.ShowLabelsToggle.isOn)
			{
				return;
			}
			foreach (SectorMapItem item in SectorMap.Items)
			{
				if (item is SectorMapUnitItem { ShowLabel: not false } sectorMapUnitItem)
				{
					DrawLabel(sectorMapUnitItem);
				}
			}
		}

		private void ClearLabels()
		{
			foreach (TextMeshProUGUI activeLabel in activeLabels)
			{
				activeLabel.enabled = false;
			}
			activeLabels.Clear();
		}

		private void DrawLabel(SectorMapUnitItem unitItem)
		{
			TextMeshProUGUI label = GetLabel();
			label.enabled = true;
			label.text = unitItem.GetLabelText();
			float unitBracketsDrawSize = SectorMap.GetUnitBracketsDrawSize(unitItem.Unit);
			label.transform.localPosition = unitItem.transform.localPosition + LabelOffsetFudge + new Vector3(unitBracketsDrawSize / 2f, 0f, 0f);
		}

		private TextMeshProUGUI GetLabel()
		{
			if (activeLabels.Count < pool.Count)
			{
				TextMeshProUGUI textMeshProUGUI = pool[activeLabels.Count];
				activeLabels.Add(textMeshProUGUI);
				return textMeshProUGUI;
			}
			TextMeshProUGUI textMeshProUGUI2 = InstantiateLabel();
			pool.Add(textMeshProUGUI2);
			activeLabels.Add(textMeshProUGUI2);
			return textMeshProUGUI2;
		}

		private TextMeshProUGUI InstantiateLabel()
		{
			TextMeshProUGUI textMeshProUGUI = Object.Instantiate(LabelPrefab, Vector3.zero, Quaternion.identity, LabelHolderTransform);
			textMeshProUGUI.transform.localScale = Vector3.one;
			return textMeshProUGUI;
		}
	}
}
