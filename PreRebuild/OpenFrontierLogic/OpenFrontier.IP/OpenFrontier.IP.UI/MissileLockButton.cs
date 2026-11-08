using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class MissileLockButton : MonoBehaviour
	{
		public Image ProjectileTypeImage;

		private Missile missile;

		private Button button;

		public Missile Missile
		{
			get
			{
				return missile;
			}
			set
			{
				if (missile != value)
				{
					missile = value;
					if (Missile != null && ProjectileTypeImage != null && missile.ProjectileClass.AmmoClass != null)
					{
						ProjectileTypeImage.sprite = EngineASX.Instance.EngineResources.GetCargoSpriteOrDefault(missile.ProjectileClass.AmmoClass);
					}
				}
			}
		}

		private void Awake()
		{
			button = GetComponent<Button>();
			button.onClick.AddListener(ButtonClick);
		}

		private void ButtonClick()
		{
			if (missile != null && missile.Projectile != null && missile.Projectile.Unit != null && missile.Projectile.Unit.IsValidAndNotDestroyed)
			{
				HudScreen instance = HudScreen.Instance;
				if (instance != null)
				{
					instance.CurrentTarget = missile.Projectile.Unit;
				}
			}
		}
	}
}
