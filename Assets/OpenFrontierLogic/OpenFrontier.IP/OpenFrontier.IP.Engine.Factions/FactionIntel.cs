using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.Factions.Intel;
using OpenFrontier.IP.Engine.Pathfinding;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionIntel : MonoBehaviour
	{
		private class CachedUniversePath
		{
			public UniversePath Path;

			public float CacheTime;
		}

		public struct DiscoveredUnitData
		{
			public double TimeOfDiscovery;

			public Sector Sector;

			public Vector3 SectorPosition;

			public UnitType UnitType;

			public bool IsStatic;

			public Unit Unit;
		}

		private Dictionary<int, double> sectorHostileShipDetectionTimes = new Dictionary<int, double>(8);

		private Dictionary<int, double> sectorHostileStationDetectionTimes = new Dictionary<int, double>(8);

		private HashSet<int> discoveredTraderIds = new HashSet<int>();

		private Faction faction;

		public HashSet<int> DiscoveredSectorIds = new HashSet<int>();

		public HashSet<int> EnteredWormholeIds = new HashSet<int>();

		private Dictionary<ulong, CachedUniversePath> cachedUniversePaths = new Dictionary<ulong, CachedUniversePath>();

		private Dictionary<int, DiscoveredUnitData> discoveredUnits = new Dictionary<int, DiscoveredUnitData>(200);

		private Dictionary<UnitType, HashSet<int>> discoveredUnitsByType = new Dictionary<UnitType, HashSet<int>>();

		private Dictionary<int, HashSet<int>> discoveredUnitsBySector = new Dictionary<int, HashSet<int>>();

		public int DiscoveredScenesCount => DiscoveredSectorIds.Count;

		public IEnumerable<Unit> AllDiscoveredUnits
		{
			get
			{
				foreach (int key in discoveredUnits.Keys)
				{
					Unit unitByid = EngineASX.Instance.GetUnitByid(key);
					if (unitByid != null)
					{
						yield return unitByid;
					}
				}
			}
		}

		public IEnumerable<Unit> AllDiscoveredUnitsExcludingOwned
		{
			get
			{
				foreach (int key in discoveredUnits.Keys)
				{
					Unit unitByid = EngineASX.Instance.GetUnitByid(key);
					if (unitByid != null && unitByid.Faction != faction)
					{
						yield return unitByid;
					}
				}
			}
		}

		public IEnumerable<int> DiscoveredUnitIds => discoveredUnits.Keys;

		public Faction Faction
		{
			get
			{
				return faction;
			}
			set
			{
				faction = value;
			}
		}

		public IEnumerable<int> DiscoveredTraderIds => discoveredTraderIds;

		public void DiscoverEverythingInSector(Sector sector)
		{
			DiscoverAllUnitsOfTypeInSector(sector, UnitType.Ship);
			DiscoverAllUnitsOfTypeInSector(sector, UnitType.Station);
			DiscoverAllUnitsOfTypeInSector(sector, UnitType.Cargo);
			DiscoverAllUnitsOfTypeInSector(sector, UnitType.Asteroid);
			DiscoverAllUnitsOfTypeInSector(sector, UnitType.Wormhole);
		}

		public void DiscoverAllUnitsOfTypeInSector(Sector sector, UnitType unitType)
		{
			List<Unit> unitsByType = sector.GetUnitsByType(unitType);
			if (unitsByType == null)
			{
				return;
			}
			foreach (Unit item in unitsByType)
			{
				if (item.IsValidAndNotDestroyed)
				{
					DiscoverUnit(item);
				}
			}
		}

		public void Clear()
		{
			discoveredTraderIds.Clear();
			DiscoveredSectorIds.Clear();
			EnteredWormholeIds.Clear();
			cachedUniversePaths.Clear();
			discoveredUnits.Clear();
			discoveredUnitsByType.Clear();
			discoveredUnitsBySector.Clear();
		}

		public void DiscoverAllOfFaction(Faction localFaction)
		{
			foreach (Unit unit in localFaction.Units)
			{
				if (unit != null && unit.IsValidAndNotDestroyed && unit.IsDiscoverableType)
				{
					DiscoverUnit(unit);
				}
			}
		}

		internal bool HasDiscoveredAnyWormholes()
		{
			if (discoveredUnitsByType.TryGetValue(UnitType.Wormhole, out var value))
			{
				foreach (int item in value)
				{
					Unit unitByid = EngineASX.Instance.GetUnitByid(item);
					if (unitByid != null && unitByid.IsValidAndNotDestroyed)
					{
						return true;
					}
				}
			}
			return false;
		}

		public HashSet<int> GetDiscoveredUnitIdsInSectorIgnoringTimeOfDiscovery(Sector sector)
		{
			if (discoveredUnitsBySector.TryGetValue(sector.UniqueId, out var value))
			{
				return value;
			}
			return null;
		}

		public void GetDiscoveredUnitIdsInSectorNonAlloc(Sector sector, List<Unit> targetCache, float? maxAgeOfDiscovery = null)
		{
			if (!discoveredUnitsBySector.TryGetValue(sector.UniqueId, out var value))
			{
				return;
			}
			foreach (int item in value)
			{
				DiscoveredUnitData? discoveryData = GetDiscoveryData(item, maxAgeOfDiscovery);
				if (discoveryData.HasValue)
				{
					targetCache.Add(discoveryData.Value.Unit);
				}
			}
		}

		public List<Sector> GetCopyOfDiscoveredSectors()
		{
			List<Sector> list = new List<Sector>();
			foreach (int discoveredSectorId in DiscoveredSectorIds)
			{
				Sector sectorById = EngineASX.Instance.GetSectorById(discoveredSectorId);
				if (sectorById != null)
				{
					list.Add(sectorById);
				}
			}
			return list;
		}

		public void Init(Faction faction)
		{
			Faction = faction;
		}

		public void DiscoverAllOccupiedScenes()
		{
			foreach (Unit unit in faction.Units)
			{
				if (unit != null && unit.Sector != null)
				{
					DiscoverSector(unit.Sector);
				}
			}
		}

		public double? GetScenarioTimeOfDiscovery(Unit unit)
		{
			if (discoveredUnits.TryGetValue(unit.UniqueId, out var value))
			{
				return value.TimeOfDiscovery;
			}
			return null;
		}

		public DiscoveredUnitData? GetDiscoveryData(Unit unit, float? maxAgeOfDiscoveredUnit = null)
		{
			return GetDiscoveryData(unit.UniqueId, maxAgeOfDiscoveredUnit);
		}

		public DiscoveredUnitData? GetDiscoveryData(int unitId, float? maxAgeOfDiscoveredUnit = null)
		{
			if (discoveredUnits.TryGetValue(unitId, out var value))
			{
				if (value.Unit == null)
				{
					discoveredUnits.Remove(unitId);
					return null;
				}
				if (value.IsStatic)
				{
					return value;
				}
				if (value.Unit != null && value.Unit.IsOwnedByFactionOrAlliedTo(faction))
				{
					return value;
				}
				if (!maxAgeOfDiscoveredUnit.HasValue)
				{
					maxAgeOfDiscoveredUnit = EngineASX.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTime;
				}
				if (EngineASX.Instance.ScenarioElapsedTime - value.TimeOfDiscovery < (double)maxAgeOfDiscoveredUnit.Value)
				{
					return value;
				}
			}
			return null;
		}

		public bool HasWormholeBeenEntered(Wormhole wormhole)
		{
			return EnteredWormholeIds.Contains(wormhole.Unit.UniqueId);
		}

		public bool IsSectorDiscovered(Sector sector)
		{
			return DiscoveredSectorIds.Contains(sector.UniqueId);
		}

		public bool DiscoverSector(Sector sector)
		{
			if (!DiscoveredSectorIds.Contains(sector.UniqueId))
			{
				DiscoveredSectorIds.Add(sector.UniqueId);
				return true;
			}
			return false;
		}

		public bool HasUndiscoveredUnitsInScene(Sector sector)
		{
			return GetUndiscoveredUnitsInScene(sector).Count() > 0;
		}

		public bool HasDiscoveredUnitsInSceneWhichOtherFactionHasnt(Sector sector, Faction otherFaction)
		{
			return GetDiscoveredUnitsInSceneWhichOtherFactionHasnt(sector, otherFaction).Count > 0;
		}

		public List<Unit> GetDiscoveredUnitsInSceneWhichOtherFactionHasnt(Sector sector, Faction otherFaction)
		{
			List<Unit> list = new List<Unit>();
			GetDiscoveredUnitsInSceneWhichOtherFactionHasntNonAlloc(sector, otherFaction, list);
			return list;
		}

		public List<Unit> GetDiscoveredUnitsInSceneWhichOtherFactionHasntNonAlloc(Sector sector, Faction otherFaction, List<Unit> list)
		{
			foreach (Unit item in otherFaction.Intel.GetUndiscoveredUnitsInScene(sector))
			{
				if (IsUnitDiscoveredOrOwned(item))
				{
					list.Add(item);
				}
			}
			return list;
		}

		public bool CanTraverseIntoNeighbour(SectorNeighbour neighbour, bool considerExcluded = true)
		{
			if (considerExcluded && faction.AutopilotExcludedSectors.Contains(neighbour.Sector.UniqueId))
			{
				return false;
			}
			if (HasWormholeBeenEntered(neighbour.ConnectingGate))
			{
				if (faction.FactionAI != null && !faction.FactionAI.CanTraverseIntoSector(neighbour.Sector))
				{
					return false;
				}
				return true;
			}
			return false;
		}

		public IEnumerable<Unit> GetUndiscoveredUnitsInScene(Sector sector)
		{
			UnitType[] unitTypes = EngineASX.Instance.unitTypes;
			foreach (UnitType unitType in unitTypes)
			{
				if (!Unit.UnitTypeIsDiscoverable(unitType))
				{
					continue;
				}
				List<Unit> unitsByType = sector.GetUnitsByType(unitType);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					if (item.IsValidAndNotDestroyed && !IsUnitDiscoveredOrOwned(item))
					{
						yield return item;
					}
				}
			}
		}

		public void DiscoverUnitsInSector(Sector sector)
		{
			UnitType[] unitTypes = EngineASX.Instance.unitTypes;
			foreach (UnitType unitType in unitTypes)
			{
				if (!Unit.UnitTypeIsDiscoverable(unitType))
				{
					continue;
				}
				List<Unit> unitsByType = sector.GetUnitsByType(unitType);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					if (!item.IsValidAndNotDestroyed)
					{
						DiscoverUnit(item);
					}
				}
			}
		}

		public void EnterWormhole(Wormhole wormhole)
		{
			EnterWormholeInternal(wormhole);
			if (wormhole.ActualTargetSector != null)
			{
				DiscoverSector(wormhole.ActualTargetSector);
				ClearCachedPathsBetweenSectorsTwoWay(wormhole.Sector, wormhole.ActualTargetSector);
			}
			if (wormhole.TargetGate != null)
			{
				DiscoverUnit(wormhole.TargetGate.Unit);
				if (!wormhole.IsUnstable)
				{
					EnterWormholeInternal(wormhole.TargetGate);
				}
			}
			DiscoverUnit(wormhole.Unit);
		}

		private void EnterWormholeInternal(Wormhole wormhole)
		{
			if (!EnteredWormholeIds.Contains(wormhole.Unit.UniqueId))
			{
				EnteredWormholeIds.Add(wormhole.Unit.UniqueId);
			}
		}

		public void OverrideTimeOfDiscovery(Unit unit, double timeOfDiscovery)
		{
			DiscoveredUnitData? discoveryData = GetDiscoveryData(unit, float.MaxValue);
			if (discoveryData.HasValue)
			{
				DiscoveredUnitData value = discoveryData.Value;
				value.TimeOfDiscovery = timeOfDiscovery;
				discoveredUnits[unit.UniqueId] = value;
			}
		}

		public void ChangeTimeOfDiscovery(Unit unit, double delta)
		{
			DiscoveredUnitData? discoveryData = GetDiscoveryData(unit, float.MaxValue);
			if (discoveryData.HasValue)
			{
				DiscoveredUnitData value = discoveryData.Value;
				value.TimeOfDiscovery += delta;
				discoveredUnits[unit.UniqueId] = value;
			}
		}

		public DiscoverUnitResult DiscoverUnit(Unit unit, bool setTimeOfDiscovery = true)
		{
			if (unit != null && unit.Sector != null)
			{
				DiscoverSector(unit.Sector);
				bool flag = false;
				UnitType unitType = unit.UnitType;
				if ((uint)(unitType - 1) <= 1u)
				{
					flag = unit.IsHostileToOrAlwaysHostileToTwoWay(faction);
					if (flag)
					{
						UpdateSectorWithHostileTarget(unit);
					}
				}
				if (!discoveredUnits.TryGetValue(unit.UniqueId, out var value))
				{
					double timeOfDiscovery = (setTimeOfDiscovery ? EngineASX.Instance.ScenarioElapsedTime : double.MinValue);
					discoveredUnits.Add(unit.UniqueId, new DiscoveredUnitData
					{
						TimeOfDiscovery = timeOfDiscovery,
						SectorPosition = unit.SectorPosition,
						Sector = unit.Sector,
						UnitType = unit.UnitType,
						IsStatic = IsStaticOrTreatAsStatic(unit),
						Unit = unit
					});
					if (!discoveredUnitsByType.TryGetValue(unit.UnitType, out var value2))
					{
						value2 = new HashSet<int>(GetDiscoveredUnitsByTypeListCapacity(unit.UnitType));
						discoveredUnitsByType.Add(unit.UnitType, value2);
					}
					value2.Add(unit.UniqueId);
					AddToDiscoveredUnitsBySector(unit);
					if (unit.Faction != null && unit.Faction != Faction)
					{
						Faction.CreateAttitudeIfNone(unit.Faction);
					}
					if (flag && GameController.Instance.GameSettings.DebugSettings.FactionHeatmapEnabled && faction.Heatmap != null)
					{
						faction.Heatmap.UpdateHeatmapWithHostileUnit(unit);
					}
					switch (unit.UnitType)
					{
					case UnitType.Ship:
					case UnitType.Station:
						if (unit.Components != null && unit.Components.CargoTrader != null && !discoveredTraderIds.Contains(unit.UniqueId))
						{
							discoveredTraderIds.Add(unit.UniqueId);
						}
						break;
					case UnitType.Wormhole:
						if (unit.WormholeComponent.TargetGate != null && unit.WormholeComponent.TargetGate.TargetGate == unit.WormholeComponent && IsUnitDiscovered(unit.WormholeComponent.TargetGate.Unit))
						{
							EnterWormholeInternal(unit.WormholeComponent);
							EnterWormholeInternal(unit.WormholeComponent.TargetGate);
							ClearCachedPathsBetweenSectorsTwoWay(unit.Sector, unit.WormholeComponent.TargetGate.Unit.Sector);
						}
						break;
					}
					return DiscoverUnitResult.New;
				}
				double timeOfDiscovery2 = value.TimeOfDiscovery;
				if (!value.IsStatic & setTimeOfDiscovery)
				{
					value.TimeOfDiscovery = EngineASX.Instance.ScenarioElapsedTime;
				}
				bool flag2 = value.Sector != unit.Sector;
				if (flag2)
				{
					if (value.Sector != null)
					{
						RemoveFromDiscoveredUnitsBySector(unit.UniqueId, value.Sector);
					}
					if (unit.Sector != null)
					{
						AddToDiscoveredUnitsBySector(unit);
					}
					value.Sector = unit.Sector;
				}
				value.SectorPosition = unit.SectorPosition;
				discoveredUnits[unit.UniqueId] = value;
				if (flag && GameController.Instance.GameSettings.DebugSettings.FactionHeatmapEnabled && faction.Heatmap != null && unit.Faction != null && unit.Sector != null && (flag2 || EngineASX.Instance.ScenarioElapsedTime - timeOfDiscovery2 > (double)EngineASX.Instance.GameSettings.HeatmapSettings.HostileTargetStaleTime))
				{
					faction.Heatmap.UpdateHeatmapWithHostileUnit(unit);
				}
				if (!flag2 && !(EngineASX.Instance.ScenarioElapsedTime - timeOfDiscovery2 > 20.0))
				{
					return DiscoverUnitResult.Current;
				}
				return DiscoverUnitResult.Updated;
			}
			return DiscoverUnitResult.Ignored;
		}

		private int GetDiscoveredUnitsByTypeListCapacity(UnitType unitType)
		{
			return unitType switch
			{
				UnitType.Ship => 256, 
				UnitType.Station => 128, 
				UnitType.Asteroid => 128, 
				UnitType.Cargo => 32, 
				_ => 16, 
			};
		}

		private void UpdateSectorWithHostileTarget(Unit unit)
		{
			switch (unit.UnitType)
			{
			case UnitType.Station:
				sectorHostileStationDetectionTimes[unit.Sector.UniqueId] = EngineASX.Instance.ScenarioElapsedTime;
				break;
			case UnitType.Ship:
				sectorHostileShipDetectionTimes[unit.Sector.UniqueId] = EngineASX.Instance.ScenarioElapsedTime;
				break;
			}
		}

		public bool SectorHasRecentHostiles(Sector sector, double maxAgeInSeconds = 30.0)
		{
			if (sector == null)
			{
				return false;
			}
			if (sectorHostileStationDetectionTimes.TryGetValue(sector.UniqueId, out var value) && EngineASX.Instance.ScenarioElapsedTime - value < maxAgeInSeconds)
			{
				return true;
			}
			if (sectorHostileShipDetectionTimes.TryGetValue(sector.UniqueId, out var value2) && EngineASX.Instance.ScenarioElapsedTime - value2 < maxAgeInSeconds)
			{
				return true;
			}
			return false;
		}

		public bool SectorHasRecentHostileShips(Sector sector, double maxAgeInSeconds = 30.0)
		{
			if (sector == null)
			{
				return false;
			}
			if (sectorHostileShipDetectionTimes.TryGetValue(sector.UniqueId, out var value) && EngineASX.Instance.ScenarioElapsedTime - value < maxAgeInSeconds)
			{
				return true;
			}
			return false;
		}

		public bool SectorHasRecentHostileStations(Sector sector, double maxAgeInSeconds = 30.0)
		{
			if (sector == null)
			{
				return false;
			}
			if (sectorHostileStationDetectionTimes.TryGetValue(sector.UniqueId, out var value) && EngineASX.Instance.ScenarioElapsedTime - value < maxAgeInSeconds)
			{
				return true;
			}
			return false;
		}

		public static bool IsStaticOrTreatAsStatic(Unit unit)
		{
			if (!unit.IsStatic)
			{
				return unit.UnitType == UnitType.Cargo;
			}
			return true;
		}

		public void RemoveWormholeEntry(Wormhole wormhole)
		{
			EnteredWormholeIds.Remove(wormhole.Unit.UniqueId);
		}

		public void RemoveUnit(Unit unit)
		{
			if (unit != null)
			{
				RemoveUnit(unit.UniqueId);
			}
		}

		public void RemoveUnit(int unitId)
		{
			if (discoveredUnits.TryGetValue(unitId, out var value))
			{
				if (value.Sector != null)
				{
					RemoveFromDiscoveredUnitsBySector(unitId, value.Sector);
				}
				if (!discoveredUnitsByType.TryGetValue(value.UnitType, out var value2))
				{
					value2.Remove(unitId);
				}
				discoveredUnits.Remove(unitId);
				discoveredTraderIds.Remove(unitId);
			}
		}

		private void AddToDiscoveredUnitsBySector(Unit unit)
		{
			if (!discoveredUnitsBySector.TryGetValue(unit.Sector.UniqueId, out var value))
			{
				value = new HashSet<int>(64);
				discoveredUnitsBySector.Add(unit.Sector.UniqueId, value);
			}
			if (!value.Contains(unit.UniqueId))
			{
				value.Add(unit.UniqueId);
			}
		}

		private void RemoveFromDiscoveredUnitsBySector(int uniqueId, Sector sector)
		{
			if (discoveredUnitsBySector.TryGetValue(sector.UniqueId, out var value))
			{
				value.Remove(uniqueId);
			}
		}

		[ContextMenu("Discover all Sectors")]
		public void DiscoverAllSectors()
		{
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				DiscoverSector(sector);
			}
		}

		[ContextMenu("Discover everything")]
		public void DiscoverEverything()
		{
			DiscoverAllSectors();
			DiscoverAllUnits(staticOnly: false);
			EnterAllWormholes(includeUnstable: true);
		}

		public IEnumerable<int> GetAllDiscoveredUnitIdsOfType(UnitType unitType)
		{
			if (discoveredUnitsByType.TryGetValue(unitType, out var value))
			{
				return value;
			}
			return null;
		}

		public int GetCountOfDiscoveredUnitsByType(UnitType unitType)
		{
			if (discoveredUnitsByType.TryGetValue(unitType, out var value))
			{
				return value.Count;
			}
			return 0;
		}

		[ContextMenu("Discover all gates")]
		public void DiscoverAllWormholes()
		{
			EngineASX.Instance.EnumerateUnits((Unit unit) =>
			{
				if (unit != null && unit.IsDiscoverableType && unit.UnitType == UnitType.Wormhole)
				{
					DiscoverUnit(unit);
				}
			});
		}

		[ContextMenu("Discover all stable wormholes")]
		public void DiscoverAllNormalWormholes()
		{
			EngineASX.Instance.EnumerateUnits((Unit unit) =>
			{
				if (unit != null && unit.IsDiscoverableType && unit.UnitType == UnitType.Wormhole && !unit.WormholeComponent.IsUnstable)
				{
					DiscoverUnit(unit);
				}
			});
		}

		public void DiscoverAllUnits(bool staticOnly)
		{
			EngineASX.Instance.EnumerateUnits((Unit unit) =>
			{
				if (unit != null && unit.IsDiscoverableType && (!staticOnly || unit.IsStatic))
				{
					DiscoverUnit(unit);
				}
			});
		}

		public void EnterAllWormholes(bool includeUnstable)
		{
			EngineASX.Instance.EnumerateUnits((Unit unit) =>
			{
				if (unit != null && unit.WormholeComponent != null && (includeUnstable || !unit.WormholeComponent.IsUnstable))
				{
					EnterWormhole(unit.WormholeComponent);
				}
			});
		}

		public void DiscoverStationsInSector(Sector sector)
		{
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
			if (unitsByType == null)
			{
				return;
			}
			foreach (Unit item in unitsByType)
			{
				if (item.IsDiscoverableType)
				{
					DiscoverUnit(item);
				}
			}
		}

		public void DiscoverAsteroidsInSector(Sector sector)
		{
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Asteroid);
			if (unitsByType == null)
			{
				return;
			}
			foreach (Unit item in unitsByType)
			{
				if (item != null && item.IsDiscoverableType)
				{
					DiscoverUnit(item);
				}
			}
		}

		public bool IsUnitDiscoveredOrOwned(Unit unit, float? maxAgeOfDiscoveredUnit = null)
		{
			if (!(unit.Faction == faction))
			{
				return IsUnitDiscovered(unit.UniqueId, maxAgeOfDiscoveredUnit);
			}
			return true;
		}

		public bool IsUnitDiscoveredUsingDefaultDiscoveryAge(Unit unit)
		{
			float value = (unit.IsInActiveSector ? GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTimeActiveSector : GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTime);
			return IsUnitDiscovered(unit, value);
		}

		public bool IsUnitDiscovered(Unit unit, float? maxAgeOfDiscoveredUnit = null)
		{
			return IsUnitDiscovered(unit.UniqueId, maxAgeOfDiscoveredUnit);
		}

		public bool IsUnitDiscovered(int unitId, float? maxAgeOfDiscoveredUnit = null)
		{
			return GetDiscoveryData(unitId, maxAgeOfDiscoveredUnit).HasValue;
		}

		public WorldNavpoint? GetLastKnownPosition(Unit unit, float maxAge)
		{
			if (unit == null)
			{
				return null;
			}
			DiscoveredUnitData? discoveryData = GetDiscoveryData(unit, float.MaxValue);
			if (!discoveryData.HasValue)
			{
				return null;
			}
			DiscoveredUnitData value = discoveryData.Value;
			if (value.IsStatic || value.Unit.IsOwnedByFactionOrAlliedTo(faction))
			{
				return WorldNavpoint.FromUnit(unit);
			}
			double num = EngineASX.Instance.ScenarioElapsedTime - value.TimeOfDiscovery;
			Wormhole recentEnteredWormhole = EngineASX.Instance.RecentWormholeEntries.GetRecentEnteredWormhole(unit, (float)num);
			if (recentEnteredWormhole != null)
			{
				if (EnteredWormholeIds.Contains(recentEnteredWormhole.Unit.UniqueId))
				{
					return WorldNavpoint.FromSectorPosition(recentEnteredWormhole.ActualTargetSector, recentEnteredWormhole.GetTargetSectorPosition());
				}
				WorldNavpoint value2 = WorldNavpoint.FromUnit(recentEnteredWormhole.Unit);
				value2.TargetType = WorldNavpointTargetType.Gate;
				return value2;
			}
			if (num < (double)GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTime)
			{
				return WorldNavpoint.FromUnit(unit);
			}
			if (num > (double)maxAge)
			{
				return null;
			}
			return WorldNavpoint.FromSectorPosition(value.Sector, value.SectorPosition);
		}

		public void TrimInvalidDiscoveredUnits()
		{
			KeyValuePair<int, DiscoveredUnitData>[] array = discoveredUnits.ToArray();
			discoveredUnits.Clear();
			for (int i = 0; i < array.Length; i++)
			{
				DiscoveredUnitData value = array[i].Value;
				Unit unitByid = EngineASX.Instance.GetUnitByid(array[i].Key);
				if (!(unitByid == null) && (!(unitByid.Faction != faction) || IsStaticOrTreatAsStatic(unitByid) || !(EngineASX.Instance.ScenarioElapsedTime - value.TimeOfDiscovery > (double)GameController.Instance.GameSettings.IntelSettings.NonStaticMaxTimeBeforeDisposal)))
				{
					discoveredUnits.Add(unitByid.UniqueId, array[i].Value);
				}
			}
		}

		public void PerformScan(Sector sector, Vector3 sectorPosition, float range, Unit scanner)
		{
			if (GameController.Instance.GameSettings.DebugSettings.ScanningEnabled)
			{
				int num = Physics.OverlapSphereNonAlloc(sector.ToWorldPosition(sectorPosition), GameController.Instance.GameSettings.IntelSettings.MaxScanRange + range, EngineASX.ColliderCache, GameController.Instance.AIScanMask, QueryTriggerInteraction.Collide);
				for (int i = 0; i < num; i++)
				{
					Unit component = EngineASX.ColliderCache[i].GetComponent<Unit>();
					faction.IntelProcessor.QueueUnitForProcessing(component, scanner);
				}
			}
		}

		public void PerformScanImmediate(Sector sector, Vector3 sectorPosition, float range, Unit scanner, bool callOnNewUnitScanned = true)
		{
			if (GameController.Instance.GameSettings.DebugSettings.ScanningEnabled)
			{
				int num = Physics.OverlapSphereNonAlloc(sector.ToWorldPosition(sectorPosition), GameController.Instance.GameSettings.IntelSettings.MaxScanRange + range, EngineASX.ColliderCache, GameController.Instance.AIScanMask, QueryTriggerInteraction.Collide);
				for (int i = 0; i < num; i++)
				{
					Unit component = EngineASX.ColliderCache[i].GetComponent<Unit>();
					Vector3 p = component.SectorPosition;
					float distanceIgnoreY = Maths.GetDistanceIgnoreY(ref p, ref sectorPosition);
					faction.IntelProcessor.ProcessScannedItem(component, scanner, distanceIgnoreY, callOnNewUnitScanned);
				}
			}
		}

		public bool IsUnitDetectable(Unit unit)
		{
			if (!unit.IsDiscoverableType)
			{
				return false;
			}
			if (unit.IsOwnedByFactionOrAlliedTo(faction))
			{
				return true;
			}
			if (unit.Faction != null && unit.Faction.IsIgnoredByAI)
			{
				return false;
			}
			if (!unit.IsFullyCloaked)
			{
				return GameController.Instance.GameSettings.GameplaySettings.CanDetectCloakedUnits;
			}
			return Time.time > unit.LastTimeEnteredWormhole + GameController.Instance.GameSettings.GameplaySettings.WormholeEntryScanCooldownTime;
		}

		public int GetMinSectorJumpCount(Sector sector1, Sector sector2)
		{
			return GetUniversePath(sector1, sector2)?.Jumps ?? (-1);
		}

		public void ClearCachedUniversePaths()
		{
			cachedUniversePaths.Clear();
		}

		public void ClearCachedPathsBetweenSectorsTwoWay(Sector s1, Sector s2)
		{
			ClearCachedPathsBetweenSectors(s1, s2);
			ClearCachedPathsBetweenSectors(s2, s1);
		}

		private void ClearCachedPathsBetweenSectors(Sector s1, Sector s2)
		{
			ulong key = Helper.OrderedPairId(s1.UniqueId, s2.UniqueId);
			cachedUniversePaths.Remove(key);
		}

		public UniversePath GetUniversePath(Sector s1, Sector s2)
		{
			if (s1 == null || s2 == null)
			{
				return null;
			}
			ulong key = Helper.OrderedPairId(s1.UniqueId, s2.UniqueId);
			if (cachedUniversePaths.TryGetValue(key, out var value) && Time.time - value.CacheTime < 240f)
			{
				return value.Path;
			}
			UniversePath universePath = IntelUniversePathCalculator.Calculate(this, s1, s2);
			CachedUniversePath value2 = new CachedUniversePath
			{
				Path = universePath,
				CacheTime = Time.time
			};
			cachedUniversePaths[key] = value2;
			return universePath;
		}

		public bool HasPathFromToSector(Sector s1, Sector s2)
		{
			if (cachedUniversePaths.TryGetValue(Helper.OrderedPairId(s1.UniqueId, s2.UniqueId), out var value))
			{
				return value != null;
			}
			return false;
		}

		public bool AreSectorsConnected(Sector s1, Sector s2)
		{
			return GetMinSectorJumpCount(s1, s2) >= 0;
		}

		public void InvalidateCachedPaths()
		{
			cachedUniversePaths.Clear();
		}

		public bool TrySharePathWithFaction(Sector s1, Sector s2, Faction otherFaction)
		{
			UniversePath universePath = GetUniversePath(s1, s2);
			if (universePath != null)
			{
				foreach (UniversePathNode node in universePath.Nodes)
				{
					otherFaction.Intel.DiscoverSector(node.Sector);
					if (node.Wormhole != null)
					{
						otherFaction.Intel.EnterWormhole(node.Wormhole);
					}
				}
			}
			return universePath != null;
		}
	}
}
