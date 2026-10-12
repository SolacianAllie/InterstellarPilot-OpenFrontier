using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Hud.ShipComponents
{
	public class ShipComponentHealthController : MonoBehaviour
	{
		public ComponentBase Component;

		public Image HealthImage;

		private float lastHealthNormalized = -1f;

		public float Alpha = -1f;

		private void Awake()
		{
			if (Alpha < 0f && HealthImage != null)
			{
				Alpha = HealthImage.color.a;
			}
		}

		private void Update()
		{
			if (Component != null && HealthImage != null)
			{
				float healthNormalized = Component.HealthNormalized;
				if (healthNormalized != lastHealthNormalized)
				{
					Color hullColor = EngineASX.Instance.GetHullColor(healthNormalized);
					HealthImage.color = new Color(hullColor.r, hullColor.g, hullColor.b, Alpha);
					lastHealthNormalized = healthNormalized;
				}
			}
		}

		public void SetComponent(ComponentBase component)
		{
			Component = component;
			lastHealthNormalized = -1f;
		}
	}
}
