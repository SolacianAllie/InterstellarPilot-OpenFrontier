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

		/// <summary>
		/// Distance past which a unit counts as out of range, and the nearer
		/// bound the hysteresis band closes at.
		/// </summary>
		/// <remarks>
		/// THIS IS NOT THE MODEL CULL. Read ActiveUnit.SetDrawObjectsActive
		/// before touching either value.
		///
		/// IsInDrawRange, which these set, gates per-frame work that is
		/// genuinely proximity-dependent:
		///
		///   ActiveUnitThrusterAudio - volume is computed from engine THROTTLE,
		///     not from distance, so a unit held permanently in range would be
		///     audible from across the sector and would keep its AudioSource
		///     mixing. Every thruster in the sector, not just the visible ones.
		///   Asteroid.CreateYieldExplosion - gated on IsInDrawRange, so a
		///     permanent true spawns a particle explosion for every asteroid
		///     destroyed in the sector, visible or not.
		///
		/// So the band stays real, and the per-prefab overrides (300 for
		/// debris and cargo, 500-1500 for ships and stations) stay as authored
		/// - they are doing useful work here.
		/// </remarks>
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
