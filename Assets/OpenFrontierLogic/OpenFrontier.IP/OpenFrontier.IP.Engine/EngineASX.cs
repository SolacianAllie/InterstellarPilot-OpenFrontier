using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.AmbientSounds;
using OpenFrontier.IP.Engine.AsteroidRespawning;
using OpenFrontier.IP.Engine.Bars;
using OpenFrontier.IP.Engine.CachedFleetSettings;
using OpenFrontier.IP.Engine.CargoFactory;
using OpenFrontier.IP.Engine.CargoLeakage;
using OpenFrontier.IP.Engine.Cargos;
using OpenFrontier.IP.Engine.Comms.FactionDynamicComms;
using OpenFrontier.IP.Engine.CompatibleComponents;
using OpenFrontier.IP.Engine.Core;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.IP.Engine.Dialog;
using OpenFrontier.IP.Engine.DitchShip;
using OpenFrontier.IP.Engine.EngineMusic;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.Bounty;
using OpenFrontier.IP.Engine.Fleets.FleetFormations;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.Engine.GasClouds;
using OpenFrontier.IP.Engine.Hypersleep;
using OpenFrontier.IP.Engine.MissionObjectives;
using OpenFrontier.IP.Engine.MissionSpecs;
using OpenFrontier.IP.Engine.NpcPathfinding;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using OpenFrontier.IP.Engine.OutlawNames;
using OpenFrontier.IP.Engine.PilotRankings;
using OpenFrontier.IP.Engine.Player;
using OpenFrontier.IP.Engine.SaveGame;
using OpenFrontier.IP.Engine.SkyBackgrounds;
using OpenFrontier.IP.Engine.TraderHeatmap;
using OpenFrontier.IP.Engine.Triggers;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.Engine.Validation;
using OpenFrontier.IP.Engine.Wormholes;
using OpenFrontier.IP.Scenarios;
using OpenFrontier.IP.Scenarios.RandomEvents;
using OpenFrontier.IP.Scenarios.UniverseEventsNotifier;
using OpenFrontier.IP.SpaceUnity;
using OpenFrontier.IP.UI;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.Hud;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine
{
	public class EngineASX : MonoBehaviour
	{
		public delegate void PlayerPropertyChangedSectorHandler(Unit unit, Sector oldSector, Sector newSector);

		public delegate void UnitInitialisedHandler(EngineASX sender, Unit unit);

		public delegate void LocalPlayerFactionReceivedDamageHandler(EngineASX sender, Faction playerFaction, Faction attackingFaction, Unit attackingUnit, float damage, DamageDirectType damageDirectType);

		public delegate void CameraMovedHandler(EngineASX sender);

		public delegate void MissionsChangedHandler(EngineASX sender);

		public delegate void PausedChangedHandler(EngineASX sender);

		public delegate void PlayerUnitChangedHandler(EngineASX sender, Unit oldUnit);

		public delegate void SectorChangedHandler(EngineASX sender, Sector oldScene);

		public delegate void UnitDestroyedHandler(EngineASX engine, Unit unit);

		public delegate void UnitHangarChangedHandler(EngineASX engine, Unit unit, UnitHangarBay oldBay, UnitHangarBay newBay);

		public delegate void UnitAttackedHandler(EngineASX engine, Unit unit, Sector sector, Unit attackingUnit, Faction attackerFaction);

		public delegate void UnitKilledHandler(EngineASX engine, Unit unit, Sector sector, Unit attackingUnit, Faction attackerFaction);

		public delegate void UnitConstructedHandler(EngineASX engine, Unit unit);

		public delegate void WorldLoadedHandler(EngineASX sender);

		public class RestoreTractorBeamArgs
		{
			public float InitTime;

			public Unit TargetUnit;

			public TractorTurretComponent TractorTurret;
		}

		private class SectorTransitionInfo
		{
			public Sector TargetSector { get; private set; }

			public Wormhole Wormhole { get; private set; }

			public Quaternion? ManualTargetLocalRotation { get; set; }

			public Vector3? ManualTargetSectorPosition { get; set; }

			public Unit WormholeUserUnit { get; private set; }

			public bool WasSectorDiscovered { get; set; }

			public SectorTransitionInfo(Wormhole wormhole, Sector targetScene, Unit wormholeUser)
			{
				Wormhole = wormhole;
				TargetSector = targetScene;
				WormholeUserUnit = wormholeUser;
			}
		}

		public PlayerWaypointUnitSyncer PlayerWaypointUnitSyncer;

		private TraderHeatmapModule traderHeatmapModule = new TraderHeatmapModule();

		private CachedFleetSettingsController cachedFleetSettingsController;

		public Sprite WeaponArc90Sprite;

		public Sprite WeaponArc180Sprite;

		public Sprite WeaponArc360Sprite;

		public List<Mission> MissionPrefabs;

		private Dictionary<int, TriggerGroup> triggerGroupsById = new Dictionary<int, TriggerGroup>();

		private NeutralitySyncer factionNeutralitySyncer;

		public SpaceConstructor SpaceConstructor;

		private List<Unit> destroyedUnitsToCleanup = new List<Unit>(20);

		internal UnitType[] unitTypes;

		private WormholeUpdater wormholeUpdater;

		private CargoUpdater cargoUpdater;

		private FleetFormation defaultFleetFormation;

		private Dictionary<int, FleetFormation> fleetFormationsById = new Dictionary<int, FleetFormation>(8);

		private AsteroidRespawner asteroidRespawner;

		private Dictionary<int, PilotRankingSystem> pilotRankingSystemsById = new Dictionary<int, PilotRankingSystem>(20);

		private Dictionary<int, PilotRank> pilotRanksById = new Dictionary<int, PilotRank>(20);

		public NpcPathfindingController NpcPathfindingController;

		public UnitCaptureModule UnitCaptureModule = new UnitCaptureModule();

		public DitchUnitModule DitchUnitModule;

		public DitchUnitCleanupModule DitchUnitCleanupModule;

		private Dictionary<int, float> excludUnitFromNpcTraderTargets = new Dictionary<int, float>(100);

		private Dictionary<int, FactionStrategy> shipStrategyFlags = new Dictionary<int, FactionStrategy>(30);

		private Dictionary<ulong, float> shipStrategyEffectiveness = new Dictionary<ulong, float>(30);

		public SkyBackgroundTextureMapping SkyBackgroundTextureMapping;

		public Light DirectionalLight;

		public RecentWormholeEntries RecentWormholeEntries = new RecentWormholeEntries();

		public EngineDebugInfo DebugInfo;

		public UniverseEventsNotifierController UniverseEventsNotifierController;

		public BarNamesController BarNamesController;

		public OutlawNamesController OutlawNamesController;

		private int controlledSectorCount;

		public FactionSpawner FactionSpawner;

		public RandomUniverseEventsController RandomEventsController;

		public MissionManager MissionManager;

		public PassengerManager PassengerManager;

		public CargoLeakageController CargoLeakageController;

		public AdvDialogController AdvDialogController;

		public Color DiscoveredSectorColor = Color.white;

		public Color UndiscoveredSectorColor = Color.gray;

		private double? timeOfLastAutoSave;

		private DestroyedUnitInfoController destroyedUnitInfoController = new DestroyedUnitInfoController();

		public CompatibleComponentsCacher CompatibleComponentCacher;

		public DynamicCommsWelcomeMessageCalculator DynamicCommsWelcomeMessageCalculator;

		private int badGroupCheckIndex;

		public StationDistanceCalculator DistanceCalculator;

		public StationBuildDistanceValidator StationBuildDistanceValidator;

		public static Collider[] ColliderCache = new Collider[500];

		[SerializeField]
		private EngineResources engineResources;

		public Color TextWarningColor = Color.yellow;

		private List<FactionTypeInfo> factionTypes = new List<FactionTypeInfo>(10);

		private List<FactionShipCapType> factionShipCapTypes = new List<FactionShipCapType>(10);

		public CurrentUnitChanger UnitChanger;

		public EngineAmbientSoundManager AmbientSoundManager;

		public AudioSource JumpGateExitAudioSourcePrefab;

		public AudioSource JumpGateEnterAudioSourcePrefab;

		public AudioClip EnterShipAudioClip;

		public TurretControllerSpawner TurretControllerSpawner;

		public NpcPilot TurretControllerPrefab;

		[FormerlySerializedAs("GenericGroupPrefab")]
		public Fleet GenericFleetPrefab;

		public Person GenericPilotPrefab;

		public Person AggressorPilotPrefab;

		public DialogProfile AggressorDialogProfile;

		public DialogProfile GenericDialogProfile;

		public bool UnitAmbientSoundsEnabled;

		public AudioSource EjectCargoAudioSourcePrefab;

		private Dictionary<int, List<FactionBountyItem>> bountiesByPlayer = new Dictionary<int, List<FactionBountyItem>>();

		public CharacterNameSettings CharacterNameSettings;

		public EngineDialogEvents DialogEvents;

		private float timeScaleOnWormholeEntry = 1f;

		private bool invulnerableOnWormholeEntry;

		public const float GateEntryCoolDownTime = 3f;

		public const int UnitShieldPoints = 6;

		public const float DegreesPerShieldPoint = 60f;

		public static bool LoadedAndReady = false;

		private static EngineASX instance = null;

		private Sector activeSector;

		[SerializeField]
		private ActiveSectorData activeSectorData;

		private Dictionary<int, Fleet> fleetIdMap = new Dictionary<int, Fleet>(20);

		private List<Fleet> fleets = new List<Fleet>(400);

		private Dictionary<int, FleetOrder> fleetOrderIdMap = new Dictionary<int, FleetOrder>(20);

		private Dictionary<FactionType, List<Unit>> shipsByFactionType = new Dictionary<FactionType, List<Unit>>();

		public bool AllowPlayerDock = true;

		public DockAmbientSoundPlayer AmbientSoundPlayer;

		public Color CreditsUpColor = Color.green;

		public Color CreditsNeutralColor = Color.grey;

		public Color CreditsDownColor = Color.red;

		public Color[] AttitudeColors;

		public Dictionary<UnitAttributeTypes, UnitAttributeLevels> AttributeLevelMap = new Dictionary<UnitAttributeTypes, UnitAttributeLevels>();

		public AudioClip BuySellAudioClip;

		private List<CustomParticles> cameraParticleSystems = new List<CustomParticles>();

		private MainCamera mainCamera;

		private List<CargoClass> cargoClasses = new List<CargoClass>(40);

		private Dictionary<int, Faction> factionPrefabIdMap = new Dictionary<int, Faction>(30);

		private Dictionary<int, CargoClass> cargoClassMap = new Dictionary<int, CargoClass>(100);

		public AudioSource CargoCollectAudioSource;

		public Cargo CargoContainerPrefab;

		public CharacterNames CharacterNames;

		public FactionNames FactionNames;

		private CinematicScreen cinematicScreen;

		public string[] CombatAudioSourceNames;

		public string[] JukeboxAudioSourceNames;

		private Dictionary<int, ComponentClass> componentClasses = new Dictionary<int, ComponentClass>(100);

		[NonSerialized]
		public List<ComponentClass> ComponentClasses = new List<ComponentClass>(40);

		[NonSerialized]
		public List<ComponentType> ComponentsTypes = new List<ComponentType>();

		private List<ComponentBayType> componentBayTypes = new List<ComponentBayType>();

		public CreditsAnimation CreditsAnimation;

		private int curInactiveFleetIndex;

		private int curInactiveFleetSpawnerIndex;

		private int curInactiveUnitIndex;

		public List<UnitClass> DebrisUnitClasses;

		public AudioSource DefaultCloakAudioSource;

		public AudioSource DefaultDecloakAudioSource;

		public AudioSource DefaultHullHitAudioSource;

		public AudioSource DefaultShieldDownAudioSource;

		public AudioSource DefaultShieldHitAudioSource;

		public AudioSource DefaultShieldUpAudioSource;

		private DialogController dialogController;

		private Dictionary<int, DialogProfile> dialogProfileMap = new Dictionary<int, DialogProfile>(20);

		private DockUI dockUI = new DockUI();

		public EngineEconomySettings EconomySettings;

		public float ElapsedTimeAsPilot;

		private Dictionary<int, Faction> factionIdMap = new Dictionary<int, Faction>(20);

		[NonSerialized]
		public List<Faction> Factions = new List<Faction>(20);

		private SectorTransitionInfo sectorTransitionInfo;

		[NonSerialized]
		[FormerlySerializedAs("GroupSpawners")]
		public List<FleetSpawner> FleetSpawners = new List<FleetSpawner>(20);

		public Color HostilityColor = Color.red;

		private HudScreen hud;

		public Color[] HullColors;

		public float IntroAnimationDuration = 5f;

		public SimpleFader IntroFaderPrefab;

		private Dictionary<int, float> lastDialogEventTimes = new Dictionary<int, float>();

		private float lastStateChangeTime;

		private GamePlayer localPlayer;

		public string[] LoseAudioSourceNames;

		public AudioSource MessageReceivedAudioSource;

		public AudioSource MessageReceivedUnimportantAudioSource;

		public SimpleFader MissionCompleteTransitionFaderPrefab;

		private Dictionary<int, MissionData> missionDatas = new Dictionary<int, MissionData>();

		private Dictionary<int, Mission> missionsById = new Dictionary<int, Mission>(20);

		private Dictionary<int, MissionObjective> missionObjectivesById = new Dictionary<int, MissionObjective>(20);

		private Dictionary<int, MissionSpec> jobsById = new Dictionary<int, MissionSpec>(100);

		private Dictionary<int, List<MissionSpec>> missionSpecsByUnit = new Dictionary<int, List<MissionSpec>>();

		public float NextAllowedPlayerGateEntry;

		public Color OwnedColor = Color.blue;

		public Color AlliedColor = Color.green;

		public Color CustomPathColor = Color.white;

		public Color MissionPathColor = Color.white;

		private Dictionary<int, PassengerGroup> passengerGroupMap = new Dictionary<int, PassengerGroup>(100);

		public PathFinder PathFinder;

		private Dictionary<int, AIPatrolPath> aiPatrolPathsById = new Dictionary<int, AIPatrolPath>(20);

		private Dictionary<int, Person> personIdMap = new Dictionary<int, Person>(100);

		private float playerCombatTimeout;

		private bool playerInCombat;

		public EnginePooler Pooler;

		public Color[] PriceColors;

		public Color NeutralPriceLabelColor = Color.grey;

		[FormerlySerializedAs("RecentAttacksLog")]
		public UnitRecentAttacksLog UnitRecentAttacksLog;

		private UnitRecentAttacksLogsByInflictor unitRecentAttacksLogsByInflictor = new UnitRecentAttacksLogsByInflictor();

		[FormerlySerializedAs("GroupRecentAttacksLog")]
		public FleetRecentAttacksLog FleetRecentAttacksLog;

		private Dictionary<ulong, int> sectorConnectionCache = new Dictionary<ulong, int>();

		private List<Sector> sectors = new List<Sector>(20);

		public SimpleFader SceneTransitionFaderPrefab;

		public CargoClass ScrapMetalCargoClass;

		public Color[] ShieldColors;

		public Color NoShieldColor = Color.black;

		public ShieldHitRenderer ShieldHitRenderer;

		public Vector3 ShieldNHullOffset = new Vector3(0f, 0f, -1.2f);

		public ShipNames ShipNames;

		public EngineStats Stats;

		public double ScenarioElapsedTime;

		[NonSerialized]
		public List<CargoTrader> Traders = new List<CargoTrader>(100);

		private Dictionary<int, UnitClass> unitClassMap = new Dictionary<int, UnitClass>(100);

		private List<UnitClass> unitClassList = new List<UnitClass>(64);

		private List<UnitClass> factionAIBuildableShips = new List<UnitClass>(30);

		private Dictionary<int, Unit> unitIdMap = new Dictionary<int, Unit>(100);

		private Dictionary<int, Sector> sectorIdMap = new Dictionary<int, Sector>(100);

		private List<Unit> updatableUnits = new List<Unit>(2000);

		private List<int> freeUnitIndices = new List<int>(100);

		private Dictionary<int, List<Unit>> unitsByClass = new Dictionary<int, List<Unit>>();

		private Dictionary<UnitType, List<Unit>> unitsByType = new Dictionary<UnitType, List<Unit>>();

		private Dictionary<int, List<Unit>> unitsByStationPurpose = new Dictionary<int, List<Unit>>();

		[FormerlySerializedAs("UnitSpectator")]
		public HudCamera HudCamera;

		public PlayerWaypointController WaypointController;

		public string[] WinAudioSourceNames;

		[FormerlySerializedAs("WingmenGroupPrefab")]
		public Fleet PlayerFleetPrefab;

		public Person WingmenPilotPrefab;

		private WorldBase world;

		public WormholeAnimator WormholeAnimationPrefab;

		private WormholeAnimator wormholeAnimator;

		public GasCloudController EnvironmentController;

		private float lastTimeAutoValidatedState;

		private float? cachedTotalFactionTypeShipCapWeight;

		private bool hasInitialized;

		private int debugInactiveUnitUpdateCounter;

		private float debugInactiveUnitTotalElapsedTime;

		private static List<DialogProfile> dialogProfileCache = new List<DialogProfile>(4);

		public int UnitIdCounter { get; set; } = 100000;

		public int PlayerMessageIdCounter { get; set; } = 100000;

		public int PersonIdCounter { get; set; } = 100000;

		public int FactionIdCounter { get; set; } = 100000;

		public int FleetOrderIdCounter { get; set; } = 100000;

		public int PassengerGroupIdCounter { get; set; } = 100000;

		public int JobIdCounter { get; set; } = 100000;

		public int FleetIdCounter { get; set; } = 100000;

		public int SectorIdCounter { get; set; } = 100000;

		public int MissionIdCounter { get; set; } = 100000;

		public int PatrolPathIdCounter { get; set; } = 100000;

		public int MissionObjectiveIdCounter { get; set; } = 100000;

		public DestroyedUnitInfoController DestroyedUnitInfoController => destroyedUnitInfoController;

		public static bool IsLoading
		{
			get
			{
				if (instance == null)
				{
					return true;
				}
				if (LoadedAndReady)
				{
					return false;
				}
				if (instance.world != null && instance.world.LoadFailed)
				{
					return false;
				}
				return true;
			}
		}

		public static bool LoadFailed
		{
			get
			{
				if (instance != null && instance.world != null)
				{
					return instance.world.LoadFailed;
				}
				return false;
			}
		}

		public ActiveSectorData ActiveSectorData => activeSectorData;

		public bool AllowPlayerEnterGate
		{
			get
			{
				if (world != null)
				{
					return world.ObjectiveState == WorldBase.ScenarioState.Playing;
				}
				return false;
			}
		}

		public UnitRecentAttacksLogsByInflictor UnitRecentAttacksLogsByInflictor => unitRecentAttacksLogsByInflictor;

		public int TotalBountyPersonTargets => bountiesByPlayer.Count;

		public IEnumerable<UnitClass> UnitClasses => unitClassList;

		public DialogController DialogController
		{
			get
			{
				return dialogController;
			}
			private set
			{
				dialogController = value;
			}
		}

		public IEnumerable<MissionData> MissionDatas => missionDatas.Values;

		public IEnumerable<MissionSpec> Jobs => jobsById.Values;

		public int JobCount => jobsById.Count;

		public IEnumerable<Mission> Missions => missionsById.Values;

		public List<CargoClass> CargoClasses => cargoClasses;

		public List<Unit> UpdatableUnits => updatableUnits;

		public IEnumerable<PassengerGroup> PassengerGroups => passengerGroupMap.Values;

		public List<Sector> Sectors => sectors;

		public HudScreen Hud
		{
			get
			{
				return hud;
			}
			private set
			{
				if (hud != value)
				{
					hud = value;
					if (hud != null)
					{
						ApplyHudPlayerPrefs();
					}
				}
			}
		}

		public CinematicScreen CinematicScreen
		{
			get
			{
				return cinematicScreen;
			}
			set
			{
				if (this.cinematicScreen != value)
				{
					CinematicScreen cinematicScreen = this.cinematicScreen;
					this.cinematicScreen = value;
					if (cinematicScreen != null)
					{
						cinematicScreen.Finished -= IntroCinematic_Finished;
					}
					if (this.cinematicScreen != null)
					{
						CinematicScreen.Finished += IntroCinematic_Finished;
					}
				}
			}
		}

		public DockUI DockUI => dockUI;

		public GameSettings GameSettings => GameController.Instance.GameSettings;

		public bool IsShowingJumpGateAnimation => sectorTransitionInfo != null;

		public Unit PlayerUnit
		{
			get
			{
				if (localPlayer != null)
				{
					return localPlayer.Person.CurrentUnit;
				}
				return null;
			}
		}

		public bool IsPaused
		{
			get
			{
				return Time.timeScale == 0f;
			}
			set
			{
				if (value)
				{
					Time.timeScale = 0f;
				}
				else
				{
					Time.timeScale = 1f;
				}
			}
		}

		public MainCamera MainCamera => mainCamera;

		public Sector LocalUnitSector
		{
			get
			{
				Unit localUnit = instance.LocalUnit;
				if (localUnit != null)
				{
					return localUnit.Sector;
				}
				return null;
			}
		}

		public Sector ActiveSector
		{
			get
			{
				return activeSector;
			}
			set
			{
				if (!(activeSector != value))
				{
					return;
				}
				Sector sector = activeSector;
				activeSector = value;
				if (sector != null)
				{
					Pooler.ReturnAllObjectsToPool();
					AmbientSoundManager.Clear();
				}
				EnvironmentController.ClearGasCloud();
				if (ShieldHitRenderer != null)
				{
					ShieldHitRenderer.Clear();
				}
				if (hud != null)
				{
					Hud.AutoScanner.ClearTargetCache();
					Hud.ClearAutoTurnDesiredBearing();
					Hud.SpeechModel.ClearRequests();
				}
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"{this}: Changing active sector from \"{sector}\" to \"{activeSector}\"", this, 1);
				}
				SectorPositioner.PositionSectors(sectors, GameController.Instance.GameSettings.UniverseBoundsSettings);
				foreach (Fleet fleet in fleets)
				{
					if (fleet != null)
					{
						fleet.RefreshIsInActiveSector();
					}
				}
				if (sector != null)
				{
					sector.RefreshSectorUnitsActive();
				}
				if (activeSector != null)
				{
					activeSector.RefreshSectorUnitsActive();
				}
				ActiveSectorData.OnSectorChanged();
				EnvironmentController.UpdateImmediate();
				destroyedUnitInfoController.Trim();
				SetCameraParticleSystemsActive(activeSector != null);
				RecentWormholeEntries.Trim();
				if (activeSector != null)
				{
					GasCloudHelper.FindGasCloudForUnitsInSector(activeSector);
					if (instance.localPlayer != null && instance.localPlayer.Stats != null)
					{
						instance.LocalPlayer.Stats.AddVisitedSector(activeSector);
					}
				}
				foreach (Faction faction in Factions)
				{
					if (!faction.IsFreelancer && faction.IsAIFactionType && faction.PilotRankingSystem != null)
					{
						faction.FactionAI.CacheMostPowerfulFleet();
						faction.FactionAI.AssignAllPilotRanks(updateDebugInfo: true);
					}
				}
				if (HudCamera != null)
				{
					HudCamera.ResetOrientation();
				}
				if (ActiveSectorChanged != null)
				{
					ActiveSectorChanged(this, sector);
				}
			}
		}

		public Sector LocalPlayerSector
		{
			get
			{
				if (localPlayer != null)
				{
					return localPlayer.Person.Sector;
				}
				return null;
			}
		}

		public Vector3 LocalPlayerWorldPosition
		{
			get
			{
				if (localPlayer != null)
				{
					return localPlayer.transform.position;
				}
				return Vector3.zero;
			}
		}

		public Person LocalPerson
		{
			get
			{
				if (localPlayer != null)
				{
					return localPlayer.Person;
				}
				return null;
			}
		}

		public GamePlayer LocalPlayer
		{
			get
			{
				return localPlayer;
			}
			set
			{
				if (localPlayer != value)
				{
					GamePlayer oldPlayer = localPlayer;
					localPlayer = value;
					if (localPlayer != null)
					{
						localPlayer.Awake();
					}
					OnLocalPlayerChanged(oldPlayer);
				}
			}
		}

		public Faction LocalFaction
		{
			get
			{
				if (localPlayer != null)
				{
					return localPlayer.Faction;
				}
				return null;
			}
		}

		public Unit PlayerRootUnit
		{
			get
			{
				Unit localUnit = LocalUnit;
				if (localUnit != null)
				{
					return localUnit.GetRootUnit();
				}
				return null;
			}
		}

		public Unit LocalPilottedUnit
		{
			get
			{
				if (IsPlayerPilot)
				{
					return localPlayer.Person.CurrentUnit;
				}
				return null;
			}
		}

		public Unit LocalRootUnit
		{
			get
			{
				Unit localUnit = LocalUnit;
				if (localUnit != null)
				{
					return localUnit.GetRootUnit();
				}
				return null;
			}
		}

		public Unit LocalUnit
		{
			get
			{
				if (localPlayer != null)
				{
					return localPlayer.Person.CurrentUnit;
				}
				return null;
			}
		}

		public WorldBase World
		{
			get
			{
				return world;
			}
			set
			{
				world = value;
			}
		}

		public IEnumerable<Person> People => personIdMap.Values;

		public int PersonCount => personIdMap.Count;

		public bool PlayerInCombat
		{
			get
			{
				return playerInCombat;
			}
			set
			{
				if (playerInCombat != value)
				{
					playerInCombat = value;
					if (playerInCombat)
					{
						SetPlayerCombatTimeout();
					}
				}
			}
		}

		public float TimeSinceLastGameStateChange => Time.time - lastStateChangeTime;

		public PerformanceSettings PerformanceSettings => GameController.Instance.GameSettings.PerformanceSettings;

		public bool IsPlayerPilotAndHudCurrentPanel
		{
			get
			{
				if (IsPlayerPilot && Hud != null)
				{
					return Hud.IsCurrentScreen;
				}
				return false;
			}
		}

		public bool PlayerUnitAutoPilotEnabled
		{
			get
			{
				Unit playerUnit = PlayerUnit;
				if (playerUnit != null && playerUnit.IsOwnedByPlayer)
				{
					return OrdersHelper.IsPilottedByNpc(playerUnit);
				}
				return false;
			}
		}

		public bool IsPlayerPilot
		{
			get
			{
				if (localPlayer != null && localPlayer.Person.IsPilot)
				{
					return !localPlayer.Person.CurrentUnit.IsDocked;
				}
				return false;
			}
		}

		public MissileLockController MissileLockController { get; private set; }

		public static EngineASX Instance => instance;

		public IEnumerable<AIPatrolPath> PatrolPaths => aiPatrolPathsById.Values;

		public int TriggerGroupCount => triggerGroupsById.Count;

		public Dictionary<int, TriggerGroup> TriggerGroupsById => triggerGroupsById;

		public CameraSpectator CameraSpectator
		{
			get
			{
				Camera camera = GameController.Instance.MainCamera;
				if (camera != null)
				{
					return camera.GetComponent<CameraSpectator>();
				}
				return null;
			}
		}

		public bool HasInitialized => hasInitialized;

		public CachedFleetSettingsController CachedFleetSettingsController => cachedFleetSettingsController;

		public List<ComponentBayType> ComponentBayTypes => componentBayTypes;

		public List<UnitClass> FactionAIBuildableShips => factionAIBuildableShips;

		public int UnitCount => unitIdMap.Count;

		public Color AttitudeNeutralColor => Drawing.MultiLevelColorLerp(AttitudeColors, 0.5f);

		public bool IsPlayerUnitOwnedByPlayer
		{
			get
			{
				Unit localUnit = LocalUnit;
				if (localUnit != null)
				{
					return localUnit.IsOwnedByPlayer;
				}
				return false;
			}
		}

		public bool IsPlayerRootUnitOwnedByPlayer
		{
			get
			{
				Unit playerRootUnit = PlayerRootUnit;
				if (playerRootUnit != null)
				{
					return playerRootUnit.IsOwnedByPlayer;
				}
				return false;
			}
		}

		public bool IsPlayerUnitDocked
		{
			get
			{
				Unit localUnit = LocalUnit;
				if (localUnit != null)
				{
					return localUnit.IsDocked;
				}
				return false;
			}
		}

		public int AIShipCap => sectors.Count * GameSettings.AIShipCapPerScene;

		public EngineResources EngineResources => engineResources;

		public List<FactionTypeInfo> FactionTypes => factionTypes;

		public double? TimeOfLastAutoSave
		{
			get
			{
				return timeOfLastAutoSave;
			}
			set
			{
				timeOfLastAutoSave = value;
			}
		}

		public List<Fleet> Fleets => fleets;

		public DateAndTimeUtils DateTimeUtils { get; private set; }

		public int ControlledSectorCount => controlledSectorCount;

		public int UncontrolledSectorCount => sectors.Count - ControlledSectorCount;

		public IEnumerable<CargoClass> TradableCargoClasses => cargoClasses.Where((CargoClass e) => e.IsTraded && !e.IsReserved);

		public TraderHeatmapModule TraderHeatmapModule => traderHeatmapModule;

		public event PlayerPropertyChangedSectorHandler PlayerPropertyChangedSector;

		public event UnitInitialisedHandler UnitInitialised;

		public event LocalPlayerFactionReceivedDamageHandler LocalPlayerFactionReceivedDamage;

		public event MissionsChangedHandler MissionsChanged;

		public event UnitKilledHandler UnitKilled;

		public event UnitDestroyedHandler UnitDestroyed;

		public event UnitAttackedHandler UnitAttacked;

		public event UnitConstructedHandler UnitConstructed;

		public event UnitHangarChangedHandler UnitHangarChanged;

		public event WorldLoadedHandler WorldLoaded;

		public event PlayerUnitChangedHandler PlayerUnitChanged;

		public event SectorChangedHandler ActiveSectorChanged;

		public event CameraMovedHandler CameraMoved;

		public void OnFactionTraded(Faction traderFaction, Faction dockFaction, CargoClass cargoClass, int unitsTraded)
		{
			if (unitsTraded >= 0 && !cargoClass.Legal && traderFaction != dockFaction && traderFaction != null)
			{
				int num = unitsTraded * cargoClass.BasePrice;
				VirtueHandler.HandleIllegalGoodsTraded(traderFaction, num);
				VirtueHandler.HandleIllegalGoodsTraded(dockFaction, num);
			}
		}

		public void OnCargoConsumed(Faction faction, CargoClass cargoClass, int unitsConsumed)
		{
			if (!cargoClass.Legal)
			{
				int num = unitsConsumed * cargoClass.BasePrice;
				VirtueHandler.HandleIllegalGoodsTraded(faction, (float)num * 0.5f);
			}
		}

		public static int WrapShieldIndex(int index)
		{
			return Maths.WrapValue(index, 0, 6);
		}

		public bool OnAboutToKillPerson(Person person)
		{
			if (world != null)
			{
				return world.OnAboutToKillPerson(person);
			}
			return true;
		}

		public void OnCombatDifficultyChanged()
		{
			if (world != null)
			{
				world.OnCombatDifficultyChanged();
			}
		}

		public void ApplyAllPlayerPrefs()
		{
			if (ActiveSectorData != null && ActiveSectorData.SpaceFog != null)
			{
				ActiveSectorData.SpaceFog.gameObject.SetActive(GameController.Instance.SpaceFogEnabled);
			}
			ApplyHudPlayerPrefs();
		}

		private void ApplyHudPlayerPrefs()
		{
			HudScreen hudScreen = Hud;
			if (hudScreen != null)
			{
				hudScreen.InGameMenuController.AutoCloseMenu = GameController.Instance.AutoCloseInGameMenu;
				hudScreen.ApplyShieldsDisplayType();
				hudScreen.RefreshTurretGrids();
				hudScreen.ViewOptions.ApplyPlayerPrefs();
			}
		}

		internal void OnPlayerFleetCreated(Fleet fleet)
		{
			if (world != null)
			{
				world.OnPlayerFleetCreated(fleet);
			}
		}

		public EngineASX()
		{
			MissileLockController = new MissileLockController();
		}

		public void HandleUnitAttacked(Unit attackedUnit, Faction attackerFaction, Unit attackerUnit, float baseDamage, DamageDirectType damageDirectionType)
		{
			if (!(attackedUnit != null) || !attackedUnit.IsValidAndNotDestroyed)
			{
				return;
			}
			UnitType unitType = attackedUnit.UnitType;
			if ((uint)(unitType - 1) > 1u && unitType != UnitType.Cargo)
			{
				return;
			}
			Faction faction = attackedUnit.Faction;
			if (faction == null)
			{
				return;
			}
			if (attackedUnit.IsStationOrShip())
			{
				Fleet fleet = attackedUnit.GetFleet();
				if (attackerUnit != null)
				{
					UnitRecentAttacksLogsByInflictor.LogAttack(attackerUnit, attackedUnit);
					if (fleet != null)
					{
						FleetRecentAttacksLog.LogAttack(attackerUnit, fleet);
					}
				}
				if (faction != null && faction != attackerFaction)
				{
					RecentAttackType recentAttackType = UnitRecentAttacksLog.LogAttack(attackedUnit);
					if ((recentAttackType == RecentAttackType.NewAttack || recentAttackType == RecentAttackType.UpdatedAttack) && faction != null && faction.IsAIFactionType)
					{
						faction.FactionAI.RegisterUnitUnderAttack(attackedUnit, fleet, attackerFaction, attackerUnit, recentAttackType);
					}
				}
			}
			if (faction != null && attackerFaction != null && faction != attackerFaction)
			{
				EngineASX engineASX = Instance;
				faction.HandleDamageFromSource(baseDamage, attackerFaction, attackerUnit, damageDirectionType);
				if (faction.IsPlayerFaction)
				{
					engineASX.NotifyLocalPlayerFactionReceivedDamage(attackerFaction, attackerUnit, baseDamage, damageDirectionType);
				}
			}
			if (attackerFaction != null && UnitAttacked != null)
			{
				UnitAttacked(this, attackedUnit, attackedUnit.Sector, attackerUnit, attackerFaction);
			}
		}

		public bool HasPersonGotBounty(Person person)
		{
			List<FactionBountyItem> value = null;
			if (bountiesByPlayer.TryGetValue(person.UniqueId, out value) && value != null)
			{
				foreach (FactionBountyItem item in value)
				{
					if (item.IsValid && item.Bounty > 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		internal List<FactionBountyItem> GetBountiesOnPerson(Person person)
		{
			List<FactionBountyItem> value = null;
			if (bountiesByPlayer.TryGetValue(person.UniqueId, out value))
			{
				return value;
			}
			return null;
		}

		public bool GetBountyPilotLastKnownSectorPosition(Person person, out Sector scene, out Vector3 sectorPosition)
		{
			scene = null;
			sectorPosition = Vector3.zero;
			List<FactionBountyItem> bountiesOnPerson = GetBountiesOnPerson(person);
			if (bountiesOnPerson != null)
			{
				foreach (FactionBountyItem item in bountiesOnPerson)
				{
					if (item.LastKnownSector != null && item.LastKnownSectorPosition.HasValue)
					{
						scene = item.LastKnownSector;
						sectorPosition = item.LastKnownSectorPosition.Value;
						return true;
					}
				}
			}
			return false;
		}

		public int GetBountyCountOnPilot(Person person)
		{
			return GetBountiesOnPerson(person)?.Count ?? 0;
		}

		public Faction GetFactionPrefabById(int uniqueId)
		{
			Faction value = null;
			if (factionPrefabIdMap.TryGetValue(uniqueId, out value))
			{
				return value;
			}
			return null;
		}

		public List<Faction> GetFactionPrefabs()
		{
			return factionPrefabIdMap.Values.ToList();
		}

		public int GetBountyValueOnPerson(Person person)
		{
			List<FactionBountyItem> value = null;
			if (bountiesByPlayer.TryGetValue(person.UniqueId, out value))
			{
				int num = 0;
				{
					foreach (FactionBountyItem item in value)
					{
						if (item.IsValid)
						{
							num += item.Bounty;
						}
					}
					return num;
				}
			}
			return 0;
		}

		internal void AddBountyOnPerson(Person person, FactionBountyItem bountyItem)
		{
			List<FactionBountyItem> value = null;
			if (!bountiesByPlayer.TryGetValue(person.UniqueId, out value))
			{
				value = new List<FactionBountyItem>();
				bountiesByPlayer.Add(person.UniqueId, value);
			}
			if (!value.Contains(bountyItem))
			{
				value.Add(bountyItem);
			}
		}

		public void RemoveBounty(FactionBountyItem item)
		{
			if (!(item.Person != null))
			{
				return;
			}
			List<FactionBountyItem> value = null;
			if (bountiesByPlayer.TryGetValue(item.Person.UniqueId, out value))
			{
				value.Remove(item);
				if (value.Count == 0)
				{
					bountiesByPlayer.Remove(item.Person.UniqueId);
				}
			}
		}

		internal void RemoveBountiesOnPersonInternal(Person pilot)
		{
			bountiesByPlayer.Remove(pilot.UniqueId);
		}

		public void RemoveBountiesOnPersonPlacedByFaction(Person pilot, Faction faction)
		{
			List<FactionBountyItem> value = null;
			if (!bountiesByPlayer.TryGetValue(pilot.UniqueId, out value))
			{
				return;
			}
			for (int i = 0; i < value.Count; i++)
			{
				if (value[i].Source == faction)
				{
					value.RemoveAt(i);
					i--;
				}
			}
			if (value.Count == 0)
			{
				bountiesByPlayer.Remove(pilot.UniqueId);
			}
		}

		public void TogglePause()
		{
			IsPaused = !IsPaused;
		}

		public bool AllowUiMessagesNavigation()
		{
			if (localPlayer != null)
			{
				return World.Permissions.AllowDockUIMessages;
			}
			return false;
		}

		public static IEnumerable<ComponentClass> LoadComponentClasses()
		{
			return UnityObjectHelper.LoadAll<ComponentClass>("Prefabs/WorldData/ComponentClasses");
		}

		public ActiveUnit GetOrLoadActiveUnitPrefab(UnitClass unitClass)
		{
			ActiveUnit activeUnit = GameController.Instance.ActiveUnitPrefabCache.GetActiveUnit(this, unitClass.DisplayData.ActiveUnitClassName);
			if (activeUnit == null)
			{
				activeUnit = LoadActiveUnit(unitClass.DisplayData.ActiveUnitClassName);
			}
			return activeUnit;
		}

		public ActiveUnit InstantiateActiveUnit(UnitClass unitClass)
		{
			ActiveUnit orLoadActiveUnitPrefab = GetOrLoadActiveUnitPrefab(unitClass);
			if (orLoadActiveUnitPrefab == null)
			{
				Debug.LogError($"Prefab for unitClass {unitClass} is not found", this);
				return null;
			}
			return UnityEngine.Object.Instantiate(orLoadActiveUnitPrefab);
		}

		public static ActiveUnit LoadActiveUnit(string className)
		{
			return UnityObjectHelper.Load<ActiveUnit>("Prefabs/WorldData/ActiveUnits/" + className);
		}

		public static ActiveUnit LoadAndInstantiateActiveUnit(string className)
		{
			return UnityEngine.Object.Instantiate(UnityObjectHelper.Load<ActiveUnit>("Prefabs/WorldData/ActiveUnits/" + className));
		}

		public static GameObject LoadActiveUnitClass(string className)
		{
			return UnityObjectHelper.Load<GameObject>("Prefabs/WorldData/ActiveUnitClass/" + className);
		}

		public static UnitClass LoadUnitClass(string className)
		{
			return UnityObjectHelper.Load<UnitClass>("Prefabs/WorldData/UnitClasses/" + className);
		}

		public void RaisePlayerPropertyChangedSector(Unit unit, Sector oldSector, Sector newSector)
		{
			if (PlayerPropertyChangedSector != null)
			{
				PlayerPropertyChangedSector(unit, oldSector, newSector);
			}
		}

		public static Texture LoadSceneTexture(Sector scene)
		{
			return UnityObjectHelper.Load<Texture>("Textures/UI/SceneThumbnails/" + scene.ThumbnailImageName);
		}

		public static Faction LoadFaction(string name)
		{
			return UnityObjectHelper.Load<Faction>("Prefabs/WorldData/Factions/" + name);
		}

		public static Person LoadPerson(string name)
		{
			return UnityObjectHelper.Load<Person>("Prefabs/WorldData/People/" + name);
		}

		public static Fleet LoadFleet(string name)
		{
			return UnityObjectHelper.Load<Fleet>("Prefabs/WorldData/Fleets/" + name);
		}

		public static ActiveSectorData LoadActiveSectorData(string name)
		{
			return UnityObjectHelper.Load<ActiveSectorData>("Prefabs/WorldData/ActiveSectorData/" + name);
		}

		public static ActiveSectorData LoadAndCreateActiveScene(string resourceName)
		{
			if (!string.IsNullOrEmpty(resourceName))
			{
				ActiveSectorData activeSectorData = LoadActiveSectorData(resourceName);
				if (activeSectorData != null)
				{
					return UnityEngine.Object.Instantiate(activeSectorData.gameObject, Vector3.zero, Quaternion.identity).GetComponent<ActiveSectorData>();
				}
			}
			Debug.LogWarning("Failed to load active scene. ResourceName: " + resourceName);
			return null;
		}

		public static IEnumerable<Sector> LoadSectorPrefabs()
		{
			return UnityObjectHelper.LoadAll<Sector>("Prefabs/WorldData/Sectors");
		}

		public static IEnumerable<UnitClass> LoadUnitClasses()
		{
			return UnityObjectHelper.LoadAll<UnitClass>("Prefabs/WorldData/UnitClasses");
		}

		public static void ShowPlayerCargoTfrMsg(CargoClass cargoClass, int quantity)
		{
			if (quantity != 0 && UIController.Instance.QuickMsg != null)
			{
				int num = Mathf.Abs(quantity);
				if (num > 1)
				{
					UIController.Instance.QuickMsg.AddMessage(string.Format("{0}x {1} {2}", num, cargoClass.ClassName, (quantity > 0) ? "Added" : "Removed"));
				}
				else
				{
					UIController.Instance.QuickMsg.AddMessage(string.Format("{0} {1}", cargoClass.ClassName, (quantity > 0) ? "Added" : "Removed"));
				}
			}
		}

		public void InitialisePools()
		{
			Pooler.CreatePool("Prefabs/WorldData/Projectiles", 3);
			Pooler.CreatePool("Prefabs/WorldData/Lasers", 3);
			Pooler.CreatePool("Prefabs/WorldData/Particles", 3);
			Pooler.CreatePool("Prefabs/WorldData/AudioSources", 3);
			Pooler.CreatePool("Prefabs/WorldData/AnimatedUnitExplosions", 3);
			Pooler.CreatePool("Prefabs/WorldData/Debris", 3);
			Pooler.CreatePool("Prefabs/WorldData/TrailRenderers", 3);
		}

		public void LoadWorldData()
		{
			Debug.Log("Engine looking for world data...", this);
			LoadAllFactionTypes();
			LoadAllFactionPrefabs();
			LoadAllCargoClasses();
			LoadAllPilotRanksAndRankingSystems();
			LoadAllMissionData();
			LoadAllUnitClasses();
			LoadAllDialogProfiles();
			LoadAllComponentBayTypes();
			LoadAllComponentTypes();
			LoadAllComponentClasses();
			LoadAllMissionData();
			engineResources.Load(this);
			Debug.Log("Engine finished looking for world data...", this);
		}

		public void SetUIFromPlayerStatus(bool forceExitFromHypersleep = false)
		{
			if (world.ObjectiveState == WorldBase.ScenarioState.Playing || world.ObjectiveState == WorldBase.ScenarioState.Completing)
			{
				if (!forceExitFromHypersleep && HypersleepHelper.IsHypersleepActive())
				{
					return;
				}
				if (PlayerUnit != null)
				{
					if (!PlayerUnit.IsDestroyed)
					{
						if (!PlayerUnit.IsDocked && PlayerUnit.UnitClass.IsPilottable && PlayerUnit.IsOwnedByPlayer)
						{
							ShowHud();
							return;
						}
						ShowDockUI();
						Instance.CameraOrbitPlayerUnit();
					}
				}
				else
				{
					Debug.LogError("Cannot set UI state. No current player unit", this);
				}
			}
			else if (world.ObjectiveState == WorldBase.ScenarioState.Complete)
			{
				EndGameUI loadedScreen = UIController.Instance.ScreenNavigator.GetLoadedScreen<EndGameUI>();
				if (loadedScreen != null)
				{
					loadedScreen.gameObject.SetActive(value: true);
				}
			}
		}

		public void CleanupEmptyFleets()
		{
			if (fleets.Count <= 0)
			{
				return;
			}
			badGroupCheckIndex++;
			if (badGroupCheckIndex >= fleets.Count)
			{
				badGroupCheckIndex = 0;
			}
			Fleet fleet = fleets[badGroupCheckIndex];
			if (fleet != null)
			{
				if (IsFleetNeedingDestruction(fleet))
				{
					fleet.SafeDestroy();
				}
			}
			else
			{
				fleets.RemoveAt(badGroupCheckIndex);
			}
		}

		public DialogProfile GetDialogProfileById(int id)
		{
			DialogProfile value = null;
			if (dialogProfileMap.TryGetValue(id, out value))
			{
				return value;
			}
			return null;
		}

		public bool ContainsPersonById(int id)
		{
			return personIdMap.ContainsKey(id);
		}

		public Person GetPersonById(int id)
		{
			Person value = null;
			if (personIdMap.TryGetValue(id, out value))
			{
				return value;
			}
			return null;
		}

		public bool ContainsUnitById(int id)
		{
			return unitIdMap.ContainsKey(id);
		}

		public Unit GetUnitByid(int id)
		{
			Unit value = null;
			if (unitIdMap.TryGetValue(id, out value))
			{
				return value;
			}
			return null;
		}

		public Sector GetSectorById(int id)
		{
			Sector value = null;
			if (sectorIdMap.TryGetValue(id, out value))
			{
				return value;
			}
			return null;
		}

		public bool ContainsSectorById(int id)
		{
			return sectorIdMap.ContainsKey(id);
		}

		public Faction GetFactionByid(int id)
		{
			Faction value = null;
			if (factionIdMap.TryGetValue(id, out value))
			{
				return value;
			}
			return null;
		}

		public bool ContainsFactionById(int id)
		{
			return factionIdMap.ContainsKey(id);
		}

		/// <summary>
		/// Moves a registered faction to a different id.
		/// </summary>
		/// <remarks>
		/// The map is private, so a caller that wants a faction numbered from
		/// somewhere of its own choosing - the reserved bands multiplayer uses
		/// for anything a player made - would otherwise have to reach in by
		/// reflection. Better as an operation the engine offers, because it
		/// has to keep three things in step: the map, the faction's own id,
		/// and the counter that would otherwise hand the old number out again
		/// to something else.
		///
		/// Everything referring to the faction still does, because those hold
		/// the faction rather than its id. Only code that stored the id
		/// separately is affected, and there is none that matters: saves
		/// record ids, which is the case this exists for and the reason a
		/// world saved before it may load with a faction at the old number.
		/// </remarks>
		/// <returns>False when the new id is already taken.</returns>
		/// <summary>
		/// Moves a registered unit to a different id, keeping the engine's map
		/// in step.
		/// </summary>
		/// <remarks>
		/// The map is private, and <see cref="Unit.SetUniqueId"/> only writes
		/// a field - which is enough for the exporter, because the exporter
		/// reads the field, and not enough for anything that looks a unit up by
		/// its number. A unit renumbered without being re-registered is a unit
		/// the world cannot find.
		///
		/// That is exactly what happened to a player's shuttle. It was created,
		/// renumbered into the reserved band so that "a player made this" is
		/// answerable from the number, and handed to the joiner. The exporter
		/// found it, because the exporter reads the field - which is why the
		/// joiner received a ship at that number - and every lookup found
		/// nothing, because the lookup uses the map. The joiner flew a ship
		/// the host did not have.
		///
		/// The counterpart to <see cref="ReassignFactionId"/>, and for the same
		/// reason: numbering something from outside the engine has to be
		/// something the engine can do.
		///
		/// The counter is deliberately not advanced, for the same reason as
		/// there - the map is what stops a number being handed out twice.
		/// </remarks>
		/// <returns>False when the new id is already taken.</returns>
		public bool ReassignUnitId(Unit unit, int newId)
		{
			if (unit == null)
			{
				return false;
			}

			if (unitIdMap.ContainsKey(newId))
			{
				return false;
			}

			unitIdMap.Remove(unit.UniqueId);
			unit.SetUniqueId(newId);
			unitIdMap[newId] = unit;

			return true;
		}

		public CargoClass GetCargoClassById(int id)
		{
			CargoClass value = null;
			if (cargoClassMap.TryGetValue(id, out value))
			{
				return value;
			}
			return null;
		}

		public ComponentClass GetComponentClassById(int id)
		{
			ComponentClass value = null;
			if (componentClasses.TryGetValue(id, out value))
			{
				return value;
			}
			return null;
		}

		public UnitClass GetUnitClassById(int id)
		{
			UnitClass value = null;
			if (unitClassMap.TryGetValue(id, out value))
			{
				return value;
			}
			return null;
		}

		public FleetOrder GetFleetOrderById(int id)
		{
			if (fleetOrderIdMap.TryGetValue(id, out var value))
			{
				return value;
			}
			return null;
		}

		public bool ContainsFleetOrderById(int id)
		{
			return fleetOrderIdMap.ContainsKey(id);
		}

		public Fleet GetFleetByid(int id)
		{
			if (fleetIdMap.TryGetValue(id, out var value))
			{
				return value;
			}
			return null;
		}

		public bool ContainsFleetById(int id)
		{
			return fleetIdMap.ContainsKey(id);
		}

		public PassengerGroup GetPassengerGroupByid(int id)
		{
			if (passengerGroupMap.TryGetValue(id, out var value))
			{
				return value;
			}
			return null;
		}

		public bool ContainsPassengerGroupById(int id)
		{
			return passengerGroupMap.ContainsKey(id);
		}

		public MissionSpec GetMissionSpecByid(int id)
		{
			MissionSpec value = null;
			if (jobsById.TryGetValue(id, out value))
			{
				return value;
			}
			return null;
		}

		public bool ContainsJobByid(int id)
		{
			return jobsById.ContainsKey(id);
		}

		public TriggerGroup GetTriggerGroupById(int id)
		{
			return triggerGroupsById.GetValueOrDefault(id);
		}

		public bool ContainsTriggerGroupById(int id)
		{
			return triggerGroupsById.ContainsKey(id);
		}

		public MissionData GetMissionDataById(int id)
		{
			MissionData value = null;
			if (missionDatas.TryGetValue(id, out value))
			{
				return value;
			}
			return null;
		}

		public Mission GetMissionById(int id)
		{
			Mission value = null;
			if (missionsById.TryGetValue(id, out value))
			{
				return value;
			}
			return null;
		}

		public bool ContainsMissionById(int id)
		{
			return missionsById.ContainsKey(id);
		}

		public MissionObjective GetMissionObjectiveByid(int id)
		{
			return missionObjectivesById.GetValueOrDefault(id);
		}

		public bool ContainsMissionObjectiveById(int id)
		{
			return missionObjectivesById.ContainsKey(id);
		}

		public AIPatrolPath GetPatrolPathByid(int id)
		{
			AIPatrolPath value = null;
			if (aiPatrolPathsById.TryGetValue(id, out value))
			{
				return value;
			}
			return null;
		}

		public bool ContainsPatrolPathById(int id)
		{
			return aiPatrolPathsById.ContainsKey(id);
		}

		/// <summary>A unit has entered the world for the first time.</summary>
		/// <remarks>
		/// Raised once per unit, from the one place every unit passes through
		/// on its way in. The rule this project works to is that the host
		/// tells clients everything, and the obvious way to do that - find
		/// every place a ship can be built and announce it from each - is a
		/// list that is wrong the moment somebody builds a ship somewhere new.
		///
		/// So it is one seam instead, and the multiplayer side is the only
		/// thing that listens. Singleplayer installs nothing and never sees it.
		///
		/// Note the unit has no sector yet: callers set unit.Sector after
		/// registering, so a listener has to wait a frame before it can say
		/// where the thing is.
		/// </remarks>
		public event System.Action<Unit> UnitCreated;

		public void RegisterUnit(Unit unit)
		{
			if (!unitIdMap.ContainsKey(unit.UniqueId))
			{
				unitIdMap.Add(unit.UniqueId, unit);
				UnitCreated?.Invoke(unit);
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Engine registering unit: {unit} ID: {unit.UniqueId}", unit, 3);
				}
				if (unit.IsStationOrShip())
				{
					RegisterUpdatableUnit(unit);
				}
				if (unit.UnitClass.StationPurpose != StationPurpose.None)
				{
					List<Unit> value = null;
					if (!unitsByStationPurpose.TryGetValue((int)unit.UnitClass.StationPurpose, out value))
					{
						value = new List<Unit>(100);
						unitsByStationPurpose[(int)unit.UnitClass.StationPurpose] = value;
					}
					value.Add(unit);
				}
				AddUnitByClass(unit);
				AddUnitByType(unit);
			}
			else
			{
				Debug.LogError($"Already registered unit {unit} with ID: {unit.UniqueId}", unit);
			}
		}

		private void RegisterUpdatableUnit(Unit unit)
		{
			int num = -1;
			if (freeUnitIndices.Count > 0)
			{
				num = freeUnitIndices[freeUnitIndices.Count - 1];
				freeUnitIndices.RemoveAt(freeUnitIndices.Count - 1);
				if (updatableUnits[num] != null && updatableUnits[num].internalUnitIndex > -1)
				{
					Debug.LogError($"Units collection index collision: Found a unit {updatableUnits[num]} that was marked as free", updatableUnits[num]);
					num = updatableUnits.Count;
					updatableUnits.Add(unit);
				}
				else
				{
					updatableUnits[num] = unit;
				}
			}
			else
			{
				num = updatableUnits.Count;
				updatableUnits.Add(unit);
			}
			unit.internalUnitIndex = num;
		}

		internal void AddUnitToShipsByFactionType(Unit unit)
		{
			if (ShouldAddUnitToShipsByFactionType(unit))
			{
				List<Unit> value = null;
				if (!shipsByFactionType.TryGetValue(unit.Faction.FactionType, out value))
				{
					value = new List<Unit>(20);
					shipsByFactionType[unit.Faction.FactionType] = value;
				}
				value.Add(unit);
			}
		}

		private float CalculateTotalFactionTypeShipCapWeight()
		{
			float num = 0f;
			foreach (FactionShipCapType factionShipCapType in factionShipCapTypes)
			{
				num += factionShipCapType.Weighting;
			}
			return num;
		}

		public int GetFactionTypeShipCap(FactionTypeInfo factionTypeInfo)
		{
			if (factionTypeInfo.ShipCapType == null)
			{
				return 0;
			}
			if (!cachedTotalFactionTypeShipCapWeight.HasValue)
			{
				cachedTotalFactionTypeShipCapWeight = CalculateTotalFactionTypeShipCapWeight();
			}
			return Mathf.CeilToInt(factionTypeInfo.ShipCapType.Weighting / cachedTotalFactionTypeShipCapWeight.Value * (float)AIShipCap);
		}

		public int GetCountOfFactionType(FactionType factionType, Func<Faction, bool> filter = null)
		{
			int num = 0;
			foreach (Faction faction in Factions)
			{
				if (faction.FactionType == factionType && (filter == null || filter(faction)))
				{
					num++;
				}
			}
			return num++;
		}

		internal static bool ShouldAddUnitToShipsByFactionType(Unit unit)
		{
			if (unit.UnitType == UnitType.Ship && unit.UnitClass.ShipType == ShipType.Normal && unit.Faction != null && unit.Faction.FactionType != FactionType.None)
			{
				return unit.Faction.FactionType != FactionType.Player;
			}
			return false;
		}

		internal void RemoveUnitFromShipsByFactionType(Unit unit)
		{
			if (ShouldAddUnitToShipsByFactionType(unit))
			{
				List<Unit> value = null;
				if (shipsByFactionType.TryGetValue(unit.Faction.FactionType, out value))
				{
					value.Remove(unit);
				}
			}
		}

		private void AddUnitByType(Unit unit)
		{
			List<Unit> value = null;
			if (!unitsByType.TryGetValue(unit.UnitType, out value))
			{
				value = new List<Unit>(100);
				unitsByType[unit.UnitType] = value;
			}
			value.Add(unit);
		}

		private void AddUnitByClass(Unit unit)
		{
			List<Unit> value = null;
			if (!unitsByClass.TryGetValue(unit.UnitClass.UniqueID, out value))
			{
				value = new List<Unit>(100);
				unitsByClass[unit.UnitClass.UniqueID] = value;
			}
			value.Add(unit);
		}

		public void OnUnitInitialised(Unit unit)
		{
			if (UnitInitialised != null)
			{
				UnitInitialised(this, unit);
			}
		}

		public void DeregisterUnit(Unit unit)
		{
			if (unitIdMap.Remove(unit.UniqueId))
			{
				unit.Sector = null;
				if (unit.IsStationOrShip())
				{
					DeregisterUpdatableUnit(unit);
				}
				RemoveUnitFromCountStats(unit);
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Engine deregistering unit: {unit}", unit, 3);
				}
			}
		}

		private void DeregisterUpdatableUnit(Unit unit)
		{
			if (unit.internalUnitIndex > -1)
			{
				freeUnitIndices.Add(unit.internalUnitIndex);
				updatableUnits[unit.internalUnitIndex] = null;
				unit.internalUnitIndex = -1;
			}
		}

		private void RemoveUnitFromCountStats(Unit unit)
		{
			if (unit.UnitClass.StationPurpose != StationPurpose.None)
			{
				List<Unit> value = null;
				if (unitsByStationPurpose.TryGetValue((int)unit.UnitClass.StationPurpose, out value))
				{
					value.Remove(unit);
				}
			}
			List<Unit> value2 = null;
			if (unitsByClass.TryGetValue(unit.UnitClass.UniqueID, out value2))
			{
				value2.Remove(unit);
			}
			List<Unit> value3 = null;
			if (unitsByType.TryGetValue(unit.UnitType, out value3))
			{
				value3.Remove(unit);
			}
		}

		public void RegisterSector(Sector sector)
		{
			if (!sectorIdMap.ContainsKey(sector.UniqueId))
			{
				sectorIdMap.Add(sector.UniqueId, sector);
				Sectors.Add(sector);
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Engine registering sector: {sector} ID: {sector.UniqueId}", sector, 3);
				}
			}
		}

		public void DeregisterSector(Sector sector)
		{
			if (sectorIdMap.Remove(sector.UniqueId))
			{
				Sectors.Remove(sector);
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Engine deregistering sector: {sector}", sector, 3);
				}
			}
		}

		public void RegisterFaction(Faction faction)
		{
			if (factionIdMap.ContainsKey(faction.UniqueId))
			{
				return;
			}
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"Engine registering faction: {faction}", faction, 3);
			}
			factionIdMap.Add(faction.UniqueId, faction);
			Factions.Add(faction);
			foreach (Faction faction2 in Factions)
			{
				if (faction2 != faction)
				{
					faction2.OnNewGameFaction(faction);
				}
			}
		}

		public void DeregisterFaction(Faction faction)
		{
			if (factionIdMap.Remove(faction.UniqueId))
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Engine deregistering faction: {faction}", faction, 3);
				}
				Factions.Remove(faction);
				for (int i = 0; i < Factions.Count; i++)
				{
					Factions[i].RemoveAttitude(faction);
				}
			}
		}

		public void OnWarDeclared(FactionWarDeclaration warDeclaration)
		{
			if (LogWrapper.LogMsgs)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine($"War has been declared by {warDeclaration.Aggressor} aginst {warDeclaration.Defender}. Motiviation: {warDeclaration.WarMotivation}");
				if (warDeclaration.AggressorFactionsJoined.Any())
				{
					stringBuilder.AppendLine("Aggressor is joined by: " + string.Join(", ", warDeclaration.AggressorFactionsJoined));
				}
				if (warDeclaration.DefenderFactionsJoined.Any())
				{
					stringBuilder.AppendLine("Defender is joined by: " + string.Join(", ", warDeclaration.DefenderFactionsJoined));
				}
				if (warDeclaration.AggressorFactionsJoined.Any())
				{
					stringBuilder.AppendLine("Aggressor friends declined to join: " + string.Join(", ", warDeclaration.GetAggressorsDeclinedToJoin()));
				}
				if (warDeclaration.DefenderFactionsJoined.Any())
				{
					stringBuilder.AppendLine("Defender friends declined to join: " + string.Join(", ", warDeclaration.GetDefendersDeclinedToJoin()));
				}
				LogWrapper.Log(stringBuilder.ToString(), this, 1);
			}
		}

		public void RegisterFleet(Fleet group)
		{
			if (!fleetIdMap.ContainsKey(group.UniqueId))
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Engine registering AIGroup: {group}", group, 3);
				}
				fleetIdMap.Add(group.UniqueId, group);
				fleets.Add(group);
			}
		}

		public void DeregisterFleet(Fleet group)
		{
			if (fleetIdMap.Remove(group.UniqueId))
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Engine deregistering AIGroup: {group}", group, 3);
				}
				fleets.Remove(group);
			}
		}

		public void RegisterPassengerGroup(PassengerGroup group)
		{
			if (!passengerGroupMap.ContainsKey(group.UniqueId))
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Engine registering PassengerGroup: {group}", null, 3);
				}
				passengerGroupMap.Add(group.UniqueId, group);
			}
		}

		public void DeregisterPassengerGroup(PassengerGroup group)
		{
			if (passengerGroupMap.Remove(group.UniqueId) && LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"Engine deregistering PassengerGroup: {group}", null, 3);
			}
		}

		public List<MissionSpec> GetJobsAtUnit(Unit unit)
		{
			List<MissionSpec> value = null;
			if (missionSpecsByUnit.TryGetValue(unit.UniqueId, out value))
			{
				return value;
			}
			return null;
		}

		public void RegisterMissionSpecAtUnit(Unit unit, MissionSpec missionSpec)
		{
			List<MissionSpec> value = null;
			if (!missionSpecsByUnit.TryGetValue(unit.UniqueId, out value))
			{
				value = new List<MissionSpec>(8);
				missionSpecsByUnit[unit.UniqueId] = value;
			}
			value.Add(missionSpec);
		}

		public void DeregisterMissionSpecFromUnit(Unit unit, MissionSpec missionSpec)
		{
			List<MissionSpec> value = null;
			if (missionSpecsByUnit.TryGetValue(unit.UniqueId, out value))
			{
				value.Remove(missionSpec);
				if (value.Count == 0)
				{
					missionSpecsByUnit.Remove(unit.UniqueId);
				}
			}
		}

		public void RegisterMissionSpec(MissionSpec spec)
		{
			if (!jobsById.ContainsKey(spec.UniqueId))
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Engine registering MissionSpec: {spec}", spec, 3);
				}
				jobsById.Add(spec.UniqueId, spec);
			}
		}

		public void DeregisterMissionSpec(MissionSpec spec)
		{
			if (jobsById.Remove(spec.UniqueId) && LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"Engine deregistering MissionSpec: {spec}", spec, 3);
			}
		}

		public void RegisterMission(Mission mission)
		{
			if (!missionsById.ContainsKey(mission.UniqueId))
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Engine registering Mission: {mission}", mission, 3);
				}
				missionsById.Add(mission.UniqueId, mission);
				if (MissionsChanged != null)
				{
					MissionsChanged(this);
				}
			}
		}

		public void DeregisterMission(Mission mission)
		{
			if (missionsById.Remove(mission.UniqueId))
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Engine deregistering Mission: {mission}", mission, 3);
				}
				if (MissionsChanged != null)
				{
					MissionsChanged(this);
				}
			}
		}

		public void RegisterMissionObjective(MissionObjective objective)
		{
			if (!missionObjectivesById.ContainsKey(objective.UniqueId))
			{
				missionObjectivesById.Add(objective.UniqueId, objective);
			}
		}

		public void DeregisterMissionObjective(MissionObjective missionObjective)
		{
			missionObjectivesById.Remove(missionObjective.UniqueId);
		}

		public void RegisterPath(AIPatrolPath path)
		{
			if (!aiPatrolPathsById.ContainsKey(path.UniqueId))
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Engine registering Path: {path}", path, 3);
				}
				aiPatrolPathsById.Add(path.UniqueId, path);
			}
		}

		public void DeregisterPath(AIPatrolPath path)
		{
			if (aiPatrolPathsById.Remove(path.UniqueId) && LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"Engine deregistering Path: {path}", path, 3);
			}
		}

		public void RegisterFleetOrder(FleetOrder objective)
		{
			if (!fleetOrderIdMap.ContainsKey(objective.UniqueId))
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Engine registering AIObjective: {objective}", objective, 3);
				}
				fleetOrderIdMap.Add(objective.UniqueId, objective);
			}
		}

		public void DeregisterFleetOrder(FleetOrder objective)
		{
			if (fleetOrderIdMap.Remove(objective.UniqueId) && LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"Engine deregistering AIObjective: {objective}", objective, 3);
			}
		}

		public void OnBountyPlacedOnPlayerPilot(Person targetPerson, Faction faction, int bountyValue)
		{
			string text = (targetPerson.IsLocalPlayer ? "you" : targetPerson.ShortNameWithFullRank);
			string msg = TextFormattingHelper.FormatCredits(bountyValue) + " bounty placed on " + text + " by " + faction.GetShortNameElseLong();
			UIController.Instance.QuickMsg.AddMessage(msg, 2f);
		}

		public void RegisterPerson(Person person)
		{
			if (!personIdMap.ContainsKey(person.UniqueId))
			{
				personIdMap.Add(person.UniqueId, person);
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Engine registering pilot: {person} ID: {person.UniqueId}", person, 3);
				}
			}
		}

		public void DeregisterPerson(Person person)
		{
			if (personIdMap.Remove(person.UniqueId) && LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"Engine deregistering pilot: {person}", person, 3);
			}
		}

		public void RegisterTriggerGroup(TriggerGroup triggerGroup)
		{
			if (!triggerGroupsById.ContainsKey(triggerGroup.UniqueId))
			{
				triggerGroupsById.Add(triggerGroup.UniqueId, triggerGroup);
			}
		}

		public void DeregisterTriggerGroup(TriggerGroup triggerGroup)
		{
			triggerGroupsById.Remove(triggerGroup.UniqueId);
		}

		public Color GetUnitShieldColor(Unit unit, int shieldIndex)
		{
			return GetUnitShieldColor(unit.Components, shieldIndex);
		}

		public Color GetUnitShieldColor(UnitComponentHolder unit, int shieldIndex)
		{
			float num = 0f;
			if (unit != null && unit.ShieldComponent != null && unit.ShieldEnabled && shieldIndex > -1 && shieldIndex < 6)
			{
				num = unit.ShieldComponent.GetShieldPointNormalized(shieldIndex);
			}
			if (num == 0f)
			{
				return NoShieldColor;
			}
			return Drawing.MultiLevelColorLerp(ShieldColors, 1f - num);
		}

		public Color GetUnitHullColor(Unit unit)
		{
			if (unit.Destructable != null)
			{
				return GetHullColor(unit.Destructable.HealthNormalized);
			}
			return GetHullColor(1f);
		}

		public Color GetPriceColor(TradeType tradeType, CargoClass cargoClass, float price)
		{
			float num = price / (float)cargoClass.BasePrice;
			float num2 = 0f;
			return Drawing.MultiLevelColorLerp(val: (tradeType != TradeType.Buy) ? (0.5f + (num - 1f) / EconomySettings.CargoPriceMultiplierColorAffect) : (0.5f + (1f - num) / EconomySettings.CargoPriceMultiplierColorAffect), colors: PriceColors);
		}

		public Color GetHullColor(float healthNormalized)
		{
			return Drawing.MultiLevelColorLerp(HullColors, 1f - healthNormalized);
		}

		public Color GetPilotHostilityColor(Person pilot, Person pilot2)
		{
			return GetFactionHostilityColor((pilot != null) ? pilot.Faction : null, (pilot2 != null) ? pilot2.Faction : null);
		}

		public Color GetFactionHostilityColorForPlayerTarget(Faction faction2)
		{
			return GetFactionHostilityColor(faction2, instance.LocalFaction);
		}

		public Color GetFactionHostilityColor(Faction faction1, Faction faction2)
		{
			if (faction1 != faction2)
			{
				if (faction1 == null || faction2 == null)
				{
					return AttitudeNeutralColor;
				}
				if (faction1.IsHostileToOrAlwaysHostileTo(faction2))
				{
					return HostilityColor;
				}
				if (faction1.IsAlliedTo(faction2))
				{
					return AlliedColor;
				}
				return GetFactionHostilityColor(faction1.GetOpinionNormalized(faction2));
			}
			return OwnedColor;
		}

		public Color GetFactionHostilityColor(float attitude)
		{
			return Drawing.MultiLevelColorLerp(AttitudeColors, attitude);
		}

		public Color GetSecurityColor(float security)
		{
			return Drawing.MultiLevelColorLerp(GameController.Instance.GameSettings.ColorSettings.SecurityColors, security);
		}

		public void Pause()
		{
			IsPaused = true;
		}

		public void Resume(float timeScale = 1f)
		{
			if (timeScale <= 0f)
			{
				Debug.LogError($"Should not resume at time scale of {timeScale}", this);
			}
			else
			{
				Time.timeScale = timeScale;
			}
		}

		public void PilotCurrentUnit()
		{
			PlayerUnit.Components.PilotPerson = LocalPlayer.Person;
			ShowHud();
		}

		public void ShowHud()
		{
			SetActiveSceneToPlayerUnit();
			UIController.Instance.ScreenNavigator.NavigateToRootScreen(Hud);
			InitialiseCamerasForHud();
			hud.AutoScanner.ClearTargetCache();
			hud.ClearTargetIfInvalid();
			hud.HUDScannerDisplayListController.RefreshItems();
			hud.HudTargetController.ClearActiveTargets();
			if (localPlayer != null)
			{
				localPlayer.WaypointController.RefreshAllPaths();
			}
		}

		public void InitialiseCamerasForHud()
		{
			TrySetCameraSpectatorEnabled(enabled: false);
			TrySetHudCameraEnabled(enabled: true);
		}

		public void TrySetCameraSpectatorEnabled(bool enabled)
		{
			CameraSpectator cameraSpectator = CameraSpectator;
			if (cameraSpectator != null)
			{
				cameraSpectator.enabled = enabled;
			}
		}

		public void CameraStartOrbitUnit(Unit unit)
		{
			CameraStartSpectateUnit(unit, allowFlyby: false, allowViewport: false, allowOrbit: true);
		}

		public void CameraStartSpectateUnit(Unit unit, bool allowFlyby, bool allowViewport, bool allowOrbit)
		{
			if (unit != null)
			{
				ActiveSector = unit.Sector;
				TrySetHudCameraEnabled(enabled: false);
				CameraSpectator cameraSpectator = CameraSpectator;
				if (cameraSpectator != null)
				{
					cameraSpectator.enabled = true;
					cameraSpectator.AllowFlyby = allowFlyby;
					cameraSpectator.AllowViewport = allowViewport;
					cameraSpectator.AllowOrbit = allowOrbit;
					cameraSpectator.Target = unit.gameObject;
					cameraSpectator.ChooseState();
					Instance.OnCameraMoved();
				}
				else
				{
					Debug.LogError("Cannot find CameraSpectator");
				}
			}
			else
			{
				Debug.LogError("CameraStartOrbitUnit: Null Unit");
			}
		}

		public void TrySetHudCameraEnabled(bool enabled)
		{
			if (HudCamera != null && HudCamera.enabled != enabled)
			{
				HudCamera.enabled = enabled;
				if (!enabled)
				{
					HudCamera.RemoveTarget();
				}
				else
				{
					SnapHudCameraToTarget();
				}
			}
		}

		public void DeactivateInactiveScenes()
		{
			foreach (Sector sector in Sectors)
			{
				if (sector != activeSector)
				{
					sector.gameObject.SetActive(value: false);
				}
			}
		}

		public void SetAllScenesActive(bool active)
		{
			foreach (Sector sector in Sectors)
			{
				sector.gameObject.SetActive(active);
			}
		}

		public void NotifyLocalPlayerChangedCurrentUnit(Unit oldUnit)
		{
			Unit playerUnit = PlayerUnit;
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log(string.Format("{0}: changing player CurrentUnit from: {1} to {2}", this, (oldUnit != null) ? oldUnit.ToString() : "NULL", (playerUnit != null) ? playerUnit.ToString() : "NULL"), this, 1);
			}
			if (HudCamera != null)
			{
				HudCamera.SpectateTarget = playerUnit;
				HudCamera.ResetOrientation();
			}
			if (oldUnit != null)
			{
				oldUnit.SectorChanged -= playerUnit_SectorChanged;
			}
			if (playerUnit != null)
			{
				if (Hud != null)
				{
					Hud.CurrentTarget = null;
				}
				HudCamera.RemoveTarget();
				playerUnit.SectorChanged += playerUnit_SectorChanged;
				if (playerUnit.Sector != null)
				{
					playerUnit_SectorChanged(playerUnit, null);
				}
				if (localPlayer.Faction != null && playerUnit != null && playerUnit.Faction != null && playerUnit.Faction != localPlayer.Faction)
				{
					playerUnit.Faction.CreateAttitudeIfNone(localPlayer.Faction);
				}
				localPlayer.RegisterUnitVisited(playerUnit);
				if (LoadedAndReady)
				{
					TryReplenishRootUnitMissions();
				}
			}
			AmbientSoundPlayer.CurrentShip = playerUnit;
			if (world != null)
			{
				world.OnPlayerUnitChanged(this, oldUnit);
			}
			if (PlayerUnitChanged != null)
			{
				PlayerUnitChanged(this, oldUnit);
			}
		}

		public void TryReplenishRootUnitMissions()
		{
			Unit playerRootUnit = PlayerRootUnit;
			if (playerRootUnit != null && playerRootUnit.Faction != null && playerRootUnit.Faction.CreateMissions && MissionManager != null)
			{
				MissionManager.ReplenishUnitMissions(playerRootUnit);
			}
		}

		public void NotifyLocalPlayerChangedPilotting(UnitComponentHolder ship, bool isPilotting)
		{
			if (ship != null && isPilotting)
			{
				ship.SetComponentsPowered(isPilotting);
			}
		}

		public void NotifyLocalPlayerKilled()
		{
			PlayerInCombat = false;
			if (world != null)
			{
				world.NotifyLocalPlayerKilled();
			}
		}

		public void SetHudVisibility()
		{
			if (Hud != null)
			{
				Hud.gameObject.SetActive(ShouldHudBeVisible());
			}
		}

		public void StartCustomMessageCinematic(string message, float duration, bool allowTogglePause)
		{
			StartCustomMessageCinematicWithMsgs(new string[1] { message }, duration, allowTogglePause);
		}

		public void StartCustomMessageCinematicWithMsgs(IEnumerable<string> messages, float duration, bool allowTogglePause)
		{
			StartCinematicOnPlayerUnit(allowTogglePause);
			CinematicScreen.ClearMsgQueue();
			if (messages != null)
			{
				foreach (string message in messages)
				{
					CinematicScreen.EnqueueMessage(message, duration);
				}
			}
			CreateIntroFader();
		}

		public void StartCustomCinematic(bool allowTogglePause)
		{
			CinematicScreen.ClearMsgQueue();
			CinematicScreen.FinishWhenNoMessagesLeft = false;
			StartCinematicOnPlayerUnit(allowTogglePause);
		}

		public void StartIntroCinematic()
		{
			StartCinematicOnPlayerUnit(allowTogglePause: false);
			CinematicScreen.FinishWhenNoMessagesLeft = true;
			CinematicScreen.ClearMsgQueue();
			CinematicScreen.EnqueueSceneName();
			CinematicScreen.EnqueueShipClass(getRoot: true);
			CinematicScreen.EnqueuePlayerUnitName(getRoot: true);
			CreateIntroFader();
			Pause();
		}

		public void CreateIntroFader()
		{
			if (IntroFaderPrefab != null)
			{
				UnityEngine.Object.Instantiate(IntroFaderPrefab.gameObject);
			}
		}

		public void StartCinematicOnPlayerUnit(bool allowTogglePause)
		{
			if (PlayerUnit != null)
			{
				CameraOrbitPlayerUnit();
			}
			else
			{
				Debug.LogWarning("Engine changed to Cinematic state but there is no target unit", this);
			}
			StartCinematic(allowTogglePause);
		}

		public void CameraOrbitPlayerUnit()
		{
			CameraStartOrbitUnit(PlayerUnit);
		}

		public void StartCinematic(bool allowTogglePause)
		{
			if (ActiveSector == null)
			{
				Debug.LogWarning("Engine changing to Cinematic state but does not have an ActiveScene", this);
			}
			if (Hud != null)
			{
				Hud.gameObject.SetActive(value: false);
			}
			if (CinematicScreen != null)
			{
				CinematicScreen.AllowTogglePause = allowTogglePause;
				CinematicScreen.Play();
				UIController.Instance.ScreenNavigator.NavigateToRootScreen(CinematicScreen);
			}
		}

		public void NotifyWorldLoadedAndReady()
		{
			Debug.Log("Engine: NotifyWorldLoadedAndReady", this);
			if (!IsPaused)
			{
				Time.timeScale = 1f;
			}
			if (WorldLoaded != null)
			{
				WorldLoaded(this);
			}
			if (TurretControllerSpawner != null)
			{
				TurretControllerSpawner.SpawnTurretNpcs(this);
			}
			LoadedAndReady = true;
			GameController.Instance.MainCamera.gameObject.SetActive(value: true);
		}

		public int GetSectorJumpCount(Sector sector1, Sector sector2)
		{
			if (sector1 == sector2)
			{
				return 0;
			}
			ulong num = Helper.PairId(sector1.UniqueId, sector2.UniqueId);
			int value = -1;
			if (sectorConnectionCache.TryGetValue(num, out value))
			{
				return value;
			}
			return CacheSectorConnectionDistance(sector1, sector2, num);
		}

		private int CacheSectorConnectionDistance(Sector scene1, Sector scene2, ulong connectionId)
		{
			int num = scene1.CalculateSectorJumpDistance(scene2);
			sectorConnectionCache[connectionId] = num;
			return num;
		}

		public bool AreSectorsConnectedByStableWormholes(Sector s1, Sector s2)
		{
			ulong num = Helper.PairId(s1.UniqueId, s2.UniqueId);
			if (sectorConnectionCache.TryGetValue(num, out var value))
			{
				return value >= 0;
			}
			return CacheSectorConnectionDistance(s1, s2, num) >= 0;
		}

		public string GetSaveFileName()
		{
			string text = null;
			text = ((!(localPlayer == null) && !string.IsNullOrEmpty(localPlayer.Person.Name)) ? localPlayer.Person.Name : "Player");
			if (world != null)
			{
				return $"{world.ScenarioInfo.Title}_Day{instance.GetGameDay()}_{text}";
			}
			return text;
		}

		[Obsolete]
		public AudioSource PlayPooledAudioSource(GameObject gameObject, Vector3 position)
		{
			AudioSource component = gameObject.GetComponent<AudioSource>();
			return PlayPooledAudioSource(component, position);
		}

		public AudioSource PlayPooledAudioSource(AudioSource prefab, Vector3 position)
		{
			GameObject pooledObjectOrCreate = Pooler.GetPooledObjectOrCreate(prefab.gameObject);
			pooledObjectOrCreate.gameObject.SetActive(value: true);
			AudioSource component = pooledObjectOrCreate.GetComponent<AudioSource>();
			component.enabled = true;
			component.volume = prefab.volume;
			component.transform.position = position;
			component.Play();
			return component;
		}

		public T GetPooledObject<T>(GameObject prefab) where T : Component
		{
			GameObject pooledObjectOrCreate = Pooler.GetPooledObjectOrCreate(prefab);
			pooledObjectOrCreate.gameObject.SetActive(value: true);
			return pooledObjectOrCreate.GetComponent<T>();
		}

		public ParticleSystem PlayPooledParticleSystem(GameObject prefab, Vector3 position, Quaternion rotation)
		{
			ParticleSystem pooledObject = GetPooledObject<ParticleSystem>(prefab);
			pooledObject.transform.position = position;
			pooledObject.transform.rotation = rotation;
			pooledObject.Play();
			return pooledObject;
		}

		public TrailRenderer GetPooledTrailRenderer(GameObject prefab)
		{
			GameObject pooledObjectOrCreate = Pooler.GetPooledObjectOrCreate(prefab);
			pooledObjectOrCreate.gameObject.SetActive(value: true);
			return pooledObjectOrCreate.GetComponent<TrailRenderer>();
		}

		public AnimatedUnitExplosion PlayPooledAnimatedUnitExplosion(GameObject prefab, Unit unit)
		{
			GameObject pooledObjectOrCreate = Pooler.GetPooledObjectOrCreate(prefab);
			pooledObjectOrCreate.gameObject.SetActive(value: true);
			AnimatedUnitExplosion component = pooledObjectOrCreate.GetComponent<AnimatedUnitExplosion>();
			component.transform.position = unit.transform.position;
			component.Unit = unit;
			component.Play();
			return component;
		}

		public DebrisSimple PlayPooledDebris(GameObject prefab, Vector3 position)
		{
			GameObject pooledObjectOrCreate = Pooler.GetPooledObjectOrCreate(prefab);
			pooledObjectOrCreate.gameObject.SetActive(value: true);
			DebrisSimple component = pooledObjectOrCreate.GetComponent<DebrisSimple>();
			component.transform.rotation = UnityEngine.Random.rotation;
			component.transform.position = position;
			component.transform.SetParent(null, worldPositionStays: true);
			component.Play(this);
			return component;
		}

		public void AddCreditsToPlayerFactionWithMsg(int credits, FactionTransactionType transactionType = FactionTransactionType.Unknown, Faction otherFaction = null, Unit location = null, CargoClass relatedCargoClass = null)
		{
			if (localPlayer != null && credits != 0)
			{
				instance.CreditsAnimation.AllowCreditBeepAudio = true;
				localPlayer.Faction.ApplyTransaction(credits, transactionType, otherFaction, location, relatedCargoClass);
				RaiseExchangedCreditsMessage(credits);
			}
		}

		public void RaiseExchangedCreditsMessage(int credits)
		{
			if (localPlayer != null && credits != 0)
			{
				UIController.Instance.QuickMsg.AddChangeInCreditsMessage(credits);
			}
		}

		/// <summary>
		/// Plays the win stinger, if the game has been given one.
		/// </summary>
		/// <remarks>
		/// WinAudioSourceNames is empty on the prefab, so this has always done
		/// nothing. It is kept because the field is serialized data and an
		/// empty list is a deliberate choice someone might fill in, not
		/// because anything calls it meaningfully.
		/// </remarks>
		public void PlayWinAudio()
		{
			MusicDirector.PlayJingle(WinAudioSourceNames.GetRandom());
		}

		/// <summary>
		/// Plays the lose stinger over silence.
		/// </summary>
		/// <remarks>
		/// LoseAudioSourceNames holds one track, and this is the only live
		/// user of the old audio source prefabs left in the project - which is
		/// why they were kept rather than deleted with the rest of the old
		/// music system. They are AudioSource prefabs wrapping a clip, and the
		/// clip is a defeat sting this game plays.
		/// </remarks>
		public void PlayLoseAudio()
		{
			MusicDirector.PlayJingle(LoseAudioSourceNames.GetRandom());
		}

		public void NotifyPlayerInCombat()
		{
			if (world.ObjectiveState == WorldBase.ScenarioState.Playing)
			{
				PlayerInCombat = true;
				SetPlayerCombatTimeout();
			}
		}

		public void TransferToPlayerCargoWithMsg(CargoClass cargoClass, int quantity, bool ignoreCapacity)
		{
			TransferToPlayerCargoWithMsg(LocalUnit, cargoClass, quantity, ignoreCapacity);
		}

		public void TransferToPlayerCargoWithMsg(CargoBayItem item, bool ignoreCapacity)
		{
			TransferToPlayerCargoWithMsg(LocalUnit, item.CargoClass, item.Quantity, ignoreCapacity);
		}

		public void TransferToPlayerCargoWithMsg(Unit targetUnit, CargoBayItem item, bool ignoreCapacity)
		{
			TransferToPlayerCargoWithMsg(targetUnit, item.CargoClass, item.Quantity, ignoreCapacity);
		}

		public void TransferToPlayerCargoWithMsg(Unit targetUnit, CargoClass cargoClass, int quantity, bool ignoreCapacity)
		{
			if (quantity != 0)
			{
				targetUnit.CargoBayComponent.AddToCargo(cargoClass, quantity, ignoreCapacity);
				ShowPlayerCargoTfrMsg(cargoClass, quantity);
			}
		}

		public SpeechModel.SpeechRequest RequestSpeech(Person pilot, DialogEvent dialogEvent, CompiledDialogEventHandler eventHandler, float additionalDelay, DialogRequestArguments? args)
		{
			if (Hud != null && hud.SpeechModel != null)
			{
				if (pilot == instance.LocalPerson)
				{
					return null;
				}
				if (!lastDialogEventTimes.TryGetValue(dialogEvent.UniqueId, out var value) || Time.time > value + Instance.GameSettings.SpeechModelMinTimeBeforeRepeat)
				{
					hud.SpeechModel.RequestSpeech(pilot, dialogEvent, eventHandler, additionalDelay, args);
				}
			}
			return null;
		}

		public void NotifyDialogEventMsgShown(DialogEvent dialogEvent)
		{
			lastDialogEventTimes[dialogEvent.UniqueId] = Time.time;
		}

		public void SnapHudCameraToTarget()
		{
			HudCamera.UpdateDesiredCameraOrientation();
			HudCamera.MoveCameraToDesired(instant: true);
		}

		public void NotifyUnitKilled(Unit destroyedUnit, Sector sector, Unit attackingUnit, Faction attackerFaction)
		{
			if (UnitKilled != null)
			{
				UnitKilled(this, destroyedUnit, sector, attackingUnit, attackerFaction);
			}
			if (attackerFaction != null && destroyedUnit.Faction != null)
			{
				VirtueHandler.HandleUnitKilled(attackerFaction, destroyedUnit.Faction, destroyedUnit.UnitClass);
			}
			if (destroyedUnit.Components != null && destroyedUnit.Components.PilotPerson != null && attackingUnit != null && attackingUnit.Components != null && attackingUnit.Components.PilotPerson != null && destroyedUnit.Components.PilotPerson.Faction != attackerFaction)
			{
				attackingUnit.Components.PilotPerson.AddKill(destroyedUnit);
			}
			destroyedUnitInfoController.RegisterDestroyedUnit(destroyedUnit, attackingUnit, attackerFaction);
		}

		public void NotifyPilotAboutToBeKilled(Person pilot, Faction attackerFaction)
		{
			ProcessBounties(pilot, attackerFaction);
		}

		public void ProcessBounties(Person killedPilot, Faction attackerFaction)
		{
			BountyHelper.GrantBounties(killedPilot, attackerFaction);
			BountyHelper.ReleaseBountiesOnPerson(killedPilot);
		}

		public void NotifyUnitDestroyed(Unit unit)
		{
			if (UnitDestroyed != null)
			{
				UnitDestroyed(this, unit);
			}
			if (unit.UnitClass.StationPurpose == StationPurpose.SectorControl && unit.Faction != null && unit.Faction == unit.Sector.ControllingFaction)
			{
				Faction controllingFaction = unit.Sector.ControllingFaction;
				unit.Sector.ChangeControllingFaction(null, setTimeOfChange: true);
				instance.DebugInfo.NumTimesSectorControlLostDueToDestruction++;
				if (LocalFaction != null && LocalFaction.Intel.IsSectorDiscovered(unit.Sector))
				{
					PlayerActiveMessage message = UniverseEventsNotifierController.GenerateFactionLostControlOfSectorMessage(unit.Sector, controllingFaction);
					Instance.LocalPlayer.AddMessage(message, notifications: true, important: true);
				}
			}
		}

		public void NotifyUnitFinishingDismantling(Unit unit)
		{
			if (unit.UnitClass.StationPurpose == StationPurpose.SectorControl && unit.Faction != null && unit.Faction == unit.Sector.ControllingFaction)
			{
				Faction controllingFaction = unit.Sector.ControllingFaction;
				unit.Sector.ChangeControllingFaction(null, setTimeOfChange: true);
				if (LocalFaction != null && LocalFaction.Intel.IsSectorDiscovered(unit.Sector))
				{
					PlayerActiveMessage message = UniverseEventsNotifierController.GenerateFactionLostControlOfSectorMessage(unit.Sector, controllingFaction);
					Instance.LocalPlayer.AddMessage(message);
				}
			}
		}

		public void NotifyUnitCaptured(Unit unit, Faction previousOwner, bool silent = false)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"Unit {unit} was captured by {unit.Faction} from {previousOwner}", unit, 1);
			}
			VirtueHandler.HandleUnitCaptured(unit.Faction, previousOwner, unit.UnitClass);
			if (unit.UnitClass.StationPurpose == StationPurpose.SectorControl)
			{
				OnSectorControlUnitChangedOwner(unit, previousOwner, silent);
			}
			DestroyJobsAtUnit(unit);
		}

		private void OnSectorControlUnitChangedOwner(Unit unit, Faction previousOwner, bool silent = false)
		{
			unit.Sector.ChangeControllingFaction(unit.Faction, setTimeOfChange: true);
			instance.DebugInfo.NumTimesSectorControlChangedFaction++;
			if (!silent && LocalFaction != null && LocalFaction.Intel.IsSectorDiscovered(unit.Sector))
			{
				if (previousOwner != null)
				{
					PlayerActiveMessage message = UniverseEventsNotifierController.GenerateFactionCapturesSectorMessage(unit.Sector, unit.Faction, previousOwner);
					Instance.LocalPlayer.AddMessage(message, notifications: true, important: true);
				}
				else
				{
					PlayerActiveMessage message2 = UniverseEventsNotifierController.GenerateFactionGainedControlOfSectorMessage(unit.Sector, unit.Faction);
					Instance.LocalPlayer.AddMessage(message2, notifications: true, important: true);
				}
			}
		}

		public void NotifyUnitClaimed(Unit unit, Faction claimingFaction, Fleet claimingFleet, bool silent = false)
		{
			if (unit.UnitClass.StationPurpose == StationPurpose.SectorControl && unit.Sector.ControllingFaction == null)
			{
				OnSectorControlUnitChangedOwner(unit, null, silent);
			}
			if (claimingFaction.FactionAI != null)
			{
				claimingFaction.FactionAI.OnClaimedUnit(unit, claimingFleet);
			}
		}

		public void NotifyNewStationConstructionStarted(Unit newStation, bool silent = false)
		{
			if (newStation.Faction != null)
			{
				newStation.Faction.Intel.DiscoverUnit(newStation);
			}
			if (newStation.Faction != null && newStation.UnitClass.StationPurpose == StationPurpose.SectorControl)
			{
				if (newStation.Sector.ControllingFaction != null)
				{
					Debug.LogError("Should not start construction of sector hq when sector (" + newStation.Sector.Name + ") is already controlled", newStation);
					return;
				}
				newStation.Sector.ChangeControllingFaction(newStation.Faction, setTimeOfChange: true);
				instance.DebugInfo.NumTimesSectorControlGainedDueToConstruction++;
				if (newStation.Faction.FactionType != FactionType.Empire)
				{
					instance.DebugInfo.NumTimesSectorControlGainedDueToConstructionByNonEmpire++;
				}
				if (!silent && LocalFaction != null && LocalFaction.Intel.IsSectorDiscovered(newStation.Sector))
				{
					PlayerActiveMessage message = UniverseEventsNotifierController.GenerateFactionGainedControlOfSectorMessage(newStation.Sector, newStation.Faction);
					Instance.LocalPlayer.AddMessage(message, notifications: true, important: true);
				}
			}
			if (newStation.Faction != null && newStation.Faction.FactionAI != null)
			{
				newStation.Faction.FactionAI.OnNewStationConstructionStarted(newStation);
			}
		}

		public void NotifyUnitConstructionFinished(Unit unit, bool silent = false)
		{
			if (TurretControllerSpawner.ShouldAutomateTurrets(unit))
			{
				TurretControllerSpawner.AutomateTurrets(unit);
			}
			if (!silent)
			{
				UniverseEventsNotifierController.Engine_UnitConstructed(this, unit);
			}
			if (unit.Faction != null)
			{
				if (!silent)
				{
					ConstructedStationBroadcaster.Broadcast(unit);
				}
				if (unit.Components != null && unit.Components.CargoTrader != null)
				{
					unit.Faction.InvalidateTraderTargets();
				}
				if (unit.Faction.FactionAI != null)
				{
					unit.Faction.FactionAI.OnNewStationFullyConstructed(unit);
				}
				if (!unit.UnitClass.Legal)
				{
					VirtueHandler.HandleIllegalStationConstruction(unit.Faction);
				}
			}
		}

		public void InvalidateAllTraderTargets()
		{
			foreach (Faction faction in Factions)
			{
				if (faction != null)
				{
					faction.InvalidateTraderTargets();
				}
			}
		}

		public void BroadcastStationPositionToPlayer(Unit unit)
		{
			if (LocalFaction.Intel.DiscoverUnit(unit) == DiscoverUnitResult.New)
			{
				Instance.DebugInfo.NumTimesConstructedStationBroadcastedToPlayer++;
			}
			if (ShouldSendPlayerNotificationForNewStation(unit))
			{
				SendPlayerNotificationForNewStation(unit);
			}
		}

		private void SendPlayerNotificationForNewStation(Unit unit)
		{
			PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage
			{
				ToText = "#player#",
				FromText = unit.Faction.GetLongNameElseShort(),
				SubjectText = unit.Faction.GetLongNameElseShort() + " constructs new " + unit.UnitClass.GetClassAndSeriesName(),
				MessageText = $"We have completed construction of a new {unit.UnitClass.GetClassAndSeriesName()} in the {unit.Sector.Name} sector. The coordinates of the station have been added to your database.",
				AllowDelete = true
			};
			playerActiveMessage.SetSubjectUnitAndPosition(unit);
			localPlayer.AddMessage(playerActiveMessage);
		}

		private bool ShouldSendPlayerNotificationForNewStation(Unit unit)
		{
			if (CanSendPlayNotificationForNewStation(unit) && (GameSettings.NewStationPlayerNotificationMaxJumpDistance < 0f || (float)unit.GetJumpDistTo(instance.activeSector) < GameSettings.NewStationPlayerNotificationMaxJumpDistance))
			{
				return UnityEngine.Random.value < GameSettings.NewStationPlayerNotificationProbability;
			}
			return false;
		}

		private bool CanSendPlayNotificationForNewStation(Unit unit)
		{
			if (unit.Faction == null)
			{
				return false;
			}
			if (unit.UnitType != UnitType.Station)
			{
				return false;
			}
			if (unit.Faction.HasAttitudeToFaction(LocalFaction) && unit.Engine.ScenarioElapsedTime > (double)GameSettings.NewStationPlayerNotificationMinTimeBeforeShow && unit.Faction.GetOpinion(LocalFaction) >= 0f && !unit.Faction.IsHostileToOrAlwaysHostileTo(LocalFaction))
			{
				if (!unit.UnitClass.Legal)
				{
					return unit.Faction.GetOpinion(instance.LocalFaction) > 0.5f;
				}
				if (unit.UnitType == UnitType.Station && ShouldSendNewStationNotificationForStationPurpose(unit.UnitClass.StationPurpose) && LocalFaction != null)
				{
					return unit.Faction != LocalFaction;
				}
				return false;
			}
			return false;
		}

		public bool ShouldSendNewStationNotificationForStationPurpose(StationPurpose station)
		{
			if (station == StationPurpose.Defence || station == StationPurpose.Satellite || station == StationPurpose.SectorControl)
			{
				return false;
			}
			return true;
		}

		public void MoveCamera(Vector3 position)
		{
			GameController.Instance.MainCamera.transform.position = position;
			OnCameraMoved();
		}

		public void OnCameraMoved()
		{
			if (activeSector != null)
			{
				activeSector.UpdateAllUnitsVisibility();
			}
			if (CameraMoved != null)
			{
				CameraMoved(this);
			}
		}

		public bool CanShowDialog()
		{
			if ((CinematicScreen == null || !cinematicScreen.IsPlaying) && IsPlayerPilot)
			{
				return UIController.Instance.ScreenNavigator.RealTimeSinceLastScreenChange > 2f;
			}
			return false;
		}

		public bool TryStartSectorTransition(Unit unit, Wormhole gateRequestor, Sector targetSector, Vector3? targetSectorPosition = null, Quaternion? targetLocalRotation = null)
		{
			if (targetSector == null)
			{
				Debug.LogError("Target sector is null", this);
				return false;
			}
			if (targetSector == activeSector)
			{
				Debug.LogError("Can't transition into same sector", this);
				return false;
			}
			if (sectorTransitionInfo == null)
			{
				bool wasSectorDiscovered = instance.LocalFaction.Intel.IsSectorDiscovered(targetSector);
				Instance.Hud.SpeechModel.ClearRequests();
				if (cinematicScreen != null && cinematicScreen.isActiveAndEnabled && world.ObjectiveState == WorldBase.ScenarioState.Intro)
				{
					world.ObjectiveState = WorldBase.ScenarioState.Playing;
				}
				invulnerableOnWormholeEntry = unit.Destructable.IsInvulnerable;
				if (gateRequestor != null)
				{
					gateRequestor.GetTargetSectorPosition();
				}
				else
				{
					_ = targetSectorPosition.Value;
				}
				if ((object)gateRequestor == null)
				{
					_ = targetLocalRotation.Value;
				}
				else
				{
					gateRequestor.GetTargetRotation();
				}
				UIController.Instance.ScreenNavigator.NavigateToRootScreen(null);
				timeScaleOnWormholeEntry = Time.timeScale;
				Time.timeScale = 0f;
				TrySetCameraSpectatorEnabled(enabled: false);
				TrySetHudCameraEnabled(enabled: false);
				if (GameController.Instance.PlayerOptions.Video_WormholeAnimationEnabled)
				{
					wormholeAnimator = UnityObjectHelper.InstantiateAndGetComponent(WormholeAnimationPrefab);
					Camera camera = GameController.Instance.MainCamera;
					wormholeAnimator.AnimationFinished += wormholeAnimator_AnimationFinished;
					wormholeAnimator.transform.position = camera.transform.position;
					wormholeAnimator.transform.rotation = camera.transform.rotation;
					wormholeAnimator.transform.SetParent(camera.transform, worldPositionStays: true);
					// Open Frontier: hide the sky for the jump - only the
					// tunnel should be visible (URP overlays can't clear to
					// black the way the BIRP anim camera did). Restored in
					// wormholeAnimator_AnimationFinished; both are scene
					// objects, so a mid-jump scene reload self-heals.
					if (Instance.SpaceConstructor != null)
					{
						Instance.SpaceConstructor.StaticStars.MeshRenderer.enabled = false;
						Instance.SpaceConstructor.NebulasTransform.gameObject.SetActive(value: false);
					}
				}
				PreSectorChangeCleanup();
				sectorTransitionInfo = new SectorTransitionInfo(gateRequestor, targetSector, unit)
				{
					ManualTargetLocalRotation = targetLocalRotation,
					ManualTargetSectorPosition = targetSectorPosition,
					WasSectorDiscovered = wasSectorDiscovered
				};
				if (!GameController.Instance.PlayerOptions.Video_WormholeAnimationEnabled)
				{
					wormholeAnimator_AnimationFinished(null);
				}
				return true;
			}
			Debug.LogError("Cannot start wormhole transition. Transition already in progress", this);
			return false;
		}

		public Mission GetMissionPrefab(JobType jobType)
		{
			MissionType missionType = (MissionType)jobType;
			return MissionPrefabs.FirstOrDefault((Mission e) => e.MissionType == missionType);
		}

		public Mission InstantiateMission(MissionType missionType)
		{
			return UnityEngine.Object.Instantiate(MissionPrefabs.First((Mission e) => e.MissionType == missionType));
		}

		public void PreSectorChangeCleanup()
		{
			Hud.ClearAutoTurnDesiredBearing();
			ActiveSector = null;
			Hud.gameObject.SetActive(value: false);
			SetCameraParticleSystemsActive(value: false);
		}

		public void NotifyLocalPlayerFactionReceivedDamage(Faction sourceFaction, Unit sourceUnit, float baseDamage, DamageDirectType damageDirectType)
		{
			if (LocalPlayerFactionReceivedDamage != null)
			{
				LocalPlayerFactionReceivedDamage(this, LocalFaction, sourceFaction, sourceUnit, baseDamage, damageDirectType);
			}
		}

		public void SetCameraParticleSystemsActive(bool value)
		{
			foreach (CustomParticles cameraParticleSystem in cameraParticleSystems)
			{
				if (cameraParticleSystem != null)
				{
					cameraParticleSystem.gameObject.SetActive(value);
				}
			}
		}

		public void RegisterTaxedPlayerTrade(Unit tradeStation, float tradeValue, FactionTransactionType transactionType, CargoClass relatedCargoClass = null, UnitClass relatedUnitClass = null, int? relatedCount = null)
		{
			instance.CreditsAnimation.AllowCreditBeepAudio = true;
			RaiseExchangedCreditsMessage((int)tradeValue);
			if (localPlayer.Faction != null)
			{
				localPlayer.Faction.RegisterTaxedTradeWithFaction(tradeStation, (tradeStation != null) ? tradeStation.Faction : null, tradeValue, transactionType, relatedCargoClass, relatedUnitClass, relatedCount);
			}
		}

		public void RegisterPlayerTrade(Unit tradeStation, float tradeValue, FactionTransactionType transactionType, CargoClass relatedCargoClass = null, UnitClass relatedUnitClass = null, int? relatedCount = null)
		{
			instance.CreditsAnimation.AllowCreditBeepAudio = true;
			RaiseExchangedCreditsMessage((int)tradeValue);
			if (localPlayer.Faction != null)
			{
				localPlayer.Faction.RegisterNormalTradeWithFaction(tradeStation, (tradeStation != null) ? tradeStation.Faction : null, tradeValue, transactionType, relatedCargoClass, relatedUnitClass, relatedCount);
			}
		}

		public int GetCargoCount(CargoClass cargoClass)
		{
			int num = 0;
			foreach (Sector sector in sectors)
			{
				List<Unit> list = sector.GetUnitsByType(UnitType.Cargo);
				if (list != null)
				{
					foreach (Unit item in list)
					{
						if (item.CargoComponent.CargoClass == cargoClass)
						{
							num += item.CargoComponent.Quantity;
						}
					}
				}
				List<Unit> list2 = sector.GetUnitsByType(UnitType.Ship);
				if (list2 != null)
				{
					foreach (Unit item2 in list2)
					{
						CargoBayComponent cargoBayComponent = item2.CargoBayComponent;
						if (cargoBayComponent != null)
						{
							num += cargoBayComponent.GetCountOf(cargoClass);
						}
					}
				}
				List<Unit> list3 = sector.GetUnitsByType(UnitType.Station);
				if (list3 == null)
				{
					continue;
				}
				foreach (Unit item3 in list3)
				{
					CargoBayComponent cargoBayComponent2 = item3.CargoBayComponent;
					if (cargoBayComponent2 != null)
					{
						num += cargoBayComponent2.GetCountOf(cargoClass);
					}
				}
			}
			return num;
		}

		protected virtual bool IsFleetNeedingDestruction(Fleet group)
		{
			if (group.NpcPilots.Count == 0)
			{
				return group.Settings.DestroyWhenNoPilots;
			}
			return false;
		}

		internal void NotifyUnitHangarBayChanged(Unit unit, UnitHangarBay oldBay, UnitHangarBay newBay)
		{
			if (unit.IsPlayerCurrentUnit)
			{
				if (newBay != null)
				{
					OnPlayerUnitDocked(unit);
				}
				else
				{
					SetUIFromPlayerStatus();
				}
			}
			if (UnitHangarChanged != null)
			{
				UnitHangarChanged(this, unit, oldBay, newBay);
			}
		}

		private void Awake()
		{
			DateTimeUtils = new DateAndTimeUtils(this);
			Debug.Log("Engine: Awake");
			instance = this;
		}

		public void ExitHypersleepIfActive()
		{
			if (Time.timeScale > 1f)
			{
				Time.timeScale = 1f;
			}
		}

		private IEnumerator LoadScenesCoroutine()
		{
			yield return LoadCinematicBoxScene();
			yield return LoadDialogScreenCoroutine();
			yield return LoadHudScreenCoroutine();
		}

		private IEnumerator LoadCinematicBoxScene()
		{
			ScreenNavigationRequest<CinematicScreen> request = new ScreenNavigationRequest<CinematicScreen>
			{
				LoadOnly = true
			};
			UIController.Instance.ScreenNavigator.LoadAndNavigateToScreen(request);
			yield return 0;
			CinematicScreen = request.ResultConcrete;
			CinematicScreen.gameObject.SetActive(value: false);
		}

		private IEnumerator LoadHudScreenCoroutine()
		{
			ScreenNavigationRequest<HudScreen> request = new ScreenNavigationRequest<HudScreen>
			{
				LoadOnly = true
			};
			UIController.Instance.ScreenNavigator.LoadAndNavigateToScreen(request);
			yield return 0;
			Hud = request.ResultConcrete;
			if (Hud != null)
			{
				Hud.Eng = this;
				Hud.gameObject.SetActive(value: false);
			}
			else
			{
				Debug.LogError("Failed to find HudScreen", this);
			}
		}

		private IEnumerator LoadDialogScreenCoroutine()
		{
			ScreenNavigationRequest<DialogController> request = new ScreenNavigationRequest<DialogController>
			{
				LoadOnly = true
			};
			UIController.Instance.ScreenNavigator.LoadAndNavigateToScreen(request);
			yield return 0;
			DialogController = request.ResultConcrete;
			if (DialogController != null)
			{
				DialogController.Eng = this;
				DialogController.gameObject.SetActive(value: false);
			}
			else
			{
				Debug.LogError("Failed to find DialogController", this);
			}
		}

		private void InitialiseAttributeSystem()
		{
		}

		private void Start()
		{
			Debug.Log("Engine: Start", this);
			if (GameController.Instance == null)
			{
				Debug.LogError("Engine needs GameController", this);
			}
			else
			{
				Init();
			}
		}

		private void Init()
		{
			Debug.Log("Initialising name data...", this);
			CharacterNames.Init();
			ShipNames.Load();
			FactionNames.Init();
			unitTypes = Enum.GetValues(typeof(UnitType)).Cast<UnitType>().ToArray();
			Debug.Log("Initialising pools...", this);
			InitialisePools();
			CompileFleetFormations();
			if (defaultFleetFormation == null)
			{
				Debug.LogError("No default fleet formation found");
			}
			DitchUnitModule = new DitchUnitModule();
			DitchUnitCleanupModule = new DitchUnitCleanupModule();
			WaypointController.Init();
			AdvDialogController.Init();
			EnvironmentController.Init();
			NpcPathfindingController.Init();
			StartCoroutine(LoadScenesCoroutine());
			LoadWorldData();
			CompatibleComponentCacher.Cache();
			Stats.Init();
			cachedFleetSettingsController = new CachedFleetSettingsController();
			cameraParticleSystems = GameController.Instance.MainCamera.GetComponentsInChildren<CustomParticles>(includeInactive: true).ToList();
			BarNamesController.LoadNames();
			OutlawNamesController.LoadNames();
			ActiveSectorData.Init(this);
			asteroidRespawner = new AsteroidRespawner();
			GameController.Instance.MainCamera.gameObject.SetActive(value: false);
			mainCamera = GameController.Instance.MainCamera.GetComponent<MainCamera>();
			CompileShipStats();
			factionNeutralitySyncer = new NeutralitySyncer();
			wormholeUpdater = new WormholeUpdater();
			cargoUpdater = new CargoUpdater();
			HudCamera.Init();
			hasInitialized = true;
		}

		private void CompileShipStats()
		{
			CompileShipStrategies();
			CompileShipEffectiveness();
		}

		private void CompileShipStrategies()
		{
			shipStrategyFlags.Clear();
			foreach (UnitClass unitClass in UnitClasses)
			{
				if (unitClass.UnitType != UnitType.Ship)
				{
					continue;
				}
				FactionStrategy factionStrategy = FactionStrategy.Unspecified;
				foreach (ShipPurposeItem shipPurpose in unitClass.ShipPurposes)
				{
					factionStrategy |= shipPurpose.FactionStrategy;
				}
				shipStrategyFlags[unitClass.UniqueID] = factionStrategy;
			}
		}

		private void CompileShipEffectiveness()
		{
			shipStrategyEffectiveness.Clear();
			foreach (UnitClass unitClass in UnitClasses)
			{
				if (unitClass.UnitType != UnitType.Ship)
				{
					continue;
				}
				foreach (ShipPurposeItem shipPurpose in unitClass.ShipPurposes)
				{
					ulong key = Helper.OrderedPairId(unitClass.UniqueID, (int)shipPurpose.FactionStrategy);
					shipStrategyEffectiveness.Add(key, shipPurpose.Effectiveness);
				}
			}
		}

		public FactionStrategy GetUnitClassShipStrategyFlags(UnitClass unitClass)
		{
			if (shipStrategyFlags.TryGetValue(unitClass.UniqueID, out var value))
			{
				return value;
			}
			return FactionStrategy.Unspecified;
		}

		public float GetUnitClassEffectivenessAtStrategy(UnitClass unitClass, FactionStrategy factionStrategy)
		{
			if (shipStrategyEffectiveness.TryGetValue(Helper.OrderedPairId(unitClass.UniqueID, (int)factionStrategy), out var value))
			{
				return value;
			}
			return 0f;
		}

		private void LoadAllDialogProfiles()
		{
			dialogProfileMap.Clear();
			foreach (DialogProfile item in UnityObjectHelper.LoadAll<DialogProfile>("Prefabs/WorldData/DialogProfiles"))
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(item.gameObject);
				gameObject.transform.SetParent(transform);
				gameObject.transform.localPosition = Vector3.zero;
				DialogProfile component = gameObject.GetComponent<DialogProfile>();
				component.Init();
				dialogProfileMap.Add(component.UniqueId, component);
			}
		}

		private void LoadAllComponentTypes()
		{
			ComponentsTypes.Clear();
			ComponentsTypes.AddRange(UnityObjectHelper.LoadAll<ComponentType>("Prefabs/WorldData/ComponentTypes"));
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: Found {ComponentsTypes.Count} component types", this, 1);
			}
		}

		private void LoadAllComponentBayTypes()
		{
			componentBayTypes.Clear();
			componentBayTypes.AddRange(UnityObjectHelper.LoadAll<ComponentBayType>("Prefabs/WorldData/ComponentBayTypes"));
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: Found {componentBayTypes.Count} component bay types", this, 1);
			}
		}

		private void LoadAllComponentClasses()
		{
			ComponentClasses.Clear();
			ComponentClasses.AddRange(LoadComponentClasses());
			foreach (ComponentClass componentClass in ComponentClasses)
			{
				if (componentClasses.ContainsKey(componentClass.UniqueId))
				{
					Debug.LogError("found duplicated componentclass id: " + componentClass, componentClass);
				}
				else
				{
					componentClasses.Add(componentClass.UniqueId, componentClass);
				}
			}
			Debug.Log($"Engine found {ComponentClasses.Count} Component classes", this);
		}

		private void LoadAllFactionPrefabs()
		{
			factionPrefabIdMap.Clear();
			foreach (Faction item in LoadFactions())
			{
				if (item.UniqueId > -1)
				{
					factionPrefabIdMap[item.UniqueId] = item;
				}
			}
		}

		private void LoadAllFactionTypes()
		{
			factionTypes.Clear();
			factionTypes.AddRange(LoadFactionTypes());
			factionShipCapTypes.Clear();
			factionShipCapTypes.AddRange(LoadFactionShipCapTypes());
		}

		private void LoadAllCargoClasses()
		{
			CargoClasses.Clear();
			CargoClasses.AddRange(LoadCargoClasses());
			foreach (CargoClass cargoClass in CargoClasses)
			{
				if (cargoClassMap.ContainsKey(cargoClass.UniqueId))
				{
					Debug.LogError("found duplicated cargoclass id: " + cargoClass, cargoClass);
				}
				else
				{
					cargoClassMap.Add(cargoClass.UniqueId, cargoClass);
				}
			}
		}

		private void LoadAllPilotRanksAndRankingSystems()
		{
			pilotRanksById.Clear();
			pilotRankingSystemsById.Clear();
			foreach (PilotRank item in LoadPilotRanks())
			{
				pilotRanksById.Add(item.UniqueId, item);
			}
			foreach (PilotRankingSystem item2 in LoadPilotRankingSystems())
			{
				pilotRankingSystemsById.Add(item2.UniqueId, item2);
			}
		}

		public static IEnumerable<Faction> LoadFactions()
		{
			return UnityObjectHelper.LoadAll<Faction>("Prefabs/WorldData/Factions");
		}

		public static IEnumerable<CargoFactoryProfile> LoadCargoFactoryProfiles()
		{
			return UnityObjectHelper.LoadAll<CargoFactoryProfile>("Prefabs/WorldData/CargoFactoryProfiles");
		}

		public static IEnumerable<FactionTypeInfo> LoadFactionTypes()
		{
			return UnityObjectHelper.LoadAll<FactionTypeInfo>("Prefabs/WorldData/FactionTypes");
		}

		public static IEnumerable<FactionShipCapType> LoadFactionShipCapTypes()
		{
			return UnityObjectHelper.LoadAll<FactionShipCapType>("Prefabs/WorldData/FactionShipCapTypes");
		}

		public static IEnumerable<CargoClass> LoadCargoClasses()
		{
			return UnityObjectHelper.LoadAll<CargoClass>("Prefabs/WorldData/CargoClasses");
		}

		public static IEnumerable<PilotRank> LoadPilotRanks()
		{
			return UnityObjectHelper.LoadAll<PilotRank>("Prefabs/WorldData/PilotRanks");
		}

		public static IEnumerable<PilotRankingSystem> LoadPilotRankingSystems()
		{
			return UnityObjectHelper.LoadAll<PilotRankingSystem>("Prefabs/WorldData/PilotRankingSystems");
		}

		private void LoadAllUnitClasses()
		{
			unitClassMap.Clear();
			UnitClass[] loadedUnitClasses = GameController.Instance.LoadedUnitClasses;
			foreach (UnitClass unitClass in loadedUnitClasses)
			{
				if (unitClassMap.ContainsKey(unitClass.UniqueID))
				{
					Debug.LogError("found duplicated unitclass id: " + unitClass, unitClass);
					continue;
				}
				unitClassMap.Add(unitClass.UniqueID, unitClass);
				unitClassList.Add(unitClass);
				if (unitClass.UnitType == UnitType.Ship && unitClass.ShipType == ShipType.Normal && unitClass.IsUsable && unitClass.SeedInSandbox && !unitClass.IsBarebonesShip)
				{
					factionAIBuildableShips.Add(unitClass);
				}
			}
			Debug.Log($"found {unitClassMap.Count} unit classes");
		}

		private void LoadAllMissionData()
		{
			missionDatas.Clear();
			foreach (MissionData item in UnityObjectHelper.LoadAll<MissionData>("Prefabs/WorldData/MissionData"))
			{
				if (missionDatas.ContainsKey(item.UniqueId))
				{
					Debug.LogError("found duplicated missionData id: " + item, item);
				}
				else
				{
					missionDatas.Add(item.UniqueId, item);
				}
			}
		}

		private void IntroCinematic_Finished(CinematicScreen sender)
		{
			OnIntroCinematicFinished();
			Resume();
		}

		private void OnIntroCinematicFinished()
		{
			switch (world.ObjectiveState)
			{
			case WorldBase.ScenarioState.Intro:
				world.ObjectiveState = WorldBase.ScenarioState.Playing;
				SetUIFromPlayerStatus();
				break;
			case WorldBase.ScenarioState.Playing:
				SetUIFromPlayerStatus();
				break;
			}
		}

		private void Update()
		{
			// Ahead of the IsPaused guard on purpose. Active-sector units used to be
			// ticked by UnitComponentHolder.Update(), which is a MonoBehaviour
			// callback and therefore ran while paused. Placing this here preserves
			// that exactly. (Inactive units already stopped while paused, because
			// they have always been driven from below the guard - so this is a
			// long-standing inconsistency in the engine, not a decision being made
			// here.)
			UpdateActiveSectorUnits();

			if (IsPaused)
			{
				return;
			}
			if (IsPlayerPilot)
			{
				ElapsedTimeAsPilot += Time.deltaTime;
			}
			else
			{
				ElapsedTimeAsPilot = 0f;
			}
			if (!(world != null) || !world.HasInitialised)
			{
				return;
			}
			if (localPlayer != null)
			{
				if (localPlayer.Person.CurrentUnit != null && localPlayer.Person.CurrentUnit.IsValidAndNotDestroyed && localPlayer.Person.CurrentUnit.IsOwnedByPlayer && !localPlayer.Person.CurrentUnit.IsStatic)
				{
					MovePlayerUnitBackWhenTooFarOutOfBounds();
				}
				if (localPlayer.Faction != null)
				{
					factionNeutralitySyncer.SyncFor(localPlayer.Faction);
				}
			}
			if (IsPlayerPilot && activeSector != LocalUnitSector)
			{
				MovePlayerUnitWhenInactive();
			}
			AutoDiscoverLocalUnit();
			traderHeatmapModule.Update();
			asteroidRespawner.Update();
			wormholeUpdater.Update();
			UnitCaptureModule.Update();
			DitchUnitModule.Update();
			DitchUnitCleanupModule.Update();
			cargoUpdater.Update();
			if (CanPlayerEnterWormholeAutomatically())
			{
				CheckPlayerUnitWormholeCollision(LocalUnit.transform.position, LocalUnit.transform.forward, 20f);
			}
			if (playerInCombat && (localPlayer == null || Time.time > playerCombatTimeout))
			{
				PlayerInCombat = false;
			}
			if (Hud != null && !Hud.gameObject.activeInHierarchy)
			{
				Hud.UpdateInactive();
			}
			UpdateWatchedSectorUnits();
			UpdateInactiveUnits();
			UpdateInactiveFleets();
			UpdateInactiveGroupSpawners();
			CleanupEmptyFleets();
			ScenarioElapsedTime += Time.deltaTime;
			for (int i = 0; i < destroyedUnitsToCleanup.Count; i++)
			{
				Unit unit = destroyedUnitsToCleanup[i];
				if (unit != null)
				{
					if (unit.IsDestroyedUnitReadyToCleanup())
					{
						unit.SafeDestroy();
						destroyedUnitsToCleanup.RemoveAt(i);
						i--;
					}
				}
				else
				{
					destroyedUnitsToCleanup.RemoveAt(i);
					i--;
				}
			}
		}

		private bool CheckPlayerUnitWormholeCollision(Vector3 worldPosition, Vector3 forward, float checkDistance)
		{
			if (Physics.Raycast(worldPosition, forward, out var hitInfo, checkDistance, GameController.Instance.WormholeMask, QueryTriggerInteraction.Collide))
			{
				Wormhole component = hitInfo.collider.GetComponent<Wormhole>();
				return TryEnterWormhole(component);
			}
			return false;
		}

		private void AutoDiscoverLocalUnit()
		{
			Unit localRootUnit = LocalRootUnit;
			if (localRootUnit != null && localRootUnit.UnitType == UnitType.Ship && !localRootUnit.IsOwnedByPlayer)
			{
				instance.LocalFaction.Intel.DiscoverUnit(localRootUnit);
			}
		}

		private void MovePlayerUnitWhenInactive()
		{
			if (Time.deltaTime == 0f)
			{
				return;
			}
			Vector3 lastKnownVelocity = Vector3.zero;
			if (LocalUnit.Components.EngineThrottle > 0f)
			{
				UnitEngineComponent engineComponent = LocalUnit.Components.EngineComponent;
				if (engineComponent != null && engineComponent.IsPoweredAndEnergySupplied)
				{
					float num = UnitEngineClass.CalculateMaxSpeed(engineComponent.CalculateRelativeForceAndReduceCapacitor(reduceChargeEnergy: true, Time.deltaTime), LocalUnit.UnitClass.Drag, LocalUnit.Mass);
					Vector3 vector = Vector3.forward * num;
					Vector3 position = localPlayer.transform.position;
					Vector3 vector2 = Quaternion.Euler(0f, LocalUnit.transform.eulerAngles.y, 0f) * vector;
					LocalUnit.transform.localPosition += vector2;
					if (LocalUnit.IsPullingUnit())
					{
						LocalUnit.Components.TractorTurret.TractorTarget.transform.localPosition += vector2;
					}
					lastKnownVelocity = vector2 / Time.deltaTime;
					LocalUnit.UpdateGasCloud();
					if (CanPlayerEnterWormholeAutomatically())
					{
						CheckPlayerUnitWormholeCollision(position, Vector3.Normalize(vector2), vector2.magnitude + 20f);
					}
				}
			}
			LocalUnit.Components.lastKnownVelocity = lastKnownVelocity;
		}

		public void RegisterDestroyedUnit(Unit unit)
		{
			destroyedUnitsToCleanup.Add(unit);
			NotifyUnitDestroyed(unit);
		}

		public void ValidateWorldState()
		{
			UnitOutOfBoundsValidator.ValidateAll();
			NpcPilotsFarFromFleetValidator.Validate();
			DockedShipConsistencyValidator.Validate();
			OverloadedCargoBaysValidator.Validate();
			lastTimeAutoValidatedState = Time.time;
		}

		public bool TryEnterWormhole(Wormhole wormhole)
		{
			if (!CanPlayerEnterWormholeAutomatically())
			{
				return false;
			}
			if (wormhole != null && wormhole.Unit.IsValidAndNotDestroyed && wormhole.Unit.Sector == instance.LocalUnitSector)
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Player entered wormhole {wormhole}", wormhole, 1);
				}
				if (activeSector != null)
				{
					if (TryStartSectorTransition(LocalUnit, wormhole, wormhole.ActualTargetSector))
					{
						if (LocalUnit.ActiveUnit != null)
						{
							LocalUnit.ActiveUnit.UpdateFxVisibility();
						}
						LocalUnit.Engine.NextAllowedPlayerGateEntry = Time.time + 3f;
						return true;
					}
					return false;
				}
				LocalUnit.EnterWormhole(wormhole, 2f);
				return true;
			}
			return false;
		}

		private bool CanPlayerEnterWormholeAutomatically()
		{
			if (sectorTransitionInfo == null && IsPlayerPilot && AllowPlayerEnterGate)
			{
				return Time.time > NextAllowedPlayerGateEntry;
			}
			return false;
		}

		private void MovePlayerUnitBackWhenTooFarOutOfBounds()
		{
			if (!UnitOutOfBoundsValidator.IsLocalPositionWithinBounds(PlayerUnit.SectorPosition))
			{
				Vector3 vector = Vector3.Normalize(PlayerUnit.SectorPosition);
				if (PlayerUnit.Components != null)
				{
					PlayerUnit.Components.CancelFiringOnAllTurrets();
				}
				PlayerUnit.transform.rotation = Quaternion.LookRotation(-vector, Vector3.up);
				PlayerUnit.transform.localPosition = vector * GameSettings.UniverseBoundsSettings.MaxUnitDistanceFromOriginLowerBound;
				PlayerUnit.RemoveForcesAndControlInputs();
				PlayerUnit.OnMoved();
				HudCamera.ResetOrientation();
				if (instance.activeSector != null)
				{
					UIController.Instance.QuickMsg.AddMessage("You cannot go that way", 0.1f);
					CreateIntroFader();
				}
			}
		}

		private void UpdateInactiveGroupSpawners()
		{
			if (FleetSpawners.Count <= 0)
			{
				return;
			}
			curInactiveFleetSpawnerIndex = Maths.WrapValue(curInactiveFleetSpawnerIndex + 1, 0, FleetSpawners.Count);
			if (curInactiveFleetSpawnerIndex < 0 || curInactiveFleetSpawnerIndex >= FleetSpawners.Count)
			{
				return;
			}
			FleetSpawner fleetSpawner = FleetSpawners[curInactiveFleetSpawnerIndex];
			if (fleetSpawner != null)
			{
				if (fleetSpawner.Sector != null && !fleetSpawner.Sector.IsActive)
				{
					fleetSpawner.Tick();
				}
			}
			else
			{
				FleetSpawners.RemoveAt(curInactiveFleetSpawnerIndex);
			}
		}

		private void UpdateInactiveUnits()
		{
			if (updatableUnits.Count <= 0)
			{
				return;
			}
			int num = GetInactiveUnitUpdateCount();
			if (curInactiveUnitIndex > updatableUnits.Count - 1)
			{
				curInactiveUnitIndex = updatableUnits.Count - 1;
			}
			_ = curInactiveUnitIndex;
			while (num > 0)
			{
				curInactiveUnitIndex++;
				if (curInactiveUnitIndex >= updatableUnits.Count)
				{
					curInactiveUnitIndex = 0;
				}
				Unit unit = updatableUnits[curInactiveUnitIndex];

				// Counted before anything can skip this unit, so that a
				// continue below cannot leave the loop spinning. This is
				// written here rather than at the end of the body for that
				// reason alone: the first version of this change put the skip
				// in and lost the decrement with it, and a while loop whose
				// condition nothing in its body ever changes does not compile
				// to an error - it compiles to a hang, on every launch, in
				// every build of the game.
				num--;

				// Watched sectors are skipped here and ticked in full by
				// UpdateWatchedSectorUnits instead. Two reasons, and the
				// second is the important one: spending the round-robin's
				// budget on a unit that is about to be ticked anyway makes
				// the genuinely unwatched sectors update less once per pass
				// for no reason, and ticking a watched unit from both paths
				// would simulate it twice a frame - which is the kind of bug
				// that makes ships in one sector move at double speed and
				// looks like a cheat.
				if (unit == null || unit.Sector == null
					|| unit.Sector.IsActive || unit.Sector.SimulatesEveryFrame)
				{
					continue;
				}

				unit.Components.Tick();
			}
		}

		/// <summary>
		/// Ticks every frame the sectors that are not on screen but are being
		/// watched by somebody in the session.
		/// </summary>
		/// <remarks>
		/// The engine's assumption is that a sector nobody is looking at has
		/// nothing visible happening in it, and updates it slowly on that
		/// basis. In a session that is wrong: a player two jumps away is
		/// watching theirs, and ships that only move once every three seconds
		/// look precisely like the sync being broken - worse, they look like
		/// it intermittently, which is harder to tell from a bug than a
		/// consistent one.
		///
		/// Walked from the list of watched sectors rather than from
		/// UpdatableUnits, because that list is every unit in the galaxy and
		/// this is at most a handful of sectors. On a session with two players
		/// two jumps apart this is two sectors' worth of ships per frame on
		/// top of the one already being simulated - which is the real cost of
		/// showing somebody a distant sector, paid on the host's frame time,
		/// and worth knowing about before a session grows to four players
		/// spread across four sectors.
		/// </remarks>
		/// <summary>
		/// Ticks every unit in the active sector, every frame.
		/// </summary>
		/// <remarks>
		/// This used to live in UnitComponentHolder.Update(), which meant Unity
		/// dispatched 6,308 managed->native->managed callbacks per frame to
		/// service roughly 48 ships. The work inside was always trivial - it
		/// checked IsInActiveSector and returned - so the cost was the dispatch
		/// itself: ~0.34us each, ~2.1ms a frame at 1024 sectors, the single
		/// largest item in BehaviourUpdate.
		///
		/// Nothing is disabled. UnitComponentHolder is still a component on every
		/// unit prefab, still holds the unit's state, and its GameObject is still
		/// alive and active; it simply no longer has an Update() for Unity to
		/// call. The units themselves keep ticking - these every frame, and the
		/// inactive ones via UpdateInactiveUnits / UpdateWatchedSectorUnits
		/// exactly as before.
		///
		/// The predicate is IsInActiveSector, the same cached flag the deleted
		/// Update() used, rather than the live Sector.IsActive. The two are the
		/// same condition by construction (SectorObject.RefreshIsInActiveSector:
		/// sector == EngineASX.Instance.ActiveSector), but the cached flag is
		/// what the previous behaviour actually turned on, so using it keeps
		/// this provably identical rather than merely equivalent.
		///
		/// Iterating the whole list rather than only the active sector's ships is
		/// deliberate: the scan is ~6,300 pointer checks with an early-out,
		/// which is roughly two orders of magnitude cheaper than the dispatch
		/// it replaces, and it cannot miss a unit of a type the active sector
		/// list does not cover.
		/// </remarks>
		private void UpdateActiveSectorUnits()
		{
			int num = updatableUnits.Count;
			for (int i = 0; i < num; i++)
			{
				Unit unit = updatableUnits[i];
				if (unit == null)
				{
					continue;
				}
				UnitComponentHolder components = unit.Components;
				if (components == null || !components.ShouldTickNow)
				{
					continue;
				}
				components.Tick();
			}
		}

		private void UpdateWatchedSectorUnits()
		{
			for (int s = 0; s < watchedSectors.Count; s++)
			{
				Sector sector = watchedSectors[s];

				if (sector == null || sector.IsActive)
				{
					continue;
				}

				List<Unit> inSector = sector.GetUnitsByType(UnitType.Ship);

				if (inSector == null)
				{
					continue;
				}

				for (int i = 0; i < inSector.Count; i++)
				{
					Unit unit = inSector[i];

					if (unit != null && !unit.IsDestroyed && unit.Components != null)
					{
						unit.Components.Tick();
					}
				}
			}
		}

		private readonly List<Sector> watchedSectors = new List<Sector>(4);

		/// <summary>
		/// Says that somebody in the session is watching this sector, or that
		/// they have stopped.
		/// </summary>
		/// <remarks>
		/// Reference-counted per player rather than counted per sector, so
		/// two players in one sector claim it twice and the first to leave
		/// does not switch the tick rate back down underneath the second. The
		/// sector counts its own claims; this list only has to be able to
		/// forget a sector entirely once nobody is in it.
		/// </remarks>
		public void SetSectorWatched(Sector sector, bool watched)
		{
			if (sector == null)
			{
				return;
			}

			sector.SetSimulatesEveryFrame(watched);

			if (watched)
			{
				if (!watchedSectors.Contains(sector))
				{
					watchedSectors.Add(sector);
				}

				return;
			}

			if (!sector.SimulatesEveryFrame)
			{
				watchedSectors.Remove(sector);
			}
		}

		private int GetInactiveUnitUpdateCount()
		{
			return GetInactiveUpdateCount(updatableUnits.Count, PerformanceSettings.PreferredTimeBetweenUnitUpdates);
		}

		private int GetInactiveGroupUpdateCount()
		{
			return GetInactiveUpdateCount(fleets.Count, PerformanceSettings.PreferredTimeBetweenGroupUpdates);
		}

		private static int GetInactiveUpdateCount(int count, float preferredTimeBetweenUpdates)
		{
			if (Time.deltaTime == 0f)
			{
				return 0;
			}
			float num = 1f / Time.deltaTime;
			int num2 = (int)((float)count / preferredTimeBetweenUpdates / num * Time.timeScale);
			if (num2 > count)
			{
				return count;
			}
			if (num2 < 1)
			{
				return 1;
			}
			return num2;
		}

		private void UpdateInactiveFleets()
		{
			if (fleets.Count <= 0)
			{
				return;
			}
			int inactiveGroupUpdateCount = GetInactiveGroupUpdateCount();
			for (int i = 0; i < inactiveGroupUpdateCount; i++)
			{
				curInactiveFleetIndex = Maths.WrapValue(curInactiveFleetIndex + 1, 0, fleets.Count);
				if (curInactiveFleetIndex >= 0 && curInactiveFleetIndex < fleets.Count)
				{
					Fleet fleet = fleets[curInactiveFleetIndex];
					if (fleet.Sector != null && !fleet.Sector.IsActive)
					{
						fleet.Tick();
					}
				}
			}
		}

		private void OnDestroy()
		{
			LoadedAndReady = false;
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Engine: OnDestroy", null, 1);
			}
		}

		private void ShowDockUI()
		{
			if (dockUI != null)
			{
				CreateIntroFader();
				dockUI.NavigateToInitialScreen();
			}
			else
			{
				Debug.LogError("Cannot show DockUI. NullReference", this);
			}
		}

		private void SetActiveSceneToPlayerUnit()
		{
			if (activeSector != PlayerUnit.Sector)
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log("Engine setting scene to player scene: " + PlayerUnit.Sector, this, 1);
				}
				ActiveSector = PlayerUnit.Sector;
			}
		}

		private void OnLocalPlayerChanged(GamePlayer oldPlayer)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: LocalPlayer changed from \"{oldPlayer}\" to \"{localPlayer}\"", this, 1);
			}
			if (localPlayer != null)
			{
				if (localPlayer.Person.CurrentUnit != null)
				{
					NotifyLocalPlayerChangedCurrentUnit(null);
				}
				bool isPilot = localPlayer.Person.IsPilot;
				NotifyLocalPlayerChangedPilotting(isPilot ? localPlayer.Person.CurrentUnit.Components : null, isPilot);
			}
		}

		public void HandleCargoTractoredByUnit(Cargo cargo, Faction cargoOwnerFaction, CargoClass cargoClass, Unit collectingUnit, int desiredQuantity, int transferredUnits)
		{
			Fleet fleet = collectingUnit.GetFleet();
			if (fleet != null)
			{
				fleet.InvalidateCargoUsageStats();
				if (fleet.ActiveOrder != null)
				{
					fleet.ActiveOrder.OnCargoCollectedByShip(collectingUnit, cargo);
				}
			}
			bool flag = false;
			if (transferredUnits != 0 && collectingUnit.Faction != null && cargoOwnerFaction != null && cargoOwnerFaction != collectingUnit.Faction)
			{
				int cargoValue = cargoClass.BasePrice * transferredUnits;
				if (cargoOwnerFaction.IsValidInGame)
				{
					cargoOwnerFaction.HandleCargoStolen(collectingUnit.Faction, collectingUnit, cargoValue);
				}
			}
			UnitComponentHolder components = collectingUnit.Components;
			if (components != null && components.PilotPerson != null && components.PilotPerson.IsLocalPlayer && Hud != null)
			{
				if (transferredUnits > 0)
				{
					ShowPlayerCargoTfrMsg(cargoClass, transferredUnits);
				}
				if (transferredUnits < desiredQuantity || components.CargoBayComponent.IsFull)
				{
					UIController.Instance.QuickMsg.AddMessage("Cargo Bay Full");
				}
			}
			if (transferredUnits != 0 && collectingUnit.IsActiveInEngine && Maths.GetDistanceIgnoreY(GameController.Instance.MainCamera.transform.position, collectingUnit.transform.position) < 3500f)
			{
				PlayCollectCargoAudio(collectingUnit.transform.position);
			}
		}

		public void PlayCollectCargoAudio(Vector3 worldPosition)
		{
			if (CargoCollectAudioSource != null)
			{
				PlayPooledAudioSource(CargoCollectAudioSource, worldPosition);
			}
		}

		private void RecordStatsOnCargoCollectedByNpcFleet(Fleet fleet, NpcPilot npcPilot, CargoClass cargoClass, int transferredUnits)
		{
			if (fleet.IsPlayerFaction() || !(fleet.Faction != null))
			{
				return;
			}
			if (fleet.Faction.FactionType != FactionType.Scavenger)
			{
				if (cargoClass.IsEquipment)
				{
					if (npcPilot.IsCargoClassCompatible(cargoClass))
					{
						DebugInfo.CargoValueCollectedByScavengerFaction_CompatibleEquipment += transferredUnits * cargoClass.BasePrice;
					}
					else
					{
						DebugInfo.CargoValueCollectedByScavengerFaction_IncompatibleEquipment += transferredUnits * cargoClass.BasePrice;
					}
				}
				else
				{
					DebugInfo.CargoValueCollectedByScavengerFaction_TradableCargo += transferredUnits * cargoClass.BasePrice;
				}
			}
			else if (cargoClass.IsEquipment)
			{
				if (npcPilot.IsCargoClassCompatible(cargoClass))
				{
					DebugInfo.CargoValueCollectedByNonScavengerFaction_CompatibleEquipment += transferredUnits * cargoClass.BasePrice;
				}
				else
				{
					DebugInfo.CargoValueCollectedByNonScavengerFaction_IncompatibleEquipment += transferredUnits * cargoClass.BasePrice;
				}
			}
			else
			{
				DebugInfo.CargoValueCollectedByNonScavengerFaction_TradableCargo += transferredUnits * cargoClass.BasePrice;
			}
		}

		private void playerUnit_SectorChanged(Unit sender, Sector oldScene)
		{
			if (localPlayer.Person.CurrentUnit != null && localPlayer.Person.CurrentUnit.Sector != null)
			{
				LocalFaction.Intel.DiscoverSector(localPlayer.Person.CurrentUnit.Sector);
			}
			if (Hud != null)
			{
				Hud.CurrentTarget = null;
			}
		}

		private bool ShouldHudBeVisible()
		{
			if (IsPlayerPilot)
			{
				return localPlayer != null;
			}
			return false;
		}

		private void SetPlayerCombatTimeout()
		{
			playerCombatTimeout = Time.time + GameSettings.AudioCombatMusicTimeout;
		}

		private void wormholeAnimator_AnimationFinished(WormholeAnimator sender)
		{
			if (Instance.SpaceConstructor != null)
			{
				Instance.SpaceConstructor.StaticStars.MeshRenderer.enabled = true;
				Instance.SpaceConstructor.NebulasTransform.gameObject.SetActive(value: true);
			}
			Unit wormholeUserUnit = sectorTransitionInfo.WormholeUserUnit;
			if (sectorTransitionInfo.Wormhole != null)
			{
				wormholeUserUnit.EnterWormhole(sectorTransitionInfo.Wormhole);
			}
			else
			{
				wormholeUserUnit.Sector = sectorTransitionInfo.TargetSector;
				wormholeUserUnit.RemoveForcesAndControlInputs();
			}
			if (sectorTransitionInfo.ManualTargetSectorPosition.HasValue)
			{
				wormholeUserUnit.transform.localPosition = sectorTransitionInfo.ManualTargetSectorPosition.Value;
			}
			if (sectorTransitionInfo.ManualTargetLocalRotation.HasValue)
			{
				wormholeUserUnit.transform.localRotation = sectorTransitionInfo.ManualTargetLocalRotation.Value;
			}
			ActiveSector = sectorTransitionInfo.TargetSector;
			if (activeSector != null)
			{
				activeSector.SeparateOverlappingShips();
			}
			SetUIFromPlayerStatus();
			if (HudCamera.isActiveAndEnabled)
			{
				SnapHudCameraToTarget();
			}
			if (wormholeUserUnit.ActiveUnit != null)
			{
				wormholeUserUnit.ActiveUnit.PropelFromWormhole();
			}
			else if (LogWrapper.LogMsgs)
			{
				Debug.LogWarning("Expecting player unit to have active unit on jump gate exit", this);
			}
			if (GameController.Instance.PlayerOptions.Video_WormholeAnimationEnabled)
			{
				if (SceneTransitionFaderPrefab != null)
				{
					UnityEngine.Object.Instantiate(SceneTransitionFaderPrefab.gameObject);
				}
			}
			else
			{
				CreateIntroFader();
			}
			if (!sectorTransitionInfo.WasSectorDiscovered)
			{
				UIController.Instance.QuickMsg.AddMessage(activeSector.Name + " sector discovered");
			}
			EnableUnitTrailRenderers(wormholeUserUnit);
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: Player has finished entering gate. Changing to scene: \"{sectorTransitionInfo.TargetSector.Name}\"", this, 1);
			}
			if (wormholeAnimator != null)
			{
				wormholeAnimator.AnimationFinished -= wormholeAnimator_AnimationFinished;
				UnityEngine.Object.Destroy(wormholeAnimator.gameObject);
			}
			sectorTransitionInfo = null;
			SetCameraParticleSystemsActive(value: true);
			Time.timeScale = timeScaleOnWormholeEntry;
			wormholeUserUnit.Destructable.IsInvulnerable = invulnerableOnWormholeEntry;
			if (GameController.Instance.AutoSaveSettings.AutoSaveOnWormholeExit)
			{
				AutoSaveIfPossible();
			}
			if (HudCamera != null)
			{
				HudCamera.ResetOrientation();
			}
			if (!GameController.Instance.PlayerOptions.Video_WormholeAnimationEnabled && WormholeAnimationPrefab.FinishAudioSourcePrefab != null)
			{
				UnityEngine.Object.Instantiate(WormholeAnimationPrefab.FinishAudioSourcePrefab.gameObject);
			}
		}

		private static void EnableUnitTrailRenderers(Unit jumpGateUserUnit)
		{
			if (jumpGateUserUnit.IsActiveInEngine)
			{
				TrailRenderer[] componentsInChildren = jumpGateUserUnit.GetComponentsInChildren<TrailRenderer>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					componentsInChildren[i].gameObject.SetActive(value: true);
				}
			}
		}

		private void OnPlayerUnitDocked(Unit unit)
		{
			HudCamera.RemoveTarget();
			UnloadPassengersFromPlayerUnit(unit, unit.GetRootUnit());
			Hud.AutoScanner.ClearTargetCache();
			Hud.ClearAutoTurnDesiredBearing();
			HudCamera.ResetOrientation();
			if (!HypersleepHelper.IsHypersleepActive())
			{
				CreateIntroFader();
				SetUIFromPlayerStatus();
			}
			TryReplenishRootUnitMissions();
			if (GameController.Instance.AutoSaveSettings.AutoSaveOnDocking)
			{
				AutoSaveIfPossible();
			}
			Hud.CurrentTarget = null;
		}

		public void AutoSaveIfPossible()
		{
			if (!World.Permissions.AllowSaving)
			{
				return;
			}
			if (world.ScenarioInfo != null)
			{
				if (UIController.Instance != null)
				{
					UIController.Instance.SavingGameIcon.Activate();
				}
				EngineIO.AutoSave(this);
				timeOfLastAutoSave = ScenarioElapsedTime;
			}
			else
			{
				Debug.LogWarning("Not saving. World scenario info required", this);
			}
		}

		private void UnloadPassengersFromPlayerUnit(Unit playerUnit, Unit dock)
		{
			int num = 0;
			int num2 = 0;
			PassengerGroup[] array = playerUnit.Components.PassengerGroups.ToArray();
			foreach (PassengerGroup passengerGroup in array)
			{
				if (passengerGroup.Destination == dock)
				{
					num2 += passengerGroup.PassengerCount;
					int? cachedRevenueOrCalculate = passengerGroup.GetCachedRevenueOrCalculate();
					if (cachedRevenueOrCalculate.HasValue)
					{
						num += cachedRevenueOrCalculate.Value;
					}
					passengerGroup.DeliverPassengers();
				}
			}
			if (num2 > 0)
			{
				UIController.Instance.QuickMsg.AddMessage($"{num2} passengers removed");
				RegisterTaxedPlayerTrade(dock, num, FactionTransactionType.PassengerFare, null, null, num2);
			}
		}

		public void OnUnitScanned(Unit scannedShip, Unit scanningShip)
		{
			if (scannedShip.Faction != null)
			{
				scannedShip.Faction.OnUnitScanned(scannedShip, scanningShip);
			}
		}

		public void ChangePlayerUnit(Unit unit)
		{
			UnitChanger.ChangeUnit(localPlayer.Person, unit);
			OnPlayerUnitChanged(unit);
		}

		public void OnPlayerUnitChanged(Unit unit)
		{
			NpcPilot npcPilot = unit.NpcPilot;
			if (npcPilot != null && npcPilot.Faction == LocalFaction && (!npcPilot.IsInCombat || !(unit.Destructable.HealthNormalized < 0.5f)))
			{
				DialogRequestArguments value = DialogRequestArguments.Init("TargetPilotTitle", instance.LocalPlayer.Person.Title);
				value.KeyValues.Add(("TargetUnit", unit.Components.ShipName));
				npcPilot.Person.RaiseDialogEvent(DialogEvents.PlayerEnteredOwnedPilottedShip, 0.5f, value);
			}
		}

		public int GetNonPlayerShipCount()
		{
			int countOfUnitType = GetCountOfUnitType(UnitType.Ship);
			int num = 0;
			if (LocalFaction != null)
			{
				num = LocalFaction.GetCountOfUnitType(UnitType.Ship);
			}
			return countOfUnitType - num;
		}

		public int GetShipCountByFactionType(FactionType factionType)
		{
			List<Unit> list = GetShipsByFactionType(factionType);
			int result = 0;
			if (list != null)
			{
				result = list.Count;
			}
			return result;
		}

		public List<Unit> GetShipsByFactionType(FactionType factionType)
		{
			List<Unit> value = null;
			if (shipsByFactionType.TryGetValue(factionType, out value))
			{
				return value;
			}
			return null;
		}

		internal void PlayChangeShipAudio()
		{
			if (EnterShipAudioClip != null)
			{
				AudioHelper.PlaySound(EnterShipAudioClip);
			}
		}

		public int GetCountOfUnitType(UnitType unitType)
		{
			List<Unit> value = null;
			if (unitsByType.TryGetValue(unitType, out value))
			{
				return value.Count;
			}
			return 0;
		}

		public int GetCountOfUnitClass(UnitClass unitClass)
		{
			List<Unit> value = null;
			if (unitsByClass.TryGetValue(unitClass.UniqueID, out value))
			{
				value.TrimNulls();
				return value.Count;
			}
			return 0;
		}

		public List<Unit> GetUnitsbyStationPurpose(StationPurpose stationPurpose)
		{
			return unitsByStationPurpose.GetItemOrDefault((int)stationPurpose);
		}

		public List<Unit> GetUnitsByClass(UnitClass unitClass)
		{
			return unitsByClass.GetItemOrDefault(unitClass.UniqueID);
		}

		public int GetCountOfUnitClassExcludeBandits(UnitClass unitClass)
		{
			List<Unit> value = null;
			if (unitsByClass.TryGetValue(unitClass.UniqueID, out value))
			{
				value.TrimNulls();
				int num = value.Count;
				foreach (Unit item in value)
				{
					if (item.Faction != null && item.Faction.FactionType != FactionType.Bandit)
					{
						num--;
					}
				}
			}
			return 0;
		}

		public void AbortInvalidMissions()
		{
			Mission[] array = Missions.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].AbortIfInvalid();
			}
		}

		public void DestroyInvalidJobsAtUnit(Unit unit)
		{
			List<MissionSpec> jobsAtUnit = GetJobsAtUnit(unit);
			if (jobsAtUnit == null)
			{
				return;
			}
			foreach (MissionSpec item in jobsAtUnit.ToList())
			{
				if (item != null && !item.IsValid())
				{
					item.SafeDestroy();
				}
			}
		}

		public void DestroyJobsAtUnit(Unit unit)
		{
			List<MissionSpec> jobsAtUnit = GetJobsAtUnit(unit);
			if (jobsAtUnit == null || jobsAtUnit.Count <= 0)
			{
				return;
			}
			foreach (MissionSpec item in jobsAtUnit.ToList())
			{
				if (item != null)
				{
					item.SafeDestroy();
				}
			}
		}

		public void ChangeScenarioElapsedTime(double scenarioElapsedTime)
		{
			ScenarioElapsedTime = scenarioElapsedTime;
			TimeOfLastAutoSave = scenarioElapsedTime;
		}

		public void RefreshSectorContentType()
		{
			foreach (Sector sector in sectors)
			{
				sector.RefreshSectorContentType();
			}
		}

		public void AllFactionsDiscoverOwnUnits()
		{
			foreach (Faction faction in Factions)
			{
				if (faction != null && faction.Intel != null)
				{
					faction.DiscoverOwnUnits();
				}
			}
		}

		internal void NotifyFactionRetiring(Faction faction)
		{
		}

		public void NotifySectorControlChanged(Sector sector, Faction oldController, Faction newController)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"Sector control of {sector} changed from" + " " + ((oldController != null) ? oldController.GetShortNameElseLong() : "NULL") + " to " + ((newController != null) ? newController.GetShortNameElseLong() : "NULL"));
			}
			if (oldController == null && newController != null)
			{
				controlledSectorCount++;
				if (controlledSectorCount > sectors.Count)
				{
					controlledSectorCount = sectors.Count;
				}
			}
			else if (oldController != null && newController == null)
			{
				controlledSectorCount--;
				if (controlledSectorCount < 0)
				{
					controlledSectorCount = 0;
				}
			}
			foreach (Sector sector2 in sectors)
			{
				sector2.RefreshJumpDistanceToNearestControlledSector();
			}
			if (!(newController != null))
			{
				return;
			}
			foreach (Faction item in sector.FactionsHeadquartered)
			{
				if (item != null && item.IsAIFactionType && newController != item)
				{
					item.CreateAttitudeIfNone(newController);
				}
			}
		}

		public void OnPlayerFleetInvalidOrder(ActiveFleetOrder order, string messageFromOrder)
		{
			if (order.FleetOrder.Notifications)
			{
				PlayerActiveMessage playerActiveMessage = UniverseEventsNotifierController.CreatePlayerFleetInvalidOrderMessage(order, messageFromOrder);
				if (playerActiveMessage != null)
				{
					LocalPlayer.AddMessage(playerActiveMessage);
				}
			}
		}

		public void OnPlayerFleetMaxDurationReached(ActiveFleetOrder order)
		{
		}

		public void OnPlayerFleetCompletedOrder(ActiveFleetOrder order)
		{
			if (order.FleetOrder.Notifications)
			{
				PlayerActiveMessage playerActiveMessage = UniverseEventsNotifierController.CreatePlayerFleetCompletedOrderMessage(order);
				if (playerActiveMessage != null)
				{
					LocalPlayer.AddMessage(playerActiveMessage);
				}
			}
		}

		public void OnPlayerScannedNewHostile(Unit scanner, Unit scanned, DiscoverUnitResult discoverUnitResult)
		{
			if (discoverUnitResult != DiscoverUnitResult.New && discoverUnitResult != DiscoverUnitResult.Updated)
			{
				return;
			}
			switch (scanner.UnitClass.UnitType)
			{
			case UnitType.Station:
				if (scanner.UnitClass.StationPurpose == StationPurpose.Satellite)
				{
					UniverseEventsNotifierController.OnPlayerSatelliteScannedNewHostile(scanner, scanned);
				}
				break;
			case UnitType.Ship:
			{
				Fleet fleet = scanner.GetFleet();
				if (fleet != null && fleet.Settings.NotifyWhenScannedHostile && !fleet.IsPlayerAutoPilotFleet())
				{
					UniverseEventsNotifierController.OnPlayerFleetScannedNewHostile(scanner, scanned);
				}
				break;
			}
			}
		}

		public void OnPlayerScannedNewAbandonedUnit(Unit scanner, Unit scanned, DiscoverUnitResult discoverUnitResult)
		{
			if (discoverUnitResult != DiscoverUnitResult.New)
			{
				return;
			}
			Fleet fleet = scanner.GetFleet();
			switch (scanned.UnitType)
			{
			case UnitType.Ship:
			case UnitType.Station:
				if (fleet != null && fleet.Settings.NotifyWhenAbandonedUnitFound && !fleet.IsPlayerAutoPilotFleet())
				{
					UniverseEventsNotifierController.OnPlayerFleetScannedAbandonedShipOrStation(scanner, scanned);
				}
				break;
			case UnitType.Cargo:
				if (fleet != null && fleet.Settings.NotifyWhenAbandonedCargoFound && !fleet.IsPlayerAutoPilotFleet())
				{
					UniverseEventsNotifierController.OnPlayerFleetScannedAbandonedCargo(scanner, scanned);
				}
				break;
			}
		}

		public Vector3 GetSectorMapCenter()
		{
			Vector3 zero = Vector3.zero;
			foreach (Sector sector in sectors)
			{
				zero += sector.MapPosition;
			}
			return zero / sectors.Count;
		}

		public void RefreshSectorDistancesFromUniverseCenter()
		{
			Vector3 sectorMapCenter = GetSectorMapCenter();
			foreach (Sector sector in sectors)
			{
				sector.DistanceFromUniverseCenter = (sector.MapPosition - sectorMapCenter).magnitude;
			}
			float num = sectors.Select((Sector e) => e.DistanceFromUniverseCenter).Max();
			foreach (Sector sector2 in sectors)
			{
				sector2.DistanceFromUniverseCenter01 = sector2.DistanceFromUniverseCenter / num;
			}
		}

		public DialogProfile GetDialogProfileForPerson(Person person)
		{
			dialogProfileCache.Clear();
			foreach (DialogProfile value in dialogProfileMap.Values)
			{
				if (value.UniqueId != GenericDialogProfile.UniqueId && CanApplyDialogProfileToPerson(value, person))
				{
					dialogProfileCache.Add(value);
				}
			}
			if (dialogProfileCache.Count > 0)
			{
				return GetDialogProfileById(dialogProfileCache.GetRandom().UniqueId);
			}
			return GetDialogProfileById(GenericDialogProfile.UniqueId);
		}

		private bool CanApplyDialogProfileToPerson(DialogProfile dialogProfile, Person person)
		{
			DialogProfileTargetSettings component = dialogProfile.GetComponent<DialogProfileTargetSettings>();
			if (component == null)
			{
				return true;
			}
			if (person.Aggression >= component.MinAggression && person.Aggression <= component.MaxAggression && person.Properness >= component.MinProperness)
			{
				return person.Properness <= component.MaxProperness;
			}
			return false;
		}

		public bool IsUnitExcludedFromNpcTraderTargets(Unit unit)
		{
			if (excludUnitFromNpcTraderTargets.TryGetValue(unit.UniqueId, out var value))
			{
				return Time.time < value;
			}
			return false;
		}

		public void ExcludeUnitFromNpcTraderTargets(Unit unit)
		{
			excludUnitFromNpcTraderTargets[unit.UniqueId] = Time.time + 240f;
		}

		public bool CanPlayerRenameUnit(Unit unit)
		{
			if (unit != null && unit.IsOwnedByPlayer)
			{
				return unit.IsStationOrShip();
			}
			return false;
		}

		public bool CanPlayerRenameUnitNpcPilot(Unit unit)
		{
			if (unit != null && unit.IsOwnedByPlayer && unit.GetPilot() != null)
			{
				return !unit.GetPilot().IsLocalPlayer;
			}
			return false;
		}

		public void RenameUnit(Unit unit, string newName)
		{
			if (unit.UnitType == UnitType.Ship)
			{
				unit.Components.ClearShipName();
				unit.Components.ShipName = newName;
			}
			else
			{
				unit.UnitName = newName;
			}
		}

		public PilotRank GetPilotRankById(int id)
		{
			if (pilotRanksById.TryGetValue(id, out var value))
			{
				return value;
			}
			return null;
		}

		public PilotRankingSystem GetPilotRankingSystemById(int id)
		{
			if (pilotRankingSystemsById.TryGetValue(id, out var value))
			{
				return value;
			}
			return null;
		}

		public void SetFactionSpawningEnabled(bool factionSpawnEnabled)
		{
			FactionSpawner.gameObject.SetActive(factionSpawnEnabled);
		}

		public bool CanPlayerSelfDestructUnit(Unit unit)
		{
			if (unit != null && unit.IsOwnedByPlayer && unit.Destructable != null)
			{
				return unit.Destructable.AllowDestruction;
			}
			return false;
		}

		public void CompileFleetFormations()
		{
			foreach (FleetFormationStyle formationStyle in GameController.Instance.GameSettings.FormationSettings.FormationStyles)
			{
				FleetFormation fleetFormation = new FleetFormation();
				fleetFormation.Offsets = new Vector3[formationStyle.transform.childCount][];
				fleetFormation.FormationStyle = formationStyle;
				for (int i = 0; i < formationStyle.transform.childCount; i++)
				{
					Transform child = formationStyle.transform.GetChild(i);
					List<Vector3> orderedFleetFormationPosition = GetOrderedFleetFormationPosition(child);
					fleetFormation.Offsets[i] = new Vector3[child.transform.childCount];
					for (int j = 0; j < child.childCount; j++)
					{
						fleetFormation.Offsets[i][j] = orderedFleetFormationPosition[j];
					}
				}
				if (fleetFormationsById.ContainsKey(fleetFormation.FormationStyle.UniqueId))
				{
					Debug.LogError($"Fleet formation with ID {fleetFormation.FormationStyle.UniqueId} already exists");
				}
				fleetFormationsById[fleetFormation.FormationStyle.UniqueId] = fleetFormation;
			}
			defaultFleetFormation = GetFleetFormationById(GameController.Instance.GameSettings.FormationSettings.DefaultFormationStyle.UniqueId);
		}

		private List<Vector3> GetOrderedFleetFormationPosition(Transform parent)
		{
			List<FleetFormationDesignPosition> list = new List<FleetFormationDesignPosition>();
			for (int i = 0; i < parent.transform.childCount; i++)
			{
				FleetFormationDesignPosition component = parent.GetChild(i).GetComponent<FleetFormationDesignPosition>();
				list.Add(component);
			}
			return (from e in list
				orderby e.ShipSizePreference
				select e.transform.localPosition).ToList();
		}

		public FleetFormation GetDefaultFleetFormation()
		{
			return defaultFleetFormation;
		}

		public FleetFormation GetFleetFormationById(int id)
		{
			if (fleetFormationsById.TryGetValue(id, out var value))
			{
				return value;
			}
			return null;
		}

		public void NotifyPlayerTargettedByHostileNpc(Unit source)
		{
			if (Hud != null)
			{
				Hud.NotifyPlayerTargettedByAI(source);
			}
		}

		public void EnumerateUnits(Action<Unit> callback)
		{
			foreach (Sector sector in sectors)
			{
				UnitType[] array = unitTypes;
				foreach (UnitType unitType in array)
				{
					List<Unit> list = sector.GetUnitsByType(unitType);
					if (list == null)
					{
						continue;
					}
					foreach (Unit item in list)
					{
						if (item != null)
						{
							callback(item);
						}
					}
				}
			}
		}

		public void EnumerateUnitsWithPredicate(Action<Unit> callback, Func<Unit, bool> predicate)
		{
			foreach (Sector sector in sectors)
			{
				UnitType[] array = unitTypes;
				foreach (UnitType unitType in array)
				{
					List<Unit> list = sector.GetUnitsByType(unitType);
					if (list == null)
					{
						continue;
					}
					foreach (Unit item in list)
					{
						if (item != null && predicate(item))
						{
							callback(item);
						}
					}
				}
			}
		}

		public void EnumerateUnitsOfType(UnitType unitType, Action<Unit> callback)
		{
			foreach (Sector sector in sectors)
			{
				List<Unit> list = sector.GetUnitsByType(unitType);
				if (list == null)
				{
					continue;
				}
				foreach (Unit item in list)
				{
					if (item != null)
					{
						callback(item);
					}
				}
			}
		}

		public int GetGameDay()
		{
			return DateTimeUtils.GetGameWorldDay(ScenarioElapsedTime);
		}

		/// <summary>
		/// Whether this id cannot be handed out: it is already taken, or it
		/// belongs to somebody.
		/// </summary>
		/// <remarks>
		/// Every id allocator here walks a counter upward, which is why all ten
		/// of them go through this rather than testing the map alone.
		///
		/// Multiplayer gives player-made factions and ships numbers in
		/// reserved bands, so that "was this made by a player" is answerable
		/// from the number by itself - in a save, in a log, in a debugger -
		/// with no lookup table for two copies of a world to disagree about.
		///
		/// A counter cannot be trusted to stay far away from a range that
		/// only means something to code outside this file. It did not, in
		/// fact: moving a faction into the band used to drag the counter
		/// past it, and from then on every faction the world generator
		/// created was numbered inside the player band. IsPlayerMade
		/// answered true for ordinary AI, and the corrupted counter was
		/// written into saves and read back, so it survived a restart.
		///
		/// So the engine asks. With no policy registered this is one null
		/// check, and a singleplayer game reaches nothing else.
		/// </remarks>
		private static bool IsTakenOrReserved(int id, System.Func<int, bool> inMap)
		{
			return inMap(id) || ReservedIds.IsReserved(id);
		}

		public int GetUniquePassengerGroupId()
		{
			int i;
			for (i = PassengerGroupIdCounter; IsTakenOrReserved(i, ContainsPassengerGroupById); i++)
			{
			}
			PassengerGroupIdCounter = i + 1;
			return i;
		}

		public int GetUniqueUnitId()
		{
			int i;
			for (i = UnitIdCounter; IsTakenOrReserved(i, ContainsUnitById); i++)
			{
			}
			UnitIdCounter = i + 1;
			return i;
		}

		public int GetUniqueFactionId()
		{
			int i;
			for (i = FactionIdCounter; IsTakenOrReserved(i, ContainsFactionById); i++)
			{
			}
			FactionIdCounter = i + 1;
			return i;
		}

		public int GetUniqueFleetOrderId()
		{
			int i;
			for (i = FleetOrderIdCounter; IsTakenOrReserved(i, ContainsFleetOrderById); i++)
			{
			}
			FleetOrderIdCounter = i + 1;
			return i;
		}

		public int GetUniqueFleetId()
		{
			int i;
			for (i = FleetIdCounter; IsTakenOrReserved(i, ContainsFleetById); i++)
			{
			}
			FleetIdCounter = i + 1;
			return i;
		}

		public int GetUniquePersonId()
		{
			int i;
			for (i = PersonIdCounter; IsTakenOrReserved(i, ContainsPersonById); i++)
			{
			}
			PersonIdCounter = i + 1;
			return i;
		}

		public int GetUniqueSectorId()
		{
			int i;
			for (i = SectorIdCounter; IsTakenOrReserved(i, ContainsSectorById); i++)
			{
			}
			SectorIdCounter = i + 1;
			return i;
		}

		public int GetUniqueJobId()
		{
			int i;
			for (i = JobIdCounter; ContainsJobByid(i); i++)
			{
			}
			JobIdCounter = i + 1;
			return i;
		}

		public int GetUniqueMissionId()
		{
			int i;
			for (i = MissionIdCounter; IsTakenOrReserved(i, ContainsMissionById); i++)
			{
			}
			MissionIdCounter = i + 1;
			return i;
		}

		public int GetUniqueMissionObjectiveId()
		{
			int i;
			for (i = MissionObjectiveIdCounter; IsTakenOrReserved(i, ContainsMissionObjectiveById); i++)
			{
			}
			MissionObjectiveIdCounter = i + 1;
			return i;
		}

		public int GetUniquePatrolPathId()
		{
			int i;
			for (i = PatrolPathIdCounter; IsTakenOrReserved(i, ContainsPatrolPathById); i++)
			{
			}
			PatrolPathIdCounter = i + 1;
			return i;
		}

		public int GetSectorUnitHardLimit(UnitType unitType)
		{
			return unitType switch
			{
				UnitType.Ship => 600, 
				UnitType.Station => 400, 
				UnitType.Cargo => 1000, 
				UnitType.Asteroid => 500, 
				UnitType.Wormhole => 10, 
				_ => 1000, 
			};
		}

		public bool CanSpawnUnitsInSector(Sector sector, UnitType unitType, int count)
		{
			return sector.GetCountOfUnitType(unitType) + count < GetSectorUnitHardLimit(unitType);
		}

		public int CountUnitsInArea(Sector sector, Vector3 sectorPosition, float radius, LayerMask mask, Func<Unit, bool> predicate)
		{
			int num = 0;
			int num2 = Physics.OverlapSphereNonAlloc(sector.ToWorldPosition(sectorPosition), radius, ColliderCache, mask, QueryTriggerInteraction.Collide);
			for (int i = 0; i < num2; i++)
			{
				Unit component = ColliderCache[i].GetComponent<Unit>();
				if (component != null && component.Sector == sector && predicate(component))
				{
					num++;
				}
			}
			return num;
		}

		public bool IsUnitDiscoveredByLocalFaction(Unit unit)
		{
			if (unit == null || !unit.IsValidAndNotDestroyed)
			{
				return false;
			}
			if (instance.LocalFaction == null)
			{
				return false;
			}
			float value = (unit.IsInActiveSector ? GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTimeActiveSector : GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTime);
			return instance.LocalFaction.Intel.IsUnitDiscovered(unit, value);
		}

		public void RecalculateAllNetWorths()
		{
			foreach (Faction faction in Factions)
			{
				faction.UpdateNetWorth();
			}
		}
	}
}
