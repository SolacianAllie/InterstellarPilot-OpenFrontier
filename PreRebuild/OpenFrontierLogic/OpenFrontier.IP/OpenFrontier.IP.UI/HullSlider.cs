using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class HullSlider : MonoBehaviour
	{
		public Unit Unit;

		public Text Label;

		public Slider Slider;

		public void Refresh()
		{
			if (Unit != null)
			{
				float currentHealth = Unit.Destructable.CurrentHealth;
				float maxHealth = Unit.GetMaxHealth();
				Slider.value = Unit.Destructable.HealthNormalized;
				Label.text = $"{TextFormattingHelper.FormatHullValue(currentHealth)} / {TextFormattingHelper.FormatHullValue(maxHealth)}";
			}
		}
	}
}
