using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class UnitDamageParticlesInfo : UnitFX
	{
		public float Duration = 12f;

		private float expiryTime;

		private ParticleSystem flameParticleSystem;

		private bool isPlaying;

		private ParticleSystem plumeParticleSystem;

		public void Play()
		{
			isPlaying = true;
		}

		protected override void awake()
		{
			base.awake();
			ActiveUnit.DamageParticlesInfo.Add(this);
		}

		protected override void OnFxActive()
		{
			CreateParticles();
			expiryTime = Time.time + Duration;
		}

		protected override void update()
		{
			base.update();
			if (isPlaying && Time.time > expiryTime)
			{
				isPlaying = false;
			}
		}

		protected override bool ShouldBeActive()
		{
			if (isPlaying)
			{
				return base.ShouldBeActive();
			}
			return false;
		}

		protected override void RecycleObjects()
		{
			base.RecycleObjects();
			EngineASX engineASX = ActiveUnit.Engine;
			if (!(engineASX != null))
			{
				return;
			}
			if (plumeParticleSystem != null && plumeParticleSystem.transform.parent == transform)
			{
				plumeParticleSystem.Stop();
				if (engineASX.Pooler != null)
				{
					engineASX.Pooler.RecyclePoolObject(plumeParticleSystem.gameObject);
				}
				plumeParticleSystem = null;
			}
			if (flameParticleSystem != null && flameParticleSystem.transform.parent == transform)
			{
				flameParticleSystem.Stop();
				if (engineASX.Pooler != null)
				{
					engineASX.Pooler.RecyclePoolObject(flameParticleSystem.gameObject);
				}
				flameParticleSystem = null;
			}
		}

		private void CreateParticles()
		{
			if (plumeParticleSystem == null)
			{
				ParticleSystem random = ActiveUnit.ActiveUnitClass.DamageSmokeParticlePrefabs.GetRandom();
				plumeParticleSystem = ActiveUnit.Engine.PlayPooledParticleSystem(random.gameObject, ActiveUnit.transform.position, ActiveUnit.transform.rotation);
			}
			InitialiseParticleSystem(plumeParticleSystem);
			if (flameParticleSystem == null)
			{
				ParticleSystem random2 = ActiveUnit.ActiveUnitClass.DamageFlameParticlePrefabs.GetRandom();
				flameParticleSystem = ActiveUnit.Engine.PlayPooledParticleSystem(random2.gameObject, ActiveUnit.transform.position, ActiveUnit.transform.rotation);
			}
			InitialiseParticleSystem(flameParticleSystem);
		}

		private void InitialiseParticleSystem(ParticleSystem p)
		{
			p.transform.SetParent(transform);
			p.transform.position = transform.position;
			p.transform.localRotation = Quaternion.identity;
		}
	}
}
