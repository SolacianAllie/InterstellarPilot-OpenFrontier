using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenFrontier.IP.Engine.GasClouds
{
	public class ActiveGasCloud : MonoBehaviour
	{
		private ActiveUnitGasCloud activeUnitGasCloud;

		public GasCloudData GasCloudData;

		private float? destroyTime;

		private List<ParticleSystem> spaceFogParticleSystems = new List<ParticleSystem>();

		public ActiveUnitGasCloud ActiveUnitGasCloud
		{
			get
			{
				return activeUnitGasCloud;
			}
			set
			{
				activeUnitGasCloud = value;
			}
		}

		private void Awake()
		{
			spaceFogParticleSystems = GetComponentsInChildren<ParticleSystem>().ToList();
		}

		private void Update()
		{
			if (destroyTime.HasValue && Time.time > destroyTime)
			{
				Object.Destroy(gameObject);
			}
		}

		public void PhaseIn()
		{
			gameObject.SetActive(value: true);
			PlayParticleSystems(prewarm: false);
			EngineASX.Instance.EnvironmentController.SkyOverlayColor = GasCloudData.FogColor; // Open Frontier: sky matches fog color
			EngineASX.Instance.EnvironmentController.SkySecondaryExposure = GasCloudData.SkyExposure;
			EngineASX.Instance.EnvironmentController.FogDesiredStart = GasCloudData.FogStartDistance;
			EngineASX.Instance.EnvironmentController.FogDesiredEnd = GasCloudData.FogEndDistance;
			EngineASX.Instance.EnvironmentController.FogDesiredColor = GasCloudData.FogColor;
			EngineASX.Instance.EnvironmentController.FogDesiredOn = GasCloudData.FogOn;
			EngineASX.Instance.EnvironmentController.SetBlend(0f);
		}

		public void PhaseInCompletely()
		{
			gameObject.SetActive(value: true);
			PlayParticleSystems(prewarm: true);
			EngineASX.Instance.EnvironmentController.SkyOverlayColor = GasCloudData.FogColor; // Open Frontier: sky matches fog color
			EngineASX.Instance.EnvironmentController.SkySecondaryExposure = GasCloudData.SkyExposure;
			EngineASX.Instance.EnvironmentController.FogDesiredStart = GasCloudData.FogStartDistance;
			EngineASX.Instance.EnvironmentController.FogDesiredEnd = GasCloudData.FogEndDistance;
			EngineASX.Instance.EnvironmentController.FogDesiredColor = GasCloudData.FogColor;
			EngineASX.Instance.EnvironmentController.FogDesiredOn = GasCloudData.FogOn;
			EngineASX.Instance.EnvironmentController.SetBlend(1f);
		}

		public void StartPhaseOut()
		{
			StopParticleSystems();
			destroyTime = Time.time + 20f;
		}

		public void StopParticleSystems()
		{
			foreach (ParticleSystem spaceFogParticleSystem in spaceFogParticleSystems)
			{
				spaceFogParticleSystem.Stop();
			}
		}

		public void PlayParticleSystems(bool prewarm)
		{
			PositionParticleSystems(GameController.Instance.MainCamera.transform.position);
			foreach (ParticleSystem spaceFogParticleSystem in spaceFogParticleSystems)
			{
				spaceFogParticleSystem.gameObject.SetActive(value: true);
				if (prewarm)
				{
					spaceFogParticleSystem.Clear();
					spaceFogParticleSystem.Simulate(5f, withChildren: true, restart: false);
				}
				spaceFogParticleSystem.Play();
			}
		}

		internal void PositionParticleSystems(Vector3 position)
		{
			foreach (ParticleSystem spaceFogParticleSystem in spaceFogParticleSystems)
			{
				spaceFogParticleSystem.transform.position = GameController.Instance.MainCamera.transform.position;
			}
		}
	}
}
