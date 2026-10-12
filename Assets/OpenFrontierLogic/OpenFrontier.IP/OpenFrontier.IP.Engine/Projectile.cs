using System;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OpenFrontier.IP.Engine
{
	public class Projectile : MonoBehaviour
	{
		private Countermeasure countermeasure;

		private DamageType damageType = new DamageType();

		private EngineASX engine;

		private double fireTime;

		private Missile missile;

		public ProjectileClass ProjectileClass;

		private float remainingMovement;

		private Unit sourceUnit;

		private Unit target;

		internal Collider targetCollider;

		private Rigidbody targetRigidBody;

		private Unit unit;

		private Rigidbody rBody;

		internal Vector3 targetLocalPosition;

		public Rigidbody RBody => rBody;

		public bool IsMine => ProjectileClass.IsMine;

		public Missile Missile => missile;

		public bool IsMissile => missile != null;

		public EngineASX Engine => engine;

		public Faction SourceFaction => unit.Faction;

		public double FireTime
		{
			get
			{
				return fireTime;
			}
			set
			{
				fireTime = value;
			}
		}

		public double LifeTime => engine.ScenarioElapsedTime - fireTime;

		public Unit Unit => unit;

		public Unit Target
		{
			get
			{
				return target;
			}
			set
			{
				if (target != value)
				{
					target = value;
					targetRigidBody = null;
					targetCollider = null;
					if (target != null)
					{
						AssignTargetcollider();
						targetRigidBody = ((target != null) ? target.RBody : null);
					}
				}
			}
		}

		public Unit SourceUnit
		{
			get
			{
				return sourceUnit;
			}
			set
			{
				sourceUnit = value;
			}
		}

		public DamageType Damage
		{
			get
			{
				return damageType;
			}
			set
			{
				damageType = value;
			}
		}

		public float RemainingMovement
		{
			get
			{
				return remainingMovement;
			}
			set
			{
				remainingMovement = value;
			}
		}

		public float RemainingTimeBeforeFuelDepletionAtMaxMoveSpeed => remainingMovement / ProjectileClass.MoveSpeed;

		public void Initialise(Sector sector, ProjectileClass projectileClass, ActiveTurret sourceActiveTurret, Unit target, Vector3? targetPosition, Vector3 targetLocalPosition)
		{
			if (sector == null)
			{
				throw new NullReferenceException("sector");
			}
			if (sourceActiveTurret == null)
			{
				throw new NullReferenceException("sourceActiveUnit");
			}
			this.targetLocalPosition = targetLocalPosition;
			transform.SetParent(sector.transform);
			this.unit.SetUniqueId(-1);
			this.unit.hasInit = false;
			this.unit.Init(autoFindParents: false);
			this.unit.Sector = sector;
			fireTime = this.unit.Engine.ScenarioElapsedTime;
			ProjectileClass = projectileClass;
			sourceUnit = sourceActiveTurret.Unit;
			SetDamage(sourceActiveTurret.TurretClass);
			this.unit.Faction = ((sourceUnit != null) ? sourceUnit.Faction : null);
			Target = target;
			remainingMovement = projectileClass.MaxMoveRange;
			if ((bool)countermeasure)
			{
				transform.rotation = sourceActiveTurret.transform.rotation;
			}
			else if (!IsMissile || !ProjectileClass.IsHoming)
			{
				Vector3 adjustedTargetPosition = GetAdjustedTargetPosition(ref targetPosition);
				if (ProjectileClass.RotateTowardsTarget && !ProjectileClass.IsHoming && targetPosition.HasValue)
				{
					SetRotationToTarget(adjustedTargetPosition);
				}
				if (projectileClass.TimedDestruction)
				{
					ApplyTimedExplosion(projectileClass, target, adjustedTargetPosition);
				}
				if (IsMine)
				{
					transform.rotation = sourceActiveTurret.transform.rotation;
				}
			}
			else if (ProjectileClass.IsHoming)
			{
				transform.rotation = sourceActiveTurret.transform.rotation;
			}
			else
			{
				transform.rotation = sourceActiveTurret.transform.rotation;
			}
			ApplyInaccuracy(ref sourceActiveTurret.LastFireInaccuracy);
			if (rBody != null)
			{
				rBody.linearVelocity = Vector3.zero;
			}
			if (ProjectileClass.InheritParentVelocity)
			{
				Unit unit = SourceUnit;
				if (rBody != null && unit != null && unit.RBody != null)
				{
					rBody.linearVelocity = unit.RBody.linearVelocity * EngineASX.Instance.GameSettings.ProjectileVelocityInheritanceFactor * ProjectileClass.InheritParentVelocityMultiplier;
				}
			}
			if (missile != null)
			{
				missile.Init();
				missile.LoseTargetDistance = sourceActiveTurret.TurretClass.MaxFiringRange * 1.1f;
			}
			if (countermeasure != null)
			{
				countermeasure.Init();
				if (ProjectileClass.InheritParentVelocity && sourceUnit.RBody != null)
				{
					countermeasure.Velocity = sourceUnit.RBody.linearVelocity * EngineASX.Instance.GameSettings.ProjectileVelocityInheritanceFactor * ProjectileClass.InheritParentVelocityMultiplier;
				}
				countermeasure.Velocity += transform.forward * ProjectileClass.MoveSpeed;
			}
			this.unit.Killed += unit_Killed;
			if (this.unit.Destructable != null)
			{
				this.unit.Destructable.IsDestroyed = false;
				this.unit.Destructable.RestoreHealth();
			}
		}

		public bool IsTargetValid(Unit target)
		{
			if ((bool)target && target.gameObject.activeSelf)
			{
				return !target.IsDocked;
			}
			return false;
		}

		public bool FinalizeWhenNoFuel(float fuelReduction)
		{
			remainingMovement -= fuelReduction;
			if ((double)remainingMovement < 0.0)
			{
				FinalizeProjectile(ProjectileClass.CreateExplosionWhenExpired, null, target, transform.position);
				return true;
			}
			return false;
		}

		public void OnHitTarget(Unit target, Vector3 hitPosition)
		{
			if (engine != null)
			{
				if (target != null && GameController.Instance.GameSettings.DebugSettings.DamageEnabled && target.Destructable != null && target.IsValid)
				{
					target.Destructable.ApplyDamage(hitPosition, unit.Faction, sourceUnit, DamageDirectType.Direct, damageType);
				}
				FinalizeProjectile(createExplosion: true, null, target, hitPosition);
			}
		}

		public void FinalizeProjectile(bool createExplosion, Transform explosionParent, Unit target, Vector3 hitPosition)
		{
			if (((ProjectileClass.ExplosionClass != null) & createExplosion) && unit.Sector != null)
			{
				if (unit.Sector.IsActive)
				{
					ProjectileClass.ExplosionClass.CreateExplosionEffect(engine, hitPosition, explosionParent);
				}
				ProjectileClass.ExplosionClass.ApplyToAllSectorUnits(engine, unit.Sector, hitPosition, unit.Faction, sourceUnit, unit, target);
			}
			unit.CleanupUnit();
			if (engine.Pooler != null)
			{
				TrailRenderer[] componentsInChildren = GetComponentsInChildren<TrailRenderer>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					componentsInChildren[i].Clear();
				}
				engine.Pooler.RecyclePoolObject(gameObject);
			}
		}

		private void Awake()
		{
			countermeasure = GetComponent<Countermeasure>();
			unit = GetComponent<Unit>();
			engine = EngineASX.Instance;
			missile = GetComponent<Missile>();
			rBody = GetComponent<Rigidbody>();
		}

		private void unit_Killed(Unit sender, DestroyedUnitArgs args)
		{
			sender.Killed -= unit_Killed;
			FinalizeProjectile(createExplosion: true, null, target, transform.position);
		}

		private Vector3 GetAdjustedTargetPosition(ref Vector3? targetPosition)
		{
			Vector3 pointOfInterception = Vector3.zero;
			if (ProjectileClass.RotateTowardsTarget && targetPosition.HasValue && !ProjectileClass.IsHoming && !CalculateInitialHeading(ProjectileClass, targetPosition.Value, out pointOfInterception))
			{
				pointOfInterception = target.transform.position;
			}
			return pointOfInterception;
		}

		private void SetRotationToTarget(Vector3 worldPosition)
		{
			transform.rotation = Quaternion.LookRotation(Vector3.Normalize(worldPosition - transform.position), Vector3.up);
		}

		private void ApplyTimedExplosion(ProjectileClass projectileClass, Unit target, Vector3 pointOfInterception)
		{
			if (target != null)
			{
				remainingMovement = (pointOfInterception - transform.position).magnitude + UnityEngine.Random.value * projectileClass.TimedExplosionInaccuracy;
			}
		}

		private void AssignTargetcollider()
		{
			targetCollider = null;
			if (Target != null && Target.ActiveUnit != null && ProjectileClass.CollisionEnabled)
			{
				targetCollider = Target.DamageCollider;
			}
		}

		private void CheckTargetHasCollider()
		{
			if (Target != null && Target.ActiveUnit != null && ProjectileClass.CollisionEnabled && targetCollider == null && LogWrapper.LogMsgs)
			{
				Debug.LogWarning($"Projectile {this} fired against target which has no collider. Will not be able to collide. Target:{Target}");
			}
		}

		private void ApplyInaccuracy(ref Quaternion fireInaccuracy)
		{
			transform.rotation *= fireInaccuracy;
		}

		private void SetDamage(TurretClass turretClass)
		{
			damageType.ShieldDamageType = ProjectileClass.ShieldDamageType;
			if (ProjectileClass.SetDamageUsingEnergyCost)
			{
				damageType.Damage = turretClass.GetRandomDamageFromEnergy() / (float)ProjectileClass.BurstRounds;
			}
			else
			{
				damageType.Damage = ProjectileClass.GetFuzzyDamage();
			}
			damageType.MiningDamage = damageType.Damage * ProjectileClass.MiningDamageMultiplier;
		}

		private bool CalculateInitialHeading(ProjectileClass projectileClass, Vector3 targetPosition, out Vector3 pointOfInterception)
		{
			Vector3 sourcePosition = transform.position;
			Vector3 targetVelocity = Vector3.zero;
			if (targetRigidBody != null)
			{
				targetVelocity = targetRigidBody.linearVelocity;
			}
			float moveSpeed = projectileClass.MoveSpeed;
			return Geometry.GetInteceptPosition(ref sourcePosition, moveSpeed, ref targetPosition, ref targetVelocity, out pointOfInterception);
		}

		private void Update()
		{
			if (target != null && !IsTargetValid(Target))
			{
				Target = null;
			}
			if (ProjectileClass != null && ProjectileClass.MaxDuration > 0f && engine.ScenarioElapsedTime - FireTime > (double)ProjectileClass.MaxDuration)
			{
				FinalizeProjectile(createExplosion: false, null, Target, transform.position);
			}
		}

		private void FixedUpdate()
		{
			if (ProjectileClass != null && !IsMissile && countermeasure == null)
			{
				Vector3 currentPosition = transform.position;
				float num = ProjectileClass.MoveSpeed * Time.deltaTime;
				transform.Translate(Vector3.forward * num, Space.Self);
				if ((!(ProjectileClass.MaxMoveRange > 0f) || !FinalizeWhenNoFuel(num)) && ProjectileClass.CollisionEnabled && Target != null && CollisionDelayExpired())
				{
					Vector3 direction = transform.forward;
					CheckCollision(ref currentPosition, ref direction, num);
				}
			}
		}

		public bool CollisionDelayExpired()
		{
			return engine.ScenarioElapsedTime > fireTime + (double)ProjectileClass.DestroyOnCollideDelayTime;
		}

		public void CheckCollision(ref Vector3 currentPosition, ref Vector3 direction, float movement)
		{
			if (targetCollider != null)
			{
				Ray ray = new Ray(currentPosition, direction);
				if (targetCollider.Raycast(ray, out var _, movement * 1.5f))
				{
					OnHitTarget(target, transform.position);
				}
			}
		}

		private void OnDisable()
		{
			if (unit != null)
			{
				unit.Killed -= unit_Killed;
			}
		}
	}
}
