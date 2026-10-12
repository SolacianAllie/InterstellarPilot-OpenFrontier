using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class ActiveUnitClass : MonoBehaviour
	{
		public float ThrusterDrawDistanceMultiplier = 1f;

		public float DamageParticlesDrawDistanceMultiplier = 1f;

		public float AmbientSoundPlayFar = 200f;

		public float AmbientSoundPlayNear = 150f;

		public List<AudioSource> AmbientSounds = new List<AudioSource>();

		public List<ParticleSystem> DamageFlameParticlePrefabs = new List<ParticleSystem>();

		public float DamageParticlesDrawFar = 350f;

		public float DamageParticlesDrawNear = 300f;

		public List<ParticleSystem> DamageSmokeParticlePrefabs = new List<ParticleSystem>();

		public GameObject DestructionPrefab;

		public float DestructionAnimMaxPlayDistance = 8000f;

		public List<AudioSource> DockedAmbientSounds = new List<AudioSource>();

		public float DrawDistFar = 1000f;

		public float DrawDistNear = 800f;

		public AudioSource HullHitAudioSource;

		public float MaxCameraDistance = 20f;

		public float MaxCameraHeight = 20f;

		public float MinCameraDistance = 20f;

		public float MinCameraHeight = 20f;

		public float RigidBodyAngularDrag;

		public bool RigidBodyApplyConstraints = true;

		public bool RigidBodyCreate = true;

		public bool RigidBodyFreezeY = true;

		public AudioSource ShieldDownAudioSource;

		public AudioSource ShieldHitAudioSource;

		public GameObject ShieldHitPrefab;

		public AudioSource ShieldUpAudioSource;

		public float ThrusterDrawFar => EngineASX.Instance.PerformanceSettings.ThrusterDrawDistanceSettings.QualityLevelSettings[GameController.Instance.CurrentQualityLevel].FarDrawDistance * ThrusterDrawDistanceMultiplier;

		public float ThrusterDrawNear => EngineASX.Instance.PerformanceSettings.ThrusterDrawDistanceSettings.QualityLevelSettings[GameController.Instance.CurrentQualityLevel].NearDrawDistance * ThrusterDrawDistanceMultiplier;
	}
}
