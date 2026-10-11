using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.Core;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class DestructableUnit : MonoBehaviour
	{
		public delegate void DamagedHandler(DestructableUnit sender, Unit inflictor, Faction inflictorFaction, float damage);

		public bool AllowDestruction = true;

		private Unit unit;

		private float totalDamageReceived;

		public bool IsInvulnerable;

		private bool isDestroyed;

		[SerializeField]
		private float currentHealth = -1f;

		public bool AllowInstantDestruction => !unit.IsPlayerCurrentUnit;

		public float CurrentHealth
		{
			get
			{
				return currentHealth;
			}
			set
			{
				currentHealth = value;
			}
		}

		public float HealthNormalized
		{
			get
			{
				float maxHealth = unit.GetMaxHealth();
				if (maxHealth > 0f)
				{
					return currentHealth / maxHealth;
				}
				return 1f;
			}
			set
			{
				currentHealth = unit.UnitClass.maxHealth * value;
			}
		}

		public float TotalHullDamage => unit.UnitClass.maxHealth - currentHealth;

		public bool IsDestroyed
		{
			get
			{
				return isDestroyed;
			}
			set
			{
				if (isDestroyed != value)
				{
					isDestroyed = value;
					unit.RefreshIsValidAndNotDestroyed();
					if (isDestroyed)
					{
						EngineASX.Instance.RegisterDestroyedUnit(unit);
					}
				}
			}
		}

		public Unit Unit => unit;

		public bool IsHullDamaged => HealthNormalized < 1f;

		public float TotalDamageReceived
		{
			get
			{
				return totalDamageReceived;
			}
			set
			{
				totalDamageReceived = value;
			}
		}

		public float NormalizedDamage
		{
			get
			{
				return 1f - HealthNormalized;
			}
			set
			{
				HealthNormalized = 1f - value;
			}
		}

		public int HullRepairCost
		{
			get
			{
				UnitClass unitClass = unit.UnitClass;
				float num = unitClass.maxHealth - currentHealth;
				if (num < 0f)
				{
					num = 0f;
				}
				return Mathf.RoundToInt((float)unitClass.CalculateHullValue(num, EngineASX.Instance.EconomySettings) * unitClass.RepairCostMultiplier);
			}
		}

		public event DamagedHandler Damaged;

		public void Init(Unit unit)
		{
			this.unit = unit;
		}

		public UnitDamageInfo ApplyDamage(Vector3 sourceWorldPosition, Faction sourceFaction, Unit sourceUnit, DamageDirectType damageDirectType, DamageType damageType)
		{
			int shieldIndex = unit.GetShieldIndex(sourceWorldPosition);
			return ApplyDamage(sourceWorldPosition, sourceFaction, sourceUnit, damageType, damageDirectType, shieldIndex);
		}

		public UnitDamageInfo ApplyDamage(Vector3 sourceWorldPosition, Faction sourceFaction, Unit sourceUnit, DamageType damageType, DamageDirectType damageDirectType, int shieldIndex)
		{
			UnitDamageInfo damageInfo = default;

			// Every point of damage in the game ends here: projectiles,
			// beams, point defence, and the off-screen fire that exists so
			// that a battle in another sector still resolves.
			//
			// A client was resolving them into its own copy. It fired, its
			// own numbers came up, and the host - which is also simulating
			// that target, and does not know the shot happened - streamed the
			// target back at full health ten times a second. So the damage
			// appeared and then un-did itself in front of the player. Two
			// players shooting each other did nothing whatever.
			//
			// Refused rather than left to happen: a shot that visibly does
			// nothing, with a log line saying combat is not relayed yet, is
			// something a player can report. Damage that heals itself is not.
			if (Authority.Refused(
				"firing weapons",
				"damaged " + (unit == null ? "a ship" : "ship " + unit.UniqueId)
					+ " for " + damageType.Damage))
			{
				return damageInfo;
			}

			EngineASX instance = EngineASX.Instance;
			if (!isDestroyed && instance != null)
			{
				UnitComponentHolder components = unit.Components;
				float damage = damageType.Damage;
				damage *= instance.GameSettings.GameplaySettings.GlobalDamageMultiplier;
				instance.HandleUnitAttacked(unit, sourceFaction, sourceUnit, damage, damageDirectType);
				if (unit.UnitType != UnitType.Projectile)
				{
					if (unit.IsOwnedByPlayer)
					{
						damage *= GameController.Instance.CombatDifficultyLevel.PlayerDamageReceivedMultiplier;
					}
					if (sourceUnit != null && sourceUnit.IsOwnedByPlayer)
					{
						damage *= GameController.Instance.CombatDifficultyLevel.PlayerDamageDealtMultiplier;
					}
				}
				damageInfo.ShieldDamage = 0f;
				if (unit.Asteroid != null)
				{
					unit.Asteroid.RegisterDamageWithYield(damageType.MiningDamage, ref sourceWorldPosition, sourceUnit);
				}
				totalDamageReceived += damage;
				if (!IsInvulnerable && !isDestroyed && damage > 0f)
				{
					if (GameController.Instance.GameSettings.GameplaySettings.DecloakWhenReceiveDamage && unit.IsFullyCloaked)
					{
						unit.Components.CloakComponent.StartDecloak();
					}
					float remainingDamage = damage;
					if (components != null && GameController.Instance.GameSettings.DebugSettings.ShieldsEnabled && damageType.ShieldDamageType != ShieldDamageType.Ignore && components.ShieldEnabled)
					{
						components.DamageShields(damage, shieldIndex, damageType.ShieldDamageType, ref damageInfo, ref remainingDamage);
						// Open Frontier: the stock !IsShieldDepleted gate
						// suppressed the effect on the very hit that breaks
						// the shield - the one hit that SHOULD flash (the
						// depletion pop). Gate removed.
						if (shieldIndex > -1 && damageInfo.ShieldDamage > 0f && unit.IsActiveInEngine)
						{
							CreateShieldHitEffectIfNeeded(sourceWorldPosition, instance, ref damageInfo);
						}
					}
					Person ourPilot = null;
					if (components != null)
					{
						ourPilot = components.PilotPerson;
					}
					if (remainingDamage > 0f)
					{
						remainingDamage *= GameController.Instance.GameSettings.GameplaySettings.GlobalHullDamageMultiplier;
						if (AllowDestruction && currentHealth - remainingDamage <= 0f && unit.Components != null)
						{
							unit.Components.OnAboutToBeDestroyed(sourceUnit);
						}
						ApplyHullDamage(sourceFaction, sourceUnit, damageDirectType, components, remainingDamage, damageType.ComponentDamageMultiplier);
					}
					if (unit != null && !unit.IsDestroyed && unit.Components != null)
					{
						components.OnUnitDamagedButNotDestroyed(ref damageInfo, damage, sourceUnit, sourceFaction, ourPilot);
					}
					if (Damaged != null)
					{
						Damaged(this, sourceUnit, sourceFaction, damage);
					}
				}
			}
			return damageInfo;
		}

		private void ApplyHullDamage(Faction sourceFaction, Unit sourceUnit, DamageDirectType damageDirectType, UnitComponentHolder components, float hullDamage, float componentDamageMultiplier = 1f)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: Decreasing heatlh by {hullDamage}", this, 3);
			}
			float damage = Mathf.Min(hullDamage, currentHealth);
			if (components != null)
			{
				EngineASX.Instance.CargoLeakageController.LeakCargo(unit, damage);
			}
			ApplyHullDamageAndKillWhenDestroyed(hullDamage, sourceFaction, sourceUnit, damageDirectType, componentDamageMultiplier);
			if (unit.IsActiveInEngine && !isDestroyed)
			{
				PlayHullHitAudio();
			}
		}

		public void ApplyHullDamageAndKillWhenDestroyed(float baseDamage, Faction sourceFaction, Unit sourceUnit, DamageDirectType damageDirectType, float componentDamageMultiplier = 1f)
		{
			float hullHealthBeforeDamage = currentHealth;
			currentHealth -= baseDamage;
			KillUnitWhenDestroyedRecursive(sourceFaction, sourceUnit, damageDirectType);
			if (!isDestroyed)
			{
				UnitComponentHolder components = unit.Components;
				if (components != null)
				{
					components.ApplyHullDamageToComponents(baseDamage * componentDamageMultiplier, hullHealthBeforeDamage);
				}
				ActiveUnit activeUnit = unit.ActiveUnit;
				if (activeUnit != null && activeUnit.LastDistanceFromCamera < 3000f)
				{
					activeUnit.CreateDamageParticles(baseDamage);
				}
				EngineASX.Instance.DitchUnitModule.ConsiderForDitching(unit, sourceFaction, sourceUnit, damageDirectType);
				if (UnitCaptureModule.CanUnitBeCapturedBy(unit, sourceFaction))
				{
					EngineASX.Instance.UnitCaptureModule.ConsiderForCapture(unit, sourceFaction, baseDamage);
				}
			}
		}

		private void KillUnitWhenDestroyedRecursive(Faction sourceFaction, Unit sourceUnit, DamageDirectType damageDirectType)
		{
			if (!(currentHealth <= 0f))
			{
				return;
			}
			currentHealth = 0f;
			if (isDestroyed)
			{
				return;
			}
			if (AllowDestruction)
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"{this}: Destroying due to zero health...", this, 2);
				}
				KillUnitAndChildren(sourceUnit, sourceFaction, damageDirectType);
			}
			else
			{
				currentHealth = 0.001f;
			}
		}

		public void ClampHealth()
		{
			currentHealth = Mathf.Clamp(currentHealth, 0f, unit.UnitClass.maxHealth);
		}

		public void PlayHullHitAudio()
		{
			AudioSource hullHitAudio = unit.GetHullHitAudio();
			if (hullHitAudio != null)
			{
				EngineASX.Instance.PlayPooledAudioSource(hullHitAudio, transform.position);
			}
		}

		/// <summary>A unit has been destroyed, as opposed to merely removed.</summary>
		/// <remarks>
		/// Separate from Unit.UnitGone, and deliberately earlier. Removing a
		/// ship is what happens when one is sold, dismantled, ditched or
		/// expired, and none of those should look like a ship being shot
		/// down. This is the shot-down path, and it fires here rather than at
		/// the removal because the animated explosion plays in between: a
		/// client told only at the end would set off the whole animation a
		/// full animation-length late.
		///
		/// Raised on the unit, which the host already has, so a listener needs
		/// nothing else to identify it.
		/// </remarks>
		public static event System.Action<Unit> UnitKilled;

		public KillData KillUnitAndChildren(Unit attacker, Faction attackerFaction, DamageDirectType damageDirectType)
		{
			if (!isDestroyed)
			{
				KillData killData = new KillData();
				killData.KillerFaction = attackerFaction;
				killData.KillerUnit = attacker;
				killData.DamageDirectType = damageDirectType;
				KillNestedUnit(attacker, attackerFaction, killData);
				KilledUnitHandler.Handle(killData);
				return killData;
			}
			return null;
		}

		public void KillNestedUnit(Unit attacker, Faction attackerFaction, KillData killData)
		{
			if (isDestroyed)
			{
				return;
			}
			if (LogWrapper.LogMsgs)
			{
				string text = ((attacker != null) ? attacker.ToString() : "[None]");
				string text2 = ((attackerFaction != null) ? attackerFaction.ToString() : "[None]");
				int msgVerboseLevel = (unit.IsStationOrShip() ? 2 : 3);
				if (unit.IsMajorStation())
				{
					msgVerboseLevel = 1;
				}
				LogWrapper.Log("Killing " + unit.GetEditorName() + ": Attacker: " + text + " AttackingFaction: " + text2, this, msgVerboseLevel);
			}
			IsDestroyed = true;
			killData.KilledUnits.Add(new KilledUnit
			{
				Faction = unit.Faction,
				UnitClass = unit.UnitClass,
				UnitId = unit.UniqueId,
				Unit = unit
			});
			EngineASX instance = EngineASX.Instance;
			if (instance != null)
			{
				instance.NotifyUnitKilled(unit, unit.Sector, attacker, attackerFaction);
			}
			UnitComponentHolder components = unit.Components;
			unit.UpdateRigidBodyOnDeath();
			if (components != null)
			{
				components.Kill(attackerFaction, killData);
			}
			AnimatedUnitExplosion animatedUnitExplosion = null;
			ActiveUnit activeUnit = unit.ActiveUnit;
			if (activeUnit != null)
			{
				activeUnit.OnUnitDestroyed();
				animatedUnitExplosion = activeUnit.RequestAnimatedExplosion();
				if (animatedUnitExplosion != null)
				{
					activeUnit.DestructionAnimGameObject = animatedUnitExplosion;
				}
			}

			// Said here, beside the explosion, rather than at the removal.
			UnitKilled?.Invoke(unit);
			DestroyedUnitArgs args = new DestroyedUnitArgs
			{
				Destroyer = attacker,
				Animation = animatedUnitExplosion
			};
			unit.AwardPlayerWithKilledUnitXp(attackerFaction);
			instance.MissileLockController.RemoveMissileLocks(unit);
			unit.RaiseKilled(args);
		}

		[ContextMenu("Kill Unit Anonomously")]
		public bool KillAnonymously()
		{
			if (!IsDestroyed)
			{
				_ = currentHealth;
				KillUnitAndChildren(null, null, DamageDirectType.Direct);
				return true;
			}
			return false;
		}

		[ContextMenu("Kill Unit as player & player unit")]
		public void KillUnitAsPlayer()
		{
			_ = currentHealth;
			Faction localFaction = EngineASX.Instance.LocalFaction;
			KillUnitAndChildren(EngineASX.Instance.LocalUnit, localFaction, DamageDirectType.Direct);
		}

		public void KillUnitAsFaction(Faction faction)
		{
			_ = currentHealth;
			KillUnitAndChildren(null, faction, DamageDirectType.Direct);
		}

		public void RestoreHealth()
		{
			if (unit.CargoComponent != null)
			{
				unit.CargoComponent.SetHealthBasedOnVolume();
			}
			else
			{
				CurrentHealth = unit.UnitClass.maxHealth;
			}
		}

		public void Respawn()
		{
			if (LogWrapper.LogMsgs)
			{
				Debug.Log($"{this}: Respawning", this);
			}
			IsDestroyed = false;
			RestoreHealth();
			UnitComponentHolder components = unit.Components;
			if (components != null)
			{
				components.SetComponentsEnabled(enabled: true);
				components.RestoreComponentsHealth();
			}
			unit.FindParents();
			gameObject.SetActive(value: true);
		}

		public void CreateShieldHitEffectIfNeeded(Vector3 sourceWorldPosition, EngineASX eng, ref UnitDamageInfo damageInfo)
		{
			if (EngineASX.Instance.PerformanceSettings.ShowShieldHitEffects && unit.ActiveUnit.ActiveUnitClass.ShieldHitPrefab != null && eng.ShieldHitRenderer != null)
			{
				damageInfo.ShieldHitInfo = eng.ShieldHitRenderer.RequestShieldHit(unit, sourceWorldPosition, damageInfo.ShieldDamage);
			}
		}
	}
}
