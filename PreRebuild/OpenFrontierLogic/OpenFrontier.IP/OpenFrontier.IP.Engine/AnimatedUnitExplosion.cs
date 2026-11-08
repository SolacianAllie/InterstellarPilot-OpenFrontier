using System;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OpenFrontier.IP.Engine
{
	public class AnimatedUnitExplosion : MonoBehaviour
	{
		public DebrisMaker DebrisPrefab;

		private EngineASX engine;

		private float expireTime;

		public ExplosionClass[] ExplosionClasses;

		public float ExplosionMaxInterval = 5f;

		public float ExplosionMinInterval = 2f;

		public ExplosionClass FinalExplosionClass;

		private bool instaDeath;

		public float InstaDeathProbability = 0.25f;

		public float MaxDuration = 10f;

		public float MinDuration = 1f;

		public float NextExplosionTime;

		public float HighDetailMaxDistance = 500f;

		[NonSerialized]
		public Unit Unit;

		public bool IsPlaying
		{
			get
			{
				if (gameObject.activeSelf)
				{
					return Unit != null;
				}
				return false;
			}
		}

		public void Play()
		{
			engine = Unit.Engine;
			bool flag = Unit.ActiveUnit == null || Unit.ActiveUnit.LastDistanceFromCamera > HighDetailMaxDistance;
			instaDeath = flag || (Unit.Destructable != null && Unit.Destructable.AllowInstantDestruction && UnityEngine.Random.value < InstaDeathProbability);
			if (instaDeath)
			{
				CreateFinalExplosion();
			}
			else
			{
				expireTime = Time.time + UnityEngine.Random.Range(MinDuration, MaxDuration);
			}
		}

		private void Update()
		{
			if (Unit != null)
			{
				transform.position = Unit.transform.position;
				if (Time.time > expireTime)
				{
					CreateFinalExplosion();
				}
				else if (Time.time > NextExplosionTime)
				{
					ExplosionClass random = ExplosionClasses.GetRandom();
					if (random != null)
					{
						CreateSmallExplosion(random);
					}
					NextExplosionTime = Time.time + UnityEngine.Random.Range(ExplosionMinInterval, ExplosionMaxInterval);
				}
			}
			else
			{
				Debug.LogWarning("Cleaning up AnimatedUnitExplosion because it has no unit reference", this);
				Cleanup();
			}
		}

		private void Cleanup()
		{
			Unit = null;
			if (engine.Pooler != null)
			{
				engine.Pooler.RecyclePoolObject(gameObject);
			}
		}

		public void CreateFinalExplosion()
		{
			if (FinalExplosionClass != null)
			{
				CreateExplosion(FinalExplosionClass, Unit.transform.position);
			}
			if (DebrisPrefab != null && Unit.ActiveUnit != null && Unit.ActiveUnit.LastDistanceFromCamera < 500f)
			{
				DebrisPrefab.CreateDebris(engine, Unit.transform.position);
			}
			Cleanup();
		}

		private void CreateSmallExplosion(ExplosionClass explosionClass)
		{
			Vector3 smallExplosionPosition = GetSmallExplosionPosition();
			explosionClass.CreateExplosionEffect(engine, smallExplosionPosition, Unit.transform);
		}

		private void CreateExplosion(ExplosionClass explosionClass, Vector3 position)
		{
			explosionClass.CreateExplosionEffect(engine, position);
		}

		private Vector3 GetSmallExplosionPosition()
		{
			if (Unit.ActiveUnit != null)
			{
				UnitDamageParticlesInfo random = Unit.ActiveUnit.DamageParticlesInfo.GetRandom();
				if (random != null)
				{
					return random.transform.position;
				}
				Debug.LogWarning("Could not get DamageParticlesInfo from ActiveUnit", this);
			}
			else
			{
				Debug.LogWarning("Positioning explosion using shield ring radius", this);
			}
			return transform.position + UnityEngine.Random.rotation * new Vector3(0f, 0f, Unit.UnitClass.ShieldRingRadius * 0.75f);
		}
	}
}
