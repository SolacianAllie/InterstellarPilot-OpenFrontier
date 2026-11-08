using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Pooling;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public abstract class ActiveTurret : MonoBehaviour
	{
		public delegate void FiredHandler(ActiveTurret sender);

		public delegate void FireStateChangedHandler(ActiveTurret sender, TurretFireState oldState);

		private ActiveUnitComponents activeUnitComponents;

		private int burstRoundsRemaining;

		private EngineASX engine;

		private AudioSource fireAudioSource;

		private AudioSource fireLoopAudioSource;

		private float firePrewarmStartTime;

		[SerializeField]
		private TurretFireState fireState;

		private float lastFireEnergy;

		public Quaternion LastFireInaccuracy = Quaternion.identity;

		protected float lastFireInitTime;

		[SerializeField]
		protected Unit lastFireTarget;

		[SerializeField]
		private float lastFireTime;

		public TurretComponent TurretComponent;

		protected Unit unit;

		public float MaxTargettingRange => TurretClass.MaxFiringRange * 0.9f;

		public float LastFireTime
		{
			get
			{
				return lastFireTime;
			}
			set
			{
				lastFireTime = value;
			}
		}

		public TurretClass TurretClass
		{
			get
			{
				if ((bool)TurretComponent)
				{
					return TurretComponent.TurretClass;
				}
				return null;
			}
		}

		public bool IsFiring => fireState != TurretFireState.NotFiring;

		public bool ParentUnitIsAlive
		{
			get
			{
				if (unit != null)
				{
					return !unit.IsDestroyed;
				}
				return false;
			}
		}

		public AudioSource LastFireAudioGameObject => fireAudioSource;

		public TurretFireState FireState
		{
			get
			{
				return fireState;
			}
			protected set
			{
				if (fireState == value)
				{
					return;
				}
				TurretFireState turretFireState = fireState;
				fireState = value;
				switch (fireState)
				{
				case TurretFireState.Prewarm:
					firePrewarmStartTime = Time.time;
					if (TurretClass.FireChargeAudioPrefab != null)
					{
						engine.PlayPooledAudioSource(TurretClass.FireChargeAudioPrefab, transform.position);
					}
					break;
				case TurretFireState.Firing:
					TurretComponent.RemoveCharge();
					if (!TurretClass.RequiresTarget || (lastFireTarget != null && lastFireTarget.Sector == Sector))
					{
						lastFireTime = Time.time;
						if (unit.IsActiveInEngine)
						{
							fireAudioSource = PlayFireSound();
							fireLoopAudioSource = PlayFireLoopSound();
						}
						LastFireInaccuracy = GenerateInaccuracy();
						burstRoundsRemaining--;
						if (TurretComponent.UsesAmmo)
						{
							TurretComponent.RemoveAmmoForFiring();
						}
						fire(lastFireTarget);
						if (Fired != null)
						{
							Fired(this);
						}
					}
					else
					{
						CancelFiring();
					}
					break;
				case TurretFireState.NotFiring:
					burstRoundsRemaining = 0;
					break;
				}
				OnFireStateChanged(turretFireState);
				if (FireStateChanged != null)
				{
					FireStateChanged(this, turretFireState);
				}
			}
		}

		public float LastFireEnergy => lastFireEnergy;

		public float ChargedEnergyNormalized => TurretComponent.ChargedEnergyNormalized;

		public bool AutoFire
		{
			get
			{
				return TurretComponent.AutoFire;
			}
			set
			{
				TurretComponent.AutoFire = value;
			}
		}

		public ActiveUnitComponents ActiveUnitComponents
		{
			get
			{
				return activeUnitComponents;
			}
			set
			{
				if (!(this.activeUnitComponents != value))
				{
					return;
				}
				ActiveUnitComponents activeUnitComponents = this.activeUnitComponents;
				this.activeUnitComponents = value;
				if (activeUnitComponents != null)
				{
					activeUnitComponents.ActiveTurrets.Remove(this);
					NpcPilot component = activeUnitComponents.GetComponent<NpcPilot>();
					if (component != null)
					{
						component.NotifyTurretRemoved(this);
					}
				}
				if (this.activeUnitComponents != null)
				{
					unit = ((this.activeUnitComponents != null) ? this.activeUnitComponents.Unit : null);
					this.activeUnitComponents.ActiveTurrets.Add(this);
					if (unit.NpcPilot != null)
					{
						unit.NpcPilot.NotifyTurretAdded(this);
					}
				}
				else
				{
					unit = null;
				}
			}
		}

		public UnitComponentHolder UnitComponents
		{
			get
			{
				if (activeUnitComponents != null)
				{
					return activeUnitComponents.UnitComponents;
				}
				return null;
			}
		}

		public Unit Unit => unit;

		public EngineASX Engine => engine;

		public Sector Sector => unit.Sector;

		public Faction Faction
		{
			get
			{
				if (unit != null)
				{
					return unit.Faction;
				}
				return null;
			}
		}

		public Vector3 SectorPosition => unit.Sector.ToLocalPosition(transform.position);

		public event FireStateChangedHandler FireStateChanged;

		public event FiredHandler Fired;

		public override string ToString()
		{
			return $"[ActiveTurret {name}]";
		}

		public void Init()
		{
			engine = EngineASX.Instance;
			onInit();
		}

		public bool IsTargetValid(Unit target)
		{
			if (target != null && target.IsValidAndNotDestroyed && target != unit && target.Sector == Sector)
			{
				return target.IsTargettable(Faction);
			}
			return false;
		}

		public void Update()
		{
			if (fireLoopAudioSource != null && fireState != TurretFireState.Firing && Time.time - lastFireTime > 1f)
			{
				CleanupFireLoopAudioSource();
			}
			UpdateFiring(Time.deltaTime);
			update();
		}

		public bool IsReadyToFire(Unit target, bool ignoreFiringArc = false)
		{
			if (!IsFiring)
			{
				if (TurretClass.RequiresTarget)
				{
					if (target != null)
					{
						return CanFireAt(target, ignoreFiringArc);
					}
					return false;
				}
				return true;
			}
			return false;
		}

		public bool Fire(Unit target)
		{
			if (!IsFiring)
			{
				lastFireTarget = target;
				lastFireEnergy = TurretComponent.TurretClass.EnergyCost;
				lastFireInitTime = Time.time;
				burstRoundsRemaining = GetFireBurstRounds();
				FireState = TurretFireState.Prewarm;
				return true;
			}
			return false;
		}

		public void ResetRecharge()
		{
			TurretComponent.RemoveCharge();
		}

		public virtual bool CanFireAt(Unit target, bool ignoreFiringArc = false)
		{
			if (target.Sector == Sector && target.UnitClass.CanFireAt && TurretClass.CanFireAt(target))
			{
				if (!ignoreFiringArc && !TurretComponent.ComponentClass.IgnoreFiringArc)
				{
					return IsPositionInFiringArc(target.transform.position);
				}
				return true;
			}
			return false;
		}

		public bool IsPositionInFiringArc(Vector3 position)
		{
			return TurretComponent.IsPositionInFiringArc(position);
		}

		public void CancelFiring()
		{
			FireState = TurretFireState.NotFiring;
		}

		protected virtual void onInit()
		{
		}

		protected virtual void onDestroy()
		{
		}

		protected virtual void OnNotEnoughEnergy()
		{
			CancelFiring();
		}

		protected virtual void ApplyEnergyCostPerSecond(float elapsedTime)
		{
			float num = TurretClass.EnergyCostPerSecond * elapsedTime;
			float charge = unit.Components.Capacitor.Charge;
			charge -= num;
			unit.Components.Capacitor.Charge = charge;
			if ((double)charge <= 0.0)
			{
				CancelFiring();
			}
		}

		protected virtual void UpdateBurstCharge()
		{
		}

		protected virtual void update()
		{
		}

		protected virtual int GetFireBurstRounds()
		{
			return 1;
		}

		protected virtual void fire(Unit target)
		{
		}

		protected virtual void onTargetChanged(Unit oldTarget, Unit newTarget)
		{
		}

		protected void FinishFiring()
		{
			if (fireState == TurretFireState.Firing)
			{
				if (burstRoundsRemaining > 0)
				{
					FireState = TurretFireState.BurstCharge;
				}
				else
				{
					FireState = TurretFireState.NotFiring;
				}
			}
		}

		protected virtual void OnFireStateChanged(TurretFireState oldFireState)
		{
		}

		protected virtual Quaternion GenerateInaccuracy()
		{
			return GenerateInaccuracyInternal();
		}

		protected Quaternion GenerateInaccuracyInternal(float customInaccuracyMultiplier = 1f)
		{
			if (GameController.Instance.GameSettings.TurretInaccuracyEnabled)
			{
				float inaccuracyMultiplier = TurretClass.InaccuracyMultiplier;
				if (inaccuracyMultiplier > 0f)
				{
					float num = GameController.Instance.GameSettings.TurretInaccuracyNormalWeight;
					if (TurretComponent.HealthNormalized < 1f)
					{
						num += TurretComponent.NormalizedDamage * engine.GameSettings.TurretInaccuracyDmgWeight;
					}
					num *= inaccuracyMultiplier * customInaccuracyMultiplier;
					num *= Mathf.Pow(Random.value, TurretClass.InaccuracyPower);
					float num2 = GameController.Instance.GameSettings.TurretMaxInaccuracyDegrees * num;
					if (Random.Range(0, 2) == 0)
					{
						num2 *= -1f;
					}
					float value = Random.value;
					return Quaternion.Euler(value * num2, (1f - value) * num2, 0f);
				}
			}
			return Quaternion.identity;
		}

		public void SafeDestroy()
		{
			Cleanup();
			Object.DestroyImmediate(this);
		}

		private void OnDestroy()
		{
			Cleanup();
		}

		private void Cleanup()
		{
			FireState = TurretFireState.NotFiring;
			ActiveUnitComponents = null;
			CleanUpAudioSources();
		}

		private void UpdateFiring(float elapsedTime)
		{
			switch (fireState)
			{
			case TurretFireState.Firing:
				if (TurretClass.EnergyCostPerSecond > 0f)
				{
					ApplyEnergyCostPerSecond(elapsedTime);
				}
				break;
			case TurretFireState.Prewarm:
				if (Time.time > firePrewarmStartTime + TurretClass.FirePrewarmTime)
				{
					if ((lastFireTarget == null && TurretClass.RequiresTarget) || (lastFireTarget != null && !IsTargetValid(lastFireTarget)))
					{
						CancelFiring();
					}
					else if (!TurretComponent.UsesAmmo || TurretComponent.HasRequiredAmmo)
					{
						FireState = TurretFireState.Firing;
					}
					else
					{
						CancelFiring();
					}
				}
				break;
			case TurretFireState.BurstCharge:
				UpdateBurstCharge();
				break;
			case TurretFireState.NotFiring:
				break;
			}
		}

		private AudioSource PlayFireSound()
		{
			if (TurretClass.FireAudioPrefab != null)
			{
				AudioSource audioSource = engine.PlayPooledAudioSource(TurretClass.FireAudioPrefab, transform.position);
				audioSource.transform.SetParent(transform, worldPositionStays: true);
				return audioSource;
			}
			return null;
		}

		private AudioSource PlayFireLoopSound()
		{
			if (TurretClass.FireLoopAudioSource != null)
			{
				AudioSource audioSource = engine.PlayPooledAudioSource(TurretClass.FireLoopAudioSource.gameObject, transform.position);
				audioSource.transform.SetParent(transform, worldPositionStays: true);
				return audioSource;
			}
			return null;
		}

		private void CleanUpAudioSources()
		{
			CleanupAudioSource(fireAudioSource);
			fireAudioSource = null;
			CleanupFireLoopAudioSource();
		}

		private void CleanupAudioSource(AudioSource a)
		{
			if (a != null && a.transform.parent == transform)
			{
				a.volume = 0f;
				a.transform.SetParent(null);
			}
		}

		private void CleanupFireLoopAudioSource()
		{
			if (fireLoopAudioSource != null && fireLoopAudioSource.transform.parent == transform)
			{
				if (!fireLoopAudioSource.gameObject.TryGetComponent<FadeOutAudioSource>(out var component))
				{
					component = fireLoopAudioSource.gameObject.AddComponent<FadeOutAudioSource>();
				}
				else
				{
					component.enabled = true;
				}
				fireLoopAudioSource.transform.SetParent(null);
			}
			fireLoopAudioSource = null;
		}
	}
}
