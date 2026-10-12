using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class CustomParticles : MonoBehaviour
	{
		protected EngineASX engine;

		public ParticleSystem ParticleSystem;

		public void Awake()
		{
			if (ParticleSystem != null)
			{
				ParticleSystem.gameObject.SetActive(value: false);
			}
		}

		public void Start()
		{
			engine = EngineASX.Instance;
			if (engine != null)
			{
				engine.CameraMoved += engine_CameraMoved;
			}
			start();
		}

		private void Update()
		{
			if (EngineASX.LoadedAndReady)
			{
				ParticleSystem.gameObject.SetActive(value: true);
				update();
			}
		}

		protected virtual void update()
		{
		}

		protected virtual void start()
		{
		}

		private void OnDestroy()
		{
			if (engine != null)
			{
				engine.CameraMoved -= engine_CameraMoved;
			}
		}

		private void engine_CameraMoved(EngineASX sender)
		{
			transform.position = GameController.Instance.MainCamera.transform.position;
			ParticleSystem.Clear();
			ParticleSystem.Simulate(5f, withChildren: true, restart: false);
			ParticleSystem.Play();
		}
	}
}
