using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Scenarios.RandomEvents
{
	public class NewDerilectShip : RandomUniverseEvent
	{
		private class NewDerilectShipItem : IWeighted
		{
			public UnitClass UnitClass { get; set; }

			public float Weight { get; set; }
		}

		public float MinRandomCargoPercentage;

		public float MaxRandomCargoPercentage = 0.5f;

		public float NewShipSizePower = 2f;

		public bool SpawnInActiveScene;

		public float MinShieldHealth;

		public float MaxShieldHealth = 0.25f;

		public float MaxShipHealth = 0.3f;

		public float MinShipHealth = 0.1f;

		public float MinComponentHealth = 0.05f;

		public float MaxComponentHealth = 0.4f;

		public List<UnitClass> PossibleUnitClasses = new List<UnitClass>();

		public string MessageText = "Derelict ship";

		public string SubjectText = "An unmanned ship has been sighted";

		private List<NewDerilectShipItem> weightedClasses = new List<NewDerilectShipItem>();

		private float maxShipMoneyValue;

		private List<NewDerilectShipItem> GetWeightedClasses()
		{
			List<NewDerilectShipItem> list = new List<NewDerilectShipItem>();
			maxShipMoneyValue = PossibleUnitClasses.Max((UnitClass e) => e.SaleCost);
			foreach (UnitClass possibleUnitClass in PossibleUnitClasses)
			{
				NewDerilectShipItem item = new NewDerilectShipItem
				{
					UnitClass = possibleUnitClass,
					Weight = GetUnitClassWeight(possibleUnitClass)
				};
				list.Add(item);
			}
			return list;
		}

		private float GetUnitClassWeight(UnitClass unitClass)
		{
			float num = maxShipMoneyValue * 1.5f / 1000f;
			float num2 = (float)unitClass.SaleCost / 1000f;
			return Mathf.Pow(1f - num2 / num, NewShipSizePower);
		}

		public override void Generate(bool silent = false)
		{
			base.Generate();
			if (weightedClasses.Count == 0 && PossibleUnitClasses.Count > 0)
			{
				weightedClasses = GetWeightedClasses();
			}
			NewDerilectShipItem randomWeighted = weightedClasses.GetRandomWeighted();
			if (randomWeighted == null)
			{
				return;
			}
			UnitClass unitClass = randomWeighted.UnitClass;
			Sector random = World.Engine.Sectors.Where((Sector e) => SpawnInActiveScene || !e.IsActive).GetRandom();
			if (!(random != null))
			{
				return;
			}
			Vector3 randomSafeDeploymentSectorPosition = random.GetRandomSafeDeploymentSectorPosition(1.2f, 1.5f, 20f, GameController.Instance.NonOVerlappingUnitsMask);
			Unit unit = CreateAndSetupUnit(unitClass, random, randomSafeDeploymentSectorPosition);
			RandomizeComponentHealth(unit);
			RandomizeShieldHealth(unit);
			if (!silent)
			{
				GenerateMessage(World, unit);
				if (EngineASX.Instance.LocalFaction != null)
				{
					EngineASX.Instance.LocalFaction.Intel.DiscoverSector(random);
				}
			}
		}

		private void RandomizeShieldHealth(Unit unit)
		{
			ShieldComponent shieldComponent = unit.Components.ShieldComponent;
			if (shieldComponent != null)
			{
				for (int i = 0; i < 6; i++)
				{
					shieldComponent.SetNormalizedShieldPoints(i, Random.Range(MinShieldHealth, MaxShieldHealth));
				}
			}
		}

		private Unit CreateAndSetupUnit(UnitClass unitClass, Sector sector, Vector3 sectorPosition)
		{
			ModdedUnitSeederSettings moddedUnitSettings = GameController.Instance.GameSettings.ModdedUnitSettings;
			Unit unit = WorldHelper.SpawnUnitAndInstallComponents(unitClass.UnitPrefab, sector, addCargoLoadout: false);
			bool flag = ModdedUnitSeeder.ShouldModUnitRandomly(unit, moddedUnitSettings);
			unit.transform.localPosition = sectorPosition;
			unit.Destructable.HealthNormalized = Mathf.Lerp(MinShipHealth, MaxShipHealth, Random.value);
			unit.transform.SetRandomLocalYRotation();
			if (flag)
			{
				ModdedUnitSeeder.ModUnit(unit, GameController.Instance.GameSettings.ModdedUnitSettings);
			}
			float maxEquipmentUsage = Random.Range(MinRandomCargoPercentage, MaxRandomCargoPercentage);
			ModdedUnitSeeder.AddUnitEquipment(unit, moddedUnitSettings, maxEquipmentUsage);
			return unit;
		}

		private void GenerateMessage(WorldBase world, Unit unit)
		{
			GamePlayer localPlayer = world.Engine.LocalPlayer;
			if (localPlayer != null)
			{
				PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage();
				playerActiveMessage.MessageText = MessageText;
				string subjectText = $"{SubjectText} in the {unit.Sector.Name} sector";
				playerActiveMessage.SubjectText = subjectText;
				playerActiveMessage.FromText = "# Unknown #";
				playerActiveMessage.SetSubjectUnitAndPosition(unit);
				localPlayer.AddMessage(playerActiveMessage);
			}
		}

		private void RandomizeComponentHealth(Unit unit)
		{
			if (!(unit.Components != null))
			{
				return;
			}
			foreach (ComponentBase unitComponent in unit.Components.UnitComponents)
			{
				float healthNormalized = Random.Range(MinComponentHealth, MaxComponentHealth);
				unitComponent.HealthNormalized = healthNormalized;
			}
		}
	}
}
