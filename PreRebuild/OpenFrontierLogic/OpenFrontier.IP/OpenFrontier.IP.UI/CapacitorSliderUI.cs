using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	[RequireComponent(typeof(Slider))]
	public class CapacitorSliderUI : MonoBehaviour
	{
		public float AutoHideChargeThreshold = 0.9f;

		public HudScreen Hud;

		public Slider Slider;

		public bool AutoHide;

		private void Update()
		{
			if (!EngineASX.LoadedAndReady || !(Hud != null))
			{
				return;
			}
			Unit playerUnit = Hud.PlayerUnit;
			if (!(playerUnit != null))
			{
				return;
			}
			bool flag = ShouldShowCapacitor(playerUnit);
			Slider.gameObject.SetActive(flag);
			if (flag)
			{
				if (playerUnit.Components.Capacitor != null)
				{
					Slider.value = playerUnit.Components.Capacitor.ChargeNormalized;
				}
				else
				{
					Slider.value = 0f;
				}
			}
		}

		private bool ShouldShowCapacitor(Unit unit)
		{
			if (!AutoHide || !EngineASX.Instance.World.Permissions.AutoHideCapacitor)
			{
				return true;
			}
			CapacitorComponent capacitor = unit.GetCapacitor();
			if (capacitor != null)
			{
				return capacitor.ChargeNormalized < AutoHideChargeThreshold;
			}
			return false;
		}
	}
}
