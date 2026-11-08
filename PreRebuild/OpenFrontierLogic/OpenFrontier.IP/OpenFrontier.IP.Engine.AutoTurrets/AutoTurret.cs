using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AutoTurrets
{
	public class AutoTurret : IAutoTurret
	{
		private TurretComponent turretComponent;

		private AutoTurretTargetter targetter;

		private Unit currentTarget;

		protected float nextCombatTargetSearch;

		private float rangeCheckFreq = 1f;

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

		public AutoTurret(TurretComponent turretComponent)
		{
			this.turretComponent = turretComponent;
			this.turretComponent.UnitComponents.InitAutoTurretModuleIfNull();
			targetter = new AutoTurretTargetter(this);
		}

		public void Update(float elapsedTime)
		{
			if (ComponentHolder.AutoTurretModule.FireMode == AutoTurretFireMode.Disabled)
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
				if (!IsTargetValidAndInRange(currentTarget, out var targetDistance))
				{
					CurrentTarget = null;
				}
				else
				{
					if (ComponentHolder.AutoTurretFireCooldownTime > Time.time)
					{
						return;
					}
					if (turretComponent is ProjectileTurretComponent projectileTurretComponent && NpcPilot.TurretRequiresChangeOfAmmo(projectileTurretComponent, projectileTurretComponent.UsesAmmo, currentTarget, targetDistance))
					{
						float? mobileTargetDistance = null;
						if (currentTarget != null && !currentTarget.IsStatic)
						{
							mobileTargetDistance = targetDistance;
						}
						projectileTurretComponent.TrySelectRandomConventionalProjectileClass(ignoreAmmo: false, mobileTargetDistance);
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
			switch (ComponentHolder.AutoTurretModule.FireMode)
			{
			case AutoTurretFireMode.AnyTarget:
				if (!ComponentHolder.AutoTurretModule.HasHostileTargets)
				{
					break;
				}
				if (targetter.IsSearching)
				{
					targetter.ProcessSearch(elapsedTime);
					if (!targetter.IsSearching)
					{
						OnTargetterFinishedSearching();
						nextCombatTargetSearch = Time.time + 2f;
					}
				}
				else if (Time.time > nextCombatTargetSearch)
				{
					if (turretComponent.IsReadyToFireIgnoringTarget())
					{
						targetter.StartNewSearch();
					}
					nextCombatTargetSearch = Time.time + 2f;
				}
				break;
			case AutoTurretFireMode.PreferredTargetOnly:
			{
				Unit preferredTurretTarget = ComponentHolder.AutoTurretModule.PreferredTurretTarget;
				if (preferredTurretTarget != null && Unit.IsHostileTo(preferredTurretTarget) && IsTargetValid(preferredTurretTarget))
				{
					CurrentTarget = preferredTurretTarget;
				}
				break;
			}
			}
		}

		private void OnTargetterFinishedSearching()
		{
			Unit unit = currentTarget;
			CurrentTarget = targetter.BestTarget;
			if (unit != currentTarget && currentTarget != null && currentTarget.IsValidAndNotDestroyed && currentTarget.IsPlayerCurrentUnit)
			{
				EngineASX.Instance.NotifyPlayerTargettedByHostileNpc(Unit);
			}
		}

		public bool IsTargetValid(Unit target)
		{
			if (target != null && target.IsValidAndNotDestroyed && target.Sector == Unit.Sector && Unit.IsHostileTo(target))
			{
				return target.IsTargettable(Unit.Faction);
			}
			return false;
		}

		public bool IsTargetValidAndInRange(Unit target, out float targetDistance)
		{
			targetDistance = 0f;
			if (IsTargetValid(target))
			{
				float intelMaxAgeOfNonStaticTarget = GetIntelMaxAgeOfNonStaticTarget();
				if (target.UnitType == UnitType.Projectile || Unit.Faction.Intel.IsUnitDiscovered(target, intelMaxAgeOfNonStaticTarget))
				{
					if (turretComponent.IsTargetInFiringRange(target, out targetDistance))
					{
						return turretComponent.IsPositionInFiringArc(target.transform.position);
					}
					return false;
				}
			}
			return false;
		}

		private float GetIntelMaxAgeOfNonStaticTarget()
		{
			if (!Unit.IsInActiveSector)
			{
				return GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTime;
			}
			return GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTimeActiveSector;
		}
	}
}
