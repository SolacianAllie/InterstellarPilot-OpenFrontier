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
		/// Hard cap on how many missiles one countermeasure can disrupt.
		/// </summary>
		/// <remarks>
		/// A cap on its own would have done nothing: GetDisruptedMissile
		/// returned on the first success, so a puff contesting five missiles
		/// rolled against exactly one of them - whichever OverlapSphere
		/// happened to return first. Over a long engagement that let one
		/// deployment turn around a dozen or more missiles, which is not
		/// what 'an ECM fools the missiles chasing me' is meant to mean.
		///
		/// Every missile in range is now rolled against, and this is the
		/// ceiling on the total. One deployment fools a salvo, then stops.
		/// </remarks>
		private const int MaxMissilesDisrupted = 5;

		public CountermeasureClass CountermeasureClass;

		/// <summary>
		/// The flare half of this, or null for an ECM. Resolved once rather
		/// than asked for by type on every check, because this runs four times
		/// a second for every countermeasure in the air.
		/// </summary>
		private FlareCountermeasureClass flareClass;

		private float effectiveness;

		private bool expended;

		private int disruptedMissiles;

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

		public bool Expended
		{
			get
			{
				return expended;
			}
			set
			{
				expended = value;
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
			expended = false;
			disruptedMissiles = 0;
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
		/// Rolls every hostile, undisrupted missile in range and disrupts up to
		/// whatever is left of this countermeasure's budget of five.
		/// </summary>
		/// <returns>How many were disrupted by this call.</returns>
		public int DisruptMissilesInRange()
		{
			if (disruptedMissiles >= MaxMissilesDisrupted)
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
				if (disruptedMissiles >= MaxMissilesDisrupted)
				{
					break;
				}
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
				// Behind the missile the dot product goes negative and the roll is
				// impossible, which is the behaviour that was already there.
				float facing = Vector3.Dot(Vector3.Normalize(transform.position - component.transform.position), component.transform.forward);
				if (!(Random.value < effectiveness * facing))
				{
					continue;
				}
				if (!GameController.Instance.GameSettings.DebugSettings.CountermeasureDisruptionEnabled)
				{
					continue;
				}
				component.Disrupt(projectile.Unit);
				disruptedMissiles++;
				num++;
			}
			return num;
		}

		private void Awake()
		{
			projectile = GetComponent<Projectile>();
		}

		/// <summary>
		/// Blinds the infrared seekers chasing the ship that fired this, harder
		/// the closer the flare sits to the target's own bearing.
		/// </summary>
		/// <remarks>
		/// Shaped exactly like <see cref="DisruptMissilesInRange"/> - same
		/// sphere, same 0.3 second cadence, same rule that only missiles
		/// actually chasing the firing ship count - because it is the same
		/// question asked of the same thing. The one thing it does differently
		/// is that it never rolls for a hit: a flare works or it does not, and
		/// how much it works is a continuous function of where it was dropped,
		/// not a dice throw per missile.
		///
		/// Returns zero for anything that is not a flare, so the ECM path is
		/// untouched by this existing.
		/// </remarks>
		public int DazzleMissilesInRange(float elapsed)
		{
			if (flareClass == null)
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
			if (expended)
			{
				return;
			}
			if (flareClass != null)
			{
				if (Time.time > nextCheck)
				{
					nextCheck = Time.time + projectileCheckFreq;
					if (GameController.Instance.GameSettings.DebugSettings.CountermeasureFindDisruptedMissile)
					{
						DazzleMissilesInRange(projectileCheckFreq);
					}
				}
				return;
			}
			if (disruptedMissiles < MaxMissilesDisrupted && Time.time > nextCheck)
			{
				nextCheck = Time.time + 0.3f;
				if (GameController.Instance.GameSettings.DebugSettings.CountermeasureFindDisruptedMissile)
				{
					PerformDisruptionCheck();
				}
			}
		}

		private void FixedUpdate()
		{
			velocity = Vector3.MoveTowards(velocity, Vector3.zero, CountermeasureClass.Drag * Time.deltaTime);
			transform.Translate(velocity * Time.deltaTime, Space.World);
		}

		private void PerformDisruptionCheck()
		{
			// No outer 'effectiveness' roll any more. It used to gate the whole
			// check AND be applied again per missile inside the loop, so ECM I
			// was really 0.8 x 0.8 head-on instead of the 0.8 its data says.
			if (DisruptMissilesInRange() > 0 && CountermeasureClass.ExpendAfterDisruption)
			{
				expended = true;
			}
		}
	}
}
