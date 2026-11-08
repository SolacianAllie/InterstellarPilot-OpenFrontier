using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AutoTurrets
{
	public class PointDefenceAutoTurret : IAutoTurret
	{
		private TurretComponent turretComponent;

		private PointDefenceTargetter targetter;

		private Unit currentTarget;

		protected float nextCombatTargetSearch;

		private float rangeCheckFreq = 0.6f;

		private float nextRangeCheck;

		public TurretComponent TurretComponent => turretComponent;

		public UnitComponentHolder ComponentHolder => turretComponent.UnitComponents;

		public Unit Unit => turretComponent.Unit;

		public Unit CurrentTarget
		{
			get
			{
				return currentTarget;
			}
			set
			{
				currentTarget = value;
			}
		}

		public PointDefenceAutoTurret(TurretComponent turretComponent)
		{
			this.turretComponent = turretComponent;
			this.turretComponent.UnitComponents.InitPointDefenceAutoTurretModuleIfNull();
			targetter = new PointDefenceTargetter(this);
		}

		public void Update(float elapsedTime)
		{
			Unit unit = Unit;
			if (!unit.IsInActiveSector)
			{
				return;
			}
			if (currentTarget != null)
			{
				if (!(Time.time > nextRangeCheck))
				{
					return;
				}
				nextRangeCheck = Time.time + rangeCheckFreq;
				if (!IsTargetValidAndInRange(currentTarget, unit.SectorPosition))
				{
					CurrentTarget = null;
				}
				else if (!(ComponentHolder.PointDefenceTurretModule.FireCooldownTime > Time.time))
				{
					if (turretComponent is ProjectileTurretComponent projectileTurretComponent && (projectileTurretComponent.CurProjectileClass == null || !projectileTurretComponent.HasRequiredAmmo))
					{
						projectileTurretComponent.TrySelectRandomConventionalProjectileClass(ignoreAmmo: false);
					}
					if (TurretComponent.IsReadyToFire(currentTarget))
					{
						TurretComponent.Fire(currentTarget);
						nextRangeCheck += TurretComponent.TurretClass.AutoTurretFireCooldown;
					}
				}
			}
			else
			{
				UpdateTargetting(elapsedTime);
			}
		}

		protected virtual void UpdateTargetting(float elapsedTime)
		{
			if (!ComponentHolder.PointDefenceTurretModule.HasHostileTargets)
			{
				return;
			}
			if (targetter.IsSearching)
			{
				targetter.ProcessSearch(elapsedTime);
				if (!targetter.IsSearching)
				{
					OnTargetterFinishedSearching();
					SetNextSearchTime();
				}
			}
			else if (Time.time > nextCombatTargetSearch)
			{
				targetter.StartNewSearch();
				SetNextSearchTime();
			}
		}

		private void SetNextSearchTime()
		{
			nextCombatTargetSearch = Time.time + Random.Range(0.25f, 0.35f);
		}

		private void OnTargetterFinishedSearching()
		{
			_ = currentTarget;
			CurrentTarget = targetter.BestTarget;
		}

		public bool IsTargetValid(Unit target, Vector3 ourSectorPosition)
		{
			if (target != null && target.IsValidAndNotDestroyed && target.Sector == Unit.Sector && target.IsTargettable(Unit.Faction))
			{
				Vector3 lhs = Vector3.Normalize(ourSectorPosition - target.SectorPosition);
				if (Mathf.Abs(target.transform.position.y) > 30f)
				{
					return false;
				}
				if (lhs.magnitude > Unit.Radius * 2f && Vector3.Dot(lhs, target.transform.forward) < 0.6f)
				{
					return false;
				}
				return true;
			}
			return false;
		}

		public bool IsTargetValidAndInRange(Unit target, Vector3 ourSectorPosition)
		{
			if (IsTargetValid(target, ourSectorPosition))
			{
				if (turretComponent.IsTargetInFiringRange(target, out var _))
				{
					return turretComponent.IsPositionInFiringArc(target.transform.position);
				}
				return false;
			}
			return false;
		}
	}
}
