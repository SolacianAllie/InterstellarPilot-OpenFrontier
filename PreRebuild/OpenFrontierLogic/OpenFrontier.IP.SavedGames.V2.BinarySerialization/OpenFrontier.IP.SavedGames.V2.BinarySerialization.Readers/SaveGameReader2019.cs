using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.SavedGames.V2.BinarySerialization.Readers.Helpers;
using OpenFrontier.IP.SavedGames.V2.Model;
using OpenFrontier.IP.SavedGames.V2.Model.Actions;
using OpenFrontier.IP.SavedGames.V2.Model.Factions;
using OpenFrontier.IP.SavedGames.V2.Model.Factions.Bounty;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.Models;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes;
using OpenFrontier.IP.SavedGames.V2.Model.Helpers;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs.Missions;
using OpenFrontier.IP.SavedGames.V2.Model.Scenarios;
using OpenFrontier.IP.SavedGames.V2.Model.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Readers
{
	public class SaveGameReader2019 : ISaveGameReader
	{
		private readonly HeaderReader2019 headerReader = new HeaderReader2019();

		private Dictionary<int, ModelUnit> unitsById = new Dictionary<int, ModelUnit>(512);

		private Dictionary<int, ModelSector> sectorsById = new Dictionary<int, ModelSector>(64);

		private Dictionary<int, ModelFaction> factionsById = new Dictionary<int, ModelFaction>(512);

		private Dictionary<int, ModelSectorPatrolPath> patrolPathsById = new Dictionary<int, ModelSectorPatrolPath>(512);

		private Dictionary<int, ModelPerson> peopleById = new Dictionary<int, ModelPerson>(512);

		private Dictionary<int, ModelPassengerGroup> passengerGroupsById = new Dictionary<int, ModelPassengerGroup>(512);

		private Dictionary<int, ModelFleet> fleetsById = new Dictionary<int, ModelFleet>(512);

		private Dictionary<int, ModelMission> missionsById = new Dictionary<int, ModelMission>(8);

		private Dictionary<int, ModelMissionObjective> missionObjectivesById = new Dictionary<int, ModelMissionObjective>(20);

		private Dictionary<int, ModelTriggerGroup> triggerGroupsById = new Dictionary<int, ModelTriggerGroup>(8);

		public static ISavedGame ReadFromPath(string path)
		{
			using BinaryReader reader = new BinaryReader(File.OpenRead(path));
			return new SaveGameReader().Read(reader);
		}

		public ISavedGame Read(BinaryReader reader)
		{
			SavedGame savedGame = new SavedGame();
			savedGame.Header = (ModelHeader)headerReader.Read(reader);
			PrintStatus("Loaded header", reader);
			reader.ReadString();
			savedGame.Sectors.AddRange(ReadSectors(reader));
			foreach (ModelSector sector in savedGame.Sectors)
			{
				sectorsById.Add(sector.Id, sector);
			}
			PrintStatus("Loaded sectors", reader);
			reader.ReadString();
			savedGame.Factions.AddRange(ReadFactions(reader));
			foreach (ModelFaction faction in savedGame.Factions)
			{
				factionsById.Add(faction.Id, faction);
			}
			PrintStatus("Loaded factions", reader);
			reader.ReadString();
			ReadAllFactionAvatarProfileIds(reader);
			PrintStatus("Loaded faction avatar profile Ids", reader);
			reader.ReadString();
			savedGame.PatrolPaths.AddRange(ReadPatrolPaths(reader));
			foreach (ModelSectorPatrolPath patrolPath in savedGame.PatrolPaths)
			{
				patrolPathsById.Add(patrolPath.Id, patrolPath);
			}
			PrintStatus("Loaded patrol paths", reader);
			reader.ReadString();
			ReadAllFactionRelations(reader);
			PrintStatus("Loaded faction relations", reader);
			reader.ReadString();
			ReadAllFactionRecentDamageReceived(reader);
			PrintStatus("Loaded faction recent damage", reader);
			reader.ReadString();
			ReadAllFactionOpinions(reader);
			PrintStatus("Loaded faction opinions", reader);
			reader.ReadString();
			ReadUnits(reader, savedGame);
			foreach (ModelUnit unit in savedGame.Units)
			{
				unitsById.Add(unit.Id, unit);
			}
			PrintStatus("Loaded units", reader);
			reader.ReadString();
			ReadUnitRadii(reader);
			PrintStatus("Loaded unit radii", reader);
			reader.ReadString();
			ReadNamedUnits(reader);
			PrintStatus("Loaded named units", reader);
			reader.ReadString();
			ReadAllComponentUnits(reader);
			PrintStatus("Loaded all unit components", reader);
			reader.ReadString();
			ReadUnitsUnderConstruction(reader);
			PrintStatus("Loaded units under construction", reader);
			reader.ReadString();
			ReadUnitTotalDamageReceived(reader);
			PrintStatus("Loaded units under construction", reader);
			reader.ReadString();
			ReadModdedComponents(reader);
			PrintStatus("Loaded modded components", reader);
			reader.ReadString();
			ReadUnitCapacitorCharges(reader);
			PrintStatus("Loaded unit capacitor charges", reader);
			reader.ReadString();
			ReadCloakedUnits(reader);
			PrintStatus("Loaded unit cloak states", reader);
			reader.ReadString();
			ReadPoweredDownComponents(reader);
			PrintStatus("Loaded powered down units", reader);
			reader.ReadString();
			ReadUnitEngineThrottles(reader);
			PrintStatus("Loaded engine throttle data", reader);
			reader.ReadString();
			ReadComponentUnitCargo(reader);
			PrintStatus("Loaded cargo", reader);
			reader.ReadString();
			ReadAllShieldHealthData(reader);
			PrintStatus("Loaded damaged shields", reader);
			reader.ReadString();
			ReadAllUnitComponentHealthData(reader);
			PrintStatus("Loaded damaged components", reader);
			reader.ReadString();
			ReadActiveUnits(reader);
			PrintStatus("Loaded active units", reader);
			reader.ReadString();
			ReadAllUnitHealthDatas(reader);
			PrintStatus("Loaded destructable units", reader);
			reader.ReadString();
			ReadAllFactionIntel(reader);
			PrintStatus("Loaded faction intel", reader);
			reader.ReadString();
			ReadPassengerGroups(reader);
			PrintStatus("Loaded passenger groups", reader);
			reader.ReadString();
			ReadWormholes(reader);
			PrintStatus("Loaded wormholes", reader);
			reader.ReadString();
			ReadHangars(reader);
			PrintStatus("Loaded hangers", reader);
			reader.ReadString();
			savedGame.Fleets.AddRange(ReadFleets(reader));
			foreach (ModelFleet fleet in savedGame.Fleets)
			{
				fleetsById.Add(fleet.Id, fleet);
			}
			PrintStatus("Loaded fleets", reader);
			reader.ReadString();
			ReadNamedFleets(reader);
			reader.ReadString();
			IEnumerable<ModelPerson> enumerable = ReadPeople(reader, out var playerPerson);
			savedGame.People.AddRange(enumerable);
			foreach (ModelPerson item in enumerable)
			{
				peopleById.Add(item.Id, item);
			}
			PrintStatus("Loaded people", reader);
			reader.ReadString();
			ReadFleetOrders(reader);
			PrintStatus("Loaded fleet orders", reader);
			reader.ReadString();
			ReadNpcPilots(reader);
			PrintStatus("Loaded NPC pilots", reader);
			reader.ReadString();
			ReadFactionLeaders(reader);
			PrintStatus("Loaded faction leaders", reader);
			reader.ReadString();
			ReadJobs(reader);
			PrintStatus("Loaded jobs", reader);
			reader.ReadString();
			ReadAllFactionAIsAndBountyBoards(reader);
			PrintStatus("Loaded faction AIs / bounty boards", reader);
			reader.ReadString();
			ReadFactionAIExcludedUnits(reader);
			PrintStatus("Loaded faction excluded unit data", reader);
			reader.ReadString();
			ReadFactionMercenaryData(reader);
			PrintStatus("Loaded mercenary data", reader);
			reader.ReadString();
			savedGame.FleetSpawners.AddRange(ReadFleetSpawners(reader));
			PrintStatus("Loaded NPC fleet spawners", reader);
			reader.ReadString();
			savedGame.Missions.AddRange(ReadMissions(reader));
			foreach (ModelMission mission in savedGame.Missions)
			{
				missionsById.Add(mission.Id, mission);
			}
			PrintStatus("Loaded jobs", reader);
			reader.ReadString();
			if (reader.ReadBoolean())
			{
				savedGame.Player = ReadGamePlayer(reader);
				savedGame.Player.Person = playerPerson;
				PrintStatus("Loaded player data", reader);
			}
			reader.ReadString();
			savedGame.CurrentHudTarget = reader.ReadUnit(unitsById);
			PrintStatus("Loaded hud data", reader);
			reader.ReadString();
			ReadAllFactionTransactions(reader);
			PrintStatus("Loaded all faction transactions", reader);
			reader.ReadString();
			savedGame.ScenarioData = ReadScenarioData(reader);
			PrintStatus("Loaded world", reader);
			reader.ReadString();
			savedGame.Moons.AddRange(ReadMoons(reader));
			PrintStatus("Loaded moons", reader);
			reader.ReadString();
			savedGame.SeedOptions = ReadSeedOptions(reader);
			reader.ReadString();
			ReadAutoTurretModuleData(reader);
			PrintStatus("Loaded auto-turret module data", reader);
			reader.ReadString();
			ReadAutoFireComponents(reader);
			PrintStatus("Loaded auto-fire component data", reader);
			reader.ReadString();
			ReadUnitCaptureCooldownTimes(reader);
			PrintStatus("Loaded unit capture cooldown times", reader);
			reader.ReadString();
			ReadPlayerFleetSettings(reader);
			PrintStatus("Loaded player fleet settings", reader);
			reader.ReadString();
			ReadCustomSectorAppearances(reader);
			PrintStatus("Loaded custom sector appearances", reader);
			reader.ReadString();
			ReadUnitMass(reader);
			PrintStatus("Loaded unit mass", reader);
			reader.ReadString();
			ReadUnitCargoCapacity(reader);
			PrintStatus("Loaded unit cargo capacity", reader);
			reader.ReadString();
			ReadUnitScanRange(reader);
			PrintStatus("Loaded unit scan range", reader);
			reader.ReadString();
			ReadInvulnerableUnits(reader);
			PrintStatus("Loaded invulnerable units", reader);
			reader.ReadString();
			ReadNoDestructionUnits(reader);
			PrintStatus("Loaded no destruction units", reader);
			reader.ReadString();
			ReadFleetOrderAvailableCredits(reader);
			PrintStatus("Loaded fleet order available credits", reader);
			reader.ReadString();
			ReadPersonCustomTitles(reader);
			PrintStatus("Loaded person custom titles", reader);
			reader.ReadString();
			ReadTriggerGroups(reader, savedGame);
			PrintStatus("Loaded trigger groups", reader);
			reader.ReadString();
			ReadDitchedUnits(reader, savedGame);
			PrintStatus("Loaded ditched units", reader);
			reader.ReadString();
			savedGame.EngineData = ReadEngineData(reader);
			PrintStatus("Loaded engine data", reader);
			reader.ReadString();
			ReadPlayerUnitFleetSettings(reader, savedGame);
			PrintStatus("Loaded player unit fleet settings", reader);
			reader.ReadString();
			ReadPlayerDefaultFleetSettings(reader, savedGame);
			PrintStatus("Loaded player default fleet settings", reader);
			reader.ReadString();
			ReadUnitTractorerInfo(reader, savedGame);
			PrintStatus("Loaded unit tractorer info", reader);
			reader.ReadString();
			ReadCustomUnitClasses(reader, savedGame);
			PrintStatus("Loaded custom unit class names", reader);
			return savedGame;
		}

		private void ReadCustomUnitClasses(BinaryReader reader, SavedGame savedGame)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelUnit modelUnit = reader.ReadUnit(unitsById);
				string customClassName = reader.ReadString();
				if (modelUnit != null)
				{
					modelUnit.CustomClassName = customClassName;
				}
			}
		}

		private void ReadUnitTractorerInfo(BinaryReader reader, SavedGame savedGame)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelUnit modelUnit = reader.ReadUnit(unitsById);
				ModelUnit modelUnit2 = reader.ReadUnit(unitsById);
				if (modelUnit != null && modelUnit2 != null && modelUnit != modelUnit2)
				{
					savedGame.TractorerDataItems.Add(new ModelTractorerDataItem
					{
						TractoredUnit = modelUnit2,
						TractoringUnit = modelUnit
					});
				}
			}
		}

		private void ReadPlayerDefaultFleetSettings(BinaryReader reader, SavedGame savedGame)
		{
			if (reader.ReadBoolean())
			{
				savedGame.PlayerDefaultFleetSettings = ReadPlayerFleetSettingsCombined(reader);
			}
		}

		private void ReadPlayerUnitFleetSettings(BinaryReader reader, SavedGame savedGame)
		{
			if (savedGame.PlayerUnitFleetSettingItems == null)
			{
				savedGame.PlayerUnitFleetSettingItems = new List<ModelPlayerUnitFleetSettings>();
			}
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelUnit unit = reader.ReadUnit(unitsById);
				ModelPlayerUnitFleetSettings modelPlayerUnitFleetSettings = new ModelPlayerUnitFleetSettings();
				modelPlayerUnitFleetSettings.Unit = unit;
				modelPlayerUnitFleetSettings.Settings = ReadPlayerFleetSettingsCombined(reader);
				savedGame.PlayerUnitFleetSettingItems.Add(modelPlayerUnitFleetSettings);
			}
		}

		private ModelPlayerFleetSettingsCombined ReadPlayerFleetSettingsCombined(BinaryReader reader)
		{
			ModelSectorTarget homeBase = ReadSectorTarget(reader);
			int formationId = reader.ReadInt32();
			ModelFleetSettings fleetSettings = ReadFleetSettings(reader);
			ModelPlayerFleetSettings playerFleetSettings = ReadPlayerFleetSettingsItem(reader);
			ModelPlayerFleetSettingsCombined modelPlayerFleetSettingsCombined = new ModelPlayerFleetSettingsCombined();
			modelPlayerFleetSettingsCombined.FormationId = formationId;
			modelPlayerFleetSettingsCombined.HomeBase = homeBase;
			modelPlayerFleetSettingsCombined.FleetSettings = fleetSettings;
			modelPlayerFleetSettingsCombined.FleetSettings.PlayerFleetSettings = playerFleetSettings;
			return modelPlayerFleetSettingsCombined;
		}

		private ModelSectorTarget ReadSectorTarget(BinaryReader reader)
		{
			if (reader.ReadBoolean())
			{
				return SectorTargetReader.Read(reader, sectorsById, unitsById, fleetsById);
			}
			return null;
		}

		private void ReadDitchedUnits(BinaryReader reader, SavedGame savedGame)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelUnit modelUnit = reader.ReadUnit(unitsById);
				int expiryTime = reader.ReadInt32();
				if (modelUnit != null)
				{
					savedGame.DitchedUnitsToBeCleanedUp.Add(new ModelDitchedUnit
					{
						Unit = modelUnit,
						ExpiryTime = expiryTime
					});
				}
			}
		}

		private ModelEngineData ReadEngineData(BinaryReader reader)
		{
			return new ModelEngineData
			{
				UnitIdCounter = reader.ReadInt32(),
				PlayerMessageIdCounter = reader.ReadInt32(),
				PersonIdCounter = reader.ReadInt32(),
				FactionIdCounter = reader.ReadInt32(),
				FleetOrderIdCounter = reader.ReadInt32(),
				PassengerGroupIdCounter = reader.ReadInt32(),
				JobIdCounter = reader.ReadInt32(),
				FleetIdCounter = reader.ReadInt32(),
				SectorIdCounter = reader.ReadInt32(),
				MissionIdCounter = reader.ReadInt32(),
				PatrolPathIdCounter = reader.ReadInt32(),
				MissionObjectiveIdCounter = reader.ReadInt32()
			};
		}

		private void ReadTriggerGroups(BinaryReader reader, SavedGame savedGame)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelTriggerGroup modelTriggerGroup = ReadTriggerGroup(reader);
				savedGame.TriggerGroups.Add(modelTriggerGroup);
				triggerGroupsById.Add(modelTriggerGroup.Id, modelTriggerGroup);
			}
			int num2 = reader.ReadInt32();
			for (int j = 0; j < num2; j++)
			{
				ModelTriggerGroup modelTriggerGroup2 = reader.ReadTriggerGroup(triggerGroupsById);
				int num3 = reader.ReadInt32();
				for (int k = 0; k < num3; k++)
				{
					ModelTrigger item = ReadTrigger(reader);
					modelTriggerGroup2.Triggers.Add(item);
				}
				int num4 = reader.ReadInt32();
				for (int l = 0; l < num4; l++)
				{
					ModelAction item2 = ReadAction(reader);
					modelTriggerGroup2.Actions.Add(item2);
				}
			}
		}

		private ModelTriggerGroup ReadTriggerGroup(BinaryReader reader)
		{
			return new ModelTriggerGroup
			{
				Id = reader.ReadInt32(),
				IsActive = reader.ReadBoolean(),
				FireAndDisable = reader.ReadBoolean(),
				EvaluateFrequency = reader.ReadSingle(),
				NextEvaluationTime = reader.ReadDouble(),
				FireCount = reader.ReadInt32(),
				MaxFireCount = reader.ReadInt32(),
				MaxFiredAction = (TriggerMaxFiredAction)reader.ReadByte()
			};
		}

		private ModelTrigger ReadTrigger(BinaryReader reader)
		{
			TriggerType triggerType = (TriggerType)reader.ReadInt32();
			ModelTrigger modelTrigger = TriggerUtils.CreateModelTrigger(triggerType);
			modelTrigger.Invert = reader.ReadBoolean();
			switch (triggerType)
			{
			case TriggerType.Player_CurrentHudTarget:
				((ModelTrigger_Player_CurrentHudTarget)modelTrigger).TargetUnit = reader.ReadUnit(unitsById);
				break;
			case TriggerType.Player_IsPilotting:
				((ModelTrigger_Player_IsPilotting)modelTrigger).WaitForHud = reader.ReadBoolean();
				break;
			case TriggerType.Scenario_TimeElapsed:
				((ModelTrigger_Scenario_TimeElapsed)modelTrigger).Time = reader.ReadSingle();
				break;
			}
			return modelTrigger;
		}

		private ModelAction ReadAction(BinaryReader reader)
		{
			ActionType actionType = (ActionType)reader.ReadInt32();
			ModelAction modelAction = ActionUtils.CreateModelAction(actionType);
			switch (actionType)
			{
			case ActionType.Player_NewMessageSimple:
			{
				ModelAction_Player_NewMessageSimple modelAction_Player_NewMessageSimple = (ModelAction_Player_NewMessageSimple)modelAction;
				modelAction_Player_NewMessageSimple.From = reader.ReadString();
				modelAction_Player_NewMessageSimple.To = reader.ReadString();
				modelAction_Player_NewMessageSimple.Subject = reader.ReadString();
				modelAction_Player_NewMessageSimple.Message = reader.ReadString();
				modelAction_Player_NewMessageSimple.Notifications = reader.ReadBoolean();
				break;
			}
			case ActionType.Mission_Activate:
				((ModelAction_Mission_Activate)modelAction).Mission = reader.ReadMission(missionsById);
				break;
			case ActionType.Mission_ChangeStage:
			{
				ModelAction_Mission_ChangeStage modelAction_Mission_ChangeStage = (ModelAction_Mission_ChangeStage)modelAction;
				int key = reader.ReadInt32();
				int num2 = reader.ReadInt32();
				ModelMission valueOrDefault = missionsById.GetValueOrDefault(key);
				if (valueOrDefault != null && num2 < valueOrDefault.Stages.Count)
				{
					modelAction_Mission_ChangeStage.Stage = valueOrDefault.Stages[num2];
				}
				break;
			}
			case ActionType.Mission_ActivateObjective:
			{
				ModelAction_Mission_ActivateObjective modelAction_Mission_ActivateObjective = (ModelAction_Mission_ActivateObjective)modelAction;
				int num = reader.ReadInt32();
				for (int i = 0; i < num; i++)
				{
					ModelMissionObjective modelMissionObjective = reader.ReadMissionObjective(missionObjectivesById);
					if (modelMissionObjective != null)
					{
						modelAction_Mission_ActivateObjective.Objectives.Add(modelMissionObjective);
					}
				}
				break;
			}
			case ActionType.Mission_CompleteObjective:
			{
				ModelAction_Mission_CompleteObjective modelAction_Mission_CompleteObjective = (ModelAction_Mission_CompleteObjective)modelAction;
				modelAction_Mission_CompleteObjective.MissionObjective = reader.ReadMissionObjective(missionObjectivesById);
				modelAction_Mission_CompleteObjective.Success = reader.ReadBoolean();
				break;
			}
			case ActionType.TriggerGroup_Activate:
				((ModelAction_TriggerGroup_Activate)modelAction).TriggerGroup = reader.ReadTriggerGroup(triggerGroupsById);
				break;
			}
			return modelAction;
		}

		private void ReadPersonCustomTitles(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int key = reader.ReadInt32();
				string customTitle = reader.ReadString();
				ModelPerson valueOrDefault = peopleById.GetValueOrDefault(key);
				if (valueOrDefault != null)
				{
					valueOrDefault.CustomTitle = customTitle;
				}
			}
		}

		private void ReadFleetOrderAvailableCredits(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int key = reader.ReadInt32();
				int num2 = reader.ReadInt32();
				int availableCredits = reader.ReadInt32();
				ModelFleet valueOrDefault = fleetsById.GetValueOrDefault(key);
				if (valueOrDefault != null && num2 >= 0 && num2 < valueOrDefault.OrdersCollection.Orders.Count)
				{
					valueOrDefault.OrdersCollection.Orders[num2].AvailableCredits = availableCredits;
				}
			}
		}

		private void ReadUnitCargoCapacity(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelUnit modelUnit = reader.ReadUnit(unitsById);
				float value = reader.ReadSingle();
				if (modelUnit != null && modelUnit.ComponentUnitData != null)
				{
					modelUnit.ComponentUnitData.CargoCapacity = value;
				}
			}
		}

		private void ReadUnitScanRange(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelUnit modelUnit = reader.ReadUnit(unitsById);
				int value = reader.ReadInt32();
				if (modelUnit != null && modelUnit.ComponentUnitData != null)
				{
					modelUnit.ComponentUnitData.ScanRange = value;
				}
			}
		}

		private void ReadInvulnerableUnits(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelUnit modelUnit = reader.ReadUnit(unitsById);
				if (modelUnit != null)
				{
					modelUnit.IsInvulnerable = true;
				}
			}
		}

		private void ReadNoDestructionUnits(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelUnit modelUnit = reader.ReadUnit(unitsById);
				if (modelUnit != null)
				{
					modelUnit.AvoidDestruction = true;
				}
			}
		}

		private void ReadUnitMass(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelUnit modelUnit = reader.ReadUnit(unitsById);
				float value = reader.ReadSingle();
				if (modelUnit != null)
				{
					modelUnit.Mass = value;
				}
			}
		}

		private void ReadUnitsUnderConstruction(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int key = reader.ReadInt32();
				ConstructionState constructionState = (ConstructionState)reader.ReadByte();
				float constructionProgress = reader.ReadSingle();
				ModelUnit valueOrDefault = unitsById.GetValueOrDefault(key);
				if (valueOrDefault != null)
				{
					valueOrDefault.ComponentUnitData.ConstructionState = constructionState;
					valueOrDefault.ComponentUnitData.ConstructionProgress = constructionProgress;
				}
			}
		}

		private void ReadUnitTotalDamageReceived(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelUnit modelUnit = reader.ReadUnit(unitsById);
				float totalDamagedReceived = reader.ReadSingle();
				if (modelUnit != null)
				{
					modelUnit.TotalDamagedReceived = totalDamagedReceived;
				}
			}
		}

		private void ReadCustomSectorAppearances(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelSector modelSector = reader.ReadSector(sectorsById);
				ModelSectorAppearance customAppearance = ReadCustomSectorAppearance(reader);
				if (modelSector != null)
				{
					modelSector.CustomAppearance = customAppearance;
				}
			}
		}

		private ModelSectorAppearance ReadCustomSectorAppearance(BinaryReader reader)
		{
			return new ModelSectorAppearance
			{
				NebulaBrightness = (NebulaBrightness)reader.ReadInt32(),
				NebulaColors = (NebulaColour)reader.ReadInt32(),
				NebulaComplexity = reader.ReadSingle(),
				NebulaCount = reader.ReadInt32(),
				NebulaTextureCount = reader.ReadInt32(),
				NebulaStyles = (NebulaStyle)reader.ReadInt32(),
				StarsCount = (StarsCount)reader.ReadInt32(),
				StarsIntensity = reader.ReadSingle()
			};
		}

		private void ReadPlayerFleetSettings(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelFleet modelFleet = reader.ReadFleet(fleetsById);
				bool notifyWhenOrderComplete = reader.ReadBoolean();
				bool notifyWhenScannedHostile = reader.ReadBoolean();
				bool notifyWhenAbandonedUnitFound = reader.ReadBoolean();
				bool notifyWhenAbandonedCargoFound = reader.ReadBoolean();
				if (modelFleet != null)
				{
					modelFleet.FleetSettings.PlayerFleetSettings = new ModelPlayerFleetSettings();
					modelFleet.FleetSettings.PlayerFleetSettings.NotifyWhenOrderComplete = notifyWhenOrderComplete;
					modelFleet.FleetSettings.PlayerFleetSettings.NotifyWhenScannedHostile = notifyWhenScannedHostile;
					modelFleet.FleetSettings.PlayerFleetSettings.NotifyWhenAbandonedUnitFound = notifyWhenAbandonedUnitFound;
					modelFleet.FleetSettings.PlayerFleetSettings.NotifyWhenAbandonedCargoFound = notifyWhenAbandonedCargoFound;
				}
			}
		}

		private ModelPlayerFleetSettings ReadPlayerFleetSettingsItem(BinaryReader reader)
		{
			bool notifyWhenOrderComplete = reader.ReadBoolean();
			bool notifyWhenScannedHostile = reader.ReadBoolean();
			bool notifyWhenAbandonedUnitFound = reader.ReadBoolean();
			bool notifyWhenAbandonedCargoFound = reader.ReadBoolean();
			return new ModelPlayerFleetSettings
			{
				NotifyWhenOrderComplete = notifyWhenOrderComplete,
				NotifyWhenScannedHostile = notifyWhenScannedHostile,
				NotifyWhenAbandonedUnitFound = notifyWhenAbandonedUnitFound,
				NotifyWhenAbandonedCargoFound = notifyWhenAbandonedCargoFound
			};
		}

		private void ReadUnitCaptureCooldownTimes(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelUnit modelUnit = reader.ReadUnit(unitsById);
				double value = reader.ReadDouble();
				if (modelUnit != null && modelUnit.ComponentUnitData != null)
				{
					modelUnit.ComponentUnitData.CaptureCooldownTime = value;
				}
			}
		}

		private void ReadAutoTurretModuleData(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelUnit modelUnit = reader.ReadUnit(unitsById);
				AutoTurretFireMode value = (AutoTurretFireMode)reader.ReadInt32();
				if (modelUnit != null && modelUnit.ComponentUnitData != null)
				{
					modelUnit.ComponentUnitData.AutoTurretFireMode = value;
				}
			}
		}

		private void ReadNamedFleets(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int key = reader.ReadInt32();
				string name = reader.ReadString();
				ModelFleet valueOrDefault = fleetsById.GetValueOrDefault(key);
				if (valueOrDefault != null)
				{
					valueOrDefault.Name = name;
				}
			}
		}

		private void ReadUnitRadii(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int key = reader.ReadInt32();
				float value = reader.ReadSingle();
				ModelUnit valueOrDefault = unitsById.GetValueOrDefault(key);
				if (valueOrDefault != null)
				{
					valueOrDefault.Radius = value;
				}
			}
		}

		private ModelSeedOptions ReadSeedOptions(BinaryReader reader)
		{
			if (reader.ReadBoolean())
			{
				return new ModelSeedOptions
				{
					SeedAbandonedCargo = reader.ReadBoolean(),
					SeedAbandonedShips = reader.ReadBoolean(),
					SeedCargoHolds = reader.ReadBoolean(),
					SeedFactionIntel = reader.ReadBoolean(),
					SeedPassengerGroups = reader.ReadBoolean()
				};
			}
			return null;
		}

		private IEnumerable<ModelMoon> ReadMoons(BinaryReader reader)
		{
			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				int key = reader.ReadInt32();
				int key2 = reader.ReadInt32();
				Vec3 offsetFromPlanet = reader.ReadVec3();
				ModelUnit valueOrDefault = unitsById.GetValueOrDefault(key);
				ModelUnit valueOrDefault2 = unitsById.GetValueOrDefault(key2);
				if (valueOrDefault != null && valueOrDefault2 != null)
				{
					yield return new ModelMoon
					{
						Unit = valueOrDefault,
						OrbitUnit = valueOrDefault2,
						OffsetFromPlanet = offsetFromPlanet
					};
				}
			}
		}

		private ModelScenarioData ReadScenarioData(BinaryReader reader)
		{
			ModelScenarioData modelScenarioData = new ModelScenarioData();
			modelScenarioData.HasRandomEvents = reader.ReadBoolean();
			if (modelScenarioData.HasRandomEvents)
			{
				modelScenarioData.NextRandomEventTime = reader.ReadDouble();
			}
			if (reader.ReadBoolean())
			{
				modelScenarioData.FactionSpawner = new ModelFactionSpawner();
				modelScenarioData.FactionSpawner.NextUpdate = reader.ReadDouble();
			}
			if (reader.ReadBoolean())
			{
				modelScenarioData.TradeRouteScenarioData = new ModelTradeRouteScenarioData();
				modelScenarioData.TradeRouteScenarioData.NumBlackSailShipsDestroyed = reader.ReadInt32();
				modelScenarioData.TradeRouteScenarioData.PirateFaction = reader.ReadFaction(factionsById);
			}
			modelScenarioData.RespawnOnDeath = (RespawnOnDeathPreference)reader.ReadInt32();
			modelScenarioData.AllowTeleporting = reader.ReadBoolean();
			modelScenarioData.Permadeath = reader.ReadBoolean();
			modelScenarioData.AsteroidRespawningEnabled = reader.ReadBoolean();
			modelScenarioData.AsteroidRespawnTime = reader.ReadSingle();
			modelScenarioData.NextProcessOtherEventsTime = reader.ReadSingle();
			return modelScenarioData;
		}

		private void ReadAllFactionTransactions(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelFaction modelFaction = reader.ReadFaction(factionsById);
				int num2 = reader.ReadInt32();
				for (int j = 0; j < num2; j++)
				{
					ModelFactionTransaction item = ReadFactionTransaction(reader);
					modelFaction.Transactions.Add(item);
				}
			}
		}

		private ModelFactionTransaction ReadFactionTransaction(BinaryReader reader)
		{
			ModelFactionTransaction modelFactionTransaction = new ModelFactionTransaction
			{
				TransactionType = (FactionTransactionType)reader.ReadInt32(),
				Value = reader.ReadInt32(),
				CurrentBalance = reader.ReadInt32(),
				LocationUnit = reader.ReadUnit(unitsById),
				OtherFaction = reader.ReadFaction(factionsById),
				RelatedCargoClass = (ModelCargoClass)reader.ReadInt32(),
				RelatedUnitClass = (ModelUnitClass)reader.ReadInt32(),
				GameWorldTime = reader.ReadDouble(),
				TaxType = (FactionTransactionTaxType)reader.ReadInt32()
			};
			int num = reader.ReadInt32();
			modelFactionTransaction.RelatedCount = ((num > -1) ? new int?(num) : ((int?)null));
			return modelFactionTransaction;
		}

		private ModelPlayer ReadGamePlayer(BinaryReader reader)
		{
			ModelPlayer modelPlayer = new ModelPlayer();
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelUnit modelUnit = reader.ReadUnit(unitsById);
				if (modelUnit != null)
				{
					modelPlayer.VisitedUnits.Add(modelUnit);
				}
			}
			int num2 = reader.ReadInt32();
			for (int j = 0; j < num2; j++)
			{
				modelPlayer.Messages.Add(ReadPlayerMessage(reader));
			}
			int num3 = reader.ReadInt32();
			for (int k = 0; k < num3; k++)
			{
				ModelPlayerDelayedMessage modelPlayerDelayedMessage = new ModelPlayerDelayedMessage();
				modelPlayerDelayedMessage.ShowTime = reader.ReadDouble();
				modelPlayerDelayedMessage.Message = ReadPlayerMessage(reader);
				modelPlayer.DelayedMessages.Add(modelPlayerDelayedMessage);
			}
			modelPlayer.CustomWaypoint = ReadPlayerWaypointIfSet(reader);
			int key = reader.ReadInt32();
			modelPlayer.ActiveJob = missionsById.GetValueOrDefault(key);
			ReadPlayerStats(reader, modelPlayer);
			return modelPlayer;
		}

		private void ReadPlayerStats(BinaryReader reader, ModelPlayer player)
		{
			int num = reader.ReadInt32();
			if (num > 0)
			{
				if (player.Stats.SectorsVisited == null)
				{
					player.Stats.SectorsVisited = new List<ModelSector>(64);
				}
				for (int i = 0; i < num; i++)
				{
					ModelSector modelSector = reader.ReadSector(sectorsById);
					if (modelSector != null)
					{
						player.Stats.SectorsVisited.Add(modelSector);
					}
				}
			}
			player.Stats.TotalBountyClaimed = reader.ReadInt64();
			player.Stats.ShipsMinedToDeath = reader.ReadInt32();
		}

		private ModelPlayerWaypoint ReadPlayerWaypointIfSet(BinaryReader reader)
		{
			if (reader.ReadBoolean())
			{
				return ReadPlayerWaypoint(reader);
			}
			return null;
		}

		public ModelPlayerWaypoint ReadPlayerWaypoint(BinaryReader reader)
		{
			return new ModelPlayerWaypoint
			{
				SectorPosition = reader.ReadVec3(),
				Sector = reader.ReadSector(sectorsById),
				TargetUnit = reader.ReadUnit(unitsById),
				HadTargetObject = reader.ReadBoolean()
			};
		}

		public ModelPlayerMessage ReadPlayerMessage(BinaryReader reader)
		{
			ModelPlayerMessage modelPlayerMessage = new ModelPlayerMessage();
			modelPlayerMessage.Id = reader.ReadInt32();
			modelPlayerMessage.EngineTimeStamp = reader.ReadDouble();
			modelPlayerMessage.AllowDelete = reader.ReadBoolean();
			modelPlayerMessage.Opened = reader.ReadBoolean();
			modelPlayerMessage.SenderUnit = reader.ReadUnit(unitsById);
			modelPlayerMessage.SenderUnitSector = reader.ReadSector(sectorsById);
			modelPlayerMessage.SenderUnitSectorPosition = reader.ReadVec3();
			modelPlayerMessage.SubjectUnit = reader.ReadUnit(unitsById);
			modelPlayerMessage.SubjectUnitSector = reader.ReadSector(sectorsById);
			modelPlayerMessage.SubjectUnitSectorPosition = reader.ReadVec3();
			if (reader.ReadBoolean())
			{
				modelPlayerMessage.MessageTemplateId = reader.ReadInt32();
			}
			else
			{
				modelPlayerMessage.MessageTemplateId = -1;
				modelPlayerMessage.ToText = reader.ReadString();
				modelPlayerMessage.FromText = reader.ReadString();
				modelPlayerMessage.MessageText = reader.ReadString();
				modelPlayerMessage.SubjectText = reader.ReadString();
			}
			return modelPlayerMessage;
		}

		private IEnumerable<ModelMission> ReadMissions(BinaryReader reader)
		{
			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				ModelMission modelMission = ReadMission(reader, sectorsById, factionsById, unitsById, fleetsById);
				if (modelMission != null)
				{
					yield return modelMission;
				}
			}
		}

		private IEnumerable<ModelFleetSpawner> ReadFleetSpawners(BinaryReader reader)
		{
			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				yield return ReadFleetSpawner(reader, factionsById, sectorsById, unitsById, fleetsById, peopleById, patrolPathsById);
			}
		}

		private void ReadFactionMercenaryData(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadInt32();
				int num3 = reader.ReadInt32();
				double hireTimeExpiry = reader.ReadDouble();
				ModelFaction valueOrDefault = factionsById.GetValueOrDefault(num2);
				ModelFaction valueOrDefault2 = factionsById.GetValueOrDefault(num3);
				if (valueOrDefault != null)
				{
					if (valueOrDefault2 != null)
					{
						if (valueOrDefault.FactionAI != null)
						{
							valueOrDefault.FactionAI.FactionMercenaryHireInfo = new ModelFactionMercenaryHireInfo
							{
								HireTimeExpiry = hireTimeExpiry,
								HiringFaction = valueOrDefault2
							};
						}
						else
						{
							Logging.Warning($"Expecting mercenary faction {num2} to have FactionAI");
						}
					}
					else
					{
						Logging.UnknownFactionMessage(num3, $"loading mercenary info for faction {num2}");
					}
				}
				else
				{
					Logging.UnknownFactionMessage(num2, "loading mercenary info");
				}
			}
		}

		private void ReadFactionAIExcludedUnits(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelFaction modelFaction = reader.ReadFaction(factionsById);
				int num2 = reader.ReadInt32();
				for (int j = 0; j < num2; j++)
				{
					ModelUnit modelUnit = reader.ReadUnit(unitsById);
					if (modelFaction != null && modelFaction.FactionAI != null && modelUnit != null)
					{
						modelFaction.FactionAI.ExcludedUnits.Add(modelUnit);
					}
				}
			}
		}

		private void ReadAllFactionAIsAndBountyBoards(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelFaction modelFaction = reader.ReadFaction(factionsById);
				if (reader.ReadBoolean())
				{
					FactionAIType factionAIType = (FactionAIType)reader.ReadInt32();
					ModelFactionAI factionAI = FactionAIReader.Read(reader, factionAIType, sectorsById);
					modelFaction.FactionAI = factionAI;
				}
				if (reader.ReadBoolean())
				{
					modelFaction.BountyBoard = ReadFactionBountyBoard(reader);
				}
			}
		}

		private ModelFactionBountyBoard ReadFactionBountyBoard(BinaryReader reader)
		{
			ModelFactionBountyBoard modelFactionBountyBoard = new ModelFactionBountyBoard();
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelFactionBountyBoardItem item = ReadFactionBountyBoardItem(reader);
				modelFactionBountyBoard.Items.Add(item);
			}
			return modelFactionBountyBoard;
		}

		private ModelFactionBountyBoardItem ReadFactionBountyBoardItem(BinaryReader reader)
		{
			ModelFactionBountyBoardItem modelFactionBountyBoardItem = new ModelFactionBountyBoardItem
			{
				TargetPerson = reader.ReadPerson(peopleById),
				Reward = reader.ReadInt32(),
				LastKnownTargetUnit = reader.ReadUnit(unitsById),
				LastKnownTargetSector = reader.ReadSector(sectorsById),
				LastKnownTargetPosition = reader.ReadNullableVec3()
			};
			double num = reader.ReadDouble();
			modelFactionBountyBoardItem.TimeOfLastSighting = ((num >= 0.0) ? new double?(num) : ((double?)null));
			modelFactionBountyBoardItem.SourceFaction = reader.ReadFaction(factionsById);
			return modelFactionBountyBoardItem;
		}

		private void ReadJobs(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				JobType jobType = (JobType)reader.ReadInt32();
				ModelJob modelJob = JobReader.Read(reader, jobType, sectorsById, factionsById, unitsById);
				if (modelJob.Unit != null)
				{
					modelJob.Unit.Jobs.Add(modelJob);
				}
			}
		}

		private void ReadFactionLeaders(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadInt32();
				int num3 = reader.ReadInt32();
				ModelFaction valueOrDefault = factionsById.GetValueOrDefault(num2);
				ModelPerson valueOrDefault2 = peopleById.GetValueOrDefault(num3);
				if (valueOrDefault != null)
				{
					if (valueOrDefault2 != null)
					{
						valueOrDefault.Leader = valueOrDefault2;
					}
					else
					{
						Logging.UnknownPersonMessage(num3, "loading faction leaders");
					}
				}
				else
				{
					Logging.UnknownFactionMessage(num2, "loading faction leaders");
				}
			}
		}

		private void ReadNpcPilots(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelNpcPilot modelNpcPilot = ReadNpcPilot(reader);
				if (modelNpcPilot.Person != null)
				{
					modelNpcPilot.Person.NpcPilot = modelNpcPilot;
				}
				int key = reader.ReadInt32();
				modelNpcPilot.Fleet = fleetsById.GetValueOrDefault(key);
				if (modelNpcPilot.Fleet != null)
				{
					modelNpcPilot.Fleet.Npcs.Add(modelNpcPilot);
				}
			}
		}

		private ModelNpcPilot ReadNpcPilot(BinaryReader reader)
		{
			ModelNpcPilot modelNpcPilot = new ModelNpcPilot();
			ModelPerson person = CollectionExtensions.GetValueOrDefault(key: reader.ReadInt32(), dictionary: peopleById);
			modelNpcPilot.Person = person;
			modelNpcPilot.DestroyWhenNoUnit = reader.ReadBoolean();
			modelNpcPilot.DestroyWhenNotPilotting = reader.ReadBoolean();
			return modelNpcPilot;
		}

		private IEnumerable<ModelPerson> ReadPeople(BinaryReader reader, out ModelPerson playerPerson)
		{
			playerPerson = null;
			List<ModelPerson> list = new List<ModelPerson>(100);
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelPerson modelPerson = ReadPerson(reader, out var isPlayer);
				if (isPlayer)
				{
					playerPerson = modelPerson;
				}
				list.Add(modelPerson);
			}
			return list;
		}

		private ModelPerson ReadPerson(BinaryReader reader, out bool isPlayer)
		{
			int num = reader.ReadInt32();
			ModelPerson modelPerson = new ModelPerson();
			modelPerson.Id = num;
			if (reader.ReadBoolean())
			{
				modelPerson.GeneratedFirstNameId = reader.ReadInt32();
				modelPerson.GeneratedLastNameId = reader.ReadInt32();
			}
			else
			{
				modelPerson.CustomName = reader.ReadString();
				modelPerson.CustomShortName = reader.ReadString();
			}
			modelPerson.Seed = reader.ReadInt32();
			modelPerson.DialogId = reader.ReadInt32();
			modelPerson.IsMale = reader.ReadBoolean();
			modelPerson.IsAutoPilot = reader.ReadBoolean();
			int key = reader.ReadInt32();
			modelPerson.Faction = factionsById.GetValueOrDefault(key);
			modelPerson.DestroyGameObjectOnKill = reader.ReadBoolean();
			int num2 = reader.ReadInt32();
			ModelUnit valueOrDefault = unitsById.GetValueOrDefault(num2);
			if (num2 > -1 && valueOrDefault == null)
			{
				Logging.UnknownUnitMessage(num2, $"loading person id {num}");
			}
			if (valueOrDefault != null)
			{
				valueOrDefault.ComponentUnitData.People.Add(modelPerson);
				modelPerson.CurrentUnit = valueOrDefault;
			}
			bool flag = reader.ReadBoolean();
			if (flag && valueOrDefault != null)
			{
				valueOrDefault.ComponentUnitData.Pilot = modelPerson;
				modelPerson.IsPilot = flag;
			}
			modelPerson.Kills = reader.ReadInt32();
			modelPerson.Deaths = reader.ReadInt32();
			modelPerson.Properness = reader.ReadSingle();
			modelPerson.Aggression = reader.ReadSingle();
			modelPerson.Greed = reader.ReadSingle();
			modelPerson.RankId = reader.ReadInt32();
			modelPerson.AvatarProfileId = reader.ReadSByte();
			modelPerson.DialogProfileId = reader.ReadSByte();
			isPlayer = reader.ReadBoolean();
			if (reader.ReadBoolean())
			{
				modelPerson.NpcPilotSettings = ReadNpcPilotSettings(reader);
			}
			return modelPerson;
		}

		private static ModelNpcPilotSettings ReadNpcPilotSettings(BinaryReader reader)
		{
			return new ModelNpcPilotSettings
			{
				RestrictedWeaponPreference = reader.ReadSingle(),
				CombatEfficiency = reader.ReadSingle(),
				CheatAmmo = reader.ReadBoolean(),
				AllowDitchShip = reader.ReadBoolean()
			};
		}

		private IEnumerable<ModelFleet> ReadFleets(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			List<ModelFleet> list = new List<ModelFleet>();
			for (int i = 0; i < num; i++)
			{
				list.Add(ReadFleet(reader));
			}
			return list;
		}

		private ModelFleet ReadFleet(BinaryReader reader)
		{
			ModelFleet modelFleet = new ModelFleet();
			modelFleet.IsActive = reader.ReadBoolean();
			modelFleet.Id = reader.ReadInt32();
			modelFleet.Seed = reader.ReadInt32();
			modelFleet.Position = reader.ReadVec3();
			modelFleet.Rotation = reader.ReadVec4();
			int key = reader.ReadInt32();
			modelFleet.Sector = sectorsById.GetValueOrDefault(key);
			int key2 = reader.ReadInt32();
			modelFleet.Faction = factionsById.GetValueOrDefault(key2);
			modelFleet.FormationId = reader.ReadInt32();
			if (reader.ReadBoolean())
			{
				modelFleet.HomeBase = SectorTargetReader.Read(reader, sectorsById, unitsById, fleetsById);
			}
			modelFleet.ExcludeFromFactionAI = reader.ReadBoolean();
			modelFleet.Strategy = (FactionStrategy)reader.ReadInt32();
			if (reader.ReadBoolean())
			{
				modelFleet.FleetSettings = ReadFleetSettings(reader);
			}
			return modelFleet;
		}

		private void ReadFleetOrders(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelFleet modelFleet = reader.ReadFleet(fleetsById);
				modelFleet.OrdersCollection = ReadOrders(reader, modelFleet.Id, factionsById, sectorsById, unitsById, fleetsById, patrolPathsById, peopleById, passengerGroupsById);
			}
		}

		private ModelFleetSettings ReadFleetSettings(BinaryReader reader)
		{
			return new ModelFleetSettings
			{
				PreferCloak = reader.ReadBoolean(),
				PreferToDock = (DockedPreference)reader.ReadByte(),
				Aggression = reader.ReadSingle(),
				AllowAttack = reader.ReadBoolean(),
				TargetInterceptionLowerDistance = reader.ReadSingle(),
				TargetInterceptionUpperDistance = reader.ReadSingle(),
				MaxJumpDistance = reader.ReadInt32(),
				AllowCombatInterception = reader.ReadBoolean(),
				DestroyWhenNoPilots = reader.ReadBoolean(),
				FormationTightness = reader.ReadSingle(),
				CargoCollectionPreference = (FleetCargoCollectionPreference)reader.ReadInt32()
			};
		}

		private void ReadHangars(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadInt32();
				List<(int, int)> list = ReadHangerItems(reader).ToList();
				ModelUnit valueOrDefault = unitsById.GetValueOrDefault(num2);
				if (valueOrDefault != null)
				{
					if (valueOrDefault.ComponentUnitData.DockData == null)
					{
						valueOrDefault.ComponentUnitData.DockData = new ModelComponentUnitDockData();
					}
					foreach (var item in list)
					{
						if (item.Item2 > -1)
						{
							ModelUnit valueOrDefault2 = unitsById.GetValueOrDefault(item.Item2);
							if (valueOrDefault2 != null)
							{
								valueOrDefault.ComponentUnitData.DockData.Items.Add(new ModelComponentUnitDockDataItem
								{
									BayId = item.Item1,
									DockedUnit = valueOrDefault2
								});
							}
							else
							{
								Logging.UnknownUnitMessage(item.Item2, $"loading hangars. Unknown docked unit inside unit {num2}");
							}
						}
					}
				}
				else
				{
					Logging.UnknownUnitMessage(num2, "loading hangars. Unknown hangar unit.");
				}
			}
		}

		private IEnumerable<(int bayId, int dockedUnitId)> ReadHangerItems(BinaryReader reader)
		{
			int dockedUnitCount = reader.ReadInt32();
			for (int j = 0; j < dockedUnitCount; j++)
			{
				int item = reader.ReadInt32();
				int item2 = reader.ReadInt32();
				yield return (bayId: item, dockedUnitId: item2);
			}
		}

		private void ReadWormholes(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadInt32();
				ModelUnitWormholeData wormholeData = ReadWormholeData(reader, num2);
				ModelUnit valueOrDefault = unitsById.GetValueOrDefault(num2);
				if (valueOrDefault != null)
				{
					valueOrDefault.WormholeData = wormholeData;
				}
				else
				{
					Logging.UnknownUnitMessage(num2, "loading wormhole data");
				}
			}
		}

		private ModelUnitWormholeData ReadWormholeData(BinaryReader reader, int unitId)
		{
			ModelUnitWormholeData modelUnitWormholeData = new ModelUnitWormholeData();
			int num = reader.ReadInt32();
			modelUnitWormholeData.TargetWormholeUnit = unitsById.GetValueOrDefault(num);
			if (modelUnitWormholeData.TargetWormholeUnit == null)
			{
				Logging.UnknownUnitMessage(num, $"loading wormhole data for unit {unitId}");
			}
			modelUnitWormholeData.IsUnstable = reader.ReadBoolean();
			modelUnitWormholeData.UnstableNextChangeTargetTime = reader.ReadDouble();
			modelUnitWormholeData.UnstableTargetPosition = reader.ReadVec3();
			modelUnitWormholeData.UnstableTargetRotation = reader.ReadVec3();
			int key = reader.ReadInt32();
			modelUnitWormholeData.UnstableTargetSector = sectorsById.GetValueOrDefault(key);
			if (modelUnitWormholeData.IsUnstable && modelUnitWormholeData.UnstableTargetSector == null)
			{
				Logging.MissingSectorMessage($"loading wormhole data for unit {unitId}. Unstable wormhole must have target");
			}
			return modelUnitWormholeData;
		}

		private void ReadPassengerGroups(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelPassengerGroup modelPassengerGroup = ReadPassengerGroup(reader);
				if (modelPassengerGroup.Unit != null)
				{
					modelPassengerGroup.Unit.PassengerGroups.Add(modelPassengerGroup);
					passengerGroupsById.Add(modelPassengerGroup.Id, modelPassengerGroup);
				}
			}
		}

		private ModelPassengerGroup ReadPassengerGroup(BinaryReader reader)
		{
			ModelPassengerGroup modelPassengerGroup = new ModelPassengerGroup
			{
				Id = reader.ReadInt32()
			};
			int key = reader.ReadInt32();
			int key2 = reader.ReadInt32();
			int key3 = reader.ReadInt32();
			modelPassengerGroup.Unit = unitsById.GetValueOrDefault(key);
			modelPassengerGroup.SourceUnit = unitsById.GetValueOrDefault(key2);
			modelPassengerGroup.DestinationUnit = unitsById.GetValueOrDefault(key3);
			modelPassengerGroup.PassengerCount = reader.ReadInt32();
			modelPassengerGroup.ExpiryTime = reader.ReadDouble();
			modelPassengerGroup.Revenue = reader.ReadInt32();
			return modelPassengerGroup;
		}

		private void ReadAllFactionIntel(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadInt32();
				ModelFactionIntel intel = ReadFactionIntel(reader, num2);
				ModelFaction valueOrDefault = factionsById.GetValueOrDefault(num2);
				if (valueOrDefault != null)
				{
					valueOrDefault.Intel = intel;
				}
				else
				{
					Logging.UnknownFactionMessage(num2, "loading intel");
				}
			}
		}

		private ModelFactionIntel ReadFactionIntel(BinaryReader reader, int factionId)
		{
			ModelFactionIntel modelFactionIntel = new ModelFactionIntel();
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadInt32();
				ModelSector valueOrDefault = sectorsById.GetValueOrDefault(num2);
				if (valueOrDefault != null)
				{
					modelFactionIntel.Sectors.Add(valueOrDefault);
				}
				else
				{
					Logging.UnknownSectorMessage(num2, $"loading intel for faction {factionId}");
				}
			}
			int num3 = reader.ReadInt32();
			for (int j = 0; j < num3; j++)
			{
				int num4 = reader.ReadInt32();
				ModelUnit valueOrDefault2 = unitsById.GetValueOrDefault(num4);
				if (valueOrDefault2 != null)
				{
					modelFactionIntel.Units.Add(valueOrDefault2);
				}
				else
				{
					Logging.UnknownUnitMessage(num4, $"loading intel for faction {factionId}");
				}
			}
			int num5 = reader.ReadInt32();
			for (int k = 0; k < num5; k++)
			{
				int num6 = reader.ReadInt32();
				ModelUnit valueOrDefault3 = unitsById.GetValueOrDefault(num6);
				if (valueOrDefault3 != null)
				{
					modelFactionIntel.EnteredWormholes.Add(valueOrDefault3);
				}
				else
				{
					Logging.UnknownUnitMessage(num6, $"loading intel for faction {factionId}");
				}
			}
			return modelFactionIntel;
		}

		private void ReadAllUnitHealthDatas(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadInt32();
				ModelUnitHealthData healthData = ReadUnitHealthData(reader);
				ModelUnit valueOrDefault = unitsById.GetValueOrDefault(num2);
				if (valueOrDefault != null)
				{
					valueOrDefault.HealthData = healthData;
				}
				else
				{
					Logging.UnknownUnitMessage(num2, $"loading health data for unit {num2}");
				}
			}
		}

		private ModelUnitHealthData ReadUnitHealthData(BinaryReader reader)
		{
			return new ModelUnitHealthData
			{
				IsDestroyed = reader.ReadBoolean(),
				Health = reader.ReadSingle()
			};
		}

		private void ReadActiveUnits(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadInt32();
				ModelUnitActiveData activeData = ReadActiveUnitData(reader);
				ModelUnit valueOrDefault = unitsById.GetValueOrDefault(num2);
				if (valueOrDefault != null)
				{
					valueOrDefault.ActiveData = activeData;
				}
				else
				{
					Logging.UnknownUnitMessage(num2, $"loading active data for unit {num2}");
				}
			}
		}

		private ModelUnitActiveData ReadActiveUnitData(BinaryReader reader)
		{
			return new ModelUnitActiveData
			{
				Velocity = reader.ReadVec3(),
				CurrentTurn = reader.ReadSingle()
			};
		}

		private void ReadAllUnitComponentHealthData(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadInt32();
				ModelUnit valueOrDefault = unitsById.GetValueOrDefault(num2);
				ModelComponentUnitComponentHealthData componentHealthData = ReadUnitComponentHealthData(reader);
				if (valueOrDefault?.ComponentUnitData != null)
				{
					valueOrDefault.ComponentUnitData.ComponentHealthData = componentHealthData;
				}
				else
				{
					Logging.UnknownUnitMessage(num2, $"loading component heatlh data for unit {num2}");
				}
			}
		}

		private ModelComponentUnitComponentHealthData ReadUnitComponentHealthData(BinaryReader reader)
		{
			ModelComponentUnitComponentHealthData modelComponentUnitComponentHealthData = new ModelComponentUnitComponentHealthData();
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelComponentUnitComponentHealthDataItem modelComponentUnitComponentHealthDataItem = new ModelComponentUnitComponentHealthDataItem();
				modelComponentUnitComponentHealthDataItem.BayId = reader.ReadInt32();
				modelComponentUnitComponentHealthDataItem.Health = reader.ReadSingle();
				modelComponentUnitComponentHealthData.Items.Add(modelComponentUnitComponentHealthDataItem);
			}
			return modelComponentUnitComponentHealthData;
		}

		private void ReadAllShieldHealthData(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadInt32();
				ModelComponentUnitShieldHealthData shieldData = ReadShieldHealthData(reader);
				ModelComponentUnitData modelComponentUnitData = unitsById.GetValueOrDefault(num2)?.ComponentUnitData;
				if (modelComponentUnitData != null)
				{
					modelComponentUnitData.ShieldData = shieldData;
				}
				else
				{
					Logging.UnknownUnitMessage(num2, $"loading shield health data for unit {num2}");
				}
			}
		}

		private ModelComponentUnitShieldHealthData ReadShieldHealthData(BinaryReader reader)
		{
			byte b = reader.ReadByte();
			ModelComponentUnitShieldHealthData modelComponentUnitShieldHealthData = new ModelComponentUnitShieldHealthData();
			for (int i = 0; i < b; i++)
			{
				ModelComponentUnitShieldHealthDataItem modelComponentUnitShieldHealthDataItem = new ModelComponentUnitShieldHealthDataItem();
				modelComponentUnitShieldHealthDataItem.ShieldPointIndex = reader.ReadByte();
				modelComponentUnitShieldHealthDataItem.Health = reader.ReadSingle();
				modelComponentUnitShieldHealthData.Items.Add(modelComponentUnitShieldHealthDataItem);
			}
			return modelComponentUnitShieldHealthData;
		}

		private void ReadComponentUnitCargo(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadInt32();
				ModelComponentUnitData modelComponentUnitData = unitsById.GetValueOrDefault(num2)?.ComponentUnitData;
				ModelComponentUnitCargoData cargoData = ReadComponentUnitCargoData(reader);
				if (modelComponentUnitData != null)
				{
					modelComponentUnitData.CargoData = cargoData;
				}
				else
				{
					Logging.UnknownUnitMessage(num2, $"loading cargo data for unit {num2}");
				}
			}
		}

		private ModelComponentUnitCargoData ReadComponentUnitCargoData(BinaryReader reader)
		{
			ModelComponentUnitCargoData modelComponentUnitCargoData = new ModelComponentUnitCargoData();
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				modelComponentUnitCargoData.Items.Add(ComponentUnitCargoDataItemReader.Read(reader));
			}
			return modelComponentUnitCargoData;
		}

		private void ReadUnitEngineThrottles(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadInt32();
				float value = reader.ReadSingle();
				ModelUnit valueOrDefault = unitsById.GetValueOrDefault(num2);
				if (valueOrDefault?.ComponentUnitData != null)
				{
					valueOrDefault.ComponentUnitData.EngineThrottle = value;
				}
				else
				{
					Logging.UnknownUnitMessage(num2, $"loading engine throttle data for unit {num2}");
				}
			}
		}

		private void ReadPoweredDownComponents(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadInt32();
				int num3 = reader.ReadInt32();
				ModelUnit valueOrDefault = unitsById.GetValueOrDefault(num2);
				for (int j = 0; j < num3; j++)
				{
					int item = reader.ReadInt32();
					if (valueOrDefault != null && valueOrDefault.ComponentUnitData != null)
					{
						valueOrDefault.ComponentUnitData.PoweredDownBayIds.Add(item);
					}
					else
					{
						Logging.UnknownUnitMessage(num2, $"loading powered-down component data for unit {num2}");
					}
				}
			}
		}

		private void ReadAutoFireComponents(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadInt32();
				int num3 = reader.ReadInt32();
				ModelUnit valueOrDefault = unitsById.GetValueOrDefault(num2);
				for (int j = 0; j < num3; j++)
				{
					int item = reader.ReadInt32();
					if (valueOrDefault != null && valueOrDefault.ComponentUnitData != null)
					{
						valueOrDefault.ComponentUnitData.AutoFireBayIds.Add(item);
					}
					else
					{
						Logging.UnknownUnitMessage(num2, $"loading auto-fire component data for unit {num2}");
					}
				}
			}
		}

		private void ReadCloakedUnits(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadInt32();
				ModelUnit valueOrDefault = unitsById.GetValueOrDefault(num2);
				if (valueOrDefault?.ComponentUnitData != null)
				{
					valueOrDefault.ComponentUnitData.IsCloaked = true;
				}
				else
				{
					Logging.UnknownUnitMessage(num2, $"loading cloak state data for unit {num2}");
				}
			}
		}

		private void ReadUnitCapacitorCharges(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadInt32();
				float value = reader.ReadSingle();
				ModelUnit valueOrDefault = unitsById.GetValueOrDefault(num2);
				if (valueOrDefault?.ComponentUnitData != null)
				{
					valueOrDefault.ComponentUnitData.CapacitorCharge = value;
				}
				else
				{
					Logging.UnknownUnitMessage(num2, $"loading capacitor charge data for unit {num2}");
				}
			}
		}

		private void ReadModdedComponents(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int key = reader.ReadInt32();
				int bayId = reader.ReadInt32();
				int componentClass = reader.ReadInt32();
				ModelUnit valueOrDefault = unitsById.GetValueOrDefault(key);
				if (valueOrDefault != null)
				{
					if (valueOrDefault.ComponentUnitData.ModData == null)
					{
						valueOrDefault.ComponentUnitData.ModData = new ModelComponentUnitModData();
					}
					valueOrDefault.ComponentUnitData.ModData.Items.Add(new ModelComponentUnitModDataItem
					{
						BayId = bayId,
						ComponentClass = (ModelComponentClass)componentClass
					});
				}
			}
		}

		private void ReadAllComponentUnits(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int key = reader.ReadInt32();
				unitsById.GetValueOrDefault(key).ComponentUnitData = ReadComponentUnitData(reader);
			}
		}

		private ModelComponentUnitData ReadComponentUnitData(BinaryReader reader)
		{
			ModelComponentUnitData modelComponentUnitData = new ModelComponentUnitData();
			modelComponentUnitData.ShipNameIndex = reader.ReadInt32();
			if (modelComponentUnitData.ShipNameIndex == -1)
			{
				modelComponentUnitData.CustomShipName = reader.ReadString();
			}
			if (reader.ReadBoolean())
			{
				modelComponentUnitData.FactoryData = ReadComponentUnitFactoryData(reader);
			}
			return modelComponentUnitData;
		}

		private ModelComponentUnitFactoryData ReadComponentUnitFactoryData(BinaryReader reader)
		{
			ModelComponentUnitFactoryData modelComponentUnitFactoryData = new ModelComponentUnitFactoryData();
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelComponentUnitFactoryItemData modelComponentUnitFactoryItemData = new ModelComponentUnitFactoryItemData();
				modelComponentUnitFactoryItemData.State = (CargoFactoryItemState)reader.ReadInt32();
				modelComponentUnitFactoryItemData.ProductionElapsed = reader.ReadSingle();
				modelComponentUnitFactoryData.Items.Add(modelComponentUnitFactoryItemData);
			}
			return modelComponentUnitFactoryData;
		}

		private void ReadNamedUnits(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int key = reader.ReadInt32();
				string name = reader.ReadString();
				string shortName = reader.ReadString();
				ModelUnit valueOrDefault = unitsById.GetValueOrDefault(key);
				if (valueOrDefault != null)
				{
					valueOrDefault.Name = name;
					valueOrDefault.ShortName = shortName;
				}
			}
		}

		private void PrintStatus(string message, BinaryReader reader)
		{
			Console.WriteLine($"{message} - {reader.BaseStream.Position - 1} bytes read");
		}

		private void ReadUnits(BinaryReader reader, SavedGame savedGame)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelUnit item = ReadUnit(reader, savedGame.Sectors, savedGame.Factions, savedGame.Units);
				savedGame.Units.Add(item);
			}
		}

		private ModelUnit ReadUnit(BinaryReader reader, IEnumerable<ModelSector> sectors, IEnumerable<ModelFaction> factions, IEnumerable<ModelUnit> units)
		{
			ModelUnit modelUnit = new ModelUnit();
			modelUnit.Id = reader.ReadInt32();
			modelUnit.Seed = reader.ReadInt32();
			int num = reader.ReadInt32();
			if (!Enum.IsDefined(typeof(ModelUnitClass), modelUnit.Class))
			{
				throw new Exception($"Unrecognised unit class {modelUnit.Class}");
			}
			modelUnit.Class = (ModelUnitClass)num;
			int sectorId = reader.ReadInt32();
			modelUnit.Sector = sectors.FirstOrDefault((ModelSector e) => e.Id == sectorId);
			if (sectorId > -1 && modelUnit.Sector == null)
			{
				Logging.UnknownSectorMessage(sectorId, $"loading unit {modelUnit.Id}");
			}
			modelUnit.Position = reader.ReadVec3();
			modelUnit.Rotation = reader.ReadVec3();
			int factionId = reader.ReadInt32();
			modelUnit.Faction = factions.FirstOrDefault((ModelFaction e) => e.Id == factionId);
			if (factionId > -1 && modelUnit.Faction == null)
			{
				Logging.UnknownFactionMessage(factionId, $"loading unit {modelUnit.Id}");
			}
			modelUnit.RpProvision = reader.ReadInt32();
			if (reader.ReadBoolean())
			{
				modelUnit.CargoData = ReadUnitCargoData(reader);
			}
			if (reader.ReadBoolean())
			{
				modelUnit.DebrisData = ReadUnitDebrisData(reader);
			}
			if (reader.ReadBoolean())
			{
				modelUnit.AsteroidData = ReadUnitAsteroidData(reader);
			}
			if (reader.ReadBoolean())
			{
				modelUnit.ShipTraderData = ReadShipTrader(reader);
			}
			if (UnitHelper.IsProjectile(modelUnit.Class) && reader.ReadBoolean())
			{
				modelUnit.ProjectileData = ReadUnitProjectileData(reader);
			}
			return modelUnit;
		}

		private ModelUnitProjectileData ReadUnitProjectileData(BinaryReader reader)
		{
			return new ModelUnitProjectileData
			{
				SourceUnit = reader.ReadUnit(unitsById),
				TargetUnit = reader.ReadUnit(unitsById),
				FireTime = reader.ReadDouble(),
				RemainingMovement = reader.ReadSingle(),
				DamageType = reader.ReadDamageType()
			};
		}

		private ModelUnitShipTraderData ReadShipTrader(BinaryReader reader)
		{
			ModelUnitShipTraderData modelUnitShipTraderData = new ModelUnitShipTraderData();
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelUnitShipTraderItem modelUnitShipTraderItem = new ModelUnitShipTraderItem();
				modelUnitShipTraderItem.SellMultiplier = reader.ReadSingle();
				modelUnitShipTraderItem.UnitClass = (ModelUnitClass)reader.ReadInt32();
				modelUnitShipTraderData.Items.Add(modelUnitShipTraderItem);
			}
			return modelUnitShipTraderData;
		}

		private ModelUnitAsteroidData ReadUnitAsteroidData(BinaryReader reader)
		{
			return new ModelUnitAsteroidData
			{
				RemainingYield = reader.ReadInt32()
			};
		}

		private ModelUnitDebrisData ReadUnitDebrisData(BinaryReader reader)
		{
			return new ModelUnitDebrisData
			{
				ScrapQuantity = reader.ReadInt32(),
				Expires = reader.ReadBoolean(),
				ExpiryTime = reader.ReadDouble(),
				RelatedUnitClass = (ModelUnitClass)reader.ReadInt32()
			};
		}

		private ModelUnitCargoData ReadUnitCargoData(BinaryReader reader)
		{
			return new ModelUnitCargoData
			{
				CargoClass = (ModelCargoClass)reader.ReadInt32(),
				Quantity = reader.ReadInt32(),
				Expires = reader.ReadBoolean(),
				SpawnTime = reader.ReadDouble()
			};
		}

		private IEnumerable<ModelFaction> ReadFactions(BinaryReader reader)
		{
			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				yield return ReadFaction(reader);
			}
		}

		private void ReadAllFactionAvatarProfileIds(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelFaction modelFaction = reader.ReadFaction(factionsById);
				byte b = reader.ReadByte();
				for (int j = 0; j < b; j++)
				{
					byte item = reader.ReadByte();
					if (modelFaction != null)
					{
						if (modelFaction.AvatarProfileIds == null)
						{
							modelFaction.AvatarProfileIds = new List<byte>();
						}
						modelFaction.AvatarProfileIds.Add(item);
					}
				}
			}
		}

		private IEnumerable<ModelSector> ReadSectors(BinaryReader reader)
		{
			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				yield return ReadSector(reader);
			}
		}

		private IEnumerable<ModelSectorPatrolPath> ReadPatrolPaths(BinaryReader reader)
		{
			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				yield return ReadPatrolPath(reader);
			}
		}

		private void ReadAllFactionRelations(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadInt32();
				ModelFactionRelationData relations = ReadFactionRelationData(reader, num2);
				ModelFaction valueOrDefault = factionsById.GetValueOrDefault(num2);
				if (valueOrDefault != null)
				{
					valueOrDefault.Relations = relations;
				}
				else
				{
					Logging.UnknownFactionMessage(num2, "loading faction relations");
				}
			}
		}

		private void ReadAllFactionRecentDamageReceived(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelFaction modelFaction = reader.ReadFaction(factionsById);
				int num2 = reader.ReadInt32();
				for (int j = 0; j < num2; j++)
				{
					ModelFaction modelFaction2 = reader.ReadFaction(factionsById);
					float recentDamageReceived = reader.ReadSingle();
					if (modelFaction != null && modelFaction2 != null)
					{
						modelFaction.RecentDamageItems.Add(new ModelFactionRecentDamageItem
						{
							OtherFaction = modelFaction2,
							RecentDamageReceived = recentDamageReceived
						});
					}
				}
			}
		}

		private ModelFactionRelationData ReadFactionRelationData(BinaryReader reader, int factionId)
		{
			ModelFactionRelationData modelFactionRelationData = new ModelFactionRelationData();
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelFactionRelationDataItem modelFactionRelationDataItem = ReadFactionRelationDataItem(reader, factionId);
				if (modelFactionRelationDataItem.OtherFaction != null)
				{
					modelFactionRelationData.Items.Add(modelFactionRelationDataItem);
				}
			}
			return modelFactionRelationData;
		}

		private ModelFactionRelationDataItem ReadFactionRelationDataItem(BinaryReader reader, int factionId)
		{
			ModelFactionRelationDataItem modelFactionRelationDataItem = new ModelFactionRelationDataItem();
			int num = reader.ReadInt32();
			modelFactionRelationDataItem.OtherFaction = factionsById.GetValueOrDefault(num);
			if (modelFactionRelationDataItem.OtherFaction == null)
			{
				Logging.UnknownFactionMessage(num, $"loading faction relation data for faction {factionId}");
			}
			modelFactionRelationDataItem.PermanentPeace = reader.ReadBoolean();
			modelFactionRelationDataItem.RestrictHostilityTimeout = reader.ReadBoolean();
			modelFactionRelationDataItem.Neutrality = (Neutrality)reader.ReadInt32();
			modelFactionRelationDataItem.HostilityEndTime = reader.ReadDouble();
			return modelFactionRelationDataItem;
		}

		private void ReadAllFactionOpinions(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadInt32();
				int num3 = reader.ReadInt32();
				float opinion = reader.ReadSingle();
				double createdTime = reader.ReadUInt32();
				ModelFaction valueOrDefault = factionsById.GetValueOrDefault(num2);
				ModelFaction valueOrDefault2 = factionsById.GetValueOrDefault(num3);
				if (valueOrDefault != null)
				{
					if (valueOrDefault2 == valueOrDefault)
					{
						Logging.Warning($"Faction opinion for faction {num2} is referencing itself. Item will not be loaded.");
						continue;
					}
					if (valueOrDefault.Opinions == null)
					{
						valueOrDefault.Opinions = new ModelFactionOpinionData();
					}
					if (valueOrDefault2 != null)
					{
						valueOrDefault.Opinions.Items.Add(new ModelFactionOpinionDataItem
						{
							OtherFaction = valueOrDefault2,
							Opinion = opinion,
							CreatedTime = createdTime
						});
					}
					else
					{
						Logging.UnknownFactionMessage(num3, "loading faction opinions");
					}
				}
				else
				{
					Logging.UnknownFactionMessage(num2, "loading faction opinions");
				}
			}
		}

		private ModelSector ReadSector(BinaryReader reader)
		{
			return new ModelSector
			{
				Id = reader.ReadInt32(),
				Name = reader.ReadString(),
				MapPosition = reader.ReadVec3(),
				Description = reader.ReadString(),
				GateDistanceMultiplier = reader.ReadSingle(),
				RandomSeed = reader.ReadInt32(),
				BackgroundRotation = reader.ReadVec3(),
				AmbientLightColor = reader.ReadVec3(),
				DirectionLightColor = reader.ReadVec3(),
				DirectionLightRotation = reader.ReadVec3(),
				LastTimeChangedControl = reader.ReadDouble(),
				LightDirectionFudge = reader.ReadSingle()
			};
		}

		private ModelFaction ReadFaction(BinaryReader reader)
		{
			ModelFaction modelFaction = new ModelFaction();
			modelFaction.Id = reader.ReadInt32();
			modelFaction.GeneratedNameId = reader.ReadInt32();
			modelFaction.GeneratedSuffixId = reader.ReadInt32();
			modelFaction.CustomName = reader.ReadString();
			modelFaction.CustomShortName = reader.ReadString();
			modelFaction.HomeSector = reader.ReadSector(sectorsById);
			modelFaction.HomeSectorPosition = reader.ReadNullableVec3();
			modelFaction.Credits = reader.ReadInt32();
			modelFaction.Description = reader.ReadString();
			modelFaction.IsCivilian = reader.ReadBoolean();
			modelFaction.FactionType = (FactionType)reader.ReadInt32();
			modelFaction.Aggression = reader.ReadSingle();
			modelFaction.Virtue = reader.ReadSingle();
			modelFaction.Greed = reader.ReadSingle();
			modelFaction.Cooperation = reader.ReadSingle();
			modelFaction.TradeEfficiency = reader.ReadSingle();
			modelFaction.DynamicRelations = reader.ReadBoolean();
			modelFaction.ShowJobBoards = reader.ReadBoolean();
			modelFaction.CreateJobs = reader.ReadBoolean();
			modelFaction.RequisitionPointMultiplier = reader.ReadSingle();
			modelFaction.DestroyWhenNoUnits = reader.ReadBoolean();
			modelFaction.MinNpcCombatEfficiency = reader.ReadSingle();
			modelFaction.MaxNpcCombatEfficiency = reader.ReadSingle();
			modelFaction.AdditionalRpProvision = reader.ReadInt32();
			modelFaction.TradeIllegalGoods = reader.ReadBoolean();
			modelFaction.SpawnTime = reader.ReadDouble();
			modelFaction.HighestEverNetWorth = reader.ReadInt64();
			modelFaction.RankingSystemId = reader.ReadInt32();
			modelFaction.PreferredFormationId = reader.ReadInt32();
			if (reader.ReadBoolean())
			{
				modelFaction.CustomSettings = ReadFactionCustomSettings(reader);
			}
			if (reader.ReadBoolean())
			{
				modelFaction.Stats = ReadFactionStats(reader);
			}
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int key = reader.ReadInt32();
				ModelSector valueOrDefault = sectorsById.GetValueOrDefault(key);
				modelFaction.AutopilotExcludedSectors.Add(valueOrDefault);
			}
			return modelFaction;
		}

		private ModelFactionCustomSettings ReadFactionCustomSettings(BinaryReader reader)
		{
			return new ModelFactionCustomSettings
			{
				BuildShips = reader.ReadBoolean(),
				RepairShips = reader.ReadBoolean(),
				UpgradeShips = reader.ReadBoolean(),
				RepairMinHullDamage = reader.ReadSingle(),
				RepairMinCreditsBeforeRepair = reader.ReadInt32(),
				PreferenceToPlaceBounty = reader.ReadSingle(),
				LargeShipPreference = reader.ReadSingle(),
				CloakShipPreference = reader.ReadSingle(),
				DailyIncome = reader.ReadInt32(),
				HostileWithAll = reader.ReadBoolean(),
				MinFleetUnitCount = reader.ReadInt32(),
				MaxFleetUnitCount = reader.ReadInt32(),
				OffensiveStance = reader.ReadSingle(),
				AllowOtherFactionToUseDocks = reader.ReadBoolean(),
				PreferenceToBuildTurrets = reader.ReadSingle(),
				PreferenceToBuildStations = reader.ReadSingle(),
				PreferenceToHaveAmmo = reader.ReadSingle(),
				IgnoreStationCreditsReserve = reader.ReadBoolean(),
				MaxJumpDistanceFromHomeSector = reader.ReadInt32(),
				MaxStationBuildDistanceFromHomeSector = reader.ReadInt32(),
				PilotGender = (GenderChoice)reader.ReadInt32(),
				FixedShipCount = reader.ReadInt32(),
				SectorControlLikelihood = reader.ReadSingle()
			};
		}

		private ModelFactionStats ReadFactionStats(BinaryReader reader)
		{
			return new ModelFactionStats
			{
				TotalShipsClaimed = reader.ReadInt32(),
				UnitsDestroyedByClassId = ReadFactionStatsUnitCounts(reader),
				UnitLostByClassId = ReadFactionStatsUnitCounts(reader),
				ScratchcardsScratched = reader.ReadInt32(),
				HighestScratchcardWin = reader.ReadInt32()
			};
		}

		private Dictionary<ModelUnitClass, int> ReadFactionStatsUnitCounts(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			Dictionary<ModelUnitClass, int> dictionary = new Dictionary<ModelUnitClass, int>();
			for (int i = 0; i < num; i++)
			{
				dictionary.Add((ModelUnitClass)reader.ReadInt32(), reader.ReadInt32());
			}
			return dictionary;
		}

		private ModelSectorPatrolPath ReadPatrolPath(BinaryReader reader)
		{
			ModelSectorPatrolPath modelSectorPatrolPath = new ModelSectorPatrolPath();
			modelSectorPatrolPath.Id = reader.ReadInt32();
			modelSectorPatrolPath.Sector = reader.ReadSector(sectorsById);
			modelSectorPatrolPath.IsLoop = reader.ReadBoolean();
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				modelSectorPatrolPath.Nodes.Add(new ModelSectorPatrolPathNode
				{
					SectorPosition = reader.ReadVec3(),
					Order = reader.ReadInt32()
				});
			}
			return modelSectorPatrolPath;
		}

		public ModelMission ReadMission(BinaryReader reader, Dictionary<int, ModelSector> sectors, Dictionary<int, ModelFaction> factions, Dictionary<int, ModelUnit> units, Dictionary<int, ModelFleet> fleets)
		{
			ModelMission modelMission = CreateMissionFromMissionType.Create((MissionType)reader.ReadInt32());
			modelMission.Id = reader.ReadInt32();
			modelMission.Title = reader.ReadString();
			modelMission.IsPrimary = reader.ReadBoolean();
			modelMission.NotificationsEnabled = reader.ReadBoolean();
			modelMission.IsActive = reader.ReadBoolean();
			modelMission.StageIndex = reader.ReadInt32();
			modelMission.IsFinished = reader.ReadBoolean();
			modelMission.CompletionSuccess = reader.ReadBoolean();
			modelMission.ShowInJournal = reader.ReadBoolean();
			modelMission.OwnerFaction = reader.ReadFaction(factions);
			modelMission.MissionGiverFaction = reader.ReadFaction(factions);
			modelMission.CompletionOpinionChange = reader.ReadSingle();
			modelMission.FailureOpinionChange = reader.ReadSingle();
			modelMission.StartTime = reader.ReadDouble();
			modelMission.RewardCredits = reader.ReadInt32();
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelMissionStage modelMissionStage = ReadMissionStage(reader);
				modelMission.Stages.Add(modelMissionStage);
				modelMissionStage.Mission = modelMission;
			}
			int num2 = reader.ReadInt32();
			for (int j = 0; j < num2; j++)
			{
				ModelMissionObjective modelMissionObjective = ReadMissionObjective(reader);
				modelMission.Objectives.Add(modelMissionObjective);
				missionObjectivesById.Add(modelMissionObjective.Id, modelMissionObjective);
			}
			switch (modelMission.MissionType)
			{
			case MissionType.Courier:
			{
				ModelCourierMission modelCourierMission = (ModelCourierMission)modelMission;
				modelCourierMission.PickupUnit = reader.ReadUnit(units);
				modelCourierMission.DestinationUnit = reader.ReadUnit(units);
				modelCourierMission.CargoItem = new ModelComponentUnitCargoDataItem
				{
					CargoClass = (ModelCargoClass)reader.ReadInt32(),
					Quantity = reader.ReadInt32()
				};
				modelCourierMission.HasPlayerPickedUpCargo = reader.ReadBoolean();
				break;
			}
			case MissionType.DestroyGroup:
			{
				ModelDestroyUnitsMission modelDestroyUnitsMission = (ModelDestroyUnitsMission)modelMission;
				int num3 = reader.ReadInt32();
				for (int k = 0; k < num3; k++)
				{
					modelDestroyUnitsMission.TargetUnits.Add(reader.ReadUnit(units));
				}
				modelDestroyUnitsMission.HasSetGroupHostileToPlayer = reader.ReadBoolean();
				modelDestroyUnitsMission.TargetFaction = reader.ReadFaction(factions);
				modelDestroyUnitsMission.TargetSector = reader.ReadSector(sectors);
				modelDestroyUnitsMission.TargetFleet = reader.ReadFleet(fleets);
				break;
			}
			case MissionType.DeliverShip:
			{
				ModelDeliverShipMission modelDeliverShipMission = (ModelDeliverShipMission)modelMission;
				modelDeliverShipMission.UnitClass = (ModelUnitClass)reader.ReadInt32();
				modelDeliverShipMission.DestinationUnit = reader.ReadUnit(units);
				break;
			}
			case MissionType.Breakdown:
			{
				ModelBreakdownMission modelBreakdownMission = (ModelBreakdownMission)modelMission;
				modelBreakdownMission.BaseUnit = reader.ReadUnit(units);
				modelBreakdownMission.BreakdownUnit = reader.ReadUnit(units);
				break;
			}
			}
			return modelMission;
		}

		private static ModelMissionObjective ReadMissionObjective(BinaryReader reader)
		{
			return new ModelMissionObjective
			{
				Id = reader.ReadInt32(),
				Title = reader.ReadString(),
				Description = reader.ReadString(),
				IsOptional = reader.ReadBoolean(),
				Order = reader.ReadInt32(),
				IsActive = reader.ReadBoolean(),
				IsComplete = reader.ReadBoolean(),
				Success = reader.ReadBoolean(),
				ShowInJournal = reader.ReadBoolean()
			};
		}

		private static ModelMissionStage ReadMissionStage(BinaryReader reader)
		{
			return new ModelMissionStage
			{
				CompletesMission = reader.ReadBoolean(),
				JournalEntry = reader.ReadString(),
				MissionSuccess = reader.ReadBoolean()
			};
		}

		private static JobType GetJobTypeFromJobDataId(JobDataIds jobDataId)
		{
			return jobDataId switch
			{
				JobDataIds.None => JobType.DeliverShip, 
				JobDataIds.Courier => JobType.Courier, 
				JobDataIds.DestroyFleet => JobType.DestroyGroup, 
				JobDataIds.Breakdown => JobType.Breakdown, 
				_ => throw new NotImplementedException($"Unknown job data id {(int)jobDataId}"), 
			};
		}

		public static ModelFleetOrderCollection ReadOrders(BinaryReader reader, int fleetId, Dictionary<int, ModelFaction> factions, Dictionary<int, ModelSector> sectors, Dictionary<int, ModelUnit> units, Dictionary<int, ModelFleet> fleets, Dictionary<int, ModelSectorPatrolPath> patrolPaths, Dictionary<int, ModelPerson> people, Dictionary<int, ModelPassengerGroup> passengerGroups)
		{
			ModelFleetOrderCollection modelFleetOrderCollection = new ModelFleetOrderCollection();
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ModelFleetOrder item = ReadOrder(reader, factions, sectors, units, fleets, patrolPaths);
				modelFleetOrderCollection.Orders.Add(item);
			}
			int num2 = reader.ReadInt32();
			for (int j = 0; j < num2; j++)
			{
				int num3 = reader.ReadInt32();
				if (num3 < 0 || num3 >= modelFleetOrderCollection.Orders.Count)
				{
					Logging.Warning($"Fleet {fleetId} contains an invalid order index {num3}");
				}
				else
				{
					modelFleetOrderCollection.QueuedOrders.Add(modelFleetOrderCollection.Orders[num3]);
				}
			}
			if (reader.ReadBoolean())
			{
				int num4 = reader.ReadInt32();
				if (num4 < 0 || num4 >= modelFleetOrderCollection.Orders.Count)
				{
					throw new Exception($"Fleet {fleetId} contains an invalid order index {num4}");
				}
				ModelFleetOrder fleetOrder = modelFleetOrderCollection.Orders[num4];
				ModelActiveFleetOrder currentOrder = ReadActiveOrder(reader, fleetOrder, fleetId, factions, sectors, units, fleets, patrolPaths, people, passengerGroups);
				modelFleetOrderCollection.CurrentOrder = currentOrder;
			}
			return modelFleetOrderCollection;
		}

		public static ModelActiveFleetOrder ReadActiveOrder(BinaryReader reader, ModelFleetOrder fleetOrder, int fleetId, Dictionary<int, ModelFaction> factions, Dictionary<int, ModelSector> sectors, Dictionary<int, ModelUnit> units, Dictionary<int, ModelFleet> fleets, Dictionary<int, ModelSectorPatrolPath> patrolPaths, Dictionary<int, ModelPerson> people, Dictionary<int, ModelPassengerGroup> passengerGroups)
		{
			FleetOrderType orderType = fleetOrder.OrderType;
			ModelActiveFleetOrder modelActiveFleetOrder = ActiveFleetOrderReader.Read(reader, orderType, fleetId, factions, sectors, units, fleets, patrolPaths, people, passengerGroups);
			modelActiveFleetOrder.Order = fleetOrder;
			return modelActiveFleetOrder;
		}

		public static ModelFleetOrder ReadOrder(BinaryReader reader, Dictionary<int, ModelFaction> factions, Dictionary<int, ModelSector> sectors, Dictionary<int, ModelUnit> units, Dictionary<int, ModelFleet> fleets, Dictionary<int, ModelSectorPatrolPath> patrolPaths)
		{
			ModelFleetOrder modelFleetOrder = CreateFleetOrderFromType.Create((FleetOrderType)reader.ReadInt32());
			modelFleetOrder.Id = reader.ReadInt32();
			modelFleetOrder.CompletionMode = (FleetOrderCompletionMode)reader.ReadInt32();
			modelFleetOrder.AllowCombatInterception = reader.ReadBoolean();
			modelFleetOrder.CloakPreference = (FleetOrderCloakPreference)reader.ReadInt32();
			modelFleetOrder.MaxJumpDistance = reader.ReadInt32();
			modelFleetOrder.AllowTimeout = reader.ReadBoolean();
			modelFleetOrder.TimeoutTime = reader.ReadSingle();
			modelFleetOrder.MaxDuration = reader.ReadSingle();
			modelFleetOrder.Priority = reader.ReadSingle();
			modelFleetOrder.Notifications = reader.ReadBoolean();
			switch (modelFleetOrder.OrderType)
			{
			case FleetOrderType.AttackGroup:
			{
				ModelAttackFleetOrder modelAttackFleetOrder = (ModelAttackFleetOrder)modelFleetOrder;
				int key8 = reader.ReadInt32();
				modelAttackFleetOrder.Target = fleets.GetValueOrDefault(key8);
				modelAttackFleetOrder.AttackPriority = reader.ReadSingle();
				break;
			}
			case FleetOrderType.CollectCargo:
				((ModelCollectCargoOrder)modelFleetOrder).TargetUnit = reader.ReadUnit(units);
				break;
			case FleetOrderType.Scavenge:
			{
				ModelScavengeOrder modelScavengeOrder = (ModelScavengeOrder)modelFleetOrder;
				modelScavengeOrder.TargetSector = reader.ReadSector(sectors);
				modelScavengeOrder.CollectOwnerMode = (CollectCargoOwnerMode)reader.ReadInt32();
				break;
			}
			case FleetOrderType.Mine:
			{
				ModelMineOrder modelMineOrder = (ModelMineOrder)modelFleetOrder;
				modelMineOrder.TargetSector = reader.ReadSector(sectors);
				modelMineOrder.CollectOwnerMode = (CollectCargoOwnerMode)reader.ReadInt32();
				int key7 = reader.ReadInt32();
				ModelUnit valueOrDefault2 = units.GetValueOrDefault(key7);
				if (valueOrDefault2 != null)
				{
					modelMineOrder.ManualMineTarget = valueOrDefault2;
				}
				break;
			}
			case FleetOrderType.Dock:
			{
				ModelDockOrder modelDockOrder = (ModelDockOrder)modelFleetOrder;
				int key5 = reader.ReadInt32();
				modelDockOrder.TargetDock = units.GetValueOrDefault(key5);
				break;
			}
			case FleetOrderType.Patrol:
			{
				ModelPatrolOrder modelPatrolOrder = (ModelPatrolOrder)modelFleetOrder;
				modelPatrolOrder.PathDirection = reader.ReadInt32();
				modelPatrolOrder.IsLooping = reader.ReadBoolean();
				int num2 = reader.ReadInt32();
				for (int j = 0; j < num2; j++)
				{
					ModelPatrolPathNode modelPatrolPathNode = new ModelPatrolPathNode();
					int key4 = reader.ReadInt32();
					modelPatrolPathNode.Sector = sectors.GetValueOrDefault(key4);
					modelPatrolPathNode.SectorPosition = reader.ReadVec3();
					modelPatrolOrder.Nodes.Add(modelPatrolPathNode);
				}
				modelPatrolOrder.IsLoop = reader.ReadBoolean();
				break;
			}
			case FleetOrderType.PatrolPath:
			{
				ModelPatrolPathOrder modelPatrolPathOrder = (ModelPatrolPathOrder)modelFleetOrder;
				modelPatrolPathOrder.PathDirection = reader.ReadInt32();
				modelPatrolPathOrder.IsLooping = reader.ReadBoolean();
				int key10 = reader.ReadInt32();
				modelPatrolPathOrder.PatrolPath = patrolPaths.GetValueOrDefault(key10);
				break;
			}
			case FleetOrderType.Wait:
				((ModelWaitOrder)modelFleetOrder).WaitTime = reader.ReadSingle();
				break;
			case FleetOrderType.AttackTarget:
			{
				ModelAttackTargetOrder modelAttackTargetOrder = (ModelAttackTargetOrder)modelFleetOrder;
				int key9 = reader.ReadInt32();
				modelAttackTargetOrder.TargetUnit = units.GetValueOrDefault(key9);
				modelAttackTargetOrder.AttackPriority = reader.ReadSingle();
				break;
			}
			case FleetOrderType.Trade:
			{
				ModelTradeOrder modelTradeOrder = (ModelTradeOrder)modelFleetOrder;
				modelTradeOrder.MinBuyQuantity = reader.ReadInt32();
				modelTradeOrder.MinBuyCargoPercentage = reader.ReadSingle();
				break;
			}
			case FleetOrderType.ManualTrade:
			{
				ModelManualTradeOrder modelManualTradeOrder = (ModelManualTradeOrder)modelFleetOrder;
				modelManualTradeOrder.MinBuyQuantity = reader.ReadInt32();
				modelManualTradeOrder.MinBuyCargoPercentage = reader.ReadSingle();
				if (reader.ReadBoolean())
				{
					modelManualTradeOrder.CustomTradeRoute = CustomTradeRouteReader.Read(reader, units);
				}
				break;
			}
			case FleetOrderType.AutonomousTrade:
			{
				ModelUniverseTradeOrder modelUniverseTradeOrder = (ModelUniverseTradeOrder)modelFleetOrder;
				modelUniverseTradeOrder.MinBuyQuantity = reader.ReadInt32();
				modelUniverseTradeOrder.MinBuyCargoPercentage = reader.ReadSingle();
				modelUniverseTradeOrder.TradeOnlySpecificCargoClasses = reader.ReadBoolean();
				modelUniverseTradeOrder.TradeSpecificCargoClasses.Clear();
				int num3 = reader.ReadInt32();
				for (int k = 0; k < num3; k++)
				{
					modelUniverseTradeOrder.TradeSpecificCargoClasses.Add((ModelCargoClass)reader.ReadInt32());
				}
				break;
			}
			case FleetOrderType.JoinFleet:
			{
				ModelJoinFleetOrder modelJoinFleetOrder = (ModelJoinFleetOrder)modelFleetOrder;
				int key6 = reader.ReadInt32();
				ModelFleet valueOrDefault = fleets.GetValueOrDefault(key6);
				modelJoinFleetOrder.TargetFleet = valueOrDefault;
				break;
			}
			case FleetOrderType.MoveTo:
			{
				ModelMoveToOrder modelMoveToOrder = (ModelMoveToOrder)modelFleetOrder;
				modelMoveToOrder.CompleteOnReachTarget = reader.ReadBoolean();
				modelMoveToOrder.ArrivalThreshold = reader.ReadSingle();
				modelMoveToOrder.MatchTargetOrientation = reader.ReadBoolean();
				modelMoveToOrder.PreferredRelativeVectorFromTarget = reader.ReadNullableVec3();
				if (reader.ReadBoolean())
				{
					modelMoveToOrder.Target = SectorTargetReader.Read(reader, sectors, units, fleets);
				}
				break;
			}
			case FleetOrderType.Protect:
			{
				ModelProtectOrder modelProtectOrder = (ModelProtectOrder)modelFleetOrder;
				modelProtectOrder.CompleteOnReachTarget = reader.ReadBoolean();
				modelProtectOrder.ArrivalThreshold = reader.ReadSingle();
				modelProtectOrder.MatchTargetOrientation = reader.ReadBoolean();
				modelProtectOrder.PreferredRelativeVectorFromTarget = reader.ReadNullableVec3();
				if (reader.ReadBoolean())
				{
					modelProtectOrder.Target = SectorTargetReader.Read(reader, sectors, units, fleets);
				}
				break;
			}
			case FleetOrderType.SellCargo:
			{
				ModelSellCargoOrder modelSellCargoOrder = (ModelSellCargoOrder)modelFleetOrder;
				modelSellCargoOrder.FreeUnitsCompleteThreshold = reader.ReadInt32();
				modelSellCargoOrder.MinBuyPriceMultiplier = reader.ReadSingle();
				modelSellCargoOrder.SellOnlyListedCargos = reader.ReadBoolean();
				modelSellCargoOrder.CompleteWhenNoBuyerFound = reader.ReadBoolean();
				modelSellCargoOrder.CompleteWhenNoCargoToSell = reader.ReadBoolean();
				int key3 = reader.ReadInt32();
				modelSellCargoOrder.ManualBuyerUnit = units.GetValueOrDefault(key3);
				modelSellCargoOrder.CustomSellCargoTime = reader.ReadSingle();
				int num = reader.ReadInt32();
				for (int i = 0; i < num; i++)
				{
					int item = reader.ReadInt32();
					modelSellCargoOrder.SellCargoClasses.Add((ModelCargoClass)item);
				}
				modelSellCargoOrder.SellEquipment = reader.ReadBoolean();
				break;
			}
			case FleetOrderType.ManualRearm:
			{
				ModelManualRearmFleetOrder modelManualRearmFleetOrder = (ModelManualRearmFleetOrder)modelFleetOrder;
				modelManualRearmFleetOrder.EquipmentCargoUsage = reader.ReadSingle();
				modelManualRearmFleetOrder.InsufficientCreditsMode = (InsufficientCreditsMode)reader.ReadInt32();
				int key2 = reader.ReadInt32();
				modelManualRearmFleetOrder.RearmLocationUnit = units.GetValueOrDefault(key2);
				break;
			}
			case FleetOrderType.RearmAtNearest:
			{
				ModelRearmAtNearestFleetOrder modelRearmAtNearestFleetOrder = (ModelRearmAtNearestFleetOrder)modelFleetOrder;
				modelRearmAtNearestFleetOrder.EquipmentCargoUsage = reader.ReadSingle();
				modelRearmAtNearestFleetOrder.InsufficientCreditsMode = (InsufficientCreditsMode)reader.ReadInt32();
				break;
			}
			case FleetOrderType.ManualRepair:
			{
				ModelManualRepairFleetOrder modelManualRepairFleetOrder = (ModelManualRepairFleetOrder)modelFleetOrder;
				modelManualRepairFleetOrder.InsufficientCreditsMode = (InsufficientCreditsMode)reader.ReadInt32();
				int key = reader.ReadInt32();
				modelManualRepairFleetOrder.RepairLocationUnit = units.GetValueOrDefault(key);
				break;
			}
			case FleetOrderType.RepairAtNearest:
				((ModelRepairAtNearestStationOrder)modelFleetOrder).InsufficientCreditsMode = (InsufficientCreditsMode)reader.ReadInt32();
				break;
			case FleetOrderType.MoveToNearestFriendlyStation:
				((ModelMoveToNearestFriendlyStationOrder)modelFleetOrder).CompleteOnReachTarget = reader.ReadBoolean();
				break;
			case FleetOrderType.EnterWormhole:
				((ModelEnterWormholeOrder)modelFleetOrder).TargetWormhole = reader.ReadUnit(units);
				break;
			case FleetOrderType.ExploreSector:
				((ModelExploreSectorOrder)modelFleetOrder).Sector = reader.ReadSector(sectors);
				break;
			case FleetOrderType.MoveToSector:
				((ModelMoveToSectorOrder)modelFleetOrder).TargetSector = reader.ReadSector(sectors);
				break;
			case FleetOrderType.WaitForAutoRepair:
			{
				ModelWaitForAutoRepairOrder modelWaitForAutoRepairOrder = (ModelWaitForAutoRepairOrder)modelFleetOrder;
				modelWaitForAutoRepairOrder.HullConditionThreshold = reader.ReadSingle();
				modelWaitForAutoRepairOrder.ComponentsConditionThreshold = reader.ReadSingle();
				modelWaitForAutoRepairOrder.ShieldConditionThreshold = reader.ReadSingle();
				break;
			}
			case FleetOrderType.BuildStation:
			{
				ModelBuildStationOrder modelBuildStationOrder = (ModelBuildStationOrder)modelFleetOrder;
				modelBuildStationOrder.UnitClass = (ModelUnitClass)reader.ReadInt32();
				modelBuildStationOrder.Sector = reader.ReadSector(sectors);
				modelBuildStationOrder.SectorPosition = reader.ReadVec3();
				modelBuildStationOrder.InsufficientCreditsMode = (InsufficientCreditsMode)reader.ReadInt32();
				break;
			}
			case FleetOrderType.ClaimUnit:
				((ModelClaimUnitOrder)modelFleetOrder).Unit = reader.ReadUnit(units);
				break;
			default:
				throw new Exception($"Unable to read data for objective of type {modelFleetOrder.OrderType}. Unknown type");
			case FleetOrderType.RTB:
			case FleetOrderType.DisposeCargo:
			case FleetOrderType.AutonomousTransportPassengers:
			case FleetOrderType.AutonomousRoamLocationsObjective:
			case FleetOrderType.AutonomousBountyHunterObjective:
			case FleetOrderType.Explore:
			case FleetOrderType.Undock:
				break;
			}
			return modelFleetOrder;
		}

		public static ModelFleetSpawner ReadFleetSpawner(BinaryReader reader, Dictionary<int, ModelFaction> factions, Dictionary<int, ModelSector> sectors, Dictionary<int, ModelUnit> units, Dictionary<int, ModelFleet> fleets, Dictionary<int, ModelPerson> people, Dictionary<int, ModelSectorPatrolPath> patrolPaths)
		{
			ModelFleetSpawner modelFleetSpawner = new ModelFleetSpawner();
			modelFleetSpawner.Name = reader.ReadString();
			modelFleetSpawner.Position = reader.ReadVec3();
			modelFleetSpawner.Rotation = reader.ReadVec4();
			modelFleetSpawner.InitialSpawnTimeRandomness = reader.ReadSingle();
			modelFleetSpawner.SpawnTimeRandomness = reader.ReadSingle();
			modelFleetSpawner.ShipDesignation = reader.ReadString();
			modelFleetSpawner.ShipName = reader.ReadString();
			modelFleetSpawner.NamePrefix = reader.ReadString();
			modelFleetSpawner.SpawnCounter = reader.ReadInt32();
			modelFleetSpawner.RespawnWhenNoObjectives = reader.ReadBoolean();
			modelFleetSpawner.RespawnWhenNoPilots = reader.ReadBoolean();
			modelFleetSpawner.AllowRespawnInActiveScene = reader.ReadBoolean();
			modelFleetSpawner.FleetHomeBase = reader.ReadUnit(units);
			modelFleetSpawner.FleetHomeSector = reader.ReadSector(sectors);
			modelFleetSpawner.OwnerFaction = reader.ReadFaction(factions);
			modelFleetSpawner.Sector = reader.ReadSector(sectors);
			modelFleetSpawner.SpawnDock = reader.ReadUnit(units);
			modelFleetSpawner.NextSpawnTime = reader.ReadDouble();
			modelFleetSpawner.MinTimeBeforeSpawn = reader.ReadSingle();
			modelFleetSpawner.MaxTimeBeforeSpawn = reader.ReadSingle();
			modelFleetSpawner.MinGroupUnitCount = reader.ReadInt32();
			modelFleetSpawner.MaxGroupUnitCount = reader.ReadInt32();
			modelFleetSpawner.SpawnedFleet = reader.ReadFleet(fleets);
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				modelFleetSpawner.UnitClasses.Add((ModelUnitClass)reader.ReadInt32());
			}
			int num2 = reader.ReadInt32();
			for (int j = 0; j < num2; j++)
			{
				modelFleetSpawner.PilotResourceNames.Add(reader.ReadString());
			}
			modelFleetSpawner.FleetResourceName = reader.ReadString();
			int num3 = reader.ReadInt32();
			for (int k = 0; k < num3; k++)
			{
				ModelFleetOrder item = ReadOrder(reader, factions, sectors, units, fleets, patrolPaths);
				modelFleetSpawner.Orders.Add(item);
			}
			return modelFleetSpawner;
		}
	}
}
