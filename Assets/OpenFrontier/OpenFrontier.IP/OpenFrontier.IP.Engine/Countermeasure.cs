using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class Countermeasure : MonoBehaviour
	{
		private Vector3 velocity = Vector3.zero;

		private const float projectileCheckFreq = 0.3f;

		public CountermeasureClass CountermeasureClass;

		private float effectiveness;

		private bool expended;

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
			nextCheck = 0f;
			expended = false;
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

		public Missile GetDisruptedMissile()
		{
			Faction sourceFaction = projectile.SourceFaction;
			if (sourceFaction != null && projectile.Engine.ScenarioElapsedTime - projectile.FireTime > (double)CountermeasureClass.MinTimeBeforeDisruption)
			{
				Collider[] array = Physics.OverlapSphere(transform.position, CountermeasureClass.MaxDisruptionRange, GameController.Instance.MissileLayer);
				for (int i = 0; i < array.Length; i++)
				{
					Missile component = array[i].GetComponent<Missile>();
					if (!component.IsDisrupted && Faction.IsFactionHostileTo(component.Projectile.Unit.Faction, sourceFaction))
					{
						float num = Vector3.Dot(Vector3.Normalize(transform.position - component.transform.position), component.transform.forward);
						float num2 = effectiveness * num;
						if (Random.value < num2)
						{
							return component;
						}
					}
				}
			}
			return null;
		}

		private void Awake()
		{
			projectile = GetComponent<Projectile>();
		}

		private void Update()
		{
			if (!expended && Time.time > nextCheck)
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
			if (!(effectiveness > Random.value))
			{
				return;
			}
			Missile disruptedMissile = GetDisruptedMissile();
			if (disruptedMissile != null)
			{
				if (GameController.Instance.GameSettings.DebugSettings.CountermeasureDisruptionEnabled)
				{
					disruptedMissile.Disrupt(projectile.Unit);
				}
				if (CountermeasureClass.ExpendAfterDisruption)
				{
					expended = true;
				}
			}
		}
	}
}
