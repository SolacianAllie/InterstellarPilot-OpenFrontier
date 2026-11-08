using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class DebrisSimple : MonoBehaviour
	{
		public float Duration = 10f;

		private EngineASX engine;

		private float expireTime;

		public float MaxMoveSpeed = 10f;

		public float MaxRotationSpeed = 100f;

		public float MinMoveSpeed = 5f;

		public float MinRotationSpeed = 50f;

		private float moveSpeed;

		private Vector3 moveVector = Vector3.forward;

		private Vector3 rotateAngles = Vector3.zero;

		private List<ParticleSystem> trailParticlesSystems = new List<ParticleSystem>();

		public ParticleSystem[] TrailParticleSystems;

		public void Play(EngineASX engine)
		{
			trailParticlesSystems.Clear();
			this.engine = engine;
			moveSpeed = Random.Range(MinMoveSpeed, MaxMoveSpeed);
			rotateAngles = new Vector3(Random.Range(MinRotationSpeed, MaxRotationSpeed), Random.Range(MinRotationSpeed, MaxRotationSpeed), Random.Range(MinRotationSpeed, MaxRotationSpeed));
			if (Random.value > 0.5f)
			{
				rotateAngles.x *= -1f;
			}
			if (Random.value > 0.5f)
			{
				rotateAngles.y *= -1f;
			}
			if (Random.value > 0.5f)
			{
				rotateAngles.z *= -1f;
			}
			moveVector = transform.forward * moveSpeed;
			for (int i = 0; i < TrailParticleSystems.Length; i++)
			{
				ParticleSystem item = engine.PlayPooledParticleSystem(TrailParticleSystems[i].gameObject, transform.position, transform.rotation);
				trailParticlesSystems.Add(item);
			}
			expireTime = Time.time + Duration;
		}

		public void Stop()
		{
			if (engine.Pooler != null)
			{
				engine.Pooler.RecyclePoolObject(gameObject);
				for (int i = 0; i < trailParticlesSystems.Count; i++)
				{
					trailParticlesSystems[i].Stop();
					engine.Pooler.RecyclePoolObject(trailParticlesSystems[i].gameObject);
				}
			}
			trailParticlesSystems.Clear();
		}

		private void Update()
		{
			transform.Translate(moveVector * Time.deltaTime, Space.World);
			transform.Rotate(rotateAngles * Time.deltaTime);
			for (int i = 0; i < trailParticlesSystems.Count; i++)
			{
				trailParticlesSystems[i].transform.position = transform.position;
			}
			if (Time.time > expireTime)
			{
				Stop();
			}
		}
	}
}
