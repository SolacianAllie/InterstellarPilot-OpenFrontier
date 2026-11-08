using UnityEngine;

namespace OpenFrontier.IP.Engine.FX
{
	public class AutoRepositionParticles : MonoBehaviour
	{
		public ParticleSystem ParticleSystem;

		private bool hasInit;

		public void Init()
		{
			if (!hasInit && EngineASX.Instance != null)
			{
				hasInit = true;
				EngineASX.Instance.CameraMoved += engine_CameraMoved;
			}
		}

		private void Start()
		{
			Init();
		}

		private void Update()
		{
			if (EngineASX.LoadedAndReady)
			{
				transform.position = GameController.Instance.MainCamera.transform.position;
			}
		}

		private void OnDestroy()
		{
			if (EngineASX.Instance != null)
			{
				EngineASX.Instance.CameraMoved -= engine_CameraMoved;
			}
		}

		private void engine_CameraMoved(EngineASX sender)
		{
			if (ParticleSystem != null)
			{
				OnCameraMoved();
			}
		}

		public void OnCameraMoved()
		{
			transform.position = GameController.Instance.MainCamera.transform.position;
			ParticleSystem.Clear();
			ParticleSystem.Simulate(5f, withChildren: true, restart: false);
			ParticleSystem.Play();
		}
	}
}
