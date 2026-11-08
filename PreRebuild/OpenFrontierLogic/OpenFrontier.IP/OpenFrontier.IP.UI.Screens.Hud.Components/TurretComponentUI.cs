using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Hud.Components
{
	public class TurretComponentUI : MonoBehaviour
	{
		public Slider HealthSlider;

		public ComponentBase Component;

		public Graphic HealthGraphic;

		public ComponentDamageFlashController FlashController;

		public Graphic PoweredDownGraphic;

		private void Awake()
		{
			PoweredDownGraphic.color = GameController.Instance.GameSettings.ColorSettings.PoweredDownComponentColor;
		}

		public void RefreshVolatile()
		{
			FlashController.Component = Component;
			if (!(Component != null))
			{
				return;
			}
			if (HealthSlider != null)
			{
				float healthNormalized = Component.HealthNormalized;
				bool flag = ShouldShowHealthSlider(healthNormalized);
				HealthSlider.gameObject.SetActive(flag);
				if (flag)
				{
					HealthSlider.value = healthNormalized;
					HealthGraphic.color = EngineASX.Instance.GetHullColor(healthNormalized);
				}
			}
			PoweredDownGraphic.enabled = Component.CanChangeUserPowered && !Component.UserPowered;
		}

		public bool ShouldShowHealthSlider(float healthNormalized)
		{
			return healthNormalized < 1f;
		}

		public void ClearFlashState()
		{
			FlashController.ClearState();
		}

		private void Update()
		{
			RefreshVolatile();
		}
	}
}
