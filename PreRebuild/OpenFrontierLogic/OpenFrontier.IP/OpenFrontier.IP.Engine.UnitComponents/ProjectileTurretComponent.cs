using System.Collections.Generic;
using OpenFrontier.IP.Common;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class ProjectileTurretComponent : TurretComponent
	{
		private class ProjectileClassComparer : IComparer<ProjectileClass>
		{
			public int Compare(ProjectileClass x, ProjectileClass y)
			{
				if (x.AmmoClass != null && y.AmmoClass != null)
				{
					return x.AmmoClass.BasePrice.CompareTo(y.AmmoClass.BasePrice);
				}
				return 1;
			}
		}

		private static ProjectileClassComparer sorter = new ProjectileClassComparer();

		public static List<ProjectileClass> projectileClassTmp = new List<ProjectileClass>(20);

		private static DamageType damageType = new DamageType();

		private ProjectileClass curProjectileClass;

		public ProjectileTurretClass ProjectileTurretClass;

		public ProjectileClass CurProjectileClass
		{
			get
			{
				return curProjectileClass;
			}
			set
			{
				if (curProjectileClass != value)
				{
					curProjectileClass = value;
				}
			}
		}

		public override bool UsesAmmo
		{
			get
			{
				if (curProjectileClass != null)
				{
					return curProjectileClass.AmmoClass != null;
				}
				return false;
			}
		}

		public override int GetAvailableAmmoCount
		{
			get
			{
				if (curProjectileClass != null && curProjectileClass.AmmoClass != null)
				{
					return UnitComponents.CargoBayComponent.GetCountOf(curProjectileClass.AmmoClass);
				}
				return -1;
			}
		}

		public override bool HasRequiredAmmo
		{
			get
			{
				if (UsesAmmo)
				{
					if (curProjectileClass != null)
					{
						return GetAvailableAmmoCount >= curProjectileClass.AmmoRequirement;
					}
					return false;
				}
				return true;
			}
		}

		public bool IsCountermeasureTurret => ProjectileTurretClass.ProjectileTurretType == TurretType.Countermeasure;

		protected override void onAttachedToUnit(UnitComponentHolder unit)
		{
			base.onAttachedToUnit(unit);
			LoadSingleCompatibleProjectileClass();
			if (IsCountermeasureTurret)
			{
				unit.CountermeasureTurret = this;
			}
		}

		protected override void onDetachedFromUnit(UnitComponentHolder unit)
		{
			base.onDetachedFromUnit(unit);
			if (IsCountermeasureTurret && unit.CountermeasureTurret == this)
			{
				unit.CountermeasureTurret = null;
			}
		}

		private void LoadSingleCompatibleProjectileClass()
		{
			if (ProjectileTurretClass.CompatibleProjectiles.Count == 1)
			{
				CurProjectileClass = ProjectileTurretClass.CompatibleProjectiles[0];
			}
		}

		public override void RemoveAmmoForFiring()
		{
			if (curProjectileClass != null && Unit.CargoBayComponent != null)
			{
				Unit.CargoBayComponent.AddToCargoIfFits(curProjectileClass.AmmoClass, -curProjectileClass.AmmoRequirement);
			}
		}

		public override bool CanUseCargoClass(CargoClass c)
		{
			foreach (ProjectileClass compatibleProjectile in ProjectileTurretClass.CompatibleProjectiles)
			{
				if (compatibleProjectile.AmmoClass == c)
				{
					return true;
				}
			}
			return false;
		}

		public override bool IsReadyToFire(Unit target, bool ignoreFiringArc = false)
		{
			if (curProjectileClass != null)
			{
				return base.IsReadyToFire(target, ignoreFiringArc);
			}
			return false;
		}

		/// <summary>
		/// Only the laser seeker needs the launcher to be holding a designator on
		/// the target. Everything else here flies on its own, so asking for a
		/// beam for them would leave one burning on a ship that has no reason
		/// to be pointing one at anything.
		/// </summary>
		protected override bool NeedsLaserDesignation =>
			curProjectileClass != null && curProjectileClass.GuidanceType == MissileGuidanceType.Optical;

		/// <summary>
		/// The ship has to be pointed within the loaded missile's own seeker
		/// cone before it will launch it.
		/// </summary>
		/// <remarks>
		/// Not a number of its own, and deliberately so. A missile leaves along
		/// the hull's nose because nothing ever rotates a turret, and its seeker
		/// can only look along the missile's nose - so the arc the ship must be
		/// pointing within and the arc the missile can see are the same arc.
		/// Storing it twice let the two disagree, and every value they disagreed
		/// on was a launch that could not possibly work.
		///
		/// Reading the loaded missile rather than the launcher means a launcher
		/// holding a wide-cone round can shoot much further off the bow than one
		/// holding a tight-cone round, which is the right way round: the tight
		/// one is the missile that needs the ship pointed at the target.
		///
		/// A missile with no cone at all - SeekerConeAngle zero, which is what
		/// every prefab written before the field existed reads - imposes no
		/// requirement, which is also what it did before.
		/// </remarks>
		protected override float RequiredLaunchAngle =>
			curProjectileClass != null ? curProjectileClass.SeekerConeAngle : 0f;

		public void SelectFirstProjectileClass(bool conventionalWeaponsOnly)
		{
			if (PopulateProjectileClasses(ignoreAmmo: true, sort: true, conventionalWeaponsOnly) > 0)
			{
				CurProjectileClass = projectileClassTmp[0];
			}
			else
			{
				CurProjectileClass = null;
			}
		}

		public bool TrySelectRandomConventionalProjectileClass(bool ignoreAmmo, float? mobileTargetDistance = null)
		{
			ProjectileClass randomConventionalProjectileClass = GetRandomConventionalProjectileClass(ignoreAmmo, mobileTargetDistance);
			if (randomConventionalProjectileClass != null)
			{
				CurProjectileClass = randomConventionalProjectileClass;
				return true;
			}
			return false;
		}

		public bool TrySelectNextProjectileClass(bool ignoreAmmo, bool conventionalWeaponsOnly)
		{
			ProjectileClass nextProjectileClass = GetNextProjectileClass(ignoreAmmo, conventionalWeaponsOnly);
			if (nextProjectileClass != null)
			{
				CurProjectileClass = nextProjectileClass;
				return true;
			}
			return false;
		}

		public int PopulateProjectileClasses(bool ignoreAmmo, bool sort, bool conventionalWeaponsOnly, float? mobileTargetDistance = null)
		{
			projectileClassTmp.Clear();
			CargoBayComponent cargoBayComponent = UnitComponents.CargoBayComponent;
			if (cargoBayComponent != null)
			{
				for (int i = 0; i < ProjectileTurretClass.CompatibleProjectiles.Count; i++)
				{
					ProjectileClass projectileClass = ProjectileTurretClass.CompatibleProjectiles[i];
					if (projectileClass != null && (!conventionalWeaponsOnly || projectileClass.IsConventionalWeapon) && (!mobileTargetDistance.HasValue || !(projectileClass.EffectiveFiringRangeAgainstMobile < mobileTargetDistance)) && (ignoreAmmo || projectileClass.AmmoClass == null || cargoBayComponent.GetCountOf(projectileClass.AmmoClass) >= projectileClass.AmmoRequirement))
					{
						projectileClassTmp.Add(projectileClass);
					}
				}
				if (sort)
				{
					projectileClassTmp.Sort(sorter);
				}
			}
			return projectileClassTmp.Count;
		}

		public ProjectileClass GetRandomConventionalProjectileClass(bool ignoreAmmo, float? mobileTargetDistance = null)
		{
			if (Unit != null)
			{
				int num = PopulateProjectileClasses(ignoreAmmo, sort: false, conventionalWeaponsOnly: true, mobileTargetDistance);
				if (num > 0)
				{
					return projectileClassTmp.GetRandom(num);
				}
			}
			return null;
		}

		public bool CanSwitchConventionalProjectileClass(bool ignoreAmmo)
		{
			ProjectileClass nextConventionalProjectileClass = GetNextConventionalProjectileClass(ignoreAmmo);
			if (nextConventionalProjectileClass != null)
			{
				return nextConventionalProjectileClass != curProjectileClass;
			}
			return false;
		}

		public ProjectileClass GetNextConventionalProjectileClass(bool ignoreAmmo)
		{
			return GetNextProjectileClass(ignoreAmmo, conventionalWeaponsOnly: true);
		}

		public ProjectileClass GetNextProjectileClass(bool ignoreAmmo, bool conventionalWeaponsOnly)
		{
			if (Unit != null)
			{
				int num = PopulateProjectileClasses(ignoreAmmo, sort: true, conventionalWeaponsOnly);
				if (num > 0)
				{
					int index = Maths.WrapValue(projectileClassTmp.IndexOf(CurProjectileClass) + 1, 0, num);
					if (projectileClassTmp[index] != curProjectileClass)
					{
						return projectileClassTmp[index];
					}
				}
			}
			return null;
		}

		protected override ActiveTurret AddActiveTurretComponent(GameObject g)
		{
			ActiveProjectileTurret activeProjectileTurret = g.AddComponent<ActiveProjectileTurret>();
			activeProjectileTurret.ProjectileTurret = this;
			return activeProjectileTurret;
		}

		private bool ShouldApplyInactiveFireDamage(Unit target)
		{
			if (curProjectileClass != null && ProjectileTurretClass.ProjectileTurretType == TurretType.Missiles)
			{
				NpcPilot npcPilot = target.NpcPilot;
				if (npcPilot != null)
				{
					if (TargetPilotEvadesMissileWithCountermeasures(target, npcPilot))
					{
						return false;
					}
					if (target.Components.EstimatedPointDefenceEffectiveness > 0f && TargetPilotShootsDownMissileWithPointDefence(target, npcPilot))
					{
						return false;
					}
				}
			}
			return true;
		}

		protected override void InactiveFire(Unit target)
		{
			base.InactiveFire(target);
			if (target != null && ShouldApplyInactiveFireDamage(target))
			{
				Vector3 sourePosition = transform.position;
				float num = 0f;
				num = ((!curProjectileClass.SetDamageUsingEnergyCost) ? (curProjectileClass.Damage * (float)curProjectileClass.BurstRounds) : TurretClass.GetRandomDamageFromEnergy());
				num *= Engine.GameSettings.InactiveFireMultiplier;
				ApplyDamageWhenInactive(target, ref sourePosition, num);
			}
			if (UsesAmmo)
			{
				RemoveAmmoForFiring();
			}
		}

		private bool TargetPilotShootsDownMissileWithPointDefence(Unit target, NpcPilot targetPilot)
		{
			float estimatedPointDefenceEffectiveness = target.Components.EstimatedPointDefenceEffectiveness;
			if (Random.value < estimatedPointDefenceEffectiveness)
			{
				EngineASX.Instance.DebugInfo.NumTimesNpcPilotSuccessfullyUsesPointDefenceInInactiveSector++;
				return true;
			}
			EngineASX.Instance.DebugInfo.NumTimesNpcPilotFailsToUsePointDefenceInInactiveSector++;
			return false;
		}

		private bool TargetPilotEvadesMissileWithCountermeasures(Unit target, NpcPilot targetPilot)
		{
			ProjectileTurretComponent countermeasureTurret = target.Components.CountermeasureTurret;
			if (countermeasureTurret == null)
			{
				return false;
			}
			if (countermeasureTurret.IsReadyToFireIgnoringTarget())
			{
				ProjectileClass projectileClass = countermeasureTurret.curProjectileClass;
				if (projectileClass == null)
				{
					return false;
				}
				countermeasureTurret.Fire(null);
				try
				{
					float effectiveness = projectileClass.ProjectilePrefab.GetComponent<Countermeasure>().CountermeasureClass.Effectiveness;
					effectiveness = Mathf.Pow(effectiveness, 0.5f);
					if (Random.value < Mathf.Lerp(effectiveness * 0.8f, effectiveness, targetPilot.Settings.CombatEfficiency))
					{
						EngineASX.Instance.DebugInfo.NumTimesNpcPilotEvadesMissilesInInactiveSector++;
						return true;
					}
				}
				catch
				{
				}
				EngineASX.Instance.DebugInfo.NumTimesNpcPilotFailsToEvadeMissilesInInactiveSector++;
			}
			return false;
		}

		private void ApplyDamageWhenInactive(Unit target, ref Vector3 sourePosition, float damage)
		{
			if (damage <= 0f)
			{
				return;
			}
			ProjectileClass projectileClass = CurProjectileClass;
			if (projectileClass != null)
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Applying damage {damage} to {target}", this, 3);
				}
				damageType.Damage = damage * Engine.GameSettings.InactiveFireMultiplier;
				damageType.MiningDamage = projectileClass.MiningDamageMultiplier;
				damageType.ShieldDamageType = projectileClass.ShieldDamageType;
				if (GameController.Instance.GameSettings.DebugSettings.DamageEnabled && target.Destructable != null)
				{
					target.Destructable.ApplyDamage(transform.position, Faction, Unit, damageType, DamageDirectType.Direct, Random.Range(0, 6));
				}
			}
		}
	}
}
