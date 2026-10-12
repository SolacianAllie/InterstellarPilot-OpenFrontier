using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding
{
	public static class WorldSeederUnitDiscovery
	{
		public static void SeedFactionStaticIntel(Faction faction, Sector homeSector, FactionIntelSeederSettings intelSeederSettings, float intelMultiplier = 1f, List<Unit> excludeUnits = null)
		{
			HashSet<int> hashSet = null;
			if (excludeUnits != null && excludeUnits.Count > 0)
			{
				hashSet = new HashSet<int>();
				foreach (Unit excludeUnit in excludeUnits)
				{
					if (excludeUnit != null && !hashSet.Contains(excludeUnit.UniqueId))
					{
						hashSet.Add(excludeUnit.UniqueId);
					}
				}
			}
			if (homeSector != null)
			{
				float num = GetSeedBaseStaticIntelValue(faction, intelSeederSettings) * intelMultiplier;
				HashSet<int> hashSet2 = new HashSet<int>(20);
				PriorityQueue<Unit, float> priorityQueue = new PriorityQueue<Unit, float>(20);
				PriorityQueue<WorldSeederUnitDiscoverySectorNode, float> priorityQueue2 = new PriorityQueue<WorldSeederUnitDiscoverySectorNode, float>(10);
				priorityQueue2.Enqueue(new WorldSeederUnitDiscoverySectorNode(homeSector, 0), 0f);
				while (priorityQueue2.Count > 0)
				{
					WorldSeederUnitDiscoverySectorNode value = priorityQueue2.Dequeue().Value;
					hashSet2.Add(value.Sector.UniqueId);
					faction.Intel.DiscoverSector(value.Sector);
					List<Unit> unitsByType = value.Sector.GetUnitsByType(UnitType.Asteroid);
					if (unitsByType != null)
					{
						float probabilityOfDiscoveringAnyAsteroidsInSector = GetProbabilityOfDiscoveringAnyAsteroidsInSector(faction, value.Sector);
						if (Random.value < probabilityOfDiscoveringAnyAsteroidsInSector)
						{
							float probabilityOfDiscoveringAsteroidInSector = GetProbabilityOfDiscoveringAsteroidInSector(faction, value.Sector);
							foreach (Unit item in unitsByType)
							{
								if ((hashSet == null || !hashSet.Contains(item.UniqueId)) && Random.value < probabilityOfDiscoveringAsteroidInSector)
								{
									faction.Intel.DiscoverUnit(item);
								}
							}
						}
					}
					priorityQueue.Clear();
					foreach (SectorNeighbour neighbour in value.Sector.Neighbours)
					{
						if (neighbour.IsStableConnection && (hashSet == null || !hashSet.Contains(neighbour.ConnectingGate.Unit.UniqueId)))
						{
							float priority = CalculateSectorVisitPriority(faction, neighbour.ConnectingGate.ActualTargetSector, intelSeederSettings);
							priorityQueue.Enqueue(neighbour.ConnectingGate.Unit, priority);
						}
					}
					while (priorityQueue.Count > 0 && num > intelSeederSettings.WormholeDiscoveryCost)
					{
						Unit value2 = priorityQueue.Dequeue().Value;
						Wormhole wormholeComponent = value2.WormholeComponent;
						Sector actualTargetSector = wormholeComponent.ActualTargetSector;
						faction.Intel.DiscoverUnit(value2);
						num -= intelSeederSettings.WormholeDiscoveryCost;
						if (value.JumpDistance < intelSeederSettings.MaxDiscoveryDistance && num > intelSeederSettings.WormholeEntryCost)
						{
							faction.Intel.EnterWormhole(wormholeComponent);
							num -= intelSeederSettings.WormholeEntryCost;
							if (!hashSet2.Contains(actualTargetSector.UniqueId))
							{
								float priority2 = CalculateSectorVisitPriority(faction, actualTargetSector, intelSeederSettings);
								priorityQueue2.Enqueue(new WorldSeederUnitDiscoverySectorNode(actualTargetSector, value.JumpDistance + 1), priority2);
							}
						}
					}
				}
			}
			else
			{
				Debug.LogWarning("Faction {0} does not have a home sector. Cannot seed discovery", faction);
			}
		}

		private static float GetProbabilityOfDiscoveringAsteroidInSector(Faction faction, Sector sector)
		{
			float num = 1f;
			if (faction.HomeSector != null)
			{
				int jumpDistanceTo = sector.GetJumpDistanceTo(faction.HomeSector);
				num = Mathf.Lerp(1f, 0.1f, Mathf.Clamp01((float)jumpDistanceTo / 5f));
			}
			if (faction.FactionType == FactionType.Miner)
			{
				return 0.9f * num;
			}
			return 0.2f * num;
		}

		private static float GetProbabilityOfDiscoveringAnyAsteroidsInSector(Faction faction, Sector sector)
		{
			if (faction.FactionType == FactionType.Miner)
			{
				return 1f;
			}
			if (faction.HomeSector != null)
			{
				int jumpDistanceTo = sector.GetJumpDistanceTo(faction.HomeSector);
				if (jumpDistanceTo > -1)
				{
					return Mathf.Lerp(1f, 0.1f, Mathf.Clamp01((float)jumpDistanceTo / 5f));
				}
			}
			return 0.2f;
		}

		private static float CalculateSectorVisitPriority(Faction faction, Sector sector, FactionIntelSeederSettings intelSeederSettings)
		{
			return FactionHomeScenePicker.GetSectorPreference(sector, faction.FactionTypeInfo) + Random.value * intelSeederSettings.MaxStaticIntelSectorRandomness;
		}

		public static void SeedFactionUnitIntel(Faction faction, Sector homeSector, FactionIntelSeederSettings intelSeederSettings, float intelMultiplier = 1f, List<Unit> excludeUnits = null)
		{
			HashSet<int> hashSet = null;
			if (excludeUnits != null && excludeUnits.Count > 0)
			{
				hashSet = new HashSet<int>();
				foreach (Unit excludeUnit in excludeUnits)
				{
					if (!(excludeUnit == null) && !hashSet.Contains(excludeUnit.UniqueId))
					{
						hashSet.Add(excludeUnit.UniqueId);
					}
				}
			}
			if (homeSector != null)
			{
				float num = GetSeedBaseNonStaticIntelValue(faction, intelSeederSettings) * intelMultiplier;
				List<WorldSeederUnitDiscoverySectorNode> list = new List<WorldSeederUnitDiscoverySectorNode>();
				HashSet<int> hashSet2 = new HashSet<int>();
				list.Add(new WorldSeederUnitDiscoverySectorNode(homeSector, 0));
				float f = 0.9f;
				while (list.Count > 0)
				{
					WorldSeederUnitDiscoverySectorNode worldSeederUnitDiscoverySectorNode = list[0];
					hashSet2.Add(worldSeederUnitDiscoverySectorNode.Sector.UniqueId);
					list.RemoveAt(0);
					float num2 = Mathf.Pow(f, worldSeederUnitDiscoverySectorNode.JumpDistance);
					List<Unit> unitsByType = worldSeederUnitDiscoverySectorNode.Sector.GetUnitsByType(UnitType.Station);
					if (unitsByType != null)
					{
						foreach (Unit item in unitsByType)
						{
							if ((hashSet == null || !hashSet.Contains(item.UniqueId)) && Random.value < intelSeederSettings.StationProbability * num * num2 * GetStationDiscoveryProbabilityMultiplier(item))
							{
								faction.Intel.DiscoverUnit(item);
							}
						}
					}
					List<Unit> unitsByType2 = worldSeederUnitDiscoverySectorNode.Sector.GetUnitsByType(UnitType.Cargo);
					if (unitsByType2 != null)
					{
						foreach (Unit item2 in unitsByType2)
						{
							if ((hashSet == null || !hashSet.Contains(item2.UniqueId)) && Random.value < intelSeederSettings.CargoProbability * num * num2 * GetDiscoveryProbabilityBasedOnDistanceFromSectorOrigin(item2))
							{
								faction.Intel.DiscoverUnit(item2);
							}
						}
					}
					foreach (SectorNeighbour neighbour in worldSeederUnitDiscoverySectorNode.Sector.Neighbours)
					{
						if (neighbour.IsStableConnection && !hashSet2.Contains(neighbour.Sector.UniqueId) && (hashSet == null || !hashSet.Contains(neighbour.ConnectingGate.Unit.UniqueId)) && faction.Intel.IsSectorDiscovered(neighbour.Sector) && faction.Intel.IsUnitDiscoveredOrOwned(neighbour.ConnectingGate.Unit) && faction.Intel.IsUnitDiscoveredOrOwned(neighbour.ConnectingGate.TargetGate.Unit))
						{
							list.Add(new WorldSeederUnitDiscoverySectorNode(neighbour.Sector, worldSeederUnitDiscoverySectorNode.JumpDistance + 1));
						}
					}
				}
			}
			else
			{
				Debug.LogWarning("Faction {0} does not have a home sector. Cannot seed discovery", faction);
			}
		}

		private static float GetStationDiscoveryProbabilityMultiplier(Unit station)
		{
			if (station.Faction != null)
			{
				float num = 1f;
				switch (station.Faction.FactionType)
				{
				case FactionType.Bandit:
					num = 0.4f;
					break;
				case FactionType.Outlaw:
					num = 0.6f;
					break;
				}
				switch (station.UnitClass.StationPurpose)
				{
				case StationPurpose.TradeStation:
				case StationPurpose.Bar:
				case StationPurpose.SectorControl:
					return 2f;
				case StationPurpose.Refinery:
				case StationPurpose.Shipyard:
					return 1.5f;
				case StationPurpose.Factory:
				case StationPurpose.Equipment:
				case StationPurpose.Repair:
				case StationPurpose.Outpost:
				case StationPurpose.Scrapyard:
					return 1f * num;
				case StationPurpose.Satellite:
					return 0.06f * num;
				case StationPurpose.Defence:
					return 0.15f * num;
				default:
					return 0.5f * num;
				}
			}
			return GetDiscoveryProbabilityBasedOnDistanceFromSectorOrigin(station);
		}

		private static float GetDiscoveryProbabilityBasedOnDistanceFromSectorOrigin(Unit unit)
		{
			float magnitude = unit.transform.localPosition.magnitude;
			float num = EngineASX.Instance.GameSettings.UniverseBoundsSettings.SectorSizeOverTwo - unit.Sector.GetActualGateDistance();
			float num2 = 0.4f;
			float num3 = 1f - num2;
			float num4 = magnitude / EngineASX.Instance.GameSettings.UniverseBoundsSettings.SectorSizeOverTwo;
			if (num4 > num2)
			{
				return 1f - Mathf.Clamp01((num4 - num2) / num * num3);
			}
			return 1f;
		}

		private static float GetSeedBaseStaticIntelValue(Faction faction, FactionIntelSeederSettings intelSeederSettings)
		{
			float t = 0.5f;
			if (faction.SpawnType != null)
			{
				t = Maths.RandomFloatWithPower(faction.SpawnType.MinStaticIntel, faction.SpawnType.MaxStaticIntel, intelSeederSettings.BaseStaticIntelPower);
			}
			return Mathf.Lerp(intelSeederSettings.MinBaseStaticIntel, intelSeederSettings.MaxBaseStaticIntel, t);
		}

		private static float GetSeedBaseNonStaticIntelValue(Faction faction, FactionIntelSeederSettings intelSeederSettings)
		{
			float t = 0.5f;
			if (faction.SpawnType != null)
			{
				t = Maths.RandomFloatWithPower(faction.SpawnType.MinNonStaticIntel, faction.SpawnType.MaxNonStaticIntel, intelSeederSettings.BaseNonStaticIntelPower);
			}
			return Mathf.Lerp(intelSeederSettings.MinBaseNonStaticIntel, intelSeederSettings.MaxBaseNonStaticIntel, t);
		}
	}
}
