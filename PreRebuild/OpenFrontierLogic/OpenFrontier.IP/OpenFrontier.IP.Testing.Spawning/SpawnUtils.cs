using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Scenarios;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OpenFrontier.IP.Testing.Spawning
{
	public static class SpawnUtils
	{
		public static int SpawnEveryShip()
		{
			if (EngineASX.LoadedAndReady)
			{
				int num = 0;
				Vector3 zero = Vector3.zero;
				Vector3 spawnSectorPositionFromLocalUnit = GetSpawnSectorPositionFromLocalUnit(EngineASX.Instance.LocalUnit);
				{
					foreach (UnitClass item in from e in GameController.Instance.LoadedUnitClasses
						where e.IsUsable && e.UnitType == UnitType.Ship
						orderby e.SaleCost
						select e)
					{
						TrySpawnUnit(item.UnitPrefab, EngineASX.Instance.LocalPlayerSector, spawnSectorPositionFromLocalUnit + zero, EngineASX.Instance.LocalFaction);
						zero += Vector3.right * (item.UnitPrefab.Radius + 3f);
						num++;
					}
					return num;
				}
			}
			return 0;
		}

		public static Unit SpawnUnitNearLocalUnit(Unit unitPrefab, Faction owner, float distance = 300f)
		{
			EngineASX instance = EngineASX.Instance;
			if (instance != null)
			{
				Unit localUnit = instance.LocalUnit;
				if (localUnit != null)
				{
					Vector3 spawnSectorPositionFromLocalUnit = GetSpawnSectorPositionFromLocalUnit(localUnit, distance);
					return SpawnUnit(unitPrefab, instance.ActiveSector, spawnSectorPositionFromLocalUnit, owner);
				}
			}
			return null;
		}

		public static int SpawnEveryStation()
		{
			if (EngineASX.LoadedAndReady)
			{
				int num = 0;
				{
					foreach (UnitClass item in from e in GameController.Instance.LoadedUnitClasses
						where e.IsUsable && e.UnitType == UnitType.Station && e.SeedInSandbox
						orderby e.SaleCost
						select e)
					{
						Vector3 randomSectorPositionWithinGateDistance = EngineASX.Instance.LocalUnitSector.GetRandomSectorPositionWithinGateDistance(0.75f);
						Vector3? vector = PhysicsNonOverlappingPositionFinder.FindSectorPositionOrNull(EngineASX.Instance.LocalPlayerSector, randomSectorPositionWithinGateDistance, GameController.Instance.GameSettings.MinDistanceBetweenStations, GameController.Instance.StaticNonOverlappingMask);
						if (vector.HasValue && (item.StationPurpose != StationPurpose.SectorControl || !(EngineASX.Instance.LocalUnitSector.ControllingFaction != null)))
						{
							SpawnUnit(item.UnitPrefab, EngineASX.Instance.LocalPlayerSector, vector.Value, EngineASX.Instance.LocalFaction, addCargoLoadout: true, isUnderConstruction: false, silent: true);
							num++;
						}
					}
					return num;
				}
			}
			return 0;
		}

		public static Vector3 GetSpawnSectorPositionFromLocalUnit(Unit localUnit, float distance = 300f)
		{
			if (localUnit == null)
			{
				throw new Exception("Local unit missing");
			}
			return localUnit.SectorPosition + localUnit.transform.rotation * Vector3.forward * distance;
		}

		public static Faction GetOrCreateHostileWithAllBanditFaction()
		{
			Faction faction = EngineASX.Instance.Factions.Where((Faction e) => e.IsValidInGame && e.FactionType == FactionType.Bandit && e.AISettings.HostileWithAll).GetRandom();
			if (faction == null)
			{
				FactionSpawnerSpawnType factionSpawnerSpawnType = EngineASX.Instance.FactionSpawner.Settings.FactionTypes.FirstOrDefault((FactionSpawnerSpawnType e) => e.TypeInfo.FactionType == FactionType.Bandit && !e.IsFreelancer);
				if (factionSpawnerSpawnType != null)
				{
					faction = FactionSpawner.CreateFactionAndAIAndAssignName(factionSpawnerSpawnType);
					faction.AISettings.HostileWithAll = true;
				}
			}
			return faction;
		}

		public static int SpawnBanditHordesAtRandomSectorPositions(Sector sector, int bigFleetCount = 2, int smallFleetCount = 5)
		{
			int num = 0;
			FactionSpawnerSpawnType factionSpawnerSpawnType = EngineASX.Instance.FactionSpawner.Settings.FactionTypes.FirstOrDefault((FactionSpawnerSpawnType e) => e.TypeInfo.FactionType == FactionType.Bandit && !e.IsFreelancer);
			if (factionSpawnerSpawnType != null)
			{
				Faction faction = FactionSpawner.CreateFactionAndAIAndAssignName(factionSpawnerSpawnType);
				faction.AISettings.HostileWithAll = true;
				for (int num2 = 0; num2 < bigFleetCount; num2++)
				{
					int num3 = UnityEngine.Random.Range(5, 8);
					SpawnFleetWithLargerUnitsAtRandomSectorPosition(sector, faction, num3);
					num += num3;
				}
				for (int num4 = 0; num4 < smallFleetCount; num4++)
				{
					int num5 = UnityEngine.Random.Range(5, 8);
					SpawnFleetWithSmallUnitsAtRandomSectorPosition(sector, faction, num5);
					num += num5;
				}
			}
			return num;
		}

		public static int SpawnBanditHordesAtCurrentSectorPosition(int bigFleetCount = 2, int smallFleetCount = 5)
		{
			return SpawnBanditHordesAtSectorPosition(EngineASX.Instance.LocalUnitSector, EngineASX.Instance.LocalRootUnit.SectorPosition, bigFleetCount, smallFleetCount);
		}

		public static int SpawnBanditHordesAtSectorPosition(Sector sector, Vector3 sectorPosition, int bigFleetCount = 2, int smallFleetCount = 5)
		{
			if (EngineASX.Instance.FactionSpawner == null || EngineASX.Instance.FactionSpawner.Settings == null)
			{
				return 0;
			}
			int num = 0;
			FactionSpawnerSpawnType factionSpawnerSpawnType = EngineASX.Instance.FactionSpawner.Settings.FactionTypes.FirstOrDefault((FactionSpawnerSpawnType e) => e.TypeInfo.FactionType == FactionType.Bandit && !e.IsFreelancer);
			if (factionSpawnerSpawnType != null)
			{
				Faction faction = FactionSpawner.CreateFactionAndAIAndAssignName(factionSpawnerSpawnType);
				faction.AISettings.HostileWithAll = true;
				for (int num2 = 0; num2 < bigFleetCount; num2++)
				{
					int num3 = UnityEngine.Random.Range(5, 8);
					SpawnFleetWithLargerUnits(faction, sector, sectorPosition + Geometry.RandomXZUnitVector() * UnityEngine.Random.Range(100f, 300f), num3);
					num += num3;
				}
				for (int num4 = 0; num4 < smallFleetCount; num4++)
				{
					int num5 = UnityEngine.Random.Range(5, 8);
					SpawnFleetWithSmallUnits(faction, sector, sectorPosition + Geometry.RandomXZUnitVector() * UnityEngine.Random.Range(100f, 300f), num5);
					num += num5;
				}
			}
			return num;
		}

		public static void SpawnFleetWithSmallUnitsAtRandomSectorPosition(Sector sector, Faction faction, int count)
		{
			Vector3 randomSectorPositionWithinGateDistance = sector.GetRandomSectorPositionWithinGateDistance();
			SpawnFleetWithSmallUnits(faction, sector, randomSectorPositionWithinGateDistance, count);
		}

		public static void SpawnFleetWithLargerUnitsAtRandomSectorPosition(Sector sector, Faction faction, int count)
		{
			Vector3 randomSectorPositionWithinGateDistance = sector.GetRandomSectorPositionWithinGateDistance();
			SpawnFleetWithLargerUnits(faction, sector, randomSectorPositionWithinGateDistance, count);
		}

		public static void SpawnFleetWithSmallUnits(Faction faction, Sector sector, Vector3 sectorPosition, int count)
		{
			IList<Unit> units = SpawnSmallCombatUnits(faction, sector, sectorPosition, count);
			SpawnPlayerFleetWithUnits(faction, units);
		}

		public static void SpawnFleetWithLargerUnits(Faction faction, Sector sector, Vector3 sectorPosition, int count)
		{
			IList<Unit> units = SpawnLargerCombatUnits(faction, sector, sectorPosition, count);
			SpawnPlayerFleetWithUnits(faction, units);
		}

		public static void SpawnFleetWithLargerMiningUnits(Faction faction, Sector sector, Vector3 sectorPosition, int count)
		{
			IList<Unit> units = SpawnLargeMiningUnits(faction, sector, sectorPosition, count);
			SpawnPlayerFleetWithUnits(faction, units);
		}

		public static void SpawnPlayerFleetWithUnits(Faction faction, IList<Unit> units)
		{
			Fleet fleet = OrdersHelper.CreateAndInitPlayerFleet(faction, units[0].Sector, units[0].SectorPosition, EngineASX.Instance.PlayerFleetPrefab);
			foreach (Unit unit in units)
			{
				OrdersHelper.FindOrCreateNpc(unit).Fleet = fleet;
			}
		}

		public static Fleet SpawnGenericFleetWithUnits(Faction faction, IList<Unit> units)
		{
			Fleet genericFleetPrefab = EngineASX.Instance.GenericFleetPrefab;
			Sector sector = units[0].Sector;
			Vector3 sectorPosition = units[0].SectorPosition;
			Fleet fleet = UnityObjectHelper.InstantiateAndGetComponent(genericFleetPrefab);
			fleet.Init();
			fleet.Faction = faction;
			fleet.Sector = sector;
			fleet.transform.localPosition = sectorPosition;
			if (faction.PreferredFormationStyle != null)
			{
				fleet.FleetFormation = EngineASX.Instance.GetFleetFormationById(faction.PreferredFormationStyle.UniqueId);
			}
			foreach (Unit unit in units)
			{
				OrdersHelper.FindOrCreateNpc(unit).Fleet = fleet;
			}
			return fleet;
		}

		private static IList<Unit> SpawnSmallCombatUnits(Faction faction, Sector sector, Vector3 sectorPosition, int count)
		{
			Unit[] prefabs = new Unit[3]
			{
				GameController.Instance.UnitClasses.Hauler_A.UnitPrefab,
				GameController.Instance.UnitClasses.Venture_A.UnitPrefab,
				GameController.Instance.UnitClasses.Shuttle_A.UnitPrefab
			};
			return SpawnUnitsFromSet(faction, sector, sectorPosition, prefabs, count);
		}

		public static IList<Unit> SpawnLargeMiningUnits(Faction faction, Sector sector, Vector3 sectorPosition, int count)
		{
			Unit[] prefabs = new Unit[1] { GameController.Instance.UnitClasses.Hauler_M.UnitPrefab };
			return SpawnUnitsFromSet(faction, sector, sectorPosition, prefabs, count);
		}

		public static IList<Unit> SpawnLargerCombatUnits(Faction faction, Sector sector, Vector3 sectorPosition, int count)
		{
			Unit[] prefabs = new Unit[5]
			{
				GameController.Instance.UnitClasses.Orion_A.UnitPrefab,
				GameController.Instance.UnitClasses.Thunder_A.UnitPrefab,
				GameController.Instance.UnitClasses.Venture_A.UnitPrefab,
				GameController.Instance.UnitClasses.Overlord_A.UnitPrefab,
				GameController.Instance.UnitClasses.Magnus_A.UnitPrefab
			};
			return SpawnUnitsFromSet(faction, sector, sectorPosition, prefabs, count);
		}

		public static IList<Unit> SpawnCombatShips(Faction faction, Sector sector, Vector3 sectorPosition, int count, float minCombatRating, float maxCombatRating)
		{
			IEnumerable<Unit> prefabs = from e in EngineASX.Instance.UnitClasses
				where e.IsUsable && !e.IsBarebonesShip && e.SeedInSandbox && e.UnitType == UnitType.Ship && e.ShipType == ShipType.Normal && e.CombatRating >= minCombatRating && e.CombatRating <= maxCombatRating && e.PurposeWarship
				select e.UnitPrefab;
			return SpawnUnitsFromSet(faction, sector, sectorPosition, prefabs, count);
		}

		public static IList<Unit> SpawnUnitsFromSet(Faction faction, Sector sector, Vector3 sectorPosition, IEnumerable<Unit> prefabs, int count)
		{
			Unit[] array = new Unit[count];
			for (int i = 0; i < count; i++)
			{
				Unit unit = SpawnUnit(prefabs.GetRandom(), sector, sectorPosition, faction);
				Vector3 localPosition = sectorPosition + Vector3.right * i * 100f;
				localPosition.y = 0f;
				unit.transform.localPosition = localPosition;
				array[i] = unit;
			}
			return array;
		}

		public static Unit TrySpawnUnit(Unit unitPrefab, Sector sector, Vector3 sectorPosition, Faction faction, bool addCargoLoadout = true, bool isUnderConstruction = false, bool silent = false)
		{
			if (EngineASX.Instance.CanSpawnUnitsInSector(sector, unitPrefab.UnitClass.UnitType, 1))
			{
				return SpawnUnit(unitPrefab, sector, sectorPosition, faction, addCargoLoadout, isUnderConstruction, silent);
			}
			return null;
		}

		public static Unit SpawnUnit(Unit unitPrefab, Sector sector, Vector3 sectorPosition, Faction faction, bool addCargoLoadout = true, bool isUnderConstruction = false, bool silent = false)
		{
			sectorPosition.y = 0f;
			Unit unit = WorldHelper.SpawnUnitAndInstallComponentsAtSafePosition(unitPrefab, sector, sectorPosition, 0f, addCargoLoadout, isUnderConstruction);
			unit.Faction = faction;
			if (unit.Faction != null && unit.Faction.Intel != null && unit.IsDiscoverableType)
			{
				unit.Faction.Intel.DiscoverUnit(unit);
			}
			if (TurretControllerSpawner.ShouldAutomateTurrets(unit))
			{
				EngineASX.Instance.TurretControllerSpawner.AutomateTurrets(unit);
			}
			if (unit.IsStation())
			{
				EngineASX.Instance.NotifyNewStationConstructionStarted(unit, silent);
				EngineASX.Instance.NotifyUnitConstructionFinished(unit, silent);
			}
			return unit;
		}

		public static Unit SpawnCargoContainerNearLocalUnit(CargoClass cargoClass, int quantity = 10, float distance = 50f)
		{
			Vector3 spawnSectorPositionFromLocalUnit = GetSpawnSectorPositionFromLocalUnit(EngineASX.Instance.LocalUnit, distance);
			return SpawnCargoContainer(cargoClass, quantity, EngineASX.Instance.ActiveSector, spawnSectorPositionFromLocalUnit, null);
		}

		public static Unit SpawnCargoContainer(CargoClass cargoClass, int quantity, Sector sector, Vector3 sectorPosition, Faction faction)
		{
			Unit unit = SpawnUnit(GameController.Instance.UnitClasses.CargoContainer.UnitPrefab, sector, sectorPosition, faction, addCargoLoadout: false);
			unit.CargoComponent.CargoClass = cargoClass;
			unit.CargoComponent.Quantity = quantity;
			unit.CargoComponent.SetSpawnTime();
			unit.CargoComponent.SetHealthBasedOnVolume();
			return unit;
		}

		public static List<Unit> SpawnPropertyEverywhere(Faction faction, Sector startSector, int maxJumpDistance = 5, float chanceOfStationInSector = 0.25f, float chanceOfShipInSector = 0.35f, int maxStationsInSector = 4, int maxShipsInSector = 20, float stationCountPower = 4f, float shipCountPower = 1f)
		{
			List<Unit> units = new List<Unit>(20);
			WorldHelper.SimpleSectorSearch(startSector, maxJumpDistance, (Sector sector) =>
			{
				if (units.Count == 0 || UnityEngine.Random.value < 0.5f)
				{
					if (units.Count == 0 || UnityEngine.Random.value < chanceOfStationInSector)
					{
						int num = Maths.RandomIntWithPower(1, maxStationsInSector, stationCountPower);
						for (int i = 0; i < num; i++)
						{
							UnitClass random = EngineASX.Instance.UnitClasses.Where((UnitClass e) => e.StationPurpose != StationPurpose.SectorControl && e.SeedInSandbox && e.IsUsable && e.UnitType == UnitType.Station && !e.IsMinorStation()).GetRandom();
							if (random != null)
							{
								Vector3? randomSafeDeploymentSectorPositionOrNull = sector.GetRandomSafeDeploymentSectorPositionOrNull(UnityEngine.Random.Range(0f, 0.8f), GameController.Instance.GameSettings.MinDistanceBetweenStations);
								if (randomSafeDeploymentSectorPositionOrNull.HasValue)
								{
									Unit unit = TrySpawnUnit(random.UnitPrefab, sector, randomSafeDeploymentSectorPositionOrNull.Value, faction, addCargoLoadout: true, isUnderConstruction: false, silent: true);
									if (unit != null)
									{
										units.Add(unit);
									}
								}
							}
						}
					}
					if (units.Count == 0)
					{
						_ = 1;
					}
					else
						_ = UnityEngine.Random.value < chanceOfShipInSector;
					int num2 = Maths.RandomIntWithPower(1, maxShipsInSector, shipCountPower);
					for (int num3 = 0; num3 < num2; num3++)
					{
						UnitClass random2 = EngineASX.Instance.UnitClasses.Where((UnitClass e) => e.UnitType == UnitType.Ship && e.SeedInSandbox && e.IsUsable && !e.IsBarebonesShip).GetRandom();
						if (random2 != null)
						{
							Vector3? randomSafeDeploymentSectorPositionOrNull2 = sector.GetRandomSafeDeploymentSectorPositionOrNull(UnityEngine.Random.Range(0f, 0.8f), random2.ShieldRingRadius, GameController.Instance.StaticNonOverlappingMask);
							if (randomSafeDeploymentSectorPositionOrNull2.HasValue)
							{
								Unit unit2 = TrySpawnUnit(random2.UnitPrefab, sector, randomSafeDeploymentSectorPositionOrNull2.Value, faction, addCargoLoadout: true, isUnderConstruction: false, silent: true);
								if (unit2 != null)
								{
									units.Add(unit2);
								}
							}
						}
					}
				}
			});
			return units;
		}
	}
}
