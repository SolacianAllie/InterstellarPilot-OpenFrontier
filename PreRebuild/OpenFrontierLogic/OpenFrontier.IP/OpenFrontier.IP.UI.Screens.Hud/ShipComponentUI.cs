using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Hud
{
	public class ShipComponentUI : MonoBehaviour
	{
		public enum ShipComponentUIAutoHideMode
		{
			Nothing,
			HealthOnly,
			Everything
		}

		public float PoweredDownFlashInterval = 1f;

		public float PoweredDownFlashOnInterval = 0.75f;

		public Transform Root;

		public ShipComponentUIAutoHideMode AutoHideMode = ShipComponentUIAutoHideMode.HealthOnly;

		public Slider HealthSlider;

		public ComponentBase Component;

		public Graphic HealthGraphic;

		public ComponentDamageFlashController FlashController;

		public Graphic PoweredDownGraphic;

		private void Awake()
		{
			PoweredDownGraphic.color = GameController.Instance.GameSettings.ColorSettings.PoweredDownComponentColor;
			switch (AutoHideMode)
			{
			case ShipComponentUIAutoHideMode.Everything:
				HealthSlider.gameObject.SetActive(value: false);
				Root.gameObject.SetActive(value: false);
				break;
			case ShipComponentUIAutoHideMode.HealthOnly:
				HealthSlider.gameObject.SetActive(value: false);
				break;
			}
		}

		public void RefreshVolatile()
		{
			FlashController.Component = Component;
			if (!(Component != null))
			{
				return;
			}
			bool flag = ShouldShowRoot();
			Root.gameObject.SetActive(flag);
			if (!flag)
			{
				return;
			}
			if (HealthSlider != null)
			{
				bool isDamaged = Component.IsDamaged;
				if (AutoHideMode != ShipComponentUIAutoHideMode.Nothing)
				{
					HealthSlider.gameObject.SetActive(isDamaged);
				}
				if (isDamaged)
				{
					HealthSlider.value = Component.HealthNormalized;
					HealthGraphic.color = EngineASX.Instance.GetHullColor(Component.HealthNormalized);
				}
			}
			PoweredDownGraphic.enabled = ShouldShowPoweredDown() && RealTime.time % PoweredDownFlashInterval > PoweredDownFlashOnInterval;
		}

		private bool ShouldShowPoweredDown()
		{
			if (Component.CanChangeUserPowered)
			{
				return !Component.UserPowered;
			}
			return false;
		}

		public bool ShouldShowRoot()
		{
			switch (AutoHideMode)
			{
			case ShipComponentUIAutoHideMode.Nothing:
			case ShipComponentUIAutoHideMode.HealthOnly:
				return true;
			case ShipComponentUIAutoHideMode.Everything:
				if (!Component.IsDamaged)
				{
					return ShouldShowPoweredDown();
				}
				return true;
			default:
				return true;
			}
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
