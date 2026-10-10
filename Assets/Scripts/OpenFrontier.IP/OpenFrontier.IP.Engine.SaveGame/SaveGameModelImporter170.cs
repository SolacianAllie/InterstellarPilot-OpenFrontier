using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Avatars;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.CachedFleetSettings;
using OpenFrontier.IP.Engine.CargoFactory;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.IP.Engine.Core.Units.ActiveUnits;
using OpenFrontier.IP.Engine.DitchShip;
using OpenFrontier.IP.Engine.EngineActions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.Bounty;
using OpenFrontier.IP.Engine.Fleets.ActiveOrders;
using OpenFrontier.IP.Engine.Fleets.FleetFormations;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.Engine.MissionObjectives;
using OpenFrontier.IP.Engine.MissionSpecs;
using OpenFrontier.IP.Engine.Missions;
using OpenFrontier.IP.Engine.Triggers;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers;
using OpenFrontier.IP.SavedGames.V2.Model;
using OpenFrontier.IP.SavedGames.V2.Model.Actions;
using OpenFrontier.IP.SavedGames.V2.Model.Factions;
using OpenFrontier.IP.SavedGames.V2.Model.Factions.Bounty;
using OpenFrontier.IP.SavedGames.V2.Model.Factions.FactionAITypes;
using Random = UnityEngine.Random;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.ActiveOrderTypes;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.Models;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs.JobTypes;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs.Missions;
using OpenFrontier.IP.SavedGames.V2.Model.Triggers;
using OpenFrontier.IP.Scenarios;
using OpenFrontier.IP.SpaceUnity;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.SaveGame
{
	public class SaveGameModelImporter170 : ISaveGameImporter
	{
		private List<FleetOrder> fleetOrderCache = new List<FleetOrder>(20);

		private List<ModdedComponent> moddedComponentCache = new List<ModdedComponent>(100);

		private SavedGame savedGame;

		private EngineASX engine;

		public void Import(ISavedGame _savedGame, EngineASX _engine)
		{
			engine = _engine;
			savedGame = (SavedGame)_savedGame;
			engine.World.SaveGameCount = savedGame.Header.SaveNumber;
			if (savedGame.Header.CreatedVersion == null)
			{
				savedGame.Header.CreatedVersion = savedGame.Header.Version;
			}
			else
			{
				engine.World.CreatedVersion = savedGame.Header.CreatedVersion;
			}
			engine.World.ScenarioTitle = savedGame.Header.ScenarioTitle;
			engine.World.ScenarioAuthor = savedGame.Header.ScenarioAuthor;
			engine.World.ScenarioAuthoringTool = savedGame.Header.ScenarioAuthoringTool;
			engine.World.ScenarioDescription = savedGame.Header.ScenarioDescription;
			engine.World.ScenarioDateYear = savedGame.Header.GameStartDate.Year;
			engine.World.ScenarioDateMonth = savedGame.Header.GameStartDate.Month;
			engine.World.ScenarioDateDay = savedGame.Header.GameStartDate.Day;
			engine.World.ScenarioDateHour = savedGame.Header.GameStartDate.Hour;
			engine.World.ScenarioDateMinute = savedGame.Header.GameStartDate.Minute;
			double secondsElapsed = savedGame.Header.SecondsElapsed;
			engine.ChangeScenarioElapsedTime(secondsElapsed);
			ImportSectors();
			SectorPositioner.PositionSectors(engine.Sectors, GameController.Instance.GameSettings.UniverseBoundsSettings);
			ImportFactions();
			ImportFactionAIs();
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				faction.CreateTradeNetworkIfRequired();
			}
			ImportPatrolPaths();
			ImportFactionRelations();
			ImportFactionOpinions();
			ImportUnits();
			ImportNamedUnits();
			ImportUnitComponents();
			ImportModdedComponents();
			RechargeComponents();
			ImportUnitCapacitorCharges();
			ImportUnitCloakStates();
			ImportPoweredDownComponents();
			ImportAutoFireComponents();
			ImportUnitEngineThrottles();
			ImportUnitCargos();
			ImportDamagedUnitShields();
			ImportDamagedComponents();
			ImportActiveUnits();
			ImportDestructableUnits();
			ImportFactionIntel();
			ImportPassengerGroups();
			ImportWormholes();
			ImportHangars();
			ImportFleets();
			ImportPlayerFleetSettings();
			ImportNamedFleets();
			ImportPeople();
			ImportFleetOrders();
			RepopulateTradeRouteEstimatedQuantities();
			ImportNpcs();
			ImportFactionLeaders();
			ImportJobs();
			ImportFactionAIExcludedUnits();
			ImportFactionMercenaryData();
			ImportFleetSpawners();
			ImportMissions();
			if (savedGame.Player != null)
			{
				ImportGamePlayer(engine.LocalPlayer, savedGame.Player);
			}
			SetFreelancerFactionNames();
			ImportHudData();
			ImportFactionTransactions();
			ImportWorld();
			ImportMoons();
			ImportSeedOptions();
			FixUpPlanetsAndMoons(savedGame.Header.CreatedVersion);
			ImportFactionBountyBoards();
			ImportTriggerGroups();
			ImportDitchedUnits();
			ImportEngineData();
			ImportPlayerUnitFleetSettings();
			ImportDefaultPlayerFleetSettings();
			ImportTractoringUnits();
			if (savedGame.Header.Version < new Version(2, 0, 13))
			{
				FixUpMissingPowerGenerators();
				EnsureNpcPeopleDestroyedWhenKilled();
			}
			if (savedGame.Header.Version < new Version(2, 0, 13))
			{
				try
				{
					FixUpAIOrdersWithInfiniteMaxDuration();
				}
				catch
				{
				}
			}
			if (savedGame.Header.Version < new Version(2, 0, 17))
			{
				try
				{
					FixUpInvalidCargo();
				}
				catch
				{
				}
			}
			try
			{
				FixNpcsPilottingInvalidUnits();
			}
			catch
			{
			}
			try
			{
				if (savedGame.Header.Version <= new Version(2, 0, 18))
				{
					EngineASX.Instance.LocalFaction.TradeIllegalGoods = true;
				}
			}
			catch
			{
			}
			try
			{
				if (!(savedGame.Header.Version <= new Version(2, 0, 18)))
				{
					return;
				}
				foreach (Faction faction2 in EngineASX.Instance.Factions)
				{
					if (faction2 != null && !faction2.IsPlayerFaction)
					{
						faction2.AutopilotExcludedSectors.Clear();
					}
				}
			}
			catch
			{
			}
		}

		private void RepopulateTradeRouteEstimatedQuantities()
		{
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				foreach (Fleet fleet in faction.Fleets)
				{
					if (fleet.ActiveOrder is ActiveTradeOrder { TradeRoute: not null } activeTradeOrder)
					{
						activeTradeOrder.TradeRoute.EstimatedQuantity = CalculateEstimatedTradeRouteQuantity(fleet, activeTradeOrder, activeTradeOrder.TradeRoute);
					}
				}
			}
		}

		private int CalculateEstimatedTradeRouteQuantity(Fleet fleet, ActiveTradeOrder activeTradeOrder, AITradeRoute tradeRoute)
		{
			if (tradeRoute.CargoClass == null)
			{
				return 0;
			}
			switch (activeTradeOrder.CurrentState)
			{
			case ActiveTradeOrderState.GoBuy:
			case ActiveTradeOrderState.Buying:
				if (tradeRoute.BuyLocation != null)
				{
					float a = fleet.GetCachedFreeCargoSpace() / tradeRoute.CargoClass.Volume;
					a = Mathf.Min(a, tradeRoute.BuyLocation.Unit.GetCargoCountOf(tradeRoute.CargoClass));
					if (tradeRoute.BuyPriceMultiplier > 0f)
					{
						a = Mathf.Min(a, (float)fleet.Faction.Credits / tradeRoute.BuyPriceMultiplier);
					}
					return (int)a;
				}
				break;
			case ActiveTradeOrderState.GoSell:
			case ActiveTradeOrderState.Selling:
				if (tradeRoute.SellLocation != null)
				{
					return fleet.GetCargoCount(tradeRoute.CargoClass);
				}
				break;
			}
			return 0;
		}

		private void FixNpcsPilottingInvalidUnits()
		{
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				foreach (Person person in faction.People)
				{
					if (person.IsPilot && !person.CurrentUnit.IsPilottable())
					{
						Debug.LogError($"Removing {person} control of unit {person.CurrentUnit} as the unit is not pilottable", person);
						person.IsPilot = false;
						if (person.NpcPilot != null)
						{
							person.NpcPilot.Fleet = null;
						}
					}
				}
			}
		}

		private void RechargeComponents()
		{
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Ship);
				if (unitsByType != null)
				{
					foreach (Unit item in unitsByType)
					{
						foreach (ComponentBase unitComponent in item.Components.UnitComponents)
						{
							unitComponent.RechargeFull();
						}
					}
				}
				List<Unit> unitsByType2 = sector.GetUnitsByType(UnitType.Station);
				if (unitsByType2 == null)
				{
					continue;
				}
				foreach (Unit item2 in unitsByType2)
				{
					foreach (ComponentBase unitComponent2 in item2.Components.UnitComponents)
					{
						unitComponent2.RechargeFull();
					}
				}
			}
		}

		private void ImportTractoringUnits()
		{
			foreach (ModelTractorerDataItem tractorerDataItem in savedGame.TractorerDataItems)
			{
				Unit unit = GetUnit(tractorerDataItem.TractoringUnit);
				Unit unit2 = GetUnit(tractorerDataItem.TractoredUnit);
				if (!(unit != null) || !(unit2 != null) || !(unit.Sector == unit2.Sector))
				{
					continue;
				}
				TractorTurretComponent tractorTurret = unit.GetTractorTurret();
				if (tractorTurret != null)
				{
					tractorTurret.PullUnit(unit2);
					if (tractorTurret.ActiveTurret != null)
					{
						((ActiveTractorTurret)tractorTurret.ActiveTurret).RestoreTractorBeam();
					}
				}
			}
		}

		private void FixUpInvalidCargo()
		{
			List<CargoClass> cargoClassCache = new List<CargoClass>();
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					FixUpInvalidCargo(item, cargoClassCache);
				}
			}
		}

		private void FixUpInvalidCargo(Unit unit, List<CargoClass> cargoClassCache)
		{
			if (!(unit.Components != null) || !(unit.Components.CargoBayComponent != null))
			{
				return;
			}
			cargoClassCache.Clear();
			foreach (KeyValuePair<CargoClass, int> cargo in unit.Components.CargoBayComponent.Cargos)
			{
				if (cargo.Key.IsReserved && cargo.Value > 0)
				{
					cargoClassCache.Add(cargo.Key);
				}
			}
			foreach (CargoClass item in cargoClassCache)
			{
				int countOf = unit.Components.CargoBayComponent.GetCountOf(item);
				unit.Components.CargoBayComponent.AddToCargo(item, -countOf, ignoreCapacity: true);
			}
		}

		private void FixUpAIOrdersWithInfiniteMaxDuration()
		{
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (!faction.IsAIFactionType)
				{
					continue;
				}
				foreach (Fleet fleet in faction.Fleets)
				{
					FixUpAIOrdersWithInfiniteMaxDuration(fleet);
				}
			}
		}

		private void FixUpAIOrdersWithInfiniteMaxDuration(Fleet fleet)
		{
			if (fleet.ActiveOrder != null && fleet.ActiveOrder.FleetOrder.MaxDuration == 0f && fleet.ActiveOrder.FleetOrder.AIDefaultMaxDuration > 0f)
			{
				double timeElapsed = fleet.ActiveOrder.TimeElapsed;
				fleet.ActiveOrder.FleetOrder.MaxDuration = (float)timeElapsed + fleet.ActiveOrder.FleetOrder.AIDefaultMaxDuration * UnityEngine.Random.Range(0.5f, 1f);
			}
			foreach (FleetOrder item in fleet.OrderQueue)
			{
				if (item != null && item.MaxDuration == 0f && item.AIDefaultMaxDuration > 0f)
				{
					item.MaxDuration = item.AIDefaultMaxDuration;
				}
			}
		}

		private void EnsureNpcPeopleDestroyedWhenKilled()
		{
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				foreach (Person person in faction.People)
				{
					if (!person.IsLocalPlayer && !person.DestroyGameObjectOnKill)
					{
						person.DestroyGameObjectOnKill = true;
					}
				}
			}
		}

		private void FixUpMissingPowerGenerators()
		{
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Ship);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					FixUpMissingComponents(item);
				}
			}
		}

		private void FixUpMissingComponents(Unit unit)
		{
			if (unit.Components != null && unit.Components.PowerGenerator == null)
			{
				ComponentBay componentBay = unit.Components.Bays.FirstOrDefault((ComponentBay e) => e.BayType.BayType == BayType.PowerGenerator);
				if (componentBay != null && componentBay.InitialComponentClass != null)
				{
					componentBay.InstallComponent(componentBay.InitialComponentClass);
				}
			}
		}

		private void ImportDefaultPlayerFleetSettings()
		{
			if (savedGame.PlayerDefaultFleetSettings != null)
			{
				EngineASX.Instance.CachedFleetSettingsController.DefaultFleetSettings = new CachedFleetSettingsItem
				{
					FleetSettings = savedGame.PlayerDefaultFleetSettings.FleetSettings,
					FormationId = savedGame.PlayerDefaultFleetSettings.FormationId,
					HomeBase = ImportSectorTarget(savedGame.PlayerDefaultFleetSettings.HomeBase)
				};
			}
		}

		private void ImportPlayerUnitFleetSettings()
		{
			foreach (ModelPlayerUnitFleetSettings playerUnitFleetSettingItem in savedGame.PlayerUnitFleetSettingItems)
			{
				if (playerUnitFleetSettingItem.Unit != null && playerUnitFleetSettingItem.Settings != null)
				{
					Unit unit = GetUnit(playerUnitFleetSettingItem.Unit);
					if (unit != null)
					{
						CachedFleetSettingsItem cachedFleetSettingsItem = new CachedFleetSettingsItem();
						cachedFleetSettingsItem.FleetSettings = playerUnitFleetSettingItem.Settings.FleetSettings;
						cachedFleetSettingsItem.HomeBase = ImportSectorTarget(playerUnitFleetSettingItem.Settings.HomeBase);
						cachedFleetSettingsItem.FormationId = playerUnitFleetSettingItem.Settings.FormationId;
						EngineASX.Instance.CachedFleetSettingsController.SetItem(unit, cachedFleetSettingsItem);
					}
				}
			}
		}

		private void ImportDitchedUnits()
		{
			foreach (ModelDitchedUnit item in savedGame.DitchedUnitsToBeCleanedUp)
			{
				Unit unitByid = EngineASX.Instance.GetUnitByid(item.Unit?.Id ?? (-1));
				if (unitByid != null)
				{
					EngineASX.Instance.DitchUnitCleanupModule.CleanupItems.Add(new DitchUnitCleanupModule.DitchedUnitCleanupItem(unitByid, item.ExpiryTime));
				}
			}
		}

		private void ImportEngineData()
		{
			EngineASX.Instance.UnitIdCounter = savedGame.EngineData.UnitIdCounter;
			EngineASX.Instance.PlayerMessageIdCounter = savedGame.EngineData.PlayerMessageIdCounter;
			EngineASX.Instance.PersonIdCounter = savedGame.EngineData.PersonIdCounter;
			EngineASX.Instance.FactionIdCounter = savedGame.EngineData.FactionIdCounter;
			EngineASX.Instance.FleetOrderIdCounter = savedGame.EngineData.FleetOrderIdCounter;
			EngineASX.Instance.PassengerGroupIdCounter = savedGame.EngineData.PassengerGroupIdCounter;
			EngineASX.Instance.JobIdCounter = savedGame.EngineData.JobIdCounter;
			EngineASX.Instance.FleetIdCounter = savedGame.EngineData.FleetIdCounter;
			EngineASX.Instance.SectorIdCounter = savedGame.EngineData.SectorIdCounter;
			EngineASX.Instance.MissionIdCounter = savedGame.EngineData.MissionIdCounter;
			EngineASX.Instance.PatrolPathIdCounter = savedGame.EngineData.PatrolPathIdCounter;
			EngineASX.Instance.MissionObjectiveIdCounter = savedGame.EngineData.MissionObjectiveIdCounter;
		}

		private void ImportTriggerGroups()
		{
			foreach (ModelTriggerGroup triggerGroup in savedGame.TriggerGroups)
			{
				ImportTriggerGroup(triggerGroup);
			}
			foreach (ModelTriggerGroup triggerGroup2 in savedGame.TriggerGroups)
			{
				TriggerGroup triggerGroupById = EngineASX.Instance.GetTriggerGroupById(triggerGroup2.Id);
				if (triggerGroupById != null)
				{
					ImportTriggers(triggerGroupById, triggerGroup2);
					ImportActions(triggerGroupById, triggerGroup2);
				}
			}
		}

		private void ImportTriggerGroup(ModelTriggerGroup modelTriggerGroup)
		{
			TriggerGroup triggerGroup = UnityObjectHelper.NewGameObject<TriggerGroup>(EngineASX.Instance.World.transform);
			triggerGroup.UniqueId = modelTriggerGroup.Id;
			triggerGroup.gameObject.SetActive(modelTriggerGroup.IsActive);
			triggerGroup.FireAndDisable = modelTriggerGroup.FireAndDisable;
			triggerGroup.EvaluateFrequency = modelTriggerGroup.EvaluateFrequency;
			if (triggerGroup.EvaluateFrequency < 0.2f)
			{
				Debug.LogWarning($"Setting a trigger group evaluation frequency below {0.2f:N2} is not allowed. Specified value: {triggerGroup.EvaluateFrequency}");
				triggerGroup.EvaluateFrequency = 0.2f;
			}
			triggerGroup.NextEvaluationTime = modelTriggerGroup.NextEvaluationTime;
			triggerGroup.FireCount = modelTriggerGroup.FireCount;
			triggerGroup.MaxFireCount = modelTriggerGroup.MaxFireCount;
			triggerGroup.MaxFiredAction = modelTriggerGroup.MaxFiredAction;
			triggerGroup.Init();
		}

		private void ImportTriggers(TriggerGroup triggerGroup, ModelTriggerGroup modelTriggerGroup)
		{
			foreach (ModelTrigger trigger in modelTriggerGroup.Triggers)
			{
				ImportTrigger(triggerGroup, trigger);
			}
			triggerGroup.FindTriggers();
		}

		private void ImportTrigger(TriggerGroup triggerGroup, ModelTrigger modelTrigger)
		{
			if (!(modelTrigger is ModelTrigger_Always))
			{
				if (!(modelTrigger is ModelTrigger_Player_IsPilotting modelTrigger_Player_IsPilotting))
				{
					if (!(modelTrigger is ModelTrigger_Player_CurrentHudTarget modelTrigger_Player_CurrentHudTarget))
					{
						if (!(modelTrigger is ModelTrigger_Player_NoHostileFactions))
						{
							if (modelTrigger is ModelTrigger_Scenario_TimeElapsed modelTrigger_Scenario_TimeElapsed)
							{
								triggerGroup.gameObject.AddComponent<Trigger_Scenario_TimeElapsed>().Time = modelTrigger_Scenario_TimeElapsed.Time;
							}
						}
						else
						{
							triggerGroup.gameObject.AddComponent<Trigger_Player_NoHostileFactions>();
						}
					}
					else
					{
						triggerGroup.gameObject.AddComponent<Trigger_Player_CurrentHudTarget>().Target = GetUnit(modelTrigger_Player_CurrentHudTarget.TargetUnit);
					}
				}
				else
				{
					triggerGroup.gameObject.AddComponent<Trigger_Player_IsPilotting>().WaitForHud = modelTrigger_Player_IsPilotting.WaitForHud;
				}
			}
			else
			{
				triggerGroup.gameObject.AddComponent<Trigger_Always>();
			}
		}

		private void ImportActions(TriggerGroup triggerGroup, ModelTriggerGroup modelTriggerGroup)
		{
			foreach (ModelAction action in modelTriggerGroup.Actions)
			{
				ImportAction(triggerGroup, action);
			}
		}

		private void ImportAction(TriggerGroup triggerGroup, ModelAction modelAction)
		{
			if (!(modelAction is ModelAction_Player_NewMessageSimple modelAction_Player_NewMessageSimple))
			{
				if (!(modelAction is ModelAction_Mission_Activate modelAction_Mission_Activate))
				{
					if (!(modelAction is ModelAction_Mission_ChangeStage modelAction_Mission_ChangeStage))
					{
						if (!(modelAction is ModelAction_Mission_ActivateObjective modelAction_Mission_ActivateObjective))
						{
							if (!(modelAction is ModelAction_Mission_CompleteObjective modelAction_Mission_CompleteObjective))
							{
								if (modelAction is ModelAction_TriggerGroup_Activate modelAction_TriggerGroup_Activate)
								{
									triggerGroup.gameObject.AddComponent<Action_TriggerGroup_Activate>().Target = GetTriggerGroup(modelAction_TriggerGroup_Activate.TriggerGroup);
								}
							}
							else
							{
								Action_Mission_CompleteObjective action_Mission_CompleteObjective = triggerGroup.gameObject.AddComponent<Action_Mission_CompleteObjective>();
								action_Mission_CompleteObjective.Objective = GetMissionObjective(modelAction_Mission_CompleteObjective.MissionObjective);
								action_Mission_CompleteObjective.Success = modelAction_Mission_CompleteObjective.Success;
							}
							return;
						}
						Action_Mission_ActivateObjective action_Mission_ActivateObjective = triggerGroup.gameObject.AddComponent<Action_Mission_ActivateObjective>();
						{
							foreach (ModelMissionObjective objective in modelAction_Mission_ActivateObjective.Objectives)
							{
								MissionObjective missionObjectiveByid = EngineASX.Instance.GetMissionObjectiveByid(objective.Id);
								if (missionObjectiveByid != null)
								{
									action_Mission_ActivateObjective.Objectives.Add(missionObjectiveByid);
								}
							}
							return;
						}
					}
					Action_Mission_ChangeStage action_Mission_ChangeStage = triggerGroup.gameObject.AddComponent<Action_Mission_ChangeStage>();
					if (modelAction_Mission_ChangeStage.Stage == null || modelAction_Mission_ChangeStage.Stage.Mission == null)
					{
						return;
					}
					action_Mission_ChangeStage.Mission = GetMission(modelAction_Mission_ChangeStage.Stage.Mission);
					if (action_Mission_ChangeStage.Mission != null)
					{
						int num = modelAction_Mission_ChangeStage.Stage.Mission.Stages.IndexOf(modelAction_Mission_ChangeStage.Stage);
						if (num > -1)
						{
							action_Mission_ChangeStage.Stage = action_Mission_ChangeStage.Mission.Stages[num];
						}
					}
				}
				else
				{
					triggerGroup.gameObject.AddComponent<Action_Mission_Activate>().Mission = GetMission(modelAction_Mission_Activate.Mission);
				}
			}
			else
			{
				Action_Player_NewMessageSimple action_Player_NewMessageSimple = triggerGroup.gameObject.AddComponent<Action_Player_NewMessageSimple>();
				action_Player_NewMessageSimple.From = modelAction_Player_NewMessageSimple.From;
				action_Player_NewMessageSimple.To = modelAction_Player_NewMessageSimple.To;
				action_Player_NewMessageSimple.Subject = modelAction_Player_NewMessageSimple.Subject;
				action_Player_NewMessageSimple.Message = modelAction_Player_NewMessageSimple.Message;
				action_Player_NewMessageSimple.Notifications = modelAction_Player_NewMessageSimple.Notifications;
			}
		}

		private void FixUpPlanetsAndMoons(Version createdVersion)
		{
			if (!(createdVersion < new Version(1, 7, 22)))
			{
				return;
			}
			engine.World.CreatedVersion = Versioning.Version;
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Planet);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					Moon component = item.GetComponent<Moon>();
					if (component != null)
					{
						CreatePlanetsSeeder.RandomizeMoon(component, GameController.Instance.GameSettings.DefaultWorldSeedSettings.CreatePlanetsSeederSettings);
					}
					else
					{
						item.transform.localPosition = CreatePlanetsSeeder.GetRandomPlanetLocalPosition(item.Sector, GameController.Instance.GameSettings.DefaultWorldSeedSettings.CreatePlanetsSeederSettings);
					}
				}
			}
		}

		private void ImportNamedFleets()
		{
			foreach (ModelFleet item in savedGame.Fleets.Where((ModelFleet e) => !string.IsNullOrWhiteSpace(e.Name)))
			{
				Fleet fleetByid = engine.GetFleetByid(item.Id);
				if (fleetByid != null)
				{
					fleetByid.Name = item.Name;
				}
			}
		}

		private void ImportSeedOptions()
		{
			if (savedGame.SeedOptions != null)
			{
				engine.World.Seeder.Settings.CustomScenarioSeederSettings.SeedAbandonedCargo = savedGame.SeedOptions.SeedAbandonedCargo;
				engine.World.Seeder.Settings.CustomScenarioSeederSettings.SeedAbandonedShips = savedGame.SeedOptions.SeedAbandonedShips;
				engine.World.Seeder.Settings.CustomScenarioSeederSettings.SeedCargoHolds = savedGame.SeedOptions.SeedCargoHolds;
				engine.World.Seeder.Settings.CustomScenarioSeederSettings.SeedFactionIntel = savedGame.SeedOptions.SeedFactionIntel;
				engine.World.Seeder.Settings.CustomScenarioSeederSettings.SeedPassengerGroups = savedGame.SeedOptions.SeedPassengerGroups;
			}
		}

		private void ImportFactionMercenaryData()
		{
			foreach (ModelFaction faction in savedGame.Factions)
			{
				if (faction.FactionAI == null || faction.FactionAI.FactionMercenaryHireInfo == null)
				{
					continue;
				}
				int id = faction.Id;
				int num = faction.FactionAI.FactionMercenaryHireInfo.HiringFaction?.Id ?? (-1);
				double hireTimeExpiry = faction.FactionAI.FactionMercenaryHireInfo.HireTimeExpiry;
				Faction factionByid = engine.GetFactionByid(id);
				Faction factionByid2 = engine.GetFactionByid(num);
				if (factionByid != null)
				{
					if (factionByid2 != null)
					{
						if (factionByid.FactionAI != null)
						{
							factionByid.FactionAI.MercenaryHireInfo = new MercenaryHireInfo
							{
								HireTimeExpiry = hireTimeExpiry,
								HiringFaction = factionByid2
							};
						}
						else
						{
							Debug.LogError("Expecting mercenary faction to have FactionAI", factionByid);
						}
					}
					else
					{
						MissingFactionMessage(num);
					}
				}
				else
				{
					MissingFactionMessage(id);
				}
			}
		}

		private void ImportNamedUnits()
		{
			foreach (ModelUnit unit in savedGame.Units)
			{
				if (!string.IsNullOrWhiteSpace(unit.Name))
				{
					Unit unitByid = engine.GetUnitByid(unit.Id);
					if (unitByid != null)
					{
						unitByid.UnitName = unit.Name;
						unitByid.UnitShortName = unit.ShortName;
					}
					else
					{
						MissingUnitMessage(unit.Id);
					}
				}
			}
		}

		private void ImportMoons()
		{
			foreach (ModelMoon moon in savedGame.Moons)
			{
				Unit unitByid = engine.GetUnitByid(moon.Unit?.Id ?? (-1));
				if (unitByid != null)
				{
					Unit unitByid2 = engine.GetUnitByid(moon.OrbitUnit?.Id ?? (-1));
					if (unitByid2 != null)
					{
						Moon component = unitByid.GetComponent<Moon>();
						if (component != null)
						{
							component.OrbitingAroundUnit = unitByid2;
							component.OffsetFromPlanet = moon.OffsetFromPlanet.ToVector3();
							if (unitByid.ActiveUnit != null)
							{
								unitByid.ActiveUnit.GetComponent<ActiveUnitMoon>().Reposition();
							}
						}
					}
					else
					{
						MissingUnitMessage(moon.OrbitUnit?.Id ?? (-1));
					}
				}
				else
				{
					MissingUnitMessage(moon.Unit?.Id ?? (-1));
				}
			}
		}

		private void SetFreelancerFactionNames()
		{
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (faction.FactionAI != null && faction.AISettings.PreferSingleShip && faction.LeaderPerson != null)
				{
					FactionSpawner.AssignFactionNameFromPilotName(faction, faction.LeaderPerson);
				}
			}
		}

		private void ImportFactionOpinions()
		{
			foreach (ModelFaction faction in savedGame.Factions)
			{
				if (faction.Opinions == null || faction.Opinions.Items == null)
				{
					continue;
				}
				Faction factionByid = engine.GetFactionByid(faction.Id);
				if (factionByid == null)
				{
					MissingFactionMessage(faction.Id);
					continue;
				}
				foreach (ModelFactionOpinionDataItem item in faction.Opinions.Items)
				{
					Faction factionByid2 = engine.GetFactionByid(item.OtherFaction?.Id ?? (-1));
					if (factionByid2 != null && factionByid2 != factionByid)
					{
						FactionAttitude orCreateAttitude = factionByid.GetOrCreateAttitude(factionByid2);
						orCreateAttitude.Opinion = item.Opinion;
						orCreateAttitude.CreatedTime = item.CreatedTime;
					}
				}
			}
		}

		private void ImportUnitCloakStates()
		{
			foreach (ModelUnit unit in savedGame.Units)
			{
				if (unit.ComponentUnitData == null || !unit.ComponentUnitData.IsCloaked)
				{
					continue;
				}
				Unit unitByid = engine.GetUnitByid(unit.Id);
				if (unitByid != null)
				{
					if (unitByid.Components != null && unitByid.Components.CloakComponent != null)
					{
						unitByid.Components.CloakComponent.State = CloakState.Cloaked;
						continue;
					}
					Debug.LogErrorFormat("Expecting to have cloak component on unit ID {0}", unit.Id);
				}
				else
				{
					MissingUnitMessage(unit.Id);
				}
			}
		}

		private void ImportUnitCapacitorCharges()
		{
			foreach (ModelUnit unit in savedGame.Units)
			{
				if (unit.ComponentUnitData == null || !unit.ComponentUnitData.CapacitorCharge.HasValue)
				{
					continue;
				}
				float value = unit.ComponentUnitData.CapacitorCharge.Value;
				Unit unitByid = engine.GetUnitByid(unit.Id);
				if (unitByid != null)
				{
					if (unitByid.Components != null && unitByid.Components.Capacitor != null)
					{
						unitByid.Components.Capacitor.ChargeNormalized = value;
						continue;
					}
					Debug.LogErrorFormat("Expecting to have capacitor component on unit ID {0}", unit.Id);
				}
				else
				{
					MissingUnitMessage(unit.Id);
				}
			}
		}

		private void ImportUnitEngineThrottles()
		{
			foreach (ModelUnit unit in savedGame.Units)
			{
				if (unit.ComponentUnitData == null || !unit.ComponentUnitData.EngineThrottle.HasValue)
				{
					continue;
				}
				float value = unit.ComponentUnitData.EngineThrottle.Value;
				Unit unitByid = engine.GetUnitByid(unit.Id);
				if (unitByid != null)
				{
					if (unitByid.Components != null && unitByid.Components.EngineComponent != null)
					{
						unitByid.Components.EngineComponent.EngineThrottle = value;
						continue;
					}
					Debug.LogErrorFormat(unitByid, "Unable to apply throttle for unit {0}. No engine component", unitByid);
				}
				else
				{
					MissingUnitMessage(unit.Id);
				}
			}
		}

		private void InstallModdedUnitComponents()
		{
			EngineASX.Instance.EnumerateUnitsWithPredicate((Unit unit) =>
			{
				foreach (ComponentBay bay in unit.Components.Bays)
				{
					ModdedComponent? moddedComponentClassId = GetModdedComponentClassId(unit, bay);
					if (!moddedComponentClassId.HasValue)
					{
						unit.Components.InstallBayDefaultComponent(bay);
					}
					else if (moddedComponentClassId.Value.ComponentClassId > 0)
					{
						ComponentClass componentClassById = engine.GetComponentClassById(moddedComponentClassId.Value.ComponentClassId);
						if (componentClassById != null)
						{
							bay.InstallComponent(componentClassById);
						}
						else
						{
							MissingComponentClassError(moddedComponentClassId.Value.UnitId, moddedComponentClassId.Value.BayId, moddedComponentClassId.Value.ComponentClassId);
						}
					}
				}
			}, (Unit unit) => unit.Components != null);
		}

		private ModdedComponent? GetModdedComponentClassId(Unit unit, ComponentBay bay)
		{
			ModdedComponent value = moddedComponentCache.FirstOrDefault((ModdedComponent e) => e.UnitId == unit.UniqueId && e.BayId == bay.Id);
			if (value.UnitId != 0)
			{
				return value;
			}
			return null;
		}

		private void ImportPoweredDownComponents()
		{
			foreach (ModelUnit unit in savedGame.Units)
			{
				if (unit.ComponentUnitData == null || unit.ComponentUnitData.PoweredDownBayIds == null || unit.ComponentUnitData.PoweredDownBayIds.Count <= 0)
				{
					continue;
				}
				Unit unitByid = engine.GetUnitByid(unit.Id);
				if (unitByid == null)
				{
					MissingUnitMessage(unit.Id);
					continue;
				}
				foreach (int poweredDownBayId in unit.ComponentUnitData.PoweredDownBayIds)
				{
					ComponentBay bayById = unitByid.Components.GetBayById(poweredDownBayId);
					if (bayById != null)
					{
						if (bayById.InstalledComponent != null)
						{
							bayById.InstalledComponent.UserPowered = false;
						}
						else
						{
							MissingBayComponentError(unit.Id, poweredDownBayId);
						}
					}
					else
					{
						MissingBayError(unit.Id, poweredDownBayId);
					}
				}
			}
		}

		private void ImportAutoFireComponents()
		{
			foreach (ModelUnit unit in savedGame.Units)
			{
				if (unit.ComponentUnitData == null || unit.ComponentUnitData.AutoFireBayIds == null || unit.ComponentUnitData.AutoFireBayIds.Count <= 0)
				{
					continue;
				}
				Unit unitByid = engine.GetUnitByid(unit.Id);
				if (unitByid == null)
				{
					MissingUnitMessage(unit.Id);
					continue;
				}
				foreach (int autoFireBayId in unit.ComponentUnitData.AutoFireBayIds)
				{
					ComponentBay bayById = unitByid.Components.GetBayById(autoFireBayId);
					if (bayById != null)
					{
						if (bayById.InstalledComponent is TurretComponent turretComponent && turretComponent.CanAutoFire())
						{
							turretComponent.AutoFire = true;
						}
						else
						{
							MissingBayComponentError(unit.Id, autoFireBayId);
						}
					}
					else
					{
						MissingBayError(unit.Id, autoFireBayId);
					}
				}
			}
		}

		private void ImportModdedComponents()
		{
			moddedComponentCache.Clear();
			foreach (ModelUnit unit in savedGame.Units)
			{
				if (unit.ComponentUnitData != null && unit.ComponentUnitData.ModData != null)
				{
					Unit unitByid = engine.GetUnitByid(unit.Id);
					if (unitByid != null)
					{
						ImportModdedComponents(unitByid, unit.ComponentUnitData.ModData);
					}
					else
					{
						MissingUnitMessage(unit.Id);
					}
				}
			}
			InstallModdedUnitComponents();
			EngineASX.Instance.EnumerateUnitsWithPredicate((Unit unit) =>
			{
				unit.Components.RefreshIsModded();
			}, (Unit unit) => unit.Components != null);
		}

		private void ImportModdedComponents(Unit unit, ModelComponentUnitModData modelComponentUnitModData)
		{
			foreach (ModelComponentUnitModDataItem item2 in modelComponentUnitModData.Items)
			{
				int uniqueId = unit.UniqueId;
				int bayId = item2.BayId;
				int componentClass = (int)item2.ComponentClass;
				ModdedComponent item = new ModdedComponent
				{
					UnitId = uniqueId,
					BayId = bayId,
					ComponentClassId = componentClass
				};
				moddedComponentCache.Add(item);
			}
		}

		private void ImportUnitCargos()
		{
			foreach (ModelUnit unit in savedGame.Units)
			{
				if (unit.ComponentUnitData != null && unit.ComponentUnitData.CargoData != null)
				{
					Unit unitByid = engine.GetUnitByid(unit.Id);
					if (unitByid != null)
					{
						ImportUnitCargo(unitByid.Components, unit.ComponentUnitData.CargoData);
					}
					else
					{
						MissingUnitMessage(unit.Id);
					}
				}
			}
		}

		private void ImportDamagedComponents()
		{
			foreach (ModelUnit unit in savedGame.Units)
			{
				if (unit.ComponentUnitData != null && unit.ComponentUnitData.ComponentHealthData != null && unit.ComponentUnitData.ComponentHealthData.Items != null)
				{
					Unit unitByid = engine.GetUnitByid(unit.Id);
					if (unitByid != null)
					{
						ImportDamagedComponents(unitByid, unit.ComponentUnitData.ComponentHealthData);
					}
					else
					{
						MissingUnitMessage(unit.Id);
					}
				}
			}
		}

		private void ImportDamagedComponents(Unit unit, ModelComponentUnitComponentHealthData modelComponentUnitComponentHealthData)
		{
			foreach (ModelComponentUnitComponentHealthDataItem item in modelComponentUnitComponentHealthData.Items)
			{
				int bayId = item.BayId;
				float health = item.Health;
				if (!(unit != null))
				{
					continue;
				}
				ComponentBay bayById = unit.Components.GetBayById(bayId);
				if (bayById != null)
				{
					if (bayById.InstalledComponent != null)
					{
						bayById.InstalledComponent.HealthNormalized = health;
					}
					else
					{
						MissingBayComponentError(unit.UniqueId, bayId);
					}
				}
				else
				{
					MissingBayError(unit.UniqueId, bayId);
				}
			}
		}

		private void MissingBayComponentError(int unitId, int bayId)
		{
			Debug.LogErrorFormat("Expecting to have component in bay ID:{0} on unit ID {1}", bayId, unitId);
		}

		private void MissingBayError(int unitId, int bayId)
		{
			Debug.LogErrorFormat("Cannot find bay ID:{0} on unit ID {1}", bayId, unitId);
		}

		private void ImportUnitComponents()
		{
			foreach (ModelUnit unit in savedGame.Units)
			{
				if (unit.ComponentUnitData != null)
				{
					Unit unitByid = engine.GetUnitByid(unit?.Id ?? (-1));
					if (unitByid != null)
					{
						ImportUnitComponents(unitByid.Components, unit.ComponentUnitData);
					}
					else
					{
						MissingUnitMessage(unit.Id);
					}
				}
			}
		}

		private void ImportDamagedUnitShields()
		{
			foreach (ModelUnit unit in savedGame.Units)
			{
				if (unit.ComponentUnitData == null || unit.ComponentUnitData.ShieldData == null)
				{
					continue;
				}
				Unit unitByid = engine.GetUnitByid(unit?.Id ?? (-1));
				if (unitByid != null)
				{
					foreach (ModelComponentUnitShieldHealthDataItem item in unit.ComponentUnitData.ShieldData.Items)
					{
						unitByid.Components.ShieldComponent.SetNormalizedShieldPoints(item.ShieldPointIndex, item.Health);
					}
				}
				else
				{
					MissingUnitMessage(unit.Id);
				}
			}
		}

		private void ImportDestructableUnits()
		{
			foreach (ModelUnit unit in savedGame.Units)
			{
				if (unit.HealthData == null && !(unit.TotalDamagedReceived > 0f) && !unit.IsInvulnerable && !unit.AvoidDestruction)
				{
					continue;
				}
				Unit unitByid = engine.GetUnitByid(unit?.Id ?? (-1));
				if (unitByid != null)
				{
					if (unitByid.Destructable != null)
					{
						if (unit.HealthData != null)
						{
							ImportDestructableUnit(unitByid, unit.HealthData);
						}
						if (unit.TotalDamagedReceived > 0f && unitByid != null && unitByid.Destructable != null)
						{
							unitByid.Destructable.TotalDamageReceived = unit.TotalDamagedReceived;
						}
						unitByid.Destructable.AllowDestruction = !unit.AvoidDestruction;
						unitByid.Destructable.IsInvulnerable = unit.IsInvulnerable;
					}
				}
				else
				{
					MissingUnitMessage(unit.Id);
				}
			}
		}

		private void ImportDestructableUnit(Unit unit, ModelUnitHealthData modelUnitHealthData)
		{
			if (unit.Destructable != null)
			{
				unit.Destructable.IsDestroyed = modelUnitHealthData.IsDestroyed;
				unit.Destructable.CurrentHealth = modelUnitHealthData.Health;
			}
		}

		private void ImportActiveUnits()
		{
			foreach (ModelUnit unit in savedGame.Units)
			{
				if (unit.ActiveData != null)
				{
					Unit unitByid = engine.GetUnitByid(unit?.Id ?? (-1));
					if (unitByid != null)
					{
						ImportActiveUnit(unitByid, unit.ActiveData);
					}
					else
					{
						MissingUnitMessage(unit.Id);
					}
				}
			}
		}

		private void ImportActiveUnit(Unit unit, ModelUnitActiveData modelUnitActiveData)
		{
			unit.IsActiveInEngine = true;
			if (unit.ActiveUnit != null && unit.ActiveUnit.UnitRigidBody != null)
			{
				unit.ActiveUnit.UnitRigidBody.linearVelocity = modelUnitActiveData.Velocity.ToVector3();
			}
			if (unit.ActiveUnit != null && unit.ActiveUnit.ActiveUnitShip != null)
			{
				unit.ActiveUnit.ActiveUnitShip.CurrentTurn = modelUnitActiveData.CurrentTurn;
			}
			if (unit.UnitType == UnitType.Planet)
			{
				unit.ActiveUnit.GetComponent<ActiveUnitPlanet>().Rotate();
			}
		}

		private void MissingUnitMessage(int unitId)
		{
			Debug.LogError("SaveGameReader: Cannot find unit ID: " + unitId);
		}

		private void MissingFactionMessage(int factionId)
		{
			Debug.LogError("SaveGameReader: Cannot find faction ID: " + factionId);
		}

		private void ImportFactionAIExcludedUnits()
		{
			foreach (ModelFaction faction in savedGame.Factions)
			{
				if (faction.FactionAI == null || faction.FactionAI.ExcludedUnits == null || !faction.FactionAI.ExcludedUnits.Any())
				{
					continue;
				}
				Faction factionByid = engine.GetFactionByid(faction?.Id ?? (-1));
				if (!(factionByid != null) || !(factionByid.FactionAI != null))
				{
					continue;
				}
				foreach (ModelUnit excludedUnit in faction.FactionAI.ExcludedUnits)
				{
					Unit unitByid = engine.GetUnitByid(excludedUnit?.Id ?? (-1));
					if (unitByid != null)
					{
						factionByid.FactionAI.AISettings.ExcludedUnitIds.Add(unitByid.UniqueId);
					}
				}
			}
		}

		private void ImportFactionLeaders()
		{
			foreach (ModelFaction faction in savedGame.Factions)
			{
				Faction factionByid = engine.GetFactionByid(faction?.Id ?? (-1));
				if (factionByid != null && faction.Leader != null)
				{
					Person personById = engine.GetPersonById(faction.Leader?.Id ?? (-1));
					if (personById.Faction != factionByid)
					{
						Debug.LogError($"Cannot assign person to be leader of faction as person doesn't belong to faction. Person ID: {faction.Leader.Id}. Faction ID: {faction.Id}");
					}
					else
					{
						factionByid.LeaderPerson = personById;
					}
				}
			}
		}

		private void ImportFactionIntel()
		{
			foreach (ModelFaction faction in savedGame.Factions)
			{
				Faction factionByid = engine.GetFactionByid(faction.Id);
				if (factionByid != null && faction.Intel != null)
				{
					ImportFactionIntel(factionByid.Intel, faction.Intel);
				}
			}
		}

		private void ImportWorld()
		{
			ImportRandomEventsController();
			ImportFactionSpawner();
			if (savedGame.ScenarioData.TradeRouteScenarioData != null)
			{
				TradeRouteScenarioWorld tradeRouteScenarioWorld = (TradeRouteScenarioWorld)EngineASX.Instance.World;
				tradeRouteScenarioWorld.NumBlackSailShipsDestroyed = savedGame.ScenarioData.TradeRouteScenarioData.NumBlackSailShipsDestroyed;
				tradeRouteScenarioWorld.PirateFaction = engine.GetFactionByid(savedGame.ScenarioData.TradeRouteScenarioData.PirateFaction?.Id ?? (-1));
			}
			EngineASX.Instance.World.ScenarioOptions.RespawnOnDeath = savedGame.ScenarioData.RespawnOnDeath;
			EngineASX.Instance.World.ScenarioOptions.AllowTeleporting = savedGame.ScenarioData.AllowTeleporting;
			EngineASX.Instance.World.ScenarioOptions.Permadeath = savedGame.ScenarioData.Permadeath;
			EngineASX.Instance.World.ScenarioOptions.AsteroidRespawningEnabled = savedGame.ScenarioData.AsteroidRespawningEnabled;
			EngineASX.Instance.World.ScenarioOptions.AsteroidRespawnTime = Mathf.Clamp01(savedGame.ScenarioData.AsteroidRespawnTime);
			EngineASX.Instance.World.ScenarioOptions.AllowStationCapture = savedGame.ScenarioData.AllowStationCapture;
			EngineASX.Instance.World.ScenarioOptions.AllowAbandonShip = savedGame.ScenarioData.AllowAbandonShip;
			EngineASX.Instance.World.HasCheated = (double)savedGame.ScenarioData.NextProcessOtherEventsTime >= savedGame.Header.SecondsElapsed + 11.0;
		}

		private void ImportRandomEventsController()
		{
			EngineASX.Instance.RandomEventsController.gameObject.SetActive(savedGame.ScenarioData.HasRandomEvents);
			EngineASX.Instance.RandomEventsController.NextEventTime = savedGame.ScenarioData.NextRandomEventTime;
		}

		private void ImportFactionSpawner()
		{
			bool flag = savedGame.ScenarioData.FactionSpawner != null;
			EngineASX.Instance.World.ScenarioOptions.FactionSpawningEnabled = flag;
			if (flag)
			{
				EngineASX.Instance.FactionSpawner.NextFactionSpawnTime = savedGame.ScenarioData.FactionSpawner.NextUpdate;
				EngineASX.Instance.FactionSpawner.NextFreelancerSpawnTime = EngineASX.Instance.ScenarioElapsedTime + (double)(UnityEngine.Random.value * EngineASX.Instance.FactionSpawner.Settings.TimeBetweenMinorFactionSpawns);
			}
		}

		private void ImportPatrolPaths()
		{
			foreach (ModelSectorPatrolPath patrolPath in savedGame.PatrolPaths)
			{
				ImportPatrolPath(patrolPath);
			}
		}

		private void ImportPatrolPath(ModelSectorPatrolPath modelSectorPatrolPath)
		{
			AIPatrolPath aIPatrolPath = UnityObjectHelper.NewGameObject<AIPatrolPath>();
			aIPatrolPath.UniqueId = modelSectorPatrolPath.Id;
			aIPatrolPath.Sector = engine.GetSectorById(modelSectorPatrolPath.Sector?.Id ?? (-1));
			aIPatrolPath.IsLoop = modelSectorPatrolPath.IsLoop;
			aIPatrolPath.name = "AIPatrolPath";
			foreach (ModelSectorPatrolPathNode node in modelSectorPatrolPath.Nodes)
			{
				AIPatrolPathNodeBehaviour aIPatrolPathNodeBehaviour = UnityObjectHelper.NewGameObject<AIPatrolPathNodeBehaviour>(aIPatrolPath);
				aIPatrolPathNodeBehaviour.transform.SetParent(aIPatrolPath.transform, worldPositionStays: false);
				aIPatrolPathNodeBehaviour.transform.localPosition = node.SectorPosition.ToVector3();
				aIPatrolPathNodeBehaviour.Order = node.Order;
			}
			aIPatrolPath.Init();
		}

		public void ImportFleetOrders(Fleet fleet, ModelFleet modelFleet)
		{
			if (fleet == null)
			{
				throw new ArgumentNullException("fleet");
			}
			fleetOrderCache.Clear();
			foreach (ModelFleetOrder order in modelFleet.OrdersCollection.Orders)
			{
				FleetOrder fleetOrder = ImportFleetOrder(order, fleet);
				if (fleetOrder == null)
				{
					throw new Exception("Failed to read fleet order");
				}
				fleetOrder.transform.SetParent(fleet.transform);
				fleetOrder.transform.localPosition = Vector3.zero;
				fleetOrderCache.Add(fleetOrder);
				fleetOrder.Init();
			}
			foreach (ModelFleetOrder queuedOrder in modelFleet.OrdersCollection.QueuedOrders)
			{
				int num = modelFleet.OrdersCollection.Orders.IndexOf(queuedOrder);
				if (num > -1 && num < fleetOrderCache.Count)
				{
					fleet.OrderQueue.Add(fleetOrderCache[num]);
					continue;
				}
				Debug.LogErrorFormat(fleet, "Invalid Queued objective index {0} for Fleet {1}", num, fleet.UniqueId);
			}
			if (modelFleet.OrdersCollection.CurrentOrder != null)
			{
				int num2 = modelFleet.OrdersCollection.Orders.IndexOf(modelFleet.OrdersCollection.CurrentOrder.Order);
				if (num2 > -1 && num2 < fleetOrderCache.Count)
				{
					FleetOrder fleetOrder2 = fleetOrderCache[num2];
					ActiveFleetOrder activeOrder = ImportActiveFleetOrder(fleet, fleetOrder2, modelFleet.OrdersCollection.CurrentOrder);
					fleet.ActiveOrder = activeOrder;
					if (fleet.ActiveOrder != null)
					{
						fleet.ActiveOrder.ResetTargetPosition();
					}
				}
				else
				{
					Debug.LogErrorFormat(fleet, "Invalid Current fleet order index {0} for Fleet ID {1}", num2, fleet.UniqueId);
				}
			}
			fleetOrderCache.Clear();
		}

		public FleetOrder ImportFleetOrder(ModelFleetOrder modelFleetOrder, Fleet fleet)
		{
			FleetOrderType orderType = modelFleetOrder.OrderType;
			FleetOrder fleetOrder = SaveGameUtilities.CreateFleetOrderFromType(orderType);
			if (fleetOrder != null)
			{
				fleetOrder.UniqueId = modelFleetOrder.Id;
				fleetOrder.Init();
				fleetOrder.CompletionMode = modelFleetOrder.CompletionMode;
				fleetOrder.AllowCombatInterception = modelFleetOrder.AllowCombatInterception;
				fleetOrder.PreferCloak = modelFleetOrder.CloakPreference;
				fleetOrder.MaxJumpDistance = modelFleetOrder.MaxJumpDistance;
				fleetOrder.AllowTimeout = modelFleetOrder.AllowTimeout;
				fleetOrder.TimeoutTime = modelFleetOrder.TimeoutTime;
				fleetOrder.MaxDuration = modelFleetOrder.MaxDuration;
				fleetOrder.Priority = modelFleetOrder.Priority;
				fleetOrder.Notifications = modelFleetOrder.Notifications;
				if (modelFleetOrder.AvailableCredits > 0)
				{
					fleetOrder.Credits = modelFleetOrder.AvailableCredits;
					fleetOrder.SetAsRestrictedCreditsSpend(fleet);
				}
				else
				{
					fleetOrder.Credits = 0;
					fleetOrder.HasUnlimitedSpend = true;
				}
				switch (fleetOrder.OrderType)
				{
				case FleetOrderType.AttackGroup:
				{
					AttackFleetOrder attackFleetOrder = (AttackFleetOrder)fleetOrder;
					ModelAttackFleetOrder modelAttackFleetOrder = (ModelAttackFleetOrder)modelFleetOrder;
					attackFleetOrder.Target = engine.GetFleetByid(modelAttackFleetOrder.Target?.Id ?? (-1));
					attackFleetOrder.AttackPriority = modelAttackFleetOrder.AttackPriority;
					break;
				}
				case FleetOrderType.CollectCargo:
				{
					CollectCargoOrder collectCargoOrder = (CollectCargoOrder)fleetOrder;
					ModelCollectCargoOrder modelCollectCargoOrder = (ModelCollectCargoOrder)modelFleetOrder;
					collectCargoOrder.TargetUnit = engine.GetUnitByid(modelCollectCargoOrder.TargetUnit?.Id ?? (-1));
					break;
				}
				case FleetOrderType.Scavenge:
				{
					ScavengeOrder scavengeOrder = (ScavengeOrder)fleetOrder;
					ModelScavengeOrder modelScavengeOrder = (ModelScavengeOrder)modelFleetOrder;
					scavengeOrder.TargetSector = engine.GetSectorById(modelScavengeOrder.TargetSector?.Id ?? (-1));
					scavengeOrder.CollectOwnerMode = modelScavengeOrder.CollectOwnerMode;
					break;
				}
				case FleetOrderType.Mine:
				{
					MineOrder mineOrder = (MineOrder)fleetOrder;
					ModelMineOrder modelMineOrder = (ModelMineOrder)modelFleetOrder;
					mineOrder.TargetSector = engine.GetSectorById(modelMineOrder.TargetSector?.Id ?? (-1));
					mineOrder.CollectOwnerMode = modelMineOrder.CollectOwnerMode;
					Unit unitByid = engine.GetUnitByid(modelMineOrder.ManualMineTarget?.Id ?? (-1));
					if (unitByid != null)
					{
						mineOrder.ManualMineTarget = unitByid.Asteroid;
					}
					break;
				}
				case FleetOrderType.Dock:
				{
					DockOrder dockOrder = (DockOrder)fleetOrder;
					ModelDockOrder modelDockOrder = (ModelDockOrder)modelFleetOrder;
					dockOrder.TargetDock = engine.GetUnitByid(modelDockOrder.TargetDock?.Id ?? (-1));
					break;
				}
				case FleetOrderType.Patrol:
				{
					PatrolOrder patrolOrder = (PatrolOrder)fleetOrder;
					ModelPatrolOrder modelPatrolOrder = (ModelPatrolOrder)modelFleetOrder;
					patrolOrder.PathDirection = modelPatrolOrder.PathDirection;
					patrolOrder.IsLooping = modelPatrolOrder.IsLooping;
					foreach (ModelPatrolPathNode node in modelPatrolOrder.Nodes)
					{
						AIPatrolPathNode aIPatrolPathNode = new AIPatrolPathNode();
						aIPatrolPathNode.Sector = engine.GetSectorById(node.Sector?.Id ?? (-1));
						aIPatrolPathNode.SectorPosition = node.SectorPosition.ToVector3();
						patrolOrder.Nodes.Add(aIPatrolPathNode);
					}
					patrolOrder.IsLoop = modelPatrolOrder.IsLoop;
					break;
				}
				case FleetOrderType.PatrolPath:
				{
					PatrolPathOrder patrolPathOrder = (PatrolPathOrder)fleetOrder;
					ModelPatrolPathOrder modelPatrolPathOrder = (ModelPatrolPathOrder)modelFleetOrder;
					patrolPathOrder.PathDirection = modelPatrolPathOrder.PathDirection;
					patrolPathOrder.IsLooping = modelPatrolPathOrder.IsLooping;
					patrolPathOrder.PatrolPath = engine.GetPatrolPathByid(modelPatrolPathOrder.PatrolPath?.Id ?? (-1));
					break;
				}
				case FleetOrderType.Wait:
				{
					WaitOrder waitOrder = (WaitOrder)fleetOrder;
					ModelWaitOrder modelWaitOrder = (ModelWaitOrder)modelFleetOrder;
					waitOrder.WaitTime = modelWaitOrder.WaitTime;
					break;
				}
				case FleetOrderType.AttackTarget:
				{
					AttackTargetOrder attackTargetOrder = (AttackTargetOrder)fleetOrder;
					ModelAttackTargetOrder modelAttackTargetOrder = (ModelAttackTargetOrder)modelFleetOrder;
					attackTargetOrder.TargetUnit = engine.GetUnitByid(modelAttackTargetOrder.TargetUnit?.Id ?? (-1));
					attackTargetOrder.AttackPriority = modelAttackTargetOrder.AttackPriority;
					break;
				}
				case FleetOrderType.Trade:
				{
					TradeOrder tradeOrder = (TradeOrder)fleetOrder;
					ModelTradeOrder modelTradeOrder = (ModelTradeOrder)modelFleetOrder;
					tradeOrder.MinBuyQuantity = modelTradeOrder.MinBuyQuantity;
					tradeOrder.MinBuyCargoPercentage = modelTradeOrder.MinBuyCargoPercentage;
					break;
				}
				case FleetOrderType.ManualTrade:
				{
					ManualTradeOrder manualTradeOrder = (ManualTradeOrder)fleetOrder;
					ModelManualTradeOrder modelManualTradeOrder = (ModelManualTradeOrder)modelFleetOrder;
					manualTradeOrder.MinBuyQuantity = modelManualTradeOrder.MinBuyQuantity;
					manualTradeOrder.MinBuyCargoPercentage = modelManualTradeOrder.MinBuyCargoPercentage;
					if (modelManualTradeOrder.CustomTradeRoute != null)
					{
						manualTradeOrder.CustomTradeRoute = ImportTradeRoute(modelManualTradeOrder.CustomTradeRoute);
					}
					break;
				}
				case FleetOrderType.AutonomousTrade:
				{
					AutonomousTradeOrder autonomousTradeOrder = (AutonomousTradeOrder)fleetOrder;
					ModelUniverseTradeOrder modelUniverseTradeOrder = (ModelUniverseTradeOrder)modelFleetOrder;
					autonomousTradeOrder.MinBuyQuantity = modelUniverseTradeOrder.MinBuyQuantity;
					autonomousTradeOrder.MinBuyCargoPercentage = modelUniverseTradeOrder.MinBuyCargoPercentage;
					autonomousTradeOrder.TradeOnlySpecificCargoTypes = modelUniverseTradeOrder.TradeOnlySpecificCargoClasses;
					autonomousTradeOrder.TradeSpecificCargoTypes.Clear();
					foreach (ModelCargoClass tradeSpecificCargoClass in modelUniverseTradeOrder.TradeSpecificCargoClasses)
					{
						CargoClass cargoClassById = engine.GetCargoClassById((int)tradeSpecificCargoClass);
						if (cargoClassById != null)
						{
							autonomousTradeOrder.TradeSpecificCargoTypes.Add(cargoClassById);
						}
					}
					break;
				}
				case FleetOrderType.JoinFleet:
				{
					JoinFleetOrder joinFleetOrder = (JoinFleetOrder)fleetOrder;
					ModelJoinFleetOrder modelJoinFleetOrder = (ModelJoinFleetOrder)modelFleetOrder;
					joinFleetOrder.TargetFleet = engine.GetFleetByid(modelJoinFleetOrder.TargetFleet?.Id ?? (-1));
					break;
				}
				case FleetOrderType.MoveTo:
				{
					MoveToOrder moveToOrder = (MoveToOrder)fleetOrder;
					ModelMoveToOrder modelMoveToOrder = (ModelMoveToOrder)modelFleetOrder;
					moveToOrder.CompleteOnReachTarget = modelMoveToOrder.CompleteOnReachTarget;
					moveToOrder.ArrivalThreshold = modelMoveToOrder.ArrivalThreshold;
					moveToOrder.MatchTargetOrientation = modelMoveToOrder.MatchTargetOrientation;
					moveToOrder.PreferredRelativeVectorFromTarget = modelMoveToOrder.PreferredRelativeVectorFromTarget?.ToVector3();
					moveToOrder.Target = ImportSectorTarget(modelMoveToOrder.Target);
					break;
				}
				case FleetOrderType.Protect:
				{
					ProtectOrder protectOrder = (ProtectOrder)fleetOrder;
					ModelProtectOrder modelProtectOrder = (ModelProtectOrder)modelFleetOrder;
					protectOrder.CompleteOnReachTarget = modelProtectOrder.CompleteOnReachTarget;
					protectOrder.ArrivalThreshold = modelProtectOrder.ArrivalThreshold;
					protectOrder.MatchTargetOrientation = modelProtectOrder.MatchTargetOrientation;
					protectOrder.PreferredRelativeVectorFromTarget = modelProtectOrder.PreferredRelativeVectorFromTarget?.ToVector3();
					protectOrder.Target = ImportSectorTarget(modelProtectOrder.Target);
					break;
				}
				case FleetOrderType.SellCargo:
				{
					SellCargoOrder sellCargoOrder = (SellCargoOrder)fleetOrder;
					ModelSellCargoOrder modelSellCargoOrder = (ModelSellCargoOrder)modelFleetOrder;
					sellCargoOrder.FreeUnitsCompleteThreshold = modelSellCargoOrder.FreeUnitsCompleteThreshold;
					sellCargoOrder.MinBuyPriceMultiplier = modelSellCargoOrder.MinBuyPriceMultiplier;
					sellCargoOrder.SellOnlyListedCargos = modelSellCargoOrder.SellOnlyListedCargos;
					sellCargoOrder.CompleteWhenNoBuyerFound = modelSellCargoOrder.CompleteWhenNoBuyerFound;
					sellCargoOrder.CompleteWhenNoCargoToSell = modelSellCargoOrder.CompleteWhenNoCargoToSell;
					sellCargoOrder.ManualBuyer = GetCargoTraderFromUnit(modelSellCargoOrder.ManualBuyerUnit);
					sellCargoOrder.CustomSellCargoTime = modelSellCargoOrder.CustomSellCargoTime;
					sellCargoOrder.SellCargoClasses.Clear();
					foreach (ModelCargoClass sellCargoClass in modelSellCargoOrder.SellCargoClasses)
					{
						CargoClass cargoClassById2 = engine.GetCargoClassById((int)sellCargoClass);
						if (cargoClassById2 != null)
						{
							sellCargoOrder.SellCargoClasses.Add(cargoClassById2);
						}
					}
					sellCargoOrder.SellEquipment = modelSellCargoOrder.SellEquipment;
					break;
				}
				case FleetOrderType.ManualRepair:
				{
					ManualRepairFleetOrder manualRepairFleetOrder = (ManualRepairFleetOrder)fleetOrder;
					ModelManualRepairFleetOrder modelManualRepairFleetOrder = (ModelManualRepairFleetOrder)modelFleetOrder;
					manualRepairFleetOrder.InsufficientCreditsMode = modelManualRepairFleetOrder.InsufficientCreditsMode;
					manualRepairFleetOrder.SpecificRepairLocation = engine.GetUnitByid(modelManualRepairFleetOrder.RepairLocationUnit?.Id ?? (-1));
					break;
				}
				case FleetOrderType.ManualRearm:
				{
					ManualRearmOrder manualRearmOrder = (ManualRearmOrder)fleetOrder;
					ModelManualRearmFleetOrder modelManualRearmFleetOrder = (ModelManualRearmFleetOrder)modelFleetOrder;
					manualRearmOrder.EquipmentUsage = modelManualRearmFleetOrder.EquipmentCargoUsage;
					manualRearmOrder.InsufficientCreditsMode = modelManualRearmFleetOrder.InsufficientCreditsMode;
					manualRearmOrder.SpecificRearmLocation = engine.GetUnitByid(modelManualRearmFleetOrder.RearmLocationUnit?.Id ?? (-1));
					break;
				}
				case FleetOrderType.RearmAtNearest:
				{
					RearmAtNearestOrder rearmAtNearestOrder = (RearmAtNearestOrder)fleetOrder;
					ModelRearmAtNearestFleetOrder modelRearmAtNearestFleetOrder = (ModelRearmAtNearestFleetOrder)modelFleetOrder;
					rearmAtNearestOrder.EquipmentUsage = modelRearmAtNearestFleetOrder.EquipmentCargoUsage;
					rearmAtNearestOrder.InsufficientCreditsMode = modelRearmAtNearestFleetOrder.InsufficientCreditsMode;
					break;
				}
				case FleetOrderType.RepairAtNearest:
				{
					RepairAtNearestStationOrder repairAtNearestStationOrder = (RepairAtNearestStationOrder)fleetOrder;
					ModelRepairAtNearestStationOrder modelRepairAtNearestStationOrder = (ModelRepairAtNearestStationOrder)modelFleetOrder;
					repairAtNearestStationOrder.InsufficientCreditsMode = modelRepairAtNearestStationOrder.InsufficientCreditsMode;
					break;
				}
				case FleetOrderType.MoveToNearestFriendlyStation:
				{
					MoveToNearestFriendlyStationOrder moveToNearestFriendlyStationOrder = (MoveToNearestFriendlyStationOrder)fleetOrder;
					ModelMoveToNearestFriendlyStationOrder modelMoveToNearestFriendlyStationOrder = (ModelMoveToNearestFriendlyStationOrder)modelFleetOrder;
					moveToNearestFriendlyStationOrder.CompleteOnReachTarget = modelMoveToNearestFriendlyStationOrder.CompleteOnReachTarget;
					break;
				}
				case FleetOrderType.EnterWormhole:
				{
					EnterWormholeOrder enterWormholeOrder = (EnterWormholeOrder)fleetOrder;
					ModelEnterWormholeOrder modelEnterWormholeOrder = (ModelEnterWormholeOrder)modelFleetOrder;
					Unit unitByid2 = engine.GetUnitByid(modelEnterWormholeOrder.TargetWormhole?.Id ?? (-1));
					if (unitByid2 != null)
					{
						enterWormholeOrder.TargetWormhole = unitByid2.WormholeComponent;
					}
					break;
				}
				case FleetOrderType.ExploreSector:
				{
					ExploreSectorOrder exploreSectorOrder = (ExploreSectorOrder)fleetOrder;
					ModelExploreSectorOrder modelExploreSectorOrder = (ModelExploreSectorOrder)modelFleetOrder;
					exploreSectorOrder.Sector = engine.GetSectorById(modelExploreSectorOrder.Sector?.Id ?? (-1));
					break;
				}
				case FleetOrderType.MoveToSector:
				{
					MoveToSectorOrder moveToSectorOrder = (MoveToSectorOrder)fleetOrder;
					ModelMoveToSectorOrder modelMoveToSectorOrder = (ModelMoveToSectorOrder)modelFleetOrder;
					moveToSectorOrder.TargetSector = engine.GetSectorById(modelMoveToSectorOrder.TargetSector?.Id ?? (-1));
					break;
				}
				case FleetOrderType.WaitForAutoRepair:
				{
					WaitForAutoRepairOrder waitForAutoRepairOrder = (WaitForAutoRepairOrder)fleetOrder;
					ModelWaitForAutoRepairOrder modelWaitForAutoRepairOrder = (ModelWaitForAutoRepairOrder)modelFleetOrder;
					waitForAutoRepairOrder.HullConditionThreshold = modelWaitForAutoRepairOrder.HullConditionThreshold;
					waitForAutoRepairOrder.ComponentsConditionThreshold = modelWaitForAutoRepairOrder.ComponentsConditionThreshold;
					waitForAutoRepairOrder.ShieldConditionThreshold = modelWaitForAutoRepairOrder.ShieldConditionThreshold;
					break;
				}
				case FleetOrderType.BuildStation:
				{
					BuildStationOrder buildStationOrder = (BuildStationOrder)fleetOrder;
					ModelBuildStationOrder modelBuildStationOrder = (ModelBuildStationOrder)modelFleetOrder;
					buildStationOrder.UnitClass = EngineASX.Instance.GetUnitClassById((int)modelBuildStationOrder.UnitClass);
					buildStationOrder.Sector = EngineASX.Instance.GetSectorById(modelBuildStationOrder.Sector?.Id ?? (-1));
					buildStationOrder.SectorPosition = modelBuildStationOrder.SectorPosition.ToVector3();
					buildStationOrder.InsufficientCreditsMode = modelBuildStationOrder.InsufficientCreditsMode;
					break;
				}
				case FleetOrderType.ClaimUnit:
				{
					ClaimUnitOrder claimUnitOrder = (ClaimUnitOrder)fleetOrder;
					ModelClaimUnitOrder modelClaimUnitOrder = (ModelClaimUnitOrder)modelFleetOrder;
					claimUnitOrder.TargetUnit = GetUnit(modelClaimUnitOrder.Unit);
					break;
				}
				default:
					Debug.LogErrorFormat("Unable to read data for fleet order of type {0}. Does it have any data", fleetOrder.OrderType);
					break;
				case FleetOrderType.RTB:
				case FleetOrderType.DisposeCargo:
				case FleetOrderType.AutonomousTransportPassengers:
				case FleetOrderType.AutonomousRoamLocationsObjective:
				case FleetOrderType.AutonomousBountyHunterObjective:
				case FleetOrderType.Explore:
				case FleetOrderType.Undock:
					break;
				}
				return fleetOrder;
			}
			Debug.LogError($"Could not read fleet order of type: {orderType}");
			return null;
		}

		public AITradeRoute ImportTradeRoute(ModelCustomTradeRoute modelCustomTradeRoute)
		{
			AITradeRoute aITradeRoute = new AITradeRoute
			{
				CargoClass = engine.GetCargoClassById((int)modelCustomTradeRoute.CargoClass),
				BuyLocation = GetCargoTraderFromUnit(modelCustomTradeRoute.BuyLocation),
				SellLocation = GetCargoTraderFromUnit(modelCustomTradeRoute.SellLocation),
				BuyPriceMultiplier = modelCustomTradeRoute.BuyPriceMultiplier
			};
			_ = aITradeRoute.CargoClass == null;
			return aITradeRoute;
		}

		public CargoTrader GetCargoTraderFromUnit(ModelUnit modelUnit)
		{
			Unit unitByid = engine.GetUnitByid(modelUnit?.Id ?? (-1));
			if (unitByid == null)
			{
				return null;
			}
			return unitByid.GetComponent<CargoTrader>();
		}

		public void ImportFactionRelations()
		{
			foreach (ModelFaction faction in savedGame.Factions)
			{
				Faction factionByid = engine.GetFactionByid(faction?.Id ?? (-1));
				if (factionByid == null)
				{
					continue;
				}
				foreach (ModelFactionRelationDataItem item in faction.Relations.Items)
				{
					Faction factionByid2 = engine.GetFactionByid(item.OtherFaction?.Id ?? (-1));
					if (factionByid2 != null && factionByid2 != factionByid)
					{
						FactionAttitude orCreateAttitude = factionByid.GetOrCreateAttitude(factionByid2);
						if (orCreateAttitude != null)
						{
							orCreateAttitude.PermanentPeace = item.PermanentPeace;
							orCreateAttitude.RestrictHostilityTimeout = item.RestrictHostilityTimeout;
							orCreateAttitude.Neutrality = item.Neutrality;
							orCreateAttitude.HostilityEndTime = item.HostilityEndTime;
						}
					}
					else
					{
						Debug.LogWarning($"EngineIO: Cannot apply attitude to non-existant faction");
					}
				}
				foreach (ModelFactionRecentDamageItem recentDamageItem in faction.RecentDamageItems)
				{
					Faction factionByid3 = engine.GetFactionByid(recentDamageItem.OtherFaction?.Id ?? (-1));
					if (factionByid3 != null)
					{
						factionByid.GetOrCreateRecentDamageItem(factionByid3).RecentDamageReceived = recentDamageItem.RecentDamageReceived;
					}
				}
			}
		}

		private void ImportHudData()
		{
			engine.Hud.CurrentTarget = engine.GetUnitByid(savedGame.CurrentHudTarget?.Id ?? (-1));
		}

		private void ImportGamePlayer(GamePlayer gamePlayer, ModelPlayer modelPlayer)
		{
			foreach (ModelUnit visitedUnit in modelPlayer.VisitedUnits)
			{
				Unit unitByid = engine.GetUnitByid(visitedUnit?.Id ?? (-1));
				if (unitByid != null)
				{
					gamePlayer.RegisterUnitVisited(unitByid);
				}
			}
			foreach (ModelPlayerMessage message in modelPlayer.Messages)
			{
				PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage();
				ImportPlayerActiveMessage(playerActiveMessage, message);
				gamePlayer.MessagesById.Add(playerActiveMessage.UniqueId, playerActiveMessage);
				gamePlayer.MessageList.Add(playerActiveMessage);
			}
			foreach (ModelPlayerDelayedMessage delayedMessage in modelPlayer.DelayedMessages)
			{
				GamePlayer.delayedMessage item = new GamePlayer.delayedMessage
				{
					ShowTime = delayedMessage.ShowTime,
					Important = delayedMessage.Important,
					Notifications = delayedMessage.Notifications
				};
				PlayerActiveMessage playerActiveMessage2 = new PlayerActiveMessage();
				ImportPlayerActiveMessage(playerActiveMessage2, delayedMessage.Message);
				item.Message = playerActiveMessage2;
				gamePlayer.DelayedMessages.Add(item);
			}
			ImportPlayerWaypointController(gamePlayer.WaypointController, savedGame.Player.CustomWaypoint);
			if (savedGame.Player.ActiveJob != null)
			{
				int? num = savedGame.Player.ActiveJob?.Id;
				Mission missionById = engine.GetMissionById(savedGame.Player.ActiveJob?.Id ?? (-1));
				if (missionById != null)
				{
					if (missionById.IsFinished)
					{
						Debug.LogWarningFormat(gamePlayer, "GamePlayer.LoadBinary: Attempting to activate guidance on completed mission. MissionId: {0}", num);
					}
					else
					{
						gamePlayer.ActiveMission = missionById;
					}
				}
				else
				{
					Debug.LogWarningFormat(gamePlayer, "GamePlayer.LoadBinary: MissionId: {0} is invalid", num);
				}
			}
			if (savedGame.Player.Stats != null)
			{
				ImportPlayerStats(gamePlayer, savedGame.Player.Stats);
			}
		}

		private void ImportPlayerStats(GamePlayer gamePlayer, ModelPlayerStats modelPlayerStats)
		{
			gamePlayer.Stats.ClearVisitedSectors();
			foreach (ModelSector item in modelPlayerStats.SectorsVisited)
			{
				Sector sectorById = engine.GetSectorById(item?.Id ?? (-1));
				if (sectorById != null)
				{
					gamePlayer.Stats.AddVisitedSector(sectorById);
				}
			}
			gamePlayer.Stats.TotalBountyClaimed = modelPlayerStats.TotalBountyClaimed;
			gamePlayer.Stats.ShipsMinedToDeath = modelPlayerStats.ShipsMinedToDeath;
		}

		public void ImportPlayerActiveMessage(PlayerActiveMessage playerActiveMessage, ModelPlayerMessage modelPlayerMessage)
		{
			playerActiveMessage.UniqueId = modelPlayerMessage.Id;
			playerActiveMessage.EngineTimeStamp = modelPlayerMessage.EngineTimeStamp;
			playerActiveMessage.AllowDelete = modelPlayerMessage.AllowDelete;
			playerActiveMessage.Opened = modelPlayerMessage.Opened;
			playerActiveMessage.SenderUnit = engine.GetUnitByid(modelPlayerMessage.SenderUnit?.Id ?? (-1));
			playerActiveMessage.SenderUnitSector = engine.GetSectorById(modelPlayerMessage.SenderUnitSector?.Id ?? (-1));
			playerActiveMessage.SenderUnitSectorPosition = modelPlayerMessage.SenderUnitSectorPosition.ToVector3();
			playerActiveMessage.SubjectUnit = engine.GetUnitByid(modelPlayerMessage.SubjectUnit?.Id ?? (-1));
			playerActiveMessage.SubjectUnitSector = engine.GetSectorById(modelPlayerMessage.SubjectUnitSector?.Id ?? (-1));
			playerActiveMessage.SubjectUnitSectorPosition = modelPlayerMessage.SubjectUnitSectorPosition.ToVector3();
			playerActiveMessage.MessageTemplate = engine.World.GetMessageDataById(playerActiveMessage.MessageTemplate?.UniqueId ?? (-1));
			playerActiveMessage.ToText = modelPlayerMessage.ToText;
			playerActiveMessage.FromText = modelPlayerMessage.FromText;
			playerActiveMessage.MessageText = modelPlayerMessage.MessageText;
			playerActiveMessage.SubjectText = modelPlayerMessage.SubjectText;
		}

		private void ImportPlayerWaypointController(PlayerWaypointController waypointController, ModelPlayerWaypoint modelPlayerWaypoint)
		{
			if (modelPlayerWaypoint != null)
			{
				PlayerWaypoint value = ImportPlayerWaypoint(modelPlayerWaypoint);
				waypointController.CustomPath.Waypoint = value;
			}
		}

		private PlayerWaypoint ImportPlayerWaypoint(ModelPlayerWaypoint modelPlayerWaypoint)
		{
			return new PlayerWaypoint
			{
				SectorPosition = modelPlayerWaypoint.SectorPosition.ToVector3(),
				Sector = engine.GetSectorById(modelPlayerWaypoint.Sector?.Id ?? (-1)),
				TargetUnit = engine.GetUnitByid(modelPlayerWaypoint.TargetUnit?.Id ?? (-1)),
				HadSceneObject = modelPlayerWaypoint.HadTargetObject
			};
		}

		private void ImportFleetSpawners()
		{
			foreach (ModelFleetSpawner fleetSpawner in savedGame.FleetSpawners)
			{
				ImportFleetSpawner(fleetSpawner);
			}
		}

		private FleetSpawner ImportFleetSpawner(ModelFleetSpawner modelFleetSpawner)
		{
			FleetSpawner fleetSpawner = UnityObjectHelper.NewGameObject<FleetSpawner>();
			fleetSpawner.name = modelFleetSpawner.Name;
			Vector3 sectorPosition = modelFleetSpawner.Position.ToVector3();
			fleetSpawner.transform.rotation = modelFleetSpawner.Rotation.ToQuaternion();
			fleetSpawner.InitialSpawnTimeRandomness = modelFleetSpawner.InitialSpawnTimeRandomness;
			fleetSpawner.SpawnTimeRandomness = modelFleetSpawner.SpawnTimeRandomness;
			fleetSpawner.ShipDesignation = modelFleetSpawner.ShipDesignation;
			fleetSpawner.ShipName = modelFleetSpawner.ShipName;
			fleetSpawner.NamePrefix = modelFleetSpawner.NamePrefix;
			fleetSpawner.SpawnCounter = modelFleetSpawner.SpawnCounter;
			fleetSpawner.RespawnWhenNoObjectives = modelFleetSpawner.RespawnWhenNoObjectives;
			fleetSpawner.RespawnWhenNoPilots = modelFleetSpawner.RespawnWhenNoPilots;
			fleetSpawner.AllowRespawnInActiveScene = modelFleetSpawner.AllowRespawnInActiveScene;
			fleetSpawner.HomeBaseUnit = engine.GetUnitByid(modelFleetSpawner.FleetHomeBase?.Id ?? (-1));
			fleetSpawner.HomeSector = engine.GetSectorById(modelFleetSpawner.FleetHomeSector?.Id ?? (-1));
			fleetSpawner.Faction = engine.GetFactionByid(modelFleetSpawner.OwnerFaction?.Id ?? (-1));
			fleetSpawner.Sector = engine.GetSectorById(modelFleetSpawner.Sector?.Id ?? (-1));
			fleetSpawner.SpawnDock = engine.GetUnitByid(modelFleetSpawner.SpawnDock?.Id ?? (-1));
			fleetSpawner.NextSpawnTime = modelFleetSpawner.NextSpawnTime;
			fleetSpawner.MinTimeBeforeSpawn = modelFleetSpawner.MinTimeBeforeSpawn;
			fleetSpawner.MaxTimeBeforeSpawn = modelFleetSpawner.MaxTimeBeforeSpawn;
			fleetSpawner.MinUnitCount = modelFleetSpawner.MinGroupUnitCount;
			fleetSpawner.MaxUnitCount = modelFleetSpawner.MaxGroupUnitCount;
			fleetSpawner.SpawnedGroup = engine.GetFleetByid(modelFleetSpawner.SpawnedFleet?.Id ?? (-1));
			if (fleetSpawner.SpawnDock != null)
			{
				fleetSpawner.transform.SetParent(fleetSpawner.SpawnDock.transform);
			}
			else if (fleetSpawner.Sector != null)
			{
				fleetSpawner.transform.SetParent(fleetSpawner.Sector.transform);
			}
			fleetSpawner.transform.localPosition = fleetSpawner.Sector.ToWorldPosition(sectorPosition);
			foreach (ModelUnitClass unitClass in modelFleetSpawner.UnitClasses)
			{
				UnitClass unitClassById = engine.GetUnitClassById((int)unitClass);
				if (unitClassById != null)
				{
					fleetSpawner.UnitClasses.Add(unitClassById);
				}
			}
			foreach (string pilotResourceName in modelFleetSpawner.PilotResourceNames)
			{
				Person person = EngineASX.LoadPerson(pilotResourceName);
				if (person != null)
				{
					fleetSpawner.PilotPrefabs.Add(person);
				}
			}
			string fleetResourceName = modelFleetSpawner.FleetResourceName;
			fleetSpawner.Prefab = EngineASX.LoadFleet(fleetResourceName);
			foreach (ModelFleetOrder order in modelFleetSpawner.Orders)
			{
				FleetOrder fleetOrder = ImportFleetOrder(order, null);
				fleetOrder.transform.SetParent(fleetSpawner.transform);
				fleetOrder.transform.localPosition = Vector3.zero;
			}
			fleetSpawner.Init();
			return fleetSpawner;
		}

		private void ImportFleets()
		{
			foreach (ModelFleet fleet2 in savedGame.Fleets)
			{
				Fleet fleet = UnityObjectHelper.NewGameObject<Fleet>();
				ImportFleet(fleet, fleet2);
			}
		}

		private void ImportPlayerFleetSettings()
		{
			foreach (ModelFleet fleet in savedGame.Fleets)
			{
				if (fleet.FleetSettings.PlayerFleetSettings != null)
				{
					Fleet fleetByid = engine.GetFleetByid(fleet.Id);
					if (fleetByid != null)
					{
						ImportPlayerFleetSettings(fleet.FleetSettings.PlayerFleetSettings, fleetByid.Settings);
					}
				}
			}
		}

		public static void ImportPlayerFleetSettings(ModelPlayerFleetSettings modelPlayerFleetSettings, FleetSettings fleetSettings)
		{
			fleetSettings.NotifyWhenOrderComplete = modelPlayerFleetSettings.NotifyWhenOrderComplete;
			fleetSettings.NotifyWhenScannedHostile = modelPlayerFleetSettings.NotifyWhenScannedHostile;
			fleetSettings.NotifyWhenAbandonedUnitFound = modelPlayerFleetSettings.NotifyWhenAbandonedUnitFound;
			fleetSettings.NotifyWhenAbandonedCargoFound = modelPlayerFleetSettings.NotifyWhenAbandonedCargoFound;
		}

		private void ImportFleetOrders()
		{
			foreach (ModelFleet fleet in savedGame.Fleets)
			{
				Fleet fleetByid = engine.GetFleetByid(fleet?.Id ?? (-1));
				if (fleetByid == null)
				{
					Debug.LogError($"Unknown fleet. Id: {fleet?.Id}");
				}
				else
				{
					ImportFleetOrders(fleetByid, fleet);
				}
			}
		}

		private void ImportFleet(Fleet fleet, ModelFleet modelFleet)
		{
			fleet.gameObject.SetActive(modelFleet.IsActive);
			fleet.UniqueId = modelFleet.Id;
			fleet.Seed = modelFleet.Seed;
			fleet.Init();
			Vector3 localPosition = modelFleet.Position.ToVector3();
			Quaternion localRotation = modelFleet.Rotation.ToQuaternion();
			fleet.Sector = engine.GetSectorById(modelFleet.Sector?.Id ?? (-1));
			fleet.transform.localPosition = localPosition;
			fleet.transform.localRotation = localRotation;
			fleet.Faction = engine.GetFactionByid(modelFleet.Faction?.Id ?? (-1));
			fleet.SetHomeBase(ImportSectorTarget(modelFleet.HomeBase));
			fleet.ExcludeFromFactionAI = modelFleet.ExcludeFromFactionAI;
			fleet.FleetStrategy = modelFleet.Strategy;
			fleet.FleetFormation = EngineASX.Instance.GetFleetFormationById(modelFleet.FormationId);
			if (fleet.FleetFormation == null)
			{
				fleet.FleetFormation = EngineASX.Instance.GetDefaultFleetFormation();
			}
			if (modelFleet.FleetSettings != null)
			{
				FleetSettings fleetSettings = fleet.Settings;
				if (fleetSettings == null)
				{
					fleetSettings = fleet.gameObject.AddComponent<FleetSettings>();
				}
				ImportFleetSettings(fleetSettings, modelFleet.FleetSettings);
			}
		}

		public static void ImportFleetSettings(FleetSettings fleetSettings, ModelFleetSettings modelFleetSettings)
		{
			fleetSettings.PreferCloak = modelFleetSettings.PreferCloak;
			fleetSettings.PreferToDock = modelFleetSettings.PreferToDock;
			fleetSettings.Aggression = modelFleetSettings.Aggression;
			fleetSettings.AllowAttack = modelFleetSettings.AllowAttack;
			fleetSettings.TargetInterceptionLowerDistance = modelFleetSettings.TargetInterceptionLowerDistance;
			fleetSettings.TargetInterceptionUpperDistance = modelFleetSettings.TargetInterceptionUpperDistance;
			fleetSettings.MaxJumpDistance = modelFleetSettings.MaxJumpDistance;
			fleetSettings.AllowCombatInterception = modelFleetSettings.AllowCombatInterception;
			fleetSettings.DestroyWhenNoPilots = modelFleetSettings.DestroyWhenNoPilots;
			fleetSettings.FormationTightness = modelFleetSettings.FormationTightness;
			fleetSettings.CargoCollectionPreference = modelFleetSettings.CargoCollectionPreference;
		}

		private ActiveFleetOrder ImportActiveFleetOrder(Fleet fleet, FleetOrder fleetOrder, ModelActiveFleetOrder modelActiveFleetOrder)
		{
			ActiveFleetOrder activeFleetOrder = fleetOrder.CreateActiveFleetOrder();
			activeFleetOrder.Engine = engine;
			activeFleetOrder.Fleet = fleet;
			activeFleetOrder.TimeoutTime = modelActiveFleetOrder.TimeoutTime;
			activeFleetOrder.StartTime = modelActiveFleetOrder.StartTime;
			switch (activeFleetOrder.FleetOrder.OrderType)
			{
			case FleetOrderType.AttackGroup:
			{
				ModelActiveAttackFleetOrder modelActiveAttackFleetOrder = (ModelActiveAttackFleetOrder)modelActiveFleetOrder;
				((ActiveAttackFleetOrder)activeFleetOrder).Target = engine.GetFleetByid(modelActiveAttackFleetOrder.TargetFleet?.Id ?? (-1));
				break;
			}
			case FleetOrderType.AttackTarget:
			{
				ActiveAttackTargetOrder activeAttackTargetOrder = (ActiveAttackTargetOrder)activeFleetOrder;
				ModelActiveAttackTargetOrder modelActiveAttackTargetOrder = (ModelActiveAttackTargetOrder)modelActiveFleetOrder;
				activeAttackTargetOrder.Target = engine.GetUnitByid(modelActiveAttackTargetOrder.TargetUnit?.Id ?? (-1));
				activeAttackTargetOrder.OriginalTargetFaction = engine.GetFactionByid(modelActiveAttackTargetOrder.OriginalTargetFaction?.Id ?? (-1));
				break;
			}
			case FleetOrderType.AutonomousBountyHunterObjective:
			{
				ActiveBountyHunterOrder activeBountyHunterOrder = (ActiveBountyHunterOrder)activeFleetOrder;
				ModelActiveUniverseBountyHunterOrder modelActiveUniverseBountyHunterOrder = (ModelActiveUniverseBountyHunterOrder)modelActiveFleetOrder;
				activeBountyHunterOrder.TargetPilot = engine.GetPersonById(modelActiveUniverseBountyHunterOrder.TargetPerson?.Id ?? (-1));
				break;
			}
			case FleetOrderType.AutonomousRoamLocationsObjective:
			{
				ActiveRoamLocationsOrder activeRoamLocationsOrder = (ActiveRoamLocationsOrder)activeFleetOrder;
				ModelActiveUniverseRoamOrder modelActiveUniverseRoamOrder = (ModelActiveUniverseRoamOrder)modelActiveFleetOrder;
				activeRoamLocationsOrder.CurrentTargetSector = engine.GetSectorById(modelActiveUniverseRoamOrder.CurrentTargetSector?.Id ?? (-1));
				activeRoamLocationsOrder.CurrentTargetSectorPosition = modelActiveUniverseRoamOrder.CurrentTargetPosition.ToVector3();
				break;
			}
			case FleetOrderType.Explore:
			{
				ActiveExploreOrder activeExploreOrder = (ActiveExploreOrder)activeFleetOrder;
				ModelActiveExploreOrder modelActiveExploreOrder = (ModelActiveExploreOrder)modelActiveFleetOrder;
				activeExploreOrder.CurrentTargetSector = engine.GetSectorById(modelActiveExploreOrder.CurrentTargetSector?.Id ?? (-1));
				Unit unitByid = engine.GetUnitByid(modelActiveExploreOrder.CurrentTargetWormhole?.Id ?? (-1));
				if (unitByid != null)
				{
					activeExploreOrder.CurrentTargetWormhole = unitByid.WormholeComponent;
				}
				activeExploreOrder.CurrentTargetSectorPosition = modelActiveExploreOrder.CurrentTargetSectorPosition.ToVector3();
				break;
			}
			case FleetOrderType.AutonomousTrade:
			case FleetOrderType.ManualTrade:
			case FleetOrderType.Trade:
			{
				ActiveTradeOrder activeTradeOrder = (ActiveTradeOrder)activeFleetOrder;
				ModelActiveTradeOrder modelActiveTradeOrder = (ModelActiveTradeOrder)modelActiveFleetOrder;
				if (modelActiveTradeOrder.TradeRoute != null)
				{
					activeTradeOrder.TradeRoute = ImportTradeRoute(modelActiveTradeOrder.TradeRoute);
				}
				activeTradeOrder.EndBuySellTime = modelActiveTradeOrder.EndBuySellTime;
				activeTradeOrder.LastStateChangeTime = modelActiveTradeOrder.LastStateChangeTime;
				if (activeTradeOrder.TradeRoute != null)
				{
					activeTradeOrder.CurrentState = modelActiveTradeOrder.CurrentState;
				}
				break;
			}
			case FleetOrderType.AutonomousTransportPassengers:
			{
				ActiveTransportPassengersOrder activeTransportPassengersOrder = (ActiveTransportPassengersOrder)activeFleetOrder;
				ModelActiveUniversePassengerTransportOrder modelActiveUniversePassengerTransportOrder = (ModelActiveUniversePassengerTransportOrder)modelActiveFleetOrder;
				activeTransportPassengersOrder.PassengerGroup = engine.GetPassengerGroupByid(modelActiveUniversePassengerTransportOrder.PassengerGroup?.Id ?? (-1));
				activeTransportPassengersOrder.EndBuySellTime = modelActiveUniversePassengerTransportOrder.EndBuySellTime;
				activeTransportPassengersOrder.LastStateChangeTime = modelActiveUniversePassengerTransportOrder.LastStateChangeTime;
				ActiveTransportPassengerOrderState currentState = modelActiveUniversePassengerTransportOrder.CurrentState;
				if (activeTransportPassengersOrder.PassengerGroup != null)
				{
					activeTransportPassengersOrder.CurrentState = currentState;
				}
				else
				{
					activeTransportPassengersOrder.CurrentState = ActiveTransportPassengerOrderState.None;
				}
				break;
			}
			case FleetOrderType.Mine:
			{
				ActiveMineOrder activeMineOrder = (ActiveMineOrder)activeFleetOrder;
				ModelActiveMineOrder modelActiveMineOrder = (ModelActiveMineOrder)modelActiveFleetOrder;
				activeMineOrder.MineTarget = engine.GetUnitByid(modelActiveMineOrder.MineTarget?.Id ?? (-1));
				activeMineOrder.State = modelActiveMineOrder.State;
				activeMineOrder.AngleFromAsteroid = modelActiveMineOrder.AngleFromAsteroid;
				activeMineOrder.DistanceFromAsteroid = modelActiveMineOrder.DistanceFromAsteroid;
				activeMineOrder.ResetTargetPosition();
				break;
			}
			case FleetOrderType.Scavenge:
			{
				ActiveScavengeOrder activeScavengeOrder = (ActiveScavengeOrder)activeFleetOrder;
				ModelActiveScavengeOrder modelActiveScavengeOrder = (ModelActiveScavengeOrder)modelActiveFleetOrder;
				activeScavengeOrder.RoamSectorLocalPosition = modelActiveScavengeOrder.Position?.ToVector3();
				break;
			}
			case FleetOrderType.Patrol:
			case FleetOrderType.PatrolPath:
			{
				ActivePatrolOrder activePatrolOrder = (ActivePatrolOrder)activeFleetOrder;
				ModelActivePatrolOrder modelActivePatrolOrder = (ModelActivePatrolOrder)modelActiveFleetOrder;
				activePatrolOrder.PathDirection = modelActivePatrolOrder.PathDirection;
				activePatrolOrder.NodeIndex = modelActivePatrolOrder.NodeIndex;
				activePatrolOrder.StartNodeIndex = modelActivePatrolOrder.StartNodeIndex;
				break;
			}
			case FleetOrderType.ManualRepair:
			case FleetOrderType.RepairAtNearest:
			{
				ActiveRepairFleetOrder activeRepairFleetOrder = (ActiveRepairFleetOrder)activeFleetOrder;
				ModelActiveRepairFleetOrder modelActiveRepairFleetOrder = (ModelActiveRepairFleetOrder)modelActiveFleetOrder;
				activeRepairFleetOrder.RepairState = modelActiveRepairFleetOrder.RepairState;
				activeRepairFleetOrder.CurrentRepairLocation = engine.GetUnitByid(modelActiveRepairFleetOrder.CurrentRepairLocationUnit?.Id ?? (-1));
				break;
			}
			case FleetOrderType.ManualRearm:
			case FleetOrderType.RearmAtNearest:
			{
				ActiveRearmOrder activeRearmOrder = (ActiveRearmOrder)activeFleetOrder;
				ModelActiveRearmFleetOrder modelActiveRearmFleetOrder = (ModelActiveRearmFleetOrder)modelActiveFleetOrder;
				activeRearmOrder.State = modelActiveRearmFleetOrder.State;
				activeRearmOrder.CurrentRearmLocation = engine.GetUnitByid(modelActiveRearmFleetOrder.CurrentRearmLocationUnit?.Id ?? (-1));
				break;
			}
			case FleetOrderType.Wait:
			{
				ActiveWaitOrder activeWaitOrder = (ActiveWaitOrder)activeFleetOrder;
				ModelActiveWaitOrder modelActiveWaitOrder = (ModelActiveWaitOrder)modelActiveFleetOrder;
				activeWaitOrder.WaitExpiryTime = modelActiveWaitOrder.WaitExpiryTime;
				break;
			}
			case FleetOrderType.SellCargo:
			{
				ActiveSellCargoOrder activeSellCargoOrder = (ActiveSellCargoOrder)activeFleetOrder;
				ModelActiveSellCargoOrder modelActiveSellCargoOrder = (ModelActiveSellCargoOrder)modelActiveFleetOrder;
				activeSellCargoOrder.SellExpireTime = modelActiveSellCargoOrder.SellExpireTime;
				activeSellCargoOrder.SellCargoClass = engine.GetCargoClassById((int)modelActiveSellCargoOrder.SellCargoClass);
				activeSellCargoOrder.TraderTarget = GetCargoTraderFromUnit(modelActiveSellCargoOrder.TraderTargetUnit);
				activeSellCargoOrder.State = modelActiveSellCargoOrder.State;
				break;
			}
			case FleetOrderType.MoveToNearestFriendlyStation:
			{
				ActiveMoveToNearestFriendlyStationOrder activeMoveToNearestFriendlyStationOrder = (ActiveMoveToNearestFriendlyStationOrder)activeFleetOrder;
				ModelActiveMoveToNearestFriendlyStationOrder modelActiveMoveToNearestFriendlyStationOrder = (ModelActiveMoveToNearestFriendlyStationOrder)modelActiveFleetOrder;
				activeMoveToNearestFriendlyStationOrder.TargetStation = engine.GetUnitByid(modelActiveMoveToNearestFriendlyStationOrder.TargetStationUnit?.Id ?? (-1));
				break;
			}
			case FleetOrderType.EnterWormhole:
			{
				ActiveEnterWormholeOrder activeEnterWormholeOrder = (ActiveEnterWormholeOrder)activeFleetOrder;
				ModelActiveEnterWormholeOrder modelActiveEnterWormholeOrder = (ModelActiveEnterWormholeOrder)modelActiveFleetOrder;
				activeEnterWormholeOrder.State = modelActiveEnterWormholeOrder.State;
				break;
			}
			case FleetOrderType.ExploreSector:
			{
				ActiveExploreSectorOrder activeExploreSectorOrder = (ActiveExploreSectorOrder)activeFleetOrder;
				ModelActiveExploreSectorOrder modelActiveExploreSectorOrder = (ModelActiveExploreSectorOrder)modelActiveFleetOrder;
				activeExploreSectorOrder.CurrentTargetSectorPosition = modelActiveExploreSectorOrder.CurrentTargetSectorPosition?.ToVector3();
				break;
			}
			}
			return activeFleetOrder;
		}

		private void ImportPeople()
		{
			LogWrapper.Log($"EngineIO Reading {savedGame.People.Count} people...", null, 2);
			foreach (ModelPerson person in savedGame.People)
			{
				ImportPerson(person);
			}
		}

		private void ImportNpcs()
		{
			foreach (ModelPerson person in savedGame.People)
			{
				if (person.NpcPilot != null)
				{
					int id = person.Id;
					Person personById = engine.GetPersonById(id);
					NpcPilot npcPilot = personById.gameObject.AddComponent<NpcPilot>();
					personById.FindNpcPilot();
					npcPilot.Init();
					ImportNpcPilot(npcPilot, person.NpcPilot);
				}
			}
		}

		private void ImportPerson(ModelPerson modelPerson)
		{
			int id = modelPerson.Id;
			Person person = UnityObjectHelper.NewGameObject<Person>();
			person.UniqueId = id;
			person.Init();
			if (modelPerson.GeneratedFirstNameId > -1)
			{
				int generatedFirstNameId = modelPerson.GeneratedFirstNameId;
				int generatedLastNameId = modelPerson.GeneratedLastNameId;
				if (engine.CharacterNames.GetName(generatedFirstNameId, generatedLastNameId) != null)
				{
					person.HasGeneratedName = true;
					person.GeneratedFirstNameId = generatedFirstNameId;
					person.GeneratedLastNameId = generatedLastNameId;
				}
				else
				{
					Debug.LogWarningFormat(person, "Pilot: {0} - Read unknown first ({1}) or last ({2}) name index. Generating new name...", person.UniqueId, generatedFirstNameId, generatedLastNameId);
					engine.CharacterNames.GenerateName(person);
				}
			}
			else
			{
				person.CustomName = modelPerson.CustomName;
				person.CustomShortName = modelPerson.CustomShortName;
			}
			person.RefreshName();
			person.CustomTitle = modelPerson.CustomTitle;
			person.CommsHandler = engine.World.GetDialogById(modelPerson.DialogId);
			person.IsMale = modelPerson.IsMale;
			person.IsAutoPilot = modelPerson.IsAutoPilot;
			person.Faction = engine.GetFactionByid(modelPerson.Faction?.Id ?? (-1));
			person.DestroyGameObjectOnKill = modelPerson.DestroyGameObjectOnKill;
			int num = modelPerson.CurrentUnit?.Id ?? (-1);
			if (num > -1)
			{
				Unit unitByid = engine.GetUnitByid(num);
				if (unitByid == null)
				{
					Debug.LogError($"Pilot id: {id} ({person.Name}) is assigned to non-existant unit (id: {num})", person);
				}
				person.CurrentUnit = unitByid;
			}
			bool isPilot = modelPerson.IsPilot;
			if (isPilot && person.CurrentUnit == null)
			{
				Debug.LogError($"Pilot id: {id} ({person.Name}) is assigned as pilot but does not have a unit", person);
			}
			else
			{
				person.IsPilot = isPilot;
			}
			person.Seed = modelPerson.Seed;
			person.Kills = modelPerson.Kills;
			person.Deaths = modelPerson.Deaths;
			person.Properness = modelPerson.Properness;
			person.Aggression = modelPerson.Aggression;
			person.Greed = modelPerson.Greed;
			person.ChangeRank(EngineASX.Instance.GetPilotRankById(modelPerson.RankId));
			person.AvatarProfile = GameController.Instance.AvatarController.GetAvatarProfileById(modelPerson.AvatarProfileId);
			person.DialogProfile = EngineASX.Instance.GetDialogProfileById(modelPerson.DialogProfileId);
			person.AutoAssignDialogProfileIfNone();
			if (modelPerson == savedGame.Player.Person)
			{
				GamePlayer gamePlayer = person.gameObject.AddComponent<GamePlayer>();
				engine.LocalPlayer = gamePlayer;
				gamePlayer.CreateStatsIfNull();
			}
			if (modelPerson.NpcPilotSettings != null)
			{
				NpcPilotSettings profile = person.gameObject.AddComponent<NpcPilotSettings>();
				ImportNpcPilotSettings(profile, modelPerson.NpcPilotSettings);
			}
		}

		private void ImportNpcPilot(NpcPilot npcPilot, ModelNpcPilot modelNpcPilot)
		{
			npcPilot.DestroyWhenNoUnit = modelNpcPilot.DestroyWhenNoUnit;
			npcPilot.DestroyWhenNotPilotting = modelNpcPilot.DestroyWhenNotPilotting;
			int num = modelNpcPilot.Fleet?.Id ?? (-1);
			if (num <= -1)
			{
				return;
			}
			if (npcPilot.CurrentUnit == null)
			{
				Debug.LogError($"Cannot assign pilot id: {npcPilot.Person.UniqueId} ({npcPilot.Person.Name}) to Fleet: {num} as no ControlledUnit assigned");
				return;
			}
			Fleet fleetByid = engine.GetFleetByid(num);
			if (fleetByid == null)
			{
				Debug.LogError($"Unknown Fleet id: {num}");
			}
			npcPilot.Fleet = fleetByid;
		}

		private void ImportNpcPilotSettings(NpcPilotSettings profile, ModelNpcPilotSettings modelNpcPilotSettings)
		{
			profile.RestrictedWeaponPreference = modelNpcPilotSettings.RestrictedWeaponPreference;
			profile.CombatEfficiency = modelNpcPilotSettings.CombatEfficiency;
			profile.AICheatAmmo = modelNpcPilotSettings.CheatAmmo;
			profile.AllowDitchShip = modelNpcPilotSettings.AllowDitchShip;
		}

		private void ImportFactionAIs()
		{
			foreach (ModelFaction faction in savedGame.Factions)
			{
				Faction factionByid = engine.GetFactionByid(faction?.Id ?? (-1));
				if (!(factionByid == null) && faction.FactionAI != null)
				{
					FactionAIType aIType = faction.FactionAI.AIType;
					FactionAIBase factionAIBase = FactionAIBase.CreateFactionAIType(factionByid.transform, aIType);
					if (factionAIBase != null)
					{
						factionAIBase.Init();
						ImportFactionAI(factionAIBase, faction.FactionAI);
					}
					else
					{
						Debug.LogError("Unknown faction ai type loaded: " + aIType);
					}
				}
			}
		}

		private void ImportFactionBountyBoards()
		{
			foreach (ModelFaction faction in savedGame.Factions)
			{
				if (faction.BountyBoard != null)
				{
					Faction factionByid = engine.GetFactionByid(faction?.Id ?? (-1));
					if (!(factionByid == null))
					{
						factionByid.FindOrCreateBountyBoard();
						ImportFactionBountyBoard(factionByid.BountyBoard, faction.BountyBoard);
					}
				}
			}
		}

		private void ImportFactionTransactions()
		{
			foreach (ModelFaction faction in savedGame.Factions)
			{
				if (faction.Transactions != null && faction.Transactions.Any())
				{
					Faction factionByid = engine.GetFactionByid(faction.Id);
					ImportFactionTransactions(factionByid, faction);
				}
			}
		}

		private void ImportFactionAI(FactionAIBase factionAI, ModelFactionAI modelFactionAI)
		{
			factionAI.NextUnitSpawnTime = modelFactionAI.NextUnitSpawnTime;
			factionAI.NumGroupsSpawned = modelFactionAI.NumFleetsSpawned;
			factionAI.NumUnitsSpawned = modelFactionAI.NumUnitsSpawned;
			factionAI.ShipBuildOnlyAtOwnedDocks = modelFactionAI.SpawnOnlyAtOwnedDocks;
			factionAI.LastBuiltUnitTime = modelFactionAI.LastBuiltUnitTime;
			factionAI.LastOrderedPatrolTime = modelFactionAI.LastOrderedPatrolTime;
			factionAI.ShipBuildSectorMode = modelFactionAI.SpawnMode;
			foreach (ModelSector spawnSector in modelFactionAI.SpawnSectors)
			{
				Sector sectorById = engine.GetSectorById(spawnSector?.Id ?? (-1));
				if (sectorById != null)
				{
					factionAI.ShipBuildSectors.Add(sectorById);
				}
			}
			switch (factionAI.AIType)
			{
			case FactionAIType.Trader:
			{
				FactionAITrader factionAITrader = (FactionAITrader)factionAI;
				ModelFactionAITrader modelFactionAITrader = (ModelFactionAITrader)modelFactionAI;
				factionAITrader.TradeOnlySpecificCargoTypes = modelFactionAITrader.TradeOnlySpecificCargoTypes;
				factionAITrader.TradeSpecificCargoTypes.Clear();
				{
					foreach (ModelCargoClass tradeSpecificCargoType in modelFactionAITrader.TradeSpecificCargoTypes)
					{
						CargoClass cargoClassById = engine.GetCargoClassById((int)tradeSpecificCargoType);
						factionAITrader.TradeSpecificCargoTypes.Add(cargoClassById);
					}
					break;
				}
			}
			case FactionAIType.Miner:
			case FactionAIType.Patroller:
			case FactionAIType.StationBuilder:
			case FactionAIType.Generic:
			case FactionAIType.Empire:
			case FactionAIType.Mercenary:
				break;
			}
		}

		private void ImportSectors()
		{
			foreach (ModelSector sector in savedGame.Sectors)
			{
				ImportSector(sector);
			}
		}

		private void ImportSector(ModelSector modelSector)
		{
			Sector sector = UnityObjectHelper.InstantiateAndGetComponent(engine.GameSettings.LoadGameScenePrefab);
			sector.UniqueId = modelSector.Id;
			sector.Init();
			sector.Name = modelSector.Name;
			sector.MapPosition = modelSector.MapPosition.ToVector3();
			sector.Description = modelSector.Description;
			sector.GateDistanceMultiplier = modelSector.GateDistanceMultiplier;
			sector.RandomSeed = modelSector.RandomSeed;
			sector.BackgroundRotation = Quaternion.Euler(modelSector.BackgroundRotation.ToVector3());
			sector.AmbientLightColor = modelSector.AmbientLightColor.ToColor();
			sector.DirectionLightColor = modelSector.DirectionLightColor.ToColor();
			sector.LightRotation = Quaternion.Euler(modelSector.DirectionLightRotation.ToVector3());
			sector.LastTimeChangedControl = modelSector.LastTimeChangedControl;
			sector.LightDirectionFudge = modelSector.LightDirectionFudge;
			if (sector.RandomSeed == 5466554 && (savedGame.Header.CreatedVersion == new Version(2, 0, 10) || savedGame.Header.CreatedVersion == new Version(2, 0, 11)))
			{
				sector.AssignRandomSeed();
			}
			if (modelSector.CustomAppearance != null)
			{
				CustomSectorAppearance customSectorAppearance = sector.gameObject.AddComponent<CustomSectorAppearance>();
				ImportCustomSectorAppearance(customSectorAppearance, modelSector.CustomAppearance);
			}
		}

		private void ImportCustomSectorAppearance(CustomSectorAppearance customSectorAppearance, ModelSectorAppearance modelSectorAppearance)
		{
			customSectorAppearance.SpaceConstructorParams = new SpaceConstructorParams();
			customSectorAppearance.SpaceConstructorParams.NebulaBrightness = modelSectorAppearance.NebulaBrightness;
			customSectorAppearance.SpaceConstructorParams.NebulaColors = modelSectorAppearance.NebulaColors;
			customSectorAppearance.SpaceConstructorParams.NebulaComplexity = modelSectorAppearance.NebulaComplexity;
			customSectorAppearance.SpaceConstructorParams.NebulaCount = modelSectorAppearance.NebulaCount;
			customSectorAppearance.SpaceConstructorParams.NebulaTextureCount = modelSectorAppearance.NebulaTextureCount;
			customSectorAppearance.SpaceConstructorParams.NebulaStyles = modelSectorAppearance.NebulaStyles;
			customSectorAppearance.SpaceConstructorParams.StarsCount = modelSectorAppearance.StarsCount;
			customSectorAppearance.SpaceConstructorParams.StarsIntensity = modelSectorAppearance.StarsIntensity;
			if (customSectorAppearance.SpaceConstructorParams.NebulaCount > 64)
			{
				customSectorAppearance.SpaceConstructorParams.NebulaCount = 64;
			}
		}

		private void ImportFactions()
		{
			foreach (ModelFaction faction in savedGame.Factions)
			{
				ImportFaction(faction);
			}
		}

		private void ImportFaction(ModelFaction modelFaction)
		{
			Faction faction = UnityObjectHelper.InstantiateAndGetComponent(GameController.Instance.GenericFactionPrefab);
			faction.UniqueId = modelFaction.Id;
			faction.Init();
			faction.Name = modelFaction.CustomName;
			faction.ShortName = modelFaction.CustomShortName;
			if (string.IsNullOrWhiteSpace(faction.Name))
			{
				faction.GeneratedNameId = modelFaction.GeneratedNameId;
				faction.GeneratedSuffixId = modelFaction.GeneratedSuffixId;
				if (faction.GeneratedNameId > -1)
				{
					EngineASX.Instance.FactionNames.ReserveName(faction.GeneratedNameId);
				}
				if (faction.GeneratedSuffixId > -1)
				{
					EngineASX.Instance.FactionNames.ReserveSuffix(faction.GeneratedSuffixId);
				}
				if (faction.HasGeneratedName)
				{
					faction.UpdateGeneratedName();
				}
			}
			Sector sectorById = engine.GetSectorById(modelFaction.HomeSector?.Id ?? (-1));
			faction.ChangeHomeSector(sectorById);
			faction.HomeSectorPosition = modelFaction.HomeSectorPosition?.ToVector3();
			faction.Credits = modelFaction.Credits;
			faction.Description = modelFaction.Description;
			faction.IsCivilian = modelFaction.IsCivilian;
			faction.FactionType = modelFaction.FactionType;
			faction.Aggression = modelFaction.Aggression;
			faction.Virtue = modelFaction.Virtue;
			faction.Greed = modelFaction.Greed;
			faction.Cooperation = modelFaction.Cooperation;
			faction.TradeEfficiency = modelFaction.TradeEfficiency;
			faction.DynamicFactionAttitudes = modelFaction.DynamicRelations;
			faction.ShouldFactionShowMissionSpecs = modelFaction.ShowJobBoards;
			faction.CreateMissions = modelFaction.CreateJobs;
			faction.RequisitionPointMultiplier = modelFaction.RequisitionPointMultiplier;
			faction.DestroyWhenNoUnits = modelFaction.DestroyWhenNoUnits;
			faction.MinAIUnitControllerCombatEfficiency = modelFaction.MinNpcCombatEfficiency;
			faction.MaxAIUnitControllerCombatEfficiency = modelFaction.MaxNpcCombatEfficiency;
			faction.AdditionalRpProvision = modelFaction.AdditionalRpProvision;
			faction.TradeIllegalGoods = modelFaction.TradeIllegalGoods;
			faction.SpawnTime = modelFaction.SpawnTime;
			faction.HighestEverNetWorth = modelFaction.HighestEverNetWorth;
			faction.PilotRankingSystem = EngineASX.Instance.GetPilotRankingSystemById(modelFaction.RankingSystemId);
			if (modelFaction.PreferredFormationId > -1)
			{
				faction.PreferredFormationStyle = GameController.Instance.GameSettings.FormationSettings.FormationStyles.FirstOrDefault((FleetFormationStyle e) => e.UniqueId == modelFaction.PreferredFormationId);
			}
			if (modelFaction.CustomSettings != null)
			{
				if (faction.AISettings == null)
				{
					faction.AISettings = faction.gameObject.AddComponent<FactionAISettings>();
				}
				ImportFactionAISettings(faction.AISettings, modelFaction.CustomSettings);
			}
			if (modelFaction.Stats != null)
			{
				FactionStats stats = faction.gameObject.AddComponent<FactionStats>();
				faction.Stats = stats;
				ImportFactionStats(faction.Stats, modelFaction.Stats);
			}
			faction.AutopilotExcludedSectors.Clear();
			foreach (ModelSector autopilotExcludedSector in modelFaction.AutopilotExcludedSectors)
			{
				faction.AutopilotExcludedSectors.Add(autopilotExcludedSector.Id);
			}
			faction.FactionTypeInfo = faction.FindFactionTypeInfo();
			if (modelFaction.AvatarProfileIds == null || modelFaction.AvatarProfileIds.Count <= 0)
			{
				return;
			}
			foreach (byte avatarProfileId in modelFaction.AvatarProfileIds)
			{
				AvatarProfile avatarProfileById = GameController.Instance.AvatarController.GetAvatarProfileById(avatarProfileId);
				if (avatarProfileById != null)
				{
					faction.PersonAvatarProfiles.Add(avatarProfileById);
				}
			}
		}

		private void ImportFactionIntel(FactionIntel factionIntel, ModelFactionIntel modelFactionIntel)
		{
			foreach (ModelSector sector in modelFactionIntel.Sectors)
			{
				Sector sectorById = engine.GetSectorById(sector?.Id ?? (-1));
				if (sectorById != null)
				{
					factionIntel.DiscoverSector(sectorById);
				}
			}
			foreach (ModelUnit unit in modelFactionIntel.Units)
			{
				Unit unitByid = engine.GetUnitByid(unit?.Id ?? (-1));
				if (unitByid != null)
				{
					factionIntel.DiscoverUnit(unitByid, setTimeOfDiscovery: false);
				}
			}
			foreach (ModelUnit enteredWormhole in modelFactionIntel.EnteredWormholes)
			{
				Unit unitByid2 = engine.GetUnitByid(enteredWormhole?.Id ?? (-1));
				if (unitByid2 != null && unitByid2.WormholeComponent != null)
				{
					factionIntel.EnteredWormholeIds.Add(unitByid2.UniqueId);
				}
			}
		}

		private void ImportFactionTransactions(Faction faction, ModelFaction modelFaction)
		{
			foreach (ModelFactionTransaction transaction in modelFaction.Transactions)
			{
				FactionTransaction factionTransaction = new FactionTransaction();
				ImportFactionTransaction(factionTransaction, transaction);
				if (faction.RecentTransactions == null)
				{
					faction.RecentTransactions = new List<FactionTransaction>();
				}
				faction.RecentTransactions.Add(factionTransaction);
			}
		}

		private void ImportFactionTransaction(FactionTransaction transaction, ModelFactionTransaction modelFactionTransaction)
		{
			transaction.TransactionType = modelFactionTransaction.TransactionType;
			transaction.Value = modelFactionTransaction.Value;
			transaction.CurrentBalance = modelFactionTransaction.CurrentBalance;
			transaction.Location = engine.GetUnitByid(modelFactionTransaction.LocationUnit?.Id ?? (-1));
			transaction.OtherFaction = engine.GetFactionByid(modelFactionTransaction.OtherFaction?.Id ?? (-1));
			transaction.RelatedCargoClass = engine.GetCargoClassById((int)modelFactionTransaction.RelatedCargoClass);
			transaction.RelatedUnitClass = engine.GetUnitClassById((int)modelFactionTransaction.RelatedUnitClass);
			transaction.GameWorldTime = modelFactionTransaction.GameWorldTime;
			transaction.TaxType = modelFactionTransaction.TaxType;
			transaction.RelatedCount = modelFactionTransaction.RelatedCount;
		}

		private void ImportFactionAISettings(FactionAISettings settings, ModelFactionCustomSettings modelSettings)
		{
			settings.BuildShips = modelSettings.BuildShips;
			settings.RepairShips = modelSettings.RepairShips;
			settings.UpgradeShips = modelSettings.UpgradeShips;
			settings.RepairMinHullDamage = modelSettings.RepairMinHullDamage;
			settings.RepairMinCreditsBeforeRepair = modelSettings.RepairMinCreditsBeforeRepair;
			settings.PreferenceToPlaceBounty = modelSettings.PreferenceToPlaceBounty;
			settings.LargeShipPreference = modelSettings.LargeShipPreference;
			settings.CloakShipPreference = modelSettings.CloakShipPreference;
			settings.DailyIncome = modelSettings.DailyIncome;
			settings.HostileWithAll = modelSettings.HostileWithAll;
			settings.MinGroupUnitCount = modelSettings.MinFleetUnitCount;
			settings.MaxGroupUnitCount = modelSettings.MaxFleetUnitCount;
			settings.OffensiveStance = modelSettings.OffensiveStance;
			settings.AllowForeignFactionToUseDocks = modelSettings.AllowOtherFactionToUseDocks;
			settings.PreferenceToBuildTurrets = modelSettings.PreferenceToBuildTurrets;
			settings.PreferenceToBuildStations = modelSettings.PreferenceToBuildStations;
			settings.PreferenceToHaveAmmo = modelSettings.PreferenceToHaveAmmo;
			settings.IgnoreStationCreditsReserve = modelSettings.IgnoreStationCreditsReserve;
			settings.MaxJumpDistanceFromHomeSector = modelSettings.MaxJumpDistanceFromHomeSector;
			settings.MaxStationBuildDistanceFromHomeSector = modelSettings.MaxStationBuildDistanceFromHomeSector;
			settings.PilotGender = modelSettings.PilotGender;
			settings.FixedShipCount = modelSettings.FixedShipCount;
			settings.SectorControlLikelihood = modelSettings.SectorControlLikelihood;
		}

		private void ImportFactionBountyBoard(FactionBountyBoard bountyBoard, ModelFactionBountyBoard modelFactionBountyBoard)
		{
			foreach (ModelFactionBountyBoardItem item in modelFactionBountyBoard.Items)
			{
				Person personById = engine.GetPersonById(item.TargetPerson?.Id ?? (-1));
				int reward = item.Reward;
				Unit unitByid = engine.GetUnitByid(item.LastKnownTargetUnit?.Id ?? (-1));
				Sector sectorById = engine.GetSectorById(item.LastKnownTargetSector?.Id ?? (-1));
				Vector3? lastKnownSectorPosition = item.LastKnownTargetPosition?.ToVector3();
				double? timeOfLastSighting = item.TimeOfLastSighting;
				Faction factionByid = engine.GetFactionByid(item.SourceFaction?.Id ?? (-1));
				if (personById != null && reward > 0)
				{
					FactionBountyItem factionBountyItem = BountyHelper.AddBounty(factionByid, bountyBoard, personById, reward, (timeOfLastSighting >= 0.0) ? timeOfLastSighting : ((double?)null), updateLastKnownPosition: false);
					factionBountyItem.LastKnownPilottedShip = unitByid;
					factionBountyItem.LastKnownSector = sectorById;
					factionBountyItem.LastKnownSectorPosition = lastKnownSectorPosition;
				}
				else
				{
					Debug.LogWarning($"FactionBounty item for {bountyBoard.Faction} is invalid. Bounty is zero of pilot null");
				}
			}
		}

		private void ImportFactionStats(FactionStats factionStats, ModelFactionStats modelFactionStats)
		{
			factionStats.TotalShipsClaimed = modelFactionStats.TotalShipsClaimed;
			Dictionary<UnitClass, int> dictionary = ReadFactionStatsUnitCounts(modelFactionStats.UnitsDestroyedByClassId);
			Dictionary<UnitClass, int> dictionary2 = ReadFactionStatsUnitCounts(modelFactionStats.UnitLostByClassId);
			foreach (KeyValuePair<UnitClass, int> item in dictionary)
			{
				factionStats.UnitsDestroyedCountByClass.Add(item.Key, item.Value);
			}
			foreach (KeyValuePair<UnitClass, int> item2 in dictionary2)
			{
				factionStats.UnitsLostCountByClass.Add(item2.Key, item2.Value);
			}
			factionStats.ScratchcardsScratched = modelFactionStats.ScratchcardsScratched;
			factionStats.HighestScratchcardWin = modelFactionStats.HighestScratchcardWin;
		}

		private Dictionary<UnitClass, int> ReadFactionStatsUnitCounts(Dictionary<ModelUnitClass, int> counts)
		{
			Dictionary<UnitClass, int> dictionary = new Dictionary<UnitClass, int>(8);
			foreach (KeyValuePair<ModelUnitClass, int> count in counts)
			{
				int key = (int)count.Key;
				int value = count.Value;
				UnitClass unitClassById = engine.GetUnitClassById(key);
				if (unitClassById != null)
				{
					if (value > 0)
					{
						dictionary.Add(unitClassById, value);
					}
				}
				else
				{
					Debug.LogWarning("Unknown unit class found in faction stats: " + key);
				}
			}
			return dictionary;
		}

		private void ImportPassengerGroups()
		{
			foreach (ModelUnit unit in savedGame.Units)
			{
				if (unit.PassengerGroups == null || !unit.PassengerGroups.Any() || !(engine.GetUnitByid(unit?.Id ?? (-1)) != null))
				{
					continue;
				}
				foreach (ModelPassengerGroup passengerGroup2 in unit.PassengerGroups)
				{
					PassengerGroup passengerGroup = new PassengerGroup();
					ImportPassengerGroup(passengerGroup, passengerGroup2);
				}
			}
		}

		private void ImportPassengerGroup(PassengerGroup passengerGroup, ModelPassengerGroup modelPassengerGroup)
		{
			passengerGroup.UniqueId = modelPassengerGroup.Id;
			passengerGroup.Init();
			passengerGroup.CurrentUnit = engine.GetUnitByid(modelPassengerGroup.Unit?.Id ?? (-1));
			passengerGroup.Source = engine.GetUnitByid(modelPassengerGroup.SourceUnit?.Id ?? (-1));
			passengerGroup.Destination = engine.GetUnitByid(modelPassengerGroup.DestinationUnit?.Id ?? (-1));
			passengerGroup.PassengerCount = modelPassengerGroup.PassengerCount;
			passengerGroup.ExpiryTime = modelPassengerGroup.ExpiryTime;
			int revenue = modelPassengerGroup.Revenue;
			passengerGroup.CachedRevenue = ((revenue >= 0) ? new int?(revenue) : ((int?)null));
		}

		private void ImportJobs()
		{
			foreach (ModelUnit unit in savedGame.Units)
			{
				if (unit.Jobs == null || !unit.Jobs.Any())
				{
					continue;
				}
				Unit unitByid = engine.GetUnitByid(unit?.Id ?? (-1));
				if (unitByid == null)
				{
					continue;
				}
				foreach (ModelJob job in unit.Jobs)
				{
					JobType jobType = job.JobType;
					MissionSpec missionSpec = MissionSpecCreator.CreateMissionSpecFromType(jobType, unitByid);
					if (missionSpec != null)
					{
						ImportJob(missionSpec, job, unitByid);
						continue;
					}
					throw new ApplicationException("Failed to create mission spec for type: " + Enum.GetName(typeof(JobType), jobType));
				}
			}
		}

		private void ImportJob(MissionSpec job, ModelJob modelJob, Unit unit)
		{
			job.UniqueId = modelJob.Id;
			job.SetUnit(unit, updateParent: false);
			job.MissionPrefab = engine.GetMissionPrefab(modelJob.JobType);
			job.Init();
			job.SetFaction(engine.GetFactionByid(modelJob.Faction?.Id ?? (-1)), updateParent: false);
			job.ExpiryTime = modelJob.ExpiryTime;
			job.RewardCredits = modelJob.RewardCredits;
			job.ProfitCredits = modelJob.ProfitCredits;
			switch (job.JobType)
			{
			case JobType.Courier:
			{
				CourierMissionSpec courierMissionSpec = (CourierMissionSpec)job;
				ModelCourierJob modelCourierJob = (ModelCourierJob)modelJob;
				courierMissionSpec.PickupUnit = engine.GetUnitByid(modelCourierJob.PickupUnit?.Id ?? (-1));
				courierMissionSpec.DestinationUnit = engine.GetUnitByid(modelCourierJob.DestinationUnit?.Id ?? (-1));
				courierMissionSpec.CargoItem = new CargoBayItem
				{
					CargoClass = engine.GetCargoClassById((int)modelCourierJob.Cargo.CargoClass),
					Quantity = modelCourierJob.Cargo.Quantity
				};
				break;
			}
			case JobType.DeliverShip:
			{
				DeliverShipMissionSpec deliverShipMissionSpec = (DeliverShipMissionSpec)job;
				ModelDeliverShipJob modelDeliverShipJob = (ModelDeliverShipJob)modelJob;
				deliverShipMissionSpec.UnitClass = engine.GetUnitClassById((int)modelDeliverShipJob.UnitClass);
				deliverShipMissionSpec.DestinationUnit = engine.GetUnitByid(modelDeliverShipJob.DestinationUnit?.Id ?? (-1));
				break;
			}
			case JobType.Breakdown:
			{
				BreakdownMissionSpec breakdownMissionSpec = (BreakdownMissionSpec)job;
				ModelBreakdownJob modelBreakdownJob = (ModelBreakdownJob)modelJob;
				breakdownMissionSpec.BreakdownUnitClass = engine.GetUnitClassById((int)modelBreakdownJob.BreakdownUnitClass);
				breakdownMissionSpec.BreakdownDestinationSector = engine.GetSectorById(modelBreakdownJob.BreakdownDestinationSector?.Id ?? (-1));
				breakdownMissionSpec.BreakdownDestinationSectorPosition = modelBreakdownJob.BreakdownDestinationPosition.ToVector3();
				break;
			}
			case JobType.DestroyGroup:
			{
				DestroyGroupMissionSpec destroyGroupMissionSpec = (DestroyGroupMissionSpec)job;
				ModelDestroyFleetJob modelDestroyFleetJob = (ModelDestroyFleetJob)modelJob;
				destroyGroupMissionSpec.TargetGroupParams = ImportFleetSpawnParams(modelDestroyFleetJob.FleetSpawnParams);
				break;
			}
			}
		}

		private FleetSpawnParams ImportFleetSpawnParams(ModelFleetSpawnParams modelFleetSpawnParams)
		{
			FleetSpawnParams fleetSpawnParams = new FleetSpawnParams();
			fleetSpawnParams.TargetSector = engine.GetSectorById(modelFleetSpawnParams.TargetSector?.Id ?? (-1));
			fleetSpawnParams.TargetSectorPosition = modelFleetSpawnParams.TargetPosition.ToVector3();
			fleetSpawnParams.TargetDock = engine.GetUnitByid(modelFleetSpawnParams.TargetDockUnit?.Id ?? (-1));
			fleetSpawnParams.FleetPrefab = EngineASX.LoadFleet(modelFleetSpawnParams.FleetResourceName);
			fleetSpawnParams.Faction = engine.GetFactionByid(modelFleetSpawnParams.Faction?.Id ?? (-1));
			fleetSpawnParams.ShipDesignation = modelFleetSpawnParams.ShipDesignation;
			fleetSpawnParams.HomeSector = engine.GetSectorById(modelFleetSpawnParams.HomeSector?.Id ?? (-1));
			fleetSpawnParams.HomeBase = engine.GetUnitByid(modelFleetSpawnParams.HomeBaseUnit?.Id ?? (-1));
			foreach (ModelFleetSpawnParamsItem item2 in modelFleetSpawnParams.Items)
			{
				FleetSpawnShipParams item = ImportFleetSpawnShipParams(item2);
				fleetSpawnParams.Ships.Add(item);
			}
			return fleetSpawnParams;
		}

		private FleetSpawnShipParams ImportFleetSpawnShipParams(ModelFleetSpawnParamsItem modelFleetSpawnParamsItem)
		{
			return new FleetSpawnShipParams
			{
				UnitClass = engine.GetUnitClassById((int)modelFleetSpawnParamsItem.UnitClass),
				PilotPrefab = EngineASX.LoadPerson(modelFleetSpawnParamsItem.PilotResourceName),
				ShipName = modelFleetSpawnParamsItem.ShipName
			};
		}

		private void ImportMissions()
		{
			foreach (ModelMission mission in savedGame.Missions)
			{
				ImportMission(mission);
			}
		}

		private void ImportMission(ModelMission modelMission)
		{
			if (!Enum.IsDefined(typeof(MissionType), modelMission.MissionType))
			{
				Debug.LogError($"Undefined mission type: {modelMission.MissionType}");
				return;
			}
			Mission mission = null;
			int id = modelMission.Id;
			mission = ((modelMission.MissionType != MissionType.Custom) ? EngineASX.Instance.InstantiateMission(modelMission.MissionType) : UnityObjectHelper.NewGameObject<Mission>());
			mission.UniqueId = id;
			mission.Title = modelMission.Title;
			mission.IsPrimary = modelMission.IsPrimary;
			mission.BroadcastMessages = modelMission.NotificationsEnabled;
			mission.gameObject.SetActive(modelMission.IsActive);
			mission.Init();
			int stageIndex = modelMission.StageIndex;
			mission.IsFinished = modelMission.IsFinished;
			mission.CompletionSuccess = modelMission.CompletionSuccess;
			mission.ShowInJournal = modelMission.ShowInJournal;
			mission.OwnerFaction = engine.GetFactionByid(modelMission.OwnerFaction?.Id ?? (-1));
			mission.MissionGiverFaction = engine.GetFactionByid(modelMission.MissionGiverFaction?.Id ?? (-1));
			mission.CompletionOpinionChange = modelMission.CompletionOpinionChange;
			mission.FailureOpinionChange = modelMission.FailureOpinionChange;
			mission.StartTime = modelMission.StartTime;
			mission.MissionRewardCredits = modelMission.RewardCredits;
			for (int i = 0; i < modelMission.Stages.Count; i++)
			{
				MissionStage missionStage = null;
				if (modelMission.MissionType == MissionType.Custom)
				{
					missionStage = UnityObjectHelper.NewGameObject<MissionStage>(mission.transform);
					mission.Stages.Add(missionStage);
				}
				else
				{
					missionStage = mission.Stages[i];
				}
				if (missionStage != null)
				{
					ImportMissionStage(missionStage, modelMission.Stages[i]);
				}
				else
				{
					Debug.LogError($"{mission}: Unknown/mission mission stage imported");
				}
			}
			mission.CurrentStage = mission.Stages.ItemOrDefault(stageIndex);
			for (int j = 0; j < modelMission.Objectives.Count; j++)
			{
				MissionObjective missionObjective = null;
				if (modelMission.MissionType == MissionType.Custom)
				{
					missionObjective = UnityObjectHelper.NewGameObject<MissionObjective>(mission.transform);
					mission.Objectives.Add(missionObjective);
				}
				else
				{
					missionObjective = mission.Objectives[j];
				}
				if (missionObjective != null)
				{
					ImportMissionObjective(missionObjective, modelMission.Objectives[j]);
					missionObjective.Init(mission);
				}
				else
				{
					Debug.LogError($"{mission}: Unknown/mission mission objective imported");
				}
			}
			if (mission is CourierMission)
			{
				CourierMission courierMission = (CourierMission)mission;
				ModelCourierMission modelCourierMission = (ModelCourierMission)modelMission;
				courierMission.PickupUnit = engine.GetUnitByid(modelCourierMission.PickupUnit?.Id ?? (-1));
				courierMission.DestinationUnit = engine.GetUnitByid(modelCourierMission.DestinationUnit?.Id ?? (-1));
				courierMission.CargoItem = new CargoBayItem
				{
					CargoClass = engine.GetCargoClassById((int)modelCourierMission.CargoItem.CargoClass),
					Quantity = modelCourierMission.CargoItem.Quantity
				};
				courierMission.HasPlayerPickedUpCargo = modelCourierMission.HasPlayerPickedUpCargo;
			}
			else if (mission is DestroyUnitsMission)
			{
				DestroyUnitsMission destroyUnitsMission = (DestroyUnitsMission)mission;
				ModelDestroyUnitsMission modelDestroyUnitsMission = (ModelDestroyUnitsMission)modelMission;
				foreach (ModelUnit targetUnit in modelDestroyUnitsMission.TargetUnits)
				{
					Unit unitByid = engine.GetUnitByid(targetUnit?.Id ?? (-1));
					if (unitByid != null)
					{
						destroyUnitsMission.TargetUnits.Add(unitByid);
					}
				}
				destroyUnitsMission.HasSetGroupHostileToPlayer = modelDestroyUnitsMission.HasSetGroupHostileToPlayer;
				destroyUnitsMission.TargetFaction = engine.GetFactionByid(modelDestroyUnitsMission.TargetFaction?.Id ?? (-1));
				destroyUnitsMission.TargetSector = engine.GetSectorById(modelDestroyUnitsMission.TargetSector?.Id ?? (-1));
				destroyUnitsMission.TargetGroup = engine.GetFleetByid(modelDestroyUnitsMission.TargetFleet?.Id ?? (-1));
			}
			else if (mission is DeliverShipMission)
			{
				DeliverShipMission deliverShipMission = (DeliverShipMission)mission;
				ModelDeliverShipMission modelDeliverShipMission = (ModelDeliverShipMission)modelMission;
				deliverShipMission.UnitClass = engine.GetUnitClassById((int)modelDeliverShipMission.UnitClass);
				deliverShipMission.Destination = engine.GetUnitByid(modelDeliverShipMission.DestinationUnit?.Id ?? (-1));
			}
			else if (mission is BreakdownMission)
			{
				BreakdownMission breakdownMission = (BreakdownMission)mission;
				ModelBreakdownMission modelBreakdownMission = (ModelBreakdownMission)modelMission;
				breakdownMission.BaseUnit = engine.GetUnitByid(modelBreakdownMission.BaseUnit?.Id ?? (-1));
				breakdownMission.BreakdownUnit = engine.GetUnitByid(modelBreakdownMission.BreakdownUnit?.Id ?? (-1));
			}
			mission.transform.SetParent(engine.World.transform);
			mission.transform.localPosition = Vector3.zero;
		}

		private void ImportMissionStage(MissionStage missionStage, ModelMissionStage modelMissionStage)
		{
			missionStage.CompletesMission = modelMissionStage.CompletesMission;
			missionStage.JournalEntry = modelMissionStage.JournalEntry;
			missionStage.MissionSuccess = modelMissionStage.MissionSuccess;
		}

		private void ImportMissionObjective(MissionObjective objective, ModelMissionObjective modelActiveJobObjective)
		{
			objective.Title = modelActiveJobObjective.Title;
			objective.Description = modelActiveJobObjective.Description;
			objective.IsOptional = modelActiveJobObjective.IsOptional;
			objective.Order = modelActiveJobObjective.Order;
			objective.gameObject.SetActive(modelActiveJobObjective.IsActive);
			objective.IsComplete = modelActiveJobObjective.IsComplete;
			objective.Success = modelActiveJobObjective.Success;
			objective.ShowInJournal = modelActiveJobObjective.ShowInJournal;
		}

		private void ImportUnits()
		{
			Debug.Log($"Importing {savedGame.Units.Count} units");
			foreach (ModelUnit unit in savedGame.Units)
			{
				ImportUnit(unit);
			}
		}

		private void ImportUnit(ModelUnit modelUnit)
		{
			Unit unit = SaveGameUtilities.CreateUnit(engine, modelUnit.Id, (int)modelUnit.Class, modelUnit.Sector?.Id ?? (-1));
			unit.Seed = modelUnit.Seed;
			unit.transform.localPosition = modelUnit.Position.ToVector3();
			if (unit.UnitType == UnitType.Planet)
			{
				unit.GetComponent<UnitPlanet>().Rotation = modelUnit.Rotation.ToVector3();
			}
			else
			{
				unit.transform.localRotation = Quaternion.Euler(modelUnit.Rotation.ToVector3());
			}
			unit.Faction = engine.GetFactionByid(modelUnit.Faction?.Id ?? (-1));
			unit.RpProvision = modelUnit.RpProvision;
			if (modelUnit.CargoData != null && unit.CargoComponent != null)
			{
				ImportCargoComponent(unit.CargoComponent, modelUnit.CargoData);
			}
			if (modelUnit.DebrisData != null && unit.DebrisComponent != null)
			{
				ImportDebrisComponent(unit.DebrisComponent, modelUnit.DebrisData);
			}
			if (modelUnit.AsteroidData != null && unit.Asteroid != null)
			{
				ImportAsteroidComponent(unit.Asteroid, modelUnit.AsteroidData);
			}
			if (modelUnit.ShipTraderData != null && unit.Components != null)
			{
				UnitShipTrader unitShipTrader = unit.Components.CustomShipTrader;
				if (unitShipTrader == null)
				{
					unitShipTrader = unit.Components.gameObject.AddComponent<UnitShipTrader>();
				}
				ImportShipTrader(unitShipTrader, modelUnit.ShipTraderData);
				unit.Components.UpdateShipTraderReference();
			}
			if (modelUnit.ProjectileData != null)
			{
				Projectile component = unit.GetComponent<Projectile>();
				if (component != null)
				{
					ImportUnitProjectile(component, modelUnit.ProjectileData);
				}
			}
			if (modelUnit.Radius.HasValue)
			{
				unit.Radius = modelUnit.Radius.Value;
			}
			if (modelUnit.Mass.HasValue)
			{
				unit.Mass = modelUnit.Mass.Value;
				if (unit.Mass < 0f)
				{
					unit.Mass = 0f;
				}
			}
			if (unit.UnitType == UnitType.GasCloud)
			{
				unit.GetComponent<UnitGasCloud>().ApplyRadius();
			}
			if (!string.IsNullOrWhiteSpace(modelUnit.CustomClassName))
			{
				unit.ClassName = modelUnit.CustomClassName;
			}
		}

		private void ImportUnitProjectile(Projectile projectile, ModelUnitProjectileData modelUnitProjectileData)
		{
			projectile.SourceUnit = engine.GetUnitByid(modelUnitProjectileData.SourceUnit?.Id ?? (-1));
			projectile.Target = engine.GetUnitByid(modelUnitProjectileData.TargetUnit?.Id ?? (-1));
			projectile.FireTime = modelUnitProjectileData.FireTime;
			projectile.RemainingMovement = modelUnitProjectileData.RemainingMovement;
			projectile.Damage = ImportDamageType(modelUnitProjectileData.DamageType);
		}

		private DamageType ImportDamageType(ModelDamageType modelDamageType)
		{
			return new DamageType
			{
				Damage = modelDamageType.Damage,
				MiningDamage = modelDamageType.MiningDamage,
				ShieldDamageType = modelDamageType.ShieldDamageType
			};
		}

		private void ImportShipTrader(UnitShipTrader unitShipTrader, ModelUnitShipTraderData modelUnitShipTraderData)
		{
			foreach (ModelUnitShipTraderItem item in modelUnitShipTraderData.Items)
			{
				UnitShipTraderItem unitShipTraderItem = ImportShipTraderItem(item);
				if (unitShipTraderItem.UnitClass != null)
				{
					unitShipTrader.ShipTradeItems.Add(unitShipTraderItem);
				}
			}
		}

		private UnitShipTraderItem ImportShipTraderItem(ModelUnitShipTraderItem modelUnitShipTraderItem)
		{
			return new UnitShipTraderItem
			{
				SellMultiplier = modelUnitShipTraderItem.SellMultiplier,
				UnitClass = engine.GetUnitClassById((int)modelUnitShipTraderItem.UnitClass)
			};
		}

		private void ImportWormholes()
		{
			foreach (ModelUnit unit in savedGame.Units)
			{
				if (unit.WormholeData != null)
				{
					Unit unitByid = engine.GetUnitByid(unit?.Id ?? (-1));
					if (unitByid != null)
					{
						ImportWormhole(unitByid, unit.WormholeData);
					}
					else
					{
						MissingUnitMessage(unit.Id);
					}
				}
			}
		}

		private void ImportWormhole(Unit unit, ModelUnitWormholeData modelUnitWormholeData)
		{
			Wormhole wormholeComponent = unit.WormholeComponent;
			Unit unitByid = engine.GetUnitByid(modelUnitWormholeData.TargetWormholeUnit?.Id ?? (-1));
			if (unitByid != null)
			{
				wormholeComponent.TargetGate = unitByid.GetComponent<Wormhole>();
				wormholeComponent.OnTargetGateChanged();
			}
			wormholeComponent.IsUnstable = modelUnitWormholeData.IsUnstable;
			wormholeComponent.NextUnstableChgTargetTime = modelUnitWormholeData.UnstableNextChangeTargetTime;
			wormholeComponent.ManualTargetSectorPosition = modelUnitWormholeData.UnstableTargetPosition.ToVector3();
			wormholeComponent.ManualTargetRotation = modelUnitWormholeData.UnstableTargetRotation.ToVector3();
			wormholeComponent.ManualTargetSector = engine.GetSectorById(modelUnitWormholeData.UnstableTargetSector?.Id ?? (-1));
		}

		private void ImportHangars()
		{
			foreach (ModelUnit unit in savedGame.Units)
			{
				if (unit.ComponentUnitData != null && unit.ComponentUnitData.DockData != null)
				{
					Unit unitByid = engine.GetUnitByid(unit?.Id ?? (-1));
					if (unitByid != null)
					{
						ImportHangar(unitByid.Components.HangarComponent, unit.ComponentUnitData.DockData);
					}
					else
					{
						MissingUnitMessage(unit.Id);
					}
				}
			}
		}

		private void ImportHangar(UnitHangar hangar, ModelComponentUnitDockData modelComponentUnitDockData)
		{
			foreach (ModelComponentUnitDockDataItem item in modelComponentUnitDockData.Items)
			{
				Unit unitByid = engine.GetUnitByid(item.DockedUnit?.Id ?? (-1));
				UnitHangarBay bayById = hangar.GetBayById(item.BayId);
				if (unitByid != null)
				{
					if (bayById != null)
					{
						unitByid.Components.DockedInHangarBay = bayById;
						continue;
					}
					throw new Exception($"EngineIO: Unknown bay iD: {item.BayId} of unit {hangar.UnitComponents.Unit.UniqueId}");
				}
				throw new Exception("EngineIO: Unknown docked unit. iD: " + item.DockedUnit.Id);
			}
		}

		private void ImportUnitComponents(UnitComponentHolder unitComponents, ModelComponentUnitData modelComponentUnitData)
		{
			int shipNameIndex = modelComponentUnitData.ShipNameIndex;
			if (shipNameIndex > -1)
			{
				unitComponents.AssignAndReserveShipName(engine.ShipNames, shipNameIndex);
			}
			else
			{
				unitComponents.ShipName = modelComponentUnitData.CustomShipName;
			}
			if (modelComponentUnitData.CargoCapacity.HasValue)
			{
				unitComponents.CargoCapacity = modelComponentUnitData.CargoCapacity.Value;
			}
			if (modelComponentUnitData.ScanRange.HasValue)
			{
				unitComponents.ScanRange = modelComponentUnitData.ScanRange.Value;
			}
			if (modelComponentUnitData.FactoryData != null)
			{
				UnitCargoFactory component = unitComponents.GetComponent<UnitCargoFactory>();
				int count = modelComponentUnitData.FactoryData.Items.Count;
				if (component == null || count != component.Items.Count)
				{
					Debug.LogError($"Factory items for unit \"{unitComponents.Unit.name}\" in scene {unitComponents.Unit.Sector.Name} id: {unitComponents.Unit.UniqueId} have been changed from prefab. This is not supported");
				}
				else
				{
					for (int i = 0; i < modelComponentUnitData.FactoryData.Items.Count; i++)
					{
						ModelComponentUnitFactoryItemData modelComponentUnitFactoryItemData = modelComponentUnitData.FactoryData.Items[i];
						CargoFactoryItemState state = modelComponentUnitFactoryItemData.State;
						float productionElapsed = modelComponentUnitFactoryItemData.ProductionElapsed;
						if (i < component.Items.Count)
						{
							component.Items[i].State = state;
							component.Items[i].ProductionElapsed = productionElapsed;
						}
					}
					component.Init();
				}
			}
			unitComponents.ConstructionState = modelComponentUnitData.ConstructionState;
			if (unitComponents.ConstructionState != ConstructionState.Constructed)
			{
				unitComponents.ConstructionProgress = modelComponentUnitData.ConstructionProgress;
			}
			else
			{
				unitComponents.ConstructionProgress = 1f;
			}
			unitComponents.CaptureCooldownTime = modelComponentUnitData.CaptureCooldownTime.GetValueOrDefault();
			if (modelComponentUnitData.AutoTurretFireMode.HasValue)
			{
				unitComponents.InitAutoTurretModuleIfNull();
				unitComponents.AutoTurretModule.FireMode = modelComponentUnitData.AutoTurretFireMode.Value;
			}
		}

		private void ImportAsteroidComponent(Asteroid asteroid, ModelUnitAsteroidData modelUnitAsteroidData)
		{
			asteroid.RemainingYield = modelUnitAsteroidData.RemainingYield;
		}

		private void ImportDebrisComponent(UnitDebris debris, ModelUnitDebrisData modelUnitDebrisData)
		{
			debris.ScrapQuantity = modelUnitDebrisData.ScrapQuantity;
			debris.Expires = modelUnitDebrisData.Expires;
			debris.ExpiryTime = modelUnitDebrisData.ExpiryTime;
			debris.RelatedUnitClass = engine.GetUnitClassById((int)modelUnitDebrisData.RelatedUnitClass);
			debris.UpdateName();
		}

		private void ImportCargoComponent(Cargo cargo, ModelUnitCargoData modelUnitCargoData)
		{
			cargo.CargoClass = engine.GetCargoClassById((int)modelUnitCargoData.CargoClass);
			cargo.Quantity = modelUnitCargoData.Quantity;
			cargo.Expires = modelUnitCargoData.Expires;
			cargo.SpawnTime = modelUnitCargoData.SpawnTime;
			cargo.SetHealthBasedOnVolume();
		}

		private void ImportUnitCargo(UnitComponentHolder ship, ModelComponentUnitCargoData modelComponentUnitCargoData)
		{
			foreach (ModelComponentUnitCargoDataItem item in modelComponentUnitCargoData.Items)
			{
				CargoClass cargoClassById = engine.GetCargoClassById((int)item.CargoClass);
				int quantity = item.Quantity;
				if (cargoClassById != null)
				{
					ship.CargoBayComponent.AddToCargo(cargoClassById, quantity, ignoreCapacity: true);
				}
			}
		}

		private void MissingComponentClassError(int unitId, int bayId, int componentClassId)
		{
			Debug.LogErrorFormat("Failed to find component class with id {0} for unit \"{1}\" on bay id {2}. Cannot install component", componentClassId, unitId, bayId);
		}

		private void GetAttitudesToPlayer(EngineASX engine, out FactionAttitude[] attitudes, out Faction[] factions)
		{
			List<FactionAttitude> list = new List<FactionAttitude>();
			List<Faction> list2 = new List<Faction>();
			foreach (Faction faction in engine.Factions)
			{
				if (faction != engine.LocalPlayer.Faction && faction.HasAttitudeToFaction(engine.LocalPlayer.Faction))
				{
					list.Add(faction.GetAttitude(engine.LocalPlayer.Faction));
					list2.Add(faction);
				}
			}
			attitudes = list.ToArray();
			factions = list2.ToArray();
		}

		private SectorTarget ImportSectorTarget(ModelSectorTarget modelSectorTarget)
		{
			if (modelSectorTarget == null)
			{
				return null;
			}
			return new SectorTarget
			{
				SectorPosition = modelSectorTarget.Position.ToVector3(),
				Sector = engine.GetSectorById(modelSectorTarget.Sector?.Id ?? (-1)),
				TargetUnit = engine.GetUnitByid(modelSectorTarget.TargetUnit?.Id ?? (-1)),
				TargetFleet = engine.GetFleetByid(modelSectorTarget.TargetFleet?.Id ?? (-1)),
				HadSceneObject = modelSectorTarget.HadValidTarget
			};
		}

		private Unit GetUnit(ModelUnit modelUnit)
		{
			return engine.GetUnitByid(modelUnit?.Id ?? (-1));
		}

		private Mission GetMission(ModelMission modelMission)
		{
			return engine.GetMissionById(modelMission?.Id ?? (-1));
		}

		private MissionObjective GetMissionObjective(ModelMissionObjective modelMissionObjective)
		{
			return engine.GetMissionObjectiveByid(modelMissionObjective?.Id ?? (-1));
		}

		private TriggerGroup GetTriggerGroup(ModelTriggerGroup modelTriggerGroup)
		{
			return engine.GetTriggerGroupById(modelTriggerGroup?.Id ?? (-1));
		}
	}
}
