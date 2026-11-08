using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.Bounty;
using OpenFrontier.IP.Scenarios;
using OpenFrontier.IP.Testing.FactionControl;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class SpawnPointSeeder : MonoBehaviour
	{
		public SpawnPointSeederSettings SpawnPointSeederSettings;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Create spawn points...", this, 1);
			}
			SpawnPointSeederSettings = world.Seeder.Settings.SpawnPointSeederSettings;
			OpenFrontier.IP.Scenarios.UniverseWorld universeWorld = (OpenFrontier.IP.Scenarios.UniverseWorld)world;
			UniverseScenarioData universeScenarioData = universeWorld.GetUniverseScenarioData();
			List<UniverseWorldSpawnData> spawnLocations = CreateSpawnPoints(universeScenarioData.GameTypeInfo, SpawnPointSeederSettings, out var spawnStation);
			universeWorld.RespawnPlayerIfNull();
			if (spawnStation != null && spawnStation.Faction != null)
			{
				float change = UnityEngine.Random.Range(universeScenarioData.GameTypeInfo.MinBonusWithSpawnedStationFaction, universeScenarioData.GameTypeInfo.MaxBonusWithSpawnedStationFaction);
				spawnStation.Faction.ChangeOpinion(EngineASX.Instance.LocalFaction, change);
			}
			Debug.Log($"Loading the {universeScenarioData.GameTypeInfo.name} game type");
			EngineASX.Instance.LocalFaction.Credits = universeScenarioData.GameTypeInfo.StartingCredits;
			EngineASX.Instance.LocalPlayer.Person.CustomName = universeScenarioData.PilotName;
			if (string.IsNullOrEmpty(universeScenarioData.PilotName))
			{
				EngineASX.Instance.LocalPlayer.Person.CustomName = GameController.Instance.DefaultPilotName;
			}
			EngineASX.Instance.LocalPlayer.Person.RefreshName();
			SpawnStartingShipsFromGameType(universeWorld, universeScenarioData, spawnLocations);
		}

		public static List<UniverseWorldSpawnData> CreateSpawnPoints(UniverseGameTypeInfo gameTypeInfo, SpawnPointSeederSettings settings, out Unit spawnStation)
		{
			List<UniverseWorldSpawnData> list = new List<UniverseWorldSpawnData>();
			if (GetGameTypeSpawnPosition(EngineASX.Instance, gameTypeInfo, settings, out spawnStation, out var spawnSector, out var sectorPosition, out var rotation))
			{
				for (int i = 0; i < gameTypeInfo.SpawnUnits.Count; i++)
				{
					UniverseWorldSpawnData universeWorldSpawnData = UnityObjectHelper.NewGameObject<UniverseWorldSpawnData>();
					universeWorldSpawnData.PlayerVirtue = gameTypeInfo.DefaultVirtue;
					universeWorldSpawnData.transform.SetParent(spawnSector.transform);
					universeWorldSpawnData.transform.localRotation = rotation;
					universeWorldSpawnData.transform.localPosition = sectorPosition + rotation * new Vector3((float)i * 30f, 0f, 0f);
					universeWorldSpawnData.SpawnedUnitIndex = i;
					universeWorldSpawnData.GameTypeInfo = gameTypeInfo;
					list.Add(universeWorldSpawnData);
				}
			}
			else
			{
				Debug.LogError("SpawnPointSeeder: Failed to determine spawn position for game type: " + gameTypeInfo);
			}
			return list;
		}

		private void SpawnStartingShipsFromGameType(OpenFrontier.IP.Scenarios.UniverseWorld universeWorld, UniverseScenarioData loadData, List<UniverseWorldSpawnData> spawnLocations)
		{
			if (spawnLocations.Count > 0)
			{
				foreach (UniverseWorldSpawnData spawnLocation in spawnLocations)
				{
					Unit unit = spawnLocation.Spawn(universeWorld, loadData);
					if (spawnLocation.IsPlayerSpawn && unit != null)
					{
						universeWorld.RespawnAtUnit(unit.Components);
					}
				}
			}
			else
			{
				Debug.LogErrorFormat(this, "No spawn location defined for game type {0}", loadData.GameTypeInfo.GameType);
			}
			ApplySpecificGameTypeSetup(loadData.GameTypeInfo);
		}

		private void ApplySpecificGameTypeSetup(UniverseGameTypeInfo gameTypeInfo)
		{
			switch (gameTypeInfo.GameType)
			{
			case UniverseGameType.FleetCommander:
				SetupFleetCommanderGameType();
				break;
			case UniverseGameType.RefineryOwner:
				SetupRefineryGameMode();
				break;
			case UniverseGameType.TheFugitive:
				SetupFugitiveGameMode();
				break;
			case UniverseGameType.Empire:
				SetupEmpireGameMode();
				break;
			case UniverseGameType.Bandits:
				SetupBanditsGameMode(gameTypeInfo);
				break;
			case UniverseGameType.Interceptor:
				break;
			}
		}

		private bool CanTakeControlOfEmpireFaction(Faction faction)
		{
			if (faction.Name == "CLL Systems")
			{
				return false;
			}
			if (EngineASX.Instance.World.ScenarioInfo.UniqueId == 4000)
			{
				int uniqueId = faction.UniqueId;
				if (uniqueId == 1000 || uniqueId == 6000 || uniqueId == 9000)
				{
					return true;
				}
				return false;
			}
			return true;
		}

		private void SetupEmpireGameMode()
		{
			Unit unit = EngineASX.Instance.LocalFaction.Units.Where((Unit e) => e.UnitType == UnitType.Ship).FirstOrDefault();
			Faction randomEmpire = EngineASX.Instance.Factions.Where((Faction e) => e.FactionType == FactionType.Empire && !e.IsPlayerFaction && CanTakeControlOfEmpireFaction(e)).GetRandom();
			if (!EngineASX.Instance.World.Seeder.Settings.CreateEmpireFactionsSeederSettings.SpecialFactionPrefabs.Any((Faction e) => e.Name == randomEmpire.Name))
			{
				randomEmpire.Virtue = 0.5f;
			}
			FactionControlUtil.SwitchLocalPlayerFactionToAndDestroyExisting(randomEmpire);
			BountyHelper.ReleaseBountiesOnFaction(EngineASX.Instance.LocalFaction);
			if (unit != null)
			{
				unit.Sector = randomEmpire.HomeSector;
				Unit unit2 = (from e in randomEmpire.Units
					where e.UnitType == UnitType.Station
					orderby e.UnitClass.SaleCost descending
					select e).FirstOrDefault();
				if (unit2 != null)
				{
					unit.MoveNearTo(unit2);
					unit.LookAt(unit2);
				}
			}
			try
			{
				randomEmpire.RepairAllUnits();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private float ScoreBanditFactionForSpawning(Faction faction)
		{
			float num = 0f;
			if (faction.IsFreelancer)
			{
				num -= 10f;
			}
			if (faction.GetValidShipAndStationCount() < 3)
			{
				num -= 5f;
			}
			return num + UnityEngine.Random.value * 4f;
		}

		private void SetupBanditsGameMode(UniverseGameTypeInfo gameTypeInfo)
		{
			Unit unit = EngineASX.Instance.LocalFaction.Units.Where((Unit e) => e.UnitType == UnitType.Ship).FirstOrDefault();
			Faction faction = (from e in EngineASX.Instance.Factions
				where e.FactionType == FactionType.Bandit && e.ShortName != "Bandits" && !e.IsPlayerFaction
				orderby ScoreBanditFactionForSpawning(e) descending
				select e).FirstOrDefault();
			if (faction == null)
			{
				Debug.LogError("Could not find bandit faction to take control of", this);
				return;
			}
			FactionControlUtil.SwitchLocalPlayerFactionToAndDestroyExisting(faction);
			BountyHelper.ReleaseBountiesOnFaction(EngineASX.Instance.LocalFaction);
			EngineASX.Instance.LocalFaction.Virtue = gameTypeInfo.DefaultVirtue;
			if (unit != null)
			{
				unit.Sector = faction.HomeSector;
				Unit unit2 = (from e in faction.Units
					where e.UnitType == UnitType.Station
					orderby e.UnitClass.SaleCost descending
					select e).FirstOrDefault();
				if (unit2 != null)
				{
					unit.MoveNearTo(unit2);
					unit.LookAt(unit2);
				}
			}
			try
			{
				faction.RepairAllUnits();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private void SetupRefineryGameMode()
		{
			Unit localUnit = EngineASX.Instance.LocalUnit;
			if (localUnit == null)
			{
				Debug.LogError("Missing player unit for refinery owner mode", this);
				return;
			}
			if (GameController.Instance.UnitClasses.Refinery == null)
			{
				Debug.LogError("Cannot setup refinery owner game type. Refinery parts not defined", this);
				return;
			}
			UnitClass refinery = GameController.Instance.UnitClasses.Refinery;
			Unit unit = WorldHelper.SpawnUnitAndInstallComponentsAtSafePosition(refinery.UnitPrefab, localUnit.Sector, localUnit.SectorPosition + localUnit.transform.forward * 200f, refinery.DisplayData.BuildBlockerRadius, addCargoLoadout: false);
			unit.Faction = localUnit.Faction;
			int num = 0;
			foreach (Unit unit2 in localUnit.Faction.Units)
			{
				if (unit2.UnitType == UnitType.Ship && unit2.Sector == localUnit.Sector && !unit2.IsDocked)
				{
					unit2.transform.localPosition = unit.SectorPosition + Vector3.back * 100f + Vector3.right * num * 40f;
					unit2.transform.localRotation = Quaternion.LookRotation(Vector3.Normalize(unit.SectorPosition - unit2.SectorPosition), Vector3.up);
					num++;
				}
			}
			ConstructedStationBroadcaster.Broadcast(unit);
		}

		private void SetupFugitiveGameMode()
		{
			int num = 5;
			int num2 = 0;
			List<Faction> list = EngineASX.Instance.Factions.ToList();
			list.Shuffle();
			foreach (Faction item in list)
			{
				if (!ShouldAddBountyForFugitiveMode(item))
				{
					continue;
				}
				FactionBountyBoard nearestBountyBoardToHomeSector = item.GetNearestBountyBoardToHomeSector();
				if (!(nearestBountyBoardToHomeSector == null))
				{
					int num3 = UnityEngine.Random.Range(100000, 200000);
					if (item.FactionType == FactionType.Empire)
					{
						num3 *= UnityEngine.Random.Range(10, 20);
					}
					if (BountyHelper.AddBountyWithRounding(item, nearestBountyBoardToHomeSector, EngineASX.Instance.LocalPlayer.Person, num3, 0.0, updateLastKnownPosition: true) != null)
					{
						item.SetOpinionWithTwoWay(EngineASX.Instance.LocalFaction, UnityEngine.Random.Range(-1f, -0.8f));
					}
					num2++;
					if (num2 >= num)
					{
						break;
					}
				}
			}
		}

		private bool ShouldAddBountyForFugitiveMode(Faction faction)
		{
			if (faction.IsPlayerFaction)
			{
				return false;
			}
			switch (faction.FactionType)
			{
			case FactionType.Scavenger:
			case FactionType.BountyHunter:
			case FactionType.Bandit:
			case FactionType.EquipmentDealer:
			case FactionType.Bar:
				return false;
			default:
				if (!faction.IsMinor)
				{
					return !faction.IsFreelancer;
				}
				return false;
			}
		}

		private static void SetupFleetCommanderGameType()
		{
			Unit playerUnit = EngineASX.Instance.PlayerUnit;
			if (!(playerUnit != null) || !playerUnit.IsOwnedByPlayer || !playerUnit.IsDockable)
			{
				return;
			}
			List<Unit> unitsByType = playerUnit.Faction.GetUnitsByType(UnitType.Ship);
			if (unitsByType == null)
			{
				return;
			}
			foreach (Unit item in unitsByType)
			{
				if (item != playerUnit)
				{
					item.Components.TryDockInUnit(playerUnit);
				}
			}
		}

		private static bool GetGameTypeSpawnPosition(EngineASX engine, UniverseGameTypeInfo gameTypeInfo, SpawnPointSeederSettings settings, out Unit station, out Sector spawnSector, out Vector3 sectorPosition, out Quaternion rotation)
		{
			if (gameTypeInfo.GameType == UniverseGameType.RefineryOwner)
			{
				station = null;
				return GetSpawnPositionForRefineryOwner(out spawnSector, out sectorPosition, out rotation);
			}
			return GetSafePositionNearStation(engine, gameTypeInfo, settings, out station, out spawnSector, out sectorPosition, out rotation);
		}

		private static bool GetSpawnPositionForRefineryOwner(out Sector spawnSector, out Vector3 sectorPosition, out Quaternion rotation)
		{
			float num = 0f;
			Sector sector = null;
			foreach (Sector sector2 in EngineASX.Instance.Sectors)
			{
				float num2 = ScoreSectorForRefineryOwnerGameType(sector2);
				if (sector == null || num2 > num)
				{
					sector = sector2;
					num = num2;
				}
			}
			spawnSector = sector;
			rotation = Quaternion.identity;
			sectorPosition = GetRefineryOwnerSpawnSectorPosition(spawnSector);
			return true;
		}

		private static Vector3 GetRefineryOwnerSpawnSectorPosition(Sector spawnSector)
		{
			List<Unit> unitsByType = spawnSector.GetUnitsByType(UnitType.AsteroidCluster);
			if (unitsByType != null && unitsByType.Count > 0)
			{
				Unit unit = null;
				float num = 0f;
				foreach (Unit item in unitsByType)
				{
					float num2 = 0f;
					int countOfStationPurposeWithinRadius = item.GetCountOfStationPurposeWithinRadius(StationPurpose.Refinery);
					num2 -= (float)countOfStationPurposeWithinRadius * 4f;
					int num3 = EngineASX.Instance.CountUnitsInArea(spawnSector, item.SectorPosition, item.Radius, GameController.Instance.StationsMask, (Unit unit2) => unit2.Faction != null && (unit2.Faction.FactionType == FactionType.Bandit || unit2.Faction.IsHostileToAll()));
					num2 -= (float)num3 * 4f;
					int num4 = EngineASX.Instance.CountUnitsInArea(spawnSector, item.SectorPosition, item.Radius, GameController.Instance.ShipsMask, (Unit unit2) => unit2.Faction != null && (unit2.Faction.FactionType == FactionType.Bandit || unit2.Faction.IsHostileToAll()));
					num2 -= (float)num4 * 1f;
					if (unit == null || num2 > num)
					{
						unit = item;
						num = num2;
					}
				}
				Vector3 randomSectorPositionWithinRadius = unit.GetRandomSectorPositionWithinRadius();
				return PhysicsNonOverlappingPositionFinder.FindSectorPosition(spawnSector, randomSectorPositionWithinRadius, 500f, GameController.Instance.StaticNonOverlappingMask);
			}
			Vector3 randomSectorPositionWithinBounds = spawnSector.GetRandomSectorPositionWithinBounds(0.8f);
			return PhysicsNonOverlappingPositionFinder.FindSectorPosition(spawnSector, randomSectorPositionWithinBounds, 500f, GameController.Instance.StaticNonOverlappingMask);
		}

		private static float ScoreSectorForRefineryOwnerGameType(Sector sector)
		{
			float num = 0f;
			int countOfUnitType = sector.GetCountOfUnitType(UnitType.AsteroidCluster);
			if ((float)countOfUnitType > 0f)
			{
				num += 100f;
				num += (float)countOfUnitType * 1f;
			}
			int countOfStationPurpose = sector.GetCountOfStationPurpose(StationPurpose.Refinery);
			num -= 4f * (float)countOfStationPurpose;
			if (sector.SecurityLevel < 0.25f)
			{
				num -= 2f;
			}
			if (sector.FactionsHeadquartered.Any((Faction e) => !e.IsBanditOrOutlaw()))
			{
				num += 2f;
			}
			if (sector.UnclampedSecurityLevel < 0f)
			{
				num += Mathf.Clamp(sector.UnclampedSecurityLevel, -1f, 1f) * 10f;
			}
			if (sector.JumpDistanceToNearestControlledSector > 3)
			{
				num -= 10f + (float)sector.JumpDistanceToNearestControlledSector * 1f;
			}
			return num + UnityEngine.Random.value * 8f;
		}

		private static bool GetSafePositionNearStation(EngineASX engine, UniverseGameTypeInfo gameTypeInfo, SpawnPointSeederSettings settings, out Unit station, out Sector spawnSector, out Vector3 sectorPosition, out Quaternion rotation)
		{
			if (!GetPositionNearStation(engine, gameTypeInfo, settings, out station, out spawnSector, out sectorPosition, out rotation))
			{
				spawnSector = engine.Sectors.GetRandom();
				if (spawnSector != null)
				{
					sectorPosition = Vector3.zero;
					sectorPosition = PhysicsNonOverlappingPositionFinder.FindSectorPosition(spawnSector, sectorPosition, 100f, GameController.Instance.NonOVerlappingUnitsMask);
					return true;
				}
				return false;
			}
			return true;
		}

		private static bool GetPositionNearStation(EngineASX engine, UniverseGameTypeInfo gameTypeInfo, SpawnPointSeederSettings settings, out Unit randomStation, out Sector spawnSector, out Vector3 sectorPosition, out Quaternion rotation)
		{
			spawnSector = null;
			sectorPosition = Vector3.zero;
			rotation = Quaternion.identity;
			randomStation = GetBestStationForGameType(engine, gameTypeInfo, settings);
			if (randomStation != null)
			{
				spawnSector = randomStation.Sector;
				float num = 70f;
				Vector3 vector = Vector3.back * (randomStation.UnitClass.ShieldRingRadius * 1.5f + num);
				sectorPosition = PhysicsNonOverlappingPositionFinder.FindSectorPosition(spawnSector, randomStation.SectorPosition + Geometry.RandomYRotation() * vector, num, GameController.Instance.StationsMask);
				float num2 = 40f;
				float y = UnityEngine.Random.Range(0f - num2, num2);
				rotation = Quaternion.LookRotation(randomStation.SectorPosition - sectorPosition, Vector3.up) * Quaternion.Euler(0f, y, 0f);
				return true;
			}
			return false;
		}

		private static bool CanSpawnAtStation(Unit station)
		{
			if (station.IsDockable && station.UnitClass.StationPurpose != StationPurpose.Defence)
			{
				return station.Faction != null;
			}
			return false;
		}

		private static float ScoreStationForGameTypeSpawn(UniverseGameTypeInfo gameTypeInfo, Unit station, SpawnPointSeederSettings settings)
		{
			Sector sector = station.Sector;
			float num = ScoreSectorForGameTypeSpawn(gameTypeInfo, sector);
			if (gameTypeInfo.SecurityTypePreference != SecurityTypePreference.None)
			{
				switch (gameTypeInfo.SecurityTypePreference)
				{
				case SecurityTypePreference.Secure:
					num += sector.SecurityLevel * gameTypeInfo.SecurityTypePreferenceMultiplier;
					break;
				case SecurityTypePreference.Insecure:
					num -= sector.SecurityLevel * gameTypeInfo.SecurityTypePreferenceMultiplier;
					break;
				}
			}
			switch (gameTypeInfo.GameType)
			{
			case UniverseGameType.Merchant:
			case UniverseGameType.MerchantFleetCaptain:
				switch (station.UnitClass.StationPurpose)
				{
				case StationPurpose.Factory:
					num += 2f;
					break;
				case StationPurpose.TradeStation:
					num += 3f;
					break;
				}
				break;
			case UniverseGameType.VeteranFighter:
			case UniverseGameType.Interceptor:
				if (station.UnitClass.StationPurpose == StationPurpose.Equipment || station.UnitClass.StationPurpose == StationPurpose.Repair)
				{
					num += 2f;
				}
				break;
			case UniverseGameType.Scavenger:
				if (station.UnitClass.StationPurpose == StationPurpose.Scrapyard)
				{
					num += 2f;
				}
				break;
			case UniverseGameType.Plunderer:
				if (station.UnitClass.StationPurpose == StationPurpose.Outpost && station.Faction != null && station.Faction.FactionType != FactionType.Empire)
				{
					num += 2f;
				}
				if (station.Faction != null && station.Faction.IsBanditOrOutlaw())
				{
					num += 1.5f;
				}
				if (!station.UnitClass.Legal)
				{
					num += 2f;
				}
				break;
			}
			num += (1f - (station.Faction.Virtue - gameTypeInfo.SpawnLocationVirtuePreference)) * gameTypeInfo.SpawnLocationVirtuePreferenceMultiplier;
			if (station.Faction.Virtue < 0.5f && gameTypeInfo.SpawnLocationVirtuePreference >= 0.5f)
			{
				num -= 100f;
			}
			return num;
		}

		private static float ScoreSectorForGameTypeSpawn(UniverseGameTypeInfo gameTypeInfo, Sector sector)
		{
			float num = 0f;
			if (gameTypeInfo.DefaultVirtue > 0.4f && sector.HasBandits())
			{
				num -= 2f;
				num -= UnityEngine.Random.value * 2f;
			}
			switch (gameTypeInfo.GameType)
			{
			case UniverseGameType.Newcomer:
			case UniverseGameType.Citizen:
				if (sector.HasPlanets)
				{
					num += 10f;
				}
				break;
			case UniverseGameType.MiningFleetCaptain:
			case UniverseGameType.Miner:
			case UniverseGameType.RefineryOwner:
				if (sector.HasAsteroidClusters)
				{
					num += 10f;
				}
				break;
			case UniverseGameType.Plunderer:
				if (sector.ControllingFaction == null)
				{
					num++;
				}
				break;
			case UniverseGameType.Enforcer:
				if (sector.ControllingFaction != null)
				{
					num += 2f;
				}
				break;
			case UniverseGameType.Merchant:
				if (sector.HasPlanets)
				{
					num += 2f;
				}
				if (sector.HasAsteroidClusters)
				{
					num++;
				}
				break;
			}
			return num + UnityEngine.Random.value * 2f;
		}

		private static bool CanSpawnGameTypeAtStation(UniverseGameTypeInfo gameTypeInfo, Unit station)
		{
			if (station.Faction != null && (station.Faction.AISettings.HostileWithAll || station.Faction.FactionType == FactionType.Bandit))
			{
				return false;
			}
			switch (gameTypeInfo.GameType)
			{
			case UniverseGameType.Newcomer:
			case UniverseGameType.Citizen:
				if (station.UnitClass.StationPurpose != StationPurpose.TradeStation && station.UnitClass.StationPurpose != StationPurpose.Factory)
				{
					return station.UnitClass.StationPurpose == StationPurpose.Scrapyard;
				}
				return true;
			case UniverseGameType.RefineryOwner:
				if (station.UnitClass.StationPurpose != StationPurpose.Refinery)
				{
					return station.Sector.GetCountOfUnitType(UnitType.AsteroidCluster) > 0;
				}
				return false;
			case UniverseGameType.MiningFleetCaptain:
			case UniverseGameType.Miner:
				return station.UnitClass.StationPurpose == StationPurpose.Refinery;
			default:
				return true;
			}
		}

		private static Unit GetBestStationForGameType(EngineASX engine, UniverseGameTypeInfo gameTypeInfo, SpawnPointSeederSettings settings)
		{
			Unit unit = null;
			float num = 0f;
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					if (CanSpawnAtStation(item) && CanSpawnGameTypeAtStation(gameTypeInfo, item))
					{
						float num2 = ScoreStationForGameTypeSpawn(gameTypeInfo, item, settings);
						if (unit == null || num2 > num)
						{
							unit = item;
							num = num2;
						}
					}
				}
			}
			return unit;
		}
	}
}
