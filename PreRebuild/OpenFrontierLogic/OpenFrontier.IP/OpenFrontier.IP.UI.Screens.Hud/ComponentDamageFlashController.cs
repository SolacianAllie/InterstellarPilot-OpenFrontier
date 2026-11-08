using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Hud
{
	public class ComponentDamageFlashController : MonoBehaviour
	{
		public DamageFlashControllerBase[] DamageFlashControllers;

		public ComponentBase Component;

		private float? lastTurretHealth;

		private void Update()
		{
			if (!(Component != null))
			{
				return;
			}
			float healthNormalized = Component.HealthNormalized;
			if (lastTurretHealth.HasValue && healthNormalized < lastTurretHealth)
			{
				DamageFlashControllerBase[] damageFlashControllers = DamageFlashControllers;
				foreach (DamageFlashControllerBase damageFlashControllerBase in damageFlashControllers)
				{
					if (!damageFlashControllerBase.HasStarted)
					{
						damageFlashControllerBase.StartFlash();
					}
				}
			}
			lastTurretHealth = healthNormalized;
		}

		private void OnDisable()
		{
			ClearState();
		}

		public void ClearState()
		{
			lastTurretHealth = null;
			DamageFlashControllerBase[] damageFlashControllers = DamageFlashControllers;
			foreach (DamageFlashControllerBase damageFlashControllerBase in damageFlashControllers)
			{
				if (damageFlashControllerBase.HasStarted)
				{
					damageFlashControllerBase.StopFlash();
				}
			}
		}
	}
}
