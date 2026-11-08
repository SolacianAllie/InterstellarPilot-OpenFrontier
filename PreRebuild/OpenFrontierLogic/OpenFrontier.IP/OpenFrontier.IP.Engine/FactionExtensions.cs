using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.MissionSpecs;
using OpenFrontier.IP.Testing;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public static class FactionExtensions
	{
		private static List<Faction> tempFactionCache = new List<Faction>();

		public static bool IsHostileToAll(this Faction faction)
		{
			if (faction.FactionAI != null)
			{
				return faction.FactionAI.AISettings.HostileWithAll;
			}
			return false;
		}

		public static void MakePeaceWithAllEnemiesOneWay(this Faction faction)
		{
			tempFactionCache.Clear();
			foreach (FactionAttitude relation in faction.Relations)
			{
				if (relation.TargetFaction != null && relation.Neutrality == Neutrality.Hostile)
				{
					tempFactionCache.Add(relation.TargetFaction);
				}
			}
			foreach (Faction item in tempFactionCache)
			{
				faction.MakePeace(item);
			}
		}

		public static int GetCountOfStationPurpose(this Faction faction, StationPurpose stationPurpose)
		{
			int num = 0;
			List<Unit> unitsByType = faction.GetUnitsByType(UnitType.Station);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item != null && item.IsValidAndNotDestroyed && item.UnitClass.StationPurpose == stationPurpose)
					{
						num++;
					}
				}
			}
			return num;
		}

		public static Faction GetFirstAlliedFaction(this Faction faction)
		{
			foreach (FactionAttitude relation in faction.Relations)
			{
				if (relation.TargetFaction != null && relation.Neutrality == Neutrality.Allied)
				{
					return relation.TargetFaction;
				}
			}
			return null;
		}

		public static void LoseOwnershipOfAllCargo(this Faction faction)
		{
			faction.LoseOwnershipOfAllUnitsOfType(UnitType.Cargo);
		}

		public static void LoseOwnershipOfAllUnitsOfType(this Faction faction, UnitType unitType)
		{
			List<Unit> unitsByType = faction.GetUnitsByType(unitType);
			if (unitsByType == null)
			{
				return;
			}
			Unit[] array = unitsByType.ToArray();
			foreach (Unit unit in array)
			{
				if (unit != null && unit.IsValidAndNotDestroyed)
				{
					unit.Faction = null;
				}
			}
		}

		public static void DiscoverOwnUnits(this Faction faction)
		{
			foreach (Unit unit in faction.Units)
			{
				if (unit != null && unit.IsDiscoverableType)
				{
					faction.Intel.DiscoverUnit(unit);
				}
			}
		}

		public static double GetTimeSinceSpawn(this Faction faction)
		{
			return faction.Engine.ScenarioElapsedTime - faction.SpawnTime;
		}

		public static string GetPowerLevelDescription(this Faction faction)
		{
			GameSettings gameSettings = faction.Engine.GameSettings;
			if (gameSettings.PowerLevels != null && gameSettings.PowerLevels.Levels != null && gameSettings.PowerLevels.Levels.Length != 0)
			{
				return GetWealthDescription(gameSettings.PowerLevels, faction.GetCachedNetWorth());
			}
			return null;
		}

		public static string GetWealthLevelDescription(this Faction faction)
		{
			GameSettings gameSettings = faction.Engine.GameSettings;
			if (gameSettings.WealthLevels != null && gameSettings.WealthLevels.Levels != null && gameSettings.WealthLevels.Levels.Length != 0)
			{
				return GetWealthDescription(gameSettings.WealthLevels, faction.Credits);
			}
			return null;
		}

		private static string GetWealthDescription(WealthLevels levels, long credits)
		{
			int i;
			for (i = 0; credits > levels.Levels[i].CreditLimit && i < levels.Levels.Length - 1; i++)
			{
			}
			return levels.Levels[i].Description;
		}

		public static void UndockAllUnits(this Faction faction)
		{
			UndockUnits(faction, UnitType.Ship);
			UndockUnits(faction, UnitType.Station);
		}

		private static void UndockUnits(Faction faction, UnitType unitType)
		{
			List<Unit> unitsByType = faction.GetUnitsByType(unitType);
			if (unitsByType == null)
			{
				return;
			}
			foreach (Unit item in unitsByType)
			{
				if (item != null && item.Components != null)
				{
					item.Components.UndockAllDockedUnits();
				}
			}
		}

		public static void DestroyInvalidMissionSpecs(this Faction faction)
		{
			for (int i = 0; i < faction.MissionSpecs.Count; i++)
			{
				MissionSpec missionSpec = faction.MissionSpecs[i];
				if (!missionSpec.IsValid())
				{
					missionSpec.SafeDestroy();
				}
			}
		}

		public static int GetStationCountExcludingDefence(this Faction faction)
		{
			List<Unit> unitsByType = faction.GetUnitsByType(UnitType.Station);
			int num = 0;
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item != null && item.IsValidAndNotDestroyed && item.UnitClass.StationPurpose != StationPurpose.Defence)
					{
						num++;
					}
				}
			}
			return num;
		}

		public static void CopyPersonalityTo(this Faction faction, Faction otherFaction)
		{
			otherFaction.Aggression = faction.Aggression;
			otherFaction.Greed = faction.Greed;
			otherFaction.Virtue = faction.Virtue;
			otherFaction.Cooperation = faction.Cooperation;
		}

		public static bool IsHostileTo(this Faction faction, Fleet fleet)
		{
			if (fleet != null)
			{
				return faction.IsHostileTo(fleet.Faction);
			}
			return false;
		}

		public static int GetCountOfDockedShips(this Faction faction)
		{
			int num = 0;
			List<Unit> unitsByType = faction.GetUnitsByType(UnitType.Ship);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item != null && item.IsDocked)
					{
						num++;
					}
				}
			}
			return num;
		}

		public static string GetDescriptiveFactionName(this Faction faction, bool shortName)
		{
			if (!faction.IsFreelancer)
			{
				if (!shortName)
				{
					return faction.GetLongNameElseShort();
				}
				return faction.GetShortNameElseLong();
			}
			if (faction.FactionType == FactionType.Outlaw)
			{
				return "Outlaw";
			}
			if (faction.FactionType == FactionType.Bandit)
			{
				return "Bandit";
			}
			return "Freelance " + Faction.GetFactionTypeDescription(faction);
		}

		public static string GetDescriptiveFactionNameIncludingPilotName(this Faction faction, bool shortName)
		{
			string text = (shortName ? faction.GetShortNameElseLong() : faction.GetLongNameElseShort());
			if (!faction.IsFreelancer)
			{
				return faction.GetFriendlyName(shortName);
			}
			switch (faction.FactionType)
			{
			case FactionType.Outlaw:
				return text + " (Outlaw)";
			case FactionType.Bandit:
				return text + " (Bandit)";
			case FactionType.Mercenary:
				return text + " (Mercenary)";
			case FactionType.EquipmentDealer:
				return text + " (Equip. Dealer)";
			default:
				if (shortName)
				{
					return text + " (" + Faction.GetFactionTypeDescription(faction) + ")";
				}
				return text + " (Freelance " + Faction.GetFactionTypeDescription(faction) + ")";
			}
		}

		public static string GetHomeSectorNameForPlayer(this Faction faction)
		{
			if (faction.HomeSector == null)
			{
				return "-";
			}
			if (faction.HomeSector.IsDiscoveredByLocalFaction())
			{
				return faction.HomeSector.Name;
			}
			return "Unknown sector";
		}

		public static int GetMaxJumpDistanceFromHomeSector(this Faction faction)
		{
			if (faction.AISettings != null && faction.AISettings.MaxJumpDistanceFromHomeSector >= 0)
			{
				return faction.AISettings.MaxJumpDistanceFromHomeSector;
			}
			return 99;
		}

		public static bool IsBanditOrOutlaw(this Faction faction)
		{
			FactionType factionType = faction.FactionType;
			if (factionType == FactionType.Bandit || factionType == FactionType.Outlaw)
			{
				return true;
			}
			return false;
		}

		public static Color GetFactionHostilityColor(this Faction faction1, Faction faction2)
		{
			return EngineASX.Instance.GetFactionHostilityColor(faction1, faction2);
		}

		public static bool IsCivilianOrVirtuous(this Faction faction)
		{
			if (!faction.IsCivilianFromFactionType)
			{
				return faction.Virtue >= 0f;
			}
			return true;
		}

		public static bool IsNormallyOpposedTo(this Faction faction, Faction otherFaction)
		{
			if (faction.IsBanditOrOutlaw() && otherFaction.IsCivilianOrVirtuous())
			{
				return true;
			}
			if (otherFaction.IsBanditOrOutlaw() && faction.IsCivilianOrVirtuous())
			{
				return true;
			}
			if (faction.Virtue >= 0.7f)
			{
				return otherFaction.Virtue <= 0.3f;
			}
			if (otherFaction.Virtue >= 0.7f)
			{
				return faction.Virtue <= 0.3f;
			}
			return false;
		}

		public static void RepairAllUnits(this Faction faction)
		{
			List<Unit> unitsByType = faction.GetUnitsByType(UnitType.Ship);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					DamageTestHelper.FullyRestoreUnitHealth(item);
				}
			}
			List<Unit> unitsByType2 = faction.GetUnitsByType(UnitType.Station);
			if (unitsByType2 == null)
			{
				return;
			}
			foreach (Unit item2 in unitsByType2)
			{
				DamageTestHelper.FullyRestoreUnitHealth(item2);
			}
		}
	}
}
