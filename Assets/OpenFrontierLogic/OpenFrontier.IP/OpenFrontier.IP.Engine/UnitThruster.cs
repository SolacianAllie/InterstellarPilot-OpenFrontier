using System.Collections.Generic;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using UnityEngine.Rendering;

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

		// Engine flare config, serialized per UnitThruster doc in the APK
		// (ripped values: ships carry per-ship min/draw; multiplier is 2 and
		// the occlusion reference 25 everywhere). YAML field order follows the
		// rip stub's declaration order.
		public float EngineFlareMinIntensity;

		public float EngineFlareIntensityMultiplier;

		public float EngineFlareDrawDistance;

		// Widens the deactivation range to 1/0.9x the nominal draw distance:
		// a flare that has switched on must not strobe when the camera rides
		// the boundary. Declared between the serialized fields to match the
		// rip stub's layout.
		private const float EngineFlareHysteresisFraction = 0.9f;

		public float EngineFlareOcclusionReferenceDistance;

		// Rebuilt from the recovered stub surface (UnitThruster.cs): the lost
		// week drove one LensFlareComponentSRP per thruster. The component
		// lives on the "Flare" child GameObject authored per ship prefab; the
		// leaked week's gating logic ran from this class.
		private LensFlareComponentSRP engineFlare;

		private float engineFlarePeakIntensity;

		private float engineFlareBaseOcclusionRadius;

		private bool isFlareActive;

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
			// The authored flare docs ship m_Enabled: 1; hold every flare dark
			// from the first frame - the Update loop below switches it on only
			// once the engine is actually burning. Thruster-class prefabs carry
			// no flare child, so a null here is the normal state for those.
			engineFlare = GetComponentInChildren<LensFlareComponentSRP>(true);
			if (engineFlare != null)
			{
				engineFlarePeakIntensity = engineFlare.intensity;
				engineFlareBaseOcclusionRadius = engineFlare.occlusionRadius;
				engineFlare.enabled = false;
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
				if (unitThrusterParticleSystem == null
					|| unitThrusterParticleSystem.ParticleSystemPrefab == null)
				{
					// Skip rather than throw.
					//
					// Every entry in this list is expected to name a particle
					// prefab, and one that does not is a content mistake. The old
					// behaviour dereferenced it unconditionally, which meant a
					// single bad entry took out every particle system after it in
					// the list - not just its own. That is how the engine smoke
					// went missing: a heat value had been appended as a list entry
					// with no prefab, the loop threw on it, and the smoke puff
					// that came after it in the same class was never instantiated.
					// Five thruster classes were quietly rendering only their
					// exhaust flame.
					//
					// One bad entry costing one effect is a bug. One bad entry
					// costing every effect after it is a trap.
					continue;
				}
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
			UpdateEngineFlare();
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

		// Flare gating, rebuilt from the rip stub's member list. Called every
		// frame even when the particles are dormant, so a flare that was on
		// gets switched off the moment the thruster exits its draw range or
		// the engine stops burning.
		private void UpdateEngineFlare()
		{
			if (engineFlare == null)
			{
				return;
			}
			SetEngineFlareOcclusionRadius(GetCameraDistance());
			// The flare rides the engine-power tie-in below, which needs the
			// sim unit. GetShouldEffectsBeDrawn() already rules unit-less
			// active-units out of drawing every other effect - honour the
			// same contract here instead of throwing through it per frame
			// (menu-after-quit ships spawn with Unit unset for a while).
			UnitComponentHolder components = activeUnit != null && activeUnit.Unit != null ? activeUnit.Unit.Components : null;
			bool enginesInUse = components != null && components.EngineComponent != null && components.EngineComponent.EngineThrottle > 0f;
			SetEngineFlareActive(isActive && enginesInUse && InFlareDrawRange());
			if (isFlareActive)
			{
				SetEngineFlareIntensity();
			}
		}

		private void SetEngineFlareOcclusionRadius(float distance)
		{
			if (EngineFlareOcclusionReferenceDistance <= 0f)
			{
				return;
			}
			engineFlare.occlusionRadius = Mathf.Max(engineFlareBaseOcclusionRadius, engineFlareBaseOcclusionRadius * distance / EngineFlareOcclusionReferenceDistance);
		}

		private void SetEngineFlareIntensity()
		{
			UnitComponentHolder components = activeUnit.Unit.Components;
			if (components.EngineComponent == null)
			{
				return;
			}
			float engineThrottle = components.EngineComponent.EngineThrottle;
			engineFlare.intensity = engineFlarePeakIntensity * (EngineFlareMinIntensity + EngineFlareIntensityMultiplier * engineThrottle);
		}

		private void SetEngineFlareActive(bool active)
		{
			if (isFlareActive == active)
			{
				return;
			}
			isFlareActive = active;
			engineFlare.enabled = active;
		}

		// Hysteresis across the draw distance: activation at the nominal range,
		// deactivation only beyond 1/0.9x of it.
		private bool InFlareDrawRange()
		{
			float num = GetCameraDistance();
			return isFlareActive ? num <= EngineFlareDrawDistance / EngineFlareHysteresisFraction : num < EngineFlareDrawDistance;
		}

		private float GetCameraDistance()
		{
			Camera mainCamera = GameController.Instance.MainCamera;
			if (mainCamera == null)
			{
				return float.MaxValue;
			}
			return Vector3.Distance(mainCamera.transform.position, transform.position);
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
