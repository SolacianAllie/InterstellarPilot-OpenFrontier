using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.Engine.Triggers;
using OpenFrontier.IP.SavedGames.V2.BinarySerialization.Readers;
using OpenFrontier.IP.SavedGames.V2.Model;
using OpenFrontier.IP.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.Engine.SaveGame
{
	public static class SaveGameUtilities
	{
		public static string HeaderDateFormat = "yyyy-MM-dd HH-mm-ss";

		private const string backupFileName = "backup";

		public static string Extension = "dat";

		public static string BackupExtension = "bak";

		private const string saveGamesPath = "SaveGames";

		public static EngineSaveGameHeader LoadHeader(string savePath)
		{
			EngineSaveGameHeader engineSaveGameHeader = new EngineSaveGameHeader();
			engineSaveGameHeader.FullPath = savePath;
			engineSaveGameHeader.FileSize = new FileInfo(savePath).Length;
			using (BinaryReader binaryReader = new BinaryReader(File.OpenRead(savePath)))
			{
				Version version = binaryReader.ReadVersion();
				binaryReader.BaseStream.Position = 0L;
				if (version < new Version(1, 6, 2))
				{
					if (!LegacySaveGameHeaderReader.ReadHeader160(binaryReader, engineSaveGameHeader))
					{
						engineSaveGameHeader = null;
					}
				}
				else if (version < new Version(1, 7, 21))
				{
					if (!LegacySaveGameHeaderReader.ReadHeader162(binaryReader, engineSaveGameHeader))
					{
						engineSaveGameHeader = null;
					}
				}
				else if (version < new Version(2, 0, 43))
				{
					ModelHeader modelHeader = (ModelHeader)new HeaderReader2019().Read(binaryReader);
					engineSaveGameHeader.Credits = modelHeader.Credits;
					engineSaveGameHeader.NetWorth = modelHeader.NetWorth;
					engineSaveGameHeader.FactionName = modelHeader.FactionName;
					engineSaveGameHeader.GlobalSaveNumber = modelHeader.GlobalSaveNumber;
					engineSaveGameHeader.IsAutoSave = modelHeader.IsAutoSave;
					engineSaveGameHeader.PilotName = modelHeader.PlayerName;
					engineSaveGameHeader.SceneName = modelHeader.PlayerSectorName;
					engineSaveGameHeader.TimeStamp = modelHeader.TimeStamp;
					engineSaveGameHeader.SaveVersion = modelHeader.Version;
					engineSaveGameHeader.CreatedVersion = modelHeader.CreatedVersion;
					engineSaveGameHeader.SaveNumber = modelHeader.SaveNumber;
					engineSaveGameHeader.ScenarioInfoId = modelHeader.ScenarioInfoId;
					engineSaveGameHeader.Permadeath = modelHeader.Permadeath;
					engineSaveGameHeader.GameStartDate = modelHeader.GameStartDate;
					engineSaveGameHeader.SecondsElapsed = modelHeader.SecondsElapsed;
				}
				else
				{
					ModelHeader modelHeader2 = (ModelHeader)new HeaderReader().Read(binaryReader);
					engineSaveGameHeader.ScenarioTitle = modelHeader2.ScenarioTitle;
					engineSaveGameHeader.ScenarioAuthor = modelHeader2.ScenarioAuthor;
					engineSaveGameHeader.ScenarioAuthoringTool = modelHeader2.ScenarioAuthoringTool;
					engineSaveGameHeader.ScenarioDescription = modelHeader2.ScenarioDescription;
					engineSaveGameHeader.Credits = modelHeader2.Credits;
					engineSaveGameHeader.NetWorth = modelHeader2.NetWorth;
					engineSaveGameHeader.FactionName = modelHeader2.FactionName;
					engineSaveGameHeader.GlobalSaveNumber = modelHeader2.GlobalSaveNumber;
					engineSaveGameHeader.IsAutoSave = modelHeader2.IsAutoSave;
					engineSaveGameHeader.PilotName = modelHeader2.PlayerName;
					engineSaveGameHeader.SceneName = modelHeader2.PlayerSectorName;
					engineSaveGameHeader.TimeStamp = modelHeader2.TimeStamp;
					engineSaveGameHeader.SaveVersion = modelHeader2.Version;
					engineSaveGameHeader.CreatedVersion = modelHeader2.CreatedVersion;
					engineSaveGameHeader.SaveNumber = modelHeader2.SaveNumber;
					engineSaveGameHeader.ScenarioInfoId = modelHeader2.ScenarioInfoId;
					engineSaveGameHeader.Permadeath = modelHeader2.Permadeath;
					engineSaveGameHeader.GameStartDate = modelHeader2.GameStartDate;
					engineSaveGameHeader.SecondsElapsed = modelHeader2.SecondsElapsed;
				}
			}
			return engineSaveGameHeader;
		}

		public static bool CanFindHeaders(out EngineSaveGameHeader firstHeader)
		{
			firstHeader = null;
			firstHeader = FindFirstValidGameHeader();
			return firstHeader != null;
		}

		public static EngineSaveGameHeader GetMostRecentSaveGame()
		{
			return (from e in FindSavedGameHeaders()
				orderby e.TimeStamp descending
				select e).FirstOrDefault();
		}

		public static EngineSaveGameHeader[] FindSavedGameHeaders(bool includeIncompatibleVersions = false)
		{
			List<EngineSaveGameHeader> list = new List<EngineSaveGameHeader>();
			foreach (string orderedSaveGame in GetOrderedSaveGames())
			{
				EngineSaveGameHeader engineSaveGameHeader = null;
				try
				{
					engineSaveGameHeader = LoadHeader(orderedSaveGame);
				}
				catch (Exception exception)
				{
					Debug.LogError("Failed to read save game header at path \"" + orderedSaveGame + "\"");
					Debug.LogException(exception);
				}
				if (engineSaveGameHeader != null && (engineSaveGameHeader.CanBeLoaded | includeIncompatibleVersions))
				{
					list.Add(engineSaveGameHeader);
				}
			}
			return list.ToArray();
		}

		public static EngineSaveGameHeader FindFirstValidGameHeader()
		{
			foreach (string orderedSaveGame in GetOrderedSaveGames())
			{
				EngineSaveGameHeader engineSaveGameHeader = null;
				try
				{
					engineSaveGameHeader = LoadHeader(orderedSaveGame);
				}
				catch (Exception exception)
				{
					Debug.LogError("Failed to read save game header at path\"" + orderedSaveGame + "\"");
					Debug.LogException(exception);
				}
				if (engineSaveGameHeader != null)
				{
					return engineSaveGameHeader;
				}
			}
			return null;
		}

		public static bool TryLoadLastSavedGame(ScenarioLoaderUI loader)
		{
			EngineSaveGameHeader mostRecentSaveGame = GetMostRecentSaveGame();
			if (mostRecentSaveGame != null)
			{
				return loader.TryLoadScenario(mostRecentSaveGame);
			}
			return false;
		}

		public static string GetSaveDirectory()
		{
			return Path.Combine(Application.persistentDataPath, "SaveGames");
		}

		public static string GetSavePath(string saveFileName, bool ensureUnique)
		{
			return GetSavePath(saveFileName, Extension, ensureUnique);
		}

		public static string GetSavePath(string saveFileName, string extension, bool ensureUnique)
		{
			string path = saveFileName + "." + extension;
			string saveDirectory = GetSaveDirectory();
			if (ensureUnique && File.Exists(Path.Combine(saveDirectory, path)))
			{
				int num = 1;
				do
				{
					path = $"{saveFileName}({num}).{extension}";
					num++;
				}
				while (File.Exists(Path.Combine(saveDirectory, path)));
			}
			return Path.Combine(saveDirectory, path);
		}

		public static void BackupSaveFile(string savePath)
		{
			string savePath2 = GetSavePath(string.Format("{0}", "backup"), BackupExtension, ensureUnique: false);
			if (File.Exists(savePath2))
			{
				File.Delete(savePath2);
			}
			File.Copy(savePath, savePath2);
			Debug.Log("Backed up old save game to: " + savePath2);
			File.Delete(savePath);
		}

		public static bool IsHeaderFileNameValid(string fileName)
		{
			return !fileName.Contains("backup");
		}

		public static List<string> GetOrderedSaveGames()
		{
			List<string> list = new List<string>();
			string saveDirectory = GetSaveDirectory();
			if (Directory.Exists(saveDirectory))
			{
				string text = "." + Extension;
				foreach (string item2 in from e in Directory.GetFiles(saveDirectory)
					orderby new FileInfo(e).LastWriteTime descending
					select e)
				{
					if (Path.GetExtension(item2) == text)
					{
						string item = Path.Combine(saveDirectory, item2);
						list.Add(item);
					}
				}
			}
			return list;
		}

		public static Unit CreateUnit(EngineASX engine, int id, int unitClassId, int sectorId)
		{
			UnitClass unitClassById = engine.GetUnitClassById(unitClassId);
			if (unitClassById != null)
			{
				Unit component = UnityEngine.Object.Instantiate(unitClassById.UnitPrefab.gameObject).GetComponent<Unit>();
				component.SetUniqueId(id);
				component.Init();
				component.Sector = engine.GetSectorById(sectorId);
				return component;
			}
			Debug.LogError($"EngineIO: Unknown unitclass UniqueID: \"{unitClassId}\"");
			return null;
		}

		public static FleetOrder CreateFleetOrderFromType(FleetOrderType objectiveType)
		{
			return objectiveType switch
			{
				FleetOrderType.AttackTarget => (FleetOrder)UnityObjectHelper.NewGameObject<AttackTargetOrder>(), 
				FleetOrderType.AutonomousTrade => UnityObjectHelper.NewGameObject<AutonomousTradeOrder>(), 
				FleetOrderType.CollectCargo => UnityObjectHelper.NewGameObject<CollectCargoOrder>(), 
				FleetOrderType.Dock => UnityObjectHelper.NewGameObject<DockOrder>(), 
				FleetOrderType.ManualTrade => UnityObjectHelper.NewGameObject<ManualTradeOrder>(), 
				FleetOrderType.Mine => UnityObjectHelper.NewGameObject<MineOrder>(), 
				FleetOrderType.MoveTo => UnityObjectHelper.NewGameObject<MoveToOrder>(), 
				FleetOrderType.Patrol => UnityObjectHelper.NewGameObject<PatrolOrder>(), 
				FleetOrderType.PatrolPath => UnityObjectHelper.NewGameObject<PatrolPathOrder>(), 
				FleetOrderType.RTB => UnityObjectHelper.NewGameObject<ReturnToBaseOrder>(), 
				FleetOrderType.Scavenge => UnityObjectHelper.NewGameObject<ScavengeOrder>(), 
				FleetOrderType.SellCargo => UnityObjectHelper.NewGameObject<SellCargoOrder>(), 
				FleetOrderType.Trade => UnityObjectHelper.NewGameObject<TradeOrder>(), 
				FleetOrderType.Wait => UnityObjectHelper.NewGameObject<WaitOrder>(), 
				FleetOrderType.Undock => UnityObjectHelper.NewGameObject<UndockOrder>(), 
				FleetOrderType.AttackGroup => UnityObjectHelper.NewGameObject<AttackFleetOrder>(), 
				FleetOrderType.JoinFleet => UnityObjectHelper.NewGameObject<JoinFleetOrder>(), 
				FleetOrderType.DisposeCargo => UnityObjectHelper.NewGameObject<DisposeCargoOrder>(), 
				FleetOrderType.Protect => UnityObjectHelper.NewGameObject<ProtectOrder>(), 
				FleetOrderType.AutonomousBountyHunterObjective => UnityObjectHelper.NewGameObject<AutonomousBountyHunterOrder>(), 
				FleetOrderType.AutonomousRoamLocationsObjective => UnityObjectHelper.NewGameObject<AutonomousRoamLocationsOrder>(), 
				FleetOrderType.ManualRepair => UnityObjectHelper.NewGameObject<ManualRepairFleetOrder>(), 
				FleetOrderType.ManualRearm => UnityObjectHelper.NewGameObject<ManualRearmOrder>(), 
				FleetOrderType.RearmAtNearest => UnityObjectHelper.NewGameObject<RearmAtNearestOrder>(), 
				FleetOrderType.AutonomousTransportPassengers => UnityObjectHelper.NewGameObject<AutonomousTransportPassengersOrder>(), 
				FleetOrderType.Explore => UnityObjectHelper.NewGameObject<ExploreOrder>(), 
				FleetOrderType.RepairAtNearest => UnityObjectHelper.NewGameObject<RepairAtNearestStationOrder>(), 
				FleetOrderType.MoveToNearestFriendlyStation => UnityObjectHelper.NewGameObject<MoveToNearestFriendlyStationOrder>(), 
				FleetOrderType.EnterWormhole => UnityObjectHelper.NewGameObject<EnterWormholeOrder>(), 
				FleetOrderType.ExploreSector => UnityObjectHelper.NewGameObject<ExploreSectorOrder>(), 
				FleetOrderType.MoveToSector => UnityObjectHelper.NewGameObject<MoveToSectorOrder>(), 
				FleetOrderType.WaitForAutoRepair => UnityObjectHelper.NewGameObject<WaitForAutoRepairOrder>(), 
				FleetOrderType.BuildStation => UnityObjectHelper.NewGameObject<BuildStationOrder>(), 
				FleetOrderType.ClaimUnit => UnityObjectHelper.NewGameObject<ClaimUnitOrder>(), 
				_ => throw new NotImplementedException($"Cannot create order of type: {(int)objectiveType} {objectiveType}"), 
			};
		}

		public static void MatchCargoCapacityToPrefab(UnitComponentHolder unitComponents)
		{
			UnitComponentHolder component = unitComponents.UnitClass.UnitPrefab.GetComponent<UnitComponentHolder>();
			if (component != null)
			{
				unitComponents.CargoCapacity = component.CargoCapacity;
			}
		}

		public static TriggerBase CreateTriggerFromType(TriggerType triggerType)
		{
			throw new Exception($"Unknown trigger type {triggerType}");
		}
	}
}
