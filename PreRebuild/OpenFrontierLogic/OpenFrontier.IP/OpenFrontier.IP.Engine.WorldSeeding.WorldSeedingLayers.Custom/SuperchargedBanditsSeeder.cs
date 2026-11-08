using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.WorldPopulation;
using OpenFrontier.IP.Testing.Spawning;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.Custom
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class SuperchargedBanditsSeeder : MonoBehaviour
	{
		public void SeedLayer(WorldBase world)
		{
			if (!world.Seeder.Settings.FactionSeederSettings.SuperchargedBanditsEnabled)
			{
				return;
			}
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding super-charged bandits...", this, 1);
			}
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (faction.FactionType == FactionType.Bandit && faction.IsAIFactionType)
				{
					SuperchargeBanditFaction(faction);
				}
			}
		}

		private bool TryGetRandomPositionOutsideSectorBounds(Sector sector, out Vector3 resultSectorPosition)
		{
			resultSectorPosition = Vector3.zero;
			Vector3 randomSectorPositionOutsideGateDistance = sector.GetRandomSectorPositionOutsideGateDistance();
			Vector3? vector = PhysicsNonOverlappingPositionFinder.FindSectorPositionOrNull(sector, randomSectorPositionOutsideGateDistance, 1500f, GameController.Instance.StaticNonOverlappingMask);
			if (vector.HasValue)
			{
				resultSectorPosition = vector.Value;
				return true;
			}
			return false;
		}

		private void SuperchargeBanditFaction(Faction faction)
		{
			if (faction.Name.Contains("Bandits"))
			{
				faction.Credits += Random.Range(100000, 1500000);
			}
			else if (faction.IsFreelancerOrGang)
			{
				faction.Credits += Random.Range(100000, 1500000);
			}
			else
			{
				faction.Credits += Random.Range(7000000, 20000000);
			}
			if (!faction.IsFreelancerOrGang)
			{
				Sector homeSector = faction.HomeSector;
				if (!GetFactionHomeSectorPosition(faction, out var sectorPosition))
				{
					return;
				}
				if (homeSector != null)
				{
					int num = Random.Range(1, 3);
					for (int i = 0; i < num; i++)
					{
						Vector3 homeSectorPosition = sectorPosition;
						if (Random.value < 0.5f && TryGetRandomPositionOutsideSectorBounds(homeSector, out var resultSectorPosition))
						{
							homeSectorPosition = resultSectorPosition;
						}
						TryBuildStationAtSafePosition(faction, homeSector, homeSectorPosition, GameController.Instance.UnitClasses.Shipyard);
					}
					if (Random.value < 0.5f)
					{
						Vector3 homeSectorPosition2 = sectorPosition;
						if (Random.value < 0.5f && TryGetRandomPositionOutsideSectorBounds(homeSector, out var resultSectorPosition2))
						{
							homeSectorPosition2 = resultSectorPosition2;
						}
						TryBuildStationAtSafePosition(faction, homeSector, homeSectorPosition2, GameController.Instance.UnitClasses.Lab);
					}
					if (Random.value < 0.5f)
					{
						Vector3 homeSectorPosition3 = sectorPosition;
						if (Random.value < 0.5f && TryGetRandomPositionOutsideSectorBounds(homeSector, out var resultSectorPosition3))
						{
							homeSectorPosition3 = resultSectorPosition3;
						}
						TryBuildStationAtSafePosition(faction, homeSector, homeSectorPosition3, GameController.Instance.UnitClasses.MilitaryOutpost);
					}
					if (Random.value < 0.5f)
					{
						Vector3 homeSectorPosition4 = sectorPosition;
						if (Random.value < 0.5f && TryGetRandomPositionOutsideSectorBounds(homeSector, out var resultSectorPosition4))
						{
							homeSectorPosition4 = resultSectorPosition4;
						}
						TryBuildStationAtSafePosition(faction, homeSector, homeSectorPosition4, GameController.Instance.UnitClasses.RepairShop);
					}
				}
			}
			List<Unit> unitsByType = faction.GetUnitsByType(UnitType.Station);
			if (unitsByType != null && unitsByType.Count > 0)
			{
				CreateNewFleets(faction, unitsByType);
				foreach (Unit item in unitsByType.ToList())
				{
					if (item.IsMinorStation() || !(Random.value < 0.5f))
					{
						continue;
					}
					int num2 = Maths.RandomIntWithPower(0, 4, 1.5f);
					for (int j = 0; j < num2; j++)
					{
						Vector3? newDefensiveStationSectorPosition = WorldStationSeeder.GetNewDefensiveStationSectorPosition(item, 2f);
						if (newDefensiveStationSectorPosition.HasValue)
						{
							UnitClass unitClass = ((Random.value < 0.5f) ? GameController.Instance.UnitClasses.LightWeaponsPlatform : GameController.Instance.UnitClasses.MediumWeaponsPlatform);
							BuildStation(faction, unitClass, item.Sector, newDefensiveStationSectorPosition.Value);
						}
					}
				}
			}
			foreach (Unit unit in faction.Units)
			{
				if (ModdedUnitSeeder.ShouldModUnit(unit))
				{
					unit.Components.CargoBayComponent.RemoveAllEquipment();
					ModdedUnitSeeder.ModUnit(unit, EngineASX.Instance.World.Seeder.Settings.ModdedUnitSettings);
					float maxEquipmentUsage = 0.2f;
					if (unit.UnitType == UnitType.Ship)
					{
						maxEquipmentUsage = 0.5f;
					}
					ModdedUnitSeeder.AddUnitEquipment(unit, EngineASX.Instance.World.Seeder.Settings.ModdedUnitSettings, maxEquipmentUsage);
				}
			}
		}

		private static void CreateNewFleets(Faction faction, List<Unit> stations)
		{
			int num = Maths.RandomIntWithPower(1, 6, 1f);
			if (faction.Name.Contains("Bandits"))
			{
				num = 1;
			}
			for (int i = 0; i < num; i++)
			{
				Unit random = stations.GetRandom();
				if (random != null)
				{
					float minCombatRating = 0f;
					float maxCombatRating = 10f;
					switch (Random.Range(0, 3))
					{
					case 0:
						minCombatRating = 1f;
						maxCombatRating = 4f;
						break;
					case 1:
						minCombatRating = 1f;
						maxCombatRating = 6f;
						break;
					case 2:
						minCombatRating = 2f;
						maxCombatRating = 20f;
						break;
					}
					int count = Maths.RandomIntWithPower(3, 8, 1.2f);
					Vector3 sectorPosition = random.SectorPosition + Geometry.RandomXZUnitVector() * Random.Range(200f, 400f);
					IList<Unit> list = SpawnUtils.SpawnCombatShips(faction, random.Sector, sectorPosition, count, minCombatRating, maxCombatRating);
					if (list != null && list.Count > 0)
					{
						faction.FactionAI.CreateFleetAndNpcPilotsForUnits(faction.FactionAI.GetFleetPrefab(), faction.FactionAI.GetPilotPrefab(), list);
					}
				}
			}
		}

		private bool GetFactionHomeSectorPosition(Faction faction, out Vector3 sectorPosition)
		{
			sectorPosition = Vector3.zero;
			if (faction.HomeSectorPosition.HasValue)
			{
				sectorPosition = faction.HomeSectorPosition.Value;
				return true;
			}
			for (int i = 0; i < 6; i++)
			{
				Vector3 randomSectorPositionOutsideGateDistance = faction.HomeSector.GetRandomSectorPositionOutsideGateDistance();
				Vector3? vector = PhysicsNonOverlappingPositionFinder.FindSectorPositionOrNull(faction.HomeSector, randomSectorPositionOutsideGateDistance, 1500f, GameController.Instance.StaticNonOverlappingMask);
				if (vector.HasValue)
				{
					sectorPosition = vector.Value;
					return true;
				}
			}
			return false;
		}

		private static void TryBuildStationAtSafePosition(Faction faction, Sector sector, Vector3 homeSectorPosition, UnitClass unitClass)
		{
			Vector3? vector = PhysicsNonOverlappingPositionFinder.FindSectorPositionOrNull(sector, homeSectorPosition, 300f, GameController.Instance.StaticNonOverlappingMask);
			if (vector.HasValue)
			{
				BuildStation(faction, unitClass, sector, vector.Value);
			}
		}

		private static void BuildStation(Faction faction, UnitClass unitClass, Sector sector, Vector3 sectorPosition)
		{
			Unit unit = WorldHelper.SpawnUnitAndInstallComponents(unitClass.UnitPrefab, sector);
			unit.Faction = faction;
			unit.transform.localPosition = sectorPosition;
		}
	}
}
