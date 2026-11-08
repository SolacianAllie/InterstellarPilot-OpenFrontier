using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.FleetSettings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Orders
{
	public class StanceSettingsController : MonoBehaviour
	{
		public float MinInterceptionDistance = 1000f;

		public float MaxInterceptionDistance = 20000f;

		private IEnumerable<Fleet> fleets;

		public Slider AggressionSlider;

		public Slider MaxInterceptionDistanceSlider;

		public Toggle CanAttackToggle;

		public Toggle CanInterceptToggle;

		public TextMeshProUGUI InterceptionDistanceLabel;

		public TextMeshProUGUI CanAttackToggleLabel;

		public TextMeshProUGUI CanInterceptToggleLabel;

		public TextMeshProUGUI AggressionLabel;

		public IEnumerable<Fleet> Fleets
		{
			get
			{
				return fleets;
			}
			set
			{
				fleets = value;
			}
		}

		private Fleet SingleFleet
		{
			get
			{
				if (fleets.Count() == 1)
				{
					return fleets.First();
				}
				return null;
			}
		}

		public event SettingValueChangedHandler SettingValueChanged;

		private void RaiseValueChanged()
		{
			if (SettingValueChanged != null)
			{
				SettingValueChanged();
			}
		}

		public void Init(IEnumerable<Fleet> fleets)
		{
			RemoveListeners();
			Fleets = fleets;
			PopulateControls();
			AddListeners();
			Refresh();
		}

		private void RemoveListeners()
		{
			AggressionSlider.onValueChanged.RemoveAllListeners();
			MaxInterceptionDistanceSlider.onValueChanged.RemoveAllListeners();
			CanInterceptToggle.onValueChanged.RemoveAllListeners();
			CanAttackToggle.onValueChanged.RemoveAllListeners();
		}

		private void AddListeners()
		{
			AggressionSlider.onValueChanged.AddListener(AggressionSliderValueChanged);
			MaxInterceptionDistanceSlider.onValueChanged.AddListener(MaxInterceptionDistanceSliderValueChanged);
			CanInterceptToggle.onValueChanged.AddListener(CanInterceptToggleValueChanged);
			CanAttackToggle.onValueChanged.AddListener(CanAttackToggleValueChanged);
		}

		private void CanInterceptToggleValueChanged(bool value)
		{
			foreach (Fleet fleet in fleets)
			{
				if (fleet != null)
				{
					fleet.Settings.AllowCombatInterception = value;
				}
			}
			RaiseValueChanged();
			RefreshCanInterceptLabel();
			RefreshMaxInterceptionDistanceSliderVisible();
		}

		private void CanAttackToggleValueChanged(bool value)
		{
			foreach (Fleet fleet in fleets)
			{
				if (fleet != null)
				{
					fleet.Settings.AllowAttack = value;
				}
			}
			RaiseValueChanged();
			RefreshCanAttackLabel();
			RefreshMaxInterceptionDistanceSliderVisible();
			RefreshInterceptionToggleVisible();
			RefreshAggressionSliderVisible();
		}

		private void AggressionSliderValueChanged(float value)
		{
			foreach (Fleet fleet in fleets)
			{
				if (fleet != null)
				{
					fleet.Settings.Aggression = value;
				}
			}
			RaiseValueChanged();
			RefreshAggressionLabel();
		}

		private void MaxInterceptionDistanceSliderValueChanged(float value)
		{
			foreach (Fleet fleet in fleets)
			{
				if (fleet != null)
				{
					fleet.Settings.TargetInterceptionLowerDistance = Mathf.Lerp(MinInterceptionDistance, MaxInterceptionDistance, value);
					fleet.Settings.TargetInterceptionUpperDistance = fleet.Settings.TargetInterceptionLowerDistance * 1.2f;
				}
			}
			RaiseValueChanged();
			RefreshInterceptionDistanceLabel();
		}

		public void Refresh()
		{
			RefreshInterceptionDistanceLabel();
			RefreshAggressionLabel();
			RefreshCanAttackLabel();
			RefreshCanInterceptLabel();
			RefreshMaxInterceptionDistanceSliderVisible();
			RefreshInterceptionToggleVisible();
			RefreshAggressionSliderVisible();
		}

		private void RefreshMaxInterceptionDistanceSliderVisible()
		{
			MaxInterceptionDistanceSlider.gameObject.SetActive(CanAttackToggle.isOn && CanInterceptToggle.isOn);
		}

		private void RefreshInterceptionToggleVisible()
		{
			CanInterceptToggle.gameObject.SetActive(CanAttackToggle.isOn);
		}

		private void RefreshAggressionSliderVisible()
		{
			AggressionSlider.gameObject.SetActive(CanAttackToggle.isOn);
		}

		private void RefreshCanInterceptLabel()
		{
			CanInterceptToggleLabel.text = GetCanInterceptLabel();
		}

		private string GetCanInterceptLabel()
		{
			bool firstValue = fleets.First().Settings.AllowCombatInterception;
			if (fleets.Where((Fleet e) => e != null).All((Fleet e) => e.Settings.AllowCombatInterception == firstValue))
			{
				return string.Empty;
			}
			return "[Multiple]";
		}

		private void RefreshAggressionLabel()
		{
			AggressionLabel.text = GetAggressionLabel();
		}

		private string GetAggressionLabel()
		{
			float firstValue = fleets.First().Settings.Aggression;
			if (fleets.Where((Fleet e) => e != null).All((Fleet e) => e.Settings.Aggression == firstValue))
			{
				return string.Empty;
			}
			return "[Multiple]";
		}

		private void RefreshCanAttackLabel()
		{
			CanAttackToggleLabel.text = GetAttackLabelText();
		}

		private string GetAttackLabelText()
		{
			bool firstValue = fleets.First().Settings.AllowAttack;
			if (fleets.Where((Fleet e) => e != null).All((Fleet e) => e.Settings.AllowAttack == firstValue))
			{
				return string.Empty;
			}
			return "[Multiple]";
		}

		private void RefreshInterceptionDistanceLabel()
		{
			InterceptionDistanceLabel.text = GetInterceptionDistanceText();
		}

		private float? GetSingleInterceptionDistance()
		{
			float? num = null;
			foreach (Fleet fleet in fleets)
			{
				if (fleet != null && fleet.FleetFormation != null)
				{
					if (!num.HasValue)
					{
						num = fleet.Settings.TargetInterceptionLowerDistance;
					}
					else if (num != fleet.Settings.TargetInterceptionLowerDistance)
					{
						return null;
					}
				}
			}
			return num;
		}

		private string GetInterceptionDistanceText()
		{
			Fleet singleFleet = SingleFleet;
			if (singleFleet != null)
			{
				return FormatInterceptionDistance(singleFleet.Settings.TargetInterceptionLowerDistance);
			}
			float? singleInterceptionDistance = GetSingleInterceptionDistance();
			if (singleInterceptionDistance.HasValue)
			{
				return FormatInterceptionDistance(singleInterceptionDistance.Value);
			}
			return "[Multiple]";
		}

		private string FormatInterceptionDistance(float lowerDistance)
		{
			return TextFormattingHelper.FormatDistance(Mathf.Clamp(lowerDistance, 0f, MaxInterceptionDistance));
		}

		private void PopulateControls()
		{
			if (fleets.Any())
			{
				Fleet fleet = fleets.First();
				if (fleet != null)
				{
					CanAttackToggle.isOn = fleet.Settings.AllowAttack;
					CanInterceptToggle.isOn = fleet.Settings.AllowCombatInterception;
					AggressionSlider.value = fleet.Settings.Aggression;
					MaxInterceptionDistanceSlider.value = Mathf.Clamp01((fleet.Settings.TargetInterceptionLowerDistance - MinInterceptionDistance) / (MaxInterceptionDistance - MinInterceptionDistance));
				}
			}
		}
	}
}
