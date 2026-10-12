using System.Collections.Generic;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.ActiveEffects;
using OpenFrontier.IP.Engine.ExplosionModifiers;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class ExplosionClass : MonoBehaviour
	{
		public Light LightPrefab;

		public GameObject AudioSourcePrefab;

		public bool DamageEnabled = true;

		private DamageType DamageType = new DamageType();

		public float ExplosiveForceMultiplier = 1f;

		public float ForceFalloutPower = 1f;

		public float MaxDamage = 100f;

		public float MaxDistance = 100f;

		public GameObject[] ParticlePrefabs;

		public ShieldDamageType ShieldDamageType = ShieldDamageType.Spread;

		public List<ActiveEffectBase> ActiveEffects;

		public List<ExplosionModifier> ExplosionModifiers;

		public void CreateExplosionEffect(EngineASX engine, Vector3 position, Transform parent = null)
		{
			float num = Vector3.Distance(GameController.Instance.MainCamera.transform.position, position);
			if (AudioSourcePrefab != null)
			{
				AudioSource component = AudioSourcePrefab.GetComponent<AudioSource>();
				if (num < component.maxDistance * 2f)
				{
					engine.PlayPooledAudioSource(AudioSourcePrefab, position);
				}
			}
			if (GameController.Instance.LightingFXEnabled && LightPrefab != null && num < LightPrefab.range * GameController.Instance.GameSettings.PerformanceSettings.LightingFxMaxCameraDistanceMultiplier)
			{
				Object.Instantiate(LightPrefab, position, Quaternion.identity, parent);
			}
			if (ParticlePrefabs == null)
			{
				return;
			}
			for (int i = 0; i < ParticlePrefabs.Length; i++)
			{
				ParticleSystem particleSystem = engine.PlayPooledParticleSystem(ParticlePrefabs[i], position, Quaternion.identity);
				if (parent != null)
				{
					particleSystem.transform.SetParent(parent, worldPositionStays: true);
				}
			}
		}

		public void ApplyToAllSectorUnits(EngineASX engine, Sector sourceSector, Vector3 worldPosition, Faction sourceFaction, Unit sourceUnit, Unit exclusionUnit, Unit targettedUnit)
		{
			if (!DamageEnabled && ActiveEffects.Count <= 0)
			{
				return;
			}
			DamageType.ShieldDamageType = ShieldDamageType;
			DamageType.MiningDamage = 0f;
			int num = 0;
			if (DamageEnabled)
			{
				num = LayerMaskUtility.CombineMasks(num, GameController.Instance.DamageableMask);
			}
			if (ActiveEffects != null)
			{
				foreach (ActiveEffectBase activeEffect in ActiveEffects)
				{
					LayerMask? targetMask = activeEffect.TargetMask;
					if (targetMask.HasValue)
					{
						num = LayerMaskUtility.CombineMasks(num, targetMask.Value);
					}
				}
			}
			float maxDistance = MaxDistance;
			if (ExplosionModifiers.Count > 0)
			{
				foreach (ExplosionModifier explosionModifier in ExplosionModifiers)
				{
					maxDistance = explosionModifier.GetMaxDistance(sourceSector, sourceSector.ToLocalPosition(worldPosition), maxDistance);
				}
			}
			int num2 = Physics.OverlapSphereNonAlloc(worldPosition, maxDistance, EngineASX.ColliderCache, num, QueryTriggerInteraction.Collide);
			for (int i = 0; i < num2; i++)
			{
				Unit component = EngineASX.ColliderCache[i].GetComponent<Unit>();
				if (component != exclusionUnit && component.IsValidAndNotDestroyed && !component.IsDocked && component.Sector == sourceSector)
				{
					applyToUnit(engine, worldPosition, sourceFaction, sourceUnit, component, targettedUnit);
				}
			}
		}

		public void ApplyToUnit(EngineASX engine, Sector sourceScene, Vector3 position, Faction sourceFaction, Unit sourceUnit, Unit targetUnit, Unit targettedUnit)
		{
			if (DamageEnabled)
			{
				DamageType.ShieldDamageType = ShieldDamageType;
				DamageType.MiningDamage = 0f;
				applyToUnit(engine, position, sourceFaction, sourceUnit, targetUnit, targettedUnit);
			}
			if (sourceScene == engine.ActiveSector)
			{
				CreateExplosionEffect(engine, position);
			}
		}

		private void applyToUnit(EngineASX engine, Vector3 explosionPosition, Faction sourceFaction, Unit sourceUnit, Unit unitToApplyTo, Unit targettedUnit)
		{
			if (!(unitToApplyTo != null))
			{
				return;
			}
			if (DamageEnabled)
			{
				ApplyConventionalDamage(engine, explosionPosition, sourceFaction, sourceUnit, unitToApplyTo, targettedUnit);
			}
			if (!(unitToApplyTo != null))
			{
				return;
			}
			foreach (ActiveEffectBase activeEffect in ActiveEffects)
			{
				activeEffect.ApplyToUnit(unitToApplyTo, sourceFaction);
			}
		}

		private void ApplyConventionalDamage(EngineASX engine, Vector3 explosionPosition, Faction sourceFaction, Unit sourceUnit, Unit unitToDamage, Unit targettedUnit)
		{
			float num = Vector3.Distance(explosionPosition, unitToDamage.transform.position) - unitToDamage.UnitClass.ShieldRingRadius;
			Collider damageCollider = unitToDamage.DamageCollider;
			if (damageCollider != null && damageCollider.Raycast(new Ray(explosionPosition, Vector3.Normalize(unitToDamage.transform.position - explosionPosition)), out var hitInfo, MaxDistance))
			{
				explosionPosition = hitInfo.point;
				num = hitInfo.distance;
			}
			if (!(num < MaxDistance))
			{
				return;
			}
			float num2 = Mathf.Pow(1f - num / MaxDistance, EngineASX.Instance.GameSettings.ExplosionDamageFalloutPower) * MaxDamage;
			if (unitToDamage.ActiveUnit != null && unitToDamage.ActiveUnit.LastDistanceFromCamera < 800f)
			{
				Rigidbody rBody = unitToDamage.RBody;
				if (rBody != null)
				{
					float num3 = Mathf.Pow(1f - num / MaxDistance, ForceFalloutPower) * (num2 * engine.GameSettings.ExplosionDamageToForceRatio * ExplosiveForceMultiplier);
					rBody.AddForce(Vector3.Normalize(unitToDamage.transform.position - explosionPosition) * num3, ForceMode.Impulse);
				}
			}
			DamageType.Damage = num2;
			DamageType.ComponentDamageMultiplier = GameController.Instance.GameSettings.GameplaySettings.ExplosionComponentDamageMultiplier;
			if (GameController.Instance.GameSettings.DebugSettings.DamageEnabled && unitToDamage.Destructable != null)
			{
				DamageDirectType damageDirectType = ((!(unitToDamage == targettedUnit)) ? DamageDirectType.Indirect : DamageDirectType.Direct);
				unitToDamage.Destructable.ApplyDamage(explosionPosition, sourceFaction, sourceUnit, damageDirectType, DamageType);
			}
		}
	}
}
