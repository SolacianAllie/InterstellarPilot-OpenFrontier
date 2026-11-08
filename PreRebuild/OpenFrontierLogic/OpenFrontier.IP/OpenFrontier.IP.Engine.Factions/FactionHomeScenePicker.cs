using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Scenarios;

namespace OpenFrontier.IP.Engine.Factions
{
	public static class FactionHomeScenePicker
	{
		private struct HomeSectorScore
		{
			public float Score { get; set; }

			public Sector Sector { get; set; }
		}

		public static Sector PickFromOwnedUnits(EngineASX engine, Faction faction)
		{
			List<HomeSectorScore> items = GetItems(engine, faction);
			if (items.Count > 0)
			{
				return items.OrderByDescending((HomeSectorScore e) => e.Score).FirstOrDefault().Sector;
			}
			return null;
		}

		private static List<HomeSectorScore> GetItems(EngineASX engine, Faction faction)
		{
			Dictionary<int, HomeSectorScore> dictionary = new Dictionary<int, HomeSectorScore>();
			FactionTypeInfo factionTypeInfo = faction.FindFactionTypeInfo();
			List<Unit> unitsByType = faction.GetUnitsByType(UnitType.Station);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item.Sector != null && IsValidSectorForFaction(item.Sector, factionTypeInfo))
					{
						if (!dictionary.TryGetValue(item.Sector.UniqueId, out var value))
						{
							value = new HomeSectorScore
							{
								Score = GetHomeSectorScore(item.Sector, factionTypeInfo, !faction.IsFreelancer, faction),
								Sector = item.Sector
							};
						}
						value.Score += ScoreStationAsHomeBase(item, faction);
						dictionary[item.Sector.UniqueId] = value;
					}
				}
			}
			if (dictionary.Count == 0)
			{
				List<Unit> unitsByType2 = faction.GetUnitsByType(UnitType.Ship);
				if (unitsByType2 != null)
				{
					foreach (Unit item2 in unitsByType2)
					{
						if (item2.Sector != null && IsValidSectorForFaction(item2.Sector, factionTypeInfo))
						{
							if (!dictionary.TryGetValue(item2.Sector.UniqueId, out var value2))
							{
								value2 = new HomeSectorScore
								{
									Score = GetHomeSectorScore(item2.Sector, factionTypeInfo, !faction.IsFreelancer, faction),
									Sector = item2.Sector
								};
							}
							value2.Score++;
							dictionary[item2.Sector.UniqueId] = value2;
						}
					}
				}
			}
			return dictionary.Values.ToList();
		}

		private static float ScoreStationAsHomeBase(Unit station, Faction faction)
		{
			float num = 1f;
			if (station.UnitClass.StationPurpose == StationPurpose.TradeStation)
			{
				num += 100f;
			}
			if (!station.IsMinorStation())
			{
				num += 5f;
				switch (faction.FactionType)
				{
				case FactionType.Scavenger:
					if (station.UnitClass.StationPurpose == StationPurpose.Scrapyard)
					{
						num += 100f;
					}
					break;
				case FactionType.Miner:
					if (station.UnitClass.StationPurpose == StationPurpose.Refinery)
					{
						num += 100f;
					}
					break;
				case FactionType.Trader:
					if (station.UnitClass.StationPurpose == StationPurpose.Factory)
					{
						num += 50f;
					}
					break;
				case FactionType.Empire:
					if (station.UnitClass.StationPurpose == StationPurpose.Shipyard)
					{
						num += 50f;
					}
					else if (station.UnitClass.StationPurpose == StationPurpose.Equipment)
					{
						num += 25f;
					}
					else if (station.UnitClass.StationPurpose == StationPurpose.Repair)
					{
						num += 25f;
					}
					break;
				}
			}
			return num;
		}

		public static bool IsValidSectorForFaction(Sector sector, FactionTypeInfo factionTypeInfo)
		{
			return true;
		}

		public static float GetHomeSectorScore(Sector sector, FactionTypeInfo factionTypeInfo, bool isMajorFaction, Faction faction = null)
		{
			FactionType factionType = factionTypeInfo.FactionType;
			float sectorPreference = GetSectorPreference(sector, factionTypeInfo);
			sectorPreference += GetScoreBasedOnAdjustedSecurityType(sector, factionTypeInfo) * 50f;
			if (factionTypeInfo.BorderSectorPreference > 0f)
			{
				if (sector.IsSectorAnUncontrolledBorderSector())
				{
					sectorPreference += factionTypeInfo.BorderSectorPreference * 70f;
				}
				if (factionTypeInfo.FactionType != FactionType.Bandit)
				{
					sectorPreference -= (float)GetCountOfNonFreelancerFactionTypeInSector(FactionType.Bandit, sector, faction) * 20f;
				}
			}
			if (isMajorFaction)
			{
				switch (factionType)
				{
				case FactionType.Bandit:
					sectorPreference -= (float)GetCountOfNonFreelancerFactionTypeInSector(factionType, sector, faction) * 40f;
					SectorFinder.FindSectorsWithinJumpDistanceOfSimple(sector, 3, includeUnstableWormholes: false);
					foreach (SectorFinder.SectorResult result in SectorFinder.Results)
					{
						foreach (Faction item in result.Sector.FactionsHeadquartered)
						{
							if (item.FactionType == FactionType.Bandit && item != faction)
							{
								float num = 1f - (float)result.Distance / 3f;
								int countOfFactionTypeInSector = GetCountOfFactionTypeInSector(factionType, result.Sector, faction, (Faction otherFaction) => !otherFaction.IsFreelancer && otherFaction.GetCachedNetWorth() > 1000000);
								sectorPreference -= (float)countOfFactionTypeInSector * 30f * num;
							}
						}
					}
					break;
				case FactionType.Outlaw:
					sectorPreference -= (float)GetCountOfNonFreelancerFactionTypeInSector(factionType, sector, faction) * 10f;
					break;
				case FactionType.Bar:
					sectorPreference -= (float)GetCountOfNonFreelancerFactionTypeInSector(factionType, sector, faction) * 20f;
					break;
				case FactionType.PassengerTransport:
					sectorPreference -= (float)GetCountOfNonFreelancerFactionTypeInSector(factionType, sector, faction) * 5f;
					break;
				case FactionType.Trader:
				case FactionType.Miner:
					sectorPreference -= (float)GetCountOfNonFreelancerFactionTypeInSector(factionType, sector, faction) * 30f;
					break;
				case FactionType.Scavenger:
					sectorPreference -= (float)GetCountOfNonFreelancerFactionTypeInSector(factionType, sector, faction) * 10f;
					break;
				}
			}
			return sectorPreference;
		}

		public static float GetSectorPreference(Sector sector, FactionTypeInfo factionTypeInfo)
		{
			FactionType factionType = factionTypeInfo.FactionType;
			float num = 10f;
			num += GetScoreBasedOnAdjustedSecurityType(sector, factionTypeInfo) * 50f;
			switch (factionType)
			{
			case FactionType.Miner:
				if (sector.HasAsteroidClusters)
				{
					num += 80f;
				}
				break;
			case FactionType.Scavenger:
				if (sector.HasPlanets || sector.HasAsteroidClusters)
				{
					num += 10f;
				}
				break;
			case FactionType.Empire:
				if (sector.HasPlanets)
				{
					num += 20f;
				}
				break;
			case FactionType.Bandit:
				num += sector.FringeSectorRating * 10f;
				if (sector.ControllingFaction != null)
				{
					num -= 10f;
				}
				num -= (float)GetCountOfNonFreelancerFactionTypeInSector(FactionType.Bandit, sector) * 10f;
				break;
			case FactionType.Outlaw:
				if (sector.HasPlanets)
				{
					num -= 7.5f;
				}
				if (sector.HasGasClouds)
				{
					num += 10f;
				}
				break;
			case FactionType.PassengerTransport:
				if (sector.HasPlanets)
				{
					num += 20f;
				}
				break;
			case FactionType.Mercenary:
				if (sector.HasPlanets)
				{
					num += 10f;
				}
				else if (sector.HasAsteroidClusters)
				{
					num += 5f;
				}
				break;
			}
			return num;
		}

		public static int GetCountOfFactionTypeInSector(FactionType factionType, Sector sector, Faction excludeFaction, Func<Faction, bool> predicate)
		{
			int num = 0;
			foreach (Faction item in sector.FactionsHeadquartered)
			{
				if ((!(excludeFaction != null) || !(item == excludeFaction)) && item.FactionType == factionType && predicate(item))
				{
					num++;
				}
			}
			return num;
		}

		public static int GetCountOfNonFreelancerFactionTypeInSector(FactionType factionType, Sector sector, Faction excludeFaction = null)
		{
			int num = 0;
			foreach (Faction item in sector.FactionsHeadquartered)
			{
				if ((!(excludeFaction != null) || !(item == excludeFaction)) && item.FactionType == factionType && !item.IsFreelancer)
				{
					num++;
				}
			}
			return num;
		}

		public static float GetScoreBasedOnSecurityType(Sector sector, FactionTypeInfo factionTypeInfo)
		{
			return factionTypeInfo.SecurityTypePreference switch
			{
				SecurityTypePreference.Secure => sector.SecurityLevel * factionTypeInfo.SecurityTypePreferenceMultiplier, 
				SecurityTypePreference.Insecure => (1f - sector.SecurityLevel) * factionTypeInfo.SecurityTypePreferenceMultiplier, 
				_ => 0f, 
			};
		}

		public static float GetScoreBasedOnAdjustedSecurityType(Sector sector, FactionTypeInfo factionTypeInfo)
		{
			float adjustedSecurityLevel = sector.AdjustedSecurityLevel;
			return factionTypeInfo.SecurityTypePreference switch
			{
				SecurityTypePreference.Secure => adjustedSecurityLevel * factionTypeInfo.SecurityTypePreferenceMultiplier, 
				SecurityTypePreference.Insecure => (0f - adjustedSecurityLevel) * factionTypeInfo.SecurityTypePreferenceMultiplier, 
				_ => 0f, 
			};
		}
	}
}
