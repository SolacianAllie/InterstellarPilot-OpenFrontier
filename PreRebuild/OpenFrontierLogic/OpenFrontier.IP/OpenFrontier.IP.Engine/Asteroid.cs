using OpenFrontier.IP.Common;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	[RequireComponent(typeof(Unit))]
	public class Asteroid : MonoBehaviour
	{
		public int RemainingYield = -1;

		public AsteroidClass AsteroidClass;

		private Unit unit;

		public Unit Unit => unit;

		public bool RegisterDamage(float damage)
		{
			if (damage > 0f)
			{
				GameSettings gameSettings = unit.Engine.GameSettings;
				float t = Mathf.Clamp01(damage / gameSettings.MiningProbabilityDamageRef);
				float p = Mathf.Lerp(unit.Engine.EconomySettings.MiningMinPower, unit.Engine.EconomySettings.MiningMaxPower, t);
				if (Mathf.Pow(Random.value, p) < AsteroidClass.ProbabilityOfYield)
				{
					return true;
				}
			}
			return false;
		}

		public void RegisterDamageWithYield(float damage, ref Vector3 absolutePointOfImpact, Unit damageSource)
		{
			if (RegisterDamage(damage))
			{
				Cargo cargo = YieldCargo(absolutePointOfImpact - Unit.transform.position, damageSource, applyForces: true, applyEffects: true, setExpiryTime: true);
				DecreaseYieldAndDestroyIfDepleted(cargo.Quantity);
			}
		}

		public void DecreaseYieldAndDestroyIfDepleted(int quantityMined)
		{
			RemainingYield -= quantityMined;
			if (RemainingYield <= 0)
			{
				unit.Destructable.KillUnitAndChildren(null, null, DamageDirectType.Direct);
				EngineASX.Instance.DebugInfo.NumAsteroidsDepleted++;
			}
		}

		public CargoClass GetRandomCargoYieldType()
		{
			if (AsteroidClass.AsteroidYieldItems.Count > 0)
			{
				return AsteroidClass.AsteroidYieldItems.GetRandomWeighted().CargoClass;
			}
			return null;
		}

		public int GetRandomYieldQuantity()
		{
			return Mathf.CeilToInt((float)Random.Range(AsteroidClass.MinYieldQuantity, AsteroidClass.MaxYieldQuantity + 1) * unit.Engine.EconomySettings.AsteroidYieldQuantityMultiplier);
		}

		public void Init(Unit unit)
		{
			this.unit = unit;
			if (RemainingYield < 0)
			{
				RemainingYield = Random.Range(AsteroidClass.MinTotalYield, AsteroidClass.MaxTotalYield);
			}
		}

		public Cargo YieldCargo(Vector3? relativePointOfImpact, Unit damageSource, bool applyForces, bool applyEffects, bool setExpiryTime, float spawnForceMultiplier = 1f)
		{
			if (unit.Sector == null)
			{
				Debug.LogWarning("Cannot yield asteroid cargo because no sector", this);
				return null;
			}
			CargoClass randomCargoYieldType = GetRandomCargoYieldType();
			if (randomCargoYieldType != null)
			{
				Cargo nonContainerPrefab = randomCargoYieldType.NonContainerPrefab;
				if (nonContainerPrefab != null)
				{
					if (!relativePointOfImpact.HasValue)
					{
						relativePointOfImpact = ((AsteroidClass.RandomYieldPositions == null || AsteroidClass.RandomYieldPositions.Count <= 0) ? new Vector3?(Geometry.RandomXZUnitVector() * unit.UnitClass.ShieldRingRadius) : new Vector3?(transform.InverseTransformPoint(AsteroidClass.RandomYieldPositions.GetRandom().position)));
					}
					Cargo cargo = UnityObjectHelper.InstantiateAndGetComponent(nonContainerPrefab, unit.Sector.transform);
					Unit component = cargo.GetComponent<Unit>();
					component.Init(autoFindParents: false);
					cargo.CargoClass = randomCargoYieldType;
					cargo.Expires = setExpiryTime;
					cargo.SetSpawnTime();
					cargo.SetHealthBasedOnVolume();
					component.SetSector(unit.Sector, updateGameObjectParent: false);
					if (component.IsInActiveSector & applyForces)
					{
						component.transform.localPosition = transform.localPosition + relativePointOfImpact.Value;
						Rigidbody rBody = component.RBody;
						if (rBody != null)
						{
							ApplyCargoForces(relativePointOfImpact.Value, rBody, spawnForceMultiplier);
						}
					}
					else
					{
						Vector3 directionVector = Vector3.Normalize(relativePointOfImpact.Value);
						float spawnAsteroidMaxRandomAngle = EngineASX.Instance.GameSettings.SpawnAsteroidMaxRandomAngle;
						Geometry.RotateVectorRandomlyY(ref directionVector, 0f - spawnAsteroidMaxRandomAngle, spawnAsteroidMaxRandomAngle);
						component.transform.localPosition = transform.localPosition + directionVector * Random.Range(5f, 40f);
					}
					if ((component.IsInActiveSector & applyEffects) && unit.ActiveUnit != null && unit.ActiveUnit.IsInDrawRange)
					{
						CreateYieldExplosion(transform.position + relativePointOfImpact.Value);
					}
					if (damageSource != null)
					{
						component.Faction = damageSource.Faction;
					}
					cargo.Quantity = GetRandomYieldQuantity();
					return cargo;
				}
				Debug.LogWarning($"Cannot yield cargo of type: {randomCargoYieldType} because it has not set a NonContainer prefab", this);
			}
			else
			{
				Debug.LogWarning(string.Format("Cannot yield cargo as unable to find random yield type", randomCargoYieldType), this);
			}
			return null;
		}

		private void CreateYieldExplosion(Vector3 absolutePointOfImpact)
		{
			AsteroidClass.YieldExplosionClass.CreateExplosionEffect(unit.Engine, absolutePointOfImpact);
		}

		private void ApplyCargoForces(Vector3 relativePointOfImpact, Rigidbody rb, float spawnForceMultiplier = 1f)
		{
			GameSettings gameSettings = unit.Engine.GameSettings;
			float num = Random.Range(gameSettings.SpawnAsteroidMinAngularVelocity, gameSettings.SpawnAsteroidMaxAngularVelocity);
			rb.angularVelocity = Random.rotationUniform * Vector3.forward * Random.value * num;
			Vector3 directionVector = Vector3.Normalize(relativePointOfImpact);
			float spawnAsteroidMaxRandomAngle = gameSettings.SpawnAsteroidMaxRandomAngle;
			Geometry.RotateVectorRandomlyY(ref directionVector, 0f - spawnAsteroidMaxRandomAngle, spawnAsteroidMaxRandomAngle);
			float num2 = Random.Range(gameSettings.AsteroidSpawnMinForce, gameSettings.AsteroidSpawnMaxForce);
			rb.AddForce(num2 * directionVector * spawnForceMultiplier, ForceMode.Force);
		}

		public string GetFriendlyName()
		{
			return unit.ClassName + " " + unit.GetShortDesignation();
		}
	}
}
