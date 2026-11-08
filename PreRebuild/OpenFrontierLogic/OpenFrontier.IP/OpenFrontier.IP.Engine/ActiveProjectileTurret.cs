using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class ActiveProjectileTurret : ActiveTurret
	{
		private Projectile lastFiredProjectile;

		public ProjectileTurretComponent ProjectileTurret;

		private ProjectileTurretClass projectileTurretClass;

		private Vector3 targetFireLocalPosition;

		public ProjectileTurretClass ProjectileTurretClass
		{
			get
			{
				return projectileTurretClass;
			}
			set
			{
				projectileTurretClass = value;
			}
		}

		public Projectile LastFiredProjectile => lastFiredProjectile;

		public ProjectileClass CurProjectileClass
		{
			get
			{
				return ProjectileTurret.CurProjectileClass;
			}
			set
			{
				ProjectileTurret.CurProjectileClass = value;
			}
		}

		protected override void onInit()
		{
			projectileTurretClass = (ProjectileTurretClass)TurretComponent.TurretClass;
			base.onInit();
		}

		protected override void fire(Unit target)
		{
			base.fire(target);
			ProjectileClass curProjectileClass = ProjectileTurret.CurProjectileClass;
			if (curProjectileClass != null && curProjectileClass.ProjectilePrefab != null)
			{
				GameObject pooledObjectOrCreate = Engine.Pooler.GetPooledObjectOrCreate(curProjectileClass.ProjectilePrefab);
				pooledObjectOrCreate.SetActive(value: true);
				pooledObjectOrCreate.transform.position = transform.position;
				Projectile component = pooledObjectOrCreate.GetComponent<Projectile>();
				Vector3? targetPosition = null;
				if (target != null)
				{
					targetPosition = target.transform.TransformPoint(targetFireLocalPosition);
				}
				component.Initialise(Sector, curProjectileClass, this, target, targetPosition, targetFireLocalPosition);
				lastFiredProjectile = component;
			}
			else
			{
				Debug.LogWarning("Projectile turret fired but doesn't have an assigned projectile type or the projectile type has no prefab. ProjectileType: " + ProjectileTurret.CurProjectileClass, this);
			}
			FinishFiring();
		}

		protected override Quaternion GenerateInaccuracy()
		{
			return GenerateInaccuracyInternal(CurProjectileClass?.InaccuracyMultiplier ?? 1f);
		}

		protected override void UpdateBurstCharge()
		{
			ProjectileClass curProjectileClass = CurProjectileClass;
			if (curProjectileClass != null && (!TurretComponent.UsesAmmo || TurretComponent.HasRequiredAmmo))
			{
				if (Time.time > LastFireTime + curProjectileClass.BurstTimeBetweenRounds)
				{
					FireState = TurretFireState.Firing;
				}
			}
			else
			{
				CancelFiring();
			}
		}

		protected override void OnFireStateChanged(TurretFireState oldFireState)
		{
			base.OnFireStateChanged(oldFireState);
			if (FireState == TurretFireState.Prewarm)
			{
				targetFireLocalPosition = Vector3.zero;
				if (lastFireTarget != null)
				{
					targetFireLocalPosition = GetSubTargetLocalPosition();
				}
			}
		}

		protected virtual Vector3 GetSubTargetLocalPosition()
		{
			if (ProjectileTurretClass.UseSubTargetPositions)
			{
				return TurretComponent.GetUnitSubTargetLocalPosition(lastFireTarget);
			}
			return Vector3.zero;
		}

		protected override int GetFireBurstRounds()
		{
			ProjectileClass curProjectileClass = CurProjectileClass;
			if (curProjectileClass != null)
			{
				return curProjectileClass.BurstRounds;
			}
			return base.GetFireBurstRounds();
		}
	}
}
