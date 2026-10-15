using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.AutoTurrets;
using OpenFrontier.IP.Scenarios;
using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class TurretComponent : ComponentBase
	{
		private IAutoTurret autoTurret;

		[SerializeField]
		private bool autoFire;

		private ActiveTurret activeTurret;

		public float AiNextRestrictedWeaponsFireTime;

		private float chargedEnergy;

		private bool hasMinimumEnergyCharge;

		protected bool isCharged;

		public TurretClass TurretClass;

		public override ComponentClass ComponentClass => TurretClass;

		public bool HasFullEnergyCharge => chargedEnergy >= TurretClass.EnergyCost;

		public ActiveTurret ActiveTurret
		{
			get
			{
				return activeTurret;
			}
			set
			{
				activeTurret = value;
			}
		}

		public virtual bool UsesAmmo => false;

		public virtual int GetAvailableAmmoCount => 0;

		public virtual bool HasRequiredAmmo => false;

		public virtual bool IsFiring
		{
			get
			{
				if (activeTurret != null)
				{
					return activeTurret.IsFiring;
				}
				return false;
			}
		}

		public float ChargedEnergy
		{
			get
			{
				return chargedEnergy;
			}
			set
			{
				if (chargedEnergy != value)
				{
					chargedEnergy = Mathf.Clamp(value, 0f, TurretClass.EnergyCost);
					isCharged = chargedEnergy >= TurretClass.EnergyCost;
					hasMinimumEnergyCharge = chargedEnergy >= TurretClass.GetMinFireRequiredEnergy();
					if (!isCharged)
					{
						UnitComponents.anyComponentRequiresRecharge = true;
					}
				}
			}
		}

		public float ChargedEnergyNormalized => chargedEnergy / TurretClass.EnergyCost;

		public bool HasMinimumEnergyCharge => hasMinimumEnergyCharge;

		public bool IsMiningLaser => ComponentClass.ComponentBayType.BayType == BayType.Mining;

		public bool AutoFire
		{
			get
			{
				return autoFire;
			}
			set
			{
				if (autoFire != value)
				{
					autoFire = value;
					if (autoFire)
					{
						UnitComponents.hasAutoFireTurrets = true;
					}
				}
			}
		}

		public bool IsAlwaysAutoFire => TurretClass.IsPointDefence;

		public bool AutoFireActive
		{
			get
			{
				if (IsAlwaysAutoFire)
				{
					return true;
				}
				if (autoFire && UnitComponents.AutoTurretModule != null)
				{
					return UnitComponents.AutoTurretModule.FireMode != AutoTurretFireMode.Disabled;
				}
				return false;
			}
		}

		public bool IsWeapon => TurretClass.IsWeapon;

		public bool IsMineDropper => TurretClass.DefaultTurretGroup == TurretGroups.Mine;

		public static Vector3 GetUnitSubTargetLocalPosition(Unit unit)
		{
			return unit.ActiveUnit.GetRandomHullTargetLocalPosition();
		}

		public override bool RequiresRecharge()
		{
			return !isCharged;
		}

		public virtual bool CanChargeTurret()
		{
			if (!UsesAmmo || HasRequiredAmmo)
			{
				return !IsFiring;
			}
			return false;
		}

		public bool Fire(Unit target)
		{
			if (TurretClass.AIConventionalWeapon)
			{
				UnitComponents.RegisterWeaponsFire();
			}
			UnitComponents.anyComponentRequiresRecharge = true;
			RequestLaserDesignation(target);
			onFired(target);
			if (activeTurret != null)
			{
				return activeTurret.Fire(target);
			}
			RemoveCharge();
			InactiveFire(target);
			return true;
		}

		/// <summary>
		/// Tells this ship's utility mounts to hold their beam on whatever this
		/// shot is aimed at.
		/// </summary>
		/// <remarks>
		/// Only a homing projectile asks, because only one can be waiting on a
		/// beam. A gun is aimed by the shot itself and needs nothing kept on the
		/// target afterwards, so asking for a beam it will never ride would
		/// leave one burning for no reason.
		///
		/// This is also where deselection comes from, without any of it being
		/// about the HUD: the beam follows the shots. Stop shooting at a ship,
		/// or start shooting at a different one, and the laser moves - which is
		/// exactly what losing it by deselection means. An AI ship designates
		/// through the same call as a player's, because the firing code is the
		/// firing code.
		/// </remarks>
		private void RequestLaserDesignation(Unit target)
		{
			if (target == null || !NeedsLaserDesignation)
			{
				return;
			}
			UnitComponents.RequestUtilityMountDesignation(target);
		}

		/// <summary>
		/// Whether a shot from this turret needs a laser designator held on the
		/// target for it to work.
		/// </summary>
		/// <remarks>
		/// False for everything that is not a projectile turret. It is a
		/// question about the round in the tube, not about the launcher - a
		/// launcher that can fire a laser missile and a radar missile asks for
		/// a beam only when the laser one is the one loaded.
		/// </remarks>
		protected virtual bool NeedsLaserDesignation => false;

		public virtual void RemoveAmmoForFiring()
		{
		}

		public void CancelFiring()
		{
			if (IsFiring)
			{
				OnFiringCancelled();
			}
		}

		public virtual bool IsReadyToFireIgnoringTarget()
		{
			if (IsPoweredAndEnergySupplied && enabled && hasMinimumEnergyCharge && IsUnitReadyToFire() && (activeTurret == null || !activeTurret.IsFiring))
			{
				if (UsesAmmo)
				{
					return HasRequiredAmmo;
				}
				return true;
			}
			return false;
		}

		public virtual bool IsReadyToFire(Unit target, bool ignoreFiringArc = false)
		{
			if (IsPoweredAndEnergySupplied && enabled && hasMinimumEnergyCharge && IsUnitReadyToFire() && (activeTurret == null || activeTurret.IsReadyToFire(target, ignoreFiringArc)) && (!UsesAmmo || HasRequiredAmmo))
			{
				if (TurretClass.RequiresTarget)
				{
					float distance;
					if (target != null && target.IsValidAndNotDestroyed)
					{
						// The bay's own arc is answered by the activeTurret check
						// above. This is the other half of it: whether the ship
						// is pointing at the target at all, which for a launcher
						// is what decides whether the missile even leaves in the
						// right direction.
						if (!ignoreFiringArc && !IsShipFacingTarget(target))
						{
							return false;
						}
						return IsTargetInFiringRange(target, out distance);
					}
					return false;
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Whether the ship's nose is close enough to the target's bearing for
		/// this launcher to fire, per <see cref="TurretClass.LauncherFacingArc"/>.
		/// </summary>
		/// <remarks>
		/// Same bearing arithmetic as <see cref="IsLocalDirectionInFiringArc"/>,
		/// measured from the hull rather than from a bay, and flattened onto
		/// the combat plane because units are Y-frozen and "below the ship" is
		/// not a direction anything can be in.
		/// </remarks>
		private bool IsShipFacingTarget(Unit target)
		{
			float halfAngle = TurretClass.LauncherFacingArc;
			if (halfAngle <= 0f)
			{
				return true;
			}
			Vector3 local = Unit.transform.InverseTransformPoint(target.transform.position);
			Vector3 flat = new Vector3(local.x, 0f, local.z);
			if (flat.sqrMagnitude <= 0.000001f)
			{
				// Target is on top of us. There is no bearing to be outside of,
				// and MinFiringRange is what decides this case.
				return true;
			}
			float bearing = Geometry.WrapDegreesForBearing(Quaternion.LookRotation(flat, Vector3.up).eulerAngles.y);
			return Mathf.Abs(bearing) <= halfAngle;
		}

		public bool IsUnitReadyToFire()
		{
			if (!Unit.IsDocked && UnitComponents.IsFullyDecloaked)
			{
				return UnitComponents.ConstructionState == ConstructionState.Constructed;
			}
			return false;
		}

		public bool IsTargetInFiringRange(Unit target, out float distance)
		{
			distance = GetDistance2D(target.transform.position, transform.position);
			return IsTargetInFiringRange(distance);
		}

		private static float GetDistance2D(Vector3 ourPosition, Vector3 targetPosition)
		{
			return Vector2.Distance(new Vector2(ourPosition.x, ourPosition.z), new Vector2(targetPosition.x, targetPosition.z));
		}

		public bool IsTargetInFiringRange(float distance, bool ignoreMinFiringRange = false)
		{
			if (ignoreMinFiringRange || TurretClass.MinFiringRange <= 0f || !Unit.IsActiveInEngine || distance > TurretClass.MinFiringRange)
			{
				return distance < TurretClass.MaxFiringRange;
			}
			return false;
		}

		public virtual bool CanCancelFire()
		{
			return false;
		}

		protected override void onUnitInactive()
		{
			if (activeTurret != null)
			{
				activeTurret.SafeDestroy();
			}
			ActiveTurret = null;
			base.onUnitInactive();
		}

		protected override GameObject CreateActiveGameObject()
		{
			if (TurretClass != null)
			{
				GameObject gameObject = null;
				if (TurretClass.TurretPrefab != null)
				{
					gameObject = Object.Instantiate(TurretClass.TurretPrefab, transform);
				}
				else
				{
					gameObject = new GameObject();
					gameObject.transform.SetParent(transform);
				}
				gameObject.name = TurretClass.name;
				ActiveTurret = AddActiveTurretComponent(gameObject);
				if (activeTurret != null)
				{
					activeTurret.transform.localPosition = Vector3.zero;
					activeTurret.transform.localRotation = Quaternion.identity;
					activeTurret.TurretComponent = this;
					activeTurret.ActiveUnitComponents = UnitComponents.ActiveUnitComponents;
					activeTurret.Init();
				}
				return gameObject;
			}
			return null;
		}

		protected override void onAttachedToUnit(UnitComponentHolder unit)
		{
			base.onAttachedToUnit(unit);
			unit.Turrets.Add(this);
			unit.UpdateTurretRangesWithTurret(this);
			if (TurretClass.UsesAmmo)
			{
				unit.AnyTurretUsesAmmo = true;
			}
			if (TurretClass.AIConventionalWeapon)
			{
				unit.conventionalTurretCount++;
				unit.IsArmedWithConventionalWeapons = true;
				if (TurretControllerSpawner.ShouldAutomateTurrets(unit.Unit))
				{
					TurretControllerSpawner.SetupAutoTurretModule(unit.Unit);
					AutoFire = true;
				}
			}
			unit.InvalidateCombatRating();
			if (TurretClass.IsPointDefence)
			{
				AutoFire = true;
				UnitComponents.UpdateEstimatedPointDefenceEffectiveness();
			}
		}

		protected override void onDetachedFromUnit(UnitComponentHolder unit)
		{
			base.onDetachedFromUnit(unit);
			if (TurretClass.AIConventionalWeapon)
			{
				unit.conventionalTurretCount--;
			}
			unit.Turrets.Remove(this);
			unit.InvalidateCombatRating();
			unit.UpdateTurretRanges();
			unit.RefreshIsArmed();
			unit.RefreshAnyTurretUsesAmmo();
			if (TurretClass.IsPointDefence)
			{
				unit.UpdateEstimatedPointDefenceEffectiveness();
			}
		}

		protected override void rechargeTick(float elapsedTime)
		{
			if (!isCharged && CanChargeTurret())
			{
				chargeTurret(elapsedTime * UnitComponents.WeaponsPowerMultiplier);
			}
		}

		protected override void removeCharge()
		{
			ChargedEnergy = 0f;
		}

		protected override void rechargeFull()
		{
			base.rechargeFull();
			ChargedEnergy = TurretClass.EnergyCost;
		}

		protected override void OnUserPoweredChanged()
		{
			base.OnUserPoweredChanged();
			if (!IsPoweredAndEnergySupplied && CanCancelFire())
			{
				CancelFiring();
			}
		}

		protected virtual ActiveTurret AddActiveTurretComponent(GameObject g)
		{
			return null;
		}

		protected virtual void onFired(Unit target)
		{
		}

		protected virtual void InactiveFire(Unit target)
		{
		}

		protected virtual void OnFiringCancelled()
		{
			if (activeTurret != null)
			{
				activeTurret.CancelFiring();
			}
		}

		private void chargeTurret(float elapsedTime)
		{
			float a = TurretClass.EnergyCost - chargedEnergy;
			float b = TurretClass.EnergyChargeRate * EnergySupply * elapsedTime;
			float num = Mathf.Min(Mathf.Min(a, b), Unit.Components.CapacitorCharge);
			float num2 = num;
			if (num2 > 0f)
			{
				if (NormalizedDamage > 0f)
				{
					num2 *= GetChargeMultiplierFromDamage();
				}
				num2 = Mathf.Max(num2, 0.0001f);
				ChargedEnergy += num2;
				Unit.Components.CapacitorCharge -= num;
			}
		}

		private float GetChargeMultiplierFromDamage()
		{
			if (HealthNormalized < 1f)
			{
				return 1f - NormalizedDamage * Engine.GameSettings.ComponentDamageEffectSettings.DamagedTurretChargeRateEffect;
			}
			return 1f;
		}

		public bool IsPositionInFiringArc(Vector3 position)
		{
			if (TurretClass.IgnoreFiringArc || Bay.WeaponArc.y >= 360f)
			{
				return true;
			}
			Vector3 localPosition = transform.InverseTransformPoint(position);
			return IsLocalPositionInFiringArcInternal(localPosition);
		}

		public bool IsLocalPositionInFiringArc(Vector3 localPosition)
		{
			if (Bay.WeaponArc.y >= 360f)
			{
				return true;
			}
			return IsLocalPositionInFiringArcInternal(localPosition);
		}

		private bool IsLocalPositionInFiringArcInternal(Vector3 localPosition)
		{
			Vector3 direction = Vector3.Normalize(localPosition);
			return IsLocalDirectionInFiringArcInternal(direction);
		}

		public bool IsLocalDirectionInFiringArc(Vector3 direction)
		{
			if (Bay.WeaponArc.y >= 360f)
			{
				return true;
			}
			return IsLocalDirectionInFiringArcInternal(direction);
		}

		private bool IsLocalDirectionInFiringArcInternal(Vector3 direction)
		{
			return Mathf.Abs(Geometry.WrapDegreesForBearing(Quaternion.LookRotation(direction, Vector3.up).eulerAngles.y)) <= Bay.WeaponArc.y / 2f;
		}

		/// <summary>
		/// Draws the arc this turret can shoot into, and how far it reaches.
		/// </summary>
		/// <remarks>
		/// WeaponArc.y is a number in an inspector and nothing else, and it
		/// decides whether this turret can ever hit anything. There was no way
		/// to see it, so editing one was guesswork and a wrong value was
		/// invisible until the turret sat there refusing to fire.
		///
		/// The bay's own forward is the centre of the arc, not the ship's,
		/// because that is what the test measures against - so a bay rotated
		/// to face aft draws its arc aft, which is the single most confusing
		/// thing to get wrong here and the reason it is drawn from Bay rather
		/// than from Unit.
		///
		/// The wedge is horizontal. That is not a simplification: the test
		/// wraps the direction's Y bearing and compares it to the arc, with no
		/// pitch anywhere in it, so a vertical cone would show a constraint the
		/// game does not have.
		///
		/// Selected-only. Every component on every ship drawing its own arc
		/// turns the scene view into a bowl of orange, and this is for editing
		/// one component at a time.
		/// </remarks>
		/// <summary>
		/// Draws the same thing the bay does, so selecting either works.
		/// </summary>
		/// <remarks>
		/// Drawing it twice is free when both are not selected, which is
		/// almost always, and neither is worth guarding.
		/// </remarks>
		private void OnDrawGizmosSelected()
		{
			ComponentGizmos.DrawBay(Bay);
		}

		public void UpdateAutoFire(float elapsedTime)
		{
			if (autoTurret == null)
			{
				autoTurret = CreateAutoTurret();
			}
			autoTurret.Update(elapsedTime);
		}

		public IAutoTurret CreateAutoTurret()
		{
			if (TurretClass.IsPointDefence)
			{
				return new PointDefenceAutoTurret(this);
			}
			return new AutoTurret(this);
		}

		public bool CanAutoFire()
		{
			return TurretClass.AIConventionalWeapon;
		}

		public override bool ShouldShowFiringArcSprite()
		{
			return !TurretClass.IgnoreFiringArc;
		}
	}
}
