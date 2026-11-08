using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public static class FactionRetirer
	{
		public static bool ShouldRetireFaction(Faction faction)
		{
			if (!IsFactionOldEnoughToRetire(faction) || !EnoughTimeSinceNoBuiltUnits(faction))
			{
				return false;
			}
			if (faction.IsFreelancer && faction.People.Count == 0)
			{
				return true;
			}
			if (faction.EstimateNonMinorShipAndStationCount() > 0)
			{
				return false;
			}
			return true;
		}

		private static bool EnoughTimeSinceNoBuiltUnits(Faction faction)
		{
			return faction.Engine.ScenarioElapsedTime - faction.FactionAI.LastBuiltUnitTime > (double)faction.Engine.GameSettings.FactionSettings.FactionRetireMaxTimeWithNoUnitsBuilt;
		}

		public static bool IsFactionOldEnoughToRetire(Faction faction)
		{
			return faction.GetTimeSinceSpawn() > faction.Engine.GameSettings.FactionSettings.FactionRetireMinFactionAge;
		}

		public static void RetireFaction(Faction faction)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"Faction {faction} retiring..", faction, 1);
			}
			EngineASX.Instance.NotifyFactionRetiring(faction);
			if (ShouldShowFactionRetireMessage(faction))
			{
				CreateFactionRetireMessage(faction);
			}
			faction.UndockAllUnits();
			List<Unit> unitsByType = faction.GetUnitsByType(UnitType.Station);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item.IsValidAndNotDestroyed)
					{
						item.Components.UndockAllDockedUnits();
					}
				}
			}
			faction.LoseOwnershipOfAllUnitsOfType(UnitType.Cargo);
			faction.LoseOwnershipOfAllUnitsOfType(UnitType.Station);
			faction.LoseOwnershipOfAllUnitsOfType(UnitType.Ship);
			faction.DestroyAllFactionPeople();
			faction.SafeDestroy();
		}

		private static bool ShouldShowFactionRetireMessage(Faction faction)
		{
			if (faction.FactionType != FactionType.StationBuilder && faction.FactionAI != null && !faction.FactionAI.AISettings.PreferSingleShip)
			{
				return faction.HighestEverNetWorth >= GameController.Instance.GameSettings.FactionSettings.FactionRetireNotificationMinHighestNetWorth;
			}
			return false;
		}

		public static void CreateFactionRetireMessage(Faction faction)
		{
			PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage();
			playerActiveMessage.SubjectText = $"News: {faction.GetShortNameElseLong()} {GetFactionRetiresSubject(faction)}";
			if (faction.IsFreelancer)
			{
				playerActiveMessage.MessageText = $"{faction.Name} resigned today";
			}
			else
			{
				playerActiveMessage.MessageText = $"The {faction.Name} faction resigned today";
			}
			faction.Engine.LocalPlayer.AddMessageDelayed(playerActiveMessage, 2f + Random.value * 10f);
		}

		private static string GetFactionRetiresSubject(Faction faction)
		{
			if (faction.AISettings.PreferSingleShip)
			{
				return new string[2] { "Resigns", "Retires" }.GetRandom();
			}
			return new string[4] { "Resigns", "Retires", "Folds", "Collapses" }.GetRandom();
		}
	}
}
