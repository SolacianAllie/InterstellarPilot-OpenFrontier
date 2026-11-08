using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class ActiveLaserTurret : ActiveTurret
	{
		private static DamageType damageType = new DamageType();

		protected bool hasHitTarget;

		protected float laserLength;

		[SerializeField]
		private ActiveLaserState laserState;

		private float laserStateChangeTime;

		public LaserTurretComponent LaserTurret;

		private LaserTurretClass laserTurretClass;

		protected float? lastDistToTarget;

		private LineRenderer lastFireLineRenderer;

		protected Vector3 lastFireSubTargetPosition = Vector3.zero;

		protected Vector3 lastLaserHitRelativePosition = Vector3.zero;

		private ShieldHitInfo lastShieldHitInfo;

		private Vector3 lastTargetSectorPosition = Vector3.zero;

		private MaterialPropertyBlock lineRenderedMaterialPropertyBlock;

		public LaserTurretClass LaserTurretClass
		{
			get
			{
				return laserTurretClass;
			}
			set
			{
				laserTurretClass = value;
			}
		}

		public ActiveLaserState LaserState
		{
			get
			{
				return laserState;
			}
			set
			{
				if (laserState == value)
				{
					return;
				}
				ActiveLaserState oldState = laserState;
				laserState = value;
				laserStateChangeTime = Time.time;
				switch (laserState)
				{
				case ActiveLaserState.Firing:
					laserLength = 0f;
					if (lastFireLineRenderer != null)
					{
						SetLineColors(1f);
					}
					break;
				case ActiveLaserState.NotActive:
					CancelFiring();
					break;
				}
				OnLaserStateChanged(oldState);
			}
		}

		public Unit LastFireTarget => lastFireTarget;

		protected override void onInit()
		{
			base.onInit();
			laserTurretClass = (LaserTurretClass)TurretComponent.TurretClass;
			laserState = ActiveLaserState.NotActive;
			lineRenderedMaterialPropertyBlock = new MaterialPropertyBlock();
			lastDistToTarget = null;
		}

		protected override Quaternion GenerateInaccuracy()
		{
			return GenerateInaccuracyInternal(GameController.Instance.GameSettings.GameplaySettings.LaserInaccuracyMultiplier);
		}

		protected override void update()
		{
			base.update();
			if (!IsFiring)
			{
				return;
			}
			bool flag = false;
			if (lastFireTarget != null)
			{
				flag = IsLastFireTargetValid();
				if (flag)
				{
					UpdateLastTargetPosition();
				}
			}
			Vector3 vectorToTarget = LastFireInaccuracy * (lastTargetSectorPosition - SectorPosition);
			lastDistToTarget = vectorToTarget.magnitude;
			if (flag && laserTurretClass.ShieldDamageType != ShieldDamageType.Ignore && lastFireTarget.ShieldEnabled && !lastFireTarget.Components.ShieldComponent.IsShieldDepleted(GetTargetShieldIndex()))
			{
				lastDistToTarget -= lastFireTarget.UnitClass.ShieldRingRadius;
				if (lastDistToTarget < 0f)
				{
					lastDistToTarget = 0f;
				}
			}
			switch (LaserState)
			{
			case ActiveLaserState.Firing:
				IncreaseLaserLength();
				if (!hasHitTarget & flag)
				{
					float distance = 0f;
					Vector3 intersectPointWorld = Vector3.zero;
					if (DetectTargetUnitCollision(lastFireTarget, ref vectorToTarget, out distance, out intersectPointWorld))
					{
						lastLaserHitRelativePosition = lastFireTarget.transform.InverseTransformPoint(intersectPointWorld);
						hasHitTarget = true;
						laserLength = distance;
						OnLaserHitTarget();
						flag = IsLastFireTargetValid();
					}
				}
				if (!hasHitTarget && (Time.time - LastFireTime > laserTurretClass.FiringTimeout || (lastDistToTarget.HasValue && laserLength > lastDistToTarget * 2f)))
				{
					LaserState = ActiveLaserState.FadeOut;
				}
				break;
			case ActiveLaserState.HitTarget:
				if (!flag)
				{
					LaserState = ActiveLaserState.FadeOut;
					break;
				}
				if (Time.time - LastFireTime > laserTurretClass.Duration)
				{
					LaserState = ActiveLaserState.FadeOut;
				}
				UpdateShieldImpact();
				if (lastFireLineRenderer != null)
				{
					SetLineColors(1f);
				}
				break;
			case ActiveLaserState.FadeOut:
			{
				if (!hasHitTarget)
				{
					IncreaseLaserLength();
				}
				float num = Time.time - laserStateChangeTime;
				float num2 = 1f - num / laserTurretClass.FadeOutTime;
				if (num2 > 0f)
				{
					if (lastFireLineRenderer != null)
					{
						SetLineColors(num2);
					}
				}
				else
				{
					LaserState = ActiveLaserState.NotActive;
				}
				break;
			}
			}
			if (lastFireLineRenderer != null)
			{
				UpdateLineRenderer(flag, ref vectorToTarget);
			}
		}

		protected override void fire(Unit target)
		{
			if (lastFireTarget != null)
			{
				lastFireSubTargetPosition = Vector3.zero;
				lastFireLineRenderer = null;
				hasHitTarget = false;
				TryCreateLineRenderer();
				lastShieldHitInfo = null;
				LaserState = ActiveLaserState.Firing;
				base.fire(target);
				UpdateLastTargetPosition();
			}
		}

		protected void TryCreateLineRenderer()
		{
			if (laserTurretClass != null && laserTurretClass.LaserLineRendererPrefab != null)
			{
				GameObject pooledObjectOrCreate = Engine.Pooler.GetPooledObjectOrCreate(laserTurretClass.LaserLineRendererPrefab);
				pooledObjectOrCreate.SetActive(value: true);
				pooledObjectOrCreate.transform.SetParent(transform);
				pooledObjectOrCreate.transform.localPosition = Vector3.zero;
				pooledObjectOrCreate.transform.localRotation = Quaternion.identity;
				lastFireLineRenderer = pooledObjectOrCreate.GetComponent<LineRenderer>();
				if (lastFireLineRenderer != null)
				{
					lastFireLineRenderer.SetPosition(0, transform.position);
					lastFireLineRenderer.SetPosition(1, transform.position);
					lastFireLineRenderer.enabled = true;
				}
			}
		}

		protected override void OnFireStateChanged(TurretFireState oldFireState)
		{
			base.OnFireStateChanged(oldFireState);
			if (FireState != TurretFireState.Firing)
			{
				LaserState = ActiveLaserState.NotActive;
			}
		}

		protected virtual bool DetectTargetUnitCollision(Unit unit, ref Vector3 vectorToTarget, out float distance, out Vector3 intersectPointWorld)
		{
			distance = 0f;
			intersectPointWorld = Vector3.zero;
			if (lastFireTarget != null)
			{
				Collider damageCollider = lastFireTarget.DamageCollider;
				if (damageCollider != null && damageCollider.Raycast(new Ray(direction: (!(vectorToTarget != Vector3.zero)) ? TurretComponent.transform.forward : Vector3.Normalize(vectorToTarget), origin: transform.position), out var hitInfo, laserLength))
				{
					distance = hitInfo.distance;
					intersectPointWorld = hitInfo.point;
					return true;
				}
			}
			return false;
		}

		protected virtual void OnLaserHitTarget()
		{
			UpdateShieldImpact();
			if (GameController.Instance.GameSettings.PerformanceSettings.CreateImpactParticles)
			{
				CreateImpactParticles();
			}
			LaserState = ActiveLaserState.HitTarget;
			if (lastFireTarget != null)
			{
				ApplyDamage();
			}
		}

		protected virtual void OnLaserStateChanged(ActiveLaserState oldState)
		{
			if (laserState == ActiveLaserState.NotActive && lastFireLineRenderer != null)
			{
				if (Engine.Pooler != null)
				{
					lastFireLineRenderer.enabled = false;
					Engine.Pooler.RecyclePoolObject(lastFireLineRenderer.gameObject);
				}
				lastFireLineRenderer = null;
			}
		}

		private bool IsLastFireTargetValid()
		{
			if (lastFireTarget.IsTargettable(Faction) && lastFireTarget.Sector == Sector && lastFireTarget.ActiveUnit != null)
			{
				return lastFireTarget.DamageCollider != null;
			}
			return false;
		}

		private void UpdateLineRenderer(bool lastFireTargetValid, ref Vector3 vectorToTarget)
		{
			lastFireLineRenderer.SetPosition(0, transform.position);
			if (hasHitTarget & lastFireTargetValid)
			{
				Vector3 vector = lastFireTarget.transform.TransformPoint(lastLaserHitRelativePosition);
				vectorToTarget = vector - transform.position;
				laserLength = vectorToTarget.magnitude;
			}
			ClampLaserLength();
			lastFireLineRenderer.SetPosition(1, transform.position + Vector3.Normalize(vectorToTarget) * laserLength);
			lastFireLineRenderer.GetPropertyBlock(lineRenderedMaterialPropertyBlock);
			Vector2 vector2 = new Vector2(laserLength / laserTurretClass.LaserTextureRealWorldSize, 1f);
			float num = (Time.time - LastFireTime) * GameController.Instance.GameSettings.VideoSettings.LaserScaleRate;
			lineRenderedMaterialPropertyBlock.SetVector("_MainTex_ST", new Vector4(vector2.x, vector2.y, 0f - num, 0f));
			lastFireLineRenderer.SetPropertyBlock(lineRenderedMaterialPropertyBlock);
		}

		private void IncreaseLaserLength()
		{
			laserLength += laserTurretClass.LaserMoveSpd * Time.deltaTime;
			ClampLaserLength();
		}

		private void ClampLaserLength()
		{
			float num = laserTurretClass.MaxFiringRange * GameController.Instance.GameSettings.GameplaySettings.LaserOvershootMultiplier;
			if (laserLength > num)
			{
				laserLength = num;
			}
		}

		private void UpdateLastTargetPosition()
		{
			lastTargetSectorPosition = lastFireTarget.Sector.ToLocalPosition(lastFireTarget.transform.TransformPoint(lastFireSubTargetPosition));
		}

		private void UpdateShieldImpact()
		{
			// Open Frontier: the stock behavior reset the shield hit
			// effect's StartExpiryTime every frame the beam held,
			// pinning the envelope at t=0 (a permanent white flicker
			// while lased) - and once the effect died and its pool slot
			// was reused, this stale reference restarted OTHER effects'
			// timers. The flash now plays its full envelope once per
			// contact; the beam's own visual carries the sustained glow.
		}

		private void CreateImpactParticles()
		{
			if (!(Vector3.Distance(lastFireTarget.transform.position, GameController.Instance.MainCamera.transform.position) < 500f))
			{
				return;
			}
			for (int i = 0; i < laserTurretClass.ImpactPrefabs.Length; i++)
			{
				ParticleSystem particleSystem = laserTurretClass.ImpactPrefabs[i];
				if (particleSystem != null)
				{
					ParticleSystem pooledObject = Engine.GetPooledObject<ParticleSystem>(particleSystem.gameObject);
					pooledObject.transform.SetParent(lastFireTarget.transform);
					pooledObject.transform.localPosition = lastLaserHitRelativePosition;
					pooledObject.Play();
				}
			}
		}

		private int GetTargetShieldIndex()
		{
			return lastFireTarget.GetShieldIndex(transform.position);
		}

		private void SetLineColors(float alpha)
		{
			Color color = new Color(1f, 1f, 1f, alpha);
			Color startColor = color;
			startColor.a = 0.8f;
			lastFireLineRenderer.startColor = startColor;
			lastFireLineRenderer.endColor = color;
		}

		private void ApplyDamage()
		{
			float num = 1f;
			if (Engine.GameSettings.ReduceLaserDamageWithRange)
			{
				num = ReduceLaserDamageWithRange(num);
			}
			float num2 = LastFireEnergy / laserTurretClass.EnergyCost;
			float damage = laserTurretClass.GetRandomDamageFromEnergy() * num * num2;
			Vector3 sourePosition = lastFireTarget.transform.TransformPoint(lastLaserHitRelativePosition);
			applyDamage(ref sourePosition, damage);
		}

		private float ReduceLaserDamageWithRange(float damageDistMultiplier)
		{
			if (laserLength < laserTurretClass.MaxFiringRange)
			{
				float num = Random.Range(laserTurretClass.MaxRangeMinDamageMultiplier, laserTurretClass.MaxRangeMaxDamageMultiplier);
				damageDistMultiplier = num + (1f - laserLength / laserTurretClass.MaxFiringRange) * (1f - num);
			}
			return damageDistMultiplier;
		}

		private void applyDamage(ref Vector3 sourePosition, float damage)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"Applying damage {damage} to {lastFireTarget}", this, 3);
			}
			damageType.ShieldDamageType = laserTurretClass.ShieldDamageType;
			damageType.Damage = damage;
			damageType.MiningDamage = laserTurretClass.MiningDamageMultiplier * damage;
			if (!GameController.Instance.GameSettings.DebugSettings.DamageEnabled)
			{
				return;
			}
			if (lastFireTarget != null)
			{
				if (lastFireTarget.Destructable != null)
				{
					bool isDestroyed = lastFireTarget.Destructable.IsDestroyed;
					Faction faction = lastFireTarget.Faction;
					UnitDamageInfo unitDamageInfo = lastFireTarget.Destructable.ApplyDamage(lastFireTarget.transform.TransformPoint(lastLaserHitRelativePosition), Faction, unit, DamageDirectType.Direct, damageType);
					if (!isDestroyed && lastFireTarget.IsDestroyed)
					{
						OnDestroyedTarget(lastFireTarget, faction);
					}
					lastShieldHitInfo = unitDamageInfo.ShieldHitInfo;
				}
				else if (LogWrapper.LogMsgs)
				{
					Debug.LogError("Cannot applyDamage. Target does not have Destructable", this);
				}
			}
			else if (LogWrapper.LogMsgs)
			{
				Debug.LogError("Cannot applyDamage. Target is null", this);
			}
		}

		private void OnDestroyedTarget(Unit target, Faction targetFaction)
		{
			if (TurretComponent.IsMiningLaser && Faction == EngineASX.Instance.LocalFaction && target.IsStationOrShip() && targetFaction != null && targetFaction != EngineASX.Instance.LocalFaction && targetFaction.IsHostileTo(EngineASX.Instance.LocalFaction))
			{
				EngineASX.Instance.LocalPlayer.Stats.ShipsMinedToDeath++;
			}
		}
	}
}
