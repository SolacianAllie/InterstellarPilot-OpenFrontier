using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class CargoUsageSlider : MonoBehaviour
	{
		public Text Label;

		public Slider Slider;

		private float oldUsage = -1f;

		private float oldCapacity = -1f;

		public void RefreshFromCargoBayComponent(CargoBayComponent cargoBay)
		{
			if ((bool)cargoBay)
			{
				Refresh(cargoBay.Usage, cargoBay.Capacity);
			}
		}

		public void Refresh(float usage, float capacity)
		{
			if (usage != oldUsage || capacity != oldCapacity)
			{
				oldUsage = usage;
				oldCapacity = capacity;
				Slider.value = usage / capacity;
				if (Label != null)
				{
					Label.text = $"{TextFormattingHelper.FormatCargoVolume(usage)} / {TextFormattingHelper.FormatCargoVolume(capacity)}";
				}
			}
		}
	}
}
