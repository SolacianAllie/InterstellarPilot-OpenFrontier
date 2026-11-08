using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class Countermeasure : MonoBehaviour
	{
		private Vector3 velocity = Vector3.zero;

		private const float projectileCheckFreq = 0.3f;

		/// <summary>
		/// How much of the cloud's life it spends still being able to jam, as a
		/// fraction of the projectile class's MaxDuration.
		/// </summary>
		/// <remarks>
		/// Read off the projectile rather than stored, because MaxDuration is
		/// what actually takes the cloud off the board - deriving from it means
		/// the jam window can never outlive the thing it belongs to.
		///
		/// The scenario clock, not Time.time: MaxDuration is enforced on
		/// ScenarioElapsedTime, which advances on a fixed delta and keeps
		/// running while the game is paused. A jam window measured on a
		/// different clock would quietly disagree with the cloud it is part of.
		/// </remarks>
		private float JamDuration => (float)(projectile.ProjectileClass.MaxDuration * CountermeasureClass.JamFraction);

		/// <summary>
		/// Whether this puff is still in the part of its life where it can jam.
		/// </summary>
		public bool IsJamming => projectile.LifeTime < JamDuration;

		/// <summary>
		/// How much easier a fully notched missile is to jam than one whose
		/// target is running away from it.
		/// </summary>
		/// <remarks>
		/// This is the hinge between notching and jamming. Notching is a
		/// side-on activity - the ship turns its beam to the missile and
		/// deploys into the weakened lock - and neither half is much without
		/// the other. So the countermeasure's odds scale with the notch, and a
		/// seeker holding a sliver of a signature is a seeker worth attacking.
		///
		/// It is a multiplier on the roll rather than a flat addition because
		/// the roll is already a probability against effectiveness. It is also
		/// only worth anything because each puff rolls once per missile - see
		/// <see cref="Missile.HasRolledAgainst"/>. Rolling again on the next
		/// 0.3 second check is what made the ECM a guaranteed break and the
		/// notch decoration.
		///
		/// 1.75 is measured against the numbers, not chosen. The notch peaks at
		/// 0.25 - the biggest hull held beam-on, which is where the aspect
		/// falloff is halved by a hull that gets the full falloff - so this can
		/// only be worth a quarter of itself at the extreme. At 1.75 that is an
		/// ECM I going from 80 percent head-on to 95 against a Creon held
		/// beam-on, with the small hulls barely moving off 80 because a fighter
		/// has almost no flank to present. Two puffs at 95 percent is 90, which
		/// is a combination you have to mean.
		///
		/// The ceiling is 2.0, and that is a real one: at 2.0 the product passes
		/// one at maximum notch and ECM I becomes a guaranteed hit, which is
		/// ECM II's job and would leave nothing for the manoeuvre to buy.
		/// </remarks>
		private const float NotchedJamBonus = 1.75f;

		public CountermeasureClass CountermeasureClass;

		/// <summary>
		/// The flare half of this, or null for an ECM. Resolved once rather
		/// than asked for by type on every check, because this runs four times
		/// a second for every countermeasure in the air.
		/// </summary>
		private FlareCountermeasureClass flareClass;

		private float effectiveness;

		private float nextCheck;

		private Projectile projectile;

		public List<ParticleSystem> ParticleSystems = new List<ParticleSystem>();

		public Projectile Projectile => projectile;

		public float Effectiveness
		{
			get
			{
				return effectiveness;
			}
			set
			{
				effectiveness = value;
			}
		}

		public Vector3 Velocity
		{
			get
			{
				return velocity;
			}
			set
			{
				velocity = value;
			}
		}

		public void Init()
		{
			effectiveness = CountermeasureClass.Effectiveness;
			flareClass = CountermeasureClass as FlareCountermeasureClass;
			nextCheck = 0f;
			bool flag = Vector3.Distance(projectile.transform.position, GameController.Instance.MainCamera.transform.position) < GameController.Instance.GameSettings.PerformanceSettings.CountermeasureParticlesMaxDistance;
			foreach (ParticleSystem particleSystem in ParticleSystems)
			{
				particleSystem.gameObject.SetActive(flag);
				if (flag)
				{
					particleSystem.Clear();
					particleSystem.Play();
				}
			}
			velocity = Vector3.zero;
		}

		/// <summary>
		/// Rolls once against every hostile, undisrupted missile in range that
		/// this puff has not already tried.
		/// </summary>
		/// <remarks>
		/// There is no cap on how many this can defeat. There used to be one -
		/// five - and it was arbitrary: it did not describe anything about the
		/// fight, it just stopped the fifth missile and let the sixth through,
		/// which is the worst of both. What actually limits a puff is its
		/// radius, and that is a real handicap rather than a number chosen to
		/// make a salvo feel big. A salvo that has already closed inside the
		/// cloud is entirely answered; one still outside it is untouched, so
		/// deploying early enough to sit in the gap is the whole of the timing.
		/// </remarks>
		/// <returns>How many were disrupted by this call.</returns>
		public int DisruptMissilesInRange()
		{
			Faction sourceFaction = projectile.SourceFaction;
			if (sourceFaction == null || projectile.Engine.ScenarioElapsedTime - projectile.FireTime <= (double)CountermeasureClass.MinTimeBeforeDisruption)
			{
				return 0;
			}
			Collider[] array = Physics.OverlapSphere(transform.position, CountermeasureClass.MaxDisruptionRange, GameController.Instance.MissileLayer);
			int num = 0;
			for (int i = 0; i < array.Length; i++)
			{
				Missile component = array[i].GetComponent<Missile>();
				// Target check first: only a missile actually chasing the ship that
				// fired this ECM. Otherwise an ECM sat between two ships would
				// turn around missiles that were never going to reach it.
				if (component == null || component.IsDisrupted || component.Projectile == null
					|| component.Projectile.Target != projectile.Unit
					|| component.Projectile.Unit == null
					|| !Faction.IsFactionHostileTo(component.Projectile.Unit.Faction, sourceFaction))
				{
					continue;
				}
				// One roll per missile per puff, recorded whichever way it went.
				// Without this the roll repeated every 0.3 seconds for the whole
				// jam window and seven or eight attempts at 0.8 left a miss
				// probability of 0.2^7 - so the ECM broke every lock in front of
				// it by itself and the notch was worth nothing at all.
				if (component.HasRolledAgainst(this))
				{
					continue;
				}
				component.NoteRollAgainst(this);
				// Behind the missile the dot product goes negative and the roll is
				// impossible, which is right for the cheap countermeasure, which
				// should have to be pointed at the threat. The expensive one drops
				// the facing term, so anything in range is disrupted.
				//
				// The notch term is what makes notching and jamming one manoeuvre
				// rather than two. Notching is a side-on activity: the ship
				// turns its beam to the missile and deploys into the weakened
				// lock, so the odds of the deploy landing scale with how well
				// the turn was done. Flying bow-on is worth nothing here even
				// though it dims the ship most - a ship presenting its nose to
				// an incoming missile is not evading it.
				//
				// Neither a notch nor an ECM breaks a large hull's lock on its
				// own - hull sizes span a wider range than aspects do, so no
				// single signature threshold can tell 'notched' from 'astern'
				// across the whole ladder - but doing both at once is what
				// loses it.
				if (!CountermeasureClass.AffectsAllInRange)
				{
					float facing = Vector3.Dot(Vector3.Normalize(transform.position - component.transform.position), component.transform.forward);
					float notch = Mathf.Lerp(1f, NotchedJamBonus, component.NotchQuality);
					if (!(Random.value < effectiveness * facing * notch))
					{
						continue;
					}
				}
				if (!GameController.Instance.GameSettings.DebugSettings.CountermeasureDisruptionEnabled)
				{
					continue;
				}
				component.Disrupt(projectile.Unit);
				num++;
			}
			return num;
		}

		private void Awake()
		{
			projectile = GetComponent<Projectile>();
			// Publish this puff on its Unit so a seeker can find it without a
			// component lookup per candidate. It is a Unit of type Other, so
			// without this it is scored as a ship with no engines and sails
			// straight past the thing it exists to be.
			if (projectile != null && projectile.Unit != null)
			{
				projectile.Unit.CountermeasureCloud = this;
			}
		}

		/// <summary>
		/// What this cloud is worth to a heat seeker.
		/// </summary>
		/// <remarks>
		/// Constant for the whole life of the puff, and it used not to be. A
		/// spent puff - one that had already fooled something - used to report
		/// nothing, on the reasoning that it had stopped burning. But the jam
		/// window and the burn are now separate: the cloud jams for the first
		/// half of its life and is nothing but a decoy for the second, so
		/// "has jammed yet" and "is still hot" are different questions and only
		/// one of them should change the answer.
		///
		/// Ending it is the projectile's job, not this class's:
		/// <see cref="Projectile.MaxDuration"/> takes the cloud off the board,
		/// at which point there is nothing left for a seeker to be tempted by.
		///
		/// An ECM carries this as well as a flare does, which is the whole
		/// point of it: one puff answers a radar missile by jamming it and a
		/// heat missile by being more interesting than the ship.
		/// </remarks>
		public float IrSignature => CountermeasureClass.IrSignature;

		/// <summary>
		/// Blinds the infrared seekers chasing the ship that fired this, harder
		/// the closer the flare sits to the target's own bearing.
		/// </summary>
		/// <remarks>
		/// Shaped exactly like <see cref="DisruptMissilesInRange"/> - same
		/// sphere, same 0.3 second cadence, same rule that only missiles
		/// actually chasing the firing ship count, and the same half-life jam
		/// window. The one thing it does differently is that it never rolls for
		/// a hit: a flare works or it does not, and how much it works is a
		/// continuous function of where it was dropped, not a dice throw per
		/// missile.
		///
		/// It also does not record having dazzled a missile, because dazzle
		/// decays and a flare is a sustained burn rather than a single
		/// attempt. Stopping it early would let the seeker walk straight back
		/// onto the ship.
		///
		/// Returns zero for anything that is not a flare, so the ECM path is
		/// untouched by this existing.
		/// </remarks>
		public int DazzleMissilesInRange(float elapsed)
		{
			if (flareClass == null || !IsJamming)
			{
				return 0;
			}
			Faction sourceFaction = projectile.SourceFaction;
			if (sourceFaction == null || projectile.Engine.ScenarioElapsedTime - projectile.FireTime <= (double)CountermeasureClass.MinTimeBeforeDisruption)
			{
				return 0;
			}
			Collider[] array = Physics.OverlapSphere(transform.position, CountermeasureClass.MaxDisruptionRange, GameController.Instance.MissileLayer);
			int num = 0;
			for (int i = 0; i < array.Length; i++)
			{
				Missile component = array[i].GetComponent<Missile>();
				if (component == null || component.IsDazzled || component.Projectile == null
					|| component.Projectile.Target != projectile.Unit
					|| component.Projectile.Unit == null
					|| !Faction.IsFactionHostileTo(component.Projectile.Unit.Faction, sourceFaction))
				{
					continue;
				}
				component.AddDazzle(DazzleRateFor(component));
				num++;
			}
			return num;
		}

		/// <summary>
		/// How much dazzle one missile picks up from this flare, per second.
		/// </summary>
		/// <remarks>
		/// The alignment term is the whole mechanic. A flare sitting on the same
		/// bearing from the missile as the target does is the hardest possible
		/// thing to tell from the target; one off to the side is obviously not
		/// it, and dazzles for far less. AspectWeighting of zero removes the
		/// effect entirely, which is the honest way to author "this flare does
		/// not care where you put it".
		/// </remarks>
		private float DazzleRateFor(Missile missile)
		{
			Vector3 toTarget = missile.Projectile.Target.transform.position - missile.transform.position;
			Vector3 toFlare = transform.position - missile.transform.position;
			if (toTarget.sqrMagnitude <= 0.000001f || toFlare.sqrMagnitude <= 0.000001f)
			{
				return flareClass.DazzleStrength;
			}
			float alignment = Vector3.Dot(toTarget.normalized, toFlare.normalized);
			// 1 when the flare is exactly on the target's bearing, 0 when the
			// two are 90 degrees apart, and it keeps falling past that rather
			// than going negative - a flare behind the missile dazzles less,
			// not "backwards".
			alignment = Mathf.Clamp01(alignment);
			float weighted = Mathf.Lerp(1f - flareClass.AspectWeighting, 1f, alignment);
			return Mathf.Max(0f, flareClass.DazzleStrength * weighted);
		}

		private void Update()
		{
			// IsJamming reads the projectile, and this runs every frame rather
			// than on the 0.3 second cadence, so a missing Projectile component
			// would throw here continuously instead of once every third of a
			// second inside the check. A countermeasure always shares a pooled
			// GameObject with one, but a null is cheap to survive.
			if (projectile == null || !IsJamming || Time.time <= nextCheck)
			{
				return;
			}
			nextCheck = Time.time + projectileCheckFreq;
			if (!GameController.Instance.GameSettings.DebugSettings.CountermeasureFindDisruptedMissile)
			{
				return;
			}
			if (flareClass != null)
			{
				DazzleMissilesInRange(projectileCheckFreq);
				return;
			}
			DisruptMissilesInRange();
		}

		private void FixedUpdate()
		{
			velocity = Vector3.MoveTowards(velocity, Vector3.zero, CountermeasureClass.Drag * Time.deltaTime);
			transform.Translate(velocity * Time.deltaTime, Space.World);
		}
	}
}
