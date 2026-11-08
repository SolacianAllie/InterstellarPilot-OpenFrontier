using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.SavedGames.V2.BinarySerialization.Writers.Helpers;
using OpenFrontier.IP.SavedGames.V2.Model;
using OpenFrontier.IP.SavedGames.V2.Model.Actions;
using OpenFrontier.IP.SavedGames.V2.Model.Factions;
using OpenFrontier.IP.SavedGames.V2.Model.Factions.Bounty;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders;
using OpenFrontier.IP.SavedGames.V2.Model.Helpers;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs;
using OpenFrontier.IP.SavedGames.V2.Model.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Writers
{
	public class SaveGameWriter : ISaveGameWriter
	{
		private readonly HeaderWriter headerWriter;

		public SaveGameWriter(HeaderWriter headerWriter)
		{
			this.headerWriter = headerWriter;
		}

		public static void WriteToPath(SavedGame savedGame, string path)
		{
			using BinaryWriter writer = new BinaryWriter(File.OpenWrite(path));
			new SaveGameWriter(new HeaderWriter()).Write(writer, savedGame);
		}

		public void Write(BinaryWriter writer, ISavedGame savedGame, Action<string> logger = null)
		{
			SavedGame savedGame2 = (SavedGame)savedGame;
			headerWriter.Write(writer, savedGame2.Header);
			PrintStatus("Saved header", writer, logger);
			writer.Write("[Sectors]");
			WriteSectors(writer, savedGame2.Sectors);
			PrintStatus("Saved sectors", writer, logger);
			writer.Write("[Factions]");
			WriteFactions(writer, savedGame2.Factions);
			PrintStatus("Saved factions", writer, logger);
			writer.Write("[Avatars]");
			WriteFactionAvatarProfiles(writer, savedGame2.Factions);
			PrintStatus("Saved faction avatar profiles", writer, logger);
			writer.Write("[PatrolPaths]");
			WritePatrolPaths(writer, savedGame2.PatrolPaths);
			PrintStatus("Saved patrol paths", writer, logger);
			writer.Write("[FactionRelations]");
			WriteAllFactionRelations(writer, savedGame2.Factions);
			PrintStatus("Saved faction relations", writer, logger);
			writer.Write("[FactionDamage]");
			WriteFactionRecentDamageReceived(writer, savedGame2.Factions);
			PrintStatus("Saved faction recent damage", writer, logger);
			writer.Write("[FactionOpinions]");
			WriteAllFactionOpinions(writer, savedGame2.Factions);
			PrintStatus("Saved faction opinions", writer, logger);
			writer.Write("[Units]");
			WriteUnits(writer, savedGame2.Units);
			PrintStatus("Saved units", writer, logger);
			writer.Write("[UnitRadius]");
			WriteUnitRadii(writer, savedGame2.Units);
			PrintStatus("Saved unit radii", writer, logger);
			writer.Write("[UnitsNamed]");
			WriteNamedUnits(writer, savedGame2.Units);
			PrintStatus("Saved named units", writer, logger);
			writer.Write("[UnitComponents]");
			WriteAllComponentUnits(writer, savedGame2.Units);
			PrintStatus("Saved all unit components", writer, logger);
			writer.Write("[UnitsConstructing]");
			WriteUnitsUnderConstruction(writer, savedGame2.Units);
			PrintStatus("Saved units under construction", writer, logger);
			writer.Write("[UnitDamageCounter]");
			WriteUnitTotalDamageReceived(writer, savedGame2.Units);
			PrintStatus("Saved unit damage received", writer, logger);
			writer.Write("[UnitComponnentMod]");
			WriteModdedComponents(writer, savedGame2.Units);
			PrintStatus("Saved modded components", writer, logger);
			writer.Write("[CapacitorCharge]");
			WriteUnitCapacitorCharges(writer, savedGame2.Units);
			PrintStatus("Saved unit capacitor charges", writer, logger);
			writer.Write("[CloakState]");
			WriteCloakedUnits(writer, savedGame2.Units);
			PrintStatus("Saved unit cloak states", writer, logger);
			writer.Write("[UnitsPoweredDown]");
			WritePoweredDownComponents(writer, savedGame2.Units);
			PrintStatus("Saved powered down units", writer, logger);
			writer.Write("[EngineThrottle]");
			WriteUnitEngineThrottles(writer, savedGame2.Units);
			PrintStatus("Saved engine throttle data", writer, logger);
			writer.Write("[Cargo]");
			WriteComponentUnitCargo(writer, savedGame2.Units);
			PrintStatus("Saved cargo", writer, logger);
			writer.Write("[ShieldHealth]");
			WriteAllShieldHealthData(writer, savedGame2.Units);
			PrintStatus("Saved damaged shields", writer, logger);
			writer.Write("[ComponentHealth]");
			WriteAllUnitComponentHealthData(writer, savedGame2.Units);
			PrintStatus("Saved damaged components", writer, logger);
			writer.Write("[ActiveUnits]");
			WriteActiveUnits(writer, savedGame2.Units);
			PrintStatus("Saved active units", writer, logger);
			writer.Write("[UnitHealth]");
			WriteAllUnitHealthDatas(writer, savedGame2.Units);
			PrintStatus("Saved destructable units", writer, logger);
			writer.Write("[FactionIntel]");
			WriteAllFactionIntel(writer, savedGame2.Factions);
			PrintStatus("Saved faction intel", writer, logger);
			writer.Write("[Passengers]");
			WritePassengerGroups(writer, savedGame2.Units);
			PrintStatus("Saved passenger groups", writer, logger);
			writer.Write("[Wormholes]");
			WriteWormholes(writer, savedGame2.Units);
			PrintStatus("Saved wormholes", writer, logger);
			writer.Write("[Hangars]");
			WriteHangars(writer, savedGame2.Units);
			PrintStatus("Saved hangers", writer, logger);
			writer.Write("[Fleets]");
			WriteFleets(writer, savedGame2.Fleets);
			PrintStatus("Saved fleets", writer, logger);
			writer.Write("[FleetsNamed]");
			WriteNamedFleets(writer, savedGame2.Fleets);
			writer.Write("[People]");
			WritePeople(writer, savedGame2.People, savedGame2.Player?.Person);
			PrintStatus("Saved people", writer, logger);
			writer.Write("[FleetOrders]");
			WriteFleetOrders(writer, savedGame2.Fleets);
			PrintStatus("Saved fleet orders", writer, logger);
			writer.Write("[NpcPilots]");
			WriteNpcPilots(writer, savedGame2.People);
			PrintStatus("Saved NPC pilots", writer, logger);
			writer.Write("[FactionLeaders]");
			WriteFactionLeaders(writer, savedGame2.Factions);
			PrintStatus("Saved faction leaders", writer, logger);
			writer.Write("[Jobs]");
			WriteJobs(writer, savedGame2.Units);
			PrintStatus("Saved jobs", writer, logger);
			writer.Write("[Bounty]");
			WriteAllFactionAIsAndBountyBoards(writer, savedGame2.Factions);
			PrintStatus("Saved faction AIs / bounty boards", writer, logger);
			writer.Write("[FactionAIExcludes]");
			WriteFactionAIExcludedUnits(writer, savedGame2.Factions);
			PrintStatus("Saved faction excluded unit data", writer, logger);
			writer.Write("[Mercenaries]");
			WriteFactionMercenaryData(writer, savedGame2.Factions);
			PrintStatus("Saved mercenary data", writer, logger);
			writer.Write("[FleetSpawners]");
			WriteFleetSpawners(writer, savedGame2.FleetSpawners);
			PrintStatus("Saved NPC fleet spawners", writer, logger);
			writer.Write("[Missions]");
			WriteMissions(writer, savedGame2.Missions);
			PrintStatus("Saved missions", writer, logger);
			writer.Write("[Player]");
			writer.Write(savedGame2.Player != null);
			if (savedGame2.Player != null)
			{
				WriteGamePlayer(writer, savedGame2.Player);
				PrintStatus("Saved player data", writer, logger);
			}
			writer.Write("[HUD]");
			writer.WriteUnitId(savedGame2.CurrentHudTarget);
			PrintStatus("Saved hud data", writer, logger);
			writer.Write("[FactionTransactions]");
			WriteAllFactionTransactions(writer, savedGame2.Factions);
			PrintStatus("Saved all faction transactions", writer, logger);
			writer.Write("[Scenario]");
			WriteScenarioData(writer, savedGame2.ScenarioData);
			PrintStatus("Saved world", writer, logger);
			writer.Write("[Moons]");
			WriteMoons(writer, savedGame2.Moons);
			PrintStatus("Saved moons", writer, logger);
			writer.Write("[SeedOptions]");
			WriteSeedOptions(writer, savedGame2.SeedOptions);
			PrintStatus("Saved seed options", writer, logger);
			writer.Write("[AutoTurrets]");
			WriteAutoTurretModuleData(writer, savedGame2.Units);
			PrintStatus("Saved auto-turret module data", writer, logger);
			writer.Write("[ComponentAutoFire]");
			WriteAutoFireComponents(writer, savedGame2.Units);
			PrintStatus("Saved auto-fire component data", writer, logger);
			writer.Write("[UnitCapture]");
			WriteUnitCaptureCooldownTimes(writer, savedGame2.Units);
			PrintStatus("Saved unit capture cooldown times", writer, logger);
			writer.Write("[PlayerFleetSettings]");
			WritePlayerFleetSettings(writer, savedGame2.Fleets);
			PrintStatus("Saved player fleet settings", writer, logger);
			writer.Write("[SectorAppearance]");
			WriteCustomSectorAppearances(writer, savedGame2.Sectors);
			PrintStatus("Saved custom sector appearance", writer, logger);
			writer.Write("[UnitMass]");
			WriteUnitMass(writer, savedGame2.Units);
			PrintStatus("Saved unit mass", writer, logger);
			writer.Write("[UnitCargoCapacity]");
			WriteUnitCargoCapacity(writer, savedGame2.Units);
			PrintStatus("Saved unit cargo capacity", writer, logger);
			writer.Write("[UnitScanRange]");
			WriteUnitScanRange(writer, savedGame2.Units);
			PrintStatus("Saved unit scan range", writer, logger);
			writer.Write("[UnitInvulnerable]");
			WriteInvulnerableUnits(writer, savedGame2.Units);
			PrintStatus("Saved invulnerable units", writer, logger);
			writer.Write("[UnitNoDestruction]");
			WriteNoDestructionUnits(writer, savedGame2.Units);
			PrintStatus("Saved units no destruction", writer, logger);
			writer.Write("[FleetOrderAvailableCredits]");
			WriteFleetOrderAvailableCredits(writer, savedGame2.Fleets);
			PrintStatus("Saved fleet order available credits", writer, logger);
			writer.Write("[PersonCustomTitles]");
			WritePersonCustomTitles(writer, savedGame2.People);
			PrintStatus("Saved person custom titles", writer, logger);
			writer.Write("[TriggerGroups]");
			WriteTriggerGroups(writer, savedGame2.TriggerGroups);
			PrintStatus("Saved trigger groups", writer, logger);
			writer.Write("[DitchedUnits]");
			WriteDitchedUnits(writer, savedGame2.DitchedUnitsToBeCleanedUp);
			PrintStatus("Saved ditched units", writer, logger);
			writer.Write("[EngineData]");
			WriteEngineData(writer, savedGame2.EngineData);
			PrintStatus("Saved engine data", writer, logger);
			writer.Write("[PlayerUnitFleetSettings]");
			WritePlayerUnitFleetSettings(writer, savedGame2.PlayerUnitFleetSettingItems);
			PrintStatus("Saved player unit fleet settings", writer, logger);
			writer.Write("[PlayerDefaultFleetSettings]");
			WritePlayerDefaultFleetSettings(writer, savedGame2.PlayerDefaultFleetSettings);
			PrintStatus("Saved player default fleet settings", writer, logger);
			writer.Write("[UnitTractorerInfo]");
			WriteUnitTractorerInfo(writer, savedGame2.TractorerDataItems);
			PrintStatus("Saved unit tractorer info", writer, logger);
			writer.Write("[CustomUnitClassNames]");
			WriteReadCustomUnitClasses(writer, savedGame2.Units);
			PrintStatus("Saved custom unit class names", writer, logger);
			writer.Write("[HappyModding_OpenFrontier2022]");
		}

		private void WriteReadCustomUnitClasses(BinaryWriter writer, IEnumerable<ModelUnit> units)
		{
			IEnumerable<ModelUnit> enumerable = units.Where((ModelUnit e) => !string.IsNullOrWhiteSpace(e.CustomClassName));
			writer.Write(enumerable.Count());
			foreach (ModelUnit item in enumerable)
			{
				writer.WriteUnitId(item);
				writer.WriteStringOrEmpty(item.CustomClassName);
			}
		}

		private void WriteUnitTractorerInfo(BinaryWriter writer, IEnumerable<ModelTractorerDataItem> tractorerDataItems)
		{
			writer.Write(tractorerDataItems.Count());
			foreach (ModelTractorerDataItem tractorerDataItem in tractorerDataItems)
			{
				writer.WriteUnitId(tractorerDataItem.TractoringUnit);
				writer.WriteUnitId(tractorerDataItem.TractoredUnit);
			}
		}

		private void WritePlayerDefaultFleetSettings(BinaryWriter writer, ModelPlayerFleetSettingsCombined modelPlayerFleetSettingsCombined)
		{
			writer.Write(modelPlayerFleetSettingsCombined != null);
			if (modelPlayerFleetSettingsCombined != null)
			{
				WritePlayerFleetSettingsCombined(writer, modelPlayerFleetSettingsCombined);
			}
		}

		private void WritePlayerUnitFleetSettings(BinaryWriter writer, List<ModelPlayerUnitFleetSettings> modelPlayerUnitFleetSettings)
		{
			writer.Write(modelPlayerUnitFleetSettings.Count);
			foreach (ModelPlayerUnitFleetSettings modelPlayerUnitFleetSetting in modelPlayerUnitFleetSettings)
			{
				WritePlayerUnitFleetSettingsItem(writer, modelPlayerUnitFleetSetting);
			}
		}

		private void WritePlayerUnitFleetSettingsItem(BinaryWriter writer, ModelPlayerUnitFleetSettings modelPlayerUnitFleetSettings)
		{
			writer.WriteUnitId(modelPlayerUnitFleetSettings.Unit);
			if (modelPlayerUnitFleetSettings.Settings == null)
			{
				modelPlayerUnitFleetSettings.Settings = new ModelPlayerFleetSettingsCombined();
			}
			WritePlayerFleetSettingsCombined(writer, modelPlayerUnitFleetSettings.Settings);
		}

		private void WritePlayerFleetSettingsCombined(BinaryWriter writer, ModelPlayerFleetSettingsCombined modelPlayerFleetSettingsCombined)
		{
			WriteSectorTarget(writer, modelPlayerFleetSettingsCombined.HomeBase);
			writer.Write(modelPlayerFleetSettingsCombined.FormationId);
			WriteFleetSettings(writer, modelPlayerFleetSettingsCombined.FleetSettings);
			if (modelPlayerFleetSettingsCombined.FleetSettings.PlayerFleetSettings == null)
			{
				modelPlayerFleetSettingsCombined.FleetSettings.PlayerFleetSettings = new ModelPlayerFleetSettings();
			}
			WritePlayerFleetSettings(writer, modelPlayerFleetSettingsCombined.FleetSettings.PlayerFleetSettings);
		}

		private void WriteDitchedUnits(BinaryWriter writer, List<ModelDitchedUnit> ditchedUnitsToBeCleanedUp)
		{
			writer.Write(ditchedUnitsToBeCleanedUp.Count);
			foreach (ModelDitchedUnit item in ditchedUnitsToBeCleanedUp)
			{
				writer.WriteUnitId(item.Unit);
				writer.Write(item.ExpiryTime);
			}
		}

		private void WriteEngineData(BinaryWriter writer, ModelEngineData modelEngineData)
		{
			writer.Write(modelEngineData.UnitIdCounter);
			writer.Write(modelEngineData.PlayerMessageIdCounter);
			writer.Write(modelEngineData.PersonIdCounter);
			writer.Write(modelEngineData.FactionIdCounter);
			writer.Write(modelEngineData.FleetOrderIdCounter);
			writer.Write(modelEngineData.PassengerGroupIdCounter);
			writer.Write(modelEngineData.JobIdCounter);
			writer.Write(modelEngineData.FleetIdCounter);
			writer.Write(modelEngineData.SectorIdCounter);
			writer.Write(modelEngineData.MissionIdCounter);
			writer.Write(modelEngineData.PatrolPathIdCounter);
			writer.Write(modelEngineData.MissionObjectiveIdCounter);
		}

		private void WriteTriggerGroups(BinaryWriter writer, List<ModelTriggerGroup> triggerGroups)
		{
			writer.Write(triggerGroups.Count);
			foreach (ModelTriggerGroup triggerGroup in triggerGroups)
			{
				WriteTriggerGroup(writer, triggerGroup);
			}
			writer.Write(triggerGroups.Count);
			foreach (ModelTriggerGroup triggerGroup2 in triggerGroups)
			{
				writer.Write(triggerGroup2.Id);
				writer.Write(triggerGroup2.Triggers.Count);
				foreach (ModelTrigger trigger in triggerGroup2.Triggers)
				{
					WriteTrigger(writer, trigger);
				}
				writer.Write(triggerGroup2.Actions.Count);
				foreach (ModelAction action in triggerGroup2.Actions)
				{
					WriteAction(writer, action);
				}
			}
		}

		private void WriteTriggerGroup(BinaryWriter writer, ModelTriggerGroup triggerGroup)
		{
			writer.Write(triggerGroup.Id);
			writer.Write(triggerGroup.IsActive);
			writer.Write(triggerGroup.FireAndDisable);
			writer.Write(triggerGroup.EvaluateFrequency);
			writer.Write(triggerGroup.NextEvaluationTime);
			writer.Write(triggerGroup.FireCount);
			writer.Write(triggerGroup.MaxFireCount);
			writer.Write((byte)triggerGroup.MaxFiredAction);
		}

		private void WriteTrigger(BinaryWriter writer, ModelTrigger trigger)
		{
			writer.Write((int)trigger.Type);
			writer.Write(trigger.Invert);
			switch (trigger.Type)
			{
			case TriggerType.Player_CurrentHudTarget:
			{
				ModelTrigger_Player_CurrentHudTarget modelTrigger_Player_CurrentHudTarget = (ModelTrigger_Player_CurrentHudTarget)trigger;
				writer.WriteUnitId(modelTrigger_Player_CurrentHudTarget.TargetUnit);
				break;
			}
			case TriggerType.Player_IsPilotting:
			{
				ModelTrigger_Player_IsPilotting modelTrigger_Player_IsPilotting = (ModelTrigger_Player_IsPilotting)trigger;
				writer.Write(modelTrigger_Player_IsPilotting.WaitForHud);
				break;
			}
			case TriggerType.Scenario_TimeElapsed:
			{
				ModelTrigger_Scenario_TimeElapsed modelTrigger_Scenario_TimeElapsed = (ModelTrigger_Scenario_TimeElapsed)trigger;
				writer.Write(modelTrigger_Scenario_TimeElapsed.Time);
				break;
			}
			}
		}

		private void WriteAction(BinaryWriter writer, ModelAction modelAction)
		{
			writer.Write((int)modelAction.Type);
			switch (modelAction.Type)
			{
			case ActionType.Player_NewMessageSimple:
			{
				ModelAction_Player_NewMessageSimple modelAction_Player_NewMessageSimple = (ModelAction_Player_NewMessageSimple)modelAction;
				writer.WriteStringOrEmpty(modelAction_Player_NewMessageSimple.From);
				writer.WriteStringOrEmpty(modelAction_Player_NewMessageSimple.To);
				writer.WriteStringOrEmpty(modelAction_Player_NewMessageSimple.Subject);
				writer.WriteStringOrEmpty(modelAction_Player_NewMessageSimple.Message);
				writer.Write(modelAction_Player_NewMessageSimple.Notifications);
				break;
			}
			case ActionType.Mission_Activate:
			{
				ModelAction_Mission_Activate modelAction_Mission_Activate = (ModelAction_Mission_Activate)modelAction;
				writer.WriteMissionId(modelAction_Mission_Activate.Mission);
				break;
			}
			case ActionType.Mission_ChangeStage:
			{
				ModelAction_Mission_ChangeStage modelAction_Mission_ChangeStage = (ModelAction_Mission_ChangeStage)modelAction;
				if (modelAction_Mission_ChangeStage.Stage != null && modelAction_Mission_ChangeStage.Stage.Mission != null)
				{
					writer.WriteMissionId(modelAction_Mission_ChangeStage.Stage.Mission);
					writer.Write(modelAction_Mission_ChangeStage.Stage.Mission.Stages.IndexOf(modelAction_Mission_ChangeStage.Stage));
				}
				else
				{
					writer.Write(-1);
					writer.Write(-1);
				}
				break;
			}
			case ActionType.Mission_ActivateObjective:
			{
				ModelAction_Mission_ActivateObjective modelAction_Mission_ActivateObjective = (ModelAction_Mission_ActivateObjective)modelAction;
				writer.Write(modelAction_Mission_ActivateObjective.Objectives.Count);
				{
					foreach (ModelMissionObjective objective in modelAction_Mission_ActivateObjective.Objectives)
					{
						writer.WriteMissionObjectiveId(objective);
					}
					break;
				}
			}
			case ActionType.Mission_CompleteObjective:
			{
				ModelAction_Mission_CompleteObjective modelAction_Mission_CompleteObjective = (ModelAction_Mission_CompleteObjective)modelAction;
				writer.WriteMissionObjectiveId(modelAction_Mission_CompleteObjective.MissionObjective);
				writer.Write(modelAction_Mission_CompleteObjective.Success);
				break;
			}
			case ActionType.TriggerGroup_Activate:
			{
				ModelAction_TriggerGroup_Activate modelAction_TriggerGroup_Activate = (ModelAction_TriggerGroup_Activate)modelAction;
				writer.WriteTriggerGroupId(modelAction_TriggerGroup_Activate.TriggerGroup);
				break;
			}
			}
		}

		private void WritePersonCustomTitles(BinaryWriter writer, List<ModelPerson> people)
		{
			IEnumerable<ModelPerson> enumerable = people.Where((ModelPerson e) => !string.IsNullOrWhiteSpace(e.CustomTitle));
			writer.Write(enumerable.Count());
			foreach (ModelPerson item in enumerable)
			{
				writer.Write(item.Id);
				writer.WriteStringOrEmpty(item.CustomTitle);
			}
		}

		private void WriteFleetOrderAvailableCredits(BinaryWriter writer, List<ModelFleet> fleets)
		{
			List<(int, int, int)> list = new List<(int, int, int)>(8);
			foreach (ModelFleet fleet in fleets)
			{
				if (fleet.OrdersCollection == null)
				{
					continue;
				}
				for (int i = 0; i < fleet.OrdersCollection.Orders.Count; i++)
				{
					ModelFleetOrder modelFleetOrder = fleet.OrdersCollection.Orders[i];
					if (modelFleetOrder.AvailableCredits >= 0)
					{
						list.Add((fleet.Id, i, modelFleetOrder.AvailableCredits));
					}
				}
			}
			writer.Write(list.Count);
			foreach (var item in list)
			{
				writer.Write(item.Item1);
				writer.Write(item.Item2);
				writer.Write(item.Item3);
			}
		}

		private void WriteUnitCargoCapacity(BinaryWriter writer, List<ModelUnit> units)
		{
			IEnumerable<ModelUnit> enumerable = units.Where((ModelUnit e) => e.ComponentUnitData != null && e.ComponentUnitData.CargoCapacity.HasValue);
			writer.Write(enumerable.Count());
			foreach (ModelUnit item in enumerable)
			{
				writer.WriteUnitId(item);
				writer.Write(item.ComponentUnitData.CargoCapacity.Value);
			}
		}

		private void WriteUnitScanRange(BinaryWriter writer, List<ModelUnit> units)
		{
			IEnumerable<ModelUnit> enumerable = units.Where((ModelUnit e) => e.ComponentUnitData != null && e.ComponentUnitData.ScanRange.HasValue);
			writer.Write(enumerable.Count());
			foreach (ModelUnit item in enumerable)
			{
				writer.WriteUnitId(item);
				writer.Write(item.ComponentUnitData.ScanRange.Value);
			}
		}

		private void WriteInvulnerableUnits(BinaryWriter writer, List<ModelUnit> units)
		{
			IEnumerable<ModelUnit> enumerable = units.Where((ModelUnit e) => e.IsInvulnerable);
			writer.Write(enumerable.Count());
			foreach (ModelUnit item in enumerable)
			{
				writer.WriteUnitId(item);
			}
		}

		private void WriteNoDestructionUnits(BinaryWriter writer, List<ModelUnit> units)
		{
			IEnumerable<ModelUnit> enumerable = units.Where((ModelUnit e) => e.AvoidDestruction);
			writer.Write(enumerable.Count());
			foreach (ModelUnit item in enumerable)
			{
				writer.WriteUnitId(item);
			}
		}

		private void WriteUnitMass(BinaryWriter writer, List<ModelUnit> units)
		{
			IEnumerable<ModelUnit> enumerable = units.Where((ModelUnit e) => e.Mass.HasValue);
			writer.Write(enumerable.Count());
			foreach (ModelUnit item in enumerable)
			{
				writer.WriteUnitId(item);
				writer.Write(item.Mass.Value);
			}
		}

		private void WriteUnitsUnderConstruction(BinaryWriter writer, List<ModelUnit> units)
		{
			IEnumerable<ModelUnit> enumerable = units.Where((ModelUnit e) => e.ComponentUnitData != null && e.ComponentUnitData.ConstructionState != ConstructionState.Constructed);
			writer.Write(enumerable.Count());
			foreach (ModelUnit item in enumerable)
			{
				writer.WriteUnitId(item);
				writer.Write((byte)item.ComponentUnitData.ConstructionState);
				writer.Write(item.ComponentUnitData.ConstructionProgress);
			}
		}

		private void WriteUnitTotalDamageReceived(BinaryWriter writer, List<ModelUnit> units)
		{
			IEnumerable<ModelUnit> enumerable = units.Where((ModelUnit e) => e.HealthData != null && e.TotalDamagedReceived > 0f);
			writer.Write(enumerable.Count());
			foreach (ModelUnit item in enumerable)
			{
				writer.WriteUnitId(item);
				writer.Write(item.TotalDamagedReceived);
			}
		}

		private void WriteCustomSectorAppearances(BinaryWriter writer, List<ModelSector> sectors)
		{
			IEnumerable<ModelSector> enumerable = sectors.Where((ModelSector e) => e.CustomAppearance != null);
			writer.Write(enumerable.Count());
			foreach (ModelSector item in enumerable)
			{
				writer.WriteSectorId(item);
				WriteCustomSectorAppearance(writer, item.CustomAppearance);
			}
		}

		private void WriteCustomSectorAppearance(BinaryWriter writer, ModelSectorAppearance customAppearance)
		{
			writer.Write((int)customAppearance.NebulaBrightness);
			writer.Write((int)customAppearance.NebulaColors);
			writer.Write(customAppearance.NebulaComplexity);
			writer.Write(customAppearance.NebulaCount);
			writer.Write(customAppearance.NebulaTextureCount);
			writer.Write((int)customAppearance.NebulaStyles);
			writer.Write((int)customAppearance.StarsCount);
			writer.Write(customAppearance.StarsIntensity);
		}

		private void WritePlayerFleetSettings(BinaryWriter writer, IEnumerable<ModelFleet> fleets)
		{
			IEnumerable<ModelFleet> enumerable = fleets.Where((ModelFleet e) => e.FleetSettings.PlayerFleetSettings != null);
			writer.Write(enumerable.Count());
			foreach (ModelFleet item in enumerable)
			{
				writer.WriteFleetId(item);
				WritePlayerFleetSettings(writer, item.FleetSettings.PlayerFleetSettings);
			}
		}

		private void WritePlayerFleetSettings(BinaryWriter writer, ModelPlayerFleetSettings modelPlayerFleetSettings)
		{
			writer.Write(modelPlayerFleetSettings.NotifyWhenOrderComplete);
			writer.Write(modelPlayerFleetSettings.NotifyWhenScannedHostile);
			writer.Write(modelPlayerFleetSettings.NotifyWhenAbandonedUnitFound);
			writer.Write(modelPlayerFleetSettings.NotifyWhenAbandonedCargoFound);
		}

		private void WriteUnitCaptureCooldownTimes(BinaryWriter writer, IEnumerable<ModelUnit> units)
		{
			IEnumerable<ModelUnit> enumerable = units.Where((ModelUnit e) => e.ComponentUnitData != null && e.ComponentUnitData.CaptureCooldownTime.HasValue);
			writer.Write(enumerable.Count());
			foreach (ModelUnit item in enumerable)
			{
				writer.WriteUnitId(item);
				writer.Write(item.ComponentUnitData.CaptureCooldownTime.Value);
			}
		}

		private void WriteAutoTurretModuleData(BinaryWriter writer, List<ModelUnit> units)
		{
			List<ModelUnit> list = units.Where((ModelUnit e) => e.ComponentUnitData != null && e.ComponentUnitData.AutoTurretFireMode.HasValue).ToList();
			writer.Write(list.Count);
			foreach (ModelUnit item in list)
			{
				writer.WriteUnitId(item);
				writer.Write((int)item.ComponentUnitData.AutoTurretFireMode.Value);
			}
		}

		private void WriteUnitRadii(BinaryWriter writer, List<ModelUnit> units)
		{
			List<ModelUnit> list = units.Where((ModelUnit e) => e.Radius.HasValue).ToList();
			writer.Write(list.Count);
			foreach (ModelUnit item in list)
			{
				writer.WriteUnitId(item);
				writer.Write(item.Radius.Value);
			}
		}

		private void WriteSeedOptions(BinaryWriter writer, ModelSeedOptions seedOptions)
		{
			writer.Write(seedOptions != null);
			if (seedOptions != null)
			{
				writer.Write(seedOptions.SeedAbandonedCargo);
				writer.Write(seedOptions.SeedAbandonedShips);
				writer.Write(seedOptions.SeedCargoHolds);
				writer.Write(seedOptions.SeedFactionIntel);
				writer.Write(seedOptions.SeedPassengerGroups);
			}
		}

		private void PrintStatus(string message, BinaryWriter writer, Action<string> logger)
		{
			logger?.Invoke($"{message} - {writer.BaseStream.Position - 1} bytes wrote");
		}

		private void WriteMoons(BinaryWriter writer, IList<ModelMoon> moons)
		{
			writer.Write(moons.Count);
			foreach (ModelMoon moon in moons)
			{
				writer.WriteUnitId(moon.Unit);
				writer.WriteUnitId(moon.OrbitUnit);
				writer.WriteVec3(moon.OffsetFromPlanet);
			}
		}

		private void WriteScenarioData(BinaryWriter writer, ModelScenarioData scenarioData)
		{
			writer.Write(scenarioData.HasRandomEvents);
			if (scenarioData.HasRandomEvents)
			{
				writer.Write(scenarioData.NextRandomEventTime);
			}
			writer.Write(scenarioData.FactionSpawner != null);
			if (scenarioData.FactionSpawner != null)
			{
				writer.Write(scenarioData.FactionSpawner.NextUpdate);
			}
			writer.Write(scenarioData.TradeRouteScenarioData != null);
			if (scenarioData.TradeRouteScenarioData != null)
			{
				writer.Write(scenarioData.TradeRouteScenarioData.NumBlackSailShipsDestroyed);
				writer.WriteFactionId(scenarioData.TradeRouteScenarioData.PirateFaction);
			}
			writer.Write((int)scenarioData.RespawnOnDeath);
			writer.Write(scenarioData.AllowTeleporting);
			writer.Write(scenarioData.Permadeath);
			writer.Write(scenarioData.AsteroidRespawningEnabled);
			writer.Write(scenarioData.AsteroidRespawnTime);
			writer.Write(scenarioData.NextProcessOtherEventsTime);
			writer.Write(scenarioData.AllowStationCapture);
			writer.Write(scenarioData.AllowAbandonShip);
		}

		private void WriteAllFactionTransactions(BinaryWriter writer, IEnumerable<ModelFaction> factions)
		{
			List<ModelFaction> list = factions.Where((ModelFaction e) =>
			{
				List<ModelFactionTransaction> transactions = e.Transactions;
				return transactions != null && transactions.Count > 0;
			}).ToList();
			writer.Write(list.Count);
			foreach (ModelFaction item in list)
			{
				writer.WriteFactionId(item);
				writer.Write(item.Transactions.Count);
				foreach (ModelFactionTransaction transaction in item.Transactions)
				{
					WriteFactionTransaction(writer, transaction);
				}
			}
		}

		private static void WriteFactionTransaction(BinaryWriter writer, ModelFactionTransaction transaction)
		{
			writer.Write((int)transaction.TransactionType);
			writer.Write(transaction.Value);
			writer.Write(transaction.CurrentBalance);
			writer.WriteUnitId(transaction.LocationUnit);
			writer.WriteFactionId(transaction.OtherFaction);
			writer.Write((int)transaction.RelatedCargoClass);
			writer.Write((int)transaction.RelatedUnitClass);
			writer.Write(transaction.GameWorldTime);
			writer.Write((int)transaction.TaxType);
			writer.Write(transaction.RelatedCount.HasValue ? transaction.RelatedCount.Value : (-1));
		}

		private void WriteGamePlayer(BinaryWriter writer, ModelPlayer player)
		{
			writer.Write(player.VisitedUnits.Count);
			foreach (ModelUnit visitedUnit in player.VisitedUnits)
			{
				writer.WriteUnitId(visitedUnit);
			}
			writer.Write(player.Messages.Count);
			foreach (ModelPlayerMessage message in player.Messages)
			{
				WritePlayerMessage(writer, message);
			}
			writer.Write(player.DelayedMessages.Count);
			foreach (ModelPlayerDelayedMessage delayedMessage in player.DelayedMessages)
			{
				writer.Write(delayedMessage.ShowTime);
				writer.Write(delayedMessage.Important);
				writer.Write(delayedMessage.Notifications);
				WritePlayerMessage(writer, delayedMessage.Message);
			}
			WritePlayerWaypointIfSet(writer, player.CustomWaypoint);
			writer.Write((player.ActiveJob != null) ? player.ActiveJob.Id : (-1));
			WritePlayerStats(writer, player.Stats);
		}

		private void WritePlayerStats(BinaryWriter writer, ModelPlayerStats stats)
		{
			writer.Write(stats.SectorsVisited.Count);
			foreach (ModelSector item in stats.SectorsVisited)
			{
				writer.Write(item.Id);
			}
			writer.Write(stats.TotalBountyClaimed);
			writer.Write(stats.ShipsMinedToDeath);
		}

		private static void WritePlayerWaypointIfSet(BinaryWriter writer, ModelPlayerWaypoint playerWaypoint)
		{
			writer.Write(playerWaypoint != null);
			if (playerWaypoint != null)
			{
				WritePlayerWaypoint(writer, playerWaypoint);
			}
		}

		public static void WritePlayerWaypoint(BinaryWriter writer, ModelPlayerWaypoint waypoint)
		{
			writer.WriteVec3(waypoint.SectorPosition);
			writer.WriteSectorId(waypoint.Sector);
			writer.WriteUnitId(waypoint.TargetUnit);
			writer.Write(waypoint.HadTargetObject);
		}

		public static void WritePlayerMessage(BinaryWriter writer, ModelPlayerMessage message)
		{
			writer.Write(message.Id);
			writer.Write(message.EngineTimeStamp);
			writer.Write(message.AllowDelete);
			writer.Write(message.Opened);
			writer.WriteUnitId(message.SenderUnit);
			writer.WriteSectorId(message.SenderUnitSector);
			writer.WriteVec3(message.SenderUnitSectorPosition);
			writer.WriteUnitId(message.SubjectUnit);
			writer.WriteSectorId(message.SubjectUnitSector);
			writer.WriteVec3(message.SubjectUnitSectorPosition);
			writer.Write(message.MessageTemplateId > -1);
			if (message.MessageTemplateId > -1)
			{
				writer.Write(message.MessageTemplateId);
				return;
			}
			writer.WriteStringOrEmpty(message.ToText);
			writer.WriteStringOrEmpty(message.FromText);
			writer.WriteStringOrEmpty(message.MessageText);
			writer.WriteStringOrEmpty(message.SubjectText);
		}

		private void WriteMissions(BinaryWriter writer, IList<ModelMission> missions)
		{
			writer.Write(missions.Count);
			foreach (ModelMission mission in missions)
			{
				MissionWriter.Write(writer, mission);
			}
		}

		private void WriteFleetSpawners(BinaryWriter writer, IList<ModelFleetSpawner> fleetSpawners)
		{
			writer.Write(fleetSpawners.Count);
			foreach (ModelFleetSpawner fleetSpawner in fleetSpawners)
			{
				FleetSpawnerWriter.Write(writer, fleetSpawner);
			}
		}

		private void WriteFactionMercenaryData(BinaryWriter writer, IEnumerable<ModelFaction> factions)
		{
			List<ModelFaction> list = factions.Where((ModelFaction e) => e.FactionAI?.FactionMercenaryHireInfo != null).ToList();
			writer.Write(list.Count);
			foreach (ModelFaction item in list)
			{
				writer.WriteFactionId(item);
				writer.WriteFactionId(item.FactionAI.FactionMercenaryHireInfo.HiringFaction);
				writer.Write(item.FactionAI.FactionMercenaryHireInfo.HireTimeExpiry);
			}
		}

		private void WriteFactionAIExcludedUnits(BinaryWriter writer, IEnumerable<ModelFaction> factions)
		{
			List<ModelFaction> list = factions.Where((ModelFaction e) => e.FactionAI != null && e.FactionAI.ExcludedUnits != null && e.FactionAI.ExcludedUnits.Count > 0).ToList();
			writer.Write(list.Count);
			foreach (ModelFaction item in list)
			{
				writer.WriteFactionId(item);
				writer.Write(item.FactionAI.ExcludedUnits.Count);
				foreach (ModelUnit excludedUnit in item.FactionAI.ExcludedUnits)
				{
					writer.WriteUnitId(excludedUnit);
				}
			}
		}

		private void WriteAllFactionAIsAndBountyBoards(BinaryWriter writer, IList<ModelFaction> factions)
		{
			writer.Write(factions.Count);
			foreach (ModelFaction faction in factions)
			{
				writer.WriteFactionId(faction);
				writer.Write(faction.FactionAI != null);
				if (faction.FactionAI != null)
				{
					writer.Write((int)faction.FactionAI.AIType);
					FactionAIWriter.Write(writer, faction.FactionAI.AIType, faction.FactionAI);
				}
				writer.Write(faction.BountyBoard != null);
				if (faction.BountyBoard != null)
				{
					WriteFactionBountyBoard(writer, faction.BountyBoard);
				}
			}
		}

		private static void WriteFactionBountyBoard(BinaryWriter writer, ModelFactionBountyBoard bountyBoard)
		{
			writer.Write(bountyBoard.Items.Count);
			foreach (ModelFactionBountyBoardItem item in bountyBoard.Items)
			{
				WriteFactionBountyBoardItem(writer, item);
			}
		}

		private static void WriteFactionBountyBoardItem(BinaryWriter writer, ModelFactionBountyBoardItem item)
		{
			writer.WritePersonId(item.TargetPerson);
			writer.Write(item.Reward);
			writer.WriteUnitId(item.LastKnownTargetUnit);
			writer.WriteSectorId(item.LastKnownTargetSector);
			writer.WriteNullableVec3(item.LastKnownTargetPosition);
			writer.Write(item.TimeOfLastSighting.HasValue ? item.TimeOfLastSighting.Value : (-1.0));
			writer.WriteFactionId(item.SourceFaction);
		}

		private void WriteJobs(BinaryWriter writer, IEnumerable<ModelUnit> units)
		{
			List<ModelJob> list = units.SelectMany((ModelUnit e) => e.Jobs).ToList();
			writer.Write(list.Count);
			foreach (ModelJob item in list)
			{
				writer.Write((int)item.JobType);
				JobWriter.Write(writer, item);
			}
		}

		private void WriteFactionLeaders(BinaryWriter writer, IEnumerable<ModelFaction> factions)
		{
			List<ModelFaction> list = factions.Where((ModelFaction e) => e.Leader != null).ToList();
			writer.Write(list.Count);
			foreach (ModelFaction item in list)
			{
				writer.WriteFactionId(item);
				writer.WritePersonId(item.Leader);
			}
		}

		private void WriteNpcPilots(BinaryWriter writer, List<ModelPerson> people)
		{
			List<ModelPerson> list = people.Where((ModelPerson e) => e.NpcPilot != null).ToList();
			writer.Write(list.Count);
			foreach (ModelPerson item in list)
			{
				writer.WritePersonId(item);
				WriteNpcPilot(writer, item.NpcPilot);
			}
		}

		private void WriteNpcPilot(BinaryWriter writer, ModelNpcPilot npcPilot)
		{
			writer.Write(npcPilot.DestroyWhenNoUnit);
			writer.Write(npcPilot.DestroyWhenNotPilotting);
			writer.WriteFleetId(npcPilot.Fleet);
		}

		private void WritePeople(BinaryWriter writer, IList<ModelPerson> people, ModelPerson playerPerson)
		{
			writer.Write(people.Count);
			foreach (ModelPerson person in people)
			{
				WritePerson(writer, person, person == playerPerson);
			}
		}

		private static void WritePerson(BinaryWriter writer, ModelPerson person, bool isPlayerPerson)
		{
			writer.WritePersonId(person);
			bool flag = string.IsNullOrWhiteSpace(person.CustomName);
			writer.Write(flag);
			if (flag)
			{
				writer.Write(person.GeneratedFirstNameId);
				writer.Write(person.GeneratedLastNameId);
			}
			else
			{
				writer.WriteStringOrEmpty(person.CustomName);
				writer.WriteStringOrEmpty(person.CustomShortName);
			}
			writer.Write(person.Seed);
			writer.Write(person.DialogId);
			writer.Write(person.IsMale);
			writer.Write(person.IsAutoPilot);
			writer.WriteFactionId(person.Faction);
			writer.Write(person.DestroyGameObjectOnKill);
			writer.WriteUnitId(person.CurrentUnit);
			writer.Write(person.IsPilot);
			writer.Write(person.Kills);
			writer.Write(person.Deaths);
			writer.Write(person.Properness);
			writer.Write(person.Aggression);
			writer.Write(person.Greed);
			writer.Write(person.RankId);
			writer.Write((sbyte)person.AvatarProfileId);
			writer.Write(person.DialogProfileId);
			writer.Write(isPlayerPerson);
			writer.Write(person.NpcPilotSettings != null);
			if (person.NpcPilotSettings != null)
			{
				WriteNpcPilotSettings(writer, person.NpcPilotSettings);
			}
		}

		private static void WriteNpcPilotSettings(BinaryWriter writer, ModelNpcPilotSettings settings)
		{
			writer.Write(settings.RestrictedWeaponPreference);
			writer.Write(settings.CombatEfficiency);
			writer.Write(settings.CheatAmmo);
			writer.Write(settings.AllowDitchShip);
		}

		private void WriteFleets(BinaryWriter writer, IList<ModelFleet> fleets)
		{
			writer.Write(fleets.Count);
			foreach (ModelFleet fleet in fleets)
			{
				WriteFleet(writer, fleet);
			}
		}

		private void WriteNamedFleets(BinaryWriter writer, IList<ModelFleet> fleets)
		{
			IEnumerable<ModelFleet> enumerable = fleets.Where((ModelFleet e) => !string.IsNullOrWhiteSpace(e.Name));
			writer.Write(enumerable.Count());
			foreach (ModelFleet item in enumerable)
			{
				writer.Write(item.Id);
				writer.WriteStringOrEmpty(item.Name);
			}
		}

		private void WriteFleetOrders(BinaryWriter writer, IList<ModelFleet> fleets)
		{
			List<ModelFleet> list = fleets.Where((ModelFleet e) => e.OrdersCollection.Orders.Count > 0).ToList();
			writer.Write(list.Count);
			foreach (ModelFleet item in list)
			{
				writer.WriteFleetId(item);
				FleetOrdersWriter.Write(writer, item.OrdersCollection);
			}
		}

		private void WriteSectorTarget(BinaryWriter writer, ModelSectorTarget modelSectorTarget)
		{
			writer.Write(modelSectorTarget != null);
			if (modelSectorTarget != null)
			{
				SectorTargetWriter.Write(writer, modelSectorTarget);
			}
		}

		private void WriteFleet(BinaryWriter writer, ModelFleet fleet)
		{
			writer.Write(fleet.IsActive);
			writer.Write(fleet.Id);
			writer.Write(fleet.Seed);
			writer.WriteVec3(fleet.Position);
			writer.WriteVec4(fleet.Rotation);
			writer.WriteSectorId(fleet.Sector);
			writer.WriteFactionId(fleet.Faction);
			writer.Write(fleet.FormationId);
			writer.Write(fleet.HomeBase != null);
			if (fleet.HomeBase != null)
			{
				SectorTargetWriter.Write(writer, fleet.HomeBase);
			}
			writer.Write(fleet.ExcludeFromFactionAI);
			writer.Write((int)fleet.Strategy);
			writer.Write(fleet.FleetSettings != null);
			if (fleet.FleetSettings != null)
			{
				WriteFleetSettings(writer, fleet.FleetSettings);
			}
		}

		private void WriteFleetSettings(BinaryWriter writer, ModelFleetSettings settings)
		{
			writer.Write(settings.PreferCloak);
			writer.Write((byte)settings.PreferToDock);
			writer.Write(settings.Aggression);
			writer.Write(settings.AllowAttack);
			writer.Write(settings.TargetInterceptionLowerDistance);
			writer.Write(settings.TargetInterceptionUpperDistance);
			writer.Write(settings.MaxJumpDistance);
			writer.Write(settings.AllowCombatInterception);
			writer.Write(settings.DestroyWhenNoPilots);
			writer.Write(settings.FormationTightness);
			writer.Write((int)settings.CargoCollectionPreference);
		}

		private void WriteHangars(BinaryWriter writer, IEnumerable<ModelUnit> units)
		{
			List<ModelUnit> list = units.Where((ModelUnit e) => e.ComponentUnitData?.DockData != null).ToList();
			writer.Write(list.Count);
			foreach (ModelUnit item in list)
			{
				WriteHangerItems(writer, item, item.ComponentUnitData.DockData);
			}
		}

		private void WriteHangerItems(BinaryWriter writer, ModelUnit unit, ModelComponentUnitDockData dockData)
		{
			writer.WriteUnitId(unit);
			writer.Write(dockData.Items.Count);
			foreach (ModelComponentUnitDockDataItem item in dockData.Items)
			{
				writer.Write(item.BayId);
				writer.WriteUnitId(item.DockedUnit);
			}
		}

		private void WriteWormholes(BinaryWriter writer, IEnumerable<ModelUnit> allUnits)
		{
			List<ModelUnit> list = allUnits.Where((ModelUnit e) => e.WormholeData != null).ToList();
			writer.Write(list.Count);
			foreach (ModelUnit item in list)
			{
				writer.WriteUnitId(item);
				WriteWormholeData(writer, item.WormholeData);
			}
		}

		private void WriteWormholeData(BinaryWriter writer, ModelUnitWormholeData wormholeData)
		{
			writer.WriteUnitId(wormholeData.TargetWormholeUnit);
			writer.Write(wormholeData.IsUnstable);
			writer.Write(wormholeData.UnstableNextChangeTargetTime);
			writer.WriteVec3(wormholeData.UnstableTargetPosition);
			writer.WriteVec3(wormholeData.UnstableTargetRotation);
			writer.WriteSectorId(wormholeData.UnstableTargetSector);
		}

		private void WritePassengerGroups(BinaryWriter writer, IList<ModelUnit> units)
		{
			List<ModelPassengerGroup> list = units.SelectMany((ModelUnit e) => e.PassengerGroups).ToList();
			writer.Write(list.Count);
			foreach (ModelPassengerGroup item in list)
			{
				WritePassengerGroup(writer, item);
			}
		}

		private void WritePassengerGroup(BinaryWriter writer, ModelPassengerGroup passengerGroup)
		{
			writer.Write(passengerGroup.Id);
			writer.WriteUnitId(passengerGroup.Unit);
			writer.WriteUnitId(passengerGroup.SourceUnit);
			writer.WriteUnitId(passengerGroup.DestinationUnit);
			writer.Write(passengerGroup.PassengerCount);
			writer.Write(passengerGroup.ExpiryTime);
			writer.Write(passengerGroup.Revenue);
		}

		private void WriteAllFactionIntel(BinaryWriter writer, IEnumerable<ModelFaction> factions)
		{
			List<ModelFaction> list = factions.Where((ModelFaction e) => e.Intel != null).ToList();
			writer.Write(list.Count);
			foreach (ModelFaction item in list)
			{
				writer.WriteFactionId(item);
				WriteFactionIntel(writer, item.Intel);
			}
		}

		private void WriteFactionIntel(BinaryWriter writer, ModelFactionIntel factionIntel)
		{
			writer.Write(factionIntel.Sectors.Count);
			foreach (ModelSector sector in factionIntel.Sectors)
			{
				writer.WriteSectorId(sector);
			}
			writer.Write(factionIntel.Units.Count);
			foreach (ModelUnit unit in factionIntel.Units)
			{
				writer.WriteUnitId(unit);
			}
			writer.Write(factionIntel.EnteredWormholes.Count);
			foreach (ModelUnit enteredWormhole in factionIntel.EnteredWormholes)
			{
				writer.WriteUnitId(enteredWormhole);
			}
		}

		private void WriteAllUnitHealthDatas(BinaryWriter writer, List<ModelUnit> units)
		{
			List<ModelUnit> list = units.Where((ModelUnit e) => e.HealthData != null).ToList();
			writer.Write(list.Count);
			foreach (ModelUnit item in list)
			{
				writer.WriteUnitId(item);
				WriteUnitHealthData(writer, item.HealthData);
			}
		}

		private void WriteUnitHealthData(BinaryWriter writer, ModelUnitHealthData healthData)
		{
			writer.Write(healthData.IsDestroyed);
			writer.Write(healthData.Health);
		}

		private void WriteActiveUnits(BinaryWriter writer, List<ModelUnit> units)
		{
			List<ModelUnit> list = units.Where((ModelUnit e) => e.ActiveData != null).ToList();
			writer.Write(list.Count);
			foreach (ModelUnit item in list)
			{
				writer.WriteUnitId(item);
				WriteActiveUnitData(writer, item.ActiveData);
			}
		}

		private void WriteActiveUnitData(BinaryWriter writer, ModelUnitActiveData activeData)
		{
			writer.WriteVec3(activeData.Velocity);
			writer.Write(activeData.CurrentTurn);
		}

		private void WriteAllUnitComponentHealthData(BinaryWriter writer, List<ModelUnit> units)
		{
			List<ModelUnit> list = units.Where((ModelUnit e) =>
			{
				ModelComponentUnitData componentUnitData = e.ComponentUnitData;
				return componentUnitData != null && componentUnitData.ComponentHealthData?.Items.Count > 0;
			}).ToList();
			writer.Write(list.Count);
			foreach (ModelUnit item in list)
			{
				writer.WriteUnitId(item);
				WriteUnitComponentHealthData(writer, item.ComponentUnitData.ComponentHealthData);
			}
		}

		private void WriteUnitComponentHealthData(BinaryWriter writer, ModelComponentUnitComponentHealthData componentHealthData)
		{
			writer.Write(componentHealthData.Items.Count);
			foreach (ModelComponentUnitComponentHealthDataItem item in componentHealthData.Items)
			{
				writer.Write(item.BayId);
				writer.Write(item.Health);
			}
		}

		private void WriteAllShieldHealthData(BinaryWriter writer, IEnumerable<ModelUnit> units)
		{
			List<ModelUnit> list = units.Where((ModelUnit e) =>
			{
				ModelComponentUnitData componentUnitData = e.ComponentUnitData;
				return componentUnitData != null && componentUnitData.ShieldData?.Items.Count > 0;
			}).ToList();
			writer.Write(list.Count);
			foreach (ModelUnit item in list)
			{
				writer.WriteUnitId(item);
				WriteShieldHealthData(writer, item.ComponentUnitData.ShieldData);
			}
		}

		private void WriteShieldHealthData(BinaryWriter writer, ModelComponentUnitShieldHealthData shieldHealthData)
		{
			writer.Write((byte)shieldHealthData.Items.Count);
			foreach (ModelComponentUnitShieldHealthDataItem item in shieldHealthData.Items)
			{
				writer.Write((byte)item.ShieldPointIndex);
				writer.Write(item.Health);
			}
		}

		private void WriteComponentUnitCargo(BinaryWriter writer, List<ModelUnit> units)
		{
			List<ModelUnit> list = units.Where((ModelUnit e) => e.ComponentUnitData?.CargoData != null).ToList();
			writer.Write(list.Count);
			foreach (ModelUnit item in list)
			{
				writer.WriteUnitId(item);
				WriteComponentUnitCargoData(writer, item.ComponentUnitData.CargoData);
			}
		}

		private void WriteComponentUnitCargoData(BinaryWriter writer, ModelComponentUnitCargoData cargoData)
		{
			writer.Write(cargoData.Items.Count);
			foreach (ModelComponentUnitCargoDataItem item in cargoData.Items)
			{
				ComponentUnitCargoDataItemWriter.Write(writer, item);
			}
		}

		private void WriteUnitEngineThrottles(BinaryWriter writer, List<ModelUnit> units)
		{
			List<ModelUnit> list = units.Where((ModelUnit e) => e.ComponentUnitData?.EngineThrottle.HasValue ?? false).ToList();
			writer.Write(list.Count);
			foreach (ModelUnit item in list)
			{
				writer.WriteUnitId(item);
				writer.Write(item.ComponentUnitData.EngineThrottle.Value);
			}
		}

		private void WritePoweredDownComponents(BinaryWriter writer, IEnumerable<ModelUnit> units)
		{
			List<ModelUnit> list = units.Where((ModelUnit e) => e.ComponentUnitData != null && e.ComponentUnitData.PoweredDownBayIds != null && e.ComponentUnitData.PoweredDownBayIds.Count > 0).ToList();
			writer.Write(list.Count);
			foreach (ModelUnit item in list)
			{
				writer.WriteUnitId(item);
				writer.Write(item.ComponentUnitData.PoweredDownBayIds.Count);
				foreach (int poweredDownBayId in item.ComponentUnitData.PoweredDownBayIds)
				{
					writer.Write(poweredDownBayId);
				}
			}
		}

		private void WriteAutoFireComponents(BinaryWriter writer, IEnumerable<ModelUnit> units)
		{
			List<ModelUnit> list = units.Where((ModelUnit e) => e.ComponentUnitData != null && e.ComponentUnitData.AutoFireBayIds != null && e.ComponentUnitData.AutoFireBayIds.Count > 0).ToList();
			writer.Write(list.Count);
			foreach (ModelUnit item in list)
			{
				writer.WriteUnitId(item);
				writer.Write(item.ComponentUnitData.AutoFireBayIds.Count);
				foreach (int autoFireBayId in item.ComponentUnitData.AutoFireBayIds)
				{
					writer.Write(autoFireBayId);
				}
			}
		}

		private void WriteCloakedUnits(BinaryWriter writer, IEnumerable<ModelUnit> units)
		{
			List<ModelUnit> list = units.Where((ModelUnit e) => e.ComponentUnitData?.IsCloaked ?? false).ToList();
			writer.Write(list.Count);
			foreach (ModelUnit item in list)
			{
				writer.WriteUnitId(item);
			}
		}

		private void WriteUnitCapacitorCharges(BinaryWriter writer, List<ModelUnit> units)
		{
			List<ModelUnit> list = units.Where((ModelUnit e) => e.ComponentUnitData?.CapacitorCharge.HasValue ?? false).ToList();
			writer.Write(list.Count);
			foreach (ModelUnit item in list)
			{
				writer.WriteUnitId(item);
				writer.Write(item.ComponentUnitData.CapacitorCharge.Value);
			}
		}

		private void WriteModdedComponents(BinaryWriter writer, List<ModelUnit> units)
		{
			List<ModelUnit> list = units.Where((ModelUnit e) =>
			{
				ModelComponentUnitData componentUnitData = e.ComponentUnitData;
				return componentUnitData != null && componentUnitData.ModData?.Items.Count > 0;
			}).ToList();
			writer.Write(list.SelectMany((ModelUnit e) => e.ComponentUnitData.ModData.Items).Count());
			foreach (ModelUnit item in list)
			{
				foreach (ModelComponentUnitModDataItem item2 in item.ComponentUnitData.ModData.Items)
				{
					writer.WriteUnitId(item);
					writer.Write(item2.BayId);
					writer.Write((int)item2.ComponentClass);
				}
			}
		}

		private void WriteAllComponentUnits(BinaryWriter writer, IEnumerable<ModelUnit> units)
		{
			List<ModelUnit> list = units.Where((ModelUnit e) => e.ComponentUnitData != null).ToList();
			writer.Write(list.Count);
			foreach (ModelUnit item in list)
			{
				writer.Write(item.Id);
				WriteComponentUnitData(writer, item.ComponentUnitData);
			}
		}

		private void WriteComponentUnitData(BinaryWriter writer, ModelComponentUnitData componentUnitData)
		{
			writer.Write(componentUnitData.ShipNameIndex);
			if (componentUnitData.ShipNameIndex == -1)
			{
				writer.WriteStringOrEmpty(componentUnitData.CustomShipName);
			}
			writer.Write(componentUnitData.FactoryData != null);
			if (componentUnitData.FactoryData != null)
			{
				WriteComponentUnitFactoryData(writer, componentUnitData.FactoryData);
			}
		}

		private void WriteComponentUnitFactoryData(BinaryWriter writer, ModelComponentUnitFactoryData factoryData)
		{
			writer.Write(factoryData.Items.Count);
			foreach (ModelComponentUnitFactoryItemData item in factoryData.Items)
			{
				writer.Write((int)item.State);
				writer.Write(item.ProductionElapsed);
			}
		}

		private void WriteNamedUnits(BinaryWriter writer, IEnumerable<ModelUnit> units)
		{
			List<ModelUnit> list = units.Where((ModelUnit e) => !string.IsNullOrWhiteSpace(e.Name)).ToList();
			writer.Write(list.Count);
			foreach (ModelUnit item in list)
			{
				writer.WriteUnitId(item);
				writer.WriteStringOrEmpty(item.Name);
				writer.WriteStringOrEmpty(item.ShortName);
			}
		}

		private void WriteUnits(BinaryWriter writer, IList<ModelUnit> units)
		{
			writer.Write(units.Count);
			foreach (ModelUnit unit in units)
			{
				WriteUnit(writer, unit);
			}
		}

		private void WriteUnit(BinaryWriter writer, ModelUnit unit)
		{
			writer.Write(unit.Id);
			writer.Write(unit.Seed);
			writer.Write((int)unit.Class);
			writer.WriteSectorId(unit.Sector);
			writer.WriteVec3(unit.Position);
			writer.WriteVec3(unit.Rotation);
			writer.WriteFactionId(unit.Faction);
			writer.Write(unit.RpProvision);
			writer.Write(unit.CargoData != null);
			if (unit.CargoData != null)
			{
				WriteUnitCargoData(writer, unit.CargoData);
			}
			writer.Write(unit.DebrisData != null);
			if (unit.DebrisData != null)
			{
				WriteUnitDebrisData(writer, unit.DebrisData);
			}
			writer.Write(unit.AsteroidData != null);
			if (unit.AsteroidData != null)
			{
				WriterUnitAsteroidData(writer, unit.AsteroidData);
			}
			writer.Write(unit.ShipTraderData != null);
			if (unit.ShipTraderData != null)
			{
				WriteShipTrader(writer, unit.ShipTraderData);
			}
			if (UnitHelper.IsProjectile(unit.Class))
			{
				writer.Write(unit.ProjectileData != null);
				if (unit.ProjectileData != null)
				{
					WriteUnitProjectileData(writer, unit.ProjectileData);
				}
			}
		}

		private void WriteUnitProjectileData(BinaryWriter writer, ModelUnitProjectileData projectileData)
		{
			writer.WriteUnitId(projectileData.SourceUnit);
			writer.WriteUnitId(projectileData.TargetUnit);
			writer.Write(projectileData.FireTime);
			writer.Write(projectileData.RemainingMovement);
			writer.WriteDamageType(projectileData.DamageType);
		}

		private void WriteShipTrader(BinaryWriter writer, ModelUnitShipTraderData shipTraderData)
		{
			writer.Write(shipTraderData.Items.Count);
			foreach (ModelUnitShipTraderItem item in shipTraderData.Items)
			{
				writer.Write(item.SellMultiplier);
				writer.Write((int)item.UnitClass);
			}
		}

		private void WriteUnitDebrisData(BinaryWriter writer, ModelUnitDebrisData debrisData)
		{
			writer.Write(debrisData.ScrapQuantity);
			writer.Write(debrisData.Expires);
			writer.Write(debrisData.ExpiryTime);
			writer.Write((int)debrisData.RelatedUnitClass);
		}

		private void WriterUnitAsteroidData(BinaryWriter writer, ModelUnitAsteroidData asteroidData)
		{
			writer.Write(asteroidData.RemainingYield);
		}

		private void WriteUnitCargoData(BinaryWriter writer, ModelUnitCargoData unitCargoData)
		{
			writer.Write((int)unitCargoData.CargoClass);
			writer.Write(unitCargoData.Quantity);
			writer.Write(unitCargoData.Expires);
			writer.Write(unitCargoData.SpawnTime);
		}

		private void WriteFactions(BinaryWriter writer, IList<ModelFaction> factions)
		{
			writer.Write(factions.Count);
			foreach (ModelFaction faction in factions)
			{
				WriteFaction(writer, faction);
			}
		}

		private void WriteFactionAvatarProfiles(BinaryWriter writer, IList<ModelFaction> factions)
		{
			IEnumerable<ModelFaction> enumerable = factions.Where((ModelFaction e) => e.AvatarProfileIds != null && e.AvatarProfileIds.Count > 0);
			writer.Write(enumerable.Count());
			foreach (ModelFaction item in enumerable)
			{
				writer.WriteFactionId(item);
				writer.Write((byte)item.AvatarProfileIds.Count);
				foreach (byte avatarProfileId in item.AvatarProfileIds)
				{
					writer.Write(avatarProfileId);
				}
			}
		}

		private void WriteSectors(BinaryWriter writer, List<ModelSector> sectors)
		{
			writer.Write(sectors.Count);
			foreach (ModelSector sector in sectors)
			{
				WriteSector(writer, sector);
			}
		}

		private void WritePatrolPaths(BinaryWriter writer, IList<ModelSectorPatrolPath> patrolPaths)
		{
			writer.Write(patrolPaths.Count);
			foreach (ModelSectorPatrolPath patrolPath in patrolPaths)
			{
				WritePatrolPath(writer, patrolPath);
			}
		}

		private void WriteAllFactionRelations(BinaryWriter writer, IEnumerable<ModelFaction> factions)
		{
			List<ModelFaction> list = factions.Where((ModelFaction e) =>
			{
				ModelFactionRelationData relations = e.Relations;
				return relations != null && relations.Items.Count > 0;
			}).ToList();
			writer.Write(list.Count);
			foreach (ModelFaction item in list)
			{
				writer.WriteFactionId(item);
				WriteFactionRelationData(writer, item.Relations);
			}
		}

		private void WriteFactionRecentDamageReceived(BinaryWriter writer, IEnumerable<ModelFaction> factions)
		{
			List<ModelFaction> list = factions.Where((ModelFaction e) =>
			{
				List<ModelFactionRecentDamageItem> recentDamageItems = e.RecentDamageItems;
				return recentDamageItems != null && recentDamageItems.Count > 0;
			}).ToList();
			writer.Write(list.Count);
			foreach (ModelFaction item in list)
			{
				writer.WriteFactionId(item);
				writer.Write(item.RecentDamageItems.Count);
				foreach (ModelFactionRecentDamageItem recentDamageItem in item.RecentDamageItems)
				{
					writer.WriteFactionId(recentDamageItem.OtherFaction);
					writer.Write(recentDamageItem.RecentDamageReceived);
				}
			}
		}

		private void WriteFactionRelationData(BinaryWriter writer, ModelFactionRelationData relationData)
		{
			writer.Write(relationData.Items.Count);
			foreach (ModelFactionRelationDataItem item in relationData.Items)
			{
				WriteFactionRelationDataItem(writer, item);
			}
		}

		private void WriteFactionRelationDataItem(BinaryWriter writer, ModelFactionRelationDataItem relation)
		{
			writer.WriteFactionId(relation.OtherFaction);
			writer.Write(relation.PermanentPeace);
			writer.Write(relation.RestrictHostilityTimeout);
			writer.Write((int)relation.Neutrality);
			writer.Write(relation.HostilityEndTime);
		}

		private void WriteAllFactionOpinions(BinaryWriter writer, IEnumerable<ModelFaction> factions)
		{
			var list = (from e in factions.Where((ModelFaction e) =>
				{
					ModelFactionOpinionData opinions = e.Opinions;
					return opinions != null && opinions.Items.Count > 0;
				})
				select new
				{
					Faction = e,
					Items = e.Opinions.Items.Where((ModelFactionOpinionDataItem item) => item.OtherFaction != e).ToList()
				}).ToList();
			writer.Write(list.SelectMany(e => e.Items).Count());
			foreach (var item in list)
			{
				foreach (ModelFactionOpinionDataItem item2 in item.Items)
				{
					writer.WriteFactionId(item.Faction);
					writer.WriteFactionId(item2.OtherFaction);
					writer.Write(item2.Opinion);
					writer.Write((uint)item2.CreatedTime);
				}
			}
		}

		private void WriteSector(BinaryWriter writer, ModelSector sector)
		{
			writer.Write(sector.Id);
			writer.WriteStringOrEmpty(sector.Name);
			writer.WriteVec3(sector.MapPosition);
			writer.WriteStringOrEmpty(sector.Description);
			writer.Write(sector.GateDistanceMultiplier);
			writer.Write(sector.RandomSeed);
			writer.WriteVec3(sector.BackgroundRotation);
			writer.WriteVec3(sector.AmbientLightColor);
			writer.WriteVec3(sector.DirectionLightColor);
			writer.WriteVec3(sector.DirectionLightRotation);
			writer.Write(sector.LastTimeChangedControl);
			writer.Write(sector.LightDirectionFudge);
		}

		private void WriteFaction(BinaryWriter writer, ModelFaction faction)
		{
			writer.Write(faction.Id);
			writer.Write(faction.GeneratedNameId);
			writer.Write(faction.GeneratedSuffixId);
			writer.WriteStringOrEmpty(faction.CustomName);
			writer.WriteStringOrEmpty(faction.CustomShortName);
			writer.WriteSectorId(faction.HomeSector);
			writer.WriteNullableVec3(faction.HomeSectorPosition);
			writer.Write(faction.Credits);
			writer.WriteStringOrEmpty(faction.Description);
			writer.Write(faction.IsCivilian);
			writer.Write((int)faction.FactionType);
			writer.Write(faction.Aggression);
			writer.Write(faction.Virtue);
			writer.Write(faction.Greed);
			writer.Write(faction.Cooperation);
			writer.Write(faction.TradeEfficiency);
			writer.Write(faction.DynamicRelations);
			writer.Write(faction.ShowJobBoards);
			writer.Write(faction.CreateJobs);
			writer.Write(faction.RequisitionPointMultiplier);
			writer.Write(faction.DestroyWhenNoUnits);
			writer.Write(faction.MinNpcCombatEfficiency);
			writer.Write(faction.MaxNpcCombatEfficiency);
			writer.Write(faction.AdditionalRpProvision);
			writer.Write(faction.TradeIllegalGoods);
			writer.Write(faction.SpawnTime);
			writer.Write(faction.HighestEverNetWorth);
			writer.Write(faction.RankingSystemId);
			writer.Write(faction.PreferredFormationId);
			writer.Write(faction.CustomSettings != null);
			if (faction.CustomSettings != null)
			{
				WriteFactionCustomSettings(writer, faction.CustomSettings);
			}
			writer.Write(faction.Stats != null);
			if (faction.Stats != null)
			{
				WriteFactionStats(writer, faction.Stats);
			}
			writer.Write(faction.AutopilotExcludedSectors.Count);
			foreach (ModelSector autopilotExcludedSector in faction.AutopilotExcludedSectors)
			{
				writer.WriteSectorId(autopilotExcludedSector);
			}
		}

		private void WriteFactionCustomSettings(BinaryWriter writer, ModelFactionCustomSettings settings)
		{
			writer.Write(settings.BuildShips);
			writer.Write(settings.RepairShips);
			writer.Write(settings.UpgradeShips);
			writer.Write(settings.RepairMinHullDamage);
			writer.Write(settings.RepairMinCreditsBeforeRepair);
			writer.Write(settings.PreferenceToPlaceBounty);
			writer.Write(settings.LargeShipPreference);
			writer.Write(settings.CloakShipPreference);
			writer.Write(settings.DailyIncome);
			writer.Write(settings.HostileWithAll);
			writer.Write(settings.MinFleetUnitCount);
			writer.Write(settings.MaxFleetUnitCount);
			writer.Write(settings.OffensiveStance);
			writer.Write(settings.AllowOtherFactionToUseDocks);
			writer.Write(settings.PreferenceToBuildTurrets);
			writer.Write(settings.PreferenceToBuildStations);
			writer.Write(settings.PreferenceToHaveAmmo);
			writer.Write(settings.IgnoreStationCreditsReserve);
			writer.Write(settings.MaxJumpDistanceFromHomeSector);
			writer.Write(settings.MaxStationBuildDistanceFromHomeSector);
			writer.Write((int)settings.PilotGender);
			writer.Write(settings.FixedShipCount);
			writer.Write(settings.SectorControlLikelihood);
		}

		private void WriteFactionStats(BinaryWriter writer, ModelFactionStats factionStats)
		{
			writer.Write(factionStats.TotalShipsClaimed);
			WriteFactionStatsUnitCounts(writer, factionStats.UnitsDestroyedByClassId.Where((KeyValuePair<ModelUnitClass, int> e) => e.Value > 0));
			WriteFactionStatsUnitCounts(writer, factionStats.UnitLostByClassId.Where((KeyValuePair<ModelUnitClass, int> e) => e.Value > 0));
			writer.Write(factionStats.ScratchcardsScratched);
			writer.Write(factionStats.HighestScratchcardWin);
		}

		private void WriteFactionStatsUnitCounts(BinaryWriter writer, IEnumerable<KeyValuePair<ModelUnitClass, int>> unitCountsByClass)
		{
			writer.Write(unitCountsByClass.Count());
			foreach (KeyValuePair<ModelUnitClass, int> item in unitCountsByClass)
			{
				writer.Write((int)item.Key);
				writer.Write(item.Value);
			}
		}

		private void WritePatrolPath(BinaryWriter writer, ModelSectorPatrolPath path)
		{
			writer.Write(path.Id);
			writer.WriteSectorId(path.Sector);
			writer.Write(path.IsLoop);
			writer.Write(path.Nodes.Count);
			foreach (ModelSectorPatrolPathNode node in path.Nodes)
			{
				writer.WriteVec3(node.SectorPosition);
				writer.Write(node.Order);
			}
		}
	}
}
