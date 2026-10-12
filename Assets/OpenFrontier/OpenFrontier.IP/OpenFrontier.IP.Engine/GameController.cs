using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenFrontier.IP.Engine.Core;
using OpenFrontier.IP.Engine.CustomUnitVariants;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Sandbox;
using OpenFrontier.IP.Engine.SaveGame;
using OpenFrontier.IP.Engine.SectorNaming;
using OpenFrontier.IP.Engine.WorldGeneration.Models;
using OpenFrontier.IP.IO;
using OpenFrontier.IP.MusicPlayer;
using OpenFrontier.IP.UI;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.ZFrame;
using OpenFrontier.IP.billing;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Input = OpenFrontier.LegacyInput;

namespace OpenFrontier.IP.Engine
{
	public class GameController : MonoBehaviour
	{
		public enum EngineLaunchSource
		{
			Unspecified,
			BattlesUI,
			TutorialsUI,
			ContinueGame,
			ScenariosUI,
			LoadGame,
			Skirmish
		}

		public EventSystem EventSystem;

		public bool AutoTurnEnabled;

		public string SupportEmail = "nightvizla@gmail.com";

		public PlayerOptionConstants PlayerOptionConstants;

		public PlayerOptions PlayerOptions;

		public IPProduct GodModeRequiredProduct;

		public string DefaultPilotTitle = "Commander";

		public string DefaultFactionName = "Frontier Corporation";

		public string DefaultFactionShortName = "Frontier";

		public TextAsset CustomSectorNamesTextAsset;

		public bool AllowScreenFade = true;

		public string DialogEventsPath = "WorldData/DialogEvents";

		public AvatarController AvatarController;

		public Sprite ContextOptionsSprite;

		public float CameraDragRotateSensitivity = 0.5f;

		public float CameraDragRotateSensitityMultiplierStandalone = 1f;

		public float MinUIScaleValue;

		public float MaxUIScaleValue = 0.2f;

		public float UISafeAreaTop;

		public float UISafeAreaBottom;

		public float UISafeAreaLeft;

		public float UISafeAreaRight;

		public GameData GameData;

		public Faction GenericFactionPrefab;

		public Faction PlayerFactionPrefab;

		public AudioSource EjectPassengerAudioSource;

		public int CurrentQualityLevel = -1;

		public TextAsset NatoAlphabetFile;

		public TextAsset GreekAlphabetFile;

		public string[] GreekAlphabet;

		public string[] GreekAlphabetTitleCase;

		public string[] NatoAlphabet;

		public string[] NatoAlphabetTitleCase;

		public string[] CountNames = new string[11]
		{
			"zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
			"ten"
		};

		public string[] CountNamesTitleCase = new string[11]
		{
			"Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
			"Ten"
		};

		public SectorNamer SectorNamer;

		public WorldBlueprint WorldBlueprintToGenerate;

		private AutoSaveSettings autoSaveSettings;

		private SandboxPlayerSettings sandboxPlayerSettings;

		public string DefaultUnitModelName = "A";

		public ScreenScaler ScreenScaler;

		private UIController uiController;

		public UnitClasses UnitClasses;

		public CargoClasses CargoClasses;

		public LoadedActiveUnitPrefabCache ActiveUnitPrefabCache;

		public const string AutoSaveNumKey = "autosave_num";

		private bool isFirstRun;

		private const string firstRunKey = "first_run";

		public PurchaserBridge PurchaseBridge;

		public Color StoreDefaultColor = Color.yellow;

		public Color StorePurchasedColor = Color.green;

		public ScenarioInfo MainMenuScenario;

		public ScenarioInfo CustomScenarioInfo;

		public ScenarioInfo SandboxScenarioInfo;

		public bool AllScenariosUnlocked;

		private Camera mainCamera;

		private AudioSource mainCameraAudioSource;

		public IAPTestReponse IAPTestResponse;

		private Vector3 lastMainCameraPosition = Vector3.zero;

		public Vector3 MainCameraVelocity = Vector3.zero;

		public SkyboxCamera SkyboxCamera;

		public Camera PlanetCamera;

		public float MinCanvasRefHeight = 600f;

		public float MaxCanvasRefHeight = 1200f;

		public const bool DefaultTiltControlEnabled = false;

		public const float DefaultTiltSensitivity = 18f;

		public const float DefaultTouchTurnRadius = 100f;

		public const float DefaultCanvasHeight01 = 0.8f;

		public const float MaxTouchTurnRadius = 200f;

		public const float MinTouchTurnRadius = 30f;

		public const float MinTiltSensitivity = 4f;

		public const float MaxTiltSensitivity = 24f;

		public const float TiltResponsiveness = 2f;

		public static float TargetScreenHeightInches = 2.347441f;

		private static GameController instance = null;

		private Sector[] allSectors;

		private UnitClass[] loadedUnitClasses;

		public Color[] CompletionColors;

		public GameCombatDifficultyLevel DefaultCombatDifficultyLevel;

		public float DefaultMusicVolume = 0.5f;

		public GameCombatDifficultyLevel CombatDifficultyLevel;

		public string CombatDifficultyLevelKey = "combat_difficulty";

		[FormerlySerializedAs("DifficultyLevels")]
		public GameCombatDifficultyLevel[] CombatDifficultyLevels;

		public float FadedScreenAlpha = 0.5f;

		public bool GameSaveEnabled = true;

		public GameSettings GameSettings;

		public bool IgnoreScenarioPrereqs;

		public float CanvasHeight01 = 0.8f;

		public string CanvasHeight01Key = "interface_scale";

		public ScenarioInfo LastAttemptedScenario;

		public bool LastAttemptedScenarionCompleted;

		public EngineLaunchSource LaunchOrigin;

		public Camera MainCameraPrefab;

		public string MasterVolumeKey = "master_volume";

		public string RespawnOnDeathKey = "respawn_on_death";

		public LayerMask AsteroidMask;

		public LayerMask ShipsMask;

		public LayerMask MissileLayer;

		public LayerMask SelectableMask;

		public LayerMask DamageableMask;

		public LayerMask ShipsAndStationsMask;

		public LayerMask ShipsStationsAndMissilesMask;

		public LayerMask StaticNonOverlappingMask;

		public LayerMask AIScanMask;

		public LayerMask StationsMask;

		public LayerMask CargoLayer;

		public LayerMask NonOVerlappingUnitsMask;

		public LayerMask AsteroidClusterMask;

		public LayerMask StationsAndWormholesMask;

		public LayerMask MineMask;

		public LayerMask AsteroidClusterPlacementCheckMask;

		public LayerMask UnitGasCloudMask;

		public LayerMask DamageObjectMask;

		public SimpleMusicPlayer MusicPlayer;

		public string MusicVolumeKey = "music_volume";

		public bool AutoCloseInGameMenu = true;

		public string DefaultPilotName = "Player";

		public const string DefaultPilotNameValue = "Player";

		public string DefaultPilotNameKey = "default_pilot_name";

		public string DefaultPilotTitleKey = "default_pilot_title";

		public string DefaultFactionNameKey = "default_faction_name";

		public string DefaultFactionShortNameKey = "default_faction_short_name";

		public string AutoCloseInGameMenuKey = "auto_close_ingame_menu";

		public bool PlayButtonSounds = true;

		public string PlayButtonSoundsKey = "button_sounds";

		public bool PreferCameraLock;

		public string PreferCameraLockKey = "prefer_camera_lock";

		public bool AutoTargetHostiles = true;

		public string AutoTargetHostilesKey = "auto_target_hostiles";

		public const bool DefaultAutoTargetHostiles = true;

		private Dictionary<int, ScenarioInfo> scenarioInfoMap = new Dictionary<int, ScenarioInfo>();

		public ScenarioLoaderUI ScenarioLoader;

		public bool ShipTrails = true;

		public ScenarioInfo SkirmishScenarioInfo;

		public bool SpaceFogEnabled = true;

		// Open Frontier: the user's raw fps dropdown choice (30/60/90/120/144).
		// The applied cap is min(UserTargetFrameRate, panel refresh rate);
		// storing the choice separately lets the cap release when the
		// refresh rate goes back up.
		public int UserTargetFrameRate = 60;

		public bool LightingFXEnabled = true;

		public const bool DefaultLightingFXEnabled = true;

		public const bool DefaultSpaceFogEnabled = true;

		public const bool DefaultRenderCloakedShipOutlinesEnabled = true;

		public float HypersleepTimeMultiplier = 8f;

		public string HypersleepTimeMultiplierKey = "hypersleep_time_multiplier";

		public string UISafeAreaTopKey = "ui_safe_area_top";

		public string UISafeAreaBottomKey = "ui_safe_area_bottom";

		public string UISafeAreaLeftKey = "ui_safe_area_left";

		public string UISafeAreaRightKey = "ui_safe_area_right";

		public string GraphicsQualityLevelKey = "graphics_quality_level";

		public int DefaultQualityLevel = 2;

		public bool RenderCloakedShipOutlinesEnabled = true;

		public string RenderCloakedShipOutlinesKey = "cloaked_outline_enabled";

		public string SpaceFogEnabledKey = "space_fog_enabled";

		public string LightingFXEnabledKey = "lighting_fx_enabled";

		public float TiltControlDefaultAccelerationX;

		public string TiltControlDefaultAccelerationXKey = "tilt_default_pos_x";

		public string CameraDragRotateSensitivityKey = "camera_drag_rotate_sensitivity";

		public bool TiltControlEnabled;

		public const bool DefaultForceTouchInputEnabled = false;

		public bool ForceTouchInputEnabled;

		public string TiltControlEnabledKey = "tilt_control_enabled";

		public string ForceTouchInputEnabledKey = "force_touch_input_enabled";

		public float TiltPower = 6f;

		public float TiltSensitivity = 18f;

		public string TiltSensitivityKey = "tilt_sensitivity";

		public float TouchTurnPower = 1.8f;

		public float TouchTurnRadius = 100f;

		public string TouchTurnRadiusKey = "touch_turn_sensitivity";

		public LayerMask WormholeMask;

		public int DetectableLayer = 28;

		public bool ForceLeftClickTargetting;

		public bool RespawnOnDeath = true;

		public List<string> CustomSectorNames { get; private set; }

		public double RealDeltaTime { get; private set; }

		public double RealDeltaTimeFloat => (float)RealDeltaTime;

		public double LastRealTime { get; private set; }

		public Camera MainCamera => mainCamera;

		public AudioSource MainCameraAudioSource => mainCameraAudioSource;

		public float InterfaceSize => Mathf.Lerp(0.28f, 1f, CanvasHeight01);

		public static GameController Instance => instance;

		public int CombatDifficultyLevelIndex
		{
			get
			{
				return CombatDifficultyLevels.IndexOf(CombatDifficultyLevel);
			}
			set
			{
				GameCombatDifficultyLevel gameCombatDifficultyLevel = CombatDifficultyLevels[value];
				if (gameCombatDifficultyLevel != CombatDifficultyLevel)
				{
					CombatDifficultyLevel = gameCombatDifficultyLevel;
					if (EngineASX.LoadedAndReady)
					{
						EngineASX.Instance.OnCombatDifficultyChanged();
					}
				}
			}
		}

		public bool IsTiltControlEnabled
		{
			get
			{
				if (OpenFrontier.IP.UI.UI.IsMobileDevice)
				{
					return TiltControlEnabled;
				}
				return false;
			}
		}

		public IEnumerable<ScenarioInfo> LoadedScenarioInfos => scenarioInfoMap.Values;

		public UnitClass[] LoadedUnitClasses => loadedUnitClasses;

		public float TiltSensitivityNorm
		{
			get
			{
				return GetTiltSensitivityNorm(TiltSensitivity);
			}
			set
			{
				TiltSensitivity = GetTiltSensitivityFromNorm(value);
			}
		}

		public float TouchTurnRadiusNorm
		{
			get
			{
				return GetTouchTurnRadiusNorm(TouchTurnRadius);
			}
			set
			{
				TouchTurnRadius = GetTouchTurnRadiusFromNorm(value);
			}
		}

		public Sector[] AllSectors => allSectors;

		public float InterfaceReferenceHeight => Mathf.Lerp(instance.MinCanvasRefHeight, instance.MaxCanvasRefHeight, Mathf.Clamp01(InterfaceSize));

		public SandboxPlayerSettings SandboxPlayerSettings => sandboxPlayerSettings;

		public AutoSaveSettings AutoSaveSettings => autoSaveSettings;

		public bool IsFirstRun => isFirstRun;

		public UIController UIController
		{
			get
			{
				return uiController;
			}
			set
			{
				uiController = value;
			}
		}

		public bool ShouldShowGodModeButton => true;

		public bool IsGodModePurchased
		{
			get
			{
				if (!(GodModeRequiredProduct == null) && Products.IAPEnabled)
				{
					return Products.HasProduct(GodModeRequiredProduct);
				}
				return true;
			}
		}

		public bool RightClickTargetting
		{
			get
			{
				if (!OpenFrontier.IP.UI.UI.IsMobileDevice)
				{
					return !ForceLeftClickTargetting;
				}
				return false;
			}
		}

		public bool IsEmpireSeedingInSandboxEnabled
		{
			get
			{
				if (SandboxPlayerSettings.FactionSeedingNonBanditFactions)
				{
					return sandboxPlayerSettings.FactionSeedingMinEmpireFactions > 0;
				}
				return false;
			}
		}

		public double? DebounceEventSystemTime { get; set; }

		public float GetCameraDragRotateSensitivityMultiplier()
		{
			return CameraDragRotateSensitityMultiplierStandalone;
		}

		public void RestoreToDefaultDifficultyLevel()
		{
			CombatDifficultyLevel = DefaultCombatDifficultyLevel;
		}

		public static float GetTiltSensitivityNorm(float value)
		{
			return (value - 4f) / 20f;
		}

		public static float GetTiltSensitivityFromNorm(float normalizedValue)
		{
			return Mathf.Lerp(4f, 24f, normalizedValue);
		}

		public static float GetTouchTurnRadiusNorm(float value)
		{
			return (value - 30f) / 170f;
		}

		public static float GetTouchTurnRadiusFromNorm(float normalizedValue)
		{
			return Mathf.Lerp(30f, 200f, normalizedValue);
		}

		public void QuitToMainMenu()
		{
			Debug.Log("Moving to main menu");
			EngineASX engineASX = EngineASX.Instance;
			if (engineASX != null)
			{
				engineASX.SetCameraParticleSystemsActive(value: false);
				engineASX.ActiveSector = null;
			}
			UIController.Instance.QuickMsg.ClearMessages();
			ScenarioLoader.TryLoadScenario(MainMenuScenario);
		}

		public void SavePlayerPrefs()
		{
			PlayerPrefs.SetInt(PlayerOptionConstants.Video_FullScreenKey, Helper.BoolTo01(Screen.fullScreen));
			PlayerPrefs.SetFloat(MasterVolumeKey, AudioListener.volume);
			PlayerPrefs.SetFloat(MusicVolumeKey, (MusicPlayer != null) ? MusicPlayer.Volume : DefaultMusicVolume);
			PlayerPrefs.SetInt(PlayButtonSoundsKey, Helper.BoolTo01(PlayButtonSounds));
			PlayerPrefs.SetInt(PlayerOptionConstants.Audio_MissileLockSoundKey, Helper.BoolTo01(PlayerOptions.Audio_MissileLockSound));
			PlayerPrefs.SetInt(SpaceFogEnabledKey, Helper.BoolTo01(SpaceFogEnabled));
			PlayerPrefs.SetInt(RenderCloakedShipOutlinesKey, Helper.BoolTo01(RenderCloakedShipOutlinesEnabled));
			PlayerPrefs.SetInt(PlayerOptionConstants.Video_WormholeAnimationEnabledKey, Helper.BoolTo01(PlayerOptions.Video_WormholeAnimationEnabled));
			PlayerPrefs.SetInt(LightingFXEnabledKey, Helper.BoolTo01(LightingFXEnabled));
			PlayerPrefs.SetInt(PreferCameraLockKey, Helper.BoolTo01(PreferCameraLock));
			PlayerPrefs.SetInt(AutoTargetHostilesKey, Helper.BoolTo01(AutoTargetHostiles));
			PlayerPrefs.SetInt(TiltControlEnabledKey, Helper.BoolTo01(TiltControlEnabled));
			PlayerPrefs.SetInt(ForceTouchInputEnabledKey, Helper.BoolTo01(ForceTouchInputEnabled));
			PlayerPrefs.SetFloat(TiltSensitivityKey, TiltSensitivity);
			PlayerPrefs.SetFloat(TiltControlDefaultAccelerationXKey, TiltControlDefaultAccelerationX);
			PlayerPrefs.SetFloat(TouchTurnRadiusKey, TouchTurnRadius);
			PlayerPrefs.SetInt(AutoCloseInGameMenuKey, Helper.BoolTo01(AutoCloseInGameMenu));
			PlayerPrefs.SetInt(GraphicsQualityLevelKey, QualitySettings.GetQualityLevel());
			PlayerPrefs.SetInt(RespawnOnDeathKey, Helper.BoolTo01(RespawnOnDeath));
			PlayerPrefs.SetFloat(UISafeAreaTopKey, UISafeAreaTop);
			PlayerPrefs.SetFloat(UISafeAreaBottomKey, UISafeAreaBottom);
			PlayerPrefs.SetFloat(UISafeAreaLeftKey, UISafeAreaLeft);
			PlayerPrefs.SetFloat(UISafeAreaRightKey, UISafeAreaRight);
			PlayerPrefs.SetInt(PlayerOptionConstants.UI_AutoHideShieldsKey, Helper.BoolTo01(PlayerOptions.UI_AutoHideShields));
			PlayerPrefs.SetInt(PlayerOptionConstants.UI_ScreenSpaceShieldsKey, Helper.BoolTo01(PlayerOptions.UI_ScreenSpaceShields));
			PlayerPrefs.SetInt(PlayerOptionConstants.UI_ShowPointDefenceTurretsKey, Helper.BoolTo01(PlayerOptions.UI_ShowPointDefenceTurrets));
			PlayerPrefs.SetInt(PlayerOptionConstants.General_AllowFireAtAlliedKey, Helper.BoolTo01(PlayerOptions.General_AllowFireAtAllied));
			PlayerPrefs.SetInt(PlayerOptionConstants.General_AllowFireAtNeutralKey, Helper.BoolTo01(PlayerOptions.General_AllowFireAtNeutral));
			PlayerPrefs.SetInt(PlayerOptionConstants.General_CameraShakeOnHullHitKey, Helper.BoolTo01(PlayerOptions.General_CameraShakeOnHullHit));
			PlayerPrefs.SetInt(PlayerOptionConstants.General_CameraShakeOnShieldHitKey, Helper.BoolTo01(PlayerOptions.General_CameraShakeOnShieldHit));
			PlayerPrefs.SetFloat(CameraDragRotateSensitivityKey, CameraDragRotateSensitivity);
			// Open Frontier: save the user's raw fps CHOICE, never the
			// refresh-capped value, or the cap can never release.
			PlayerPrefs.SetInt(PlayerOptionConstants.Video_TargetFrameRateKey, UserTargetFrameRate);
			SaveAutoSaveSettings();
			PlayerPrefs.SetString(DefaultPilotNameKey, DefaultPilotName);
			PlayerPrefs.SetString(DefaultPilotTitleKey, DefaultPilotTitle);
			PlayerPrefs.SetString(DefaultFactionNameKey, DefaultFactionName);
			PlayerPrefs.SetString(DefaultFactionShortNameKey, DefaultFactionShortName);
			SaveCanvasHeight();
			SaveCombatDifficultyPreference();
			PlayerPrefs.Save();
		}

		public void SaveCombatDifficultyPreference()
		{
			PlayerPrefs.SetInt(Instance.CombatDifficultyLevelKey, CombatDifficultyLevelIndex);
		}

		public void SaveSandboxPlayerSettings()
		{
			PlayerPrefs.SetInt(SandboxPlayerSettings.GenerationNumSectorsIndexKey, SandboxPlayerSettings.GenerationNumSectorsIndex);
			PlayerPrefs.SetInt(SandboxPlayerSettings.DiscoverAllSectorsKey, Helper.BoolTo01(SandboxPlayerSettings.DiscoverAllSectors));
			PlayerPrefs.SetInt(SandboxPlayerSettings.DiscoverEverythingKey, Helper.BoolTo01(SandboxPlayerSettings.DiscoverEverything));
			PlayerPrefs.SetInt(SandboxPlayerSettings.FactionSeedingBanditFactionsKey, Helper.BoolTo01(SandboxPlayerSettings.FactionSeedingBanditFactions));
			PlayerPrefs.SetInt(SandboxPlayerSettings.FactionSeedingNonBanditFactionsKey, Helper.BoolTo01(SandboxPlayerSettings.FactionSeedingNonBanditFactions));
			PlayerPrefs.SetInt(SandboxPlayerSettings.FactionSeedingFreelancersKey, Helper.BoolTo01(SandboxPlayerSettings.FactionSeedingFreelancers));
			PlayerPrefs.SetFloat(SandboxPlayerSettings.FactionSeedingBanditPowerKey, SandboxPlayerSettings.FactionSeedingBanditPower);
			PlayerPrefs.SetFloat(SandboxPlayerSettings.FactionSeedingNonBanditPowerKey, SandboxPlayerSettings.FactionSeedingNonBanditPower);
			PlayerPrefs.SetInt(SandboxPlayerSettings.FactionSeedingMinEmpireFactionsKey, SandboxPlayerSettings.FactionSeedingMinEmpireFactions);
			PlayerPrefs.SetInt(SandboxPlayerSettings.FactionSeedingMaxEmpireFactionsKey, SandboxPlayerSettings.FactionSeedingMaxEmpireFactions);
			PlayerPrefs.SetInt(SandboxPlayerSettings.FactionSeedingGroupEmpireFactionsKey, Helper.BoolTo01(SandboxPlayerSettings.FactionSeedingGroupEmpireFactions));
			PlayerPrefs.SetFloat(sandboxPlayerSettings.FactionSeedingCargoVolumeKey, sandboxPlayerSettings.FactionSeedingCargoVolume);
			PlayerPrefs.SetInt(SandboxPlayerSettings.FactionSeedingMinEmpireExpansionKey, SandboxPlayerSettings.FactionSeedingMinEmpireExpansion);
			PlayerPrefs.SetInt(SandboxPlayerSettings.FactionSeedingMaxEmpireExpansionKey, SandboxPlayerSettings.FactionSeedingMaxEmpireExpansion);
			PlayerPrefs.SetInt(SandboxPlayerSettings.FactionSpawningKey, Helper.BoolTo01(SandboxPlayerSettings.FactionSpawning));
			PlayerPrefs.SetInt(SandboxPlayerSettings.RespawnOnDeathKey, Helper.BoolTo01(SandboxPlayerSettings.RespawnOnDeath));
			PlayerPrefs.SetInt(SandboxPlayerSettings.AllowTeleportingKey, Helper.BoolTo01(SandboxPlayerSettings.AllowTeleporting));
			PlayerPrefs.SetInt(SandboxPlayerSettings.PermadeathKey, Helper.BoolTo01(SandboxPlayerSettings.Permadeath));
			PlayerPrefs.SetInt(SandboxPlayerSettings.AsteroidRespawningEnabledKey, Helper.BoolTo01(SandboxPlayerSettings.AsteroidRespawningEnabled));
			PlayerPrefs.SetFloat(SandboxPlayerSettings.AsteroidRespawnTimeKey, SandboxPlayerSettings.AsteroidRespawnTime);
			PlayerPrefs.SetInt(SandboxPlayerSettings.SuperchargedBanditsEnabledKey, Helper.BoolTo01(SandboxPlayerSettings.SuperchargedBanditsEnabled));
			PlayerPrefs.SetInt(SandboxPlayerSettings.GenerationMaxWormholesKey, SandboxPlayerSettings.GenerationMaxWormholes);
			PlayerPrefs.SetFloat(SandboxPlayerSettings.GenerationSnakinessKey, SandboxPlayerSettings.GenerationSnakiness);
			PlayerPrefs.SetFloat(SandboxPlayerSettings.GenerationMinSectorSizeKey, SandboxPlayerSettings.GenerationMinSectorSize);
			PlayerPrefs.SetFloat(SandboxPlayerSettings.GenerationMaxSectorSizeKey, SandboxPlayerSettings.GenerationMaxSectorSize);
			PlayerPrefs.SetFloat(SandboxPlayerSettings.GenerationAsteroidDustCloudsProbabilityKey, SandboxPlayerSettings.GenerationAsteroidDustCloudsProbability);
		}

		public void SaveAutoSaveSettings()
		{
			PlayerPrefs.SetInt(AutoSaveSettings.AutoSaveOnDockingKey, Helper.BoolTo01(AutoSaveSettings.AutoSaveOnDocking));
			PlayerPrefs.SetInt(AutoSaveSettings.AutoSaveOnWormholeExitKey, Helper.BoolTo01(AutoSaveSettings.AutoSaveOnWormholeExit));
			PlayerPrefs.SetInt(AutoSaveSettings.AutoSaveAfterDurationKey, Helper.BoolTo01(AutoSaveSettings.AutoSaveAfterDuration));
			PlayerPrefs.SetInt(AutoSaveSettings.AutoSaveAfterDurationMinutesKey, AutoSaveSettings.AutoSaveAfterDurationMinutes);
		}

		public void SaveCanvasHeight()
		{
			PlayerPrefs.SetFloat(CanvasHeight01Key, CanvasHeight01);
		}

		public void ApplyResolution(FullScreenMode fullScreenMode)
		{
			Screen.SetResolution(Screen.width, Screen.height, fullScreenMode, Screen.currentResolution.refreshRate);
		}

		// Open Frontier: vsync-aware frame pacing, all platforms. When the
		// V-Sync option is on, the display decides; otherwise the Frame
		// rate limit row applies. Android: targetFrameRate paced by Swappy.
		// Desktop: real QualitySettings vSync.
		public void ApplyVSyncState()
		{
			bool vsyncOn = PlayerPrefs.GetInt(PlayerOptionConstants.Video_VSyncKey, 1) > 0;
#if UNITY_ANDROID && !UNITY_EDITOR
			ApplyMobileFrameRate();
#elif !UNITY_EDITOR
			QualitySettings.vSyncCount = vsyncOn ? 1 : 0;
			Application.targetFrameRate = vsyncOn ? -1 : (UserTargetFrameRate > 0 ? UserTargetFrameRate : -1);
#endif
		}

		// Open Frontier: vsync-aware frame pacing on mobile. When the
		// V-Sync option is on, the display decides (targetFrameRate -1,
		// paced by Swappy); otherwise the Frame rate limit row applies.
		public void ApplyMobileFrameRate()
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			if (PlayerPrefs.GetInt(PlayerOptionConstants.Video_VSyncKey, 1) > 0)
			{
				Application.targetFrameRate = -1;
				ApplyDisplayRefreshForTargetFps(999); // let the display run at its best
			}
			else
			{
				ApplyMobileFrameRateCap();
			}
#endif
		}

		// Open Frontier: the Frame rate limit row is the cap, full stop.
		// (No refresh-rate clamp - LTPO panels report dropped rates like
		// 30Hz, which pinned the cap and could never release.)
		public void ApplyMobileFrameRateCap()
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			// 0 means unset; -1 is the explicit Uncapped choice.
			Application.targetFrameRate = UserTargetFrameRate == 0 ? 60 : UserTargetFrameRate;
			ApplyDisplayRefreshForTargetFps(Application.targetFrameRate > 0 ? Application.targetFrameRate : 999);
#endif
		}

		// Open Frontier: caps above 60fps require the display MODE to run
		// above 60Hz, otherwise presentation stays at 60 no matter the
		// targetFrameRate. Switch to the smallest mode >= the target (or
		// the highest available) at the current resolution.
		public void ApplyDisplayRefreshForTargetFps(int targetFps)
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			OpenFrontier.AndroidDisplayMetrics.SetPreferredRefreshRate(targetFps);
#endif
		}

		public void ApplyResolution(bool fullScreen)
		{
			ApplyResolution(GetFullScreenMode(fullScreen));
		}

		public FullScreenMode GetFullScreenMode(bool fullScreen)
		{
			if (!fullScreen)
			{
				return FullScreenMode.Windowed;
			}
			return GameSettings.VideoSettings.DefaultFullScreenMode;
		}

		public void ApplyAllPlayerPrefs()
		{
			Debug.Log("GameController: Applying player preferences...", this);
			if (MusicPlayer != null)
			{
				MusicPlayer.Volume = PlayerPrefsHelper.SafeGetFloat(MusicVolumeKey, DefaultMusicVolume);
			}
			bool fullScreen = PlayerPrefs.GetInt(PlayerOptionConstants.Video_FullScreenKey, 1) > 0;
			ApplyResolution(fullScreen);
			AudioListener.volume = PlayerPrefsHelper.SafeGetFloat(MasterVolumeKey, 1f);
			PlayButtonSounds = PlayerPrefs.GetInt(PlayButtonSoundsKey, 1) > 0;
			PlayerOptions.Audio_MissileLockSound = PlayerPrefs.GetInt(PlayerOptionConstants.Audio_MissileLockSoundKey, 1) > 0;
			int defaultValue = CombatDifficultyLevels.IndexOf(DefaultCombatDifficultyLevel);
			CombatDifficultyLevelIndex = Mathf.Clamp(PlayerPrefs.GetInt(CombatDifficultyLevelKey, defaultValue), 0, CombatDifficultyLevels.Length);
			SpaceFogEnabled = PlayerPrefs.GetInt(SpaceFogEnabledKey, Helper.BoolToInt(val: true)) > 0;
			RenderCloakedShipOutlinesEnabled = PlayerPrefs.GetInt(RenderCloakedShipOutlinesKey, Helper.BoolToInt(val: true)) > 0;
			PlayerOptions.Video_WormholeAnimationEnabled = PlayerPrefs.GetInt(PlayerOptionConstants.Video_WormholeAnimationEnabledKey, Helper.BoolToInt(PlayerOptionConstants.Video_DefaultWormholeAnimationEnabled)) > 0;
			LightingFXEnabled = PlayerPrefs.GetInt(LightingFXEnabledKey, Helper.BoolToInt(val: true)) > 0;
			PreferCameraLock = PlayerPrefs.GetInt(PreferCameraLockKey, 0) > 0;
			AutoTargetHostiles = PlayerPrefs.GetInt(AutoTargetHostilesKey, Helper.BoolToInt(val: true)) > 0;
			AutoCloseInGameMenu = PlayerPrefs.GetInt(AutoCloseInGameMenuKey, 1) > 0;
			RespawnOnDeath = PlayerPrefs.GetInt(RespawnOnDeathKey, 1) > 0;
#if UNITY_ANDROID && !UNITY_EDITOR
			UserTargetFrameRate = PlayerPrefs.GetInt(PlayerOptionConstants.Video_TargetFrameRateKey, 60);
			// Open Frontier: vSyncCount is ignored by Android but the debug
			// overlay reads it; zero it so it does not mislead.
			QualitySettings.vSyncCount = 0;
			ApplyMobileFrameRate();
#else
			UserTargetFrameRate = PlayerPrefs.GetInt(PlayerOptionConstants.Video_TargetFrameRateKey, -1);
			ApplyVSyncState();
#endif
			ForceTouchInputEnabled = PlayerPrefs.GetInt(ForceTouchInputEnabledKey, Helper.BoolToInt(val: false)) > 0;
			TiltControlEnabled = PlayerPrefs.GetInt(TiltControlEnabledKey, Helper.BoolToInt(val: false)) > 0;
			TiltSensitivity = PlayerPrefsHelper.SafeGetFloat(TiltSensitivityKey, 18f);
			TouchTurnRadius = PlayerPrefsHelper.SafeGetFloat(TouchTurnRadiusKey, 100f);
			CanvasHeight01 = PlayerPrefsHelper.SafeGetFloat(CanvasHeight01Key, 0.8f);
			UISafeAreaTop = PlayerPrefsHelper.SafeGetFloat(UISafeAreaTopKey, 0f);
			UISafeAreaBottom = PlayerPrefsHelper.SafeGetFloat(UISafeAreaBottomKey, 0f);
			UISafeAreaLeft = PlayerPrefsHelper.SafeGetFloat(UISafeAreaLeftKey, 0f);
			UISafeAreaRight = PlayerPrefsHelper.SafeGetFloat(UISafeAreaRightKey, 0f);
			PlayerOptions.UI_AutoHideShields = PlayerPrefs.GetInt(PlayerOptionConstants.UI_AutoHideShieldsKey, 1) > 0;
			PlayerOptions.UI_ScreenSpaceShields = PlayerPrefs.GetInt(PlayerOptionConstants.UI_ScreenSpaceShieldsKey, 1) > 0;
			PlayerOptions.UI_ShowPointDefenceTurrets = PlayerPrefs.GetInt(PlayerOptionConstants.UI_ShowPointDefenceTurretsKey, 0) > 0;
			PlayerOptions.UI_ShowHudHeader = PlayerPrefs.GetInt(PlayerOptionConstants.UI_ShowHudHeaderKey, 1) > 0;
			PlayerOptions.UI_ShowHudComponents = PlayerPrefs.GetInt(PlayerOptionConstants.UI_ShowHudComponentsKey, 1) > 0;
			PlayerOptions.UI_ShowHudTargetPanel = PlayerPrefs.GetInt(PlayerOptionConstants.UI_ShowHudTargetPanelKey, 1) > 0;
			PlayerOptions.UI_ShowHudTargets = PlayerPrefs.GetInt(PlayerOptionConstants.UI_ShowHudTargetsKey, 1) > 0;
			PlayerOptions.UI_ShowHudControls = PlayerPrefs.GetInt(PlayerOptionConstants.UI_ShowHudControlsKey, 1) > 0;
			PlayerOptions.UI_ShowHudDialog = PlayerPrefs.GetInt(PlayerOptionConstants.UI_ShowHudDialogKey, 1) > 0;
			PlayerOptions.UI_ShowHudIndicators = PlayerPrefs.GetInt(PlayerOptionConstants.UI_ShowHudIndicatorsKey, 1) > 0;
			PlayerOptions.UI_ShowHudTargetIndicators = PlayerPrefs.GetInt(PlayerOptionConstants.UI_ShowHudTargetIndicatorsKey, 1) > 0;
			PlayerOptions.General_AllowFireAtAllied = PlayerPrefs.GetInt(PlayerOptionConstants.General_AllowFireAtAlliedKey, 0) > 0;
			PlayerOptions.General_AllowFireAtNeutral = PlayerPrefs.GetInt(PlayerOptionConstants.General_AllowFireAtNeutralKey, 1) > 0;
			PlayerOptions.General_CameraShakeOnHullHit = PlayerPrefs.GetInt(PlayerOptionConstants.General_CameraShakeOnHullHitKey, Helper.BoolTo01(PlayerOptionConstants.General_DefaultCameraShakeOnHullHit)) > 0;
			PlayerOptions.General_CameraShakeOnShieldHit = PlayerPrefs.GetInt(PlayerOptionConstants.General_CameraShakeOnShieldHitKey, Helper.BoolTo01(PlayerOptionConstants.General_DefaultCameraShakeOnHullHit)) > 0;
			HypersleepTimeMultiplier = PlayerPrefsHelper.SafeGetFloat(HypersleepTimeMultiplierKey, GameSettings.HypersleepSettings.DefaultTimeMultiplier);
			CameraDragRotateSensitivity = PlayerPrefsHelper.SafeGetFloat(CameraDragRotateSensitivityKey, 0.5f);
			ApplyUISafeArea();
			ApplyAutoSaveSettings();
			ApplySandboxSettings();
			ApplyQualityLevel();
			ScreenScaler.ScaleScreens();
			DefaultPilotName = PlayerPrefs.GetString(DefaultPilotNameKey, DefaultPilotName);
			DefaultPilotTitle = PlayerPrefs.GetString(DefaultPilotTitleKey, DefaultPilotTitle);
			DefaultFactionName = PlayerPrefs.GetString(DefaultFactionNameKey, DefaultFactionName);
			DefaultFactionShortName = PlayerPrefs.GetString(DefaultFactionShortNameKey, DefaultFactionShortName);
			EngineASX engineASX = EngineASX.Instance;
			if (engineASX != null)
			{
				engineASX.ApplyAllPlayerPrefs();
			}
		}

		private void ApplySandboxSettings()
		{
			int value = PlayerPrefs.GetInt(SandboxPlayerSettings.GenerationNumSectorsIndexKey, GameSettings.SandboxSettings.SectorCounts.IndexOf(GameSettings.SandboxSettings.DefaultSectorCount));
			SandboxPlayerSettings.GenerationNumSectorsIndex = Mathf.Clamp(value, 0, GameSettings.SandboxSettings.SectorCounts.Length - 1);
			SandboxPlayerSettings.DiscoverAllSectors = PlayerPrefs.GetInt(SandboxPlayerSettings.DiscoverAllSectorsKey, Helper.BoolTo01(GameSettings.SandboxSettings.DefaultDiscoverAllSectors)) > 0;
			SandboxPlayerSettings.DiscoverEverything = PlayerPrefs.GetInt(SandboxPlayerSettings.DiscoverEverythingKey, Helper.BoolTo01(GameSettings.SandboxSettings.DefaultDiscoverEverything)) > 0;
			SandboxPlayerSettings.FactionSpawning = PlayerPrefs.GetInt(SandboxPlayerSettings.FactionSpawningKey, Helper.BoolTo01(GameSettings.SandboxSettings.DefaultFactionSpawning)) > 0;
			SandboxPlayerSettings.RespawnOnDeath = PlayerPrefs.GetInt(SandboxPlayerSettings.RespawnOnDeathKey, Helper.BoolTo01(GameSettings.SandboxSettings.DefaultRespawnOnDeath)) > 0;
			SandboxPlayerSettings.AllowTeleporting = PlayerPrefs.GetInt(SandboxPlayerSettings.AllowTeleportingKey, Helper.BoolTo01(GameSettings.SandboxSettings.DefaultAllowTeleporting)) > 0;
			SandboxPlayerSettings.Permadeath = PlayerPrefs.GetInt(SandboxPlayerSettings.PermadeathKey, Helper.BoolTo01(GameSettings.SandboxSettings.DefaultPermadeath)) > 0;
			SandboxPlayerSettings.AsteroidRespawningEnabled = PlayerPrefs.GetInt(SandboxPlayerSettings.AsteroidRespawningEnabledKey, Helper.BoolTo01(GameSettings.SandboxSettings.DefaultAsteroidRespawningEnabled)) > 0;
			SandboxPlayerSettings.AsteroidRespawnTime = Mathf.Clamp01(PlayerPrefs.GetFloat(SandboxPlayerSettings.AsteroidRespawnTimeKey, GameSettings.SandboxSettings.DefaultAsteroidRespawnTime));
			SandboxPlayerSettings.SuperchargedBanditsEnabled = PlayerPrefs.GetInt(SandboxPlayerSettings.SuperchargedBanditsEnabledKey, Helper.BoolTo01(GameSettings.SandboxSettings.DefaultSuperchargedBanditsEnabled)) > 0;
			SandboxPlayerSettings.FactionSeedingBanditFactions = PlayerPrefs.GetInt(SandboxPlayerSettings.FactionSeedingBanditFactionsKey, Helper.BoolTo01(GameSettings.SandboxSettings.DefaultFactionSeedingBandits)) > 0;
			SandboxPlayerSettings.FactionSeedingNonBanditFactions = PlayerPrefs.GetInt(SandboxPlayerSettings.FactionSeedingNonBanditFactionsKey, Helper.BoolTo01(GameSettings.SandboxSettings.DefaultFactionSeedingNonBandits)) > 0;
			SandboxPlayerSettings.FactionSeedingFreelancers = PlayerPrefs.GetInt(SandboxPlayerSettings.FactionSeedingFreelancersKey, Helper.BoolTo01(GameSettings.SandboxSettings.DefaultFactionSeedingFreelancers)) > 0;
			SandboxPlayerSettings.FactionSeedingBanditPower = Mathf.Clamp01(PlayerPrefs.GetFloat(SandboxPlayerSettings.FactionSeedingBanditPowerKey, GameSettings.SandboxSettings.DefaultFactionSeedingBanditPower));
			SandboxPlayerSettings.FactionSeedingNonBanditPower = Mathf.Clamp01(PlayerPrefs.GetFloat(SandboxPlayerSettings.FactionSeedingNonBanditPowerKey, GameSettings.SandboxSettings.DefaultFactionSeedingNonBanditPower));
			sandboxPlayerSettings.FactionSeedingMinEmpireExpansion = PlayerPrefs.GetInt(SandboxPlayerSettings.FactionSeedingMinEmpireExpansionKey, GameSettings.SandboxSettings.DefaultFactionSeedingMinEmpireExpansion);
			sandboxPlayerSettings.FactionSeedingMaxEmpireExpansion = PlayerPrefs.GetInt(SandboxPlayerSettings.FactionSeedingMaxEmpireExpansionKey, GameSettings.SandboxSettings.DefaultFactionSeedingMaxEmpireExpansion);
			sandboxPlayerSettings.FactionSeedingGroupEmpireFactions = PlayerPrefs.GetInt(SandboxPlayerSettings.FactionSeedingGroupEmpireFactionsKey, Helper.BoolTo01(GameSettings.SandboxSettings.DefaultFactionSeedingGroupEmpireFactions)) > 0;
			sandboxPlayerSettings.FactionSeedingMinEmpireFactions = PlayerPrefs.GetInt(SandboxPlayerSettings.FactionSeedingMinEmpireFactionsKey, GameSettings.SandboxSettings.DefaultFactionSeedingMinEmpireFactions);
			sandboxPlayerSettings.FactionSeedingMaxEmpireFactions = PlayerPrefs.GetInt(SandboxPlayerSettings.FactionSeedingMaxEmpireFactionsKey, GameSettings.SandboxSettings.DefaultFactionSeedingMaxEmpireFactions);
			sandboxPlayerSettings.FactionSeedingCargoVolume = PlayerPrefs.GetFloat(sandboxPlayerSettings.FactionSeedingCargoVolumeKey, GameSettings.SandboxSettings.DefaultFactionSeedingCargoVolume);
			int generationMaxWormholes = PlayerPrefs.GetInt(SandboxPlayerSettings.GenerationMaxWormholesKey, GameSettings.SandboxSettings.GenerationDefaultMaxWormholes);
			sandboxPlayerSettings.GenerationMaxWormholes = generationMaxWormholes;
			sandboxPlayerSettings.GenerationSnakiness = PlayerPrefs.GetFloat(sandboxPlayerSettings.GenerationSnakinessKey, GameSettings.SandboxSettings.GenerationDefaultSnakiness);
			sandboxPlayerSettings.GenerationMinSectorSize = PlayerPrefs.GetFloat(sandboxPlayerSettings.GenerationMinSectorSizeKey, GameSettings.SandboxSettings.GenerationDefaultMinSectorSize);
			sandboxPlayerSettings.GenerationMaxSectorSize = PlayerPrefs.GetFloat(sandboxPlayerSettings.GenerationMaxSectorSizeKey, GameSettings.SandboxSettings.GenerationDefaultMaxSectorSize);
			sandboxPlayerSettings.GenerationAsteroidDustCloudsProbability = PlayerPrefs.GetFloat(sandboxPlayerSettings.GenerationAsteroidDustCloudsProbabilityKey, GameSettings.SandboxSettings.GenerationDefaultAsteroidDustCloudsProbability);
		}

		public void ResetSandboxPlayerSettings()
		{
			SandboxSettings sandboxSettings = instance.GameSettings.SandboxSettings;
			sandboxPlayerSettings.GenerationNumSectorsIndex = sandboxSettings.SectorCounts.IndexOf(sandboxSettings.DefaultSectorCount);
			sandboxPlayerSettings.GenerationMaxWormholes = sandboxSettings.GenerationDefaultMaxWormholes;
			sandboxPlayerSettings.DiscoverAllSectors = sandboxSettings.DefaultDiscoverAllSectors;
			sandboxPlayerSettings.DiscoverEverything = sandboxSettings.DefaultDiscoverAllSectors;
			sandboxPlayerSettings.GenerationSnakiness = sandboxSettings.GenerationDefaultSnakiness;
			sandboxPlayerSettings.GenerationMinSectorSize = sandboxSettings.GenerationDefaultMinSectorSize;
			sandboxPlayerSettings.GenerationMaxSectorSize = sandboxSettings.GenerationDefaultMaxSectorSize;
			sandboxPlayerSettings.GenerationAsteroidDustCloudsProbability = sandboxSettings.GenerationDefaultAsteroidDustCloudsProbability;
			sandboxPlayerSettings.FactionSeedingBanditFactions = sandboxSettings.DefaultFactionSeedingBandits;
			sandboxPlayerSettings.FactionSeedingNonBanditFactions = sandboxSettings.DefaultFactionSeedingNonBandits;
			sandboxPlayerSettings.FactionSeedingFreelancers = sandboxSettings.DefaultFactionSeedingFreelancers;
			sandboxPlayerSettings.FactionSeedingMinEmpireExpansion = sandboxSettings.DefaultFactionSeedingMinEmpireExpansion;
			sandboxPlayerSettings.FactionSeedingMaxEmpireExpansion = sandboxSettings.DefaultFactionSeedingMaxEmpireExpansion;
			sandboxPlayerSettings.FactionSeedingMinEmpireFactions = sandboxSettings.DefaultFactionSeedingMinEmpireFactions;
			sandboxPlayerSettings.FactionSeedingMaxEmpireFactions = sandboxSettings.DefaultFactionSeedingMaxEmpireFactions;
			sandboxPlayerSettings.FactionSeedingGroupEmpireFactions = sandboxSettings.DefaultFactionSeedingGroupEmpireFactions;
			sandboxPlayerSettings.FactionSeedingBanditPower = sandboxSettings.DefaultFactionSeedingBanditPower;
			sandboxPlayerSettings.FactionSeedingNonBanditPower = sandboxSettings.DefaultFactionSeedingNonBanditPower;
			sandboxPlayerSettings.FactionSeedingCargoVolume = sandboxSettings.DefaultFactionSeedingCargoVolume;
			sandboxPlayerSettings.FactionSpawning = sandboxSettings.DefaultFactionSpawning;
			sandboxPlayerSettings.RespawnOnDeath = sandboxSettings.DefaultRespawnOnDeath;
			sandboxPlayerSettings.Permadeath = sandboxSettings.DefaultPermadeath;
			sandboxPlayerSettings.AllowTeleporting = sandboxSettings.DefaultAllowTeleporting;
			sandboxPlayerSettings.AsteroidRespawningEnabled = sandboxSettings.DefaultAsteroidRespawningEnabled;
			sandboxPlayerSettings.AsteroidRespawnTime = sandboxSettings.DefaultAsteroidRespawnTime;
			sandboxPlayerSettings.SuperchargedBanditsEnabled = sandboxSettings.DefaultSuperchargedBanditsEnabled;
		}

		private void ApplyAutoSaveSettings()
		{
			AutoSaveSettings.AutoSaveOnDocking = PlayerPrefs.GetInt(AutoSaveSettings.AutoSaveOnDockingKey, Helper.BoolTo01(GameSettings.AutoSaveSettings.AutoSaveOnDocking)) > 0;
			AutoSaveSettings.AutoSaveOnWormholeExit = PlayerPrefs.GetInt(AutoSaveSettings.AutoSaveOnWormholeExitKey, Helper.BoolTo01(GameSettings.AutoSaveSettings.AutoSaveOnWormholeExit)) > 0;
			AutoSaveSettings.AutoSaveAfterDuration = PlayerPrefs.GetInt(AutoSaveSettings.AutoSaveAfterDurationKey, Helper.BoolTo01(GameSettings.AutoSaveSettings.AutoSaveAfterDuration)) > 0;
			AutoSaveSettings.AutoSaveAfterDurationMinutes = PlayerPrefs.GetInt(AutoSaveSettings.AutoSaveAfterDurationMinutesKey, GameSettings.AutoSaveSettings.AutoSaveAfterDurationMinutes);
		}

		private void ApplyQualityLevel()
		{
			int num = PlayerPrefs.GetInt(GraphicsQualityLevelKey, DefaultQualityLevel);
			if (num != QualitySettings.GetQualityLevel())
			{
				int num2 = QualitySettings.names.Length;
				int num3 = Mathf.Clamp(num, 0, num2 - 1);
				QualitySettings.SetQualityLevel(num3, applyExpensiveChanges: true);
				instance.CurrentQualityLevel = num3;
			}
		}

		public ScenarioInfo GetScenarioInfoById(int id)
		{
			ScenarioInfo value = null;
			if (scenarioInfoMap.TryGetValue(id, out value))
			{
				return value;
			}
			return null;
		}

		public SkirmishScenarioParams[] GetInstantActionsShipSets()
		{
			SkirmishScenarioParams[] componentsInChildren = GetComponentsInChildren<SkirmishScenarioParams>();
			LogOutSkirmishDifficulty(componentsInChildren);
			return componentsInChildren;
		}

		private static void LogOutSkirmishDifficulty(SkirmishScenarioParams[] skirmishParams)
		{
		}

		private void Awake()
		{
			CurrentQualityLevel = QualitySettings.GetQualityLevel();
			autoSaveSettings = UnityEngine.Object.Instantiate(GameSettings.AutoSaveSettings);
			autoSaveSettings.name = "AutoSaveSettings";
			autoSaveSettings.transform.SetParent(transform);
			sandboxPlayerSettings = UnityEngine.Object.Instantiate(GameSettings.SandboxPlayerSettings);
			sandboxPlayerSettings.name = "SandboxPlayerSettings";
			sandboxPlayerSettings.transform.SetParent(transform);
			InitialiseZPrefs();
			if (PurchaseBridge.Purchaser == null)
			{
				PurchaseBridge.InitialisePurchaser();
			}
			LoadUIController();
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			instance = this;
			RefreshIsFirstRun();
			DetermineDefaultCanvasHeight();
			ApplyAllPlayerPrefs();
			LoadUnitClasses();
			LoadUniverseSectors();
			LoadScenarios();
			CreateCamera();
			TextInfo textInfo = new CultureInfo("en-US", useUserOverride: false).TextInfo;
			GreekAlphabet = IOHelper.ReadAndSplitTextLines(GreekAlphabetFile);
			GreekAlphabetTitleCase = GreekAlphabet.Select((string e) => textInfo.ToTitleCase(e)).ToArray();
			NatoAlphabet = IOHelper.ReadAndSplitTextLines(NatoAlphabetFile);
			NatoAlphabetTitleCase = NatoAlphabet.Select((string e) => textInfo.ToTitleCase(e)).ToArray();
			GameData.Init();
			AvatarController.Init();
			CustomSectorNames = IOHelper.ReadAndSplitTextLines(CustomSectorNamesTextAsset).ToList();
			EngineIO.TryCreateSaveDirectoryIfRequired();
			CustomUnitVariantIO.TryCreateSaveDirectoryIfRequired();
		}

		private void DetermineDefaultCanvasHeight()
		{
		}

		private void RefreshIsFirstRun()
		{
			isFirstRun = DetermineIsFirstRun();
			PlayerPrefs.SetInt("first_run", 0);
		}

		public bool DetermineIsFirstRun()
		{
			try
			{
				if (SaveGameUtilities.CanFindHeaders(out var _))
				{
					return false;
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
			if (PlayerPrefs.HasKey("first_run"))
			{
				return false;
			}
			return true;
		}

		private void LoadUIController()
		{
			SceneManager.LoadScene(ScreenNames.UIControllerScene, LoadSceneMode.Additive);
		}

		private static void InitialiseZPrefs()
		{
			ZPlayerPrefs.Initialize("absji8u2wrpjfsd8f", SystemInfo.deviceUniqueIdentifier);
		}

		private void LoadUniverseSectors()
		{
			allSectors = EngineASX.LoadSectorPrefabs().ToArray();
		}

		private void LoadUnitClasses()
		{
			loadedUnitClasses = EngineASX.LoadUnitClasses().ToArray();
		}

		private void ValidateLoadedShips()
		{
			UnitClass[] array = loadedUnitClasses;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Validate();
			}
		}

		private void LoadScenarios()
		{
			foreach (ScenarioInfo item in UnityObjectHelper.LoadAll<ScenarioInfo>("Prefabs/WorldData/ScenarioInfos"))
			{
				if (scenarioInfoMap.ContainsKey(item.UniqueId))
				{
					Debug.LogError("Found duplicated scenario id: " + item.UniqueId, item);
				}
				else
				{
					scenarioInfoMap.Add(item.UniqueId, item);
				}
			}
		}

		private void CreateCamera()
		{
			mainCamera = UnityObjectHelper.InstantiateAndGetComponent(MainCameraPrefab);
			mainCameraAudioSource = mainCamera.GetComponent<AudioSource>();
			if (mainCameraAudioSource == null)
			{
				mainCameraAudioSource = mainCamera.gameObject.AddComponent<AudioSource>();
			}
		}

		private void Update()
		{
			if (DebounceEventSystemTime.HasValue && Time.realtimeSinceStartupAsDouble > DebounceEventSystemTime && Input.touchCount == 0)
			{
				EventSystem.enabled = true;
				DebounceEventSystemTime = null;
			}
		}

		private void LateUpdate()
		{
			if (mainCamera != null)
			{
				if (Time.deltaTime == 0f)
				{
					MainCameraVelocity = Vector3.zero;
				}
				else
				{
					Vector3 vector = mainCamera.transform.position - lastMainCameraPosition;
					MainCameraVelocity = vector / Time.deltaTime;
				}
				lastMainCameraPosition = mainCamera.transform.position;
			}
			if (EngineASX.LoadedAndReady && EngineASX.Instance.ActiveSector != null)
			{
				if (SkyboxCamera != null)
				{
					SkyboxCamera.transform.position = EngineASX.Instance.ActiveSector.transform.position;
					SkyboxCamera.transform.rotation = EngineASX.Instance.ActiveSector.BackgroundRotation * MainCamera.transform.rotation;
				}
				if (PlanetCamera != null)
				{
					PlanetCamera.transform.position = MainCamera.transform.position;
					PlanetCamera.transform.rotation = MainCamera.transform.rotation;
				}
			}
			RealDeltaTime = Time.realtimeSinceStartupAsDouble - LastRealTime;
			LastRealTime = Time.realtimeSinceStartupAsDouble;
		}

		public void ApplyUISafeArea()
		{
			if (!(UIController.Instance != null) || !(UIController.Instance.ScreenNavigator != null))
			{
				return;
			}
			foreach (ScreenBase loadedScreen in UIController.Instance.ScreenNavigator.LoadedScreens)
			{
				foreach (CanvasGroup canvasGroup in loadedScreen.CanvasGroups)
				{
					CustomSafeArea componentInChildren = canvasGroup.GetComponentInChildren<CustomSafeArea>();
					if (componentInChildren != null)
					{
						componentInChildren.Refresh();
					}
				}
			}
		}

		public void SetUISafeAreaTopFrom01(float value)
		{
			UISafeAreaTop = Mathf.Lerp(MinUIScaleValue, MaxUIScaleValue, value);
		}

		public void SetUISafeAreaBottomFrom01(float value)
		{
			UISafeAreaBottom = Mathf.Lerp(MinUIScaleValue, MaxUIScaleValue, value);
		}

		public void SetUISafeAreaLeftFrom01(float value)
		{
			UISafeAreaLeft = Mathf.Lerp(MinUIScaleValue, MaxUIScaleValue, value);
		}

		public void SetUISafeAreaRightFrom01(float value)
		{
			UISafeAreaRight = Mathf.Lerp(MinUIScaleValue, MaxUIScaleValue, value);
		}

		public void SaveHypersleepTimeMultiplier()
		{
			PlayerPrefs.SetFloat(HypersleepTimeMultiplierKey, HypersleepTimeMultiplier);
		}

		public void DebouceEventSystem(float delay)
		{
			DebounceEventSystemTime = Time.realtimeSinceStartupAsDouble + (double)delay;
			EventSystem.enabled = false;
		}
	}
}
