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
		[Tooltip("Heatmap drawing for the universe map: which cargo, missions and fleets light up a cell, and how bright.")]
		public TraderHeatmapSettings TraderHeatmapSettings;

		[Tooltip("How far a single good or evil act moves a faction's virtue, which in turn decides who it will trade with.")]
		public VirtueChangeSettings VirtueChangeSettings;

		[Tooltip("How long a depleted asteroid field waits before it comes back, and the cooldown between respawn attempts in one sector.")]
		public AsteroidRespawnSettings AsteroidRespawnSettings;

		[Tooltip("How long a ditched ship survives as wreckage before it is cleaned up.")]
		public DitchUnitSettings DitchUnitSettings;

		[Tooltip("Prices, taxes and how fuzzy the AI traders are about both. See EngineEconomySettings.")]
		public EngineEconomySettings EconomySettings;

		[Tooltip("The fake planets, moons and distant asteroid fields drawn behind the playfield to make a sector look populated.")]
		public BackgroundObjectsSettings BackgroundObjectsSettings;

		[Tooltip("How the player-built station pieces behave: snapping, collision and what counts as a valid placement.")]
		public SpaceConstructorSettings SpaceConstructorSettings;

		[Tooltip("What salvage drops from a destroyed ship, and how much value it is worth.")]
		public LootSettings LootSettings;

		[Tooltip("Who restocks weapons and munitions, and how long it takes.")]
		public RearmSettings RearmSettings;

		[Tooltip("Rules for hypersleep: how the jump is presented and what happens to the ship and crew while time passes.")]
		public HypersleepSettings HypersleepSettings;

		[Tooltip("How NPC ships pick a field to mine, and how much they pull out of it.")]
		public NpcMiningSettings NpcMiningSettings;

		[Tooltip("How auto-turrets score candidates and how far they will look for one.")]
		public AutoTurretTargetSearchSettings AutoTurretTargetSearchSettings;

		[Tooltip("When arriving NPC ships are allowed into the sector. The field name has a typo in it; it is left as-is because Unity keys saved data off the field name.")]
		public NpcPiilotArrivalSettings NpcPiilotArrivalSettings;

		[Tooltip("How much hull and component health is restored by automatic repairs, when they are allowed to fire.")]
		public AutoRepairSettings AutoRepairSettings;

		[Tooltip("Fleet-wide behaviour that is not faction-specific: formation, spacing and regrouping.")]
		public GeneralFleetSettings GeneralFleetSettings;

		[Tooltip("How pirate raids are launched, and how a raid behaves once it is underway.")]
		public PirateRaidSettings PirateRaidSettings;

		[Tooltip("The colours used to tell teams apart on the HUD and map.")]
		public TeamColourSettings TeamColourSettings;

		[Tooltip("The core gameplay rules: combat, cargo decay, cloaking, damage and faction AI timing.")]
		public GameplaySettings GameplaySettings;

		[Tooltip("Resolution, quality level and fullscreen mode.")]
		public VideoSettings VideoSettings;

		[Tooltip("Gas cloud density, colour and hazard values.")]
		public GasCloudSettings GasCloudSettings;

		[Tooltip("How passenger groups fill up, get unhappy and leave.")]
		public PassengerGroupSettings PassengerGroupSettings;

		[Tooltip("The tint each button takes for its different states, shared across the whole UI.")]
		public UIButtonColorSettings UIButtonColors;

		[Tooltip("How much a damaged component degrades its neighbours, visually and in output.")]
		public ComponentDamageEffectSettings ComponentDamageEffectSettings;

		[Tooltip("How damaged components lose output and how often they fail outright.")]
		public ComponentDamageSettings ComponentDamageSettings;

		[Tooltip("The screen flash and hit reaction played when the player takes damage.")]
		public DamageFlashSettings DamageFlashSettings;

		[Tooltip("Which sectors are policed, and how strongly.")]
		public SectorSecuritySettings SectorSecuritySettings;

		[Tooltip("The palette the game draws with - hull colours, price colours, hostility colours and so on.")]
		public ColorSettings ColorSettings;

		[Tooltip("How NPC ships decide what to shoot at.")]
		public NpcTargetSearchSettings NpcTargetSearchSettings;

		[Tooltip("How contacts fade on the sector and universe heatmaps.")]
		public HeatmapSettings HeatmapSettings;

		[Tooltip("Which HUD elements are shown and how they are laid out.")]
		public HudSettings HudSettings;

		[Tooltip("Offers the quick tractor button on cargo belonging to a hostile. Off hides the button rather than refusing the action.")]
		public bool ShowQuickTractorButtonForHostileCargo = true;

		[Tooltip("Rules for skirmish mode: what counts as a win and how the scenario ends.")]
		public SkirmishSettings SkirmishSettings;

		[Tooltip("How much cargo leaks out of a hull as it is damaged.")]
		public CargoLeakageSettings CargoLeakageSettings;

		[Tooltip("How intel is gathered, how fresh it is and how much of it a faction shares.")]
		public IntelSettings IntelSettings;

		[Tooltip("Pathfinding quality in the sector the player is in. Has a cheaper fallback algorithm for distant sectors.")]
		public ActiveSectorPathfindingSettings ActiveSectorPathfindingSettings;

		[Tooltip("The formation styles ships can be ordered into, and the default one.")]
		public FormationSettings FormationSettings;

		[Tooltip("How distances are displayed and converted between units.")]
		public DistanceSettings DistanceSettings;

		[FormerlySerializedAs("UnitSpectatorSettings")]
		[Tooltip("Camera behaviour for the HUD: shake, zoom and following.")]
		public HudCameraSettings HudCameraSettings;

		[Tooltip("How the tractor beam attaches, pulls and releases.")]
		public TractorBeamSettings TractorBeamSettings;

		[Tooltip("Net worth in credits at which a faction stops being 'minor' and is treated as a power. Drives diplomacy, AI aggression and which intel it gets.")]
		public int MinorFactionNetWorthThreshold = 2500000;

		[Tooltip("Unstable wormholes: how likely they are to collapse, and where they lead.")]
		public UnstableWormholeSettings UnstableWormholeSettings;

		[Tooltip("How much detail a faction has about a sector it is not standing in.")]
		public SectorIntelSettings SectorIntelSettings;

		[Tooltip("The PlayerPrefs keys behind the sandbox world's generation sliders.")]
		public SandboxPlayerSettings SandboxPlayerSettings;

		[Tooltip("When the game saves on its own, as well as when.")]
		public AutoSaveSettings AutoSaveSettings;

		[Tooltip("How much a trip costs passengers and how that scales with distance.")]
		public PassengerFareSettings PassengerFareSettings;

		[Tooltip("Extra unit types added on top of the base game by mods.")]
		public ModdedUnitSeederSettings ModdedUnitSettings;

		[Tooltip("How mercenaries are hired, how loyal they stay and what they cost.")]
		public MercenarySettings MercenarySettings;

		[Tooltip("How often the radio and crew chatter fires, and how it is picked.")]
		public DialogEventSettings DialogEventSettings;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public float MoonTimePerRevolution = 120f;

		[Tooltip("How the sector map is drawn: scale, scrolling and the unit icons.")]
		public SectorMapSettings SectorMapSettings;

		[Tooltip("Where saves are written and how many slots exist.")]
		public SaveGameSettings SaveGameSettings;

		[Tooltip("Multiple of a turret's own build radius within which it may be placed. Above 1 lets you stack turrets a little closer than their footprint suggests.")]
		public float TurretBuildDistanceRadiusMultiplier = 1.5f;

		[Tooltip("The procedurally varied comms chatter - how it is assembled and how often it repeats.")]
		public DynamicCommsSettings DynamicCommsSettings;

		[Tooltip("Distance in metres ahead of an AI ship that is checked for a mine or explosion before it commits to a course. Larger values make AI more cautious about flying into their own dropped mines.")]
		public float AIMineAvoidanceCheckDistance = 350f;

		[Tooltip("How dynamic missions are generated, weighted and handed out.")]
		public MissionSettings MissionSettings;

		[Tooltip("Per-mission-type rules: reward scaling, expiry and minimum faction standing.")]
		public MissionSpecSettings MissionSpecSettings;

		[Tooltip("Seconds before an equipment dealer's stock is restocked.")]
		public float EquipmentDealerNodeWaitTime = 360f;

		[Tooltip("Default density, size and spacing for asteroid fields when a sector does not override it.")]
		public AsteroidFieldPlacementSettings DefaultAsteroidPlacementSettings;

		[Tooltip("How big the generated universe is and where its edge lies.")]
		public UniverseBoundsSettings UniverseBoundsSettings;

		[Tooltip("Minimum distance in metres the world generator keeps between any two stations, so they never overlap.")]
		public float MinDistanceBetweenStations = 250f;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public float MinDistanceBetweenMinorStations = 50f;

		[Tooltip("Developer switches. Read by debug and cheat code paths only.")]
		public DebugSettings DebugSettings;

		[Tooltip("Frame-time budgets and distance culls. See PerformanceSettings.")]
		public PerformanceSettings PerformanceSettings;

		[Tooltip("Music, ambience and effects volume, and which battle music plays when.")]
		public GameAudioSettings AudioSettings;

		[Tooltip("How the sandbox world generates: sector count, faction seeding and wormhole layout.")]
		public SandboxSettings SandboxSettings;

		[Tooltip("The world-generation settings a new game starts from before the player adjusts them.")]
		public WorldSeedSettings DefaultWorldSeedSettings;

		[Tooltip("Maximum NPC ships simulated in the sector the player is in. The hardest cap on sector size - beyond it ships stop being simulated at all.")]
		public int AIShipCapPerScene = 30;

		[Tooltip("How many autosave slots the game keeps. Older ones are overwritten in turn.")]
		public int NumAutosaves = 3;

		[Tooltip("Restricts saving to moments when the player is docked. Safer, but a crash mid-flight loses everything since the last station.")]
		public bool SaveOnlyWhenDocked;

		[Tooltip("Scales the volume of continuous ship ambience against music and effects.")]
		public float PlayerAmbientSoundVolumeMultiplier = 0.3f;

		[Tooltip("How many low-priority messages are kept in the log before the oldest are dropped.")]
		public int PlayerMessagesMaxUnimportant = 30;

		[Tooltip("Shortest generated faction abbreviation.")]
		public int FactionMinShortNameChars = 2;

		[Tooltip("Longest generated faction abbreviation. Keeping this well under the long-name maximum is what stops abbreviations colliding with full names in the UI.")]
		public int FactionMaxShortNameChars = 10;

		[Tooltip("Longest generated faction name length when the full form is used.")]
		public int FactionMaxLongNameChars = 30;

		[Tooltip("Shortest generated faction name length.")]
		public int FactionMinNameChars = 3;

		[Tooltip("Damage figure that maps to a fixed chance of losing a miner to the field it is working. The reference point the mining damage roll is scaled against.")]
		public float MiningProbabilityDamageRef = 10f;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public Color TargetSpeakingActiveColor = Color.yellow;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public Color TargetSpeakingInactiveColor = Color.white;

		[Tooltip("Smallest bounty in credits that will be placed on a pilot. Below this a kill is not worth a contract.")]
		public int BountyMinCreditsPlaced = 5000;

		[FormerlySerializedAs("BountyProbabilityForEachAIGroup")]
		[Tooltip("Chance that any one NPC pilot ends up carrying a bounty. The field was renamed from BountyProbabilityForEachAIGroup, and the old name is kept so old saves still load.")]
		public float BountyProbabilityForEachPilot = 0.25f;

		[Tooltip("Smallest multiplier on the base reward when a whole group is taken down together.")]
		public float BountyGroupMinRewardMultiplier = 0.15f;

		[Tooltip("Largest multiplier on the base reward when a whole group is taken down together.")]
		public float BountyGroupMaxRewardMultiplier = 0.5f;

		[Tooltip("Credits to restore one point of a shield layer.")]
		public float CostToRepairShieldPoint = 1f;

		[Tooltip("Discount when repairing a ship at a station you own, as a fraction off the normal cost.")]
		public float OwnedStationShipRepairMultiplier = 0.75f;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public bool RecordFactionTransactionForAI = true;

		[Tooltip("How many past transactions each faction remembers, and therefore how far back its ledgers and grudges go.")]
		public int NumberOfFactionTransactionsHistory = 30;

		[Tooltip("Distance in metres beyond which the construction frame and its UI are not drawn.")]
		public float UIUnderConstructionDrawDistance = 200f;

		[Tooltip("Seconds of work per hull point a station construction needs. The main balance number for how long player building takes.")]
		public float ConstructionTimePerHp = 0.1f;

		[Tooltip("Seconds of work per hull point to dismantle a station.")]
		public float DismantlingTimePerHp = 0.1f;

		[Tooltip("Hull fraction a station has the moment it is built, before it has been finished off. Below 1 it starts fragile, which is what makes an unfinished station a target.")]
		public float ConstructionInitialHealth = 0.1f;

		[Tooltip("Lets the player board or take a ship without being close to it. Mostly a convenience or debug switch.")]
		public bool CanEntershipFromAnyDistance;

		[Tooltip("Lets the player move cargo between ships without being close to them.")]
		public bool CanTransferCargoFromAnyDistance;

		[Tooltip("Distance in metres within which the player may claim an abandoned ship.")]
		public float ClaimShipDistance = 70f;

		[Tooltip("Distance in metres within which the player may board and take a ship.")]
		public float EnterShipDistance = 70f;

		[Tooltip("Distance in metres within which cargo may be transferred between ships.")]
		public float TransferCargoDistance = 70f;

		[Tooltip("Hard ceiling on how far cargo may be moved by tractor beam, whatever other distance checks pass.")]
		public float MaxTransferCargoDistance = 1200f;

		[Tooltip("Minimum distance in metres from a wormhole at which the player may deploy cargo pods, so they are not dropped into the jump lane.")]
		public float CargoDeployMinDistanceFromWormhole = 750f;

		[Tooltip("Minimum distance in metres from a station before the player may deploy cargo pods.")]
		public float CargoDeployMinDistanceFromDockableStations = 200f;

		[Tooltip("The behaviour settings every NPC pilot runs on.")]
		public NpcPilotBehaviourSettings AiUnitControllerSettings;

		[Tooltip("When an NPC pilot gives up on a field and drops its cargo.")]
		public NpcPilotBehaviourMineDropSettings NpcPilotBehaviourMineDropSettings;

		[Tooltip("Throttle an AI pilot backs off to while dodging. Under 1 means it slows down as well as steering away.")]
		public float AIPreferredThrottleWhenAvoiding = 0.6f;

		[Tooltip("Adds random scatter to turret fire. Turn this off and every turret in the game becomes perfectly accurate, which makes positioning trivial.")]
		public bool TurretInaccuracyEnabled = true;

		[Tooltip("Faction-wide rules: expansion, aggression, repair and station building.")]
		public FactionSettings FactionSettings;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public DateTime DefaultScenarioStartDate = new DateTime(2235, 11, 16);

		[Tooltip("Scales the price of repairing a component, leaving the hull repair price alone.")]
		public float ComponentRepairCostMultiplier = 0.95f;

		[Tooltip("How many game seconds pass per real second. The base rate the whole simulation clock runs at - 48 means a game day passes in half an hour of real time.")]
		public float GameTimeToRealTimeConversion = 48f;

		[Tooltip("What the player is allowed to do in a fresh world - build, dock, jump and so on.")]
		public WorldPlayerPermissions DefaultWorldPermissions;

		[Tooltip("The ladder of wealth bands used to classify factions and units.")]
		public WealthLevels WealthLevels;

		[Tooltip("The ladder of power bands used to classify factions and units.")]
		public WealthLevels PowerLevels;

		[Tooltip("The scene the game shows while a save is being loaded. Referenced only so the loading screen can be streamed in ahead of time.")]
		public Sector LoadGameScenePrefab;

		[Tooltip("Reputation penalty, as a multiple of the normal cost, for abandoning a mission.")]
		public float AbortMissionPenaltyMultiplier = 4f;

		[Tooltip("Largest roll angle in degrees the player's ship banks to on a hard turn.")]
		public float ActiveUnitMaxRollAngle = 60f;

		[Tooltip("Rate of change that produces the maximum bank, in degrees per second. Turning faster than this just holds the maximum roll.")]
		public float ActiveUnitMaxRollTurnValue = 100f;

		[Tooltip("Smoothing on the bank angle. Above 1 snaps the roll faster; below 1 makes it lean in lazily.")]
		public float ActiveUnitRollLerp = 1f;

		[Tooltip("How late into a turn the ship starts rolling, 0-1. Higher means it banks early and holds the angle longer through the corner.")]
		public float ActiveUnitTurnArrivalAggression = 0.4f;

		[Tooltip("Colour a price is drawn in when the player can afford it.")]
		public Color AffordableColor = Color.green;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public bool AIActivePilotAvoidProximitityToLargerCombatTarget = true;

		[Tooltip("Shortest interval in seconds between an AI pilot re-deciding who its best target is.")]
		public float AISelectBestCombatTargetMinFrequency = 2f;

		[Tooltip("Longest interval in seconds between those decisions. The gap between the two is the spread of how decisive AI feels.")]
		public float AISelectBestCombatTargetMaxFrequency = 6f;

		[Tooltip("How fast an AI pilot commits to a new heading, in units per second. Lower values make it commit harder and turn less often.")]
		public float AIActivePilotTurnChangeRate = 0.5f;

		[Tooltip("Lets AI cloak and decloak. Turning it off leaves every AI permanently visible.")]
		public bool AICloakingUpdateEnabled = true;

		[Tooltip("Lets AI ships ram. Off stops NPCs deliberately using collisions as a weapon.")]
		public bool AICollisionDamageEnabled;

		[Tooltip("Shortest wait in seconds before an AI ship may dock at a station again.")]
		public float AIDockMinCoolDownTime = 5f;

		[Tooltip("Shortest wait before an AI docks at a station the player owns.")]
		public float AIDockPlayerOwnedMinCoolDownTime = 5f;

		[Tooltip("Longest wait before an AI docks at a station the player owns.")]
		public float AIDockPlayerOwnedPreferredCoolDownTime = 30f;

		[Tooltip("Longest wait before an AI chooses to dock again. Together with the minimum this is the range the wait is picked from.")]
		public float AIDockPreferredCoolDownTime = 120f;

		[Tooltip("Lets AI weapons actually fire. The master switch for AI combat - off, AI can manoeuvre and target but not shoot.")]
		public bool AIFiringUpdateEnabled = true;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public float AIGroupCombatCooldownTime = 20f;

		[Tooltip("Distance in metres within which AI groups will not spawn, so reinforcement ships do not appear on top of the player.")]
		public float AIGroupSpawnMinPlayerDistance = 800f;

		[Tooltip("Seconds after which a group's target is dropped for being stale, rather than being chased indefinitely.")]
		public float AIGroupStaleTargetRemoveTime = 10f;

		[Tooltip("Seconds before an AI that lost sight of a cloaked target may start hunting it again.")]
		public float AILostCloakedTargetCoolDownTime = 240f;

		[Tooltip("Seconds an AI waits before re-engaging after breaking off a fight.")]
		public float AILostCombatCombatCoolDownTime = 5f;

		[Tooltip("Flat distance in metres AI ships keep from an explosion, on top of the scaled distance below. The belt-and-braces half of the avoidance rule.")]
		public float AIMaxMineAvoidDist = 200f;

		[Tooltip("Lower bound applied to the scaled safe distance, so a tiny explosion still keeps AI clear of it.")]
		public float AIMineSafeDistanceMinMultiplier = 1.1f;

		[Tooltip("Multiple of an explosion's radius that AI ships treat as unsafe. The main half of the avoidance rule.")]
		public float AIMineSafeDistanceMultiplier = 1.3f;

		[Tooltip("Faction opinion below which a faction will not trade with another at all.")]
		public float AIMinTradeOpinion = 0.3f;

		[Tooltip("Distance in metres scattered ships close to before they count as regrouped.")]
		public float AIPilotRegroupDist = 300f;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public float AIPointDefenseDeploymentProbability = 0.45f;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public float AIPointDefenseMaxDeploymentRange = 350f;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public float AIPointDefenseMinMissileLifetime = 1.5f;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public float AIPointDefenseUpdateInterval = 0.85f;

		[Tooltip("Seconds between path recalculations for AI units.")]
		public float AITimeBetweenPathfinding = 8f;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public float AITurretExistingTargetScore = 5f;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public float AITurretPreferredTargetScore = 5f;

		[Tooltip("Lets AI group spawners place ships into the world. Turn it off and the AI stops reinforcing, which quietly freezes the entire faction simulation.")]
		public bool AllowAiGroupSpawners = true;

		[Tooltip("Lets the player dock anywhere docking is otherwise permitted.")]
		public bool AllowPlayerDock;

		[Tooltip("Highest impulse an asteroid can be spawned with, in arbitrary force units. Gives new fields some drift rather than starting perfectly static.")]
		public float AsteroidSpawnMaxForce = 90f;

		[Tooltip("Lowest impulse an asteroid can be spawned with.")]
		public float AsteroidSpawnMinForce = 45f;

		[Tooltip("The attribute points a unit gains at each level, as a list of level entries.")]
		public List<UnitAttributeLevels> AttributeLevels = new List<UnitAttributeLevels>();

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public int AttributePointsPerLevel = 10;

		[Tooltip("Seconds of fighting before combat music is allowed to start, so a short scrap does not trigger a track.")]
		public float AudioCombatMusicTimeout = 25f;

		[Tooltip("Lets the player push cargo out of the bay while docked. Off forces an undock first.")]
		public bool CanEjectCargoWhileDocked = true;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public float CloakedPlayerUnitAlphaRange = 0.8f;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public float CloakedUnitAlphaRange = 0.9f;

		[Tooltip("Master switch for ramming damage.")]
		public bool CollisionDamageEnabled;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public ShieldDamageType CollisionShieldDamageType = ShieldDamageType.Envelope;

		[Tooltip("How much of the scatter is contributed when the turret is in a normal state.")]
		public float TurretInaccuracyDmgWeight = 0.8f;

		[Tooltip("How much of the scatter is contributed when the turret is undamaged and free-running. Splitting the two lets damage change the feel of a turret's fire.")]
		public float TurretInaccuracyNormalWeight = 0.2f;

		[Tooltip("Largest angle in degrees a turret shot can miss by.")]
		public float TurretMaxInaccuracyDegrees = 10f;

		[Tooltip("Ceiling on the number of damage particles one unit may spawn.")]
		public int DamageParticlesMaxCount = 4;

		[Tooltip("Damage fraction above which damage particles are at full strength.")]
		public float DamageParticlesMaxHealth = 0.2f;

		[Tooltip("Damage fraction below which damage particles stop being emitted, giving a band rather than a hard cut.")]
		public float DamageParticlesMinHealth = 0.1f;

		[Tooltip("Damage fraction before a unit starts emitting damage particles, so a barely scratched hull stays clean.")]
		public float DamageParticlesThreshold = 0.75f;

		[Tooltip("Seconds the camera shake lasts after the player takes a hit.")]
		public float DamageShakeDuration = 0.75f;

		[Tooltip("Camera shake per point of damage taken. Together with the duration above, sets how violent a hit feels.")]
		public float DamageToShakeMagnitudeMultiplier = 0.02f;

		[Tooltip("Seconds a piece of wreckage persists before it is removed.")]
		public float DebrisLifetime = 240f;

		[Tooltip("Exponent controlling how hard a destroyed ship's remaining cargo is thrown clear. Higher keeps the pods from clumping where the wreck was.")]
		public float DestroyedUnitCargoDropPower = 2f;

		[Tooltip("Largest fraction of a destroyed ship's cargo that spills out as loose pods.")]
		public float DestroyedUnitMaxDroppedCargo = 0.75f;

		[Tooltip("Smallest fraction of a destroyed ship's cargo that spills out as loose pods.")]
		public float DestroyedUnitMinDroppedCargo = 0.05f;

		[Tooltip("Spawns physical wreckage when a ship is destroyed. Off means a destroyed ship just disappears, which is much cheaper but loses the battlefield's history.")]
		public bool DestroyedUnitsCreateDebris;

		[Tooltip("Distance in metres within which a docking prompt appears for a station or ship.")]
		public float DockableDistance = 75f;

		[Tooltip("Impulse used to push cargo pods out of a ship's bay, in arbitrary force units.")]
		public float EjectCargoForce = 5f;

		[Tooltip("Energy drawn per unit of engine power output, so throttle and fuel use stay linked.")]
		public float EnginePowerToEnergyUsage = 4f;

		[Tooltip("Lets equipment dealers sell regardless of how much they actually hold.")]
		public bool EquipmentDealerInfiniteCargo = true;

		[Tooltip("Distance in metres from the camera beyond which cargo in the player's own sector expires on its own timer even if it has not run out. Keeps abandoned pods from accumulating off-screen.")]
		public float ExpireCargoInActiveSceneDistanceFromCamera = 1750f;

		[Tooltip("How much of an explosion's damage is converted into physical force on nearby objects.")]
		public float ExplosionDamageToForceRatio = 0.1f;

		[Tooltip("Scale on the random torque an explosion applies to debris.")]
		public float ExplosionTorqueMultiplier = 10f;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public float HyperdriveAcceleration = 20f;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public float HyperdriveMovespeed = 100f;

		[Tooltip("Damage multiplier applied to units that are not in the sector the player is in. Low values let the off-screen simulation stay cheap.")]
		public float InactiveFireMultiplier = 0.1f;

		[Tooltip("Speed multiplier for groups of units moving in a sector the player is not in.")]
		public float InactiveGroupMoveSpdMultiplier = 0.4f;

		[Tooltip("Distance in metres from a wormhole at which a ship emerges. Too small and ships arrive already inside other traffic.")]
		public float JumpGateExitDistance = 180f;

		[Tooltip("The cargo types that can appear as salvage, in the same order as the Lootable values arrays below.")]
		public CargoClass[] LootableCargoClasses;

		[Tooltip("Multiplier on the maximum salvage quantity when a wreck's contents are rolled.")]
		public float LootableCargoMaxMultiplier = 2f;

		[Tooltip("Largest amount of each cargo in the array above that a wreck can hold.")]
		public int[] LootableCargoMaxValues;

		[Tooltip("Smallest amount of each cargo in the array above that a wreck can hold.")]
		public int[] LootableCargoMinValues;

		[Tooltip("For each cargo in the array above, what fraction of a ship's sale value one unit is worth.")]
		public float[] LootableCargoShipValueDivisors;

		[Tooltip("Highest spin a loot container can be given when it pops out of a wreck.")]
		public float LootContainerMaxAngularVelocity = 30f;

		[Tooltip("Lowest spin a loot container can be given when it pops out of a wreck.")]
		public float LootContainerMinAngularVelocity = 30f;

		[Tooltip("Highest impulse a loot container is thrown with when it leaves a wreck.")]
		public float LootSpawnMaxForce = 30f;

		[Tooltip("Lowest impulse a loot container is thrown with when it leaves a wreck.")]
		public float LootSpawnMinForce = 10f;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public int MaxAttributePoints = 100;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public float MaxDamageParticlesTime = 8f;

		[Tooltip("How many dynamic missions can be active at once.")]
		public int MaxDynamicMissions = 3;

		[Tooltip("Longest wait in seconds before a depleted shield layer starts regenerating. The actual delay is scaled by how badly the layer was drained, so a near-empty layer takes longer to come back.")]
		public float MaxShieldRestoreDelay = 120f;

		[Tooltip("Damage figure treated as a fully drained layer when scaling that regeneration delay. Anything worse uses this value as the cap.")]
		public float MaxShieldRestoreDelayValue = 1000f;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public float MinDamageParticlesTime = 3f;

		[Tooltip("Shortest wait in seconds before a depleted shield layer starts regenerating.")]
		public float MinShieldRestoreDelay = 10f;

		[Tooltip("Chance a delivery mission is for a ship rather than cargo.")]
		public float MissionsDeliverShipPercentage = 0.05f;

		[Tooltip("Base payment in credits for delivering a ship, before faction and distance multipliers.")]
		public int MissionsDeliverShipBaseIncome = 750;

		[Tooltip("Seconds a passenger group waits at a station before giving up and leaving.")]
		public float PassengersGroupExpireTime = 500f;

		[Tooltip("Group size the passenger system aims for at stations.")]
		public int PassengersPreferredGroupCount = 10;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public float PlayerAutoTargetArmedScore = 4f;

		[Tooltip("Score added per candidate for how close it is to the centre of the screen. Higher makes the auto-targeting prefer whatever you are pointing at.")]
		public float PlayerAutoTargetBearingScore = 3f;

		[Tooltip("Score weighting on distance when auto-targeting. Higher makes it prefer nearby ships over distant ones.")]
		public float PlayerAutoTargetDistanceScore = 5f;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public float PlayerAutoTargetHostileScore = 10f;

		[Tooltip("Score added for a target that already has a missile inbound on it. Set high, this makes auto-targeting snap to whatever is about to be hit.")]
		public float PlayerAutoTargetLockedMissileScore = 10f;

		[Tooltip("Distance in metres beyond which auto-targeting ignores a candidate entirely. Without it the auto-target will happily lock onto something too far away to matter.")]
		public float PlayerAutoTargetMaxDistance = 1000f;

		[Tooltip("Scales all damage the player takes. The player's own difficulty knob, separate from the global multiplier.")]
		public float PlayerDamageMultiplier = 0.75f;

		[Tooltip("Turns player experience on. Off stops all progression for the pilot.")]
		public bool PlayerXpEnabled = true;

		[Tooltip("Turns clickable links in the quick-message bar on.")]
		public bool QuickMsgLinksEnabled;

		[Tooltip("Makes laser damage fall off past the weapon's rated range instead of stopping dead at it.")]
		public bool ReduceLaserDamageWithRange;

		[Tooltip("Speed change, in units per second, above which scattered ships begin heading back to the group.")]
		public float RegroupSpdChangeThreshold = 50f;

		[Tooltip("Speed change above which the regroup call is abandoned. The band between the two is the hysteresis that stops a ship dithering on the threshold.")]
		public float RegroupSpdChangeThresholdUpper = 200f;

		[Tooltip("Keeps loot owned by the faction that spawned it. Off lets anyone claim salvage, including from a wreck in the middle of a fight.")]
		public bool RetainLootOwnership = true;

		[Tooltip("Regeneration rate as a fraction of capacity per second. The slower, continuous term that runs alongside the discrete restore steps.")]
		public float ShieldRelativeRegenRate = 0.01f;

		[Tooltip("Fraction of a layer's full capacity restored in one regeneration step.")]
		public float ShieldRestoreNormalizedValue = 0.05f;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public GameObject ShieldRingPrefab;

		[Tooltip("Capacitor fraction below which the HUD capacitor ring appears.")]
		public float ShowCapacitorLowerThreshold = 0.9f;

		[Tooltip("Capacitor fraction above which the HUD capacitor ring fades out again, so it does not flicker around the threshold.")]
		public float ShowCapacitorUpperThreshold = 0.95f;

		[Tooltip("Puts mission progress into the message log.")]
		public bool ShowMissionMsgs;

		[Tooltip("Puts objective updates into the message log.")]
		public bool ShowObjectiveMsgs;

		[Tooltip("Highest spin an asteroid can be spawned with, in degrees per second.")]
		public float SpawnAsteroidMaxAngularVelocity = 60f;

		[Tooltip("Largest random tumble angle an asteroid is spawned with, in degrees. Affects its initial orientation, not its visible spin.")]
		public float SpawnAsteroidMaxRandomAngle = 20f;

		[Tooltip("Lowest spin an asteroid can be spawned with.")]
		public float SpawnAsteroidMinAngularVelocity = 30f;

		[Tooltip("Seconds before the same chatter line can be picked again. Low values make the crew noticeably repetitive.")]
		public float SpeechModelMinTimeBeforeRepeat = 30f;

		[Tooltip("Colour cargo names are drawn in on the cargo screen.")]
		public Color TextCargoColor = Color.green;

		[Tooltip("Colour used for scene and location headings.")]
		public Color TextSceneColor = Color.green;

		[Tooltip("Colour used for panel headings.")]
		public Color TextHeadingColor = Color.green;

		[Tooltip("Colour a unit's class name is drawn in.")]
		public Color TextUnitClassColor = Color.green;

		[Tooltip("Colour a unit's name is drawn in. Distinct from the class colour so a long class name does not read as part of the name.")]
		public Color TextUnitNameColor = Color.green;

		[NonSerialized]
		[Tooltip("Unit types the tractor beam will attach to. Marked NonSerialized, so it is code-only and never saved - editing it in the Inspector has no effect.")]
		public UnitType TractorableUnitTypes = UnitType.Ship | UnitType.Cargo;

		[Tooltip("How quickly the tractor beam's reach collapses when it is switched off, in units per second.")]
		public float TractorBeamRetractSpeed = 5f;

		[Tooltip("Distance in metres at which a tractor beam snaps off its target. Roughly the far end of the force range, with headroom so a beam does not flicker.")]
		public float TractorBreakDistance = 125f;

		[Tooltip("Distance in metres beyond which the tractor beam stops applying force.")]
		public float TractorMaxForceApplyDist = 100f;

		[Tooltip("Distance in metres at which the tractor beam starts applying force. Below this it holds the target steady instead of pulling.")]
		public float TractorMinForceApplyDist = 50f;

		[Tooltip("Colour a price is drawn in when the player cannot afford it.")]
		public Color UnaffordableColor = Color.red;

		[Tooltip("Distance in metres the player's ship must move from a station before it is considered undocked and free to fly.")]
		public float UndockDistance = 50f;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public float UnitCollisionDamageFactor = 1f;

		[Tooltip("Impact force, in arbitrary force units, above which a collision starts doing damage.")]
		public float UnitCollisionDamageForceThreshold = 5f;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public float UnitCollisionEngineCooldownThreshold;

		[Tooltip("How much of a ramming impact is converted into the target losing engine control rather than taking hull damage.")]
		public float UnitCollisionImmobilizationFactor = 0.25f;

		[Tooltip("Seconds an engine can stay knocked out by a collision before it comes back on its own.")]
		public float UnitCollisionMaxImmobileTime = 2f;

		[Tooltip("How quickly a tumbling ship or piece of debris stops spinning. Higher settles it faster.")]
		public float UnitDefaultAngularDrag = 2f;

		[Tooltip("Lower bound of the torque range applied when a ship is destroyed. The field name has a typo ('Desctruction') and is kept for the same reason.")]
		public float UnitDesctructionMinTorqueMultiplier = 0.5f;

		[Tooltip("Scale on the random torque a ship's destruction applies to its own debris. The field name has a typo ('Desctruction') and is kept, because Unity keys saved data off it.")]
		public float UnitDesctructionTorqueMultiplier = 10f;

		[Tooltip("Turns ship banking on. Off and ships fly flat, which reads as very obviously wrong in motion.")]
		public bool UnitRollEnabled = true;

		[Tooltip("Shortest generated pilot name length.")]
		public int PilotNameMinChars = 3;

		[Tooltip("Longest generated pilot name length.")]
		public int PilotNameMaxChars = 12;

		[Tooltip("Shortest generated pilot title length.")]
		public int PilotTitleMinChars = 3;

		[Tooltip("Longest generated pilot title length.")]
		public int PilotTitleMaxChars = 20;

		[Tooltip("Chance a newly built station triggers a message to the player.")]
		public float NewStationPlayerNotificationProbability = 0.75f;

		[Tooltip("Maximum number of jumps the station may be from the player and still notify them.")]
		public float NewStationPlayerNotificationMaxJumpDistance = 6f;

		[Tooltip("Seconds after a station is built before it is allowed to notify the player, so notifications do not all arrive at once after a build spree.")]
		public float NewStationPlayerNotificationMinTimeBeforeShow = 600f;

		[Tooltip("Lets the player claim an unowned, unallied capital ship as a fleet's home base.")]
		public bool AllowFleetToSetHomeBaseToUnownedUnalliedCapitalShip;

		[Tooltip("How many orders the player can stack on one fleet before the oldest is replaced.")]
		public int MaxPlayerFleetStackedOrders = 8;

		[Tooltip("Multiple of a wormhole's rated range within which the universe map will let you plot a jump. Above 1 lets you plot destinations you cannot reach yet.")]
		public float MaxExploreRangeGateDistanceMultiplier = 1.2f;

		[Tooltip("How much of the firing ship's velocity a projectile inherits, 0-1. At 1 shots inherit it all and leading a moving target becomes pointless.")]
		public float ProjectileVelocityInheritanceFactor = 0.8f;

		[Tooltip("Exponent on distance for explosion damage. Damage falls off as (1 - distance/maxDistance) raised to this power, so above 1 damage drops off faster and the blast stays punchy at the centre.")]
		public float ExplosionDamageFalloutPower = 1.4f;

		[Tooltip("Speed an AI ship must be travelling at before it will lay a mine. Below this it passes through a field rather than seeding it.")]
		public float AIMinSpeedToDeployMine = 30f;

		[Tooltip("UNUSED - nothing in the code reads this setting, so changing it here has no effect. Left in place because the value is still serialised on the prefab.")]
		public bool BanditsAlwaysAtWarWithBandits;

		[Tooltip("Pool of names handed out as pilot titles when the player has not set one.")]
		public string[] RandomTitles;

		[Tooltip("How pilots are ranked against each other, which decides what the ranking leaderboard actually measures.")]
		public PilotRankingSystem DefaultRankingSystem;

		[Tooltip("Baseline sensor range in metres before a unit's own sensor package modifies it.")]
		public float ScanRange = 1500f;

		[Tooltip("Seconds of holding before a click on the map becomes a box multi-select rather than a single pick.")]
		public float UIAutoMultiSelectTime = 1.2f;
	}
}
