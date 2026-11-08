using OpenFrontier.IP.Engine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class MissileLockItem : ScrollListItem<Missile>
	{
		public Image MissileSprite;

		public override void Refresh()
		{
			base.Refresh();
			if (Item.ProjectileClass.AmmoClass != null)
			{
				MissileSprite.sprite = EngineASX.Instance.EngineResources.GetCargoSpriteOrDefault(Item.ProjectileClass.AmmoClass);
			}
		}

		protected override void OnToggleValueOn()
		{
			base.OnToggleValueOn();
			if (Item != null && !Item.Projectile.Unit.IsDestroyed)
			{
				EngineASX.Instance.Hud.CurrentTarget = Item.Projectile.Unit;
			}
		}
	}
}
