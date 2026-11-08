using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Hud.CurrentTargetUI.WeaponsReadyIndicator
{
	public class WeaponsReadyIndicatorItem : MonoBehaviour
	{
		public Image ReadySprite;

		public float Alpha = 217f / 255f;

		public void Refresh(TurretComponent turretComponent)
		{
			if (EngineASX.Instance.Hud != null)
			{
				Unit currentTarget = EngineASX.Instance.Hud.CurrentTarget;
				SetColor(GetColor(turretComponent, currentTarget));
			}
		}

		private Color GetColor(TurretComponent turretComponent, Unit currentTarget)
		{
			if (turretComponent.AutoFireActive)
			{
				if (turretComponent.IsReadyToFire(currentTarget))
				{
					return GameController.Instance.GameSettings.ColorSettings.WeaponAutoFireReadyColor;
				}
				return GameController.Instance.GameSettings.ColorSettings.WeaponAutoFireNotReadyColor;
			}
			if (TurretGridUI.CanFireTurret(turretComponent, currentTarget))
			{
				return GameController.Instance.GameSettings.ColorSettings.WeaponReadyColor;
			}
			return GameController.Instance.GameSettings.ColorSettings.WeaponNotReadyColor;
		}

		private void SetColor(Color c)
		{
			c.a = Alpha;
			ReadySprite.color = c;
		}
	}
}
