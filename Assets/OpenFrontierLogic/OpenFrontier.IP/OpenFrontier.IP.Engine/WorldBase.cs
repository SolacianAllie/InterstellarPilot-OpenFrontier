using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Comms;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.Bounty;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.Engine.GasClouds;
using OpenFrontier.IP.Engine.MissionObjectives;
using OpenFrontier.IP.Engine.MissionSpecs;
using OpenFrontier.IP.Engine.SaveGame;
using OpenFrontier.IP.Engine.Triggers;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.Engine.UniverseWorld;
using OpenFrontier.IP.Engine.WorldPopulation;
using OpenFrontier.IP.Engine.WorldSeeding;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.ScenarioOptions;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.TraderHeatmapSeed;
using OpenFrontier.IP.Scenarios;
using OpenFrontier.IP.UI;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.WorldBuilding.PersonUtils;
using OpenFrontier.IP.WorldBuilding.UnitUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = System.Random;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine
{
	public class WorldBase : MonoBehaviour
	{
		public enum ScenarioState
		{
			Intro,
			Playing,
			Completing,
			Complete
		}

		public delegate void NewGameHandler(WorldBase sender);

		public delegate void InitialisedHandler(WorldBase sender);

		public const int Scenario_Sandbox = 9952;

		public const int Scenario_Unchartered = 4000;

		public string ScenarioTitle;

		public string ScenarioAuthor;

		public string ScenarioAuthoringTool;

		public string ScenarioDescription;

		public int ScenarioDateDay = 1;

		public int ScenarioDateMonth = 1;

		public int ScenarioDateYear = 2236;

		public int ScenarioDateMinute;

		public int ScenarioDateHour;

		public bool GivePlayerDefaultTitleOnNewGame;

		public bool GivePlayerDefaultFactionNameOnNewGame;

		private bool hasCheated;

		private Version createdVersion;

		private float timeInitialized;

		public bool ShowQuickTractorButtonForIncompatible = true;

		public bool AllowFactionStationBuild = true;

		public bool AllowFactionShipsBuild = true;

		public bool AllowFactionRetire = true;

		public WorldSeeder Seeder;

		public GameObject FactionsRoot;

		public double MinTimeBeforeFactionSpawn = 120.0;

		public GameObject NewGameRoot;

		private int elapsedGameDays;

		private int minCombatDifficultyIndex = -1;

		public static ScenarioLoadData LoadData = null;

		public bool AITradePurchaseRequiresCredits = true;

		public bool AIIgnoreTargettingRange;

		public float AIRequisitionPointMultiplier = 1f;

		[SerializeField]
		private WorldPlayerPermissions permissions;

		[NonSerialized]
		public bool CanLoadLastSave;

		public float CheckObjectivesCompleteInterval = 2f;

		public bool CompleteWhenNoObjectivesLeft;

		public float CompletionInterval = 5f;

		private float completionTime;

		public bool DeathCinematicTimesout;

		private Dictionary<int, DialogBase> dialogsById = new Dictionary<int, DialogBase>();

		private Dictionary<int, DialogStage> dialogStagesById = new Dictionary<int, DialogStage>();

		private EngineASX engine;

		public float GateDistance = 3000f;

		public bool GenerateUnitLoot = true;

		private bool hasInitialised;

		public GamePlayer InitialPlayer;

		[FormerlySerializedAs("InitialScene")]
		public Sector InitialSector;

		protected ScenarioLoadData lastLoadedData;

		private Dictionary<int, MessageTemplate> messageTemplatesById = new Dictionary<int, MessageTemplate>();

		private float nextCheckObjectives;

		private ScenarioState objectiveState;

		public Faction PlayerFaction;

		public GamePlayer PlayerPrefab;

		public int RespawnCredits = 10000;

		public int SaveGameCount;

		public ScenarioInfo ScenarioInfo;

		protected bool scenarioSuccess;

		public bool ShowNetWorthInLog;

		public bool StartAsPilotIfPossible = true;

		public bool UsePilotNamesAsDesignations = true;

		public bool UseScenarioMusic = true;

		public HashSet<int> FleetsBeforeSeed;

		private static List<Mission> missionCache = new List<Mission>(3);

		public EngineASX Engine => engine;

		public bool HasInitialised => hasInitialised;

		public ScenarioState ObjectiveState
		{
			get
			{
				return objectiveState;
			}
			set
			{
				if (objectiveState != value)
				{
					ScenarioState oldState = objectiveState;
					objectiveState = value;
					OnStateChanged(oldState);
				}
			}
		}

		public int ActiveMissionCount
		{
			get
			{
				int num = 0;
				foreach (Mission mission in engine.Missions)
				{
					if (mission.gameObject.activeSelf)
					{
						num++;
					}
				}
				return num;
			}
		}

		public ScenarioLoadData LastLoadedData => lastLoadedData;

		public bool LoadFailed { get; private set; }

		public bool RespawnOnDeath
		{
			get
			{
				if (ScenarioOptions == null)
				{
					return false;
				}
				if (ScenarioOptions.Permadeath)
				{
					return false;
				}
				return ScenarioOptions.RespawnOnDeath switch
				{
					RespawnOnDeathPreference.DontRespawn => false, 
					RespawnOnDeathPreference.Respawn => true, 
					_ => GameController.Instance.RespawnOnDeath, 
				};
			}
		}

		public WorldPlayerPermissions Permissions
		{
			get
			{
				if (permissions != null)
				{
					return permissions;
				}
				return engine.GameSettings.DefaultWorldPermissions;
			}
		}

		public bool ScenarioSuccess => scenarioSuccess;

		public System.Random SeederRandom { get; private set; }

		public Dictionary<int, DialogBase> DialogsById => dialogsById;

		public float TimeInitialized
		{
			get
			{
				return timeInitialized;
			}
			set
			{
				timeInitialized = value;
			}
		}

		public float TimeSinceInitialization => Time.time - timeInitialized;

		public ScenarioOptions ScenarioOptions
		{
			get
			{
				if (Seeder != null && Seeder.Settings != null)
				{
					return Seeder.Settings.ScenarioOptions;
				}
				return null;
			}
		}

		public Version CreatedVersion
		{
			get
			{
				return createdVersion;
			}
			set
			{
				createdVersion = value;
			}
		}

		public bool CanEnterShipFromAnyDistance
		{
			get
			{
				if (GameController.Instance.GameSettings.CanEntershipFromAnyDistance)
				{
					return ScenarioOptions.AllowTeleporting;
				}
				return false;
			}
		}

		public bool HasCheated
		{
			get
			{
				return hasCheated;
			}
			set
			{
				hasCheated = value;
			}
		}

		public event NewGameHandler NewGame;

		public event InitialisedHandler Initialised;

		public void Awake()
		{
			Debug.Log("World: Awake. Loading engine scene...");
			SceneManager.LoadScene(ScreenNames.EngineScene, LoadSceneMode.Additive);
			OnAwake();
		}

		public void Init()
		{
			Time.timeScale = 1f;
			Debug.Log("Initialising World...", this);
			engine = EngineASX.Instance;
			engine.World = this;
			engine.UnitKilled += engine_UnitKilled;
			engine.UnitDestroyed += engine_UnitDestroyed;
			if (Seeder == null)
			{
				Seeder = UnityObjectHelper.NewGameObject<WorldSeeder>(transform);
				Seeder.name = "WorldSeeder";
			}
			if (Seeder.Settings == null)
			{
				Seeder.Settings = UnityEngine.Object.Instantiate(GameController.Instance.GameSettings.DefaultWorldSeedSettings, Seeder.transform);
			}
			if (ScenarioInfo == null)
			{
				ScenarioInfo = GameController.Instance.CustomScenarioInfo;
			}
			FindMessages(NewGameRoot.transform);
			FindDialogStages(NewGameRoot.transform);
			FindDialogs(NewGameRoot.transform);
			engine.SetAllScenesActive(active: true);
			if (LoadData != null && LoadData.ScenarioInfo != null)
			{
				ScenarioInfo = LoadData.ScenarioInfo;
			}
			if (LoadData != null)
			{
				lastLoadedData = LoadData;
			}
			else
			{
				lastLoadedData = CreateDefaultLoadData();
				if (lastLoadedData != null)
				{
					lastLoadedData.SaveVersion = EngineIO.SaveVersion;
				}
			}
			if (ScenarioInfo == null)
			{
				Debug.LogWarning($"World does not have a scenario info");
			}
			else
			{
				ScenarioDateYear = ScenarioInfo.DateYear;
				ScenarioDateMonth = ScenarioInfo.DateMonth;
				ScenarioDateDay = ScenarioInfo.DateDay;
				ScenarioDateHour = ScenarioInfo.DateHour;
				ScenarioDateMinute = ScenarioInfo.DateMinute;
			}
			bool flag = LoadData != null && !string.IsNullOrEmpty(LoadData.FullSaveGamePath);
			bool flag2 = false;
			try
			{
				if (flag)
				{
					EngineASX.Instance.ShipNames.ClearAndRandomize(new System.Random());
					EngineASX.Instance.FactionNames.ClearAndRandomize(new System.Random());
					Debug.Log($"World loading savegame data from : \"{LoadData.FullSaveGamePath}\"", this);
					DestroyNewGameUnits();
					OnLoadGame();
					if (Debug.isDebugBuild)
					{
						EngineASX.Instance.StationBuildDistanceValidator.Validate();
					}
					FixIncorrectScenarioInfo();
				}
				else
				{
					Debug.Log("World did not find any savegame data. Calling OnNewGAme", this);
					OnNewGameStarting();
				}
				flag2 = true;
				LoadFailed = false;
			}
			catch (Exception ex)
			{
				LoadFailed = true;
				string text = "Failed to start game";
				if (Debug.isDebugBuild || Versioning.IsAlphaVersion)
				{
					text = text + "\n" + ex.ToString();
				}
				UIController.Instance.ShowMessageBox(text, MessageBoxButtons.Ok, OnFailedToLoadSaveGame, MessageBoxIcon.Error);
				Debug.LogException(ex);
			}
			if (flag2)
			{
				OnWorldInitSuccessful(flag);
			}
		}

		protected internal virtual bool OnAboutToKillPerson(Person person)
		{
			if (person.IsLocalPlayer)
			{
				bool flag = OnAboutToKillLocalPlayer(person);
				if (flag)
				{
					EngineIO.TryDeletePreviousSaveFileWhenPermadeath(engine);
				}
				return flag;
			}
			return true;
		}

		public bool TryRespawn(Person person)
		{
			Unit unit = FindPlayerRespawnUnit(person);
			if (unit != null)
			{
				EngineASX.Instance.ChangePlayerUnit(unit);
				return true;
			}
			return false;
		}

		protected internal virtual bool OnAboutToKillLocalPlayer(Person person)
		{
			if (RespawnOnDeath)
			{
				Unit unit = FindPlayerRespawnUnit(person);
				if (unit != null)
				{
					person.Deaths++;
					EngineASX.Instance.ChangePlayerUnit(unit);
					UIController.Instance.QuickMsg.ClearMessages();
					UIController.Instance.QuickMsg.AddMessage("You were killed");
					return false;
				}
			}
			return true;
		}

		public void OnCombatDifficultyChanged()
		{
			if (minCombatDifficultyIndex < 0)
			{
				minCombatDifficultyIndex = GameController.Instance.CombatDifficultyLevelIndex;
			}
			else
			{
				minCombatDifficultyIndex = Mathf.Min(minCombatDifficultyIndex, GameController.Instance.CombatDifficultyLevelIndex);
			}
		}

		public Unit FindPlayerRespawnUnit(Person person)
		{
			Sector sector = person.Sector;
			if (sector != null)
			{
				Unit bestRespawnShipInSector = GetBestRespawnShipInSector(sector, person, person.Faction);
				if (bestRespawnShipInSector != null)
				{
					return bestRespawnShipInSector;
				}
				Unit bestRespawnStationInSector = GetBestRespawnStationInSector(person.Sector, person, person.Faction);
				if (bestRespawnStationInSector != null)
				{
					return bestRespawnStationInSector;
				}
				foreach (Sector item in from e in EngineASX.Instance.Sectors.Where((Sector e) => e != sector && e.GetJumpDistanceTo(sector) > -1).ToList()
					orderby e.GetJumpDistanceTo(sector)
					select e)
				{
					bestRespawnShipInSector = GetBestRespawnShipInSector(item, person, person.Faction);
					if (bestRespawnShipInSector != null)
					{
						return bestRespawnShipInSector;
					}
					bestRespawnShipInSector = GetBestRespawnStationInSector(item, person, person.Faction);
					if (bestRespawnShipInSector != null)
					{
						return bestRespawnShipInSector;
					}
				}
				return null;
			}
			return null;
		}

		public Unit GetBestRespawnShipInSector(Sector sector, Person person, Faction personFaction)
		{
			Unit unit = null;
			float num = 0f;
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Ship);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item.IsPilottable() && item.Faction == person.Faction)
					{
						float num2 = 0f;
						num2 -= Vector3.Distance(item.transform.position, person.transform.position);
						if (unit == null || num2 > num)
						{
							unit = item;
							num = num2;
						}
					}
				}
			}
			return unit;
		}

		public Unit GetBestRespawnStationInSector(Sector sector, Person person, Faction personFaction)
		{
			Unit unit = null;
			float num = 0f;
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item.IsValidAndNotDestroyed && !item.IsHostileToOrAlwaysHostileToTwoWay(personFaction) && item.IsDockable)
					{
						float num2 = 0f;
						switch (item.UnitClass.StationPurpose)
						{
						case StationPurpose.TradeStation:
							num2 += 10f;
							break;
						case StationPurpose.Shipyard:
							num2 += 5f;
							break;
						case StationPurpose.Factory:
						case StationPurpose.Scrapyard:
							num2 += 2.5f;
							break;
						}
						num2 += UnityEngine.Random.value * 5f;
						if (!person.Faction.Intel.IsUnitDiscovered(item))
						{
							num2 -= 50f;
						}
						if (item.Faction == personFaction)
						{
							num2 += 20f;
						}
						num2 -= Mathf.Clamp01(Vector3.Distance(item.transform.position, person.transform.position) / 16000f) * 10f;
						if (unit == null || num2 > num)
						{
							unit = item;
							num = num2;
						}
					}
				}
			}
			return unit;
		}

		private void OnWorldInitSuccessful(bool isLoadedGame)
		{
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				GasCloudHelper.FindGasCloudForUnitsInSector(sector);
			}
			RemoveInvalidRotations();
			ApplyScenarioOptions();
			EngineASX.Instance.AllFactionsDiscoverOwnUnits();
			EngineASX.Instance.RefreshSectorContentType();
			EngineASX.Instance.RecalculateAllNetWorths();
			StartPlayerAsPilotIfPossible();
			RefreshElapsedGameWorldDays();
			CalculateSectorSecurityLevels();
			CalculateSectorStaticInfo();
			CalculateSectorControl();
			CacheAllTraderBoughtCargoTypes();
			if (isLoadedGame)
			{
				TraderHeatmapSeeder.SeedHeatmap();
			}
			CreateAndInitFactions();
			if (EngineASX.Instance.LocalFaction != null)
			{
				EngineASX.Instance.LocalFaction.CreateStatsIfNull();
			}
			if (isLoadedGame && GivePlayerDefaultFactionNameOnNewGame)
			{
				RepairPlaceholderPlayerFactionName();
			}

			OnInit();
			if (Initialised != null)
			{
				try
				{
					Initialised(this);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
			if (!isLoadedGame)
			{
				DiscoverStationsAndFactionsInRange();
			}
			CacheAllFactionValidTraders();
			if (engine.ActiveSector != null)
			{
				engine.ActiveSector.UpdateAllUnitsVisibility();
			}
			minCombatDifficultyIndex = GameController.Instance.CombatDifficultyLevelIndex;
			AutoAssignShipNames();
			Engine.NotifyWorldLoadedAndReady();
			RandomizeTradeSearchStartTimes();
			RandomizeFactionIntelScanStartTimes();
			LoadData = null;
			hasInitialised = true;
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				faction.NextFactionAIUpdate = Time.time + UnityEngine.Random.value * GameController.Instance.GameSettings.PerformanceSettings.FactionAIUpdateInterval;
			}
			RecordFactionStartingStats(EngineASX.Instance.Factions);
			if (isLoadedGame)
			{
				PerformImmediateScanFromAllUnits(silent: true);
			}
			TimeInitialized = 0f;
			Debug.Log("Initialising World: Finished", this);
		}

		/// <summary>
		/// Puts the player's chosen faction name back if the save has only
		/// the placeholder.
		/// </summary>
		/// <remarks>
		/// A repair rather than a fix. The cause is known - the creation
		/// autosave is written before the name exists - and the name is now
		/// applied before it, but a save written by an earlier build still
		/// says 'Player', and a player who has one should not have to replay
		/// a game to get their name back.
		///
		/// Matched on the placeholder alone, and not on the name being
		/// generated or custom, because those do not distinguish a name that
		/// was lost from one that was chosen. The cost is a player who
		/// deliberately called their faction 'Player' gets the default
		/// instead, which is a smaller mistake than the one being fixed and
		/// is undone by renaming.
		/// </remarks>
		private void RepairPlaceholderPlayerFactionName()
		{
			Faction playerFaction = EngineASX.Instance.LocalFaction;

			if (playerFaction == null
				|| !string.Equals(playerFaction.Name, PlaceholderFactionName,
					StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			Debug.Log("Faction: this player's faction was saved as '"
				+ playerFaction.Name + "', so the chosen name was lost - putting '"
				+ GameController.Instance.DefaultFactionName + "' back");

			ApplyDefaultPlayerFactionName();
		}

		/// <summary>The name a faction carries before the player has one.</summary>
		private const string PlaceholderFactionName = "Player";

		private void ApplyDefaultPlayerFactionName()
		{
			if (!(EngineASX.Instance.LocalFaction == null) && (!(ScenarioInfo != null) || ScenarioInfo.UniqueId != 4000 || EngineASX.Instance.LocalFaction.UniqueId == 8000 || EngineASX.Instance.LocalFaction.UniqueId >= 100000))
			{
				Faction playerFaction = EngineASX.Instance.LocalFaction;

				// The name has to become CUSTOM, not merely be assigned.
				//
				// A faction's name is only written to a save when it is custom
				// - when GeneratedNameId is negative - and this was setting
				// the name while leaving that id alone. Two saves were read
				// straight off disk to confirm it: both contain the text, and
				// neither contains it as a stored string. The name generator
				// runs later in world creation, assigns a generated name, and
				// in doing so orphans the name that was set here - so it
				// comes back from a save as whatever the pool gave it, or as
				// the prefab's, which is why it looked like a reset to
				// 'Player'.
				playerFaction.ClearGeneratedName();

				playerFaction.Name = GameController.Instance.DefaultFactionName;
				playerFaction.ShortName = GameController.Instance.DefaultFactionShortName;
			}
		}

		private void RemoveInvalidRotations()
		{
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Ship);
				if (unitsByType != null)
				{
					foreach (Unit item in unitsByType)
					{
						if (item.ActiveUnit == null || item.ActiveUnit.ActiveUnitShip == null || item.ActiveUnit.ActiveUnitShip.CurrentTurn == 0f)
						{
							item.ClearZAndXRotation();
						}
						else
						{
							item.ClearXRotation();
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
					item2.ClearZAndXRotation();
				}
			}
		}

		public void AutoAssignShipNames()
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
					if (string.IsNullOrWhiteSpace(item.Components.ShipName) && item.Components.RequiresShipName())
					{
						item.Components.AutoAssignShipName();
					}
				}
			}
		}

		private static void AutoAssignPlayerPersonTitle()
		{
			if (EngineASX.Instance.LocalPlayer != null && string.IsNullOrWhiteSpace(EngineASX.Instance.LocalPlayer.Person.CustomTitle))
			{
				EngineASX.Instance.LocalPlayer.Person.CustomTitle = GameController.Instance.DefaultPilotTitle;
			}
		}

		private void PerformImmediateScanFromAllUnits(bool silent)
		{
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				foreach (Unit unit in faction.Units)
				{
					if (unit.Sector != null && unit.Components != null && unit.Components.ShouldPerformScan())
					{
						unit.Components.PerformImmediateScan(callOnNewUnitScanned: false);
					}
				}
			}
		}

		public virtual void OnPlayerFleetCreated(Fleet fleet)
		{
		}

		private void ApplyScenarioOptions()
		{
			ScenarioOptions.ApplyOptions(this);
		}

		private void RandomizeTradeSearchStartTimes()
		{
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				foreach (Fleet fleet in faction.Fleets)
				{
					if (fleet.ActiveOrder is ActiveAutonomousTradeOrder activeAutonomousTradeOrder)
					{
						activeAutonomousTradeOrder.NextSearchTradeRoutes = Time.time + UnityEngine.Random.value * 12f;
					}
				}
			}
		}

		private void CalculateSectorStaticInfo()
		{
			EngineASX.Instance.RefreshSectorDistancesFromUniverseCenter();
		}

		private void RandomizeFactionIntelScanStartTimes()
		{
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (faction.IntelScanner != null)
				{
					faction.IntelScanner.LastScanStartTime = Time.time + UnityEngine.Random.value * EngineASX.Instance.GameSettings.PerformanceSettings.FactionIntelScanInterval;
				}
				foreach (Unit unit in faction.Units)
				{
					if (unit.Components != null)
					{
						float num = (unit.IsInActiveSector ? GameController.Instance.GameSettings.IntelSettings.UnitScanWhenActiveFrequency : GameController.Instance.GameSettings.IntelSettings.UnitScanWhenInactiveFrequency);
						unit.Components.NextScanTime = Time.time + UnityEngine.Random.value * num;
					}
				}
			}
		}

		public void CalculateSectorSecurityLevels()
		{
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				sector.RefreshSecurityLevel();
			}
		}

		public void CalculateSectorControl()
		{
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				CalculateSectorControl(sector);
			}
		}

		public void CalculateSectorControl(Sector sector)
		{
			sector.ChangeControllingFaction(GetSectorControllingFaction(sector), setTimeOfChange: false);
		}

		public Faction GetSectorControllingFaction(Sector sector)
		{
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item.Faction != null && item.UnitClass.StationPurpose == StationPurpose.SectorControl)
					{
						return item.Faction;
					}
				}
			}
			return null;
		}

		private void StartPlayerAsPilotIfPossible()
		{
			if (StartAsPilotIfPossible)
			{
				GamePlayer localPlayer = engine.LocalPlayer;
				if (localPlayer != null && !localPlayer.Person.IsPilot && localPlayer.Person.CurrentUnit != null && localPlayer.Person.CurrentUnit.UnitClass.IsPilottable && !OrdersHelper.IsPilottedByNpc(localPlayer.Person.CurrentUnit))
				{
					localPlayer.Person.CurrentUnit.Components.PilotPerson = localPlayer.Person;
				}
			}
		}

		private void CreateAndInitFactions()
		{
			foreach (Faction faction in engine.Factions)
			{
				if (faction.IsPlayerFaction && faction.LeaderPerson == null && EngineASX.Instance.LocalPlayer != null)
				{
					faction.LeaderPerson = EngineASX.Instance.LocalPlayer.Person;
				}
				faction.CreateOrInitFactionAIIfNeeded();
				NameFactionIfNeeded(faction);
				if (faction.FactionAI != null)
				{
					faction.FactionAI.OnWorldInit();
				}
				faction.CreateTradeNetworkIfRequired();
			}
		}

		private void CacheAllTraderBoughtCargoTypes()
		{
			foreach (CargoTrader trader in engine.Traders)
			{
				trader.CacheCargoTypesIfNeeded();
			}
		}

		private void FixIncorrectScenarioInfo()
		{
			if (ScenarioInfo != null && ScenarioInfo.UniqueId == 4000 && engine.Sectors.Any((Sector e) => e.Name.Contains("Siris")))
			{
				ScenarioInfo scenarioInfo = GameController.Instance.LoadedScenarioInfos.FirstOrDefault((ScenarioInfo e) => e.UniqueId == 4050);
				if (scenarioInfo != null)
				{
					ScenarioInfo = scenarioInfo;
				}
			}
		}

		private void RefreshElapsedGameWorldDays()
		{
			elapsedGameDays = engine.DateTimeUtils.GetGameWorldElapsedDays();
		}

		private void OnFailedToLoadSaveGame(MessageBoxScreen sender, MessageBoxResult result)
		{
			OpenFrontier.IP.UI.UI.QuitToMainMenu();
		}

		public void NotifyPlayerRequireRetry()
		{
			ReactiveQuickMsgr();
			GameController.Instance.ScenarioLoader.TryLoadScenario(lastLoadedData);
		}

		public void DiscoverStationsAndFactionsInRange()
		{
			if (engine.Hud != null && engine.LocalUnit != null && engine.LocalFaction != null && engine.LocalUnit.Sector != null)
			{
				engine.LocalFaction.Intel.PerformScan(engine.LocalUnit.Sector, engine.LocalUnit.SectorPosition, engine.LocalUnit.Components.ScanRange + engine.LocalUnit.UnitClass.ShieldRingRadius, engine.LocalUnit);
				engine.LocalFaction.IntelProcessor.ProcessAll();
			}
		}

		public void DiscoverStationUnitsInScene()
		{
			if (engine.LocalPlayerSector != null)
			{
				engine.LocalPlayer.Faction.Intel.DiscoverStationsInSector(engine.LocalPlayerSector);
			}
		}

		public void DiscoverStaticUnitsInCurrentScene()
		{
			if (engine.LocalPlayerSector != null)
			{
				engine.LocalPlayer.Faction.Intel.DiscoverUnitsInSector(engine.LocalPlayerSector);
			}
		}

		public void DiscoverNormalJumpGatesInScene()
		{
			if (!(engine.LocalPlayerSector != null))
			{
				return;
			}
			List<Unit> unitsByType = engine.LocalPlayerSector.GetUnitsByType(UnitType.Wormhole);
			if (unitsByType == null)
			{
				return;
			}
			foreach (Unit item in unitsByType)
			{
				if (!item.WormholeComponent.IsUnstable)
				{
					engine.LocalFaction.Intel.DiscoverUnit(item, setTimeOfDiscovery: false);
				}
			}
		}

		public virtual bool CanCompleteMission(Mission mission)
		{
			if (objectiveState == ScenarioState.Playing && engine.PlayerUnit != null)
			{
				return !engine.PlayerUnit.IsDestroyed;
			}
			return false;
		}

		[ContextMenu("Force Win")]
		public void ForceWin()
		{
			OnScenarioCompleting(success: true);
		}

		[ContextMenu("Force Fail")]
		public void ForceFail()
		{
			OnScenarioCompleting(success: false);
		}

		[ContextMenu("Destroy Player Enemies")]
		public void DestroyPlayerEnemies()
		{
			if (!(engine.LocalPlayer != null) || !(engine.LocalPlayer.Faction != null))
			{
				return;
			}
			foreach (Faction faction in engine.Factions)
			{
				if (!faction.IsHostileTo(engine.LocalPlayer.Faction))
				{
					continue;
				}
				Unit[] array = faction.Units.ToArray();
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i].gameObject.activeInHierarchy && array[i].Destructable != null)
					{
						array[i].Destructable.KillAnonymously();
					}
				}
			}
		}

		public virtual void OnMissionObjectiveComplete(Mission mission, MissionObjective mo, bool success)
		{
			if (!engine.GameSettings.ShowObjectiveMsgs || !mission.BroadcastMessages || !mo.ShowInJournal)
			{
				return;
			}
			if (success)
			{
				string msg = "Objective Complete";
				if (!string.IsNullOrEmpty(mo.Title))
				{
					msg = $"Objective Complete \"{mo.Title}\"";
				}
				UIController.Instance.QuickMsg.AddMessage(msg);
			}
			else
			{
				string msg2 = "Objective Failed";
				if (!string.IsNullOrEmpty(mo.Title))
				{
					msg2 = $"Objective Failed  \"{mo.Title}\"";
				}
				UIController.Instance.QuickMsg.AddMessage(msg2);
			}
		}

		public virtual void OnMissionComplete(Mission mission, bool success)
		{
			if (success)
			{
				if (mission.BroadcastMessages && engine.GameSettings.ShowMissionMsgs)
				{
					string msg = "Mission Complete";
					string text = mission.CalculateTitle();
					if (!string.IsNullOrEmpty(text))
					{
						msg = $"Mission Complete \"{text}\"";
					}
					UIController.Instance.QuickMsg.AddMessage(msg, 2f);
					if (mission.MissionRewardCredits > 0)
					{
						engine.AddCreditsToPlayerFactionWithMsg(mission.MissionRewardCredits, FactionTransactionType.Mission, mission.MissionGiverFaction);
					}
				}
				if (mission.IsPrimary)
				{
					OnScenarioCompleting(success: true);
				}
				return;
			}
			if (mission.IsPrimary)
			{
				OnScenarioCompleting(success: false);
			}
			if (mission.BroadcastMessages && engine.GameSettings.ShowMissionMsgs)
			{
				string text2 = mission.CalculateTitle();
				string msg2 = "Mission Failed";
				if (!string.IsNullOrEmpty(text2))
				{
					msg2 = $"Mission Failed \"{text2}\"";
				}
				UIController.Instance.QuickMsg.AddMessage(msg2, 2f);
			}
		}

		public void NotifyLocalPlayerKilled()
		{
			OnLocalPlayerKilled();
		}

		public GamePlayer SpawnNewPlayer()
		{
			Debug.Log("World is spawning player...");
			return SpawnNewPlayerInternal();
		}

		public void InitMissions(Transform transform)
		{
			Mission[] componentsInChildren = transform.GetComponentsInChildren<Mission>(includeInactive: true);
			foreach (Mission mission in componentsInChildren)
			{
				if (mission.OwnerFaction == null)
				{
					mission.OwnerFaction = EngineASX.Instance.LocalFaction;
				}
				mission.Init();
				mission.InitObjectives();
				if (mission.gameObject.activeSelf)
				{
					mission.RecordStartTime();
				}
			}
		}

		public void InitNewGameObjects()
		{
			Debug.Log("World: InitObjects", this);
			if (NewGameRoot != null)
			{
				Sector[] componentsInChildren = NewGameRoot.GetComponentsInChildren<Sector>();
				Sector[] array = componentsInChildren;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].Init();
				}
				SectorPositioner.PositionSectors(componentsInChildren, GameController.Instance.GameSettings.UniverseBoundsSettings);
				Faction[] componentsInChildren2 = NewGameRoot.GetComponentsInChildren<Faction>();
				for (int j = 0; j < componentsInChildren2.Length; j++)
				{
					componentsInChildren2[j].Init();
					componentsInChildren2[j].CreateOrInitFactionAIIfNeeded();
				}
				InitMissions(transform);
				InitMissions(NewGameRoot.transform);
				InitNewGameUnits();
				foreach (Sector sector2 in EngineASX.Instance.Sectors)
				{
					List<Unit> unitsByType = sector2.GetUnitsByType(UnitType.Wormhole);
					if (unitsByType == null)
					{
						continue;
					}
					foreach (Unit item in unitsByType)
					{
						item.WormholeComponent.InitGateTargets();
					}
				}
				foreach (Sector sector3 in EngineASX.Instance.Sectors)
				{
					List<Unit> unitsByType2 = sector3.GetUnitsByType(UnitType.Wormhole);
					if (unitsByType2 == null)
					{
						continue;
					}
					foreach (Unit item2 in unitsByType2)
					{
						if (item2.WormholeComponent.IsUnstable)
						{
							if (item2.WormholeComponent.NextUnstableChgTargetTime <= 0.0)
							{
								item2.WormholeComponent.RandomizeTargetAndSetNextChangeTime();
							}
							item2.WormholeComponent.InitGateTargets();
						}
					}
				}
				array = componentsInChildren;
				foreach (Sector sector in array)
				{
					if (sector.LightRotation == Quaternion.identity)
					{
						sector.LightRotation = SectorCreator.SetSectorLightDirectionFromSeed(sector);
					}
					AIPatrolPath[] componentsInChildren3 = sector.GetComponentsInChildren<AIPatrolPath>(includeInactive: true);
					for (int k = 0; k < componentsInChildren3.Length; k++)
					{
						componentsInChildren3[k].Init();
					}
					Fleet[] componentsInChildren4 = sector.GetComponentsInChildren<Fleet>(includeInactive: true);
					foreach (Fleet fleet in componentsInChildren4)
					{
						fleet.Sector = sector;
						fleet.Init();
						if (fleet.ShouldZeroPositionY)
						{
							fleet.ZeroPositionY();
						}
					}
					Person[] componentsInChildren5 = sector.GetComponentsInChildren<Person>(includeInactive: true);
					foreach (Person person in componentsInChildren5)
					{
						person.Init();
						person.RefreshName();
						person.FindRankingSystemRank();
						person.FindCurrentUnit();
						OnInitNewGamePerson(person);
					}
					Unit[] componentsInChildren6 = sector.GetComponentsInChildren<Unit>(includeInactive: true);
					foreach (Unit unit in componentsInChildren6)
					{
						if (unit.Components != null && unit.Components.PilotPerson != null)
						{
							unit.Components.OnPilotChanged(null);
						}
					}
					FleetSpawner[] componentsInChildren7 = sector.GetComponentsInChildren<FleetSpawner>(includeInactive: true);
					for (int k = 0; k < componentsInChildren7.Length; k++)
					{
						componentsInChildren7[k].Init();
					}
					NpcPilot[] componentsInChildren8 = sector.GetComponentsInChildren<NpcPilot>(includeInactive: true);
					for (int k = 0; k < componentsInChildren8.Length; k++)
					{
						componentsInChildren8[k].Init();
					}
					FleetOrder[] componentsInChildren9 = sector.GetComponentsInChildren<FleetOrder>(includeInactive: true);
					for (int k = 0; k < componentsInChildren9.Length; k++)
					{
						componentsInChildren9[k].Init();
					}
					foreach (Faction faction in EngineASX.Instance.Factions)
					{
						foreach (Fleet fleet2 in faction.Fleets)
						{
							fleet2.SetPositionToLeaderShipPosition();
						}
					}
				}
				foreach (Sector sector4 in EngineASX.Instance.Sectors)
				{
					List<Unit> unitsByType3 = sector4.GetUnitsByType(UnitType.Ship);
					if (unitsByType3 == null)
					{
						continue;
					}
					foreach (Unit item3 in unitsByType3)
					{
						if (item3.Components.PilotPerson != null && item3.Components.PilotPerson.Faction != null && item3.Faction == null)
						{
							item3.Faction = item3.Components.PilotPerson.Faction;
						}
					}
				}
				foreach (Faction faction2 in EngineASX.Instance.Factions)
				{
					MissionSpec[] componentsInChildren10 = faction2.GetComponentsInChildren<MissionSpec>(includeInactive: true);
					for (int i = 0; i < componentsInChildren10.Length; i++)
					{
						componentsInChildren10[i].Init();
					}
				}
				CalculateSectorControl();
				FindTriggerGroups(transform);
				FindTriggerGroups(NewGameRoot.transform);
				Debug.Log($"World found {EngineASX.Instance.TriggerGroupCount} trigger groups", this);
			}
			else if (LogWrapper.LogMsgs)
			{
				Debug.LogError("Cannot init objects. NewGameRoot not found", this);
			}
			Debug.Log("World: InitObjects complete", this);
		}

		public void OnInitNewGamePerson(Person person)
		{
			if (!(person.Faction != null))
			{
				return;
			}
			PersonBountySeeder component = person.GetComponent<PersonBountySeeder>();
			if (!(component != null))
			{
				return;
			}
			foreach (PersonBountySeedItem bountyItem in component.BountyItems)
			{
				if (bountyItem.Bounty >= 0 && bountyItem.FactionPlacingBounty != null)
				{
					FactionBountyBoard factionBountyBoard = null;
					if (bountyItem.BountyBoardFaction != null && bountyItem.BountyBoardFaction != person.Faction && bountyItem.BountyBoardFaction.BountyBoard != null)
					{
						factionBountyBoard = bountyItem.BountyBoardFaction.BountyBoard;
					}
					if (factionBountyBoard == null && person.Sector != null)
					{
						factionBountyBoard = EngineASX.Instance.GetNearestBountyBoardToSector(person.Sector);
					}
					if (factionBountyBoard != null)
					{
						bool updateLastKnownPosition = bountyItem.ProbabilityOfUpdatingLastKnownPosition >= 1f || UnityEngine.Random.value < bountyItem.ProbabilityOfUpdatingLastKnownPosition;
						BountyHelper.AddBounty(bountyItem.FactionPlacingBounty, factionBountyBoard, person, bountyItem.Bounty, EngineASX.Instance.ScenarioElapsedTime, updateLastKnownPosition);
					}
				}
			}
		}

		private static void InitNewGameUnits()
		{
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				Unit[] componentsInChildren = sector.GetComponentsInChildren<Unit>(includeInactive: true);
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					InitNewGameUnit(componentsInChildren[i]);
				}
			}
		}

		private static void ValidateNewGameUnit(Unit unit)
		{
			if (unit.IsNormalShip())
			{
				if (unit.Components.PowerGenerator == null)
				{
					Debug.LogWarning($"{unit} is missing a power generator", unit);
				}
				if (unit.Components.Capacitor == null)
				{
					Debug.LogWarning($"{unit} is missing a capacitor", unit);
				}
				if (unit.Components.EngineComponent == null)
				{
					Debug.LogWarning($"{unit} is missing an engine", unit);
				}
			}
		}

		public static void InitNewGameUnit(Unit unit)
		{
			unit.Init();
			if (unit.ShouldZeroPositionY)
			{
				unit.ZeroPositionY();
			}
			if (unit.Components != null)
			{
				if (unit.GetComponent<RemoveAllUnitTurrets>() != null)
				{
					foreach (ComponentBay bay in unit.Components.Bays)
					{
						if (bay.BayType.BayType != BayType.Turret)
						{
							unit.Components.InstallBayDefaultComponent(bay, checkForOverride: true);
						}
					}
				}
				else
				{
					unit.Components.InstallDefaultComponents(checkForOverride: true);
				}
				unit.Components.RefreshIsModded();
				unit.Components.AddDefaultCargoLoadout();
				if (string.IsNullOrWhiteSpace(unit.Components.ShipName) && unit.Components.RequiresShipName())
				{
					unit.Components.AutoAssignShipName();
				}
			}
			if (unit.IsUnderConstruction)
			{
				unit.Components.SetHealthForConstructionProgress();
			}
			UnitGasCloud component = unit.GetComponent<UnitGasCloud>();
			if (component != null)
			{
				component.ApplyRadius();
			}
			UnitSpawnInfo component2 = unit.GetComponent<UnitSpawnInfo>();
			if (component2 != null)
			{
				component2.Apply();
			}
			if (unit.CargoComponent != null && unit.CargoComponent.CargoClass != null)
			{
				unit.CargoComponent.SetSpawnTime();
			}
			UnitCustomMass component3 = unit.GetComponent<UnitCustomMass>();
			if (component3 != null)
			{
				unit.Mass *= component3.Multiplier;
			}
		}

		private static void RecordFactionStartingStats(IEnumerable<Faction> factions)
		{
			foreach (Faction faction in factions)
			{
				faction.RecordStartingStats();
			}
		}

		private static void NameFactionIfNeeded(Faction faction)
		{
			if (!faction.IsPlayerFaction && string.IsNullOrEmpty(faction.Name))
			{
				FactionSpawner.AssignFactionName(faction, faction.FactionType);
			}
		}

		public bool HasIncompleteMissions()
		{
			foreach (Mission mission in engine.Missions)
			{
				if (!mission.IsFinished)
				{
					return true;
				}
			}
			return false;
		}

		public void NotifyPlayerRequireExit(bool continueScenario)
		{
			Debug.Log(string.Format("World: Player requires exit scenario. Continue: " + continueScenario), this);
			ReactiveQuickMsgr();
			if (continueScenario)
			{
				if (GameController.Instance.MusicPlayer != null)
				{
					GameController.Instance.MusicPlayer.ClearAllTracksWithFadeOut();
				}
				ObjectiveState = ScenarioState.Playing;
				engine.SetUIFromPlayerStatus();
				SetPlayingScenario(playing: true);
			}
			else
			{
				ExitScenario();
			}
		}

		public DialogStage GetDialogStageById(int id)
		{
			DialogStage value = null;
			if (dialogStagesById.TryGetValue(id, out value))
			{
				return value;
			}
			return null;
		}

		public DialogBase GetDialogById(int id)
		{
			DialogBase value = null;
			if (dialogsById.TryGetValue(id, out value))
			{
				return value;
			}
			return null;
		}

		public MessageTemplate GetMessageDataById(int id)
		{
			MessageTemplate value = null;
			if (messageTemplatesById.TryGetValue(id, out value))
			{
				return value;
			}
			return null;
		}

		protected virtual ScenarioLoadData CreateDefaultLoadData()
		{
			return new ScenarioLoadData
			{
				FullSaveGamePath = null,
				ScenarioInfo = ScenarioInfo
			};
		}

		protected virtual void OnInit()
		{
			if (engine.PlayerUnit != null)
			{
				ObjectiveState = ScenarioState.Intro;
				Engine.StartIntroCinematic();
			}
		}

		protected virtual void OnAwake()
		{
		}

		protected virtual void OnUnitDestroyed(Unit unit)
		{
			if (unit.Sector != null)
			{
				if (unit.Components != null && unit.UnitType != UnitType.Station && UnityEngine.Random.value < GameController.Instance.GameSettings.LootSettings.ShipDropCargoProbability)
				{
					unit.Components.DropCargoOnDestroyed();
				}
				if (UnityEngine.Random.value < GameController.Instance.GameSettings.LootSettings.ShipLootProbability && unit.IsStationOrShip())
				{
					unit.GenerateLoot();
				}
			}
		}

		protected virtual void OnUnitKilled(Unit unit, Faction attackerFaction)
		{
		}

		public void OnNewGameStarting()
		{
			if (LastLoadedData.CustomSeed < 0)
			{
				lastLoadedData.CustomSeed = UnityEngine.Random.Range(0, int.MaxValue);
			}
			UnityEngine.Random.InitState(lastLoadedData.CustomSeed);
			SeederRandom = new System.Random(lastLoadedData.CustomSeed);
			EngineASX.Instance.ShipNames.ClearAndRandomize(SeederRandom);
			EngineASX.Instance.FactionNames.ClearAndRandomize(SeederRandom);
			createdVersion = Versioning.Version;
			EngineASX.Instance.FactionSpawner.NextFactionSpawnTime = EngineASX.Instance.ScenarioElapsedTime + MinTimeBeforeFactionSpawn;
			EngineASX.Instance.FactionSpawner.NextFreelancerSpawnTime = EngineASX.Instance.ScenarioElapsedTime + (double)(UnityEngine.Random.value * EngineASX.Instance.FactionSpawner.Settings.TimeBetweenMinorFactionSpawns);
			InitNewGameObjects();
			ApplyFactionSetupOnNewGame();
			Debug.Log($"Populating economy: Found {engine.Traders.Count} traders", this);
			SeedWorldOnNewGame();
			SetInitialScene();
			PowerupFactionStations();
			RandomizeEmptyPilotNames();
			CallFactionAIOnNewGame();

			// Before OnNewGame, because that is where this autosaves.
			//
			// A new game writes a save the moment it is created, so
			// anything not yet true at this point is not in that file.
			// The default faction name used to be applied in Init, some
			// two hundred lines further on and well after this save, which
			// is why a player who started a game and then left - without
			// saving by hand, because there was nothing to save yet - came
			// back to the prefab's 'Player'. The name had been correct the
			// whole time; it had simply been applied after the only copy
			// that mattered had been written.
			if (GivePlayerDefaultTitleOnNewGame)
			{
				AutoAssignPlayerPersonTitle();
			}

			if (GivePlayerDefaultFactionNameOnNewGame)
			{
				ApplyDefaultPlayerFactionName();
			}

			OnNewGame();
			if (NewGame != null)
			{
				NewGame(this);
			}
			if (NewGameRoot != null)
			{
				NewGameRoot.transform.DetachChildren();
			}
			else
			{
				Debug.LogError("World has no NewGame root object", this);
			}
			if (Debug.isDebugBuild)
			{
				EngineASX.Instance.StationBuildDistanceValidator.Validate();
			}
		}

		private void CallFactionAIOnNewGame()
		{
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (faction.FactionAI != null)
				{
					faction.FactionAI.OnNewGame();
				}
			}
		}

		private void SetInitialScene()
		{
			if (InitialPlayer != null)
			{
				Debug.Log("World: Setting initial player: " + InitialSector);
				engine.LocalPlayer = InitialPlayer;
				if (InitialSector != null)
				{
					Debug.LogWarning("World: Ignoring initial scene value because InitialPlayer is set: " + InitialSector);
				}
				if (InitialPlayer.Sector == null)
				{
					Debug.LogError("World has been supplied with player that has no scene", InitialPlayer);
				}
				Debug.Log("World: Setting initial scene to player: " + InitialSector);
				engine.ActiveSector = engine.LocalPlayer.Person.Sector;
			}
			else if (InitialSector != null)
			{
				Debug.Log("World: Activating initial scene: " + InitialSector);
				engine.ActiveSector = InitialSector;
			}
		}

		private void ValidateTraderPrices()
		{
			CargoTraderPriceValidator cargoTraderPriceValidator = new CargoTraderPriceValidator();
			foreach (CargoTrader trader in Engine.Traders)
			{
				cargoTraderPriceValidator.Validate(trader);
			}
		}

		protected virtual void SeedWorldOnNewGame()
		{
			ScenarioLoadData scenarioLoadData = lastLoadedData;
			if (scenarioLoadData != null && scenarioLoadData.SeedSettings != null)
			{
				if (Seeder.Settings != null)
				{
					UnityEngine.Object.DestroyImmediate(Seeder.Settings.gameObject);
				}
				Seeder.Settings = scenarioLoadData.SeedSettings;
				Seeder.Settings.transform.SetParent(Seeder.transform);
			}
			if (Seeder != null)
			{
				if (Seeder.Settings == null)
				{
					Seeder.Settings = GameController.Instance.GameSettings.DefaultWorldSeedSettings;
				}
				if (Seeder.Settings != null)
				{
					FleetsBeforeSeed = GetWorldFleets(this);
					Seeder.SeedWorld(engine.World);
				}
			}
		}

		private HashSet<int> GetWorldFleets(WorldBase world)
		{
			HashSet<int> hashSet = new HashSet<int>();
			foreach (Sector sector in world.Engine.Sectors)
			{
				Fleet[] componentsInChildren = sector.GetComponentsInChildren<Fleet>(includeInactive: true);
				foreach (Fleet fleet in componentsInChildren)
				{
					hashSet.Add(fleet.UniqueId);
				}
			}
			return hashSet;
		}

		public void CacheAllFactionValidTraders()
		{
			foreach (Faction faction in engine.Factions)
			{
				if (faction != null)
				{
					faction.RecacheValidTraderTargets();
				}
			}
		}

		private void PowerupFactionStations()
		{
			foreach (Faction faction in engine.Factions)
			{
				faction.PowerUpStations();
			}
		}

		private void RandomizeEmptyPilotNames()
		{
			foreach (Person person in engine.People)
			{
				if (person.GetComponent<GamePlayer>() == null)
				{
					if (string.IsNullOrEmpty(person.CustomName) && !person.HasGeneratedName)
					{
						person.RandomizeGenderAndName(GameController.Instance.GameSettings.GameplaySettings.ProbabilityOfMaleNpc, EngineASX.Instance.CharacterNames);
					}
					person.AutoAssignAvatarProfileFromFactionIfNone();
				}
			}
		}

		private void DestroyNewGameUnits()
		{
			if (NewGameRoot != null)
			{
				UnityEngine.Object.DestroyImmediate(NewGameRoot);
			}
		}

		protected virtual void OnNewGame()
		{
		}

		protected virtual bool requestRespawn(Unit playerUnit)
		{
			Debug.Log("Request respawn...");
			if (!CanLoadLastSave || !SaveGameUtilities.TryLoadLastSavedGame(GameController.Instance.ScenarioLoader))
			{
				Debug.Log("Engine requested respawn but I'm quitting to main menu");
				return false;
			}
			return true;
		}

		protected virtual void OnLocalPlayerKilled()
		{
			if (objectiveState == ScenarioState.Playing || objectiveState == ScenarioState.Intro)
			{
				scenarioSuccess = false;
				OnScenarioCompleted();
			}
		}

		protected virtual void OnScenarioCompleted()
		{
			ObjectiveState = ScenarioState.Complete;
			UIController.Instance.QuickMsg.gameObject.SetActive(value: false);
			UIController.Instance.QuickMsg.ClearMessages();
			if (engine.PlayerUnit.Destructable != null)
			{
				Engine.PlayerUnit.Destructable.AllowDestruction = false;
			}
			Engine.PlayerUnit.Components.RemovePilotControl();
			if (ScenarioInfo != null && GameController.Instance.LastAttemptedScenario == ScenarioInfo)
			{
				GameController.Instance.LastAttemptedScenarionCompleted = scenarioSuccess;
			}
			if (scenarioSuccess)
			{
				Engine.StartCustomMessageCinematic(null, 0f, allowTogglePause: false);
				if (ScenarioInfo != null)
				{
					ScenarioInfo.SetCompleted(minCombatDifficultyIndex);
				}
			}
			else
			{
				engine.PlayLoseAudio();
				Engine.StartCustomMessageCinematic(null, 0f, allowTogglePause: false);
			}
			engine.PlayWinAudio();
			UIController.Instance.ScreenNavigator.ShowEndGameScreen();
		}

		protected virtual void update()
		{
		}

		protected virtual void ApplyFactionSetupOnNewGame()
		{
			foreach (Faction faction in Engine.Factions)
			{
				FactionSetup component = faction.GetComponent<FactionSetup>();
				if (component != null)
				{
					component.ApplyAttitudes();
				}
			}
		}

		protected virtual void OnLoadGame()
		{
			EngineIO.Load(LoadData.FullSaveGamePath, Engine);
			if (Engine.LocalPlayer != null)
			{
				PlayerFaction = Engine.LocalPlayer.Faction;
			}
			CanLoadLastSave = true;
		}

		protected virtual GamePlayer SpawnNewPlayerInternal()
		{
			if (PlayerPrefab != null)
			{
				if (PlayerFaction != null)
				{
					GamePlayer component = UnityEngine.Object.Instantiate(PlayerPrefab.gameObject).GetComponent<GamePlayer>();
					if (component != null)
					{
						component.Awake();
						component.Person.Init();
						component.Person.Faction = PlayerFaction;
						if (RespawnCredits >= 0)
						{
							component.Credits = RespawnCredits;
						}
						component.Person.CustomTitle = GameController.Instance.DefaultPilotTitle;
					}
					return component;
				}
				Debug.LogError("Cannot spawn player. No faction set", this);
			}
			else
			{
				Debug.LogError("Cannot spawn player. No player prefab set", this);
			}
			return null;
		}

		protected virtual void OnStateChanged(ScenarioState oldState)
		{
		}

		protected virtual void ExitScenario()
		{
			OpenFrontier.IP.UI.UI.QuitToMainMenu();
		}

		private static void ReactiveQuickMsgr()
		{
			UIController.Instance.QuickMsg.ClearMessages();
			UIController.Instance.QuickMsg.gameObject.SetActive(value: true);
		}

		private void engine_UnitKilled(EngineASX engine, Unit unit, Sector sector, Unit attackingUnit, Faction attackerFaction)
		{
			OnUnitKilled(unit, attackerFaction);
		}

		private void engine_UnitDestroyed(EngineASX engine, Unit unit)
		{
			OnUnitDestroyed(unit);
		}

		private void FindMessages(Transform transform)
		{
			MessageTemplate[] componentsInChildren = transform.GetComponentsInChildren<MessageTemplate>(includeInactive: true);
			foreach (MessageTemplate messageTemplate in componentsInChildren)
			{
				if (messageTemplatesById.ContainsKey(messageTemplate.UniqueId))
				{
					Debug.LogError("Found duplicate message template id: " + messageTemplate.UniqueId, messageTemplate);
				}
				else
				{
					messageTemplatesById.Add(messageTemplate.UniqueId, messageTemplate);
				}
			}
		}

		private void FindTriggerGroups(Transform transform)
		{
			TriggerGroup[] componentsInChildren = transform.GetComponentsInChildren<TriggerGroup>(includeInactive: true);
			foreach (TriggerGroup obj in componentsInChildren)
			{
				obj.FindTriggers();
				obj.Init();
			}
		}

		private void FindDialogStages(Transform transform)
		{
			DialogStage[] componentsInChildren = transform.GetComponentsInChildren<DialogStage>(includeInactive: true);
			foreach (DialogStage dialogStage in componentsInChildren)
			{
				if (dialogStagesById.ContainsKey(dialogStage.UniqueId))
				{
					Debug.LogError($"World contains dialog stage with duplicate id \"{dialogStage.UniqueId}\"", dialogStage);
					continue;
				}
				dialogStagesById.Add(dialogStage.UniqueId, dialogStage);
				dialogStage.Awake();
				dialogStage.gameObject.SetActive(value: false);
			}
		}

		private void FindDialogs(Transform transform)
		{
			DialogBase[] componentsInChildren = transform.GetComponentsInChildren<DialogBase>(includeInactive: true);
			foreach (DialogBase dialogBase in componentsInChildren)
			{
				dialogsById.Add(dialogBase.UniqueId, dialogBase);
				dialogBase.Awake();
				dialogBase.gameObject.SetActive(value: false);
			}
		}

		private void Update()
		{
			if (!hasInitialised)
			{
				EngineASX instance = EngineASX.Instance;
				if (instance != null && instance.Hud != null && GameController.Instance != null && instance.HasInitialized)
				{
					try
					{
						Init();
					}
					finally
					{
						hasInitialised = true;
					}
				}
			}
			else
			{
				if (!(engine != null))
				{
					return;
				}
				switch (objectiveState)
				{
				case ScenarioState.Playing:
				{
					int gameWorldElapsedDays = engine.DateTimeUtils.GetGameWorldElapsedDays();
					if (gameWorldElapsedDays > elapsedGameDays)
					{
						elapsedGameDays = gameWorldElapsedDays;
						GrantFactionIncome();
					}
					if (Time.time > nextCheckObjectives)
					{
						UpdateActiveMissions();
						nextCheckObjectives = Time.time + CheckObjectivesCompleteInterval;
					}
					break;
				}
				case ScenarioState.Completing:
					if (Time.time > completionTime)
					{
						OnScenarioCompleted();
					}
					break;
				}
				update();
			}
		}

		private void GrantFactionIncome()
		{
			foreach (Faction faction in engine.Factions)
			{
				if (faction.FactionAI != null && faction.AISettings.DailyIncome > 0 && faction.Credits < 5000000)
				{
					faction.ApplyTransaction(faction.AISettings.DailyIncome, FactionTransactionType.Gift);
				}
			}
		}

		private void UpdateActiveMissions()
		{
			int num = 0;
			missionCache.Clear();
			foreach (Mission mission in engine.Missions)
			{
				if (mission != null)
				{
					missionCache.Add(mission);
				}
			}
			foreach (Mission item in missionCache)
			{
				if (item.gameObject.activeSelf)
				{
					item.UpdateObjectiveState();
					if (!item.IsFinished)
					{
						num++;
					}
				}
			}
			if (objectiveState == ScenarioState.Playing && num == 0 && CompleteWhenNoObjectivesLeft)
			{
				OnScenarioCompleting(success: true);
			}
		}

		private void OnScenarioCompleting(bool success)
		{
			scenarioSuccess = success;
			ObjectiveState = ScenarioState.Completing;
			SetPlayingScenario(playing: false);
			completionTime = Time.time + CompletionInterval;
		}

		private void SetPlayingScenario(bool playing)
		{
			engine.AllowPlayerDock = playing;
			if (engine.PlayerUnit != null && engine.PlayerUnit.Destructable != null)
			{
				engine.PlayerUnit.Destructable.AllowDestruction = playing;
			}
		}

		public virtual void OnPlayerUnitChanged(EngineASX engineASX, Unit oldUnit)
		{
		}

		public DateTime GetScenarioStartDateTime()
		{
			return new DateTime(ScenarioDateYear, ScenarioDateMonth, ScenarioDateDay, ScenarioDateHour, ScenarioDateMinute, 0);
		}
	}
}
