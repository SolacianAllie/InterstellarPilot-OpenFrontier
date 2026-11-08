using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class AbandonedShipsSeeder : MonoBehaviour
	{
		private struct NewDerilectShipItem : IWeighted
		{
			public UnitClass UnitClass { get; set; }

			public float Weight { get; set; }
		}

		public AbandonedShipsSeederSettings AbandonedShipsSeederSettings;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding abandoned ships...", this, 1);
			}
			AbandonedShipsSeederSettings = world.Seeder.Settings.AbandonedShipsSeederSettings;
			SeedShips(AbandonedShipsSeederSettings);
		}

		private void SeedShips(AbandonedShipsSeederSettings settings)
		{
			List<UnitClass> unitClasses = GetUnitClasses();
			List<NewDerilectShipItem> list = new List<NewDerilectShipItem>(40);
			float minShipMoneyValue = unitClasses.Select((UnitClass e) => e.SaleCost).Min();
			float maxShipMoneyValue = unitClasses.Select((UnitClass e) => e.SaleCost).Max();
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				if (!(sector.SecurityLevel < settings.MaxSectorSecurity))
				{
					continue;
				}
				float num = settings.BaseProbability + (1f - sector.SecurityLevel) * settings.LowSecurityProbability;
				num += sector.FringeSectorRating * settings.FringeSectorProbability;
				if (!(num > 0f))
				{
					continue;
				}
				for (int num2 = 0; num2 < settings.MaxIterationsPerSector; num2++)
				{
					if (!(Random.value < num))
					{
						continue;
					}
					list.Clear();
					GetWeightedClassesNonAlloc(unitClasses, sector, settings, list, minShipMoneyValue, maxShipMoneyValue);
					if (list.Count <= 0)
					{
						continue;
					}
					NewDerilectShipItem randomWeighted = list.GetRandomWeighted();
					Vector3? sectorPosition = GetSectorPosition(sector, settings, randomWeighted.UnitClass);
					if (sectorPosition.HasValue && !AnyShipsOrStationsNearPosition(sector, sectorPosition.Value, settings.MinDistanceFromOtherShipsAndStations))
					{
						Vector3? vector = sectorPosition;
						if (vector.HasValue)
						{
							Unit unit = CreateAndSetupUnit(randomWeighted.UnitClass, sector, vector.Value, settings);
							float power = Mathf.Lerp(settings.MaxShipHealthPower, settings.MinShipHealthPower, sector.FringeSectorRating);
							unit.Destructable.HealthNormalized = Maths.RandomFloatWithPower(settings.MinShipHealth, settings.MaxShipHealth, power);
							RandomizeComponentHealth(unit, settings);
							RandomizeShieldHealth(unit, settings);
						}
					}
				}
			}
		}

		private Vector3? GetSectorPosition(Sector sector, AbandonedShipsSeederSettings settings, UnitClass unitClass)
		{
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.GasCloud);
			if (unitsByType != null)
			{
				IEnumerable<Unit> enumerable = unitsByType.Where((Unit e) => settings.SpawnInGasCloudClasses.Contains(e.GetComponent<UnitGasCloud>().GasCloudClass));
				if (enumerable.Any() && Random.value < settings.ProbabilityOfSpawnInGasCloud)
				{
					return enumerable.GetRandom().GetRandomSectorPositionWithinRadius();
				}
			}
			return sector.GetRandomSectorPositionAtBoundary();
		}

		private bool AnyShipsOrStationsNearPosition(Sector sector, Vector3 sectorPosition, float checkRadius)
		{
			return Physics.CheckSphere(sector.ToWorldPosition(sectorPosition), checkRadius, GameController.Instance.ShipsAndStationsMask, QueryTriggerInteraction.Collide);
		}

		private void GetWeightedClassesNonAlloc(IEnumerable<UnitClass> possibleUnitClasses, Sector sector, AbandonedShipsSeederSettings abandonedShipsSeederSettings, List<NewDerilectShipItem> outputList, float minShipMoneyValue, float maxShipMoneyValue)
		{
			float a = Mathf.Lerp(maxShipMoneyValue / 2f, maxShipMoneyValue, sector.FringeSectorRating);
			a = Mathf.Lerp(a, 70000f, Mathf.Pow(sector.SecurityLevel, 0.5f));
			float num = 0f;
			foreach (UnitClass possibleUnitClass in possibleUnitClasses)
			{
				if ((float)possibleUnitClass.SaleCost < a && (float)possibleUnitClass.SaleCost > num)
				{
					NewDerilectShipItem item = new NewDerilectShipItem
					{
						UnitClass = possibleUnitClass,
						Weight = GetUnitClassWeight(possibleUnitClass, abandonedShipsSeederSettings, sector)
					};
					outputList.Add(item);
				}
			}
		}

		private float GetUnitClassWeight(UnitClass unitClass, AbandonedShipsSeederSettings abandonedShipsSeederSettings, Sector sector)
		{
			float relativeShipSaleCost = unitClass.RelativeShipSaleCost;
			float p = abandonedShipsSeederSettings.SmallShipFactor * sector.AdjustedSecurityLevel01;
			return Mathf.Pow(2f - relativeShipSaleCost, p);
		}

		private Unit CreateAndSetupUnit(UnitClass unitClass, Sector sector, Vector3 sectorPosition, AbandonedShipsSeederSettings abandonedShipsSeederSettings)
		{
			ModdedUnitSeederSettings moddedUnitSettings = GameController.Instance.GameSettings.ModdedUnitSettings;
			Unit unit = WorldHelper.SpawnUnitAndInstallComponents(unitClass.UnitPrefab, sector, addCargoLoadout: false);
			bool flag = ModdedUnitSeeder.ShouldModUnitRandomly(unit, moddedUnitSettings);
			unit.transform.localPosition = sectorPosition;
			unit.transform.SetRandomLocalYRotation();
			if (flag)
			{
				ModdedUnitSeeder.ModUnit(unit, GameController.Instance.GameSettings.ModdedUnitSettings);
			}
			if (Random.value < abandonedShipsSeederSettings.ProbabilityOfAllComponentsOff)
			{
				foreach (ComponentBase unitComponent in unit.Components.UnitComponents)
				{
					if (unitComponent.CanChangeUserPowered)
					{
						unitComponent.UserPowered = false;
					}
				}
			}
			else
			{
				foreach (ComponentBase unitComponent2 in unit.Components.UnitComponents)
				{
					if (Random.value < abandonedShipsSeederSettings.ProbabilityOfIndividualComponentsOff && unitComponent2.CanChangeUserPowered)
					{
						unitComponent2.UserPowered = false;
					}
				}
			}
			return unit;
		}

		public List<UnitClass> GetUnitClasses()
		{
			return GameController.Instance.LoadedUnitClasses.Where((UnitClass e) => e.UnitType == UnitType.Ship && e.ShipType == ShipType.Normal && e.IsUsable && e.Purposes != UnitPurpose.None).ToList();
		}

		private void RandomizeComponentHealth(Unit unit, AbandonedShipsSeederSettings abandonedShipsSeederSettings)
		{
			if (!(unit.Components != null))
			{
				return;
			}
			foreach (ComponentBase unitComponent in unit.Components.UnitComponents)
			{
				float healthNormalized = Random.Range(abandonedShipsSeederSettings.MinComponentHealth, abandonedShipsSeederSettings.MaxComponentHealth);
				unitComponent.HealthNormalized = healthNormalized;
			}
		}

		private void RandomizeShieldHealth(Unit unit, AbandonedShipsSeederSettings abandonedShipsSeederSettings)
		{
			ShieldComponent shieldComponent = unit.Components.ShieldComponent;
			if (!(shieldComponent != null))
			{
				return;
			}
			for (int i = 0; i < 6; i++)
			{
				if (Random.value < abandonedShipsSeederSettings.ProbabilityOfShieldBeingUp)
				{
					shieldComponent.SetNormalizedShieldPoints(i, Random.Range(abandonedShipsSeederSettings.MinShieldHealth, abandonedShipsSeederSettings.MaxShieldHealth));
				}
				else
				{
					shieldComponent.DepleteShield(i);
				}
			}
		}
	}
}
