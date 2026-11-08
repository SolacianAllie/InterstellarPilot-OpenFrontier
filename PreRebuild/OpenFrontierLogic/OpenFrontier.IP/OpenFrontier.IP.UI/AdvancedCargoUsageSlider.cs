using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class AdvancedCargoUsageSlider : MonoBehaviour
	{
		public CargoBayComponent CargoBay;

		public Text Label;

		public Slider Slider;

		public Slider ExpectedUsageSlider;

		public float ExpectedCargoUsage;

		public void SetUnit(Unit unit)
		{
			CargoBay = unit.CargoBayComponent;
			if (CargoBay != null)
			{
				ExpectedCargoUsage = CargoBay.Usage;
			}
		}

		public void SetUnitAndRefresh(Unit unit)
		{
			SetUnit(unit);
			Refresh();
		}

		public void Refresh()
		{
			if (CargoBay != null)
			{
				float usage = CargoBay.Usage;
				float capacity = CargoBay.Capacity;
				Slider.value = usage / capacity;
				Label.text = $"{TextFormattingHelper.FormatCargoVolume(usage)} / {TextFormattingHelper.FormatCargoVolume(capacity)}";
				ExpectedUsageSlider.value = ExpectedCargoUsage / capacity;
			}
		}
	}
}
