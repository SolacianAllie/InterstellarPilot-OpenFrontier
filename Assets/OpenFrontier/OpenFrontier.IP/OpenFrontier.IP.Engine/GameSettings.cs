using System;
using System.Collections.Generic;
using OpenFrontier.IP.Assets.Scripts.Engine.GasClouds;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.CargoLeakage;
using OpenFrontier.IP.Engine.Comms.FactionDynamicComms;
using OpenFrontier.IP.Engine.Heatmaps;
using OpenFrontier.IP.Engine.Intel;
using OpenFrontier.IP.Engine.MissionSpecs;
using OpenFrontier.IP.Engine.Missions;
using OpenFrontier.IP.Engine.Npcs;
using OpenFrontier.IP.Engine.PilotRankings;
using OpenFrontier.IP.Engine.Sandbox;
using OpenFrontier.IP.Engine.Settings;
using OpenFrontier.IP.Engine.TraderHeatmap;
using OpenFrontier.IP.Engine.UniverseWorld;
using OpenFrontier.IP.Engine.WorldSeeding;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers;
using OpenFrontier.IP.Mercenaries;
using OpenFrontier.IP.SpaceUnity;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine
{
	public class GameSettings : MonoBehaviour
	{
		public TraderHeatmapSettings TraderHeatmapSettings;

		public VirtueChangeSettings VirtueChangeSettings;

		public AsteroidRespawnSettings AsteroidRespawnSettings;

		public DitchUnitSettings DitchUnitSettings;

		public EngineEconomySettings EconomySettings;

		public BackgroundObjectsSettings BackgroundObjectsSettings;

		public SpaceConstructorSettings SpaceConstructorSettings;

		public LootSettings LootSettings;

		public RearmSettings RearmSettings;

		public HypersleepSettings HypersleepSettings;

		public NpcMiningSettings NpcMiningSettings;

		public AutoTurretTargetSearchSettings AutoTurretTargetSearchSettings;

		public NpcPiilotArrivalSettings NpcPiilotArrivalSettings;

		public AutoRepairSettings AutoRepairSettings;

		public GeneralFleetSettings GeneralFleetSettings;

		public PirateRaidSettings PirateRaidSettings;

		public TeamColourSettings TeamColourSettings;

		public GameplaySettings GameplaySettings;

		public VideoSettings VideoSettings;

		public GasCloudSettings GasCloudSettings;

		public PassengerGroupSettings PassengerGroupSettings;

		public UIButtonColorSettings UIButtonColors;

		public ComponentDamageEffectSettings ComponentDamageEffectSettings;

		public ComponentDamageSettings ComponentDamageSettings;

		public DamageFlashSettings DamageFlashSettings;

		public SectorSecuritySettings SectorSecuritySettings;

		public ColorSettings ColorSettings;

		public NpcTargetSearchSettings NpcTargetSearchSettings;

		public HeatmapSettings HeatmapSettings;

		public HudSettings HudSettings;

		public bool ShowQuickTractorButtonForHostileCargo = true;

		public SkirmishSettings SkirmishSettings;

		public CargoLeakageSettings CargoLeakageSettings;

		public IntelSettings IntelSettings;

		public ActiveSectorPathfindingSettings ActiveSectorPathfindingSettings;

		public FormationSettings FormationSettings;

		public DistanceSettings DistanceSettings;

		[FormerlySerializedAs("UnitSpectatorSettings")]
		public HudCameraSettings HudCameraSettings;

		public TractorBeamSettings TractorBeamSettings;

		public int MinorFactionNetWorthThreshold = 2500000;

		public UnstableWormholeSettings UnstableWormholeSettings;

		public SectorIntelSettings SectorIntelSettings;

		public SandboxPlayerSettings SandboxPlayerSettings;

		public AutoSaveSettings AutoSaveSettings;

		public PassengerFareSettings PassengerFareSettings;

		public ModdedUnitSeederSettings ModdedUnitSettings;

		public MercenarySettings MercenarySettings;

		public DialogEventSettings DialogEventSettings;

		public float MoonTimePerRevolution = 120f;

		public SectorMapSettings SectorMapSettings;

		public SaveGameSettings SaveGameSettings;

		public float TurretBuildDistanceRadiusMultiplier = 1.5f;

		public DynamicCommsSettings DynamicCommsSettings;

		public float AIMineAvoidanceCheckDistance = 350f;

		public MissionSettings MissionSettings;

		public MissionSpecSettings MissionSpecSettings;

		public float EquipmentDealerNodeWaitTime = 360f;

		public AsteroidFieldPlacementSettings DefaultAsteroidPlacementSettings;

		public UniverseBoundsSettings UniverseBoundsSettings;

		public float MinDistanceBetweenStations = 250f;

		public float MinDistanceBetweenMinorStations = 50f;

		public DebugSettings DebugSettings;

		public PerformanceSettings PerformanceSettings;

		public GameAudioSettings AudioSettings;

		public SandboxSettings SandboxSettings;

		public WorldSeedSettings DefaultWorldSeedSettings;

		public int AIShipCapPerScene = 30;

		public int NumAutosaves = 3;

		public bool SaveOnlyWhenDocked;

		public float PlayerAmbientSoundVolumeMultiplier = 0.3f;

		public int PlayerMessagesMaxUnimportant = 30;

		public int FactionMinShortNameChars = 2;

		public int FactionMaxShortNameChars = 10;

		public int FactionMaxLongNameChars = 30;

		public int FactionMinNameChars = 3;

		public float MiningProbabilityDamageRef = 10f;

		public Color TargetSpeakingActiveColor = Color.yellow;

		public Color TargetSpeakingInactiveColor = Color.white;

		public int BountyMinCreditsPlaced = 5000;

		[FormerlySerializedAs("BountyProbabilityForEachAIGroup")]
		public float BountyProbabilityForEachPilot = 0.25f;

		public float BountyGroupMinRewardMultiplier = 0.15f;

		public float BountyGroupMaxRewardMultiplier = 0.5f;

		public float CostToRepairShieldPoint = 1f;

		public float OwnedStationShipRepairMultiplier = 0.75f;

		public bool RecordFactionTransactionForAI = true;

		public int NumberOfFactionTransactionsHistory = 30;

		public float UIUnderConstructionDrawDistance = 200f;

		public float ConstructionTimePerHp = 0.1f;

		public float DismantlingTimePerHp = 0.1f;

		public float ConstructionInitialHealth = 0.1f;

		public bool CanEntershipFromAnyDistance;

		public bool CanTransferCargoFromAnyDistance;

		public float ClaimShipDistance = 70f;

		public float EnterShipDistance = 70f;

		public float TransferCargoDistance = 70f;

		public float MaxTransferCargoDistance = 1200f;

		public float CargoDeployMinDistanceFromWormhole = 750f;

		public float CargoDeployMinDistanceFromDockableStations = 200f;

		public NpcPilotBehaviourSettings AiUnitControllerSettings;

		public NpcPilotBehaviourMineDropSettings NpcPilotBehaviourMineDropSettings;

		public float AIPreferredThrottleWhenAvoiding = 0.6f;

		public bool TurretInaccuracyEnabled = true;

		public FactionSettings FactionSettings;

		public DateTime DefaultScenarioStartDate = new DateTime(2235, 11, 16);

		public float ComponentRepairCostMultiplier = 0.95f;

		public float GameTimeToRealTimeConversion = 48f;

		public WorldPlayerPermissions DefaultWorldPermissions;

		public WealthLevels WealthLevels;

		public WealthLevels PowerLevels;

		public Sector LoadGameScenePrefab;

		public float AbortMissionPenaltyMultiplier = 4f;

		public float ActiveUnitMaxRollAngle = 60f;

		public float ActiveUnitMaxRollTurnValue = 100f;

		public float ActiveUnitRollLerp = 1f;

		public float ActiveUnitTurnArrivalAggression = 0.4f;

		public Color AffordableColor = Color.green;

		public bool AIActivePilotAvoidProximitityToLargerCombatTarget = true;

		public float AISelectBestCombatTargetMinFrequency = 2f;

		public float AISelectBestCombatTargetMaxFrequency = 6f;

		public float AIActivePilotTurnChangeRate = 0.5f;

		public bool AICloakingUpdateEnabled = true;

		public bool AICollisionDamageEnabled;

		public float AIDockMinCoolDownTime = 5f;

		public float AIDockPlayerOwnedMinCoolDownTime = 5f;

		public float AIDockPlayerOwnedPreferredCoolDownTime = 30f;

		public float AIDockPreferredCoolDownTime = 120f;

		public bool AIFiringUpdateEnabled = true;

		public float AIGroupCombatCooldownTime = 20f;

		public float AIGroupSpawnMinPlayerDistance = 800f;

		public float AIGroupStaleTargetRemoveTime = 10f;

		public float AILostCloakedTargetCoolDownTime = 240f;

		public float AILostCombatCombatCoolDownTime = 5f;

		public float AIMaxMineAvoidDist = 200f;

		public float AIMineSafeDistanceMinMultiplier = 1.1f;

		public float AIMineSafeDistanceMultiplier = 1.3f;

		public float AIMinTradeOpinion = 0.3f;

		public float AIPilotRegroupDist = 300f;

		public float AIPointDefenseDeploymentProbability = 0.45f;

		public float AIPointDefenseMaxDeploymentRange = 350f;

		public float AIPointDefenseMinMissileLifetime = 1.5f;

		public float AIPointDefenseUpdateInterval = 0.85f;

		public float AITimeBetweenPathfinding = 8f;

		public float AITurretExistingTargetScore = 5f;

		public float AITurretPreferredTargetScore = 5f;

		public bool AllowAiGroupSpawners = true;

		public bool AllowPlayerDock;

		public float AsteroidSpawnMaxForce = 90f;

		public float AsteroidSpawnMinForce = 45f;

		public List<UnitAttributeLevels> AttributeLevels = new List<UnitAttributeLevels>();

		public int AttributePointsPerLevel = 10;

		public float AudioCombatMusicTimeout = 25f;

		public bool CanEjectCargoWhileDocked = true;

		public float CloakedPlayerUnitAlphaRange = 0.8f;

		public float CloakedUnitAlphaRange = 0.9f;

		public bool CollisionDamageEnabled;

		public ShieldDamageType CollisionShieldDamageType = ShieldDamageType.Envelope;

		public float TurretInaccuracyDmgWeight = 0.8f;

		public float TurretInaccuracyNormalWeight = 0.2f;

		public float TurretMaxInaccuracyDegrees = 10f;

		public int DamageParticlesMaxCount = 4;

		public float DamageParticlesMaxHealth = 0.2f;

		public float DamageParticlesMinHealth = 0.1f;

		public float DamageParticlesThreshold = 0.75f;

		public float DamageShakeDuration = 0.75f;

		public float DamageToShakeMagnitudeMultiplier = 0.02f;

		public float DebrisLifetime = 240f;

		public float DestroyedUnitCargoDropPower = 2f;

		public float DestroyedUnitMaxDroppedCargo = 0.75f;

		public float DestroyedUnitMinDroppedCargo = 0.05f;

		public bool DestroyedUnitsCreateDebris;

		public float DockableDistance = 75f;

		public float EjectCargoForce = 5f;

		public float EnginePowerToEnergyUsage = 4f;

		public bool EquipmentDealerInfiniteCargo = true;

		public float ExpireCargoInActiveSceneDistanceFromCamera = 1750f;

		public float ExplosionDamageToForceRatio = 0.1f;

		public float ExplosionTorqueMultiplier = 10f;

		public float HyperdriveAcceleration = 20f;

		public float HyperdriveMovespeed = 100f;

		public float InactiveFireMultiplier = 0.1f;

		public float InactiveGroupMoveSpdMultiplier = 0.4f;

		public float JumpGateExitDistance = 180f;

		public CargoClass[] LootableCargoClasses;

		public float LootableCargoMaxMultiplier = 2f;

		public int[] LootableCargoMaxValues;

		public int[] LootableCargoMinValues;

		public float[] LootableCargoShipValueDivisors;

		public float LootContainerMaxAngularVelocity = 30f;

		public float LootContainerMinAngularVelocity = 30f;

		public float LootSpawnMaxForce = 30f;

		public float LootSpawnMinForce = 10f;

		public int MaxAttributePoints = 100;

		public float MaxDamageParticlesTime = 8f;

		public int MaxDynamicMissions = 3;

		public float MaxShieldRestoreDelay = 120f;

		public float MaxShieldRestoreDelayValue = 1000f;

		public float MinDamageParticlesTime = 3f;

		public float MinShieldRestoreDelay = 10f;

		public float MissionsDeliverShipPercentage = 0.05f;

		public int MissionsDeliverShipBaseIncome = 750;

		public float PassengersGroupExpireTime = 500f;

		public int PassengersPreferredGroupCount = 10;

		public float PlayerAutoTargetArmedScore = 4f;

		public float PlayerAutoTargetBearingScore = 3f;

		public float PlayerAutoTargetDistanceScore = 5f;

		public float PlayerAutoTargetHostileScore = 10f;

		public float PlayerAutoTargetLockedMissileScore = 10f;

		public float PlayerAutoTargetMaxDistance = 1000f;

		public float PlayerDamageMultiplier = 0.75f;

		public bool PlayerXpEnabled = true;

		public bool QuickMsgLinksEnabled;

		public bool ReduceLaserDamageWithRange;

		public float RegroupSpdChangeThreshold = 50f;

		public float RegroupSpdChangeThresholdUpper = 200f;

		public bool RetainLootOwnership = true;

		public float ShieldRelativeRegenRate = 0.01f;

		public float ShieldRestoreNormalizedValue = 0.05f;

		public GameObject ShieldRingPrefab;

		public float ShowCapacitorLowerThreshold = 0.9f;

		public float ShowCapacitorUpperThreshold = 0.95f;

		public bool ShowMissionMsgs;

		public bool ShowObjectiveMsgs;

		public float SpawnAsteroidMaxAngularVelocity = 60f;

		public float SpawnAsteroidMaxRandomAngle = 20f;

		public float SpawnAsteroidMinAngularVelocity = 30f;

		public float SpeechModelMinTimeBeforeRepeat = 30f;

		public Color TextCargoColor = Color.green;

		public Color TextSceneColor = Color.green;

		public Color TextHeadingColor = Color.green;

		public Color TextUnitClassColor = Color.green;

		public Color TextUnitNameColor = Color.green;

		[NonSerialized]
		public UnitType TractorableUnitTypes = UnitType.Ship | UnitType.Cargo;

		public float TractorBeamRetractSpeed = 5f;

		public float TractorBreakDistance = 125f;

		public float TractorMaxForceApplyDist = 100f;

		public float TractorMinForceApplyDist = 50f;

		public Color UnaffordableColor = Color.red;

		public float UndockDistance = 50f;

		public float UnitCollisionDamageFactor = 1f;

		public float UnitCollisionDamageForceThreshold = 5f;

		public float UnitCollisionEngineCooldownThreshold;

		public float UnitCollisionImmobilizationFactor = 0.25f;

		public float UnitCollisionMaxImmobileTime = 2f;

		public float UnitDefaultAngularDrag = 2f;

		public float UnitDesctructionMinTorqueMultiplier = 0.5f;

		public float UnitDesctructionTorqueMultiplier = 10f;

		public bool UnitRollEnabled = true;

		public int PilotNameMinChars = 3;

		public int PilotNameMaxChars = 12;

		public int PilotTitleMinChars = 3;

		public int PilotTitleMaxChars = 20;

		public float NewStationPlayerNotificationProbability = 0.75f;

		public float NewStationPlayerNotificationMaxJumpDistance = 6f;

		public float NewStationPlayerNotificationMinTimeBeforeShow = 600f;

		public bool AllowFleetToSetHomeBaseToUnownedUnalliedCapitalShip;

		public int MaxPlayerFleetStackedOrders = 8;

		public float MaxExploreRangeGateDistanceMultiplier = 1.2f;

		public float ProjectileVelocityInheritanceFactor = 0.8f;

		public float ExplosionDamageFalloutPower = 1.4f;

		public float AIMinSpeedToDeployMine = 30f;

		public bool BanditsAlwaysAtWarWithBandits;

		public string[] RandomTitles;

		public PilotRankingSystem DefaultRankingSystem;

		public float ScanRange = 1500f;

		public float UIAutoMultiSelectTime = 1.2f;
	}
}
