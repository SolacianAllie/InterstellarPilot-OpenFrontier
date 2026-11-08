using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.MessageBox;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.UIScale
{
	public class UIScaleScreen : ScreenBase
	{
		public SineTargetScaler AdjustmentSliderAnimationScaler;

		public Slider AdjustmentSlider;

		public Button ConfirmButton;

		private bool hasInitialised;

		public GameObject AdjustmentObjectsRoot;

		private bool isConfirming;

		private float confirmExpireTime;

		public float ConfirmTimeoutTime = 8f;

		protected override void awake()
		{
			base.awake();
			Reset();
			ConfirmButton.onClick.AddListener(ConfirmButtonClick);
		}

		private void Reset()
		{
			ConfirmButton.gameObject.SetActive(value: false);
			AdjustmentSliderAnimationScaler.enabled = true;
		}

		protected override void update()
		{
			base.update();
			if (!hasInitialised)
			{
				if (GameController.Instance != null)
				{
					Initialise();
				}
			}
			else if (isConfirming)
			{
				TimeoutConfirmationAfterTime();
			}
		}

		private void Initialise()
		{
			hasInitialised = true;
			AdjustmentObjectsRoot.SetActive(value: true);
			GameController.Instance.CanvasHeight01 = 1f - AdjustmentSlider.value;
			GameController.Instance.ScreenScaler.ScaleScreens();
			AdjustmentSlider.onValueChanged.AddListener(AdjustmentSliderValueChanged);
		}

		private void TimeoutConfirmationAfterTime()
		{
			if (RealTime.time > confirmExpireTime)
			{
				UIController.Instance.MessageBoxScreen.Cancel();
				isConfirming = false;
				Reset();
			}
		}

		private void ConfirmButtonClick()
		{
			GameController.Instance.SaveCanvasHeight();
			isConfirming = true;
			confirmExpireTime = RealTime.time + ConfirmTimeoutTime;
			UIController.Instance.ShowMessageBox("Keep this setting? Button sizes can be adjusted again from the Options screen", MessageBoxButtons.OkCancel, ConfirmDismiss, MessageBoxIcon.Question);
		}

		private void ConfirmDismiss(MessageBoxScreen sender, MessageBoxResult result)
		{
			isConfirming = false;
			if (result == MessageBoxResult.Ok)
			{
				UI.QuitToMainMenu();
			}
			else
			{
				Reset();
			}
		}

		private void AdjustmentSliderValueChanged(float arg0)
		{
			AdjustmentSliderAnimationScaler.enabled = false;
			if (GameController.Instance != null)
			{
				GameController.Instance.CanvasHeight01 = 1f - AdjustmentSlider.value;
				GameController.Instance.ScreenScaler.ScaleScreens();
				ConfirmButton.gameObject.SetActive(value: true);
			}
		}
	}
}
