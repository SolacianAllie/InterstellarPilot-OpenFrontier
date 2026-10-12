using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers;
using OpenFrontier.IP.Scenarios;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionShipBuilder
	{
		public struct ShipBuildLocationItem
		{
			public UnitClass UnitClass;

			public Unit Dock;

			public float Score;

			public ShipBuildLocationItem(Unit dock, UnitClass unitClass, float score)
			{
				this = default;
				Dock = dock;
				UnitClass = unitClass;
				Score = score;
			}
		}

		public const float TimeoutBuildSeconds = 5000f;

		private FactionAIBase factionAI;

		protected FactionAIShipBuildItem? bestBuildItem;

		private FactionAIBuildOrder nextBuildOrder;

		private static List<Sector> shipBuildSectorCache = new List<Sector>(20);

		public List<ShipBuildLocationItem> ShipBuildLocationCache = new List<ShipBuildLocationItem>(30);

		private float lastCachedShipBuildLocationTime;

		public FactionAIBase FactionAI
		{
			get
			{
				return factionAI;
			}
			set
			{
				factionAI = value;
			}
		}

		public static FactionShipBuildSettings DefaultBuildSettings => GameController.Instance.GameSettings.FactionSettings.ShipBuildSettings;

		public Faction Faction => factionAI.Faction;

		public bool IsBuilding
		{
			get
			{
				if (nextBuildOrder != null)
				{
					return nextBuildOrder.UnitClass != null;
				}
				return false;
			}
		}

		public FactionAIBuildOrder NextBuildOrder
		{
			get
			{
				return nextBuildOrder;
			}
			set
			{
				if (nextBuildOrder != value)
				{
					nextBuildOrder = value;
				}
			}
		}

		public bool BuildShips(float rpMultiplier, int buildCap, bool includeUnaffordableFromCredits, bool includeUnaffordableFromRp)
		{
			if (factionAI.Strategy == null)
			{
				Debug.LogError("Cannot build ships without determining strategy", factionAI);
			}
			factionAI.Faction.FindAndPickHomeSectorIfNull();
			int num = 0;
			if (CanBuildShips(out var maxBuildable))
			{
				maxBuildable = Mathf.Min(maxBuildable, buildCap);
				int num2 = factionAI.CalculateAvailableRp(rpMultiplier);
				if (nextBuildOrder == null || !nextBuildOrder.IsValid(factionAI.Faction))
				{
					bestBuildItem = FindBestShipBuildItem(num2, includeUnaffordableFromCredits, includeUnaffordableFromRp);
					UpdateNextBuildOrder();
				}
				while (CanBuildNextBuildOrder(num2) && num < maxBuildable)
				{
					ExecuteBuildOrder(nextBuildOrder);
					num2 -= nextBuildOrder.RpCost;
					NextBuildOrder = null;
					bestBuildItem = FindBestShipBuildItem(num2, includeUnaffordableFromCredits, includeUnaffordableFromRp);
					UpdateNextBuildOrder();
					num++;
				}
				if (nextBuildOrder != null && IsBuildOrderTimedOut())
				{
					NextBuildOrder = null;
				}
			}
			return num > 0;
		}

		public bool CanBuildNextBuildOrder(int availableRp)
		{
			if (IsBuildOrderValid(nextBuildOrder, availableRp))
			{
				return CanBuild(availableRp);
			}
			return false;
		}

		public bool CanBuildShips(out int maxBuildable)
		{
			maxBuildable = 0;
			if (!factionAI.AISettings.BuildShips)
			{
				return false;
			}
			if (factionAI.HomeSector == null)
			{
				return false;
			}
			return ShipCountWithinCapacity(factionAI.FactionTypeInfo, factionAI.AISettings.FixedShipCount, factionAI.Faction.GetCountOfUnitType(UnitType.Ship), out maxBuildable);
		}

		public static bool ShipCountWithinCapacity(FactionTypeInfo factionType, int fixedShipCount, int currentShipCount, out int maxBuildable)
		{
			maxBuildable = 10;
			if (fixedShipCount > -1)
			{
				maxBuildable = Mathf.Min(maxBuildable, fixedShipCount - currentShipCount);
				return maxBuildable > 0;
			}
			int shipCountByFactionType = EngineASX.Instance.GetShipCountByFactionType(factionType.FactionType);
			int factionTypeShipCap = EngineASX.Instance.GetFactionTypeShipCap(factionType);
			maxBuildable = Mathf.Min(maxBuildable, factionTypeShipCap - shipCountByFactionType);
			return maxBuildable > 0;
		}

		private bool IsBuildOrderValid(FactionAIBuildOrder nextBuildOrder, int availableRp)
		{
			if (nextBuildOrder != null)
			{
				int num = factionAI.Faction.EstimateNonMinorShipAndStationCount();
				if (nextBuildOrder.IsValid(factionAI.Faction))
				{
					if (!IsBuildOrderAffordable(nextBuildOrder, availableRp))
					{
						return num > 0;
					}
					return true;
				}
				return false;
			}
			return false;
		}

		private bool IsBuildOrderTimedOut()
		{
			return EngineASX.Instance.ScenarioElapsedTime > nextBuildOrder.CreationTime + 5000.0;
		}

		public bool CanBuild(int availableRp)
		{
			if (nextBuildOrder != null)
			{
				return IsBuildOrderAffordable(nextBuildOrder, availableRp);
			}
			return false;
		}

		private bool IsBuildOrderAffordable(FactionAIBuildOrder buildOrder, int availableRp)
		{
			return IsAffordable(buildOrder.CreditsCost, buildOrder.RpCost, availableRp);
		}

		public bool IsAffordableFromCredits(int creditsCost)
		{
			int num = 0;
			if (!factionAI.AISettings.IgnoreStationCreditsReserve)
			{
				num = factionAI.Faction.CreditsReserve;
			}
			return factionAI.Faction.Credits - num >= creditsCost;
		}

		private bool IsAffordable(int creditsCost, int rpCost, int availableRp)
		{
			if (availableRp >= rpCost)
			{
				return IsAffordableFromCredits(creditsCost);
			}
			return false;
		}

		public void GetShipBuildSectorsNonAlloc(List<Sector> sectors)
		{
			if (factionAI.ShipBuildSectorMode == FactionSpawnMode.SpecificSectors)
			{
				sectors.AddRange(factionAI.ShipBuildSectors);
				return;
			}
			SectorFinder.FindNavigableDiscoveredSectorsWithinJumpDistanceOf(factionAI.HomeSector, factionAI.GetMaxShipBuildDistFromHomeSector(), Faction);
			sectors.AddRange(from e in SectorFinder.Results
				select e.Sector into e
				where FactionAI.CanBuildShipsInSector(e)
				select e);
		}

		public void CacheShipBuildLocationCache(FactionShipBuildSettings buildSettings)
		{
			shipBuildSectorCache.Clear();
			ShipBuildLocationCache.Clear();
			lastCachedShipBuildLocationTime = Time.time;
			if (factionAI.cachedBuildableUnitClasses.Count == 0)
			{
				factionAI.CacheBuildableShipUnitClasses();
			}
			if (factionAI.cachedBuildableUnitClasses.Count == 0)
			{
				return;
			}
			GetShipBuildSectorsNonAlloc(shipBuildSectorCache);
			foreach (Sector item in shipBuildSectorCache)
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
				float sectorPreference = FactionHomeScenePicker.GetSectorPreference(item, Faction.FactionTypeInfo);
				foreach (Unit item2 in unitsByType)
				{
					if (!Faction.Intel.IsUnitDiscoveredOrOwned(item2) || !IsValidBuildLocation(item2) || !item2.Components.IsDockable)
					{
						continue;
					}
					UnitShipTrader shipTrader = item2.ShipTrader;
					if (!(shipTrader != null))
					{
						continue;
					}
					Faction faction = item2.Faction;
					if ((factionAI.ShipBuildOnlyAtOwnedDocks && !(faction == factionAI.Faction)) || !CanBuildAtDockOwnedBy(faction))
					{
						continue;
					}
					float num = 0f;
					if (faction == factionAI.Faction)
					{
						num += buildSettings.SameFactionShipyardScore;
					}
					foreach (UnitShipTraderItem shipItem in shipTrader.GetShipItems())
					{
						if (factionAI.cachedBuildableUnitClasses.Contains(shipItem.UnitClass.UniqueID))
						{
							float score = sectorPreference + num;
							ShipBuildLocationCache.Add(new ShipBuildLocationItem(item2, shipItem.UnitClass, score));
						}
					}
				}
			}
		}

		public FactionStrategy GetNextBuildItemStrategy()
		{
			FactionStrategy factionStrategy = FactionStrategy.Unspecified;
			float num = 0f;
			foreach (FactionAIStrategyItem strategy in factionAI.Strategy.Strategies)
			{
				if (factionAI.AISettings.FixedShipCount < 0 && strategy.Strategy == FactionStrategy.Mine && !Faction.ValidTraderTargetsFoundRefinery)
				{
					continue;
				}
				float num2 = strategy.Weight / factionAI.Strategy.TotalWeight;
				if (num2 > 0f)
				{
					float percentageOf = Faction.FleetComposition.GetPercentageOf(strategy.Strategy);
					float num3 = num2 - percentageOf;
					if (factionStrategy == FactionStrategy.Unspecified || num3 > num)
					{
						num = num3;
						factionStrategy = strategy.Strategy;
					}
				}
			}
			return factionStrategy;
		}

		public FactionAIShipBuildItem? FindBestShipBuildItem(int availableRp, bool includeUnaffordableFromCredits, bool includeUnaffordableFromRp)
		{
			bool trustBuildLocationsAreValid = false;
			if (ShipBuildLocationCache.Count == 0 || Time.time - lastCachedShipBuildLocationTime > 120f)
			{
				CacheShipBuildLocationCache(DefaultBuildSettings);
				trustBuildLocationsAreValid = true;
			}
			if (ShipBuildLocationCache.Count == 0)
			{
				return null;
			}
			return FindBestShipBuildItem(availableRp, ShipBuildLocationCache, trustBuildLocationsAreValid, includeUnaffordableFromCredits, includeUnaffordableFromRp);
		}

		public FactionAIShipBuildItem? FindBestShipBuildItem(int availableRp, IEnumerable<ShipBuildLocationItem> buildLocationItems, bool trustBuildLocationsAreValid, bool includeUnaffordableFromCredits, bool includeUnaffordableFromRp)
		{
			FactionAIShipBuildItem? result = null;
			FactionStrategy nextBuildItemStrategy = GetNextBuildItemStrategy();
			if (nextBuildItemStrategy == FactionStrategy.Unspecified)
			{
				Debug.LogError($"Faction ({Faction}) ship builder could not determine a type of ship to build", factionAI);
				return null;
			}
			foreach (ShipBuildLocationItem buildLocationItem in buildLocationItems)
			{
				UnitClass unitClass = buildLocationItem.UnitClass;
				if ((nextBuildItemStrategy & EngineASX.Instance.GetUnitClassShipStrategyFlags(unitClass)) != 0 && (includeUnaffordableFromRp || availableRp >= unitClass.RpCost) && (includeUnaffordableFromCredits || IsAffordableFromCredits(GetCreditsCostOfUnit(buildLocationItem.Dock.Faction, unitClass))) && (trustBuildLocationsAreValid || (IsValidBuildLocation(buildLocationItem.Dock) && IsValidBuildLocationForOurFaction(buildLocationItem.Dock))))
				{
					float score = buildLocationItem.Score;
					score += GetShipPreference(buildLocationItem.UnitClass, nextBuildItemStrategy, DefaultBuildSettings);
					score += Random.value * DefaultBuildSettings.RandomScore;
					score *= unitClass.AIBuildPreferenceMultiplierFudge;
					if (!result.HasValue || score > result.Value.Weight)
					{
						FactionAIShipBuildItem value = new FactionAIShipBuildItem
						{
							BuildLocation = buildLocationItem.Dock,
							ShipTrader = buildLocationItem.Dock.ShipTrader,
							UnitClass = unitClass,
							Weight = score
						};
						result = value;
					}
				}
			}
			return result;
		}

		public bool IsValidBuildLocation(Unit dock)
		{
			if (dock != null && dock.IsDockable)
			{
				return dock.Faction != null;
			}
			return false;
		}

		private bool IsValidBuildLocationForOurFaction(Unit dock)
		{
			if (!factionAI.ShipBuildOnlyAtOwnedDocks || dock.Faction == factionAI.Faction)
			{
				return CanBuildAtDockOwnedBy(dock.Faction);
			}
			return false;
		}

		private void ApplyBuildOrderTransaction(FactionAIBuildOrder buildOrder)
		{
			if (Faction != buildOrder.ShipyardUnit.Faction)
			{
				Faction.RegisterTaxedTradeWithFaction(buildOrder.ShipyardUnit, buildOrder.ShipyardUnit.Faction, -buildOrder.CreditsCost, FactionTransactionType.ShipPurchase, null, buildOrder.UnitClass);
			}
			else
			{
				Faction.ApplyTransaction(-buildOrder.CreditsCost, FactionTransactionType.ShipPurchase, null, buildOrder.ShipyardUnit);
			}
		}

		public void CreateUnitFromBuildOrder(FactionAIBuildOrder buildOrder)
		{
			Unit shipyardUnit = buildOrder.ShipyardUnit;
			CreateUnit(buildOrder.UnitClass, shipyardUnit);
		}

		public void ExecuteBuildOrder(FactionAIBuildOrder buildOrder)
		{
			if (!IsBuilding)
			{
				Debug.LogError("Can't create group. Faction does not have build info", factionAI);
				return;
			}
			CreateUnitFromBuildOrder(buildOrder);
			factionAI.SetLastBuiltUnitTimeToCurrent();
			ApplyBuildOrderTransaction(buildOrder);
			if (Faction.IsFreelancer)
			{
				CreateShipsSeeder.ImproveFixedShipFactionRelationsWithDockFaction(Faction, buildOrder.ShipyardUnit);
			}
			if (!factionAI.Faction.HomeSectorPosition.HasValue && factionAI.HomeSector != null && factionAI.HomeSector == buildOrder.ShipyardUnit.Sector)
			{
				factionAI.Faction.HomeSectorPosition = Geometry.RandomXZUnitVector() * Random.Range(300f, 800f) + buildOrder.ShipyardUnit.SectorPosition;
			}
		}

		public void CreateUnit(UnitClass unitClass, Unit spawnDock)
		{
			Sector sector = spawnDock.Sector;
			ModdedUnitSeederSettings moddedUnitSettings = GameController.Instance.GameSettings.ModdedUnitSettings;
			Unit unit = WorldHelper.SpawnUnitAndInstallComponents(unitClass.UnitPrefab, spawnDock.Sector, addCargoLoadout: false);
			if (ModdedUnitSeeder.ShouldModUnitRandomly(unit, moddedUnitSettings))
			{
				ModdedUnitSeeder.ModUnit(unit, moddedUnitSettings);
				ModdedUnitSeeder.AddUnitEquipment(unit, moddedUnitSettings);
			}
			else
			{
				unit.Components.AddDefaultCargoLoadout();
			}
			unit.Sector = sector;
			unit.Faction = factionAI.Faction;
			if (!unit.Components.TryDockInHangar(spawnDock.Components.HangarComponent))
			{
				OnFailedToSpawnAtHangar(spawnDock, unit);
			}
			unit.UpdateGasCloud();
		}

		private static void OnFailedToSpawnAtHangar(Unit spawnDock, Unit unit)
		{
			unit.transform.localPosition = spawnDock.GetSafeUndockSectorPosition(unit);
			unit.transform.localRotation = spawnDock.GetUndockRotation(unit);
		}

		public void UpdateNextBuildOrder()
		{
			if (!IsBuilding && (!factionAI.Faction.AISettings.PreferSingleShip || factionAI.Faction.GetCountOfUnitType(UnitType.Ship) < 1))
			{
				NextBuildOrder = GetNextBuildOrder();
			}
		}

		public FactionAIBuildOrder GetNextBuildOrder()
		{
			if (bestBuildItem.HasValue)
			{
				FactionAIBuildOrder factionAIBuildOrder = FactionAIBuildOrder.Create(EngineASX.Instance);
				FactionAIShipBuildItem value = bestBuildItem.Value;
				if (factionAIBuildOrder.ShipyardUnit == null)
				{
					factionAIBuildOrder.ShipyardUnit = value.BuildLocation;
					factionAIBuildOrder.ShipTrader = value.ShipTrader;
				}
				UnitClass unitClass = (factionAIBuildOrder.UnitClass = value.UnitClass);
				factionAIBuildOrder.RpCost += unitClass.RpCost;
				int num = unitClass.SaleCost;
				if (value.BuildLocation.Faction != factionAI.Faction)
				{
					num = GetCreditsCostOfUnit(value.BuildLocation.Faction, unitClass);
				}
				factionAIBuildOrder.CreditsCost += num;
				return factionAIBuildOrder;
			}
			return null;
		}

		private int GetCreditsCostOfUnit(Faction sellingFaction, UnitClass unitClass)
		{
			if (sellingFaction == null)
			{
				return Mathf.RoundToInt(unitClass.SaleCost);
			}
			return sellingFaction.GetMarkedUpPriceAfterOpinionChange(TradeType.Sell, unitClass.SaleCost, factionAI.Faction);
		}

		public float GetShipPreference(UnitClass unitClass, FactionStrategy desiredStrategy, FactionShipBuildSettings shipBuildSettings, float purposeScoreMultiplier = 1f, float sizeScoreMultiplier = 1f)
		{
			float num = unitClass.GetEffectivenessAtStrategy(desiredStrategy) * shipBuildSettings.CorrectPurposeScore * purposeScoreMultiplier;
			float num2 = unitClass.RelativeShipSaleCost * Random.Range(0.1f, 0.2f);
			if (!Faction.IsFreelancer)
			{
				int num3 = Faction.FleetComposition.NumShips + 1;
				float num4 = (Faction.FleetComposition.TotalShipSize * GameController.Instance.GameSettings.GameplaySettings.NpcFactionSmallShipRatio + num2 * GameController.Instance.GameSettings.GameplaySettings.NpcFactionSmallShipRatio) / (float)num3;
				num -= Mathf.Abs(num4 - factionAI.AISettings.LargeShipPreference) * shipBuildSettings.SizeScore * sizeScoreMultiplier;
			}
			else
			{
				num += num2 * factionAI.AISettings.LargeShipPreference * shipBuildSettings.SizeScore * sizeScoreMultiplier;
			}
			if (unitClass.PurposeStealth)
			{
				num += factionAI.PreferenceToBuildCloakedShips * shipBuildSettings.CloakShipScore;
			}
			return num;
		}

		public bool CanBuildAtDockOwnedBy(Faction dockFaction)
		{
			if (dockFaction != null)
			{
				if (!(dockFaction == factionAI.Faction))
				{
					return CanBuildAtForeignDock(dockFaction);
				}
				return true;
			}
			return false;
		}

		public bool CanBuildAtForeignDock(Faction dockFaction)
		{
			if (!dockFaction.IsHostileToOrAlwaysHostileTo(factionAI.Faction))
			{
				if (!(dockFaction.AISettings == null))
				{
					return dockFaction.AISettings.AllowForeignFactionToUseDocks;
				}
				return true;
			}
			return false;
		}

		private bool ShipTraderSellsValidShip(UnitShipTrader shipTrader)
		{
			foreach (UnitShipTraderItem shipItem in shipTrader.GetShipItems())
			{
				if (factionAI.IsShipUnitClassValidForBuilding(shipItem.UnitClass))
				{
					return true;
				}
			}
			return false;
		}
	}
}
