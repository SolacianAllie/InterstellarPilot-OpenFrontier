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

		/// <summary>
		/// Decides where this missile flies and whether it can still see its
		/// target. Null until <see cref="Init"/>, which is the only place a
		/// missile becomes a missile.
		/// </summary>
		private MissileSeeker seeker;

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

		/// <summary>
		/// How badly dazzled this seeker is, and how fast that wears off.
		/// </summary>
		/// <remarks>
		/// A flare does not break a lock the way an ECM disrupts one - it
		/// blinds, and a blind seeker recovers. So this decays rather than
		/// latching, and the IR seeker asks about it on every evaluation instead
		/// of losing the target outright. That is what makes a flare a thing you
		/// deploy into a salvo rather than a thing that ends one: the flare
		/// burns out, the seeker comes back, and whether the missile got close
		/// enough in between is the fight.
		/// </remarks>
		[SerializeField]
		private float dazzle;

		private const float DazzleDecayPerSecond = 0.75f;

		/// <summary>Whether a flare is currently blinding this seeker.</summary>
		public bool IsDazzled => dazzle > 0f;

		/// <summary>
		/// Blinds this seeker for a while. Called by a flare, not by the
		/// seeker - the flare finds the missiles, the same way an ECM does.
		/// </summary>
		public void AddDazzle(float amount)
		{
			if (amount > 0f)
			{
				dazzle += amount;
			}
		}

		public Projectile Projectile => projectile;

		public Rigidbody RBody => projectile?.RBody;

		/// <summary>
		/// Whether there is still a ship here worth chasing.
		/// </summary>
		/// <remarks>
		/// This used to also reject a fully cloaked target, which made cloak
		/// defeat every missile in the game outright. Cloak is a seeker
		/// question now - each type answers it differently, and none of them
		/// answers it here - so it is asked by <see cref="MissileSeeker"/>
		/// instead, and this is only asking whether the thing still exists.
		/// </remarks>
		public bool IsTargetValid
		{
			get
			{
				Unit target = projectile.Target;
				if (target != null)
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
			dazzle = 0f;
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
			seeker?.LoseLock();
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
			if (dazzle > 0f)
			{
				dazzle = Mathf.Max(0f, dazzle - DazzleDecayPerSecond * Time.deltaTime);
			}
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
			if (seeker == null)
			{
				// A projectile can be seen for a frame before its owner has
				// called Init. Build the seeker here as well rather than
				// throwing on the first Update.
				seeker = new MissileSeeker(this);
				seeker.Reset(transform.position);
			}
			seeker.Update(Time.time, Time.deltaTime);
			if (projectile.Target == null)
			{
				// The seeker gave up. Nothing to steer towards, so fly on
				// rather than sit there holding the last thrust it had.
				thrust = 1f;
				return;
			}
			UpdateTargetDistAndBearing();
			// Whether this is a warning worth sounding, and it is the same two
			// things it has always been - the seeker can see the target, and
			// the target is close enough to matter. What has changed is that
			// "can see it" is now the seeker's real answer rather than a bare
			// dot threshold, so a missile that has been shaken off or blinded
			// stops counting as one.
			hasAquiredTarget = seeker.HasLock && lastTargetDist <= LoseTargetDistance;
			if (!seeker.HasPicture)
			{
				// Nothing to fly at and nothing it was told to fly at - a
				// semi-active missile whose launcher had no lock on the target.
				// Full thrust, dead ahead, until it runs out of time. Its seeker's
				// aim point is still the position it was fired from, and steering
				// at that would turn it around to go back where it came from.
				thrust = 1f;
				return;
			}
			lastDotToTarget = lastTargetDist > 0.0001f ? Vector3.Dot(lastTargetVector / lastTargetDist, transform.forward) : 1f;
			ControlThrust();
			RotateTowardsPoint();
		}

		/// <summary>
		/// Turns towards whatever the seeker decided to aim at, which is the
		/// target itself while the seeker has it and a dead-reckoned guess
		/// while it does not.
		/// </summary>
		private void RotateTowardsPoint()
		{
			Vector3 sourcePosition = transform.position;
			bool locked = seeker != null && seeker.HasLock;
			Vector3 target;
			if (locked && Target != null)
			{
				// While locked this is the old behaviour exactly, aim point
				// offset included, so nothing changes for a missile that never
				// loses its cone.
				target = Target.transform.TransformPoint(projectile.targetLocalPosition);
			}
			else
			{
				// Blind: fly the seeker's dead-reckoned guess. Only reachable
				// before Init has run, in which case going straight is right.
				target = seeker != null ? seeker.AimPoint : sourcePosition + transform.forward * 100f;
			}
			Vector3 targetVelocity = seeker != null ? seeker.AimVelocity : Vector3.zero;
			if (ProjectileClass.MissileIntelligentGuidance && lastTargetDist < ProjectileClass.MissileIntelligentGuidanceMaxDist)
			{
				Vector3 interception = Vector3.zero;
				if (Geometry.GetInteceptPosition(ref sourcePosition, maxSpd, ref target, ref targetVelocity, out interception))
				{
					transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(interception - sourcePosition), ProjectileClass.TurnRate * Time.deltaTime);
				}
			}
			else
			{
				transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(target - sourcePosition), ProjectileClass.TurnRate * Time.deltaTime);
			}
		}

		private void UpdateTargetDistAndBearing()
		{
			lastTargetVector = Target.transform.position - transform.position;
			lastTargetDist = lastTargetVector.magnitude;
		}

		private void UpdateTarget()
		{
			// The lock the rest of the game hears about is one the seeker has
			// actually held, not one that was merely assigned at launch.
			// GetMissileLocks is read directly by the warning controller and by
			// the HUD's lock buttons, neither of which filters on whether the
			// missile can see anything - so a missile that has never held its
			// target must not be in that list.
			//
			// For a laser-guided round fired with no beam on the target that is
			// not a subtle distinction. The missile is a rock. It should not
			// set off a missile alarm, exactly as it did not before it had a
			// seeker.
			Unit current = null;
			if (ProjectileClass.IsHoming && seeker != null && seeker.HasEverLocked && Target != null)
			{
				current = Target;
			}
			if (current != oldTarget)
			{
				if (oldTarget != null)
				{
					projectile.Engine.MissileLockController.RemoveMissileLock(this, oldTarget);
				}
				oldTarget = current;
				if (oldTarget != null)
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
