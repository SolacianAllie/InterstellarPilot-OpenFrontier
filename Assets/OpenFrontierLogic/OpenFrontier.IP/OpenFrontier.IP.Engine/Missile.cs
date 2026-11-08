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

		/// <summary>
		/// Seconds of "close enough to where I am going" still owed before
		/// this missile stops steering. See
		/// <see cref="ProjectileClass.SteeringCommitTime"/>.
		/// </summary>
		private float commitRemaining;

		/// <summary>
		/// Whether the missile has given up steering and is simply coasting on
		/// the velocity it has.
		/// </summary>
		private bool committed;

		/// <summary>
		/// Whether this missile is allowed to hit anything yet. False from the
		/// moment it is fired until it has both waited out
		/// <see cref="ProjectileClass.TangibleDelay"/> and left the launcher
		/// behind by <see cref="ProjectileClass.TangibleRangeFromLauncher"/>.
		/// </summary>
		private bool tangible;

		/// <summary>
		/// <see cref="Time.time"/> at the moment this missile was initialised,
		/// which is what the spawn protection measures its delay from.
		/// </summary>
		/// <remarks>
		/// Time.time rather than the projectile's own FireTime, which is on the
		/// scenario clock. The spawn protection is about the missile being inside
		/// its launcher for the first moments of its life, and the scenario clock
		/// keeps running through pause and menus while Time.time does not - so
		/// returning from a menu into a missile that is still waiting out its
		/// spawn protection is the correct outcome, and the other way round would
		/// hand out the protection for free while the game was closed.
		/// </remarks>
		private float launchedAt;

		/// <summary>
		/// Where this missile was when it left the tube, which is what the
		/// spawn protection measures its clearance against.
		/// </summary>
		/// <remarks>
		/// Not the launcher's own position, which is the obvious thing to measure
		/// against and is wrong here for a reason worth writing down. A ship's
		/// transform origin is amidships and its missile bays can be a very long
		/// way from it, so a missile launched from a battleship's bow is already
		/// further from its launcher's origin than the clearance radius before it
		/// has moved at all - and the test passes instantly, which is exactly the
		/// bug it was written to prevent.
		///
		/// Measuring from the muzzle is also independent of hull size, which is
		/// what makes one authored number mean the same thing on a fighter and on
		/// a cruiser.
		/// </remarks>
		private Vector3 launchPosition;

		/// <summary>
		/// When this missile first found it had nothing to fly at, or -1 if it
		/// currently has something. See <see cref="CheckSelfDestruct"/>.
		/// </summary>
		private float blindSinceLaunch = -1f;

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
		/// How well this missile's seeker is currently notched: zero when the
		/// target is running away or flying straight at it, rising as the
		/// target turns its beam to the missile.
		/// </summary>
		/// <remarks>
		/// Read by a countermeasure, and it is the other half of one manoeuvre
		/// rather than a mechanic of its own. Notching is something you do
		/// side-on while deploying; neither half is much without the other, so
		/// a missile is only easy to jam when its target actually turned
		/// side-on to earn the weakness.
		///
		/// Zero before a seeker exists, so a missile nobody gave a seeker to is
		/// as easy to jam as one chasing an unnotched target.
		/// </remarks>
		public float NotchQuality => seeker != null ? seeker.Notch : 0f;

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
		/// How badly a flare is blinding this seeker, as a number rather than a
		/// yes or no.
		/// </summary>
		/// <remarks>
		/// The seeker scales its aim error by this, so a flare does not so much
		/// switch the seeker off as drag the aim point around wherever the flare
		/// happens to be sitting. That is what makes a flare something you deploy
		/// into a salvo rather than something that ends one, and it is why a
		/// flare dropped on the target's own bearing is the good one - that is
		/// the direction the error is pushed towards.
		/// </remarks>
		public float Dazzle => dazzle;

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
			rolledAgainstBy = null;
			dazzle = 0f;
			hasAquiredTarget = false;
			lastDotToTarget = 0f;
			lastTargetDist = 0f;
			oldTarget = null;
			isGuidanceActive = false;
			committed = false;
			commitRemaining = 0f;
			tangible = false;
			launchedAt = Time.time;
			launchPosition = transform.position;
			blindSinceLaunch = -1f;
			// Full thrust from the instant it leaves the tube.
			//
			// This used to be MinThrust, on the reasoning that a missile ramps up
			// once it is being guided. But guidance does not activate for a fifth
			// of a second to two thirds, and in that window the missile is
			// thrusting at a fifth of its power - so its speed converges on a
			// fifth of ThrustPower rather than all of it. That is not a missile
			// warming up, it is a missile unable to reach its own top speed, and
			// it is slow for the whole of the phase in which it is least able to
			// defend itself.
			thrust = 1f;
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

		/// <summary>
		/// The countermeasure puff that has already had its roll against this
		/// missile, whether it landed or not.
		/// </summary>
		/// <remarks>
		/// This is what makes a puff worth deploying rather than a coin to flip
		/// repeatedly. The roll used to happen every 0.3 seconds for as long as
		/// the cloud was in the air, so a puff that missed on the first look
		/// simply looked again - seven or eight times over a two second jam
		/// window. Against effectiveness 0.8 that is a per-missile miss
		/// probability of 0.2^7, which is not a roll at all: the ECM would
		/// break every lock in front of it on its own and the notch would be
		/// worth nothing but a fraction of a second.
		///
		/// So each puff gets exactly one roll per missile, recorded here. The
		/// record lives on the missile rather than in a set on the puff for two
		/// reasons: it costs one reference instead of a collection per puff in
		/// the air, and a missile that drifts out of one puff's radius and back
		/// into it is correctly not re-rolled by that same puff, while a second
		/// puff arriving is still free to try.
		/// </remarks>
		private Countermeasure rolledAgainstBy;

		public bool HasRolledAgainst(Countermeasure puff) => rolledAgainstBy == puff;

		public void NoteRollAgainst(Countermeasure puff)
		{
			rolledAgainstBy = puff;
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
				Fly();
				CheckSelfDestruct();
			}
			UpdateCollision();
			if (ProjectileClass.TimedDestruction)
			{
				UpdateFuel();
			}
		}

		/// <summary>
		/// Where the missile is actually going, or zero while it is too slow to
		/// have a direction at all.
		/// </summary>
		private Vector3 CurrentVelocityDirection()
		{
			Vector3 velocity = RBody.linearVelocity;
			return velocity.sqrMagnitude > 0.0004f ? velocity.normalized : Vector3.zero;
		}

		/// <summary>
		/// One frame of flight: the main thruster along the nose, the sideways
		/// thrusters against the difference between where the missile is going
		/// and where it has been told to go, and the nose following the
		/// velocity afterwards.
		/// </summary>
		/// <remarks>
		/// The order of those three is the whole model, and it is the opposite
		/// of the one this replaced.
		///
		/// It used to rotate the nose towards the aim point and then push along
		/// the nose, so the nose decided where the missile went and the velocity
		/// followed it within a frame. A launcher that handed its missile some
		/// sideways momentum was therefore corrected almost immediately, and the
		/// steering cone only ever described what the nose could be told to do.
		///
		/// Now the velocity is the state and the nose is told about it. The
		/// sideways thrusters push along the difference between the direction the
		/// missile is travelling and the direction it has been commanded to
		/// travel, which rotates the velocity over time rather than instantly,
		/// and the nose is then dragged round to match. Everything expensive
		/// about turning is now paid in flight distance, which is the only place
		/// it can honestly be paid.
		///
		/// It also means a missile fired from the side of a moving ship has to
		/// spend real time nulling the sideways velocity it was launched with.
		/// That is the launcher's momentum being spent as flight time, and it is
		/// the cost of firing from a ship that is moving.
		/// </remarks>
		private void Fly()
		{
			Rigidbody body = RBody;
			if (body == null)
			{
				return;
			}
			float step = Time.deltaTime;
			body.AddRelativeForce(Vector3.forward * thrust * ProjectileClass.ThrustPower * step, ForceMode.Impulse);
			if (!ProjectileClass.IsHoming)
			{
				return;
			}
			Vector3 travelling = CurrentVelocityDirection();
			if (travelling == Vector3.zero)
			{
				// Standing still, so there is no momentum for the sideways jets to
				// push against and no direction of travel to correct. Attitude
				// does not need momentum, though - there is no air to resist a
				// nose swinging round - so the missile still points itself at the
				// target and lets the main thruster get it going. Without this a
				// missile launched with no inherited velocity would be frozen on
				// whatever heading it left the tube on.
				Vector3 wanted = SeekDirection();
				if (wanted.sqrMagnitude > 0.000001f)
				{
					transform.rotation = Quaternion.RotateTowards(
						transform.rotation,
						Quaternion.LookRotation(wanted.normalized, Vector3.up),
						ProjectileClass.TurnRate * step);
				}
				return;
			}
			Vector3 commanded = CommandedDirection(travelling);
			Vector3 correction = commanded - travelling;
			if (correction.sqrMagnitude > 0.000001f)
			{
				Vector3 push = correction.normalized * (ProjectileClass.LateralThrustPower * step);
				// A thruster cannot cancel more momentum than the vehicle has.
				// Without this a missile moving at a couple of metres a second
				// has its velocity reversed outright in a single frame by an
				// impulse sized for its top speed, and it weaves instead of
				// turning.
				float momentum = body.mass * body.linearVelocity.magnitude;
				if (push.magnitude > momentum)
				{
					push = push.normalized * momentum;
				}
				body.AddForce(push, ForceMode.Impulse);
			}
			// Last, and only now, the nose catches up with where the missile is
			// really going. TurnRate still governs how fast, so a heavy missile
			// can be one that both accelerates hard and slews slowly.
			transform.rotation = Quaternion.RotateTowards(
				transform.rotation,
				Quaternion.LookRotation(travelling, Vector3.up),
				ProjectileClass.TurnRate * step);
		}

		/// <summary>
		/// The direction the sideways thrusters should be trying to turn the
		/// missile towards, already capped to the steering cone.
		/// </summary>
		/// <remarks>
		/// Returns <paramref name="travelling"/> once the missile has committed,
		/// which makes the correction zero and the thrusters stop firing
		/// altogether. Committing matters here in a way it would not with a
		/// nose-led missile: with thrusters the missile is always firing
		/// something, and a missile that never stops correcting spends its own
		/// speed sideways down the length of its flight and arrives slower than
		/// one that pointed and went.
		/// </remarks>
		private Vector3 CommandedDirection(Vector3 travelling)
		{
			if (committed)
			{
				return travelling;
			}
			Vector3 wanted = SeekDirection();
			if (Vector3.Angle(wanted, travelling) <= ProjectileClass.SteeringCommitAngle)
			{
				if (commitRemaining <= 0f)
				{
					commitRemaining = ProjectileClass.SteeringCommitTime;
				}
				commitRemaining -= Time.deltaTime;
				if (commitRemaining <= 0f)
				{
					committed = true;
					return travelling;
				}
			}
			else
			{
				// Back outside the commit angle, so the hold starts again. A
				// target that crosses the nose briefly must not be able to make
				// the missile give up the approach.
				commitRemaining = ProjectileClass.SteeringCommitTime;
			}
			return CapToSteeringCone(wanted);
		}

		/// <summary>
		/// A missile that has never had anything to fly at gives up and
		/// detonates, rather than drifting on through the engagement.
		/// </summary>
		/// <remarks>
		/// This replaces a speed test, which is what the reference uses and which
		/// was wrong for this game in a way that detonated every missile on its
		/// first frame.
		///
		/// Their rule is 'the engine is off and the missile is below a speed',
		/// because their motors burn out on fuel and a dead motor is a real state.
		/// Ours has no such state: thrust is modulated between MinThrust and one,
		/// never to zero, so a missile always converges on some fraction of
		/// ThrustPower and never falls through a speed floor for the reason the
		/// reference means.
		///
		/// Worse, it caught the launch phase. Guidance does not activate for a
		/// fifth of a second and the missile spends that window at MinThrust, so
		/// its speed was heading for a fifth of ThrustPower while the floor sat
		/// at three tenths - and every missile in the game was below its own
		/// floor before it had done anything at all.
		///
		/// What is actually true here is that a missile can be a rock: it can be
		/// fired at a target it never sees, never lase, or never has a launcher
		/// picture on, and then it has nothing to steer by for the rest of its
		/// life. That is worth ending, and it can be tested directly rather than
		/// inferred from speed.
		/// </remarks>
		private void CheckSelfDestruct()
		{
			float delay = ProjectileClass.SelfDestructDelay;
			if (delay <= 0f || seeker == null)
			{
				return;
			}
			if (seeker.HasPicture)
			{
				blindSinceLaunch = -1f;
				return;
			}
			if (blindSinceLaunch < 0f)
			{
				blindSinceLaunch = Time.time;
				return;
			}
			if (Time.time - blindSinceLaunch >= delay)
			{
				projectile.FinalizeProjectile(createExplosion: true, null, Target, transform.position);
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
			if (IsTangible && ProjectileClass != null && ProjectileClass.CollisionEnabled && Target != null && projectile.CollisionDelayExpired())
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

		/// <summary>
		/// Whether this missile is allowed to hit anything, which it is not
		/// until it has both waited out
		/// <see cref="ProjectileClass.TangibleDelay"/> and left the ship that
		/// fired it well behind.
		/// </summary>
		/// <remarks>
		/// This is the direct answer to a missile killing its own launcher, and
		/// it is the reference's rule: a missile starts intangible and is only
		/// made solid once it is clear of the owner.
		///
		/// Both conditions are needed. Time alone is not enough, because a
		/// missile fired from a very fast ship covers a great deal of ground in
		/// half a second and would be solid again while still alongside its own
		/// launcher; distance alone is not enough either, because the launcher may
		/// be sitting still with the missile drifting out of it at walking pace
		/// and technically outside the radius for a frame while still inside the
		/// hull.
		///
		/// It also has to require the missile to be moving away. A ship turning
		/// hard while it fires sweeps its own missile sideways, and a missile
		/// that has cleared the radius but is still being carried along with the
		/// hull is inside it in every way that matters.
		/// </remarks>
		public bool IsTangible
		{
			get
			{
				if (tangible)
				{
					return true;
				}
				if (Time.time < launchedAt + ProjectileClass.TangibleDelay)
				{
					return false;
				}
				Unit launcher = projectile.SourceUnit;
				float range = ProjectileClass.TangibleRangeFromLauncher;
				if (range > 0f)
				{
					// Measured from the muzzle, not from the launcher. See
					// launchPosition - a bay on a large hull is already further
					// from the ship's origin than the clearance radius, so
					// measuring from the launcher lets a battleship's own missile
					// out of its spawn protection before it has moved.
					if ((transform.position - launchPosition).sqrMagnitude < range * range)
					{
						return false;
					}
					// And it has to be separating from the launcher rather than
					// being carried along with the hull. A ship turning hard as it
					// fires sweeps its own missile sideways, and one that has
					// cleared the radius is still inside the ship in every way
					// that matters.
					//
					// toLauncher points FROM the missile TO the launcher, so a
					// missile flying away from it has a velocity opposing that
					// vector and the dot product is negative. Negative therefore
					// means separated, and is the case that passes. This had it
					// the other way round, which meant a missile could only ever
					// become solid while it was still flying towards the ship
					// that fired it - so nothing it was chasing was ever hit.
					if (launcher != null)
					{
						Vector3 toLauncher = launcher.transform.position - transform.position;
						Vector3 travel = RBody.linearVelocity;
						if (travel.sqrMagnitude > 0.0001f && Vector3.Dot(toLauncher, travel) > 0f)
						{
							return false;
						}
					}
				}
				tangible = true;
				return true;
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
		}

		/// <summary>
		/// How far off its own nose a missile may be told to point, in degrees.
		/// </summary>
		/// <remarks>
		/// The single most important difference between this and the reference
		/// game, and it is one line.
		///
		/// A missile cannot be commanded to face more than forty five degrees
		/// away from where it is already flying. It can still turn at whatever
		/// rate TurnRate allows - that is unchanged - but the command itself is
		/// capped, so turning hard enough puts the aim point outside the cone
		/// and the missile flies an arc toward it instead of snapping onto it.
		///
		/// That is the whole reason a missile is a counter to manoeuvring rather
		/// than a counter to everything. A target that turns inside the arc
		/// goes somewhere the missile cannot point at yet, the cone test fails
		/// while it is swinging, and by the time the missile has come round the
		/// target has turned again. Without the cap a missile puts its nose on
		/// a target directly astern in a single frame and manoeuvring is worth
		/// nothing at all.
		///
		/// It also quietly makes a whole set of existing limits matter again -
		/// the cone, the persistence timer, the seeker's blind tolerance - which
		/// were all built assuming a missile could snap around and were
		/// therefore doing very little.
		public const float SteeringConeHalfAngle = 45f;

		/// <summary>
		/// Turns towards whatever the seeker decided to aim at, which is the
		/// target itself while the seeker has it and a dead-reckoned guess
		/// while it does not.
		/// </summary>
		/// <summary>
		/// Where the seeker's picture says this missile should be going, before
		/// the steering cone is applied to it.
		/// </summary>
		/// <remarks>
		/// Purely a question about direction now. It used to end by rotating the
		/// transform as well, which is what made the nose the thing that decided
		/// where the missile went; the rotation now happens once per frame in
		/// <see cref="Fly"/>, after the sideways thrusters have had their say, so
		/// the nose follows the velocity rather than leading it.
		/// </remarks>
		private Vector3 SeekDirection()
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
			Vector3 aim;
			// Always solve for the interception point, and fall back on the ship's
			// present position only when there is no solution to find.
			//
			// This used to be gated on the target being inside
			// MissileIntelligentGuidanceMaxDist, and past that range the missile
			// aimed straight at where the ship was. That is the worst possible aim
			// point and the further off it gets the worse it is: a Lancer closing
			// on a target crossing at the speed of the fastest ship in the game
			// spends about eleven seconds in the air from three kilometres, and
			// by then the ship is over a kilometre from where the missile is
			// pointing. It is not a long-range penalty, it is a guarantee of
			// missing, and it is why the missiles felt like they did not work at
			// all.
			//
			// The fallback is the solver's own answer, not a distance test.
			// GetInteceptPosition returns false when the quadratic has no real
			// root - which is the only situation in which aiming at the ship is
			// the best that can be done, because no lead exists.
			//
			// The distance field is gone rather than left unread. The Small Rocket
			// keeps MissileIntelligentGuidance off entirely, which is the honest
			// way to author a dumb rocket, and that flag is still doing its job.
			if (ProjectileClass.MissileIntelligentGuidance)
			{
				Vector3 interception = Vector3.zero;
				if (Geometry.GetInteceptPosition(ref sourcePosition, maxSpd, ref target, ref targetVelocity, out interception))
				{
					aim = interception - sourcePosition;
				}
				else
				{
					aim = target - sourcePosition;
				}
			}
			else
			{
				aim = target - sourcePosition;
			}
			return aim;
		}

		/// <summary>
		/// Pulls a commanded direction back inside <see cref="SteeringConeHalfAngle"/>
		/// of where the missile is actually travelling.
		/// </summary>
		/// <remarks>
		/// RotateTowards moves <c>from</c> toward <c>to</c> by at most the given
		/// angle, which is exactly the clamp wanted: a direction already inside
		/// the cone is untouched, and one outside it comes back to the cone edge.
		///
		/// The reference it is clamped against is the velocity, not the nose.
		/// That is the difference that makes the cone mean anything in this
		/// model. Clamped against the nose it only ever described what the missile
		/// could be told to point at; clamped against the velocity it describes
		/// how far the missile may be asked to turn in one go, which is what a
		/// pilot jinking to stay outside it is actually doing.
		///
		/// A degenerate direction - no velocity yet, or a zero-length aim - has
		/// no bearing to be outside of, so it falls back to the nose rather than
		/// to nothing.
		/// </remarks>
		private Vector3 CapToSteeringCone(Vector3 commanded)
		{
			Vector3 travelling = CurrentVelocityDirection();
			if (commanded.sqrMagnitude <= 0.000001f)
			{
				return travelling == Vector3.zero ? transform.forward : travelling;
			}
			if (travelling == Vector3.zero)
			{
				return commanded.normalized;
			}
			// The four argument form is the only one Unity 6 has, and the deltaTime
			// of one is what makes it a pure angle clamp rather than a step -
			// which is the same call the reference makes for the same reason.
			return Vector3.RotateTowards(
				travelling,
				commanded.normalized,
				SteeringConeHalfAngle * Mathf.Deg2Rad,
				1f);
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
