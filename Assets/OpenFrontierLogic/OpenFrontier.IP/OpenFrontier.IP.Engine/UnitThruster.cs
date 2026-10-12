using System.Collections.Generic;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class UnitThruster : MonoBehaviour
	{
		private struct ActiveUnitThruster
		{
			public ParticleSystem ParticleSystem;

			public UnitThrusterParticleSystem ParticleSystemClass;

			public float InitialEmissionRate;

			public Color InitialStartColor;

			public ActiveUnitThruster(ParticleSystem particleSystem, UnitThrusterParticleSystem particleSystemClass, float initialEmissionRate, Color initialStartColor)
			{
				this = default;
				ParticleSystem = particleSystem;
				ParticleSystemClass = particleSystemClass;
				InitialEmissionRate = initialEmissionRate;
				InitialStartColor = initialStartColor;
			}
		}

		public UnitThrusterClass UnitThrusterClass;

		private ActiveUnit activeUnit;

		private bool isActive;

		private List<ActiveUnitThruster> activeParticleSystems = new List<ActiveUnitThruster>();

		public bool IsActive
		{
			get
			{
				return isActive;
			}
			set
			{
				if (isActive != value)
				{
					isActive = value;
					RecycleParticles();
					if (isActive)
					{
						CreateParticles();
					}
				}
			}
		}

		private void Awake()
		{
			activeUnit = UnityObjectHelper.FindInParentsOrSelf<ActiveUnit>(gameObject);
			if (activeUnit == null)
			{
				Debug.LogError("Missing ActiveUnit", this);
			}
		}

		private void CreateParticles()
		{
			EngineASX instance = EngineASX.Instance;
			if (!(instance != null))
			{
				return;
			}
			foreach (UnitThrusterParticleSystem unitThrusterParticleSystem in UnitThrusterClass.UnitThrusterParticleSystems)
			{
				ParticleSystem particleSystem = instance.PlayPooledParticleSystem(unitThrusterParticleSystem.ParticleSystemPrefab.gameObject, transform.position, transform.rotation);
				particleSystem.transform.SetParent(transform, worldPositionStays: true);
				activeParticleSystems.Add(new ActiveUnitThruster(particleSystem, unitThrusterParticleSystem, particleSystem.emission.rateOverTime.constant, particleSystem.main.startColor.color));
			}
		}

		private void Update()
		{
			if (!EngineASX.LoadedAndReady)
			{
				return;
			}
			IsActive = activeUnit.ShouldEffectsBeDrawn && InDrawRange();
			if (!isActive)
			{
				return;
			}
			UnitEngineComponent engineComponent = activeUnit.Unit.Components.EngineComponent;
			bool flag = engineComponent != null && engineComponent.UserPowered;
			foreach (ActiveUnitThruster activeParticleSystem in activeParticleSystems)
			{
				ParticleSystem.EmissionModule emission = activeParticleSystem.ParticleSystem.emission;
				emission.enabled = flag;
			}
			SetParticleEmissionRates();
		}

		private void SetParticleEmissionRates()
		{
			UnitComponentHolder components = activeUnit.Unit.Components;
			if (!(components.EngineComponent != null))
			{
				return;
			}
			foreach (ActiveUnitThruster activeParticleSystem in activeParticleSystems)
			{
				ParticleSystem particleSystem = activeParticleSystem.ParticleSystem;
				float engineThrottle = components.EngineComponent.EngineThrottle;
				ParticleSystem.MainModule main = particleSystem.main;
				ParticleSystem.EmissionModule emission = particleSystem.emission;
				if (engineThrottle > 0f)
				{
					emission.rateOverTime = (activeParticleSystem.ParticleSystemClass.MinEmissionRate + components.EngineComponent.EngineThrottle * (1f - activeParticleSystem.ParticleSystemClass.MinEmissionRate)) * activeParticleSystem.InitialEmissionRate;
					Color initialStartColor = activeParticleSystem.InitialStartColor;
					initialStartColor.a *= engineThrottle;
					main.startColor = initialStartColor;
				}
				else
				{
					emission.rateOverTime = 0f;
				}
			}
		}

		private bool InDrawRange()
		{
			if (activeUnit.LastDistanceFromCamera > 0f)
			{
				if (isActive)
				{
					if (activeUnit.LastDistanceFromCamera > activeUnit.ActiveUnitClass.ThrusterDrawFar)
					{
						return false;
					}
				}
				else if (activeUnit.LastDistanceFromCamera < activeUnit.ActiveUnitClass.ThrusterDrawNear)
				{
					return true;
				}
			}
			return isActive;
		}

		private void OnDestroy()
		{
			IsActive = false;
		}

		private void RecycleParticles()
		{
			EngineASX instance = EngineASX.Instance;
			if (!(instance != null) || !(instance.Pooler != null))
			{
				return;
			}
			foreach (ActiveUnitThruster activeParticleSystem in activeParticleSystems)
			{
				ParticleSystem particleSystem = activeParticleSystem.ParticleSystem;
				ParticleSystem.MainModule main = particleSystem.main;
				ParticleSystem.EmissionModule emission = particleSystem.emission;
				emission.rateOverTime = activeParticleSystem.InitialEmissionRate;
				main.startColor = activeParticleSystem.InitialStartColor;
				instance.Pooler.RecyclePoolObject(particleSystem.gameObject);
				particleSystem.Clear();
				particleSystem.Stop();
			}
			activeParticleSystems.Clear();
		}
	}
}
