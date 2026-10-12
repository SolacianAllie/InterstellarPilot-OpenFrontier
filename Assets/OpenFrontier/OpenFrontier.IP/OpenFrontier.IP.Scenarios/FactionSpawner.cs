using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Bars;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.WorldSeeding;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers;
using OpenFrontier.IP.OutlawNamesGenerator;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OpenFrontier.IP.Scenarios
{
	public class FactionSpawner : MonoBehaviour
	{
		public FactionSpawnerSettings Settings;

		private double nextFactionSpawnTime;

		private double nextFreelancerSpawnTime;

		public double NextFactionSpawnTime
		{
			get
			{
				return nextFactionSpawnTime;
			}
			set
			{
				nextFactionSpawnTime = value;
			}
		}

		public double NextFreelancerSpawnTime
		{
			get
			{
				return nextFreelancerSpawnTime;
			}
			set
			{
				nextFreelancerSpawnTime = value;
			}
		}

		private void Update()
		{
			if (!EngineASX.LoadedAndReady || EngineASX.Instance.World.ObjectiveState != WorldBase.ScenarioState.Playing)
			{
				return;
			}
			double scenarioElapsedTime = EngineASX.Instance.ScenarioElapsedTime;
			if (scenarioElapsedTime > nextFactionSpawnTime)
			{
				FactionSpawnerSpawnType newFactionSpawnType = GetNewFactionSpawnType(Settings.FactionTypes, freelancer: false);
				if (newFactionSpawnType != null)
				{
					TrySpawnFromType(newFactionSpawnType);
					nextFactionSpawnTime = scenarioElapsedTime + (double)Settings.TimeBetweenFactionSpawns;
				}
				else
				{
					nextFactionSpawnTime = scenarioElapsedTime + 10.0;
				}
			}
			if (scenarioElapsedTime > nextFreelancerSpawnTime)
			{
				FactionSpawnerSpawnType newFactionSpawnType2 = GetNewFactionSpawnType(Settings.FactionTypes, freelancer: true);
				if (newFactionSpawnType2 != null)
				{
					TrySpawnFromType(newFactionSpawnType2);
					nextFreelancerSpawnTime = scenarioElapsedTime + (double)Settings.TimeBetweenMinorFactionSpawns;
				}
				else
				{
					nextFreelancerSpawnTime = scenarioElapsedTime + 10.0;
				}
			}
		}

		private bool TrySpawnFromType(FactionSpawnerSpawnType spawnType)
		{
			Sector bestHomeSectorForNewFactionType = GetBestHomeSectorForNewFactionType(spawnType.TypeInfo, !spawnType.IsFreelancer);
			if (bestHomeSectorForNewFactionType != null)
			{
				Faction faction = Spawn(spawnType);
				if (faction != null)
				{
					faction.ChangeHomeSector(bestHomeSectorForNewFactionType);
					faction.SpawnType = spawnType;
					FactionIntelSeederSettings factionIntelSeederSettings = GameController.Instance.GameSettings.DefaultWorldSeedSettings.FactionIntelSeederSettings;
					if (EngineASX.Instance.World.Seeder != null)
					{
						factionIntelSeederSettings = EngineASX.Instance.World.Seeder.Settings.FactionIntelSeederSettings;
					}
					WorldSeederUnitDiscovery.SeedFactionStaticIntel(faction, faction.HomeSector, factionIntelSeederSettings, 0.5f);
					WorldSeederUnitDiscovery.SeedFactionUnitIntel(faction, faction.HomeSector, factionIntelSeederSettings, 0.5f);
					faction.DiscoverOwnUnits();
					if (LogWrapper.LogMsgs)
					{
						LogWrapper.Log($"{this}: Spawned new faction: {faction}", this, 1);
					}
					return true;
				}
			}
			return false;
		}

		public static Sector GetBestHomeSectorForNewFactionType(FactionTypeInfo factionTypeInfo, bool isMajorFaction)
		{
			float maxRandomness = 40f;
			if (isMajorFaction)
			{
				maxRandomness = 4f;
			}
			return (from e in EngineASX.Instance.Sectors
				where FactionHomeScenePicker.IsValidSectorForFaction(e, factionTypeInfo)
				orderby GetFactionHomeSectorScore(e, factionTypeInfo, isMajorFaction) + UnityEngine.Random.value * maxRandomness descending
				select e).FirstOrDefault();
		}

		private static float GetFactionHomeSectorScore(Sector sector, FactionTypeInfo factionTypeInfo, bool isMajorFaction)
		{
			return FactionHomeScenePicker.GetHomeSectorScore(sector, factionTypeInfo, isMajorFaction);
		}

		public Faction Spawn(FactionSpawnerSpawnType spawnType)
		{
			return CreateFactionAndAIAndAssignName(spawnType);
		}

		public FactionSpawnerSpawnType GetNewFactionSpawnType(IEnumerable<FactionSpawnerSpawnType> spawnTypes, bool freelancer)
		{
			FactionSpawnerSpawnType factionSpawnerSpawnType = null;
			float num = 0f;
			foreach (FactionSpawnerSpawnType spawnType in spawnTypes)
			{
				if ((freelancer && spawnType.MinFixedShipCount <= 0) || (!freelancer && spawnType.MinFixedShipCount > 0) || !(EngineASX.Instance.ScenarioElapsedTime > spawnType.MinTimeBeforeSpawn))
				{
					continue;
				}
				int factionsOfType = GetFactionsOfType(spawnType.TypeInfo.FactionType);
				int requiredFactionCountOfType = GetRequiredFactionCountOfType(spawnType);
				if (factionsOfType < requiredFactionCountOfType && (!spawnType.IsBarOwner || EngineASX.Instance.BarNamesController.PeekNextUniqueBarName().HasValue) && (spawnType.TypeInfo.FactionType != FactionType.Outlaw || spawnType.IsFreelancer || EngineASX.Instance.OutlawNamesController.PeekNextUniqueName() != null) && FactionShipBuilder.ShipCountWithinCapacity(spawnType.TypeInfo, spawnType.MinFixedShipCount, 0, out var _))
				{
					float num2 = spawnType.SpawnPriority + UnityEngine.Random.value * 4f;
					int shipCountByFactionType = EngineASX.Instance.GetShipCountByFactionType(spawnType.TypeInfo.FactionType);
					int factionTypeShipCap = EngineASX.Instance.GetFactionTypeShipCap(spawnType.TypeInfo);
					if (shipCountByFactionType < factionTypeShipCap)
					{
						num2 += (1f - Mathf.Clamp01((float)shipCountByFactionType / (float)factionTypeShipCap)) * 100f;
					}
					if (factionSpawnerSpawnType == null || num2 > num)
					{
						factionSpawnerSpawnType = spawnType;
						num = num2;
					}
				}
			}
			return factionSpawnerSpawnType;
		}

		public static int GetRequiredFactionCountOfType(FactionSpawnerSpawnType factionSpawnType)
		{
			if (EngineASX.Instance.Sectors.Count < factionSpawnType.MinSectorsBeforeSpawn)
			{
				return 0;
			}
			int requiredFactionCountFromCountTYpe = GetRequiredFactionCountFromCountTYpe(factionSpawnType.NumFactionsCountType);
			return Mathf.Max(1, Mathf.CeilToInt((float)requiredFactionCountFromCountTYpe * factionSpawnType.NumFactionsPerSector));
		}

		private static int GetRequiredFactionCountFromCountTYpe(FactionSpawnNumFactionsCountType countType)
		{
			return countType switch
			{
				FactionSpawnNumFactionsCountType.PerControlledSector => EngineASX.Instance.ControlledSectorCount, 
				FactionSpawnNumFactionsCountType.PerSector => EngineASX.Instance.Sectors.Count, 
				FactionSpawnNumFactionsCountType.PerUncontrolledSector => EngineASX.Instance.UncontrolledSectorCount, 
				FactionSpawnNumFactionsCountType.PerControlledPlanetarySector => EngineASX.Instance.Sectors.Count((Sector e) => e.ControllingFaction != null && e.HasPlanets), 
				_ => 0, 
			};
		}

		private int GetFactionsOfType(FactionType factionType)
		{
			int num = 0;
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (faction != null && faction.FactionType == factionType)
				{
					num++;
				}
			}
			return num;
		}

		private static void AssignFactionNameFromGroupName(Faction faction, Fleet group)
		{
			AssignFactionNameFromPilotName(faction, group.Leader.Person);
		}

		private static Person FindSingleFactionPilot(Faction faction)
		{
			if (faction.People.Count > 0)
			{
				return faction.People[0];
			}
			return null;
		}

		private static bool TryAssignFactionNameFromPilotName(Faction faction)
		{
			if (faction.AISettings.PreferSingleShip)
			{
				Person person = FindSingleFactionPilot(faction);
				if (person != null)
				{
					AssignFactionNameFromPilotName(faction, person);
					return true;
				}
			}
			return false;
		}

		public static void AssignFactionName(Faction faction, FactionType factionType)
		{
			faction.CreateAISettingsIfNull();
			if ((!(faction.AISettings != null) || !faction.AISettings.PreferSingleShip || !TryAssignFactionNameFromPilotName(faction)) && !faction.Engine.FactionNames.TryGenerateAndAssignName(faction, factionType))
			{
				Debug.LogErrorFormat(faction, "Failed to assign name to: {0}. FactionType {1} ", faction.gameObject.name, Enum.GetName(typeof(FactionType), faction.FactionType));
			}
		}

		public static void AssignFactionNameFromPilotName(Faction faction, Person person)
		{
			faction.Name = person.Name;
			faction.ShortName = person.ShortName;
		}

		private int NumMajorFactionsOfType(FactionType type)
		{
			int num = 0;
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (faction != null && faction.FactionType == type && !faction.AISettings.PreferSingleShip)
				{
					num++;
				}
			}
			return num;
		}

		private int NumFactionsOfType(FactionType type)
		{
			int num = 0;
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (faction != null && faction.FactionType == type)
				{
					num++;
				}
			}
			return num;
		}

		public static Faction CreateFactionAndAIAndAssignName(FactionSpawnerSpawnType spawnType)
		{
			Faction faction = CreateFactionAndAI(spawnType);
			AssignNewFactionName(spawnType, faction);
			return faction;
		}

		public static Faction CreateFactionAndAI(FactionSpawnerSpawnType spawnType)
		{
			Faction faction = UnityObjectHelper.InstantiateAndGetComponent(GameController.Instance.GenericFactionPrefab);
			faction.Virtue = Mathf.Lerp(spawnType.MinVirtue, spawnType.MaxVirtue, Mathf.Pow(UnityEngine.Random.value, spawnType.VirtueRandomPower));
			faction.CreateAISettingsIfNull();
			if (spawnType.ShouldHaveBountyBoard)
			{
				faction.FindOrCreateBountyBoard();
			}
			faction.PersonAvatarProfiles.AddRange(spawnType.PersonAvatarProfiles);
			if (spawnType.MinFixedShipCount > -1)
			{
				faction.AISettings.FixedShipCount = UnityEngine.Random.Range(spawnType.MinFixedShipCount, spawnType.MaxFixedShipCount + 1);
			}
			faction.AISettings.BuildShips = spawnType.BuildShips;
			SetSpawnedFactionCredits(spawnType, faction);
			faction.CreateMissions = true;
			faction.ShouldFactionShowMissionSpecs = true;
			faction.Greed = Mathf.Pow(UnityEngine.Random.value, 0.9f);
			faction.Cooperation = UnityEngine.Random.value;
			faction.TradeEfficiency = GetRandomTradeEfficiency();
			faction.TradeIllegalGoods = faction.Virtue < EngineASX.Instance.EconomySettings.TradeIllegalCargoThreshold;
			faction.Aggression = Mathf.Lerp(spawnType.MinAggresion, spawnType.MaxAggresion, Mathf.Pow(UnityEngine.Random.value, spawnType.AggressionRandomPower));
			faction.FactionType = spawnType.TypeInfo.FactionType;
			if (spawnType.PilotRankingSystem != null)
			{
				faction.PilotRankingSystem = spawnType.PilotRankingSystem;
			}
			else if (spawnType.PilotRankingSystems.Count > 0)
			{
				faction.PilotRankingSystem = spawnType.PilotRankingSystems.GetRandom();
			}
			else
			{
				faction.PilotRankingSystem = null;
			}
			faction.AISettings.LargeShipPreference = Mathf.Lerp(spawnType.LargeShipPreferenceLower, spawnType.LargeShipPreferenceUpper, Mathf.Pow(UnityEngine.Random.value, spawnType.LargeShipPreferencePower));
			faction.AISettings.CloakShipPreference = Mathf.Lerp(spawnType.CloakShipPreferenceLower, spawnType.CloakShipPreferenceUpper, Mathf.Pow(UnityEngine.Random.value, spawnType.LargeShipPreferencePower));
			faction.AISettings.MinGroupUnitCount = spawnType.MinShipsInGroup;
			faction.AISettings.MaxGroupUnitCount = spawnType.MaxShipsInGroup;
			faction.AISettings.PreferenceToPlaceBounty = Mathf.Lerp(spawnType.MinPreferenceToPlaceBounty, spawnType.MaxPreferenceToPlaceBounty, UnityEngine.Random.value);
			faction.AISettings.MaxJumpDistanceFromHomeSector = spawnType.MaxJumpDistanceFromHomeSector;
			faction.AISettings.MaxStationBuildDistanceFromHomeSector = spawnType.MaxStationBuildDistanceFromHomeSector;
			faction.AISettings.SectorControlLikelihood = spawnType.SectorControlLikelihood;
			faction.AISettings.OffensiveStance = Maths.RandomFloatWithPower(spawnType.MinOffensiveStance, spawnType.MaxOffensiveStance, spawnType.OffensiveStancePower);
			faction.AISettings.PreferenceToHaveAmmo = Maths.RandomFloatWithPower(spawnType.MinPreferenceToHaveAmmo, spawnType.MaxPreferenceToHaveAmmo, spawnType.PreferenceToHaveAmmoPower);
			faction.AISettings.PreferenceToBuildStations = UnityEngine.Random.Range(spawnType.MinPreferenceToBuildStations, spawnType.MaxPreferenceToBuildStations);
			faction.AISettings.PreferenceToBuildTurrets = UnityEngine.Random.Range(0f, 1f);
			faction.MinAIUnitControllerCombatEfficiency = spawnType.MinNpcCombatEfficiency;
			faction.MaxAIUnitControllerCombatEfficiency = spawnType.MaxNpcCombatEfficiency;
			faction.Init();
			if (spawnType.TypeInfo.FactionAIType != FactionAIType.None)
			{
				CreateAndInitFactionAI(spawnType.TypeInfo, faction);
			}
			if (faction.AISettings.PreferSingleShip)
			{
				NpcPilot npcPilot = faction.FactionAI.SpawnAndInitialiseNpcPilotWithRandomName(EngineASX.Instance.GenericPilotPrefab);
				npcPilot.Person.SyncPersonalityWithFaction(faction);
				npcPilot.DestroyWhenNotPilotting = false;
				npcPilot.DestroyWhenNoUnit = false;
				npcPilot.Init();
				npcPilot.Person.Faction = faction;
				npcPilot.Person.AssignFirstPilotRankIfNull();
				faction.LeaderPerson = npcPilot.Person;
			}
			faction.SetSpawnTimeToCurrent();
			if (spawnType.MaxDailyIncome > 0)
			{
				faction.AISettings.DailyIncome = UnityEngine.Random.Range(spawnType.MinDailyIncome, spawnType.MaxDailyIncome);
			}
			if (ShouldApplyTeamColour(faction))
			{
				ApplyTeamColour(faction);
			}
			faction.DestroyWhenNoUnits = false;
			if (faction.FactionType == FactionType.None)
			{
				Debug.LogErrorFormat(spawnType, "Faction type {0} has None faction type", spawnType);
				return null;
			}
			if (faction.FactionType == FactionType.Player)
			{
				Debug.LogErrorFormat(spawnType, "Faction type {0} has Player faction type", spawnType);
				return null;
			}
			return faction;
		}

		public static float GetRandomTradeEfficiency()
		{
			return Mathf.Lerp(GameController.Instance.GameSettings.EconomySettings.FactionAIMinTradeEfficiency, 1f, Mathf.Pow(UnityEngine.Random.value, GameController.Instance.GameSettings.EconomySettings.FactionAITradeEfficiencyPower));
		}

		public static void AssignNewFactionName(FactionSpawnerSpawnType spawnType, Faction faction)
		{
			if (spawnType.IsBarOwner)
			{
				BarName? andRemoveUniqueBarName = EngineASX.Instance.BarNamesController.GetAndRemoveUniqueBarName();
				if (andRemoveUniqueBarName.HasValue)
				{
					faction.Name = andRemoveUniqueBarName.Value.Name;
					faction.ShortName = andRemoveUniqueBarName.Value.ShortName;
				}
				else
				{
					Debug.LogWarning("Could not create unique name for bar owner faction. Assigning generic name", faction);
					AssignFactionName(faction, FactionType.Trader);
				}
			}
			else if (spawnType.TypeInfo.FactionType == FactionType.Outlaw && !spawnType.IsFreelancer)
			{
				GenderChoice genderChoice = GenderChoice.Unspecified;
				float num = 0.3f;
				if (UnityEngine.Random.value < num)
				{
					genderChoice = ((!(UnityEngine.Random.value < 0.5f)) ? GenderChoice.Male : GenderChoice.Female);
				}
				faction.AISettings.PilotGender = genderChoice;
				float num2 = 0.4f;
				int? personCount = null;
				if (faction.AISettings.FixedShipCount > 1 && faction.AISettings.FixedShipCount < 6 && UnityEngine.Random.value < num2)
				{
					personCount = faction.AISettings.FixedShipCount;
				}
				GeneratedPirateName generatedPirateName = PirateNameGenerator.GenerateName(EngineASX.Instance.OutlawNamesController.PirateNameData, personCount, genderChoice, 0.5f);
				if (generatedPirateName == null)
				{
					Debug.LogWarning("Failed to generate name for outlaw faction", faction);
					return;
				}
				faction.Name = generatedPirateName.Name;
				faction.ShortName = generatedPirateName.ShortName;
			}
			else
			{
				AssignFactionName(faction, faction.FactionType);
			}
		}

		public static void ApplyTeamColour(Faction faction)
		{
			faction.TeamColour = GetTeamColour(faction);
		}

		public static Color GetTeamColour(Faction faction)
		{
			return faction.FactionType switch
			{
				FactionType.Empire => GameController.Instance.GameSettings.TeamColourSettings.EmpireColors.GetRandom(), 
				FactionType.Bandit => GameController.Instance.GameSettings.TeamColourSettings.BanditColors.GetRandom(), 
				_ => GameController.Instance.GameSettings.TeamColourSettings.GenericColors.GetRandom(), 
			};
		}

		public static bool ShouldApplyTeamColour(Faction faction)
		{
			FactionType factionType = faction.FactionType;
			if (factionType == FactionType.Empire || factionType == FactionType.Bandit)
			{
				return true;
			}
			if (!faction.IsMinor)
			{
				return UnityEngine.Random.value < 0.8f;
			}
			return UnityEngine.Random.value < 0.2f;
		}

		public static void SetSpawnedFactionCredits(FactionSpawnerSpawnType spawnType, Faction faction)
		{
			int minSpawnCredits = spawnType.MinSpawnCredits;
			int maxSpawnCredits = spawnType.MaxSpawnCredits;
			faction.Credits = Mathf.CeilToInt(Mathf.Lerp(minSpawnCredits, maxSpawnCredits, Mathf.Pow(UnityEngine.Random.value, spawnType.SpawnCreditsPower)));
		}

		private static void CreateAndInitFactionAI(FactionTypeInfo factionTypeInfo, Faction faction)
		{
			faction.CreateAndInitFactionAIFromType(factionTypeInfo.FactionAIType);
		}
	}
}
