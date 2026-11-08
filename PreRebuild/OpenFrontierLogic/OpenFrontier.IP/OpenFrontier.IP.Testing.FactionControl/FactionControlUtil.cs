using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.Scenarios;
using OpenFrontier.IP.UI.Screens;
using UnityEngine;

namespace OpenFrontier.IP.Testing.FactionControl
{
	public static class FactionControlUtil
	{
		public static void SwitchLocalPlayerFactionTo(Faction faction)
		{
			if (!(EngineASX.Instance.LocalFaction != null))
			{
				return;
			}
			if (faction.FactionAI != null)
			{
				Object.DestroyImmediate(faction.FactionAI);
				faction.FactionAI = null;
			}
			Unit localPilottedUnit = EngineASX.Instance.LocalPilottedUnit;
			if (localPilottedUnit != null)
			{
				OrdersHelper.CancelAutoPilot(localPilottedUnit);
			}
			EngineASX.Instance.LocalPlayer.Person.Faction = faction;
			if (localPilottedUnit != null)
			{
				localPilottedUnit.Faction = faction;
			}
			SetupFactionForPlayerControl(faction);
			EngineASX.Instance.World.CalculateSectorSecurityLevels();
			foreach (Fleet fleet in faction.Fleets)
			{
				if (fleet.ActiveOrder != null)
				{
					SetupOrderForPlayer(fleet.ActiveOrder.FleetOrder);
				}
				foreach (FleetOrder item in fleet.OrderQueue)
				{
					SetupOrderForPlayer(item);
				}
				SetupFleetForPlayerControl(fleet);
			}
			faction.CreateStatsIfNull();
			faction.ClearGeneratedName();
			faction.RemoveRankingSystemCompletely();
			faction.LeaderPerson = EngineASX.Instance.LocalPlayer.Person;
			faction.RemoveAllPlacedBounty();
		}

		private static void SetupFactionForPlayerControl(Faction faction)
		{
			faction.AutopilotExcludedSectors.Clear();
			faction.FactionType = FactionType.Player;
			faction.TradeEfficiency = GameController.Instance.PlayerFactionPrefab.TradeEfficiency;
			faction.TradeIllegalGoods = true;
			faction.Aggression = 0.5f;
			faction.Greed = 0.5f;
		}

		public static void SwitchLocalPlayerFactionToAndDestroyExisting(Faction faction)
		{
			Faction localFaction = EngineASX.Instance.LocalFaction;
			SwitchLocalPlayerFactionTo(faction);
			localFaction.DestroyAllFactionPeople();
			localFaction.SafeDestroy();
		}

		public static void SwitchLocalPlayerFactionToAndAutomateExisting(Faction faction)
		{
			Faction localFaction = EngineASX.Instance.LocalFaction;
			SwitchLocalPlayerFactionTo(faction);
			AutomateFaction(localFaction);
		}

		public static void AutomateFaction(Faction faction)
		{
			faction.FactionType = FactionType.Generic;
			faction.CreateAndInitFactionAIFromType(FactionAIType.Generic);
			FactionSpawner.AssignFactionName(faction, FactionType.Generic);
			SetupFactionForAIControl(faction);
		}

		private static void SetupFactionForAIControl(Faction faction)
		{
			if (faction.Stats != null)
			{
				Object.Destroy(faction.Stats);
			}
			faction.AutopilotExcludedSectors.Clear();
			faction.TradeEfficiency = FactionSpawner.GetRandomTradeEfficiency();
			faction.RemoveRankingSystemCompletely();
			if (faction.PilotRankingSystem == null)
			{
				faction.PilotRankingSystem = GameController.Instance.GameSettings.DefaultRankingSystem;
			}
			faction.FactionAI.TryCreateOrPickLeaderIfNone();
			faction.FactionAI.PlaceLeaderAtBestStation();
			faction.FactionAI.AssignAllPilotRanks(updateDebugInfo: false);
			if (faction.LeaderPerson != null)
			{
				faction.Greed = faction.LeaderPerson.Greed;
				faction.Aggression = faction.LeaderPerson.Aggression;
				faction.Virtue = faction.LeaderPerson.Properness;
			}
			foreach (Fleet fleet in faction.Fleets)
			{
				if (fleet.ActiveOrder != null)
				{
					SetupOrderForAI(fleet.ActiveOrder.FleetOrder, (float)fleet.ActiveOrder.TimeElapsed);
				}
				foreach (FleetOrder item in fleet.OrderQueue)
				{
					SetupOrderForAI(item, 0f);
				}
			}
		}

		public static void LeaveCurrentPlayerFactionAndAutomate(out Faction oldPlayerFaction, out Faction newPlayerFaction)
		{
			oldPlayerFaction = EngineASX.Instance.LocalFaction;
			Unit localPilottedUnit = EngineASX.Instance.LocalPilottedUnit;
			if (localPilottedUnit != null)
			{
				OrdersHelper.CancelAutoPilot(localPilottedUnit);
			}
			newPlayerFaction = Object.Instantiate(GameController.Instance.PlayerFactionPrefab);
			newPlayerFaction.UniqueId = -1;
			newPlayerFaction.Init();
			newPlayerFaction.Credits = 30000;
			if (localPilottedUnit != null)
			{
				localPilottedUnit.Faction = newPlayerFaction;
			}
			EngineASX.Instance.LocalPlayer.Person.Faction = newPlayerFaction;
			newPlayerFaction.LeaderPerson = EngineASX.Instance.LocalPlayer.Person;
			if (oldPlayerFaction != null)
			{
				AutomateFaction(oldPlayerFaction);
			}
		}

		private static void SetupFleetForPlayerControl(Fleet fleet)
		{
			fleet.Settings.PreferToDock = EngineASX.Instance.PlayerFleetPrefab.Settings.PreferToDock;
			fleet.Settings.PreferCloak = EngineASX.Instance.PlayerFleetPrefab.Settings.PreferCloak;
			fleet.Settings.CargoCollectionPreference = EngineASX.Instance.PlayerFleetPrefab.Settings.CargoCollectionPreference;
			fleet.FleetStrategy = FactionStrategy.Unspecified;
			fleet.Settings.TargetInterceptionLowerDistance = EngineASX.Instance.PlayerFleetPrefab.Settings.TargetInterceptionLowerDistance;
			fleet.Settings.TargetInterceptionUpperDistance = EngineASX.Instance.PlayerFleetPrefab.Settings.TargetInterceptionUpperDistance;
			fleet.Settings.Aggression = EngineASX.Instance.PlayerFleetPrefab.Settings.Aggression;
		}

		private static void SetupOrderForPlayer(FleetOrder queuedOrder)
		{
			queuedOrder.MaxDuration = 0f;
			queuedOrder.AllowTimeout = false;
			if (queuedOrder.CompletionMode == FleetOrderCompletionMode.Repeat)
			{
				queuedOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
			}
		}

		private static void SetupOrderForAI(FleetOrder queuedOrder, float elapsedTime)
		{
			queuedOrder.AllowTimeout = true;
			if ((double)queuedOrder.MaxDuration == 0.0)
			{
				queuedOrder.MaxDuration = elapsedTime + Random.Range(0.5f, 1f) * queuedOrder.AIDefaultMaxDuration;
			}
		}
	}
}
