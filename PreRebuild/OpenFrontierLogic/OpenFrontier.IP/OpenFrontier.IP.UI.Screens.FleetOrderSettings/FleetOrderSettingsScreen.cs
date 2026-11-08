using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.UI.Screens.EnterNumber;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.FleetOrderSettings
{
	public class FleetOrderSettingsScreen : EngineScreen
	{
		public Transform AvailableCreditsRoot;

		public Button EditAvailableCreditsButton;

		public TextMeshProUGUI AvailableCreditsLabel;

		public Toggle UnlimitedCreditsToggle;

		public Toggle RestrictedCreditsToggle;

		public Toggle HasMaxDurationToggle;

		public Toggle RequeueWhenCompletedToggle;

		public Slider MaxDurationSlider;

		public FleetOrder FleetOrder;

		public Fleet Fleet;

		public TextMeshProUGUI DurationTimeLabel;

		public TextMeshProUGUI TitleLabel;

		public double MinOrderDurationRealSeconds = 10.0;

		public double MaxOrderDurationRealSeconds = 43200.0;

		public Button OkButton;

		public float MaxDurationPower = 4f;

		public static double LerpDouble(double a, double b, double t)
		{
			return a + (b - a) * Clamp01(t);
		}

		public static double Clamp01(double a)
		{
			if (a < 0.0)
			{
				return 0.0;
			}
			if (a > 1.0)
			{
				return 1.0;
			}
			return a;
		}

		protected override void awake()
		{
			base.awake();
			MaxDurationSlider.onValueChanged.AddListener((float value) =>
			{
				if (FleetOrder != null && HasMaxDurationToggle.isOn)
				{
					FleetOrder.MaxDuration = GetMaxDurationRealSecondsFromSlider(value);
					RefreshMaxDurationSliderText();
				}
			});
			HasMaxDurationToggle.onValueChanged.AddListener((bool value) =>
			{
				if (FleetOrder != null)
				{
					if (value)
					{
						if (FleetOrder.MaxDuration <= 0f)
						{
							FleetOrder.MaxDuration = GetMaxDurationRealSecondsFromSlider(0f);
						}
						MaxDurationSlider.gameObject.SetActive(value: true);
					}
					else
					{
						FleetOrder.MaxDuration = 0f;
						MaxDurationSlider.gameObject.SetActive(value: false);
					}
					RefreshMaxDurationSliderText();
				}
			});
			RequeueWhenCompletedToggle.onValueChanged.AddListener((bool value) =>
			{
				if (FleetOrder != null)
				{
					if (value)
					{
						FleetOrder.CompletionMode = FleetOrderCompletionMode.Requeue;
					}
					else
					{
						FleetOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
					}
				}
			});
			OkButton.onClick.AddListener(OkButtonClick);
			EditAvailableCreditsButton.gameObject.SetActive(value: false);
			EditAvailableCreditsButton.onClick.AddListener(EditAvailableCreditsButtonClick);
		}

		private void OkButtonClick()
		{
			NavigateBack();
		}

		protected override void refresh()
		{
			base.refresh();
			if (FleetOrder != null)
			{
				HasMaxDurationToggle.isOn = FleetOrder.MaxDuration > 0f;
				MaxDurationSlider.gameObject.SetActive(FleetOrder.MaxDuration > 0f);
				MaxDurationSlider.value = GetMaxDurationRealSecondsSliderValueFromFleet();
				RefreshMaxDurationSliderText();
				TitleLabel.text = FleetOrder.GetDescriptionForFaction(EngineASX.Instance.LocalFaction, null);
				RequeueWhenCompletedToggle.isOn = FleetOrder.CompletionMode == FleetOrderCompletionMode.Requeue;
				RequeueWhenCompletedToggle.gameObject.SetActive(FleetOrder.IsRepeatable());
				RestrictedCreditsToggle.isOn = !FleetOrder.HasUnlimitedSpend;
				UnlimitedCreditsToggle.isOn = FleetOrder.HasUnlimitedSpend;
				RestrictedCreditsToggle.onValueChanged.RemoveAllListeners();
				RestrictedCreditsToggle.onValueChanged.AddListener(RestrictedCreditsToggleOnValueChanged);
				RefreshAvailableSpendLabel();
				RefreshEditAvailableSpendButton();
				AvailableCreditsRoot.gameObject.SetActive(FleetOrder.CanSpendCredits);
			}
		}

		private void RestrictedCreditsToggleOnValueChanged(bool value)
		{
			if (value)
			{
				FleetOrder.SetAsRestrictedCreditsSpend(Fleet);
				FleetOrder.Credits = 0;
				ShowEditAvailableCredits();
			}
			else
			{
				FleetOrder.HasUnlimitedSpend = true;
				ApplyChangeFleetOrderAvailableCredits(0);
				RefreshAvailableSpendLabel();
				RefreshEditAvailableSpendButton();
			}
		}

		private void ShowEditAvailableCredits()
		{
			UIController.Instance.ScreenNavigator.ShowEnterNumberScreen(FleetOrder.Credits, null, "Enter credits", (EnterNumberScreen screen) =>
			{
				screen.Min = 100;
				screen.Max = TotalAvailableCredits();
				screen.EnterNumberConfirmed += Screen_EnterNumberConfirmed;
			});
		}

		private int TotalAvailableCredits()
		{
			return FleetOrder.Credits + EngineASX.Instance.LocalFaction.Credits;
		}

		private void EditAvailableCreditsButtonClick()
		{
			ShowEditAvailableCredits();
		}

		private void Screen_EnterNumberConfirmed(EnterNumberScreen handler, bool enteredValue, int? newValue)
		{
			if (enteredValue)
			{
				if (newValue.Value > 0)
				{
					FleetOrder.SetAsRestrictedCreditsSpend(Fleet);
					int value = newValue.Value;
					ApplyChangeFleetOrderAvailableCredits(value);
					RefreshAvailableSpendLabel();
					RefreshEditAvailableSpendButton();
				}
			}
			else if (FleetOrder.HasUnlimitedSpend)
			{
				UnlimitedCreditsToggle.isOn = true;
			}
		}

		private void ApplyChangeFleetOrderAvailableCredits(int newCredits)
		{
			int creditsValue = -(newCredits - FleetOrder.Credits);
			FleetOrder.Credits = newCredits;
			EngineASX.Instance.LocalFaction.ApplyTransaction(creditsValue, FactionTransactionType.FleetTransfer, null, Fleet.LeaderUnit);
		}

		private void RefreshEditAvailableSpendButton()
		{
			EditAvailableCreditsButton.gameObject.SetActive(!FleetOrder.HasUnlimitedSpend);
		}

		private void RefreshAvailableSpendLabel()
		{
			if (FleetOrder.HasUnlimitedSpend)
			{
				AvailableCreditsLabel.text = string.Empty;
			}
			else
			{
				AvailableCreditsLabel.text = TextFormattingHelper.FormatCredits(FleetOrder.Credits, includeSuffix: true);
			}
		}

		private void RefreshMaxDurationSliderText()
		{
			DurationTimeLabel.text = GetMaxDurationSliderText();
		}

		private string GetMaxDurationSliderText()
		{
			if (FleetOrder.MaxDuration > 0f)
			{
				return EngineASX.Instance.DateTimeUtils.GetShortTimespanDescriptionFromRealSeconds(FleetOrder.MaxDuration);
			}
			return "-";
		}

		private float GetMaxDurationRealSecondsFromSlider(float value)
		{
			return (float)LerpDouble(MinOrderDurationRealSeconds, MaxOrderDurationRealSeconds, Mathf.Pow(value, MaxDurationPower));
		}

		private float GetMaxDurationRealSecondsSliderValueFromFleet()
		{
			return Mathf.Pow(Mathf.Clamp01((float)((double)FleetOrder.MaxDuration - MinOrderDurationRealSeconds) / (float)(MaxOrderDurationRealSeconds - MinOrderDurationRealSeconds)), 1f / MaxDurationPower);
		}
	}
}
