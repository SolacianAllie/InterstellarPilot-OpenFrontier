using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Sandbox;
using OpenFrontier.IP.Engine.WorldGeneration;
using OpenFrontier.IP.Engine.WorldGeneration.Models;
using OpenFrontier.IP.Engine.WorldSeeding;
using OpenFrontier.IP.UI.Screens.EnterNumber;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.UniverseSandboxSettings
{
	public class UniverseSandboxSettingsScreen : ScreenBase
	{
		public int MaxWormholesMinValue = 3;

		public int MaxWormholesMaxValue = 7;

		public float SectorSizeLowerValue = 0.4f;

		public float SectorSizeUpperValue = 1f;

		public ScenarioInfo SandboxScenarioInfo;

		public Button NextButton;

		public Button PreviewButton;

		public Slider NumSectorsSlider;

		public Slider SnakinessSlider;

		public Slider MinSectorSizeSlider;

		public Slider MaxSectorSizeSlider;

		public Slider MaxWormholesSlider;

		public Slider AsteroidDustCloudsSlider;

		public Toggle DiscoverAllSectorsToggle;

		public Toggle DiscoverEverythingToggle;

		public Toggle FactionSeedingToggle;

		public Toggle BanditFactionSeedingToggle;

		public Toggle FreelancerSeedingToggle;

		public Toggle FactionSpawningToggle;

		public Toggle RespawnOnDeathToggle;

		public Toggle AllowTeleportingToggle;

		public Toggle PermadeathToggle;

		public Toggle AsteroidRespawningToggle;

		public Slider AsteroidRespawnTimeSlider;

		public Toggle SuperChargedBanditsToggle;

		public Slider FactionSeedingBanditPowerSlider;

		public Slider FactionSeedingNonBanditPowerSlider;

		public Slider FactionSeedingMinEmpireFactionsSlider;

		public Slider FactionSeedingMaxEmpireFactionsSlider;

		public Slider FactionSeedingMinEmpireExpansionSlider;

		public Slider FactionSeedingMaxEmpireExpansionSlider;

		public Slider FactionSeedingCargoVolumeSlider;

		public Toggle GroupEmpireFactionsToggle;

		public Text SectorCountLabel;

		public Text MaxWormholesLabel;

		private WorldBlueprint worldBlueprint;

		private WorldSeedSettings seedSettings;

		public Button GenerationSeedButton;

		public Button RestoreDefaultsButton;

		protected override void awake()
		{
			base.awake();
			NextButton.onClick.AddListener(Next);
			PreviewButton.onClick.AddListener(Preview);
			GenerationSeedButton.onClick.AddListener(GenerationSeedButtonClick);
			seedSettings = CreateSeedSettings();
			Object.DontDestroyOnLoad(seedSettings.gameObject);
			RestoreDefaultsButton.onClick.AddListener(RestoreDefaultsButtonClick);
		}

		protected override bool onNavigatingBack()
		{
			if (base.onNavigatingBack())
			{
				if (seedSettings != null)
				{
					Object.Destroy(seedSettings.gameObject);
				}
				return true;
			}
			return false;
		}

		protected override void start()
		{
			base.start();
			InitAllInputs();
			NumSectorsSlider.onValueChanged.AddListener(NumSectorsSliderValueChanged);
			SnakinessSlider.onValueChanged.AddListener(SnakinessSliderValueChanged);
			MaxWormholesSlider.onValueChanged.AddListener(MaxWormholesSliderValueChanged);
			MinSectorSizeSlider.onValueChanged.AddListener(MinSectorSizeSliderValueChanged);
			MaxSectorSizeSlider.onValueChanged.AddListener(MaxSectorSizeSliderValueChanged);
			FactionSeedingToggle.onValueChanged.AddListener((bool value) =>
			{
				RefreshFactionSeedingDependents();
			});
			BanditFactionSeedingToggle.onValueChanged.AddListener((bool value) =>
			{
				RefreshFactionSeedingBanditPowerSliderVisible();
				RefreshBanditSuperChargeToggleVisible();
			});
			DiscoverEverythingToggle.onValueChanged.AddListener((bool value) =>
			{
				RefreshDiscoverSectorsToggleActive();
			});
			PermadeathToggle.onValueChanged.AddListener((bool value) =>
			{
				RefreshRespawnOnDeathToggleActive();
			});
			AsteroidRespawningToggle.onValueChanged.AddListener((bool value) =>
			{
				RefreshAsteroidRespawnTimeSliderActive();
			});
			FactionSeedingMinEmpireExpansionSlider.onValueChanged.AddListener((float value) =>
			{
				if (FactionSeedingMaxEmpireExpansionSlider.value < FactionSeedingMinEmpireExpansionSlider.value)
				{
					FactionSeedingMaxEmpireExpansionSlider.value = FactionSeedingMinEmpireExpansionSlider.value;
				}
			});
			FactionSeedingMaxEmpireExpansionSlider.onValueChanged.AddListener((float value) =>
			{
				if (FactionSeedingMinEmpireExpansionSlider.value > FactionSeedingMaxEmpireExpansionSlider.value)
				{
					FactionSeedingMinEmpireExpansionSlider.value = FactionSeedingMaxEmpireExpansionSlider.value;
				}
			});
			FactionSeedingMinEmpireFactionsSlider.onValueChanged.AddListener((float value) =>
			{
				if (FactionSeedingMaxEmpireFactionsSlider.value < FactionSeedingMinEmpireFactionsSlider.value)
				{
					FactionSeedingMaxEmpireFactionsSlider.value = FactionSeedingMinEmpireFactionsSlider.value;
				}
			});
			FactionSeedingMaxEmpireFactionsSlider.onValueChanged.AddListener((float value) =>
			{
				if (FactionSeedingMinEmpireFactionsSlider.value > FactionSeedingMaxEmpireFactionsSlider.value)
				{
					FactionSeedingMinEmpireFactionsSlider.value = FactionSeedingMaxEmpireFactionsSlider.value;
				}
			});
			if (worldBlueprint == null)
			{
				seedSettings.WorldGeneratorSettings.SeedValue = Random.Range(1, 1000000);
			}
		}

		private void RefreshRespawnOnDeathToggleActive()
		{
			RespawnOnDeathToggle.gameObject.SetActive(!PermadeathToggle.isOn);
		}

		private void RefreshAsteroidRespawnTimeSliderActive()
		{
			AsteroidRespawnTimeSlider.gameObject.SetActive(AsteroidRespawningToggle.isOn);
		}

		private void RefreshFactionSeedingBanditPowerSliderVisible()
		{
			FactionSeedingBanditPowerSlider.gameObject.SetActive(BanditFactionSeedingToggle.isOn);
		}

		private void RefreshBanditSuperChargeToggleVisible()
		{
			SuperChargedBanditsToggle.gameObject.SetActive(BanditFactionSeedingToggle.isOn);
		}

		private void RefreshFactionSeedingDependents()
		{
			FactionSeedingNonBanditPowerSlider.gameObject.SetActive(FactionSeedingToggle.isOn);
			FactionSeedingMaxEmpireExpansionSlider.gameObject.SetActive(FactionSeedingToggle.isOn);
			FactionSeedingMinEmpireExpansionSlider.gameObject.SetActive(FactionSeedingToggle.isOn);
			FactionSeedingMinEmpireFactionsSlider.gameObject.SetActive(FactionSeedingToggle.isOn);
			FactionSeedingMaxEmpireFactionsSlider.gameObject.SetActive(FactionSeedingToggle.isOn);
			GroupEmpireFactionsToggle.gameObject.SetActive(FactionSeedingToggle.isOn);
			FreelancerSeedingToggle.gameObject.SetActive(FactionSeedingToggle.isOn);
			if (!FactionSeedingToggle.isOn)
			{
				FreelancerSeedingToggle.isOn = false;
			}
		}

		private void InitAllInputs()
		{
			InitSectorCountSlider();
			InitIntelCheckboxes();
			InitFactionSeedingOptions();
			FactionSpawningToggle.isOn = GameController.Instance.SandboxPlayerSettings.FactionSpawning;
			AllowTeleportingToggle.isOn = GameController.Instance.SandboxPlayerSettings.AllowTeleporting;
			RespawnOnDeathToggle.isOn = GameController.Instance.SandboxPlayerSettings.RespawnOnDeath;
			PermadeathToggle.isOn = GameController.Instance.SandboxPlayerSettings.Permadeath;
			AsteroidRespawningToggle.isOn = GameController.Instance.SandboxPlayerSettings.AsteroidRespawningEnabled;
			SuperChargedBanditsToggle.isOn = GameController.Instance.SandboxPlayerSettings.SuperchargedBanditsEnabled;
			InitAsteroidRespawnTimeSlider();
			InitSnakinessSlider();
			InitMaxWormholesSlider();
			InitMinSectorSizeSlider();
			InitMaxSectorSizeSlider();
			InitAsteroidDustCloudsSlider();
			RefreshSectorCountLabel();
			RefreshMaxWormholesLabel();
			RefreshFactionSeedingBanditPowerSliderVisible();
			RefreshBanditSuperChargeToggleVisible();
			RefreshFactionSeedingDependents();
			RefreshRespawnOnDeathToggleActive();
			RefreshAsteroidRespawnTimeSliderActive();
			RefreshDiscoverSectorsToggleActive();
		}

		private void RefreshDiscoverSectorsToggleActive()
		{
			DiscoverAllSectorsToggle.gameObject.SetActive(!DiscoverEverythingToggle.isOn);
		}

		private void RestoreDefaultsButtonClick()
		{
			GameController.Instance.ResetSandboxPlayerSettings();
			InitAllInputs();
		}

		public void Next()
		{
			PopulateSeedSettings(seedSettings);
			GameController.Instance.WorldBlueprintToGenerate = worldBlueprint;
			UIController.Instance.ScreenNavigator.ShowUniverseGameTypeScreen(SandboxScenarioInfo, seedSettings);
		}

		private void PopulateSeedSettings(WorldSeedSettings seedSettings)
		{
			SandboxSectorCount currentSectorCountSetting = GetCurrentSectorCountSetting();
			seedSettings.WorldGeneratorSettings.MinNumScenes = currentSectorCountSetting.MinCount;
			seedSettings.WorldGeneratorSettings.MaxNumScenes = currentSectorCountSetting.MaxCount;
			seedSettings.WorldGeneratorSettings.MaxConnections = GetCurrentMaxWormholesSetting();
			seedSettings.WorldGeneratorSettings.Snakiness = SnakinessSlider.value;
			seedSettings.WorldGeneratorSettings.MinGateDistanceMultiplier = Mathf.Lerp(SectorSizeLowerValue, SectorSizeUpperValue, MinSectorSizeSlider.value);
			seedSettings.WorldGeneratorSettings.MaxGateDistanceMultiplier = Mathf.Lerp(SectorSizeLowerValue, SectorSizeUpperValue, MaxSectorSizeSlider.value);
			seedSettings.DiscoverySettings.DiscoverAllNormalJumpGates = DiscoverAllSectorsToggle.isOn;
			seedSettings.DiscoverySettings.DiscoverEverything = DiscoverEverythingToggle.isOn;
			seedSettings.FactionSeederSettings.SeedNonBanditFactions = FactionSeedingToggle.isOn;
			seedSettings.FactionSeederSettings.SeedBanditFactions = BanditFactionSeedingToggle.isOn;
			seedSettings.FactionSeederSettings.SeedFreelancers = FreelancerSeedingToggle.isOn;
			seedSettings.FactionSeederSettings.BanditPower01 = FactionSeedingBanditPowerSlider.value;
			seedSettings.FactionSeederSettings.NonBanditPower01 = FactionSeedingNonBanditPowerSlider.value;
			seedSettings.WorldTraderCargoSeederSettings.InitialTraderCargoPower01 = FactionSeedingCargoVolumeSlider.value;
			seedSettings.CreateEmpireFactionsSeederSettings.MinExpansion = FactionSeedingMinEmpireExpansionSlider.value;
			seedSettings.CreateEmpireFactionsSeederSettings.MaxExpansion = FactionSeedingMaxEmpireExpansionSlider.value;
			seedSettings.CreateEmpireFactionsSeederSettings.GroupEmpireFactions = GroupEmpireFactionsToggle.isOn;
			seedSettings.CreateEmpireFactionsSeederSettings.MinFactions = Mathf.RoundToInt(FactionSeedingMaxEmpireFactionsSlider.value);
			seedSettings.CreateEmpireFactionsSeederSettings.MaxFactions = Mathf.RoundToInt(FactionSeedingMaxEmpireFactionsSlider.value);
			seedSettings.ScenarioOptions.FactionSpawningEnabled = FactionSpawningToggle.isOn;
			seedSettings.ScenarioOptions.RespawnOnDeath = ((!RespawnOnDeathToggle.isOn) ? RespawnOnDeathPreference.DontRespawn : RespawnOnDeathPreference.NotSet);
			seedSettings.ScenarioOptions.AllowTeleporting = AllowTeleportingToggle.isOn;
			seedSettings.ScenarioOptions.Permadeath = PermadeathToggle.isOn;
			seedSettings.ScenarioOptions.AsteroidRespawningEnabled = AsteroidRespawningToggle.isOn;
			seedSettings.ScenarioOptions.AsteroidRespawnTime = AsteroidRespawnTimeSlider.value;
			seedSettings.FactionSeederSettings.SuperchargedBanditsEnabled = SuperChargedBanditsToggle.isOn;
			seedSettings.CreateAsteroidClustersSeederSettings.ProbabilityOfGeneratingGasCloud = AsteroidDustCloudsSlider.value;
		}

		private int GetCurrentMaxWormholesSetting()
		{
			return Mathf.RoundToInt(MaxWormholesSlider.value);
		}

		protected override void onNotCurrentPanel()
		{
			base.onNotCurrentPanel();
			SavePreferences();
		}

		private void SavePreferences()
		{
			GameController.Instance.SandboxPlayerSettings.FactionSeedingBanditFactions = BanditFactionSeedingToggle.isOn;
			GameController.Instance.SandboxPlayerSettings.FactionSeedingNonBanditFactions = FactionSeedingToggle.isOn;
			GameController.Instance.SandboxPlayerSettings.FactionSeedingFreelancers = FreelancerSeedingToggle.isOn;
			GameController.Instance.SandboxPlayerSettings.FactionSeedingNonBanditPower = FactionSeedingNonBanditPowerSlider.value;
			GameController.Instance.SandboxPlayerSettings.FactionSeedingBanditPower = FactionSeedingBanditPowerSlider.value;
			GameController.Instance.SandboxPlayerSettings.FactionSeedingMinEmpireFactions = Mathf.RoundToInt(FactionSeedingMinEmpireFactionsSlider.value);
			GameController.Instance.SandboxPlayerSettings.FactionSeedingMaxEmpireFactions = Mathf.RoundToInt(FactionSeedingMaxEmpireFactionsSlider.value);
			GameController.Instance.SandboxPlayerSettings.FactionSeedingMinEmpireExpansion = Mathf.RoundToInt(FactionSeedingMinEmpireExpansionSlider.value);
			GameController.Instance.SandboxPlayerSettings.FactionSeedingMaxEmpireExpansion = Mathf.RoundToInt(FactionSeedingMaxEmpireExpansionSlider.value);
			GameController.Instance.SandboxPlayerSettings.FactionSeedingGroupEmpireFactions = GroupEmpireFactionsToggle.isOn;
			GameController.Instance.SandboxPlayerSettings.FactionSeedingCargoVolume = FactionSeedingCargoVolumeSlider.value;
			GameController.Instance.SandboxPlayerSettings.FactionSpawning = FactionSpawningToggle.isOn;
			GameController.Instance.SandboxPlayerSettings.RespawnOnDeath = RespawnOnDeathToggle.isOn;
			GameController.Instance.SandboxPlayerSettings.AllowTeleporting = AllowTeleportingToggle.isOn;
			GameController.Instance.SandboxPlayerSettings.Permadeath = PermadeathToggle.isOn;
			GameController.Instance.SandboxPlayerSettings.AsteroidRespawningEnabled = AsteroidRespawningToggle.isOn;
			GameController.Instance.SandboxPlayerSettings.AsteroidRespawnTime = AsteroidRespawnTimeSlider.value;
			GameController.Instance.SandboxPlayerSettings.SuperchargedBanditsEnabled = SuperChargedBanditsToggle.isOn;
			GameController.Instance.SandboxPlayerSettings.DiscoverEverything = DiscoverEverythingToggle.isOn;
			GameController.Instance.SandboxPlayerSettings.DiscoverAllSectors = DiscoverAllSectorsToggle.isOn;
			GameController.Instance.SandboxPlayerSettings.GenerationNumSectorsIndex = (int)NumSectorsSlider.value;
			GameController.Instance.SandboxPlayerSettings.GenerationMaxWormholes = Mathf.RoundToInt(MaxWormholesSlider.value);
			GameController.Instance.SandboxPlayerSettings.GenerationSnakiness = SnakinessSlider.value;
			GameController.Instance.SandboxPlayerSettings.GenerationMinSectorSize = MinSectorSizeSlider.value;
			GameController.Instance.SandboxPlayerSettings.GenerationMaxSectorSize = MaxSectorSizeSlider.value;
			GameController.Instance.SandboxPlayerSettings.GenerationAsteroidDustCloudsProbability = AsteroidDustCloudsSlider.value;
			GameController.Instance.SaveSandboxPlayerSettings();
		}

		private void GenerationSeedButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowEnterNumberScreen(seedSettings.WorldGeneratorSettings.SeedValue, EnterGenerationSeedValueConfirmed, "Enter seed value");
		}

		private void EnterGenerationSeedValueConfirmed(EnterNumberScreen sender, bool confirmed, int? newValue)
		{
			if (confirmed)
			{
				seedSettings.WorldGeneratorSettings.SeedValue = newValue.Value;
				UpdateSeedButtonText();
				ApplySettingsAndCreateWorldBlueprint();
			}
		}

		private void UpdateSeedButtonText()
		{
			GenerationSeedButton.GetComponentInChildren<Text>().text = $"Seed: {seedSettings.WorldGeneratorSettings.SeedValue}";
		}

		private WorldSeedSettings CreateSeedSettings()
		{
			WorldSeedSettings result = UnityObjectHelper.InstantiateAndGetComponent(GameController.Instance.GameSettings.DefaultWorldSeedSettings);
			PopulateSeedSettings(result);
			return result;
		}

		private void InitAsteroidDustCloudsSlider()
		{
			AsteroidDustCloudsSlider.minValue = 0f;
			AsteroidDustCloudsSlider.maxValue = 1f;
			AsteroidDustCloudsSlider.value = GameController.Instance.SandboxPlayerSettings.GenerationAsteroidDustCloudsProbability;
		}

		private void InitMaxWormholesSlider()
		{
			MaxWormholesSlider.value = GameController.Instance.SandboxPlayerSettings.GenerationMaxWormholes;
			MaxWormholesSlider.minValue = MaxWormholesMinValue;
			MaxWormholesSlider.maxValue = MaxWormholesMaxValue;
		}

		private void InitSectorCountSlider()
		{
			NumSectorsSlider.wholeNumbers = true;
			NumSectorsSlider.minValue = 0f;
			NumSectorsSlider.maxValue = GameController.Instance.GameSettings.SandboxSettings.SectorCounts.Length - 1;
			NumSectorsSlider.value = GameController.Instance.SandboxPlayerSettings.GenerationNumSectorsIndex;
		}

		private void InitIntelCheckboxes()
		{
			DiscoverAllSectorsToggle.isOn = GameController.Instance.SandboxPlayerSettings.DiscoverAllSectors;
			DiscoverEverythingToggle.isOn = GameController.Instance.SandboxPlayerSettings.DiscoverEverything;
		}

		private void InitFactionSeedingOptions()
		{
			FactionSeedingToggle.isOn = GameController.Instance.SandboxPlayerSettings.FactionSeedingNonBanditFactions;
			BanditFactionSeedingToggle.isOn = GameController.Instance.SandboxPlayerSettings.FactionSeedingBanditFactions;
			FreelancerSeedingToggle.isOn = GameController.Instance.SandboxPlayerSettings.FactionSeedingFreelancers;
			FactionSeedingBanditPowerSlider.value = GameController.Instance.SandboxPlayerSettings.FactionSeedingBanditPower;
			FactionSeedingNonBanditPowerSlider.value = GameController.Instance.SandboxPlayerSettings.FactionSeedingNonBanditPower;
			FactionSeedingMinEmpireFactionsSlider.value = GameController.Instance.SandboxPlayerSettings.FactionSeedingMinEmpireFactions;
			FactionSeedingMaxEmpireFactionsSlider.value = GameController.Instance.SandboxPlayerSettings.FactionSeedingMaxEmpireFactions;
			FactionSeedingMinEmpireExpansionSlider.value = GameController.Instance.SandboxPlayerSettings.FactionSeedingMinEmpireExpansion;
			FactionSeedingMaxEmpireExpansionSlider.value = GameController.Instance.SandboxPlayerSettings.FactionSeedingMaxEmpireExpansion;
			GroupEmpireFactionsToggle.isOn = GameController.Instance.SandboxPlayerSettings.FactionSeedingGroupEmpireFactions;
			FactionSeedingCargoVolumeSlider.value = GameController.Instance.SandboxPlayerSettings.FactionSeedingCargoVolume;
		}

		private void InitSnakinessSlider()
		{
			SnakinessSlider.value = GameController.Instance.SandboxPlayerSettings.GenerationSnakiness;
			SnakinessSlider.minValue = 0f;
			SnakinessSlider.maxValue = 1f;
		}

		private void InitMinSectorSizeSlider()
		{
			MinSectorSizeSlider.value = GameController.Instance.SandboxPlayerSettings.GenerationMinSectorSize;
			MinSectorSizeSlider.minValue = 0f;
			MinSectorSizeSlider.maxValue = 1f;
		}

		private void InitMaxSectorSizeSlider()
		{
			MaxSectorSizeSlider.value = GameController.Instance.SandboxPlayerSettings.GenerationMaxSectorSize;
			MaxSectorSizeSlider.minValue = 0f;
			MaxSectorSizeSlider.maxValue = 1f;
		}

		private void InitAsteroidRespawnTimeSlider()
		{
			AsteroidRespawnTimeSlider.value = Mathf.Clamp01(GameController.Instance.SandboxPlayerSettings.AsteroidRespawnTime);
		}

		private void NumSectorsSliderValueChanged(float arg0)
		{
			RefreshSectorCountLabel();
			worldBlueprint = null;
		}

		private void SnakinessSliderValueChanged(float arg0)
		{
			worldBlueprint = null;
		}

		private void MaxWormholesSliderValueChanged(float arg0)
		{
			RefreshMaxWormholesLabel();
			worldBlueprint = null;
		}

		private void MinSectorSizeSliderValueChanged(float arg0)
		{
			worldBlueprint = null;
			if (MaxSectorSizeSlider.value < arg0)
			{
				MaxSectorSizeSlider.value = arg0;
			}
		}

		private void MaxSectorSizeSliderValueChanged(float arg0)
		{
			worldBlueprint = null;
			if (MinSectorSizeSlider.value > arg0)
			{
				MinSectorSizeSlider.value = arg0;
			}
		}

		private void RefreshSectorCountLabel()
		{
			SectorCountLabel.text = GetSectorCountText();
		}

		private void RefreshMaxWormholesLabel()
		{
			MaxWormholesLabel.text = GetCurrentMaxWormholesSetting().ToString();
		}

		private string GetSectorCountText()
		{
			SandboxSectorCount currentSectorCountSetting = GetCurrentSectorCountSetting();
			string text = ((currentSectorCountSetting.MinCount == currentSectorCountSetting.MaxCount) ? currentSectorCountSetting.MaxCount.ToString() : $"{currentSectorCountSetting.MinCount}-{currentSectorCountSetting.MaxCount}");
			return currentSectorCountSetting.Name + " (" + text + ")";
		}

		private SandboxSectorCount GetCurrentSectorCountSetting()
		{
			int num = (int)NumSectorsSlider.value;
			return GameController.Instance.GameSettings.SandboxSettings.SectorCounts[num];
		}

		private void Preview()
		{
			CreateWorldBlueprintIfNull();
			UIController.Instance.ScreenNavigator.ShowUniverseBlueprintScreen(worldBlueprint, seedSettings.WorldGeneratorSettings, seedSettings.CreateBlueprintSectorsSeederSettings, ApplySettingsAndCreateWorldBlueprint);
		}

		private void CreateWorldBlueprintIfNull()
		{
			if (worldBlueprint == null)
			{
				ApplySettingsAndCreateWorldBlueprint();
			}
		}

		protected override void refresh()
		{
			base.refresh();
			UpdateSeedButtonText();
		}

		protected override void onMadeCurrentPanel(bool navigatedForward)
		{
			base.onMadeCurrentPanel(navigatedForward);
			if (!navigatedForward)
			{
				Refresh();
			}
		}

		public WorldBlueprint ApplySettingsAndCreateWorldBlueprint()
		{
			PopulateSeedSettings(seedSettings);
			worldBlueprint = CreateWorldBlueprint();
			return worldBlueprint;
		}

		private WorldBlueprint CreateWorldBlueprint()
		{
			return new WorldBlueprintGenerator().Generate(seedSettings.WorldGeneratorSettings, GameController.Instance.CustomSectorNames);
		}
	}
}
