using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class DifficultySlider : MonoBehaviour
	{
		public Text DifficultyLevelLabel;

		public Slider Slider;

		public TextMeshProUGUI DifficultyLevelText;

		public void Init()
		{
			SetupSlider();
			Slider.onValueChanged.RemoveAllListeners();
			Slider.onValueChanged.AddListener(Slider_ValueChanged);
		}

		public void SetGameDifficultyFromSliderValue()
		{
			if (!(GameController.Instance != null))
			{
				return;
			}
			int difficultySliderIndex = GetDifficultySliderIndex();
			if (difficultySliderIndex != GameController.Instance.CombatDifficultyLevelIndex)
			{
				if (difficultySliderIndex >= 0 && difficultySliderIndex < GameController.Instance.CombatDifficultyLevels.Length)
				{
					GameController.Instance.CombatDifficultyLevelIndex = difficultySliderIndex;
				}
				else
				{
					Debug.LogWarning("Trying to set invalid difficulty index: " + difficultySliderIndex, this);
				}
			}
		}

		public int GetDifficultySliderIndex()
		{
			return (int)Slider.value;
		}

		private void SetupSlider()
		{
			Slider.wholeNumbers = true;
			Slider.minValue = 0f;
			Slider.maxValue = GameController.Instance.CombatDifficultyLevels.Length - 1;
		}

		public void SetDifficultyLevelIndex(int index)
		{
			Slider.value = index;
			RefreshDifficultyLevelLabel();
		}

		private void Slider_ValueChanged(float newValue)
		{
			SetGameDifficultyFromSliderValue();
			RefreshDifficultyLevelLabel();
		}

		private void RefreshDifficultyLevelLabel()
		{
			if (DifficultyLevelText != null)
			{
				DifficultyLevelText.text = GameController.Instance.CombatDifficultyLevel.Name;
			}
			else
			{
				DifficultyLevelLabel.text = GameController.Instance.CombatDifficultyLevel.Name;
			}
		}
	}
}
