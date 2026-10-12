using System;
using OpenFrontier.IP.Engine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.AdjustSafeArea
{
	public class AdjustSafeAreaScreen : ScreenBase
	{
		public Action AdjustedCallback;

		public Slider TopSllider;

		public Slider BottomSlider;

		public Slider LeftSlider;

		public Slider RightSlider;

		public Button ConfirmButton;

		protected override void awake()
		{
			base.awake();
			TopSllider.value = (GameController.Instance.UISafeAreaTop - GameController.Instance.MinUIScaleValue) / (GameController.Instance.MaxUIScaleValue - GameController.Instance.MinUIScaleValue);
			BottomSlider.value = (GameController.Instance.UISafeAreaBottom - GameController.Instance.MinUIScaleValue) / (GameController.Instance.MaxUIScaleValue - GameController.Instance.MinUIScaleValue);
			LeftSlider.value = (GameController.Instance.UISafeAreaLeft - GameController.Instance.MinUIScaleValue) / (GameController.Instance.MaxUIScaleValue - GameController.Instance.MinUIScaleValue);
			RightSlider.value = (GameController.Instance.UISafeAreaRight - GameController.Instance.MinUIScaleValue) / (GameController.Instance.MaxUIScaleValue - GameController.Instance.MinUIScaleValue);
			TopSllider.onValueChanged.AddListener(TopSliderValueChanged);
			BottomSlider.onValueChanged.AddListener(BottomSliderValueChanged);
			LeftSlider.onValueChanged.AddListener(LeftSliderValueChanged);
			RightSlider.onValueChanged.AddListener(RightSliderValueChanged);
			ConfirmButton.onClick.AddListener(ConfirmButtonClick);
		}

		private void TopSliderValueChanged(float value)
		{
			GameController.Instance.SetUISafeAreaTopFrom01(value);
			GameController.Instance.ApplyUISafeArea();
		}

		private void BottomSliderValueChanged(float value)
		{
			GameController.Instance.SetUISafeAreaBottomFrom01(value);
			GameController.Instance.ApplyUISafeArea();
		}

		private void LeftSliderValueChanged(float value)
		{
			GameController.Instance.SetUISafeAreaLeftFrom01(value);
			GameController.Instance.ApplyUISafeArea();
		}

		private void RightSliderValueChanged(float value)
		{
			GameController.Instance.SetUISafeAreaRightFrom01(value);
			GameController.Instance.ApplyUISafeArea();
		}

		private void ConfirmButtonClick()
		{
			if (AdjustedCallback != null)
			{
				AdjustedCallback();
			}
			else
			{
				NavigateBack();
			}
		}
	}
}
