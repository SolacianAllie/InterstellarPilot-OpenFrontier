using OpenFrontier.IP.Common;
using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class LaserTurretComponent : TurretComponent
	{
		private static DamageType damageType = new DamageType();

		public LaserTurretClass LaserTurretClass;

		protected override ActiveTurret AddActiveTurretComponent(GameObject g)
		{
			ActiveLaserTurret activeLaserTurret = g.AddComponent<ActiveLaserTurret>();
			activeLaserTurret.LaserTurret = this;
			return activeLaserTurret;
		}

		protected override void InactiveFire(Unit target)
		{
			base.InactiveFire(target);
			if (target != null)
			{
				float randomDamageFromEnergy = LaserTurretClass.GetRandomDamageFromEnergy();
				randomDamageFromEnergy *= Engine.GameSettings.InactiveFireMultiplier;
				applyDamageInactive(target, randomDamageFromEnergy);
			}
		}

		private void applyDamageInactive(Unit target, float damage)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"Applying damage {damage} to {target}", this, 3);
			}
			damageType.MiningDamage = LaserTurretClass.MiningDamageMultiplier;
			damageType.ShieldDamageType = LaserTurretClass.ShieldDamageType;
			damageType.Damage = damage;
			if (GameController.Instance.GameSettings.DebugSettings.DamageEnabled && target.Destructable != null)
			{
				target.Destructable.ApplyDamage(transform.position, Faction, Unit, damageType, DamageDirectType.Direct, Random.Range(0, 6));
			}
		}
	}
}
