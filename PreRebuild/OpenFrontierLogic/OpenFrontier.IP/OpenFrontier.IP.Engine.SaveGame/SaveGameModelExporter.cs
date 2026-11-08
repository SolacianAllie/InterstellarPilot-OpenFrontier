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
using OpenFrontier.IP.Engine.DitchShip;
using OpenFrontier.IP.Engine.EngineActions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.Bounty;
using OpenFrontier.IP.Engine.Fleets.ActiveOrders;
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
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.ActiveOrderTypes;
using Random = UnityEngine.Random;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.Models;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs.JobTypes;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs.Missions;
using OpenFrontier.IP.SavedGames.V2.Model.Scenarios;
using OpenFrontier.IP.SavedGames.V2.Model.Triggers;
using OpenFrontier.IP.Scenarios;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.SaveGame
{
	public class SaveGameModelExporter : ISaveGameExporter
	{
		private SavedGame savedGame;

		private EngineASX engine;

		private List<FleetOrder> fleetOrderCache = new List<FleetOrder>(8);

		private Dictionary<int, ModelFleet> fleetsById = new Dictionary<int, ModelFleet>(100);

		private Dictionary<int, ModelUnit> unitsById = new Dictionary<int, ModelUnit>(1000);

		private Dictionary<int, ModelSector> sectorsById = new Dictionary<int, ModelSector>(100);

		private Dictionary<int, ModelFaction> factionsById = new Dictionary<int, ModelFaction>(1000);

		private Dictionary<int, ModelPerson> peopleById = new Dictionary<int, ModelPerson>(100);

		private Dictionary<int, ModelPassengerGroup> passengerGroupsById = new Dictionary<int, ModelPassengerGroup>(500);

		private Dictionary<int, ModelTriggerGroup> triggerGroupsById = new Dictionary<int, ModelTriggerGroup>();

		private Dictionary<int, ModelMissionObjective> missionObjectivesById = new Dictionary<int, ModelMissionObjective>();

		public ModelHeader ExportHeader(EngineASX engine, Version saveVersion, bool isAutosave)
		{
			ModelHeader modelHeader = new ModelHeader
			{
				Version = saveVersion,
				CreatedVersion = engine.World.CreatedVersion,
				IsAutoSave = isAutosave,
				TimeStamp = DateTime.Now,
				ScenarioInfoId = engine.World.ScenarioInfo.UniqueId,
				GameStartDate = engine.World.GetScenarioStartDateTime(),
				GlobalSaveNumber = -1,
				SaveNumber = engine.World.SaveGameCount,
				Permadeath = engine.World.ScenarioOptions.Permadeath,
				SecondsElapsed = engine.ScenarioElapsedTime,
				ScenarioTitle = engine.World.ScenarioTitle,
				ScenarioAuthor = engine.World.ScenarioAuthor,
				ScenarioAuthoringTool = engine.World.ScenarioAuthoringTool
			};
			modelHeader.HavePlayer = engine.LocalPlayer != null;
			if (engine.LocalPlayer != null)
			{
				modelHeader.PlayerSectorName = ((engine.LocalPlayerSector != null) ? engine.LocalPlayerSector.Name : null);
				modelHeader.PlayerName = engine.LocalPlayer.Person.CustomName;
				modelHeader.Credits = engine.LocalPlayer.Credits;
				if (engine.LocalPlayer.Faction != null)
				{
					modelHeader.NetWorth = engine.LocalPlayer.Faction.CalculateNetWorth();
					modelHeader.FactionName = engine.LocalPlayer.Faction.GetFriendlyName();
				}
			}
			return modelHeader;
		}

		public ISavedGame Export(EngineASX engine)
		{
			savedGame = new SavedGame();
			this.engine = engine;
			ExportSectors();
			ExportFactions();
			ExportPatrolPaths();
			ExportFactionRelations();
			ExportFactionOpinions();
			ExportUnits();
			ExportUnitComponents();
			ExportFactionIntel();
			ExportPassengerGroups();
			ExportWormholes();
			ExportDockedUnits();
			ExportFleets();
			ExportPeople();
			ExportFleetOrders();
			ExportJobs();
			ExportFactionAIs();
			ExportBountyBoards();
			ExportFleetSpawners();
			ExportMissions();
			savedGame.Player = ExportPlayer(engine.LocalPlayer);
			savedGame.CurrentHudTarget = GetModelUnit((engine.Hud.CurrentTarget != null) ? engine.Hud.CurrentTarget : null);
			ExportFactionTransactions();
			savedGame.ScenarioData = ExportScenarioData();
			ExportMoons();
			ExportSeedOptions();
			ExportTriggerGroups();
			savedGame.EngineData = ExportEngineData();
			ExportDitchedUnits();
			ExportPlayerUnitFleetSettings();
			ExportPlayerDefaultFleetSettings();
			ExportTractoringUnits();
			return savedGame;
		}

		private void ExportTractoringUnits()
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
					if (item.Tractorer != null && item.Tractorer.IsPullingUnit)
					{
						savedGame.TractorerDataItems.Add(new ModelTractorerDataItem
						{
							TractoredUnit = GetModelUnit(item),
							TractoringUnit = GetModelUnit(item.Tractorer.Unit)
						});
					}
				}
			}
		}

		private void ExportPlayerDefaultFleetSettings()
		{
			if (EngineASX.Instance.CachedFleetSettingsController.DefaultFleetSettings != null)
			{
				savedGame.PlayerDefaultFleetSettings = new ModelPlayerFleetSettingsCombined
				{
					FleetSettings = EngineASX.Instance.CachedFleetSettingsController.DefaultFleetSettings.FleetSettings,
					HomeBase = GetModelSectorTarget(EngineASX.Instance.CachedFleetSettingsController.DefaultFleetSettings.HomeBase),
					FormationId = EngineASX.Instance.CachedFleetSettingsController.DefaultFleetSettings.FormationId
				};
			}
		}

		private void ExportPlayerUnitFleetSettings()
		{
			foreach (KeyValuePair<int, CachedFleetSettingsItem> item in EngineASX.Instance.CachedFleetSettingsController.Items)
			{
				Unit unitByid = EngineASX.Instance.GetUnitByid(item.Key);
				if (unitByid != null && unitByid.IsOwnedByPlayer)
				{
					CachedFleetSettingsItem value = item.Value;
					ModelPlayerUnitFleetSettings modelPlayerUnitFleetSettings = new ModelPlayerUnitFleetSettings();
					modelPlayerUnitFleetSettings.Unit = GetModelUnit(unitByid);
					modelPlayerUnitFleetSettings.Settings = new ModelPlayerFleetSettingsCombined();
					modelPlayerUnitFleetSettings.Settings.FleetSettings = value.FleetSettings;
					modelPlayerUnitFleetSettings.Settings.HomeBase = GetModelSectorTarget(value.HomeBase);
					modelPlayerUnitFleetSettings.Settings.FormationId = value.FormationId;
					savedGame.PlayerUnitFleetSettingItems.Add(modelPlayerUnitFleetSettings);
				}
			}
		}

		private void ExportDitchedUnits()
		{
			foreach (DitchUnitCleanupModule.DitchedUnitCleanupItem cleanupItem in EngineASX.Instance.DitchUnitCleanupModule.CleanupItems)
			{
				ModelUnit modelUnit = GetModelUnit(cleanupItem.Unit);
				if (modelUnit != null)
				{
					savedGame.DitchedUnitsToBeCleanedUp.Add(new ModelDitchedUnit
					{
						Unit = modelUnit,
						ExpiryTime = (int)cleanupItem.TimeOfExpiry
					});
				}
			}
		}

		private ModelEngineData ExportEngineData()
		{
			return new ModelEngineData
			{
				UnitIdCounter = EngineASX.Instance.UnitIdCounter,
				PlayerMessageIdCounter = EngineASX.Instance.PlayerMessageIdCounter,
				PersonIdCounter = EngineASX.Instance.PersonIdCounter,
				FactionIdCounter = EngineASX.Instance.FactionIdCounter,
				FleetOrderIdCounter = EngineASX.Instance.FleetOrderIdCounter,
				PassengerGroupIdCounter = EngineASX.Instance.PassengerGroupIdCounter,
				JobIdCounter = EngineASX.Instance.JobIdCounter,
				FleetIdCounter = EngineASX.Instance.FleetIdCounter,
				SectorIdCounter = EngineASX.Instance.SectorIdCounter,
				MissionIdCounter = EngineASX.Instance.MissionIdCounter,
				PatrolPathIdCounter = EngineASX.Instance.PatrolPathIdCounter,
				MissionObjectiveIdCounter = EngineASX.Instance.MissionObjectiveIdCounter
			};
		}

		private void ExportSeedOptions()
		{
			if (engine.World.Seeder != null && engine.World.Seeder.Settings.CustomScenarioSeederSettings != null)
			{
				CustomScenarioSeederSettings customScenarioSeederSettings = engine.World.Seeder.Settings.CustomScenarioSeederSettings;
				savedGame.SeedOptions = new ModelSeedOptions
				{
					SeedAbandonedCargo = customScenarioSeederSettings.SeedAbandonedCargo,
					SeedAbandonedShips = customScenarioSeederSettings.SeedAbandonedShips,
					SeedCargoHolds = customScenarioSeederSettings.SeedCargoHolds,
					SeedFactionIntel = customScenarioSeederSettings.SeedFactionIntel,
					SeedPassengerGroups = customScenarioSeederSettings.SeedPassengerGroups
				};
			}
		}

		private void ExportMoons()
		{
			EngineASX.Instance.EnumerateUnitsOfType(UnitType.Planet, (Unit unit) =>
			{
				Moon component = unit.GetComponent<Moon>();
				if (component != null)
				{
					savedGame.Moons.Add(new ModelMoon
					{
						Unit = GetModelUnit(unit),
						OrbitUnit = GetModelUnit(component.OrbitingAroundUnit),
						OffsetFromPlanet = component.OffsetFromPlanet.ToVec3()
					});
				}
			});
		}

		private ModelScenarioData ExportScenarioData()
		{
			ModelScenarioData modelScenarioData = new ModelScenarioData();
			if (EngineASX.Instance.FactionSpawner != null && EngineASX.Instance.FactionSpawner.gameObject.activeSelf)
			{
				modelScenarioData.FactionSpawner = new ModelFactionSpawner
				{
					NextUpdate = EngineASX.Instance.FactionSpawner.NextFactionSpawnTime
				};
			}
			modelScenarioData.RespawnOnDeath = EngineASX.Instance.World.ScenarioOptions.RespawnOnDeath;
			modelScenarioData.AllowTeleporting = EngineASX.Instance.World.ScenarioOptions.AllowTeleporting;
			modelScenarioData.Permadeath = EngineASX.Instance.World.ScenarioOptions.Permadeath;
			modelScenarioData.AsteroidRespawningEnabled = EngineASX.Instance.World.ScenarioOptions.AsteroidRespawningEnabled;
			modelScenarioData.AsteroidRespawnTime = EngineASX.Instance.World.ScenarioOptions.AsteroidRespawnTime;
			modelScenarioData.AllowStationCapture = EngineASX.Instance.World.ScenarioOptions.AllowStationCapture;
			modelScenarioData.AllowAbandonShip = EngineASX.Instance.World.ScenarioOptions.AllowAbandonShip;
			float nextProcessOtherEventsTime = (float)EngineASX.Instance.ScenarioElapsedTime + UnityEngine.Random.Range(0f, 10f);
			if (EngineASX.Instance.World.HasCheated)
			{
				nextProcessOtherEventsTime = (float)EngineASX.Instance.ScenarioElapsedTime + UnityEngine.Random.Range(11f, 12f);
			}
			modelScenarioData.NextProcessOtherEventsTime = nextProcessOtherEventsTime;
			if (EngineASX.Instance.RandomEventsController != null && EngineASX.Instance.RandomEventsController.gameObject.activeSelf)
			{
				modelScenarioData.HasRandomEvents = true;
				modelScenarioData.NextRandomEventTime = EngineASX.Instance.RandomEventsController.NextEventTime;
			}
			if (EngineASX.Instance.World is TradeRouteScenarioWorld tradeRouteScenarioWorld)
			{
				modelScenarioData.TradeRouteScenarioData = new ModelTradeRouteScenarioData
				{
					NumBlackSailShipsDestroyed = tradeRouteScenarioWorld.NumBlackSailShipsDestroyed,
					PirateFaction = GetModelFaction(tradeRouteScenarioWorld.PirateFaction)
				};
			}
			return modelScenarioData;
		}

		private void ExportFactionTransactions()
		{
			foreach (Faction item in engine.Factions.Where((Faction e) => e.IsPlayerFaction))
			{
				item.RecentTransactions.TrimNulls();
				GetModelFaction(item).Transactions = item.RecentTransactions.Select((FactionTransaction e) => new ModelFactionTransaction
				{
					CurrentBalance = e.CurrentBalance,
					GameWorldTime = e.GameWorldTime,
					LocationUnit = GetModelUnit(e.Location),
					OtherFaction = GetModelFaction(e.OtherFaction),
					RelatedCargoClass = GetModelCargoClass(e.RelatedCargoClass),
					RelatedUnitClass = GetModelUnitClass(e.RelatedUnitClass),
					TransactionType = e.TransactionType,
					TaxType = e.TaxType,
					Value = e.Value,
					RelatedCount = e.RelatedCount
				}).ToList();
			}
		}

		private ModelPlayer ExportPlayer(GamePlayer player)
		{
			if (player != null)
			{
				ModelPlayer modelPlayer = new ModelPlayer
				{
					VisitedUnits = (from e in player.VisitedUnits
						select GetModelUnit(e) into e
						where e != null
						select e).ToList(),
					ActiveJob = GetModelMission(player.ActiveMission),
					Messages = player.Messages.Select((PlayerActiveMessage e) => ExportPlayerMessage(e)).ToList(),
					DelayedMessages = player.DelayedMessages.Select((GamePlayer.delayedMessage e) => ExportPlayerDelayedMessage(e)).ToList(),
					Person = GetModelPerson(player.Person),
					CustomWaypoint = ExportCustomWaypoint(player.WaypointController.CustomPathWaypoint)
				};
				if (player.Stats != null)
				{
					modelPlayer.Stats.SectorsVisited = player.Stats.VisistedSectorIds.Select((int e) => GetModelSector(e)).ToList();
					modelPlayer.Stats.TotalBountyClaimed = player.Stats.TotalBountyClaimed;
					modelPlayer.Stats.ShipsMinedToDeath = player.Stats.ShipsMinedToDeath;
				}
				return modelPlayer;
			}
			return null;
		}

		private ModelPlayerWaypoint ExportCustomWaypoint(object customPathWaypoint)
		{
			throw new NotImplementedException();
		}

		private ModelPlayerWaypoint ExportCustomWaypoint(PlayerWaypoint? playerWaypoint)
		{
			if (!playerWaypoint.HasValue)
			{
				return null;
			}
			return new ModelPlayerWaypoint
			{
				HadTargetObject = playerWaypoint.Value.HadSceneObject,
				SectorPosition = playerWaypoint.Value.SectorPosition.ToVec3(),
				Sector = GetModelSector(playerWaypoint.Value.Sector),
				TargetUnit = GetModelUnit(playerWaypoint.Value.TargetUnit)
			};
		}

		private ModelPlayerMessage ExportPlayerMessage(PlayerActiveMessage activeMessage)
		{
			return new ModelPlayerMessage
			{
				Id = activeMessage.UniqueId,
				EngineTimeStamp = activeMessage.EngineTimeStamp,
				AllowDelete = activeMessage.AllowDelete,
				Opened = activeMessage.Opened,
				SenderUnit = GetModelUnit(activeMessage.SenderUnit),
				SenderUnitSector = GetModelSector(activeMessage.SenderUnitSector),
				SenderUnitSectorPosition = activeMessage.SenderUnitSectorPosition.ToVec3(),
				SubjectUnit = GetModelUnit(activeMessage.SubjectUnit),
				SubjectUnitSector = GetModelSector(activeMessage.SubjectUnitSector),
				SubjectUnitSectorPosition = activeMessage.SubjectUnitSectorPosition.ToVec3(),
				MessageTemplateId = ((activeMessage.MessageTemplate != null) ? activeMessage.MessageTemplate.UniqueId : (-1)),
				FromText = activeMessage.FromText,
				ToText = activeMessage.ToText,
				MessageText = activeMessage.MessageText,
				SubjectText = activeMessage.SubjectText
			};
		}

		private ModelPlayerDelayedMessage ExportPlayerDelayedMessage(GamePlayer.delayedMessage delayedMessage)
		{
			return new ModelPlayerDelayedMessage
			{
				ShowTime = delayedMessage.ShowTime,
				Message = ExportPlayerMessage(delayedMessage.Message),
				Important = delayedMessage.Important,
				Notifications = delayedMessage.Notifications
			};
		}

		private ModelMission GetModelMission(Mission activeMission)
		{
			if (activeMission == null)
			{
				return null;
			}
			return GetModelActiveJob(activeMission.UniqueId);
		}

		private ModelMission GetModelActiveJob(int? uniqueId)
		{
			return savedGame.Missions.FirstOrDefault((ModelMission e) => e.Id == uniqueId);
		}

		private ModelMissionObjective GetModelMissionObjective(MissionObjective objective)
		{
			if (objective == null)
			{
				return null;
			}
			return GetModelMissionObjective(objective.UniqueId);
		}

		public ModelTriggerGroup GetModelTriggerGroup(TriggerGroup triggerGroup)
		{
			if (triggerGroup == null)
			{
				return null;
			}
			return triggerGroupsById.GetValueOrDefault(triggerGroup.UniqueId);
		}

		private ModelMissionObjective GetModelMissionObjective(int? uniqueId)
		{
			return missionObjectivesById.GetValueOrDefault(uniqueId ?? (-1));
		}

		private void ExportMissions()
		{
			foreach (Mission item in engine.Missions.Where((Mission e) => e != null))
			{
				savedGame.Missions.Add(ExportMission(item));
			}
		}

		private ModelMission ExportMission(Mission mission)
		{
			ModelMission modelMission = CreateMissionFromMissionType.Create(mission.MissionType);
			modelMission.Id = mission.UniqueId;
			modelMission.Title = mission.Title;
			modelMission.IsPrimary = mission.IsPrimary;
			modelMission.NotificationsEnabled = mission.BroadcastMessages;
			modelMission.IsActive = mission.gameObject.activeSelf;
			modelMission.StageIndex = mission.Stages.IndexOf(mission.CurrentStage);
			modelMission.IsFinished = mission.IsFinished;
			modelMission.CompletionSuccess = mission.CompletionSuccess;
			modelMission.ShowInJournal = mission.ShowInJournal;
			modelMission.OwnerFaction = GetModelFaction(mission.OwnerFaction);
			modelMission.MissionGiverFaction = GetModelFaction(mission.MissionGiverFaction);
			modelMission.CompletionOpinionChange = mission.CompletionOpinionChange;
			modelMission.FailureOpinionChange = mission.FailureOpinionChange;
			modelMission.StartTime = mission.StartTime;
			modelMission.RewardCredits = mission.MissionRewardCredits;
			modelMission.Stages = mission.Stages.Select((MissionStage e) => new ModelMissionStage
			{
				Mission = modelMission,
				CompletesMission = e.CompletesMission,
				JournalEntry = e.JournalEntry,
				MissionSuccess = e.MissionSuccess
			}).ToList();
			modelMission.Objectives = mission.Objectives.Select((MissionObjective e) => new ModelMissionObjective
			{
				Id = e.UniqueId,
				Title = e.Title,
				Description = e.Description,
				IsOptional = e.IsOptional,
				Order = e.Order,
				IsActive = e.gameObject.activeSelf,
				IsComplete = e.IsComplete,
				ShowInJournal = e.ShowInJournal,
				Success = e.Success
			}).ToList();
			foreach (ModelMissionObjective objective in modelMission.Objectives)
			{
				missionObjectivesById.Add(objective.Id, objective);
			}
			if (!(modelMission is ModelCourierMission modelCourierMission))
			{
				if (!(modelMission is ModelDestroyUnitsMission modelDestroyUnitsMission))
				{
					if (!(modelMission is ModelDeliverShipMission modelDeliverShipMission))
					{
						if (modelMission is ModelBreakdownMission modelBreakdownMission)
						{
							BreakdownMission breakdownMission = (BreakdownMission)mission;
							modelBreakdownMission.BaseUnit = GetModelUnit(breakdownMission.BaseUnit);
							modelBreakdownMission.BreakdownUnit = GetModelUnit(breakdownMission.BreakdownUnit);
						}
					}
					else
					{
						DeliverShipMission deliverShipMission = (DeliverShipMission)mission;
						modelDeliverShipMission.UnitClass = GetModelUnitClass(deliverShipMission.UnitClass);
						modelDeliverShipMission.DestinationUnit = GetModelUnit(deliverShipMission.Destination);
					}
				}
				else
				{
					DestroyUnitsMission destroyUnitsMission = (DestroyUnitsMission)mission;
					modelDestroyUnitsMission.TargetUnits = destroyUnitsMission.TargetUnits.Select((Unit e) => GetModelUnit(e)).ToList();
					modelDestroyUnitsMission.HasSetGroupHostileToPlayer = destroyUnitsMission.HasSetGroupHostileToPlayer;
					modelDestroyUnitsMission.TargetFaction = GetModelFaction(destroyUnitsMission.TargetFaction);
					modelDestroyUnitsMission.TargetSector = GetModelSector(destroyUnitsMission.TargetSector);
					modelDestroyUnitsMission.TargetFleet = GetModelFleet(destroyUnitsMission.TargetGroup);
				}
			}
			else
			{
				CourierMission courierMission = (CourierMission)mission;
				modelCourierMission.PickupUnit = GetModelUnit(courierMission.PickupUnit);
				modelCourierMission.DestinationUnit = GetModelUnit(courierMission.DestinationUnit);
				modelCourierMission.CargoItem = new ModelComponentUnitCargoDataItem
				{
					CargoClass = GetModelCargoClass(courierMission.CargoItem.CargoClass),
					Quantity = courierMission.CargoItem.Quantity
				};
				modelCourierMission.HasPlayerPickedUpCargo = courierMission.HasPlayerPickedUpCargo;
			}
			return modelMission;
		}

		private void ExportFleetSpawners()
		{
			engine.FleetSpawners.TrimNulls();
			foreach (FleetSpawner fleetSpawner in engine.FleetSpawners)
			{
				savedGame.FleetSpawners.Add(ExportFleetSpawner(fleetSpawner));
			}
		}

		private ModelFleetSpawner ExportFleetSpawner(FleetSpawner groupSpawner)
		{
			return new ModelFleetSpawner
			{
				Name = groupSpawner.name,
				Position = groupSpawner.Sector.ToLocalPosition(groupSpawner.transform.position).ToVec3(),
				Rotation = groupSpawner.transform.rotation.ToVec4(),
				InitialSpawnTimeRandomness = groupSpawner.InitialSpawnTimeRandomness,
				SpawnTimeRandomness = groupSpawner.SpawnTimeRandomness,
				ShipDesignation = groupSpawner.ShipDesignation,
				ShipName = groupSpawner.ShipName,
				NamePrefix = groupSpawner.NamePrefix,
				SpawnCounter = groupSpawner.SpawnCounter,
				RespawnWhenNoObjectives = groupSpawner.RespawnWhenNoObjectives,
				RespawnWhenNoPilots = groupSpawner.RespawnWhenNoPilots,
				AllowRespawnInActiveScene = groupSpawner.AllowRespawnInActiveScene,
				FleetHomeBase = GetModelUnit(groupSpawner.HomeBaseUnit),
				FleetHomeSector = GetModelSector(groupSpawner.HomeSector),
				Sector = GetModelSector(groupSpawner.Sector),
				SpawnDock = GetModelUnit(groupSpawner.SpawnDock),
				NextSpawnTime = groupSpawner.NextSpawnTime,
				MinTimeBeforeSpawn = groupSpawner.MinTimeBeforeSpawn,
				MaxTimeBeforeSpawn = groupSpawner.MaxTimeBeforeSpawn,
				MinGroupUnitCount = groupSpawner.MinUnitCount,
				MaxGroupUnitCount = groupSpawner.MaxUnitCount,
				SpawnedFleet = GetModelFleet(groupSpawner.SpawnedGroup),
				UnitClasses = groupSpawner.UnitClasses.Select((UnitClass e) => GetModelUnitClass(e)).ToList(),
				PilotResourceNames = groupSpawner.PilotPrefabs.Select((Person e) => e.name).ToList(),
				FleetResourceName = ((groupSpawner.Prefab != null) ? groupSpawner.Prefab.name : null),
				Orders = groupSpawner.Objectives.Select((FleetOrder e) => ExportFleetOrder(e)).ToList()
			};
		}

		private void ExportBountyBoards()
		{
			foreach (Faction faction in engine.Factions)
			{
				if (faction.BountyBoard != null && faction.BountyBoard.BountyItems.Any())
				{
					ModelFaction modelFaction = GetModelFaction(faction);
					modelFaction.BountyBoard = new ModelFactionBountyBoard();
					modelFaction.BountyBoard.Items = (from e in faction.BountyBoard.BountyItems
						where e.IsValid
						select new ModelFactionBountyBoardItem
						{
							LastKnownTargetPosition = e.LastKnownSectorPosition?.ToVec3(),
							LastKnownTargetSector = GetModelSector(e.LastKnownSector),
							LastKnownTargetUnit = GetModelUnit(e.LastKnownPilottedShip),
							TimeOfLastSighting = e.TimeOfLastSighting,
							Reward = e.Bounty,
							SourceFaction = GetModelFaction(e.Source),
							TargetPerson = GetModelPerson(e.Person)
						}).ToList();
				}
			}
		}

		private void ExportFactionAIs()
		{
			engine.Factions.TrimNulls();
			foreach (Faction faction in engine.Factions)
			{
				if (faction.FactionAI != null)
				{
					GetModelFaction(faction).FactionAI = ExportFactionAI(faction.FactionAI);
				}
			}
		}

		private ModelFactionAI ExportFactionAI(FactionAIBase factionAI)
		{
			ModelFactionAI modelFactionAI = CreateFactionAIFromType.Create(factionAI.AIType);
			modelFactionAI.NextUnitSpawnTime = factionAI.NextUnitSpawnTime;
			modelFactionAI.NumFleetsSpawned = factionAI.NumGroupsSpawned;
			modelFactionAI.NumUnitsSpawned = factionAI.NumUnitsSpawned;
			modelFactionAI.SpawnOnlyAtOwnedDocks = factionAI.ShipBuildOnlyAtOwnedDocks;
			modelFactionAI.LastBuiltUnitTime = factionAI.LastBuiltUnitTime;
			modelFactionAI.LastOrderedPatrolTime = factionAI.LastOrderedPatrolTime;
			modelFactionAI.SpawnMode = factionAI.ShipBuildSectorMode;
			modelFactionAI.SpawnSectors = (from e in factionAI.ShipBuildSectors
				select GetModelSector(e) into e
				where e != null
				select e).ToList();
			if (factionAI.AISettings != null && factionAI.AISettings.ExcludedUnitIds.Any())
			{
				modelFactionAI.ExcludedUnits = (from e in factionAI.AISettings.ExcludedUnitIds
					select GetModelUnit(e) into e
					where e != null
					select e).ToList();
			}
			if (modelFactionAI is ModelFactionAITrader modelFactionAITrader)
			{
				FactionAITrader factionAITrader = (FactionAITrader)factionAI;
				modelFactionAITrader.TradeOnlySpecificCargoTypes = factionAITrader.TradeOnlySpecificCargoTypes;
				modelFactionAITrader.TradeSpecificCargoTypes = factionAITrader.TradeSpecificCargoTypes.Select((CargoClass e) => GetModelCargoClass(e)).ToList();
			}
			if (factionAI.MercenaryHireInfo != null)
			{
				modelFactionAI.FactionMercenaryHireInfo = new ModelFactionMercenaryHireInfo
				{
					HireTimeExpiry = factionAI.MercenaryHireInfo.HireTimeExpiry,
					HiringFaction = GetModelFaction(factionAI.MercenaryHireInfo.HiringFaction)
				};
			}
			return modelFactionAI;
		}

		private void ExportJobs()
		{
			foreach (MissionSpec item in engine.Jobs.Where((MissionSpec e) => e != null))
			{
				GetModelUnit(item.Unit)?.Jobs.Add(ExportJob(item));
			}
		}

		private ModelJob ExportJob(MissionSpec missionSpec)
		{
			ModelJob modelJob = CreateJobFromJobType.CreateJob(missionSpec.JobType);
			modelJob.Id = missionSpec.UniqueId;
			modelJob.Faction = GetModelFaction(missionSpec.Faction);
			modelJob.ExpiryTime = missionSpec.ExpiryTime;
			modelJob.RewardCredits = missionSpec.RewardCredits;
			modelJob.ProfitCredits = missionSpec.ProfitCredits;
			modelJob.Unit = GetModelUnit(missionSpec.Unit);
			if (!(modelJob is ModelCourierJob modelCourierJob))
			{
				if (!(modelJob is ModelDeliverShipJob modelDeliverShipJob))
				{
					if (!(modelJob is ModelBreakdownJob modelBreakdownJob))
					{
						if (modelJob is ModelDestroyFleetJob modelDestroyFleetJob)
						{
							DestroyGroupMissionSpec destroyGroupMissionSpec = (DestroyGroupMissionSpec)missionSpec;
							modelDestroyFleetJob.FleetSpawnParams = ExportFleetSpawnParams(destroyGroupMissionSpec.TargetGroupParams);
						}
					}
					else
					{
						BreakdownMissionSpec breakdownMissionSpec = (BreakdownMissionSpec)missionSpec;
						modelBreakdownJob.BreakdownUnitClass = GetModelUnitClass(breakdownMissionSpec.BreakdownUnitClass);
						modelBreakdownJob.BreakdownDestinationSector = GetModelSector(breakdownMissionSpec.BreakdownDestinationSector);
						modelBreakdownJob.BreakdownDestinationPosition = breakdownMissionSpec.BreakdownDestinationSectorPosition.ToVec3();
					}
				}
				else
				{
					DeliverShipMissionSpec deliverShipMissionSpec = (DeliverShipMissionSpec)missionSpec;
					modelDeliverShipJob.UnitClass = GetModelUnitClass(deliverShipMissionSpec.UnitClass);
					modelDeliverShipJob.DestinationUnit = GetModelUnit(deliverShipMissionSpec.DestinationUnit);
				}
			}
			else
			{
				CourierMissionSpec courierMissionSpec = (CourierMissionSpec)missionSpec;
				modelCourierJob.PickupUnit = GetModelUnit(courierMissionSpec.PickupUnit);
				modelCourierJob.DestinationUnit = GetModelUnit(courierMissionSpec.DestinationUnit);
				modelCourierJob.Cargo = new ModelComponentUnitCargoDataItem
				{
					CargoClass = GetModelCargoClass(courierMissionSpec.CargoItem?.CargoClass),
					Quantity = (courierMissionSpec.CargoItem?.Quantity ?? 0)
				};
			}
			return modelJob;
		}

		private ModelFleetSpawnParams ExportFleetSpawnParams(FleetSpawnParams targetGroupParams)
		{
			if (targetGroupParams == null)
			{
				return null;
			}
			return new ModelFleetSpawnParams
			{
				TargetSector = GetModelSector(targetGroupParams.TargetSector),
				TargetPosition = targetGroupParams.TargetSectorPosition.ToVec3(),
				TargetDockUnit = GetModelUnit(targetGroupParams.TargetDock),
				FleetResourceName = ((targetGroupParams.FleetPrefab != null) ? targetGroupParams.FleetPrefab.name : null),
				Faction = GetModelFaction(targetGroupParams.Faction),
				ShipDesignation = targetGroupParams.ShipDesignation,
				HomeSector = GetModelSector(targetGroupParams.HomeSector),
				HomeBaseUnit = GetModelUnit(targetGroupParams.HomeBase),
				Items = targetGroupParams.Ships.Select((FleetSpawnShipParams e) => new ModelFleetSpawnParamsItem
				{
					UnitClass = GetModelUnitClass(e.UnitClass),
					AddCargoLoadout = e.AddCargoLoadout,
					PilotResourceName = ((e.PilotPrefab != null) ? e.PilotPrefab.name : null),
					ShipName = e.ShipName
				}).ToList()
			};
		}

		private void ExportPeople()
		{
			foreach (Person person in engine.People)
			{
				if (person != null && (person.Faction != null || GameController.Instance.GameSettings.SaveGameSettings.SavePilotsWithNoFaction))
				{
					ModelPerson modelPerson = ExportPerson(person);
					AddModelPerson(modelPerson);
				}
			}
		}

		private void AddModelPerson(ModelPerson modelPerson)
		{
			peopleById.Add(modelPerson.Id, modelPerson);
			savedGame.People.Add(modelPerson);
		}

		private void ExportTriggerGroups()
		{
			foreach (KeyValuePair<int, TriggerGroup> item in engine.TriggerGroupsById)
			{
				TriggerGroup value = item.Value;
				if (value != null)
				{
					ModelTriggerGroup modelTriggerGroup = ExportTriggerGroup(value);
					AddModelTriggerGroup(modelTriggerGroup);
				}
			}
			foreach (ModelTriggerGroup triggerGroup in savedGame.TriggerGroups)
			{
				TriggerGroup triggerGroupById = engine.GetTriggerGroupById(triggerGroup.Id);
				ExportTriggerGroupTriggers(triggerGroupById, triggerGroup);
				ExportTriggerGroupActions(triggerGroupById, triggerGroup);
			}
		}

		private void AddModelTriggerGroup(ModelTriggerGroup modelTriggerGroup)
		{
			triggerGroupsById.Add(modelTriggerGroup.Id, modelTriggerGroup);
			savedGame.TriggerGroups.Add(modelTriggerGroup);
		}

		private ModelTriggerGroup ExportTriggerGroup(TriggerGroup triggerGroup)
		{
			return new ModelTriggerGroup
			{
				Id = triggerGroup.UniqueId,
				EvaluateFrequency = triggerGroup.EvaluateFrequency,
				FireAndDisable = triggerGroup.FireAndDisable,
				IsActive = triggerGroup.gameObject.activeSelf,
				NextEvaluationTime = triggerGroup.NextEvaluationTime,
				FireCount = triggerGroup.FireCount,
				MaxFireCount = triggerGroup.MaxFireCount,
				MaxFiredAction = triggerGroup.MaxFiredAction
			};
		}

		private void ExportTriggerGroupTriggers(TriggerGroup triggerGroup, ModelTriggerGroup modelTriggerGroup)
		{
			foreach (TriggerBase trigger in triggerGroup.Triggers)
			{
				try
				{
					ModelTrigger item = ExportTrigger(trigger);
					modelTriggerGroup.Triggers.Add(item);
				}
				catch
				{
					Debug.LogError($"Could not export trigger for trigger group {modelTriggerGroup.Id}");
				}
			}
		}

		private void ExportTriggerGroupActions(TriggerGroup triggerGroup, ModelTriggerGroup modelTriggerGroup)
		{
			EngineAction[] actions = triggerGroup.GetActions();
			foreach (EngineAction action in actions)
			{
				try
				{
					modelTriggerGroup.Actions.Add(ExportAction(action));
				}
				catch
				{
					Debug.LogError($"Could not export action for trigger group {modelTriggerGroup.Id}");
				}
			}
		}

		private ModelTrigger ExportTrigger(TriggerBase trigger)
		{
			ModelTrigger modelTrigger = TriggerUtils.CreateModelTrigger(trigger.Type);
			modelTrigger.Invert = trigger.Invert;
			ExportTriggerData(trigger, modelTrigger);
			return modelTrigger;
		}

		private void ExportTriggerData(TriggerBase triggerBase, ModelTrigger modelTrigger)
		{
			if (!(triggerBase is Trigger_Player_IsPilotting trigger_Player_IsPilotting))
			{
				if (!(triggerBase is Trigger_Player_CurrentHudTarget trigger_Player_CurrentHudTarget))
				{
					if (triggerBase is Trigger_Scenario_TimeElapsed trigger_Scenario_TimeElapsed)
					{
						((ModelTrigger_Scenario_TimeElapsed)modelTrigger).Time = trigger_Scenario_TimeElapsed.Time;
					}
				}
				else
				{
					((ModelTrigger_Player_CurrentHudTarget)modelTrigger).TargetUnit = GetModelUnit(trigger_Player_CurrentHudTarget.Target);
				}
			}
			else
			{
				((ModelTrigger_Player_IsPilotting)modelTrigger).WaitForHud = trigger_Player_IsPilotting.WaitForHud;
			}
		}

		private ModelAction ExportAction(EngineAction action)
		{
			ModelAction modelAction = ActionUtils.CreateModelAction(action.Type);
			ExportActionData(action, modelAction);
			return modelAction;
		}

		private void ExportActionData(EngineAction engineAction, ModelAction modelAction)
		{
			if (!(engineAction is Action_Player_NewMessageSimple action_Player_NewMessageSimple))
			{
				if (!(engineAction is Action_Mission_Activate action_Mission_Activate))
				{
					if (!(engineAction is Action_Mission_ChangeStage action_Mission_ChangeStage))
					{
						if (!(engineAction is Action_Mission_ActivateObjective action_Mission_ActivateObjective))
						{
							if (!(engineAction is Action_Mission_CompleteObjective action_Mission_CompleteObjective))
							{
								if (engineAction is Action_TriggerGroup_Activate action_TriggerGroup_Activate)
								{
									((ModelAction_TriggerGroup_Activate)modelAction).TriggerGroup = GetModelTriggerGroup(action_TriggerGroup_Activate.Target);
								}
							}
							else
							{
								ModelAction_Mission_CompleteObjective modelAction_Mission_CompleteObjective = (ModelAction_Mission_CompleteObjective)modelAction;
								modelAction_Mission_CompleteObjective.MissionObjective = GetModelMissionObjective(action_Mission_CompleteObjective.Objective);
								modelAction_Mission_CompleteObjective.Success = action_Mission_CompleteObjective.Success;
							}
							return;
						}
						ModelAction_Mission_ActivateObjective modelAction_Mission_ActivateObjective = (ModelAction_Mission_ActivateObjective)modelAction;
						foreach (MissionObjective objective in action_Mission_ActivateObjective.Objectives)
						{
							ModelMissionObjective modelMissionObjective = GetModelMissionObjective(objective);
							if (modelMissionObjective != null)
							{
								modelAction_Mission_ActivateObjective.Objectives.Add(modelMissionObjective);
							}
						}
						if (action_Mission_ActivateObjective.Objective != null)
						{
							ModelMissionObjective modelMissionObjective2 = GetModelMissionObjective(action_Mission_ActivateObjective.Objective);
							if (modelMissionObjective2 != null)
							{
								modelAction_Mission_ActivateObjective.Objectives.Add(modelMissionObjective2);
							}
						}
						return;
					}
					ModelAction_Mission_ChangeStage modelAction_Mission_ChangeStage = (ModelAction_Mission_ChangeStage)modelAction;
					ModelMission modelMission = GetModelMission(action_Mission_ChangeStage.Mission);
					if (modelMission != null)
					{
						int num = action_Mission_ChangeStage.Mission.Stages.IndexOf(action_Mission_ChangeStage.Stage);
						if (num > -1)
						{
							modelAction_Mission_ChangeStage.Stage = modelMission.Stages[num];
						}
					}
				}
				else
				{
					((ModelAction_Mission_Activate)modelAction).Mission = GetModelMission(action_Mission_Activate.Mission);
				}
			}
			else
			{
				ModelAction_Player_NewMessageSimple modelAction_Player_NewMessageSimple = (ModelAction_Player_NewMessageSimple)modelAction;
				modelAction_Player_NewMessageSimple.Notifications = action_Player_NewMessageSimple.Notifications;
				modelAction_Player_NewMessageSimple.From = action_Player_NewMessageSimple.From;
				modelAction_Player_NewMessageSimple.To = action_Player_NewMessageSimple.To;
				modelAction_Player_NewMessageSimple.Subject = action_Player_NewMessageSimple.Subject;
				modelAction_Player_NewMessageSimple.Message = action_Player_NewMessageSimple.Message;
			}
		}

		private ModelPerson ExportPerson(Person person)
		{
			ModelPerson modelPerson = new ModelPerson();
			modelPerson.Id = person.UniqueId;
			modelPerson.GeneratedFirstNameId = person.GeneratedFirstNameId;
			modelPerson.GeneratedLastNameId = person.GeneratedLastNameId;
			modelPerson.Seed = person.Seed;
			modelPerson.CustomName = person.CustomName;
			modelPerson.CustomShortName = person.CustomShortName;
			modelPerson.CustomTitle = person.CustomTitle;
			modelPerson.DialogId = ((person.CommsHandler != null) ? person.CommsHandler.UniqueId : (-1));
			modelPerson.Faction = GetModelFaction(person.Faction);
			modelPerson.DestroyGameObjectOnKill = person.DestroyGameObjectOnKill;
			modelPerson.CurrentUnit = GetModelUnit(person.CurrentUnit);
			modelPerson.IsPilot = person.IsPilot;
			modelPerson.IsMale = person.IsMale;
			modelPerson.IsAutoPilot = person.IsAutoPilot;
			modelPerson.Kills = person.Kills;
			modelPerson.Deaths = person.Deaths;
			modelPerson.Properness = person.Properness;
			modelPerson.Aggression = person.Aggression;
			modelPerson.Greed = person.Greed;
			modelPerson.RankId = ((person.Rank != null) ? person.Rank.UniqueId : (-1));
			modelPerson.AvatarProfileId = ((person.AvatarProfile != null) ? person.AvatarProfile.Id : (-1));
			modelPerson.DialogProfileId = (sbyte)((person.DialogProfile != null) ? ((sbyte)person.DialogProfile.UniqueId) : (-1));
			if (person.Faction != null && person.Faction.LeaderPerson == person)
			{
				modelPerson.Faction.Leader = modelPerson;
			}
			NpcPilotSettings component = person.GetComponent<NpcPilotSettings>();
			if (component != null)
			{
				modelPerson.NpcPilotSettings = ExportNpcPilotSettings(component);
			}
			NpcPilot component2 = person.GetComponent<NpcPilot>();
			if (component2 != null)
			{
				modelPerson.NpcPilot = ExportNpcPilot(component2);
			}
			return modelPerson;
		}

		private ModelNpcPilot ExportNpcPilot(NpcPilot aiUnitController)
		{
			ModelNpcPilot modelNpcPilot = new ModelNpcPilot();
			modelNpcPilot.DestroyWhenNoUnit = aiUnitController.DestroyWhenNoUnit;
			modelNpcPilot.DestroyWhenNotPilotting = aiUnitController.DestroyWhenNotPilotting;
			ModelFleet modelFleet = GetModelFleet(aiUnitController.Fleet);
			if (modelFleet != null)
			{
				modelNpcPilot.Fleet = modelFleet;
				modelFleet.Npcs.Add(modelNpcPilot);
			}
			return modelNpcPilot;
		}

		private ModelNpcPilotSettings ExportNpcPilotSettings(NpcPilotSettings unitControllerProfile)
		{
			return new ModelNpcPilotSettings
			{
				RestrictedWeaponPreference = unitControllerProfile.RestrictedWeaponPreference,
				CombatEfficiency = unitControllerProfile.CombatEfficiency,
				CheatAmmo = unitControllerProfile.AICheatAmmo,
				AllowDitchShip = unitControllerProfile.AllowDitchShip
			};
		}

		private void ExportFleetOrders()
		{
			foreach (Fleet fleet in engine.Fleets)
			{
				ModelFleet modelFleet = GetModelFleet(fleet);
				ExportFleetOrders(fleet, modelFleet.OrdersCollection);
			}
		}

		private void ExportFleets()
		{
			engine.Fleets.TrimNulls();
			foreach (Fleet fleet in engine.Fleets)
			{
				ModelFleet modelFleet = ExportFleet(fleet);
				AddModelFleet(modelFleet);
			}
		}

		public static ModelPlayerFleetSettings ExportPlayerFleetSettings(Fleet fleet)
		{
			return new ModelPlayerFleetSettings
			{
				NotifyWhenOrderComplete = fleet.Settings.NotifyWhenOrderComplete,
				NotifyWhenScannedHostile = fleet.Settings.NotifyWhenScannedHostile,
				NotifyWhenAbandonedUnitFound = fleet.Settings.NotifyWhenAbandonedUnitFound,
				NotifyWhenAbandonedCargoFound = fleet.Settings.NotifyWhenAbandonedCargoFound
			};
		}

		private void AddModelFleet(ModelFleet modelFleet)
		{
			fleetsById.Add(modelFleet.Id, modelFleet);
			savedGame.Fleets.Add(modelFleet);
		}

		public static int ExportFleetFormationId(Fleet fleet)
		{
			if (fleet.FleetFormation == null)
			{
				return -1;
			}
			return fleet.FleetFormation.FormationStyle.UniqueId;
		}

		private ModelFleet ExportFleet(Fleet fleet)
		{
			ModelFleet modelFleet = new ModelFleet();
			modelFleet.Id = fleet.UniqueId;
			modelFleet.Name = fleet.Name;
			modelFleet.IsActive = fleet.gameObject.activeSelf;
			modelFleet.Position = fleet.transform.localPosition.ToVec3();
			modelFleet.Rotation = fleet.transform.localRotation.ToVec4();
			modelFleet.Sector = GetModelSector(fleet.Sector);
			modelFleet.Faction = GetModelFaction(fleet.Faction);
			modelFleet.HomeBase = GetModelSectorTarget(fleet.HomeBase);
			modelFleet.ExcludeFromFactionAI = fleet.ExcludeFromFactionAI;
			modelFleet.Seed = fleet.Seed;
			modelFleet.Strategy = fleet.FleetStrategy;
			modelFleet.FormationId = ExportFleetFormationId(fleet);
			if (fleet.Settings != null)
			{
				modelFleet.FleetSettings = ExportFleetSettings(fleet.Settings);
				if (fleet.IsOwnedByPlayer)
				{
					modelFleet.FleetSettings.PlayerFleetSettings = ExportPlayerFleetSettings(fleet);
				}
			}
			return modelFleet;
		}

		private void ExportFleetOrders(Fleet fleet, ModelFleetOrderCollection modelFleetOrders)
		{
			fleetOrderCache.Clear();
			fleetOrderCache.AddRange(from e in fleet.GetComponentsInChildren<FleetOrder>(includeInactive: true)
				where e != null && e.IsValid
				select e);
			modelFleetOrders.Orders = fleetOrderCache.Select((FleetOrder e) => ExportFleetOrder(e)).ToList();
			fleet.OrderQueue.TrimNulls();
			foreach (FleetOrder item in fleet.OrderQueue)
			{
				if (item == null)
				{
					Debug.LogError($"Fleet {fleet} has a queued order that is null. Ensure orders are cleaned up properly when destroying", item);
					continue;
				}
				if (!item.IsValid)
				{
					Debug.LogError($"Fleet {fleet} has an order that is invalid. Ensure orders are cleaned up properly when destroying", item);
					continue;
				}
				int num = fleetOrderCache.IndexOf(item);
				if (num < 0)
				{
					Debug.LogError($"Fleet {fleet} has an order in the queue that isn't a child of the fleet", item);
				}
				else
				{
					modelFleetOrders.QueuedOrders.Add(modelFleetOrders.Orders[num]);
				}
			}
			if (fleet.ActiveOrder != null)
			{
				ModelFleetOrder fleetOrder = modelFleetOrders.Orders[fleetOrderCache.IndexOf(fleet.ActiveOrder.FleetOrder)];
				modelFleetOrders.CurrentOrder = ExportActiveFleetOrder(fleet.ActiveOrder, fleetOrder);
			}
		}

		private ModelActiveFleetOrder ExportActiveFleetOrder(ActiveFleetOrder activeObjective, ModelFleetOrder fleetOrder)
		{
			ModelActiveFleetOrder modelActiveFleetOrder = CreateActiveFleetOrderFromType.Create(activeObjective.FleetOrder.OrderType);
			modelActiveFleetOrder.TimeoutTime = activeObjective.TimeoutTime;
			modelActiveFleetOrder.StartTime = activeObjective.StartTime;
			modelActiveFleetOrder.Order = fleetOrder;
			if (!(modelActiveFleetOrder is ModelActiveAttackFleetOrder modelActiveAttackFleetOrder))
			{
				if (!(modelActiveFleetOrder is ModelActiveAttackTargetOrder modelActiveAttackTargetOrder))
				{
					if (!(modelActiveFleetOrder is ModelActiveUniverseBountyHunterOrder modelActiveUniverseBountyHunterOrder))
					{
						if (!(modelActiveFleetOrder is ModelActiveUniverseRoamOrder modelActiveUniverseRoamOrder))
						{
							if (!(modelActiveFleetOrder is ModelActiveExploreOrder modelActiveExploreOrder))
							{
								if (!(modelActiveFleetOrder is ModelActiveUniverseTradeOrder) && !(modelActiveFleetOrder is ModelActiveManualTradeOrder))
								{
									if (!(modelActiveFleetOrder is ModelActiveUniversePassengerTransportOrder modelActiveUniversePassengerTransportOrder))
									{
										if (!(modelActiveFleetOrder is ModelActiveScavengeOrder modelActiveScavengeOrder))
										{
											if (!(modelActiveFleetOrder is ModelActiveMineOrder modelActiveMineOrder))
											{
												if (!(modelActiveFleetOrder is ModelActiveDockOrder) && !(modelActiveFleetOrder is ModelActiveDisposeCargoOrder) && !(modelActiveFleetOrder is ModelActiveJoinFleetOrder) && !(modelActiveFleetOrder is ModelActiveMoveToOrder) && !(modelActiveFleetOrder is ModelActiveProtectOrder) && !(modelActiveFleetOrder is ModelActiveReturnToBaseOrder) && !(modelActiveFleetOrder is ModelActiveUndockOrder) && !(modelActiveFleetOrder is ModelActiveCollectCargoOrder) && !(modelActiveFleetOrder is ModelActiveWaitForAutoRepairOrder) && !(modelActiveFleetOrder is ModelActiveBuildStationOrder) && !(modelActiveFleetOrder is ModelActiveClaimUnitOrder))
												{
													if (!(modelActiveFleetOrder is ModelActivePatrolOrder modelActivePatrolOrder))
													{
														if (!(modelActiveFleetOrder is ModelActiveRepairFleetOrder modelActiveRepairFleetOrder))
														{
															if (!(modelActiveFleetOrder is ModelActiveRearmFleetOrder modelActiveRearmFleetOrder))
															{
																if (!(modelActiveFleetOrder is ModelActiveWaitOrder modelActiveWaitOrder))
																{
																	if (!(modelActiveFleetOrder is ModelActiveSellCargoOrder modelActiveSellCargoOrder))
																	{
																		if (!(modelActiveFleetOrder is ModelActiveMoveToNearestFriendlyStationOrder modelActiveMoveToNearestFriendlyStationOrder))
																		{
																			if (!(modelActiveFleetOrder is ModelActiveEnterWormholeOrder modelActiveEnterWormholeOrder))
																			{
																				if (modelActiveFleetOrder is ModelActiveExploreSectorOrder modelActiveExploreSectorOrder)
																				{
																					ActiveExploreSectorOrder activeExploreSectorOrder = (ActiveExploreSectorOrder)activeObjective;
																					modelActiveExploreSectorOrder.CurrentTargetSectorPosition = (activeExploreSectorOrder.CurrentTargetSectorPosition.HasValue ? new Vec3?(activeExploreSectorOrder.CurrentTargetSectorPosition.Value.ToVec3()) : ((Vec3?)null));
																				}
																			}
																			else
																			{
																				ActiveEnterWormholeOrder activeEnterWormholeOrder = (ActiveEnterWormholeOrder)activeObjective;
																				modelActiveEnterWormholeOrder.State = activeEnterWormholeOrder.State;
																			}
																		}
																		else
																		{
																			ActiveMoveToNearestFriendlyStationOrder activeMoveToNearestFriendlyStationOrder = (ActiveMoveToNearestFriendlyStationOrder)activeObjective;
																			modelActiveMoveToNearestFriendlyStationOrder.TargetStationUnit = GetModelUnit(activeMoveToNearestFriendlyStationOrder.TargetStation);
																		}
																	}
																	else
																	{
																		ActiveSellCargoOrder activeSellCargoOrder = (ActiveSellCargoOrder)activeObjective;
																		modelActiveSellCargoOrder.SellExpireTime = activeSellCargoOrder.SellExpireTime;
																		modelActiveSellCargoOrder.SellCargoClass = GetModelCargoClass(activeSellCargoOrder.SellCargoClass);
																		if (activeSellCargoOrder.TraderTarget != null)
																		{
																			modelActiveSellCargoOrder.TraderTargetUnit = GetModelUnit(activeSellCargoOrder.TraderTarget.Unit);
																		}
																		modelActiveSellCargoOrder.State = activeSellCargoOrder.State;
																	}
																}
																else
																{
																	ActiveWaitOrder activeWaitOrder = (ActiveWaitOrder)activeObjective;
																	modelActiveWaitOrder.WaitExpiryTime = activeWaitOrder.WaitExpiryTime;
																}
															}
															else
															{
																ActiveRearmOrder activeRearmOrder = (ActiveRearmOrder)activeObjective;
																modelActiveRearmFleetOrder.State = activeRearmOrder.State;
																modelActiveRearmFleetOrder.CurrentRearmLocationUnit = GetModelUnit(activeRearmOrder.CurrentRearmLocation);
															}
														}
														else
														{
															ActiveRepairFleetOrder activeRepairFleetOrder = (ActiveRepairFleetOrder)activeObjective;
															modelActiveRepairFleetOrder.RepairState = activeRepairFleetOrder.RepairState;
															modelActiveRepairFleetOrder.CurrentRepairLocationUnit = GetModelUnit(activeRepairFleetOrder.CurrentRepairLocation);
														}
													}
													else
													{
														ActivePatrolOrder activePatrolOrder = (ActivePatrolOrder)activeObjective;
														modelActivePatrolOrder.PathDirection = activePatrolOrder.PathDirection;
														modelActivePatrolOrder.NodeIndex = activePatrolOrder.NodeIndex;
														modelActivePatrolOrder.StartNodeIndex = activePatrolOrder.StartNodeIndex;
													}
												}
											}
											else
											{
												ActiveMineOrder activeMineOrder = (ActiveMineOrder)activeObjective;
												modelActiveMineOrder.MineTarget = GetModelUnit(activeMineOrder.MineTarget);
												modelActiveMineOrder.State = activeMineOrder.State;
												modelActiveMineOrder.AngleFromAsteroid = activeMineOrder.AngleFromAsteroid;
												modelActiveMineOrder.DistanceFromAsteroid = activeMineOrder.DistanceFromAsteroid;
											}
										}
										else
										{
											ActiveScavengeOrder activeScavengeOrder = (ActiveScavengeOrder)activeObjective;
											modelActiveScavengeOrder.Position = (activeScavengeOrder.RoamSectorLocalPosition.HasValue ? new Vec3?(activeScavengeOrder.RoamSectorLocalPosition.Value.ToVec3()) : ((Vec3?)null));
										}
									}
									else
									{
										ActiveTransportPassengersOrder activeTransportPassengersOrder = (ActiveTransportPassengersOrder)activeObjective;
										modelActiveUniversePassengerTransportOrder.PassengerGroup = GetModelPassengerGroup(activeTransportPassengersOrder.PassengerGroup);
										modelActiveUniversePassengerTransportOrder.EndBuySellTime = activeTransportPassengersOrder.EndBuySellTime;
										modelActiveUniversePassengerTransportOrder.LastStateChangeTime = activeTransportPassengersOrder.LastStateChangeTime;
										modelActiveUniversePassengerTransportOrder.CurrentState = activeTransportPassengersOrder.CurrentState;
									}
								}
								else
								{
									ModelActiveTradeOrder modelActiveTradeOrder = (ModelActiveTradeOrder)modelActiveFleetOrder;
									ActiveTradeOrder activeTradeOrder = (ActiveTradeOrder)activeObjective;
									modelActiveTradeOrder.TradeRoute = GetModelCustomTradeRoute(activeTradeOrder.TradeRoute);
									modelActiveTradeOrder.EndBuySellTime = activeTradeOrder.EndBuySellTime;
									modelActiveTradeOrder.LastStateChangeTime = activeTradeOrder.LastStateChangeTime;
									modelActiveTradeOrder.CurrentState = activeTradeOrder.CurrentState;
								}
							}
							else
							{
								ActiveExploreOrder activeExploreOrder = (ActiveExploreOrder)activeObjective;
								modelActiveExploreOrder.CurrentTargetSector = GetModelSector(activeExploreOrder.CurrentTargetSector);
								if (activeExploreOrder.CurrentTargetWormhole != null)
								{
									modelActiveExploreOrder.CurrentTargetWormhole = GetModelUnit(activeExploreOrder.CurrentTargetWormhole.Unit);
								}
								modelActiveExploreOrder.CurrentTargetSectorPosition = activeExploreOrder.CurrentTargetSectorPosition.ToVec3();
							}
						}
						else
						{
							ActiveRoamLocationsOrder activeRoamLocationsOrder = (ActiveRoamLocationsOrder)activeObjective;
							modelActiveUniverseRoamOrder.CurrentTargetSector = GetModelSector(activeRoamLocationsOrder.CurrentTargetSector);
							modelActiveUniverseRoamOrder.CurrentTargetPosition = activeRoamLocationsOrder.CurrentTargetSectorPosition.ToVec3();
						}
					}
					else
					{
						ActiveBountyHunterOrder activeBountyHunterOrder = (ActiveBountyHunterOrder)activeObjective;
						modelActiveUniverseBountyHunterOrder.TargetPerson = GetModelPerson(activeBountyHunterOrder.TargetPilot);
					}
				}
				else
				{
					ActiveAttackTargetOrder activeAttackTargetOrder = (ActiveAttackTargetOrder)activeObjective;
					modelActiveAttackTargetOrder.TargetUnit = GetModelUnit(activeAttackTargetOrder.Target);
					modelActiveAttackTargetOrder.OriginalTargetFaction = GetModelFaction(activeAttackTargetOrder.OriginalTargetFaction);
				}
			}
			else
			{
				ActiveAttackFleetOrder activeAttackFleetOrder = (ActiveAttackFleetOrder)activeObjective;
				modelActiveAttackFleetOrder.TargetFleet = GetModelFleet(activeAttackFleetOrder.Target);
			}
			return modelActiveFleetOrder;
		}

		private ModelFleetOrder ExportFleetOrder(FleetOrder fleetOrder)
		{
			if (fleetOrder.OrderType == FleetOrderType.None)
			{
				Debug.LogWarning("Unknown objective type: " + fleetOrder.name, fleetOrder);
			}
			ModelFleetOrder modelFleetOrder = CreateFleetOrderFromType.Create(fleetOrder.OrderType);
			modelFleetOrder.Id = fleetOrder.UniqueId;
			modelFleetOrder.CompletionMode = fleetOrder.CompletionMode;
			modelFleetOrder.AllowCombatInterception = fleetOrder.AllowCombatInterception;
			modelFleetOrder.CloakPreference = fleetOrder.PreferCloak;
			modelFleetOrder.MaxJumpDistance = fleetOrder.MaxJumpDistance;
			modelFleetOrder.AllowTimeout = fleetOrder.AllowTimeout;
			modelFleetOrder.TimeoutTime = fleetOrder.TimeoutTime;
			modelFleetOrder.MaxDuration = fleetOrder.MaxDuration;
			modelFleetOrder.Priority = fleetOrder.Priority;
			modelFleetOrder.AvailableCredits = ((!fleetOrder.HasUnlimitedSpend) ? fleetOrder.Credits : (-1));
			modelFleetOrder.Notifications = fleetOrder.Notifications;
			if (!(modelFleetOrder is ModelAttackFleetOrder modelAttackFleetOrder))
			{
				if (!(modelFleetOrder is ModelCollectCargoOrder modelCollectCargoOrder))
				{
					if (!(modelFleetOrder is ModelScavengeOrder modelScavengeOrder))
					{
						if (!(modelFleetOrder is ModelMineOrder modelMineOrder))
						{
							if (!(modelFleetOrder is ModelDockOrder modelDockOrder))
							{
								if (!(modelFleetOrder is ModelPatrolOrder modelPatrolOrder))
								{
									if (!(modelFleetOrder is ModelPatrolPathOrder modelPatrolPathOrder))
									{
										if (!(modelFleetOrder is ModelWaitOrder modelWaitOrder))
										{
											if (!(modelFleetOrder is ModelAttackTargetOrder modelAttackTargetOrder))
											{
												if (!(modelFleetOrder is ModelUniverseTradeOrder modelUniverseTradeOrder))
												{
													if (!(modelFleetOrder is ModelJoinFleetOrder modelJoinFleetOrder))
													{
														if (!(modelFleetOrder is ModelManualTradeOrder modelManualTradeOrder))
														{
															if (!(modelFleetOrder is ModelProtectOrder modelProtectOrder))
															{
																if (!(modelFleetOrder is ModelSellCargoOrder modelSellCargoOrder))
																{
																	if (!(modelFleetOrder is ModelReturnToBaseOrder) && !(modelFleetOrder is ModelDisposeCargoOrder) && !(modelFleetOrder is ModelUniversePassengerTransportOrder) && !(modelFleetOrder is ModelUniverseRoamOrder) && !(modelFleetOrder is ModelUniverseBountyHunterOrder) && !(modelFleetOrder is ModelExploreOrder) && !(modelFleetOrder is ModelUndockOrder))
																	{
																		if (!(modelFleetOrder is ModelManualRepairFleetOrder modelManualRepairFleetOrder))
																		{
																			if (!(modelFleetOrder is ModelManualRearmFleetOrder modelManualRearmFleetOrder))
																			{
																				if (!(modelFleetOrder is ModelRearmAtNearestFleetOrder modelRearmAtNearestFleetOrder))
																				{
																					if (!(modelFleetOrder is ModelRepairAtNearestStationOrder modelRepairAtNearestStationOrder))
																					{
																						if (!(modelFleetOrder is ModelMoveToNearestFriendlyStationOrder modelMoveToNearestFriendlyStationOrder))
																						{
																							if (!(modelFleetOrder is ModelEnterWormholeOrder modelEnterWormholeOrder))
																							{
																								if (!(modelFleetOrder is ModelExploreSectorOrder modelExploreSectorOrder))
																								{
																									if (!(modelFleetOrder is ModelMoveToOrder modelMoveToOrder))
																									{
																										if (!(modelFleetOrder is ModelMoveToSectorOrder modelMoveToSectorOrder))
																										{
																											if (!(modelFleetOrder is ModelWaitForAutoRepairOrder modelWaitForAutoRepairOrder))
																											{
																												if (!(modelFleetOrder is ModelBuildStationOrder modelBuildStationOrder))
																												{
																													if (modelFleetOrder is ModelClaimUnitOrder modelClaimUnitOrder)
																													{
																														ClaimUnitOrder claimUnitOrder = (ClaimUnitOrder)fleetOrder;
																														modelClaimUnitOrder.Unit = GetModelUnit(claimUnitOrder.TargetUnit);
																													}
																													else
																													{
																														Debug.LogErrorFormat("Unable to save data for objective of type {0}. Does it have any data", fleetOrder.OrderType);
																													}
																												}
																												else
																												{
																													BuildStationOrder buildStationOrder = (BuildStationOrder)fleetOrder;
																													modelBuildStationOrder.UnitClass = GetModelUnitClass(buildStationOrder.UnitClass);
																													modelBuildStationOrder.Sector = GetModelSector(buildStationOrder.Sector);
																													modelBuildStationOrder.SectorPosition = buildStationOrder.SectorPosition.ToVec3();
																													modelBuildStationOrder.InsufficientCreditsMode = buildStationOrder.InsufficientCreditsMode;
																												}
																											}
																											else
																											{
																												WaitForAutoRepairOrder waitForAutoRepairOrder = (WaitForAutoRepairOrder)fleetOrder;
																												modelWaitForAutoRepairOrder.HullConditionThreshold = waitForAutoRepairOrder.HullConditionThreshold;
																												modelWaitForAutoRepairOrder.ComponentsConditionThreshold = waitForAutoRepairOrder.ComponentsConditionThreshold;
																												modelWaitForAutoRepairOrder.ShieldConditionThreshold = waitForAutoRepairOrder.ShieldConditionThreshold;
																											}
																										}
																										else
																										{
																											MoveToSectorOrder moveToSectorOrder = (MoveToSectorOrder)fleetOrder;
																											modelMoveToSectorOrder.TargetSector = GetModelSector(moveToSectorOrder.TargetSector);
																										}
																									}
																									else
																									{
																										MoveToOrder moveToOrder = (MoveToOrder)fleetOrder;
																										modelMoveToOrder.CompleteOnReachTarget = moveToOrder.CompleteOnReachTarget;
																										modelMoveToOrder.ArrivalThreshold = moveToOrder.ArrivalThreshold;
																										modelMoveToOrder.MatchTargetOrientation = moveToOrder.MatchTargetOrientation;
																										modelMoveToOrder.PreferredRelativeVectorFromTarget = moveToOrder.PreferredRelativeVectorFromTarget?.ToVec3();
																										modelMoveToOrder.Target = GetModelSectorTarget(moveToOrder.Target);
																									}
																								}
																								else
																								{
																									ExploreSectorOrder exploreSectorOrder = (ExploreSectorOrder)fleetOrder;
																									modelExploreSectorOrder.Sector = GetModelSector(exploreSectorOrder.Sector);
																								}
																							}
																							else
																							{
																								EnterWormholeOrder enterWormholeOrder = (EnterWormholeOrder)fleetOrder;
																								if (enterWormholeOrder.TargetWormhole != null)
																								{
																									modelEnterWormholeOrder.TargetWormhole = GetModelUnit(enterWormholeOrder.TargetWormhole.Unit);
																								}
																							}
																						}
																						else
																						{
																							MoveToNearestFriendlyStationOrder moveToNearestFriendlyStationOrder = (MoveToNearestFriendlyStationOrder)fleetOrder;
																							modelMoveToNearestFriendlyStationOrder.CompleteOnReachTarget = moveToNearestFriendlyStationOrder.CompleteOnReachTarget;
																						}
																					}
																					else
																					{
																						RepairAtNearestStationOrder repairAtNearestStationOrder = (RepairAtNearestStationOrder)fleetOrder;
																						modelRepairAtNearestStationOrder.InsufficientCreditsMode = repairAtNearestStationOrder.InsufficientCreditsMode;
																					}
																				}
																				else
																				{
																					RearmAtNearestOrder rearmAtNearestOrder = (RearmAtNearestOrder)fleetOrder;
																					modelRearmAtNearestFleetOrder.EquipmentCargoUsage = rearmAtNearestOrder.EquipmentUsage;
																					modelRearmAtNearestFleetOrder.InsufficientCreditsMode = rearmAtNearestOrder.InsufficientCreditsMode;
																				}
																			}
																			else
																			{
																				ManualRearmOrder manualRearmOrder = (ManualRearmOrder)fleetOrder;
																				modelManualRearmFleetOrder.EquipmentCargoUsage = manualRearmOrder.EquipmentUsage;
																				modelManualRearmFleetOrder.InsufficientCreditsMode = manualRearmOrder.InsufficientCreditsMode;
																				modelManualRearmFleetOrder.RearmLocationUnit = GetModelUnit(manualRearmOrder.SpecificRearmLocation);
																			}
																		}
																		else
																		{
																			ManualRepairFleetOrder manualRepairFleetOrder = (ManualRepairFleetOrder)fleetOrder;
																			modelManualRepairFleetOrder.InsufficientCreditsMode = manualRepairFleetOrder.InsufficientCreditsMode;
																			modelManualRepairFleetOrder.RepairLocationUnit = GetModelUnit(manualRepairFleetOrder.SpecificRepairLocation);
																		}
																	}
																}
																else
																{
																	SellCargoOrder sellCargoOrder = (SellCargoOrder)fleetOrder;
																	modelSellCargoOrder.FreeUnitsCompleteThreshold = sellCargoOrder.FreeUnitsCompleteThreshold;
																	modelSellCargoOrder.MinBuyPriceMultiplier = sellCargoOrder.MinBuyPriceMultiplier;
																	modelSellCargoOrder.SellOnlyListedCargos = sellCargoOrder.SellOnlyListedCargos;
																	modelSellCargoOrder.CompleteWhenNoBuyerFound = sellCargoOrder.CompleteWhenNoBuyerFound;
																	modelSellCargoOrder.CompleteWhenNoCargoToSell = sellCargoOrder.CompleteWhenNoCargoToSell;
																	if (sellCargoOrder.ManualBuyer != null)
																	{
																		modelSellCargoOrder.ManualBuyerUnit = GetModelUnit(sellCargoOrder.ManualBuyer.Unit);
																	}
																	modelSellCargoOrder.CustomSellCargoTime = sellCargoOrder.CustomSellCargoTime;
																	modelSellCargoOrder.SellCargoClasses = sellCargoOrder.SellCargoClasses.Select((CargoClass e) => GetModelCargoClass(e)).ToList();
																	modelSellCargoOrder.SellEquipment = sellCargoOrder.SellEquipment;
																}
															}
															else
															{
																ProtectOrder protectOrder = (ProtectOrder)fleetOrder;
																modelProtectOrder.CompleteOnReachTarget = protectOrder.CompleteOnReachTarget;
																modelProtectOrder.ArrivalThreshold = protectOrder.ArrivalThreshold;
																modelProtectOrder.MatchTargetOrientation = protectOrder.MatchTargetOrientation;
																modelProtectOrder.PreferredRelativeVectorFromTarget = protectOrder.PreferredRelativeVectorFromTarget?.ToVec3();
																modelProtectOrder.Target = GetModelSectorTarget(protectOrder.Target);
															}
														}
														else
														{
															ManualTradeOrder manualTradeOrder = (ManualTradeOrder)fleetOrder;
															modelManualTradeOrder.MinBuyQuantity = manualTradeOrder.MinBuyQuantity;
															modelManualTradeOrder.MinBuyCargoPercentage = manualTradeOrder.MinBuyCargoPercentage;
															modelManualTradeOrder.CustomTradeRoute = GetModelCustomTradeRoute(manualTradeOrder.CustomTradeRoute);
														}
													}
													else
													{
														JoinFleetOrder joinFleetOrder = (JoinFleetOrder)fleetOrder;
														modelJoinFleetOrder.TargetFleet = GetModelFleet(joinFleetOrder.TargetFleet);
													}
												}
												else
												{
													AutonomousTradeOrder autonomousTradeOrder = (AutonomousTradeOrder)fleetOrder;
													modelUniverseTradeOrder.MinBuyQuantity = autonomousTradeOrder.MinBuyQuantity;
													modelUniverseTradeOrder.MinBuyCargoPercentage = autonomousTradeOrder.MinBuyCargoPercentage;
													modelUniverseTradeOrder.TradeOnlySpecificCargoClasses = autonomousTradeOrder.TradeOnlySpecificCargoTypes;
													modelUniverseTradeOrder.TradeSpecificCargoClasses = autonomousTradeOrder.TradeSpecificCargoTypes.Select((CargoClass e) => GetModelCargoClass(e)).ToList();
												}
											}
											else
											{
												AttackTargetOrder attackTargetOrder = (AttackTargetOrder)fleetOrder;
												modelAttackTargetOrder.TargetUnit = GetModelUnit(attackTargetOrder.TargetUnit);
												modelAttackTargetOrder.AttackPriority = attackTargetOrder.AttackPriority;
											}
										}
										else
										{
											WaitOrder waitOrder = (WaitOrder)fleetOrder;
											modelWaitOrder.WaitTime = waitOrder.WaitTime;
										}
									}
									else
									{
										PatrolPathOrder patrolPathOrder = (PatrolPathOrder)fleetOrder;
										modelPatrolPathOrder.PathDirection = patrolPathOrder.PathDirection;
										modelPatrolPathOrder.IsLooping = patrolPathOrder.IsLooping;
										modelPatrolPathOrder.PatrolPath = GetModelPatrolPath(patrolPathOrder.PatrolPath);
									}
								}
								else
								{
									PatrolOrder patrolOrder = (PatrolOrder)fleetOrder;
									modelPatrolOrder.PathDirection = patrolOrder.PathDirection;
									modelPatrolOrder.IsLooping = patrolOrder.IsLooping;
									modelPatrolOrder.IsLoop = patrolOrder.IsLoop;
									modelPatrolOrder.Nodes = patrolOrder.Nodes.Select((AIPatrolPathNode e) => new ModelPatrolPathNode
									{
										Sector = GetModelSector(e.Sector),
										SectorPosition = e.SectorPosition.ToVec3()
									}).ToList();
								}
							}
							else
							{
								DockOrder dockOrder = (DockOrder)fleetOrder;
								modelDockOrder.TargetDock = GetModelUnit(dockOrder.TargetDock);
							}
						}
						else
						{
							MineOrder mineOrder = (MineOrder)fleetOrder;
							modelMineOrder.TargetSector = GetModelSector(mineOrder.TargetSector);
							modelMineOrder.CollectOwnerMode = mineOrder.CollectOwnerMode;
							if (mineOrder.ManualMineTarget != null)
							{
								modelMineOrder.ManualMineTarget = GetModelUnit(mineOrder.ManualMineTarget.Unit);
							}
						}
					}
					else
					{
						ScavengeOrder scavengeOrder = (ScavengeOrder)fleetOrder;
						modelScavengeOrder.TargetSector = GetModelSector(scavengeOrder.TargetSector);
						modelScavengeOrder.CollectOwnerMode = scavengeOrder.CollectOwnerMode;
					}
				}
				else
				{
					CollectCargoOrder collectCargoOrder = (CollectCargoOrder)fleetOrder;
					modelCollectCargoOrder.TargetUnit = GetModelUnit(collectCargoOrder.TargetUnit);
				}
			}
			else
			{
				AttackFleetOrder attackFleetOrder = (AttackFleetOrder)fleetOrder;
				modelAttackFleetOrder.Target = GetModelFleet(attackFleetOrder.Target);
				modelAttackFleetOrder.AttackPriority = attackFleetOrder.AttackPriority;
			}
			return modelFleetOrder;
		}

		public ModelSectorTarget GetModelSectorTarget(SectorTarget sceneTarget)
		{
			if (sceneTarget != null)
			{
				return new ModelSectorTarget
				{
					HadValidTarget = sceneTarget.HadSceneObject,
					Position = sceneTarget.SectorPosition.ToVec3(),
					Sector = GetModelSector(sceneTarget.Sector),
					TargetFleet = GetModelFleet(sceneTarget.TargetFleet),
					TargetUnit = GetModelUnit(sceneTarget.TargetUnit)
				};
			}
			return null;
		}

		private ModelCustomTradeRoute GetModelCustomTradeRoute(AITradeRoute tradeRoute)
		{
			if (tradeRoute != null && tradeRoute.CargoClass != null)
			{
				return new ModelCustomTradeRoute
				{
					BuyLocation = GetModelUnit((tradeRoute.BuyLocation != null) ? tradeRoute.BuyLocation.Unit : null),
					BuyPriceMultiplier = tradeRoute.BuyPriceMultiplier,
					CargoClass = GetModelCargoClass(tradeRoute.CargoClass),
					SellLocation = GetModelUnit((tradeRoute.SellLocation != null) ? tradeRoute.SellLocation.Unit : null)
				};
			}
			return null;
		}

		public static ModelFleetSettings ExportFleetSettings(FleetSettings settings)
		{
			return new ModelFleetSettings
			{
				PreferCloak = settings.PreferCloak,
				PreferToDock = settings.PreferToDock,
				Aggression = settings.Aggression,
				AllowAttack = settings.AllowAttack,
				TargetInterceptionLowerDistance = settings.TargetInterceptionLowerDistance,
				TargetInterceptionUpperDistance = settings.TargetInterceptionUpperDistance,
				MaxJumpDistance = settings.MaxJumpDistance,
				AllowCombatInterception = settings.AllowCombatInterception,
				DestroyWhenNoPilots = settings.DestroyWhenNoPilots,
				FormationTightness = settings.FormationTightness,
				CargoCollectionPreference = settings.CargoCollectionPreference
			};
		}

		private void ExportDockedUnits()
		{
			EngineASX.Instance.EnumerateUnits((Unit unit) =>
			{
				if (unit != null && unit.Components != null && unit.Components.HangarComponent != null && unit.Components.HangarComponent.DockedUnitCount > 0)
				{
					GetModelUnit(unit).ComponentUnitData.DockData = new ModelComponentUnitDockData
					{
						Items = unit.Components.HangarComponent.DockedShips.Select((UnitComponentHolder e) => new ModelComponentUnitDockDataItem
						{
							BayId = ((e.DockedInHangarBay != null) ? e.DockedInHangarBay.BayId : (-1)),
							DockedUnit = GetModelUnit(e.Unit)
						}).ToList()
					};
				}
			});
		}

		private void ExportWormholes()
		{
			EngineASX.Instance.EnumerateUnitsOfType(UnitType.Wormhole, (Unit unit) =>
			{
				GetModelUnit(unit).WormholeData = new ModelUnitWormholeData
				{
					IsUnstable = unit.WormholeComponent.IsUnstable,
					TargetWormholeUnit = GetModelUnit((unit.WormholeComponent.TargetGate != null) ? unit.WormholeComponent.TargetGate.Unit : null),
					UnstableNextChangeTargetTime = unit.WormholeComponent.NextUnstableChgTargetTime,
					UnstableTargetPosition = unit.WormholeComponent.ManualTargetSectorPosition.ToVec3(),
					UnstableTargetRotation = unit.WormholeComponent.ManualTargetRotation.ToVec3(),
					UnstableTargetSector = GetModelSector(unit.WormholeComponent.ManualTargetSector)
				};
			});
		}

		private void ExportPassengerGroups()
		{
			PassengerGroup[] array = engine.PassengerGroups.Where((PassengerGroup e) => e != null).ToArray();
			foreach (PassengerGroup passengerGroup in array)
			{
				if (passengerGroup.CurrentUnit != null)
				{
					ModelUnit modelUnit = GetModelUnit(passengerGroup.CurrentUnit);
					ModelPassengerGroup modelPassengerGroup = new ModelPassengerGroup
					{
						DestinationUnit = GetModelUnit(passengerGroup.Destination),
						ExpiryTime = passengerGroup.ExpiryTime,
						Id = passengerGroup.UniqueId,
						PassengerCount = passengerGroup.PassengerCount,
						Revenue = (passengerGroup.CachedRevenue ?? (-1)),
						SourceUnit = GetModelUnit(passengerGroup.Source),
						Unit = modelUnit
					};
					AddModelPassengerGroup(modelUnit, modelPassengerGroup);
				}
			}
		}

		private void AddModelPassengerGroup(ModelUnit modelUnit, ModelPassengerGroup modelPassengerGroup)
		{
			passengerGroupsById.Add(modelPassengerGroup.Id, modelPassengerGroup);
			modelUnit.PassengerGroups.Add(modelPassengerGroup);
		}

		private void ExportFactionIntel()
		{
			foreach (Faction faction in engine.Factions)
			{
				faction.Intel.TrimInvalidDiscoveredUnits();
				ModelFaction modelFaction = GetModelFaction(faction);
				modelFaction.Intel.Units = (from e in faction.Intel.AllDiscoveredUnitsExcludingOwned
					select GetModelUnit(e) into e
					where e != null
					select e).ToList();
				modelFaction.Intel.Sectors = (from e in faction.Intel.GetCopyOfDiscoveredSectors()
					select GetModelSector(e) into e
					where e != null
					select e).ToList();
				modelFaction.Intel.EnteredWormholes = (from e in faction.Intel.EnteredWormholeIds
					select GetModelUnit(e) into e
					where e != null
					select e).ToList();
			}
		}

		private void ExportUnits()
		{
			EngineASX.Instance.EnumerateUnits((Unit unit) =>
			{
				if (unit.UniqueId < 0)
				{
					Debug.LogError("Exporting unit with negative ID", unit);
				}
				else if (ShouldWriteUnit(unit))
				{
					ModelUnit modelUnit = ExportUnit(unit);
					AddModelUnit(modelUnit);
				}
			});
		}

		private void AddModelUnit(ModelUnit modelUnit)
		{
			unitsById.Add(modelUnit.Id, modelUnit);
			savedGame.Units.Add(modelUnit);
		}

		// One malformed unit must never abort an entire save, so this
		// warning is logged at most once per session.
		private static bool warnedMissingPlanetComponent;

		private ModelUnit ExportUnit(Unit unit)
		{
			ModelUnit modelUnit = new ModelUnit();
			modelUnit.Id = unit.UniqueId;
			modelUnit.Radius = ((unit.Radius != unit.UnitClass.UnitPrefab.Radius) ? new float?(unit.Radius) : ((float?)null));
			modelUnit.Mass = ((unit.Mass != unit.UnitClass.UnitPrefab.Mass) ? new float?(unit.Mass) : ((float?)null));
			modelUnit.Seed = unit.Seed;
			modelUnit.Class = GetModelUnitClass(unit.UnitClass);
			modelUnit.Sector = GetModelSector(unit.Sector);
			modelUnit.Position = unit.transform.localPosition.ToVec3();
			modelUnit.Rotation = unit.transform.localRotation.eulerAngles.ToVec3();
			if (unit.UnitType == UnitType.Planet)
			{
				UnitPlanet planet = unit.GetComponent<UnitPlanet>();
				if (planet != null)
				{
					modelUnit.Rotation = planet.Rotation.ToVec3();
				}
				else
				{
					// A planet without its UnitPlanet component (seen
					// in a hand-baked story scene) must not abort the
					// whole save - fall back to the transform's own
					// rotation and warn once.
					if (!warnedMissingPlanetComponent)
					{
						warnedMissingPlanetComponent = true;
						UnityEngine.Debug.LogWarning("[Save] A planet is missing its UnitPlanet component; falling back to transform rotation. Saving continues.");
					}
					modelUnit.Rotation = unit.transform.localRotation.eulerAngles.ToVec3();
				}
			}
			else
			{
				modelUnit.Rotation = unit.transform.localRotation.eulerAngles.ToVec3();
			}
			modelUnit.Faction = GetModelFaction(unit.Faction);
			modelUnit.RpProvision = unit.RpProvision;
			modelUnit.Name = unit.UnitName;
			modelUnit.ShortName = unit.UnitShortName;
			if (unit.CargoComponent != null)
			{
				modelUnit.CargoData = new ModelUnitCargoData
				{
					CargoClass = GetModelCargoClass(unit.CargoComponent.CargoClass),
					Quantity = unit.CargoComponent.Quantity,
					Expires = unit.CargoComponent.Expires,
					SpawnTime = unit.CargoComponent.SpawnTime
				};
			}
			if (unit.DebrisComponent != null)
			{
				modelUnit.DebrisData = new ModelUnitDebrisData
				{
					ScrapQuantity = unit.DebrisComponent.ScrapQuantity,
					ExpiryTime = unit.DebrisComponent.ExpiryTime,
					Expires = unit.DebrisComponent.Expires,
					RelatedUnitClass = GetModelUnitClass(unit.DebrisComponent.RelatedUnitClass)
				};
			}
			if (unit.Asteroid != null)
			{
				modelUnit.AsteroidData = new ModelUnitAsteroidData
				{
					RemainingYield = unit.Asteroid.RemainingYield
				};
			}
			if (unit.Components != null)
			{
				UnitShipTrader customShipTrader = unit.Components.CustomShipTrader;
				if (customShipTrader != null)
				{
					modelUnit.ShipTraderData = new ModelUnitShipTraderData
					{
						Items = customShipTrader.ShipTradeItems.Select((UnitShipTraderItem e) => new ModelUnitShipTraderItem
						{
							SellMultiplier = e.SellMultiplier,
							UnitClass = GetModelUnitClass(e.UnitClass)
						}).ToList()
					};
				}
			}
			if (unit.UnitType == UnitType.Projectile)
			{
				Projectile component = unit.GetComponent<Projectile>();
				modelUnit.ProjectileData = new ModelUnitProjectileData
				{
					SourceUnit = GetModelUnit(component.SourceUnit),
					TargetUnit = GetModelUnit(component.Target),
					FireTime = component.FireTime,
					RemainingMovement = component.RemainingMovement,
					DamageType = new ModelDamageType
					{
						Damage = component.Damage.Damage,
						MiningDamage = component.Damage.MiningDamage,
						ShieldDamageType = component.Damage.ShieldDamageType
					}
				};
			}
			if (unit.IsInActiveSector && unit.ActiveUnit != null)
			{
				modelUnit.ActiveData = new ModelUnitActiveData();
				if (unit.ActiveUnit.ActiveUnitShip != null && unit.ActiveUnit.ActiveUnitShip.CurrentTurn != 0f)
				{
					modelUnit.ActiveData.CurrentTurn = unit.ActiveUnit.ActiveUnitShip.CurrentTurn;
				}
				if (unit.ActiveUnit.UnitRigidBody != null)
				{
					modelUnit.ActiveData.Velocity = unit.ActiveUnit.UnitRigidBody.linearVelocity.ToVec3();
				}
			}
			if (unit.Destructable != null)
			{
				if (UnitDestructableNeedsSaving(unit.Destructable))
				{
					modelUnit.HealthData = new ModelUnitHealthData
					{
						IsDestroyed = unit.Destructable.IsDestroyed,
						Health = unit.Destructable.CurrentHealth
					};
				}
				if (unit.IsStationOrShip())
				{
					modelUnit.TotalDamagedReceived = unit.Destructable.TotalDamageReceived;
				}
				modelUnit.AvoidDestruction = !unit.Destructable.AllowDestruction;
				modelUnit.IsInvulnerable = unit.Destructable.IsInvulnerable;
			}
			if (unit.ClassName != unit.UnitClass.UnitPrefab.ClassName && !string.IsNullOrWhiteSpace(unit.ClassName))
			{
				modelUnit.CustomClassName = unit.ClassName;
			}
			return modelUnit;
		}

		private bool UnitDestructableNeedsSaving(DestructableUnit destructableUnit)
		{
			if (destructableUnit.IsDestroyed)
			{
				return true;
			}
			if (destructableUnit.Unit.CargoComponent != null)
			{
				return destructableUnit.CurrentHealth != destructableUnit.Unit.CargoComponent.GetHealthBasedOnVolume();
			}
			return destructableUnit.CurrentHealth != destructableUnit.Unit.UnitClass.maxHealth;
		}

		private void ExportUnitComponents()
		{
			EngineASX.Instance.EnumerateUnitsWithPredicate((Unit unit) =>
			{
				GetModelUnit(unit).ComponentUnitData = ExportUnitComponentData(unit.Components);
			}, (Unit unit) => unit.Components != null);
		}

		private ModelComponentUnitData ExportUnitComponentData(UnitComponentHolder unitComponents)
		{
			ModelComponentUnitData modelComponentUnitData = new ModelComponentUnitData();
			modelComponentUnitData.ShipNameIndex = unitComponents.ShipNameId;
			modelComponentUnitData.CustomShipName = unitComponents.ShipName;
			UnitComponentHolder component = unitComponents.UnitClass.UnitPrefab.GetComponent<UnitComponentHolder>();
			if (component != null)
			{
				if (unitComponents.CargoCapacity != component.CargoCapacity)
				{
					modelComponentUnitData.CargoCapacity = unitComponents.CargoCapacity;
				}
				if (unitComponents.ScanRange != component.ScanRange)
				{
					modelComponentUnitData.ScanRange = (int)unitComponents.ScanRange;
				}
			}
			if (unitComponents.FactoryComponent != null)
			{
				modelComponentUnitData.FactoryData = new ModelComponentUnitFactoryData
				{
					Items = unitComponents.FactoryComponent.Items.Select((CargoFactoryItem x) => new ModelComponentUnitFactoryItemData
					{
						ProductionElapsed = x.ProductionElapsed,
						State = x.State
					}).ToList()
				};
			}
			modelComponentUnitData.ConstructionState = unitComponents.ConstructionState;
			modelComponentUnitData.ConstructionProgress = unitComponents.ConstructionProgress;
			modelComponentUnitData.CaptureCooldownTime = ((unitComponents.CaptureCooldownTime > EngineASX.Instance.ScenarioElapsedTime) ? new double?(unitComponents.CaptureCooldownTime) : ((double?)null));
			unitComponents.RefreshIsModded();
			if (unitComponents.IsModified)
			{
				modelComponentUnitData.ModData = ExportUnitComponentsModData(unitComponents);
			}
			if (unitComponents.Capacitor != null && !unitComponents.Capacitor.IsFullyCharged)
			{
				modelComponentUnitData.CapacitorCharge = unitComponents.Capacitor.ChargeNormalized;
			}
			modelComponentUnitData.IsCloaked = unitComponents.IsCloaked;
			foreach (ComponentBase unitComponent in unitComponents.UnitComponents)
			{
				if (!unitComponent.UserPowered)
				{
					modelComponentUnitData.PoweredDownBayIds.Add(unitComponent.Bay.Id);
				}
				if (unitComponent is TurretComponent { IsAlwaysAutoFire: false, AutoFire: not false } && unitComponents.Unit.IsOwnedByPlayer && unitComponents.Unit.IsShip())
				{
					modelComponentUnitData.AutoFireBayIds.Add(unitComponent.Bay.Id);
				}
			}
			if (unitComponents.Unit.IsInActiveSector && unitComponents.EngineComponent != null && unitComponents.EngineComponent.EngineThrottle != 0f)
			{
				modelComponentUnitData.EngineThrottle = unitComponents.EngineComponent.EngineThrottle;
			}
			CargoBayItem[] cargoItems = unitComponents.CargoBayComponent.GetCargoItems();
			if (cargoItems.Any())
			{
				modelComponentUnitData.CargoData = new ModelComponentUnitCargoData();
				CargoBayItem[] array = cargoItems;
				foreach (CargoBayItem cargoBayItem in array)
				{
					modelComponentUnitData.CargoData.Items.Add(new ModelComponentUnitCargoDataItem
					{
						CargoClass = GetModelCargoClass(cargoBayItem.CargoClass),
						Quantity = cargoBayItem.Quantity
					});
				}
			}
			if (unitComponents.ShieldComponent != null && !unitComponents.ShieldComponent.IsFullyCharged)
			{
				modelComponentUnitData.ShieldData = new ModelComponentUnitShieldHealthData();
				for (int num2 = 0; num2 < 6; num2++)
				{
					if (unitComponents.ShieldComponent.IsShieldDamaged(num2))
					{
						modelComponentUnitData.ShieldData.Items.Add(new ModelComponentUnitShieldHealthDataItem
						{
							Health = unitComponents.ShieldComponent.GetShieldPointNormalized(num2),
							ShieldPointIndex = num2
						});
					}
				}
			}
			if (unitComponents.AnyComponentDamaged())
			{
				modelComponentUnitData.ComponentHealthData = new ModelComponentUnitComponentHealthData();
				foreach (ComponentBase unitComponent2 in unitComponents.UnitComponents)
				{
					if (unitComponent2.IsDamaged)
					{
						modelComponentUnitData.ComponentHealthData.Items.Add(new ModelComponentUnitComponentHealthDataItem
						{
							BayId = unitComponent2.Bay.Id,
							Health = unitComponent2.HealthNormalized
						});
					}
				}
			}
			if (unitComponents.Unit.IsShip() && unitComponents.Unit.IsOwnedByPlayer && unitComponents.AutoTurretModule != null && unitComponents.AutoTurretModule.FireMode != AutoTurretFireMode.PreferredTargetOnly)
			{
				modelComponentUnitData.AutoTurretFireMode = unitComponents.AutoTurretModule.FireMode;
			}
			return modelComponentUnitData;
		}

		private ModelComponentUnitModData ExportUnitComponentsModData(UnitComponentHolder unitComponents)
		{
			ModelComponentUnitModData modelComponentUnitModData = new ModelComponentUnitModData();
			foreach (ComponentBay bay in unitComponents.Bays)
			{
				int num = -1;
				ComponentBase installedComponent = bay.InstalledComponent;
				if (installedComponent != null)
				{
					num = bay.InstalledComponent.ComponentClass.UniqueId;
				}
				int num2 = -1;
				if (bay.InitialComponentClass != null)
				{
					num2 = bay.InitialComponentClass.UniqueId;
				}
				if (num != num2)
				{
					modelComponentUnitModData.Items.Add(new ModelComponentUnitModDataItem
					{
						BayId = bay.Id,
						ComponentClass = ((installedComponent != null) ? GetModelComponentClass(installedComponent.ComponentClass) : ModelComponentClass.None)
					});
				}
			}
			return modelComponentUnitModData;
		}

		private ModelUnitClass GetModelUnitClass(UnitClass unitClass)
		{
			if (unitClass != null)
			{
				return (ModelUnitClass)unitClass.UniqueID;
			}
			return ModelUnitClass.None;
		}

		private static ModelCargoClass GetModelCargoClass(CargoClass cargoClass)
		{
			if (cargoClass != null)
			{
				return (ModelCargoClass)cargoClass.UniqueId;
			}
			return ModelCargoClass.None;
		}

		private ModelComponentClass GetModelComponentClass(ComponentClass componentClass)
		{
			if (componentClass != null)
			{
				return (ModelComponentClass)componentClass.UniqueId;
			}
			return ModelComponentClass.None;
		}

		private bool ShouldWriteUnit(Unit unit)
		{
			if (unit == null)
			{
				return false;
			}
			return unit.UnitType switch
			{
				UnitType.Projectile => unit.IsInActiveSector, 
				UnitType.Waypoint => false, 
				_ => true, 
			};
		}

		private void ExportSectors()
		{
			foreach (ModelSector sector in GetSectors())
			{
				AddModelSector(sector);
			}
		}

		private void AddModelSector(ModelSector sector)
		{
			sectorsById.Add(sector.Id, sector);
			savedGame.Sectors.Add(sector);
		}

		private void ExportFactions()
		{
			engine.Factions.TrimNulls();
			foreach (ModelFaction item in engine.Factions.Select((Faction e) => ExportFaction(e)))
			{
				AddModelFaction(item);
			}
		}

		private void AddModelFaction(ModelFaction modelFaction)
		{
			factionsById.Add(modelFaction.Id, modelFaction);
			savedGame.Factions.Add(modelFaction);
		}

		private void ExportPatrolPaths()
		{
			savedGame.PatrolPaths.AddRange(GetPatrolPaths());
		}

		private bool FactionAttitudeRequiresSaving(FactionAttitude factionAttitude)
		{
			if (factionAttitude.PermanentPeace || factionAttitude.RestrictHostilityTimeout)
			{
				return true;
			}
			if (factionAttitude.Neutrality != Neutrality.Neutral)
			{
				return true;
			}
			return false;
		}

		private void ExportFactionRelations()
		{
			foreach (Faction faction in engine.Factions)
			{
				ModelFaction modelFaction = GetModelFaction(faction.UniqueId);
				foreach (FactionAttitude relation in faction.Relations)
				{
					if (FactionAttitudeRequiresSaving(relation))
					{
						modelFaction.Relations.Items.Add(new ModelFactionRelationDataItem
						{
							OtherFaction = GetModelFaction(relation.TargetFaction),
							PermanentPeace = relation.PermanentPeace,
							RestrictHostilityTimeout = relation.RestrictHostilityTimeout,
							Neutrality = relation.Neutrality,
							HostilityEndTime = relation.HostilityEndTime
						});
					}
					if (relation.Neutrality != Neutrality.Hostile)
					{
						float recentDamageFrom = faction.GetRecentDamageFrom(relation.TargetFaction);
						if (recentDamageFrom > 0f)
						{
							modelFaction.RecentDamageItems.Add(new ModelFactionRecentDamageItem
							{
								OtherFaction = GetModelFaction(relation.TargetFaction),
								RecentDamageReceived = recentDamageFrom
							});
						}
					}
				}
			}
		}

		private void ExportFactionOpinions()
		{
			foreach (Faction faction in engine.Factions)
			{
				ModelFaction modelFaction = GetModelFaction(faction.UniqueId);
				foreach (FactionAttitude relation in faction.Relations)
				{
					if (relation.TargetFaction != null && (relation.Opinion != 0f || faction.GetRecentDamageFrom(relation.TargetFaction) > 0f))
					{
						modelFaction.Opinions.Items.Add(new ModelFactionOpinionDataItem
						{
							OtherFaction = GetModelFaction(relation.TargetFaction),
							Opinion = relation.Opinion,
							CreatedTime = relation.CreatedTime
						});
					}
				}
			}
		}

		private IEnumerable<ModelSectorPatrolPath> GetPatrolPaths()
		{
			return engine.PatrolPaths.Select((AIPatrolPath e) => new ModelSectorPatrolPath
			{
				Id = e.UniqueId,
				IsLoop = e.IsLoop,
				Sector = GetModelSector(e.Sector),
				Nodes = e.Nodes.Select((AIPatrolPathNodeBehaviour node) => new ModelSectorPatrolPathNode
				{
					Order = node.Order,
					SectorPosition = node.SectorPosition.ToVec3()
				}).ToList()
			});
		}

		private ModelFaction ExportFaction(Faction faction)
		{
			ModelFaction modelFaction = new ModelFaction();
			modelFaction.Id = faction.UniqueId;
			modelFaction.GeneratedNameId = faction.GeneratedNameId;
			modelFaction.GeneratedSuffixId = faction.GeneratedSuffixId;
			if (modelFaction.GeneratedNameId < 0)
			{
				modelFaction.CustomName = faction.Name;
				modelFaction.CustomShortName = faction.ShortName;
			}
			modelFaction.HomeSector = GetModelSector(faction.HomeSector);
			modelFaction.HomeSectorPosition = faction.HomeSectorPosition?.ToVec3();
			modelFaction.Credits = faction.Credits;
			modelFaction.Description = faction.Description;
			modelFaction.IsCivilian = faction.IsCivilian;
			modelFaction.FactionType = faction.FactionType;
			modelFaction.Aggression = faction.Aggression;
			modelFaction.Virtue = faction.Virtue;
			modelFaction.Greed = faction.Greed;
			modelFaction.Cooperation = faction.Cooperation;
			modelFaction.TradeEfficiency = faction.TradeEfficiency;
			modelFaction.DynamicRelations = faction.DynamicFactionAttitudes;
			modelFaction.ShowJobBoards = faction.ShouldFactionShowMissionSpecs;
			modelFaction.CreateJobs = faction.CreateMissions;
			modelFaction.RequisitionPointMultiplier = faction.RequisitionPointMultiplier;
			modelFaction.DestroyWhenNoUnits = faction.DestroyWhenNoUnits;
			modelFaction.MinNpcCombatEfficiency = faction.MinAIUnitControllerCombatEfficiency;
			modelFaction.MaxNpcCombatEfficiency = faction.MaxAIUnitControllerCombatEfficiency;
			modelFaction.AdditionalRpProvision = faction.AdditionalRpProvision;
			modelFaction.TradeIllegalGoods = faction.TradeIllegalGoods;
			modelFaction.SpawnTime = faction.SpawnTime;
			modelFaction.HighestEverNetWorth = faction.HighestEverNetWorth;
			modelFaction.RankingSystemId = ((faction.PilotRankingSystem != null) ? faction.PilotRankingSystem.UniqueId : (-1));
			modelFaction.PreferredFormationId = ((faction.PreferredFormationStyle != null) ? faction.PreferredFormationStyle.UniqueId : (-1));
			if (faction.AISettings != null)
			{
				modelFaction.CustomSettings = ExportFactionCustomSettings(faction.AISettings);
			}
			if (faction.Stats != null && (faction.IsPlayerFaction || GameController.Instance.GameSettings.SaveGameSettings.SaveStatsForAIFactions))
			{
				modelFaction.Stats = ExportFactionStats(faction.Stats);
			}
			modelFaction.AutopilotExcludedSectors = faction.AutopilotExcludedSectors.Select((int e) => GetModelSector(e)).ToList();
			if (faction.PersonAvatarProfiles != null && faction.PersonAvatarProfiles.Count > 0)
			{
				if (modelFaction.AvatarProfileIds == null)
				{
					modelFaction.AvatarProfileIds = new List<byte>(4);
				}
				modelFaction.AvatarProfileIds.AddRange(faction.PersonAvatarProfiles.Select((AvatarProfile e) => (byte)e.Id));
			}
			return modelFaction;
		}

		private ModelSector GetModelSector(int? sectorId)
		{
			if (!sectorId.HasValue)
			{
				return null;
			}
			return sectorsById.GetValueOrDefault(sectorId.Value);
		}

		private ModelFaction GetModelFaction(Faction faction)
		{
			if (faction == null)
			{
				return null;
			}
			return GetModelFaction(faction.UniqueId);
		}

		private ModelFaction GetModelFaction(int? factionId)
		{
			if (!factionId.HasValue)
			{
				return null;
			}
			return factionsById.GetValueOrDefault(factionId.Value);
		}

		private ModelPerson GetModelPerson(Person person)
		{
			if (person == null)
			{
				return null;
			}
			return GetModelPerson(person.UniqueId);
		}

		private ModelPerson GetModelPerson(int? personId)
		{
			if (!personId.HasValue)
			{
				return null;
			}
			return peopleById.GetValueOrDefault(personId.Value);
		}

		private ModelUnit GetModelUnit(Unit unit)
		{
			if (unit == null)
			{
				return null;
			}
			return GetModelUnit(unit.UniqueId);
		}

		private ModelSector GetModelSector(Sector sector)
		{
			if (sector == null)
			{
				return null;
			}
			return GetModelSector(sector.UniqueId);
		}

		private ModelPassengerGroup GetModelPassengerGroup(PassengerGroup passengerGroup)
		{
			if (passengerGroup == null)
			{
				return null;
			}
			return GetModelPassengerGroup(passengerGroup.UniqueId);
		}

		private ModelPassengerGroup GetModelPassengerGroup(int? uniqueId)
		{
			if (!uniqueId.HasValue)
			{
				return null;
			}
			return passengerGroupsById.GetValueOrDefault(uniqueId.Value);
		}

		private ModelUnit GetModelUnit(int? uniqueId)
		{
			if (!uniqueId.HasValue)
			{
				return null;
			}
			return unitsById.GetValueOrDefault(uniqueId.Value);
		}

		private ModelSectorPatrolPath GetModelPatrolPath(AIPatrolPath patrolPath)
		{
			if (patrolPath == null)
			{
				return null;
			}
			return GetModelPatrolPath(patrolPath.UniqueId);
		}

		private ModelSectorPatrolPath GetModelPatrolPath(int? uniqueId)
		{
			return savedGame.PatrolPaths.FirstOrDefault((ModelSectorPatrolPath e) => e.Id == uniqueId);
		}

		private ModelFleet GetModelFleet(Fleet fleet)
		{
			if (fleet == null)
			{
				return null;
			}
			return GetModelFleet(fleet.UniqueId);
		}

		private ModelFleet GetModelFleet(int? uniqueId)
		{
			if (!uniqueId.HasValue)
			{
				return null;
			}
			return fleetsById.GetValueOrDefault(uniqueId.Value);
		}

		private ModelFactionStats ExportFactionStats(FactionStats stats)
		{
			return new ModelFactionStats
			{
				TotalShipsClaimed = stats.TotalShipsClaimed,
				UnitsDestroyedByClassId = stats.UnitsDestroyedCountByClass.ToDictionary((KeyValuePair<UnitClass, int> e) => (ModelUnitClass)e.Key.UniqueID, (KeyValuePair<UnitClass, int> e) => e.Value),
				UnitLostByClassId = stats.UnitsLostCountByClass.ToDictionary((KeyValuePair<UnitClass, int> e) => (ModelUnitClass)e.Key.UniqueID, (KeyValuePair<UnitClass, int> e) => e.Value),
				ScratchcardsScratched = stats.ScratchcardsScratched,
				HighestScratchcardWin = stats.HighestScratchcardWin
			};
		}

		private ModelFactionCustomSettings ExportFactionCustomSettings(FactionAISettings settings)
		{
			return new ModelFactionCustomSettings
			{
				PreferSingleShip = settings.PreferSingleShip,
				BuildShips = settings.BuildShips,
				RepairShips = settings.RepairShips,
				UpgradeShips = settings.UpgradeShips,
				RepairMinHullDamage = settings.RepairMinHullDamage,
				RepairMinCreditsBeforeRepair = settings.RepairMinCreditsBeforeRepair,
				PreferenceToPlaceBounty = settings.PreferenceToPlaceBounty,
				LargeShipPreference = settings.LargeShipPreference,
				CloakShipPreference = settings.CloakShipPreference,
				DailyIncome = settings.DailyIncome,
				HostileWithAll = settings.HostileWithAll,
				MinFleetUnitCount = settings.MinGroupUnitCount,
				MaxFleetUnitCount = settings.MaxGroupUnitCount,
				OffensiveStance = settings.OffensiveStance,
				AllowOtherFactionToUseDocks = settings.AllowForeignFactionToUseDocks,
				PreferenceToBuildTurrets = settings.PreferenceToBuildTurrets,
				PreferenceToBuildStations = settings.PreferenceToBuildStations,
				PreferenceToHaveAmmo = settings.PreferenceToHaveAmmo,
				IgnoreStationCreditsReserve = settings.IgnoreStationCreditsReserve,
				MaxJumpDistanceFromHomeSector = settings.MaxJumpDistanceFromHomeSector,
				MaxStationBuildDistanceFromHomeSector = settings.MaxStationBuildDistanceFromHomeSector,
				PilotGender = settings.PilotGender,
				FixedShipCount = settings.FixedShipCount,
				SectorControlLikelihood = settings.SectorControlLikelihood
			};
		}

		private IEnumerable<ModelSector> GetSectors()
		{
			return engine.Sectors.Select((Sector e) => ExportSector(e));
		}

		private ModelSector ExportSector(Sector sector)
		{
			ModelSector modelSector = new ModelSector
			{
				Id = sector.UniqueId,
				Name = sector.Name,
				MapPosition = sector.MapPosition.ToVec3(),
				Description = sector.Description,
				GateDistanceMultiplier = sector.GateDistanceMultiplier,
				RandomSeed = sector.RandomSeed,
				BackgroundRotation = sector.BackgroundRotation.eulerAngles.ToVec3(),
				AmbientLightColor = sector.AmbientLightColor.ToVec3(),
				DirectionLightColor = sector.DirectionLightColor.ToVec3(),
				DirectionLightRotation = sector.LightRotation.eulerAngles.ToVec3(),
				LastTimeChangedControl = sector.LastTimeChangedControl,
				LightDirectionFudge = sector.LightDirectionFudge
			};
			CustomSectorAppearance component = sector.GetComponent<CustomSectorAppearance>();
			if (component != null)
			{
				modelSector.CustomAppearance = ExportSectorCustomAppearance(component);
			}
			return modelSector;
		}

		private ModelSectorAppearance ExportSectorCustomAppearance(CustomSectorAppearance customSectorAppearance)
		{
			return new ModelSectorAppearance
			{
				NebulaBrightness = customSectorAppearance.SpaceConstructorParams.NebulaBrightness,
				NebulaColors = customSectorAppearance.SpaceConstructorParams.NebulaColors,
				NebulaComplexity = customSectorAppearance.SpaceConstructorParams.NebulaComplexity,
				NebulaCount = customSectorAppearance.SpaceConstructorParams.NebulaCount,
				NebulaTextureCount = customSectorAppearance.SpaceConstructorParams.NebulaTextureCount,
				NebulaStyles = customSectorAppearance.SpaceConstructorParams.NebulaStyles,
				StarsCount = customSectorAppearance.SpaceConstructorParams.StarsCount,
				StarsIntensity = customSectorAppearance.SpaceConstructorParams.StarsIntensity
			};
		}
	}
}
