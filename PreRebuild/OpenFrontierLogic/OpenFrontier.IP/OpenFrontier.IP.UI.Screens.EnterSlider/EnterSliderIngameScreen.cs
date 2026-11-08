using System;
using TMPro;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.EnterSlider
{
	public class EnterSliderIngameScreen : EngineScreen
	{
		public delegate void EnterSliderConfirmedHandler(EnterSliderIngameScreen handler, bool enteredValue, float newValue);

		public Slider Slider;

		public Button OkButton;

		public Button CancelButton;

		public TextMeshProUGUI PromptText;

		public TextMeshProUGUI CurrentValueText;

		public bool NavigateBackOnOk = true;

		public Func<float, string> GetSliderValueText { get; set; } = (float value) => $"{value:N2}";

		public event EnterSliderConfirmedHandler EnterSliderConfirmed;

		protected override void awake()
		{
			base.awake();
			OkButton.onClick.AddListener(OkButtonClick);
			CancelButton.onClick.AddListener(CancelButtonClick);
		}

		protected override void refresh()
		{
			base.refresh();
			Slider.onValueChanged.AddListener((float value) =>
			{
				RefreshCurrentValueText(value);
			});
			RefreshCurrentValueText(Slider.value);
		}

		private void RefreshCurrentValueText(float value)
		{
			CurrentValueText.text = GetSliderValueText(value);
		}

		protected override void update()
		{
			base.update();
			OkButton.interactable = ValidateInput();
		}

		private bool ValidateInput()
		{
			return true;
		}

		private void OkButtonClick()
		{
			if (EnterSliderConfirmed != null)
			{
				EnterSliderConfirmed(this, enteredValue: true, Slider.value);
			}
			if (NavigateBackOnOk)
			{
				NavigateBack();
			}
		}

		private void CancelButtonClick()
		{
			if (EnterSliderConfirmed != null)
			{
				EnterSliderConfirmed(this, enteredValue: false, 0f);
			}
			NavigateBack();
		}
	}
}
