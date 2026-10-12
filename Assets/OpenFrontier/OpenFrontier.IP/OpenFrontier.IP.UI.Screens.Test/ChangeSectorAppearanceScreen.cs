using System;
using System.Collections.Generic;
using System.Globalization;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Controls;
using OpenFrontier.IP.UI.Screens.EnterNumber;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.UI.Screens.Test
{
	public class ChangeSectorAppearanceScreen : EngineScreen
	{
		private class NebulaColourToggle
		{
			public Toggle Toggle { get; set; }

			public NebulaColour NebulaColour { get; set; }
		}

		public Button SetSeedButton;

		public Button RandomizeButton;

		public Toggle CustomSettingsToggle;

		public Transform CustomSettingsTransform;

		public Transform NebulaColourTogglesRoot;

		public Toggle NebulaTogglePrefab;

		public TextMeshProUGUI CurrentSeedValueLabel;

		public Slider StarsIntensitySlider;

		public Slider NebulaCountSlider;

		// Open Frontier: sun HSV - writes the sector's DirectionLightColor,
		// which drives both the sun visuals and the scene lighting.
		public Slider SunHueSlider;

		public Slider SunSaturationSlider;

		public Slider SunValueSlider;

		private List<NebulaColourToggle> nebulaColourToggles = new List<NebulaColourToggle>();

		protected override void awake()
		{
			base.awake();
			RandomizeButton.onClick.AddListener(RandomizeButtonClick);
			SetSeedButton.onClick.AddListener(SetSeedButtonClick);
			CustomSettingsToggle.onValueChanged.AddListener(CustomSettingsToggleOnValueChanged);
			CreateNebulaColourToggles();
			StarsIntensitySlider.minValue = 0f;
			StarsIntensitySlider.maxValue = 2f;
			StarsIntensitySlider.onValueChanged.AddListener(StarsIntensitySliderValueChanged);
			NebulaCountSlider.wholeNumbers = true;
			NebulaCountSlider.minValue = 0f;
			NebulaCountSlider.maxValue = 64f;
			NebulaCountSlider.onValueChanged.AddListener(NebulaCountSliderValueChanged);
			SunHueSlider.minValue = 0f;
			SunHueSlider.maxValue = 1f;
			SunHueSlider.onValueChanged.AddListener(SunHsvSliderValueChanged);
			SunSaturationSlider.minValue = 0f;
			SunSaturationSlider.maxValue = 1f;
			SunSaturationSlider.onValueChanged.AddListener(SunHsvSliderValueChanged);
			// Open Frontier: floor the slider at the same minimum the
			// generator uses - all stars have brightness >= 1.
			SunValueSlider.minValue = 1f;
			SunValueSlider.maxValue = 3f;
			SunValueSlider.onValueChanged.AddListener(SunHsvSliderValueChanged);
		}

		private void RandomizeButtonClick()
		{
			EngineASX.Instance.ActiveSector.GenerateSpaceBackgroundWithNewSeed();
			// Open Frontier: reseed the sun along with the background, and
			// show the new star's values on the sliders.
			EngineASX.Instance.ActiveSector.DirectionLightColor = StarColorGenerator.ForSector(EngineASX.Instance.ActiveSector.RandomSeed);
			ApplySunToCurrentSunBillboard();
			RefreshSunSliders();
			RefreshCurrentSeedText();
			SpaceReflectionProbe.Instance?.RequestRender();
		}

		private void RefreshSunSliders()
		{
			StarColorGenerator.RgbToHsv(EngineASX.Instance.ActiveSector.DirectionLightColor, out float h, out float s, out float v);
			SunHueSlider.SetValueWithoutNotify(h);
			SunSaturationSlider.SetValueWithoutNotify(s);
			SunValueSlider.SetValueWithoutNotify(Mathf.Clamp(v, SunValueSlider.minValue, SunValueSlider.maxValue));
			// SetValueWithoutNotify fires no events, so the value labels
			// need a manual nudge.
			SunHueSlider.GetComponent<SliderLabelValue>()?.Refresh();
			SunSaturationSlider.GetComponent<SliderLabelValue>()?.Refresh();
			SunValueSlider.GetComponent<SliderLabelValue>()?.Refresh();
		}

		private void SunHsvSliderValueChanged(float value)
		{
			Color color = StarColorGenerator.HsvToRgb(SunHueSlider.value, SunSaturationSlider.value, SunValueSlider.value);
			EngineASX.Instance.ActiveSector.DirectionLightColor = color;
			ApplySunToCurrentSunBillboard();
			SpaceReflectionProbe.Instance?.RequestRender();
		}

		private void ApplySunToCurrentSunBillboard()
		{
			if (EngineASX.Instance.DirectionalLight != null)
			{
				SunBillboard component = EngineASX.Instance.DirectionalLight.GetComponent<SunBillboard>();
				if (component != null)
				{
					component.StarTint = StarColorGenerator.NormalizeStarColor(EngineASX.Instance.ActiveSector.DirectionLightColor);
				}
			}
		}

		private void SetSeedButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowEnterNumberScreen(EngineASX.Instance.ActiveSector.RandomSeed, EnterSectorSeedValueConfirm, "Enter Sector Seed Value...");
		}

		private void StarsIntensitySliderValueChanged(float value)
		{
			EngineASX.Instance.ActiveSector.GetOrCreateCustomAppearanceSettings().SpaceConstructorParams.StarsIntensity = value;
			EngineASX.Instance.SpaceConstructor.StaticStars.starsIntensity = value;
			EngineASX.Instance.SpaceConstructor.StaticStars.UpdateMaterial();
			SpaceReflectionProbe.Instance?.RequestRender();
		}

		private void NebulaCountSliderValueChanged(float value)
		{
			EngineASX.Instance.ActiveSector.GetOrCreateCustomAppearanceSettings().SpaceConstructorParams.NebulaCount = (int)value;
			SpaceReflectionProbe.Instance?.RequestRender();
		}

		private void EnterSectorSeedValueConfirm(EnterNumberScreen handler, bool enteredValue, int? newValue)
		{
			if (enteredValue && newValue.HasValue && newValue.Value != EngineASX.Instance.ActiveSector.RandomSeed)
			{
				EngineASX.Instance.ActiveSector.RandomSeed = newValue.Value;
				EngineASX.Instance.ActiveSector.GenerateSpaceBackground();
				RefreshCurrentSeedText();
			}
		}

		private void CreateNebulaColourToggles()
		{
			nebulaColourToggles.Clear();
			foreach (NebulaColour nebulaColour in Enum.GetValues(typeof(NebulaColour)))
			{
				if (nebulaColour <= NebulaColour.NONE)
				{
					continue;
				}
				Toggle toggle = UnityEngine.Object.Instantiate(NebulaTogglePrefab);
				toggle.transform.SetParent(NebulaColourTogglesRoot);
				toggle.transform.localScale = Vector3.one;
				toggle.SetText(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(nebulaColour.ToString()));
				toggle.onValueChanged.AddListener((bool value) =>
				{
					CustomSectorAppearance orCreateCustomAppearanceSettings = EngineASX.Instance.ActiveSector.GetOrCreateCustomAppearanceSettings();
					if (value)
					{
						orCreateCustomAppearanceSettings.SpaceConstructorParams.NebulaColors = orCreateCustomAppearanceSettings.SpaceConstructorParams.NebulaColors | nebulaColour;
					}
					else
					{
						orCreateCustomAppearanceSettings.SpaceConstructorParams.NebulaColors &= ~nebulaColour;
					}
				});
				nebulaColourToggles.Add(new NebulaColourToggle
				{
					Toggle = toggle,
					NebulaColour = nebulaColour
				});
			}
		}

		private void CustomSettingsToggleOnValueChanged(bool value)
		{
			if (value)
			{
				EngineASX.Instance.ActiveSector.GetOrCreateCustomAppearanceSettings();
				Refresh();
			}
			else
			{
				EngineASX.Instance.ActiveSector.RemoveCustomSectorAppearanceSettings();
				CustomSettingsTransform.gameObject.SetActive(value: false);
			}
		}

		private void RefreshCustomSettingsVisible()
		{
			CustomSettingsTransform.gameObject.SetActive(EngineASX.Instance.ActiveSector.HasCustomAppearanceSettings);
		}

		protected override void onMadeCurrentPanel(bool navigatedForward)
		{
			base.onMadeCurrentPanel(navigatedForward);
			if (navigatedForward && EngineASX.Instance.LocalUnit != null)
			{
				EngineASX.Instance.CameraOrbitPlayerUnit();
			}
		}

		protected override void refresh()
		{
			base.refresh();
			if (EngineASX.Instance.ActiveSector != null)
			{
				CustomSectorAppearance component = EngineASX.Instance.ActiveSector.GetComponent<CustomSectorAppearance>();
				CustomSettingsToggle.isOn = component != null;
				if (component != null)
				{
					RefreshNebulaColourToggles(component);
					StarsIntensitySlider.value = (component.SpaceConstructorParams.StarsIntensity - StarsIntensitySlider.minValue) / (StarsIntensitySlider.maxValue - StarsIntensitySlider.minValue);
					NebulaCountSlider.value = component.SpaceConstructorParams.NebulaCount;
				}
				RefreshSunSliders();
				RefreshCustomSettingsVisible();
				RefreshCurrentSeedText();
			}
		}

		private void RefreshCurrentSeedText()
		{
			CurrentSeedValueLabel.text = EngineASX.Instance.ActiveSector.RandomSeed.ToString();
		}

		private void RefreshNebulaColourToggles(CustomSectorAppearance customSector)
		{
			foreach (NebulaColourToggle nebulaColourToggle in nebulaColourToggles)
			{
				nebulaColourToggle.Toggle.isOn = (nebulaColourToggle.NebulaColour & customSector.SpaceConstructorParams.NebulaColors) != 0;
			}
		}
	}
}
