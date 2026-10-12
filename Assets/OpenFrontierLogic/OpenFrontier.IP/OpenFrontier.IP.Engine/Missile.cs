using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class Missile : MonoBehaviour
	{
		public const float CollisionRequiredDistance = 1f;

		private bool disrupted;

		private bool hasAquiredTarget;

		private float lastDotToTarget;

		private Vector3 lastTargetVector;

		private float lastTargetDist;

		private float maxSpd;

		private Unit oldTarget;

		private Projectile projectile;

		private float thrust = 1f;

		private bool isGuidanceActive;

		public float LoseTargetDistance = 1000f;

		private float timeWhenGuidanceActivates;

		public float Thrust
		{
			get
			{
				return thrust;
			}
			set
			{
				thrust = Mathf.Max(value, MinThrust);
			}
		}

		public Unit Target => projectile.Target;

		public ProjectileClass ProjectileClass => projectile.ProjectileClass;

		public bool IsDisrupted
		{
			get
			{
				return disrupted;
			}
			set
			{
				disrupted = value;
			}
		}

		public Projectile Projectile => projectile;

		public Rigidbody RBody => projectile?.RBody;

		public bool IsTargetValid
		{
			get
			{
				Unit target = projectile.Target;
				if (target != null && !target.IsFullyCloaked)
				{
					return !target.IsDocked;
				}
				return false;
			}
		}

		private float MinThrust => ProjectileClass.MinThrust;

		public bool HasAquiredTarget => hasAquiredTarget;

		public void Init()
		{
			disrupted = false;
			hasAquiredTarget = false;
			lastDotToTarget = 0f;
			lastTargetDist = 0f;
			oldTarget = null;
			isGuidanceActive = false;
			thrust = MinThrust;
			projectile.Unit.SetRBody();
			maxSpd = UnitEngineClass.CalculateMaxSpeed(ProjectileClass.ThrustPower, RBody.linearDamping, RBody.mass);
			timeWhenGuidanceActivates = Time.time + Random.Range(ProjectileClass.MinTimeBeforeGuidanceActivation, ProjectileClass.MaxTimeBeforeGuidanceActivation);
			if (Target != null)
			{
				UpdateTargetDistAndBearing();
			}
		}

		public void Disrupt(Unit target)
		{
			disrupted = true;
			projectile.Target = target;
			hasAquiredTarget = true;
		}

		public void LoseTarget()
		{
			hasAquiredTarget = false;
		}

		public void LoseTargetPermanently()
		{
			hasAquiredTarget = false;
			projectile.Target = null;
		}

		private void Awake()
		{
			projectile = GetComponent<Projectile>();
		}

		private void OnDisable()
		{
			projectile.Target = null;
			UpdateTarget();
		}

		private void Update()
		{
			UpdateTarget();
			if (ProjectileClass.IsHoming)
			{
				if (projectile.Target != null)
				{
					if (hasAquiredTarget && !IsTargetValid)
					{
						LoseTargetPermanently();
					}
					else if (isGuidanceActive)
					{
						UpdateHoming();
						ControlThrust();
					}
					else if (Time.time > timeWhenGuidanceActivates)
					{
						isGuidanceActive = true;
					}
				}
			}
			else
			{
				thrust = 1f;
			}
			if (ProjectileClass != null)
			{
				RBody.AddRelativeForce(Vector3.forward * thrust * ProjectileClass.ThrustPower * Time.deltaTime, ForceMode.Impulse);
			}
			UpdateCollision();
			if (ProjectileClass.TimedDestruction)
			{
				UpdateFuel();
			}
		}

		private void UpdateFuel()
		{
			float fuelReduction = RBody.linearVelocity.magnitude * Time.deltaTime;
			projectile.FinalizeWhenNoFuel(fuelReduction);
		}

		private void ControlThrust()
		{
			if (lastDotToTarget > MinThrust)
			{
				thrust = MinThrust + (lastDotToTarget - MinThrust) / (1f - MinThrust) * (1f - MinThrust);
			}
			else
			{
				thrust = MinThrust;
			}
		}

		private void UpdateCollision()
		{
			if (ProjectileClass != null && ProjectileClass.CollisionEnabled && Target != null && projectile.CollisionDelayExpired())
			{
				float num = RBody.linearVelocity.magnitude / 10f;
				Vector3 direction = transform.forward;
				Vector3 currentPosition = transform.position;
				if (num > 0f)
				{
					CheckMissileCollision(ref currentPosition, ref direction, num);
				}
			}
		}

		public void CheckMissileCollision(ref Vector3 currentPosition, ref Vector3 direction, float movement)
		{
			if (projectile.targetCollider != null)
			{
				Ray ray = new Ray(currentPosition, direction);
				if (projectile.targetCollider.Raycast(ray, out var hitInfo, movement + 1f))
				{
					projectile.OnHitTarget(Target, hitInfo.point);
				}
			}
		}

		private void UpdateHoming()
		{
			UpdateTargetDistAndBearing();
			if (lastTargetDist > LoseTargetDistance)
			{
				LoseTarget();
				return;
			}
			Vector3 lhs = lastTargetVector / lastTargetDist;
			lastDotToTarget = Vector3.Dot(lhs, transform.forward);
			RotateTowardsTarget();
			UpdateHomingTargetting();
		}

		private void UpdateTargetDistAndBearing()
		{
			lastTargetVector = Target.transform.position - transform.position;
			lastTargetDist = lastTargetVector.magnitude;
		}

		private void UpdateHomingTargetting()
		{
			if (!projectile.ProjectileClass.HomingCanLoseTarget)
			{
				return;
			}
			if (hasAquiredTarget)
			{
				if (lastDotToTarget < projectile.ProjectileClass.HomingLostTargetDotThreshold)
				{
					projectile.Target = null;
				}
			}
			else if (lastDotToTarget > projectile.ProjectileClass.HomingAquiredTargetThreshold)
			{
				hasAquiredTarget = true;
			}
		}

		private void RotateTowardsTarget()
		{
			Vector3 sourcePosition = transform.position;
			Vector3 target = Target.transform.TransformPoint(projectile.targetLocalPosition);
			if (ProjectileClass.MissileIntelligentGuidance && lastTargetDist < ProjectileClass.MissileIntelligentGuidanceMaxDist)
			{
				Vector3 targetVelocity = Vector3.zero;
				Rigidbody rBody = Target.RBody;
				if (rBody != null)
				{
					targetVelocity = rBody.linearVelocity;
				}
				float sourceSpd = maxSpd;
				Vector3 interception = Vector3.zero;
				if (Geometry.GetInteceptPosition(ref sourcePosition, sourceSpd, ref target, ref targetVelocity, out interception))
				{
					transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(interception - sourcePosition), ProjectileClass.TurnRate * Time.deltaTime);
				}
			}
			else
			{
				transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(target - sourcePosition), ProjectileClass.TurnRate * Time.deltaTime);
			}
		}

		private void UpdateTarget()
		{
			if (Target != oldTarget)
			{
				if (oldTarget != null && ProjectileClass.IsHoming)
				{
					projectile.Engine.MissileLockController.RemoveMissileLock(this, oldTarget);
				}
				oldTarget = Target;
				if (oldTarget != null && ProjectileClass.IsHoming)
				{
					projectile.Engine.MissileLockController.AddMissileLock(this, oldTarget);
				}
			}
		}

		public bool IsLockedOnTo(Unit unit)
		{
			if (hasAquiredTarget && !IsDisrupted)
			{
				return projectile.Target == unit;
			}
			return false;
		}
	}
}
