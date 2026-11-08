using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class CreateShipsSeeder : MonoBehaviour
	{
		public struct SeedFactionItem
		{
			public Faction Faction;

			public List<ShipBuildLocationItem> BuildLocationItems;

			public SeedFactionItem(Faction faction, List<ShipBuildLocationItem> buildLocations)
			{
				this = default;
				Faction = faction;
				BuildLocationItems = buildLocations;
			}
		}

		public struct SeedBuildLocation
		{
			public Unit Unit;

			public Sector Sector;

			public Vector3 SectorPosition;

			public float Preference;

			public static SeedBuildLocation FromDock(Unit unit, float preference)
			{
				return new SeedBuildLocation
				{
					Unit = unit,
					Preference = preference
				};
			}

			public static SeedBuildLocation FromSectorPosition(Sector sector, Vector3 sectorPosition, float preference)
			{
				return new SeedBuildLocation
				{
					Sector = sector,
					SectorPosition = sectorPosition,
					Preference = preference
				};
			}
		}

		public struct ShipBuildLocationItem : IWeighted
		{
			public UnitClass UnitClass { get; set; }

			public Unit Dock { get; set; }

			public float Weight { get; set; }

			public Sector Sector { get; set; }

			public Vector3 SectorPosition { get; set; }

			public static ShipBuildLocationItem FromDock(Unit dock, UnitClass unitClass, float weight)
			{
				return new ShipBuildLocationItem
				{
					Dock = dock,
					UnitClass = unitClass,
					Weight = weight
				};
			}

			public static ShipBuildLocationItem FromSectorPosition(Sector sector, Vector3 sectorPosition, UnitClass unitClass, float weight)
			{
				return new ShipBuildLocationItem
				{
					Sector = sector,
					SectorPosition = sectorPosition,
					UnitClass = unitClass,
					Weight = weight
				};
			}
		}

		public bool DockShipsInHangar;

		public CreateShipsSeederSettings CreateShipsSeederSettings;

		public List<FactionType> SpecificFactionTypes = new List<FactionType>();

		public List<FactionType> ExcludeSpecificFactionTypes = new List<FactionType>();

		public bool AddShipEquipment = true;

		public static FactionShipBuildSettings DefaultBuildSettings => GameController.Instance.GameSettings.FactionSettings.ShipBuildSettings;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding ships...", this, 1);
			}
			CreateShipsSeederSettings = world.Seeder.Settings.CreateShipsSeederSettings;
			List<SeedFactionItem> factionsToSeedWithBuildLocations = GetFactionsToSeedWithBuildLocations(world);
			while (factionsToSeedWithBuildLocations.Count > 0)
			{
				int index = Random.Range(0, factionsToSeedWithBuildLocations.Count);
				SeedFactionItem seedFactionItem = factionsToSeedWithBuildLocations[index];
				float rpMultiplier = GetRpMultiplier(seedFactionItem.Faction);
				seedFactionItem.Faction.RecacheValidTraderTargets();
				if (seedFactionItem.Faction.AISettings.FixedShipCount > 0)
				{
					BuildShipsForFixedCountFactions(seedFactionItem.Faction.FactionAI.ShipBuilder, seedFactionItem.BuildLocationItems);
					int countOfUnitType = seedFactionItem.Faction.GetCountOfUnitType(UnitType.Ship);
					if (countOfUnitType < seedFactionItem.Faction.AISettings.FixedShipCount)
					{
						Debug.LogWarning($"Seeded faction [{seedFactionItem.Faction}] does not have the required fixed ship count ({countOfUnitType}). Required: {seedFactionItem.Faction.AISettings.FixedShipCount}");
					}
					factionsToSeedWithBuildLocations.RemoveAt(index);
				}
				else if (!BuildShips(seedFactionItem.Faction.FactionAI.ShipBuilder, seedFactionItem.BuildLocationItems, rpMultiplier) || seedFactionItem.Faction.IsFreelancer)
				{
					factionsToSeedWithBuildLocations.RemoveAt(index);
				}
			}
		}

		public List<SeedFactionItem> GetFactionsToSeedWithBuildLocations(WorldBase world)
		{
			List<Faction> list = world.Engine.Factions.Where((Faction e) => e.FactionAI != null && (e.FactionAI.AISettings.BuildShips || e.FactionAI.AISettings.FixedShipCount > 0) && e.FactionAI.ShipBuilder != null && (!SpecificFactionTypes.Any() || SpecificFactionTypes.Contains(e.FactionType)) && (!ExcludeSpecificFactionTypes.Any() || !ExcludeSpecificFactionTypes.Contains(e.FactionType))).ToList();
			foreach (Faction item in list)
			{
				item.FindAndPickHomeSectorIfNull();
			}
			list.Shuffle();
			List<SeedFactionItem> list2 = new List<SeedFactionItem>(list.Count);
			foreach (Faction item2 in list)
			{
				item2.FactionAI.AssignStrategyIfNull();
				item2.FactionAI.CacheBuildableShipUnitClasses();
				if (item2.HomeSector == null)
				{
					Debug.LogError($"Faction {item2} will not be seeded with ships as it doesn't have a home sector", item2);
					continue;
				}
				if (item2.FactionAI.Strategy == null)
				{
					Debug.LogError($"Faction {item2} will not be seeded with ships as it doesn't have a strategy", item2);
					continue;
				}
				List<SeedBuildLocation> shipBuildLocationCache = GetShipBuildLocationCache(item2, item2.FactionAI.ShipBuilder, FactionShipBuilder.DefaultBuildSettings);
				if (shipBuildLocationCache.Count == 0)
				{
					Debug.LogError($"Faction {item2} will not be seeded with ships as there were no build locations", item2);
					continue;
				}
				List<ShipBuildLocationItem> list3 = new List<ShipBuildLocationItem>(20);
				foreach (SeedBuildLocation item3 in shipBuildLocationCache)
				{
					foreach (int cachedBuildableUnitClass in item2.FactionAI.cachedBuildableUnitClasses)
					{
						UnitClass unitClassById = EngineASX.Instance.GetUnitClassById(cachedBuildableUnitClass);
						if (!CreateShipsSeederSettings.SeedOnlyAvailableShips || item3.Unit.ShipTrader.Sells(unitClassById))
						{
							if (item3.Unit != null)
							{
								list3.Add(ShipBuildLocationItem.FromDock(item3.Unit, unitClassById, 0f));
							}
							else
							{
								list3.Add(ShipBuildLocationItem.FromSectorPosition(item3.Sector, item3.SectorPosition, unitClassById, 0f));
							}
						}
					}
				}
				list2.Add(new SeedFactionItem(item2, list3));
			}
			return list2;
		}

		public void RescoreBuildLocationItems(FactionShipBuilder factionShipBuilder, FactionStrategy desiredStrategy, List<ShipBuildLocationItem> items, float randomMultiplier = 1f, float purposeScoreMultiplier = 1f, float sizeScoreMultiplier = 1f)
		{
			for (int i = 0; i < items.Count; i++)
			{
				ShipBuildLocationItem value = items[i];
				value.Weight = factionShipBuilder.GetShipPreference(value.UnitClass, desiredStrategy, FactionShipBuilder.DefaultBuildSettings, purposeScoreMultiplier, sizeScoreMultiplier);
				value.Weight += Random.value * FactionShipBuilder.DefaultBuildSettings.RandomScore * randomMultiplier;
				value.Weight *= value.UnitClass.AIBuildPreferenceMultiplierFudge;
				items[i] = value;
			}
		}

		public void BuildShipsForFixedCountFactions(FactionShipBuilder factionShipBuilder, List<ShipBuildLocationItem> buildLocations)
		{
			int creditsPerShip = (factionShipBuilder.Faction.Credits - factionShipBuilder.Faction.CreditsReserve) / factionShipBuilder.FactionAI.AISettings.FixedShipCount;
			List<ShipBuildLocationItem> list = buildLocations.Where((ShipBuildLocationItem e) => e.UnitClass.SaleCost <= creditsPerShip).ToList();
			for (int num = 0; num < factionShipBuilder.FactionAI.AISettings.FixedShipCount; num++)
			{
				FactionStrategy requiredShipStrategy = factionShipBuilder.GetNextBuildItemStrategy();
				float purposeScoreMultiplier = 0.7f;
				if (factionShipBuilder.Faction.IsFreelancer)
				{
					purposeScoreMultiplier = 0.55f;
				}
				RescoreBuildLocationItems(factionShipBuilder, requiredShipStrategy, list, 1f, purposeScoreMultiplier, 48f);
				ShipBuildLocationItem buildLocationItem = (from e in list.Where((ShipBuildLocationItem e) => (requiredShipStrategy & EngineASX.Instance.GetUnitClassShipStrategyFlags(e.UnitClass)) != 0).ToList()
					orderby e.Weight descending
					select e).FirstOrDefault();
				if (buildLocationItem.UnitClass != null)
				{
					CreateUnit(buildLocationItem, factionShipBuilder.Faction);
					factionShipBuilder.Faction.Credits -= buildLocationItem.UnitClass.SaleCost;
					ImproveFixedShipFactionRelationsWithDockFaction(factionShipBuilder.Faction, buildLocationItem.Dock);
				}
				else
				{
					Debug.LogWarning($"Couldn't create a ship for faction [{factionShipBuilder.Faction}] with a fixed ship count of {factionShipBuilder.FactionAI.AISettings.FixedShipCount}. Available Credits: {factionShipBuilder.Faction.Credits}. Credits per ship: {creditsPerShip}", factionShipBuilder.Faction);
				}
			}
		}

		public static void ImproveFixedShipFactionRelationsWithDockFaction(Faction newFaction, Unit dock)
		{
			if (dock != null && dock.Faction != newFaction && !newFaction.IsHostileTo(dock.Faction) && !dock.Faction.IsHostileTo(newFaction) && !newFaction.IsNormallyOpposedTo(dock.Faction) && newFaction.GetOpinion(dock.Faction) <= 0f)
			{
				newFaction.SetOpinionWithTwoWay(dock.Faction, Maths.RandomFloatWithPower(0.3f, 0.7f, 1.5f));
			}
		}

		public bool BuildShips(FactionShipBuilder factionShipBuilder, List<ShipBuildLocationItem> buildLocations, float rpMultiplier)
		{
			if (!FactionShipBuilder.ShipCountWithinCapacity(factionShipBuilder.Faction.FactionTypeInfo, factionShipBuilder.FactionAI.AISettings.FixedShipCount, factionShipBuilder.Faction.GetCountOfUnitType(UnitType.Ship), out var _))
			{
				return false;
			}
			int num = factionShipBuilder.FactionAI.CalculateAvailableRp(rpMultiplier);
			FactionStrategy nextShipBuildStrategy = factionShipBuilder.GetNextBuildItemStrategy();
			if (nextShipBuildStrategy == FactionStrategy.Unspecified)
			{
				Debug.LogError($"Faction ({factionShipBuilder.Faction}) ship seeder could not determine a type of ship to build", factionShipBuilder.Faction);
				return false;
			}
			RescoreBuildLocationItems(factionShipBuilder, nextShipBuildStrategy, buildLocations);
			ShipBuildLocationItem buildLocationItem = (from e in buildLocations
				where (nextShipBuildStrategy & EngineASX.Instance.GetUnitClassShipStrategyFlags(e.UnitClass)) != 0
				orderby e.Weight descending
				select e).FirstOrDefault();
			if (buildLocationItem.UnitClass != null && buildLocationItem.UnitClass.RpCost <= num && factionShipBuilder.IsAffordableFromCredits(buildLocationItem.UnitClass.SaleCost))
			{
				CreateUnit(buildLocationItem, factionShipBuilder.Faction);
				factionShipBuilder.Faction.Credits -= buildLocationItem.UnitClass.SaleCost;
				return true;
			}
			return false;
		}

		public Unit CreateUnit(ShipBuildLocationItem buildLocationItem, Faction faction)
		{
			UnitClass unitClass = buildLocationItem.UnitClass;
			ModdedUnitSeederSettings moddedUnitSettings = GameController.Instance.GameSettings.ModdedUnitSettings;
			Unit unit = WorldHelper.SpawnUnitAndInstallComponents(unitClass.UnitPrefab, buildLocationItem.Sector, addCargoLoadout: false);
			if (ModdedUnitSeeder.ShouldModUnitRandomly(unit, moddedUnitSettings))
			{
				ModdedUnitSeeder.ModUnit(unit, moddedUnitSettings);
			}
			if (AddShipEquipment)
			{
				float maxEquipmentUsage = Maths.RandomFloatWithPower(0.2f, 0.6f, 1f);
				AddUnitEquipment(unit, 0f, 2f, 1.5f, maxEquipmentUsage);
			}
			unit.Faction = faction;
			if (buildLocationItem.Dock != null)
			{
				unit.Sector = buildLocationItem.Dock.Sector;
				if (!DockShipsInHangar || !unit.Components.TryDockInHangar(buildLocationItem.Dock.Components.HangarComponent))
				{
					PositionSeedUnitRandomlyAroundDock(buildLocationItem.Dock, unit);
				}
			}
			else
			{
				unit.Sector = buildLocationItem.Sector;
				unit.transform.localPosition = buildLocationItem.SectorPosition;
			}
			return unit;
		}

		public static void AddUnitEquipment(Unit unit, float minCargoLoadoutMultiplier, float maxCargoLoadoutMultiplier, float cargoLoadoutPower, float maxEquipmentUsage)
		{
			foreach (ComponentBay bay in unit.Components.Bays)
			{
				ProjectileTurretComponent projectileTurretComponent = bay.InstalledComponent as ProjectileTurretComponent;
				if (!(projectileTurretComponent != null))
				{
					continue;
				}
				CargoBayItems component = projectileTurretComponent.ProjectileTurretClass.GetComponent<CargoBayItems>();
				if (!(component != null))
				{
					continue;
				}
				foreach (CargoBayItem item in component.Items)
				{
					if (item != null)
					{
						int b = Mathf.RoundToInt((float)item.Quantity * Maths.RandomFloatWithPower(minCargoLoadoutMultiplier, maxCargoLoadoutMultiplier, cargoLoadoutPower));
						int num = Mathf.Min(unit.Components.CargoBayComponent.GetFreeSpaceFor(item.CargoClass, maxEquipmentUsage), b);
						if (num > 0)
						{
							unit.Components.CargoBayComponent.AddToCargoIfFits(item.CargoClass, num);
						}
					}
				}
			}
		}

		private static void PositionSeedUnitRandomlyAroundDock(Unit spawnDock, Unit unit)
		{
			float shieldRingRadius = spawnDock.UnitClass.ShieldRingRadius;
			Vector3 checkSectorPosition = spawnDock.SectorPosition + Geometry.RandomXZUnitVector() * (200f + Random.Range(shieldRingRadius, shieldRingRadius * 2f));
			unit.transform.localPosition = PhysicsNonOverlappingPositionFinder.FindSectorPosition(spawnDock.Sector, checkSectorPosition, unit.UnitClass.ShieldRingRadius, GameController.Instance.NonOVerlappingUnitsMask);
			unit.transform.rotation = Quaternion.FromToRotation(spawnDock.SectorPosition, unit.SectorPosition);
		}

		public List<SeedBuildLocation> GetShipBuildLocationCache(Faction faction, FactionShipBuilder shipBuilder, FactionShipBuildSettings buildSettings)
		{
			List<Sector> list = new List<Sector>(8);
			List<SeedBuildLocation> list2 = new List<SeedBuildLocation>(8);
			shipBuilder.GetShipBuildSectorsNonAlloc(list);
			foreach (Sector item in list)
			{
				if (!(item != null))
				{
					continue;
				}
				List<Unit> unitsByType = item.GetUnitsByType(UnitType.Station);
				if (unitsByType == null || unitsByType.Count == 0)
				{
					continue;
				}
				float sectorPreference = FactionHomeScenePicker.GetSectorPreference(item, faction.FactionTypeInfo);
				foreach (Unit item2 in unitsByType)
				{
					if (!faction.Intel.IsUnitDiscoveredOrOwned(item2) || !shipBuilder.IsValidBuildLocation(item2) || !item2.Components.IsDockable || !(item2.ShipTrader != null))
					{
						continue;
					}
					Faction faction2 = item2.Faction;
					if (shipBuilder.CanBuildAtDockOwnedBy(faction2))
					{
						float num = 0f;
						if (faction2 == faction)
						{
							num += buildSettings.SameFactionShipyardScore;
						}
						float preference = sectorPreference + num;
						list2.Add(SeedBuildLocation.FromDock(item2, preference));
					}
				}
			}
			if (list2.Count == 0)
			{
				Vector3 randomSectorPositionWithinGateDistance = faction.HomeSector.GetRandomSectorPositionWithinGateDistance();
				list2.Add(SeedBuildLocation.FromSectorPosition(faction.HomeSector, randomSectorPositionWithinGateDistance, 1f));
			}
			return list2;
		}

		private float GetRpMultiplier(Faction faction)
		{
			if (!faction.AISettings.PreferSingleShip)
			{
				float p = ((faction.FactionType == FactionType.Bandit) ? CreateShipsSeederSettings.BanditsRpUsedOnNewGamePower : CreateShipsSeederSettings.FactionsRpUsedOnNewGamePower);
				return Mathf.Lerp(CreateShipsSeederSettings.FactionsMinRpUsedOnNewGame, CreateShipsSeederSettings.FactionsMaxRpUsedOnNewGame, Mathf.Pow(Random.value, p));
			}
			return 1f;
		}
	}
}
