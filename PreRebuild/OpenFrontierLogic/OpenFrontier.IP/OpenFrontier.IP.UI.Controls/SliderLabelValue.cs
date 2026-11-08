using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Controls
{
	[RequireComponent(typeof(Slider))]
	public class SliderLabelValue : MonoBehaviour
	{
		public string CustomFormat;

		public TextMeshProUGUI Label;

		private Slider slider;

		private void Awake()
		{
			slider = GetComponent<Slider>();
			slider.onValueChanged.AddListener((float value) =>
			{
				RefreshLabel(slider, value);
			});
			RefreshLabel(slider, slider.value);
		}

		// Open Frontier: refresh the label manually - needed when the slider
		// value is set via SetValueWithoutNotify (which fires no events).
		public void Refresh()
		{
			RefreshLabel(slider, slider.value);
		}

		private void RefreshLabel(Slider slider, float value)
		{
			if (!string.IsNullOrWhiteSpace(CustomFormat))
			{
				Label.text = value.ToString(CustomFormat);
			}
			else if (slider.wholeNumbers)
			{
				Label.text = $"{value:N0}";
			}
			else
			{
				Label.text = $"{value:N1}";
			}
		}
	}
}
