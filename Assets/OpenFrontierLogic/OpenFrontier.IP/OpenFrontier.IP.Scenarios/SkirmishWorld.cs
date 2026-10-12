using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.CustomUnitVariants;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.PilotRankings;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.PilotRanking;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.Scenarios
{
	public class SkirmishWorld : WorldBase
	{
		public PilotRankingSystem PilotRankingSystem;

		public Vector3 PlayerUnitFudgePosition = new Vector3(0f, 0f, -200f);

		public float MinNpcRestrictedWeaponUsage = 0.1f;

		public float MaxNpcRestrictedWeaponUsage = 0.5f;

		private List<Faction> activeAIFactions = new List<Faction>();

		public Faction AIFactionPrefab;

		public Person AIPilotPrefab;

		public SkirmishScenarioData DefaultLoadData;

		[FormerlySerializedAs("GroupPrefab")]
		public Fleet FleetPrefab;

		public int MaxShipLootCount = 3;

		public int MaxShipLootInstances = 3;

		public int MinShipLootCount = 1;

		public int MinShipLootInstances = 1;

		private Dictionary<CargoClass, ProjectileClass> playerCompatibleCargoClasses = new Dictionary<CargoClass, ProjectileClass>();

		private SkirmishScenarioData scenarioData;

		public float TeamDistanceFromOrigin = 1000f;

		public float UnitSpacing = 100f;

		public float ShipLootProbability = 0.25f;

		protected override ScenarioLoadData CreateDefaultLoadData()
		{
			return DefaultLoadData;
		}

		protected override void OnNewGame()
		{
			scenarioData = (SkirmishScenarioData)lastLoadedData;
			if (!IsScenarioDataValid(scenarioData))
			{
				Debug.LogError("Skirmish scenario data is invalid.. creating new data");
				scenarioData = DefaultLoadData;
			}
			base.OnNewGame();
			Permissions.AllowSaving = GameController.Instance.LaunchOrigin != GameController.EngineLaunchSource.BattlesUI;
			Permissions.AllowGodMode = GameController.Instance.LaunchOrigin != GameController.EngineLaunchSource.BattlesUI;
			if (EngineASX.Instance.Sectors.Count == 0)
			{
				throw new ApplicationException("Cannot initialise skirmish. No sectors have been created");
			}
			if (InitialSector == null)
			{
				InitialSector = EngineASX.Instance.Sectors.FirstOrDefault();
			}
			CreateTeamsAndShips();
			SetupDiplomacy();
			PromotePilotsFromShipSeeder.Seed(includePlayerFaction: true, autoCreateLeaderIfNone: true, updateDebugInfo: false);
			PopulatePlayerCompatibleCargoClasses();
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				faction.IntelScanner.PerformFullScanImmediate();
				foreach (Fleet fleet in faction.Fleets)
				{
					fleet.TargetScanner.PerformScanImmediate();
					foreach (NpcPilot npcPilot in fleet.NpcPilots)
					{
						npcPilot.NextCombatTargetSearch = Time.time;
					}
				}
			}
		}

		private bool IsScenarioDataValid(SkirmishScenarioData scenarioData)
		{
			if (scenarioData.Teams == null || scenarioData.Teams.Count < 2)
			{
				return false;
			}
			if (scenarioData.Teams.Count((SkirmishTeamParams team) => team.ShipItems.Count((SkirmishTeamParamsItem shipItem) => shipItem.UnitClass != null) > 0) < 2)
			{
				return false;
			}
			return true;
		}

		protected override void OnStateChanged(ScenarioState oldState)
		{
			base.OnStateChanged(oldState);
			if (oldState == ScenarioState.Intro)
			{
				_ = ObjectiveState;
				_ = 1;
			}
		}

		protected override void update()
		{
			base.update();
			if (ObjectiveState == ScenarioState.Playing)
			{
				Engine.NotifyPlayerInCombat();
			}
		}

		protected override void OnUnitDestroyed(Unit unit)
		{
			base.OnUnitDestroyed(unit);
			CreateLootItemsForDestroyedUnits(unit);
		}

		private void CreateLootItemsForDestroyedUnits(Unit unit)
		{
			if (!(unit.Sector != null) || (unit.UnitType != UnitType.Ship && unit.UnitType != UnitType.Station) || playerCompatibleCargoClasses.Count <= 0 || !(UnityEngine.Random.value < ShipLootProbability))
			{
				return;
			}
			int num = UnityEngine.Random.Range(MinShipLootInstances, MaxShipLootInstances + 1);
			for (int i = 0; i < num; i++)
			{
				KeyValuePair<CargoClass, ProjectileClass> random = playerCompatibleCargoClasses.GetRandom();
				CargoClass key = random.Key;
				int num2 = UnityEngine.Random.Range(MinShipLootCount, MaxShipLootCount + 1) * random.Value.BurstRounds;
				if (num2 > 0)
				{
					unit.CreateLootItem(key, num2);
				}
			}
		}

		private void SetupDiplomacy()
		{
			SetFactionsAtWar();
		}

		private void SetFactionsAtWar()
		{
			for (int i = 0; i < activeAIFactions.Count; i++)
			{
				for (int j = 0; j < activeAIFactions.Count; j++)
				{
					if (i != j)
					{
						activeAIFactions[i].SetAsHostileTo(activeAIFactions[j], permanentWar: true);
					}
				}
			}
		}

		private void CreateTeamsAndShips()
		{
			SkirmishTeamParams[] array = scenarioData.Teams.Where((SkirmishTeamParams e) => e.ShipItems.Any((SkirmishTeamParamsItem f) => f != null)).ToArray();
			int num = 0;
			for (int num2 = 0; num2 < GameController.Instance.GameSettings.SkirmishSettings.MaxSkirmishTeams; num2++)
			{
				if (num2 >= scenarioData.Teams.Count || !scenarioData.Teams[num2].ShipItems.Any())
				{
					continue;
				}
				Faction faction = UnityObjectHelper.InstantiateAndGetComponent(AIFactionPrefab);
				faction.ShortName = (faction.Name = $"Team {num2 + 1}");
				faction.Init();
				faction.Virtue = UnityEngine.Random.Range(0f, 1f);
				faction.AutoNameGameObject();
				faction.PilotRankingSystem = PilotRankingSystem;
				if (num2 == 0)
				{
					Fleet fleet = CreateFleetAndShips(num, array.Length, faction, scenarioData.Teams[num2].ShipItems.Where((SkirmishTeamParamsItem e) => e != null).Skip(1).ToArray());
					Unit unit = CreateUnit(scenarioData.Teams[num2].ShipItems.Where((SkirmishTeamParamsItem e) => e != null).First());
					unit.transform.localPosition = fleet.transform.localPosition + fleet.transform.localRotation * PlayerUnitFudgePosition;
					unit.transform.localRotation = fleet.transform.localRotation;
					GamePlayer gamePlayer = CreatePlayer();
					gamePlayer.Person.Faction = faction;
					unit.Components.PilotPerson = gamePlayer.Person;
					Person person = CreateNpcPerson(faction);
					person.CurrentUnit = unit;
					CreateNpcPilot(person);
					Engine.LocalPlayer = gamePlayer;
				}
				else
				{
					CreateFleetAndShips(num, array.Length, faction, scenarioData.Teams[num2].ShipItems.Where((SkirmishTeamParamsItem e) => e != null).ToArray());
				}
				faction.TeamColour = scenarioData.Teams[num2].TeamColor;
				activeAIFactions.Add(faction);
				num++;
			}
		}

		private Fleet CreateFleetAndShips(int teamIndex, int teamCount, Faction faction, SkirmishTeamParamsItem[] ships)
		{
			Fleet fleet = UnityEngine.Object.Instantiate(FleetPrefab);
			fleet.Init();
			fleet.Faction = faction;
			fleet.Sector = InitialSector;
			fleet.Settings.TargetInterceptionLowerDistance = (fleet.Settings.TargetInterceptionUpperDistance = float.MaxValue);
			fleet.Settings.DestroyWhenNoPilots = false;
			Quaternion quaternion = Quaternion.Euler(0f, (float)teamIndex / (float)teamCount * 360f, 0f);
			Quaternion localRotation = quaternion * Quaternion.Euler(0f, 180f, 0f);
			Vector3 localPosition = quaternion * new Vector3(0f, 0f, TeamDistanceFromOrigin);
			fleet.transform.localPosition = localPosition;
			fleet.transform.localRotation = localRotation;
			CreateNpcShips(fleet, ships, localPosition, localRotation);
			return fleet;
		}

		private void CreateNpcShips(Fleet fleet, SkirmishTeamParamsItem[] ships, Vector3 localPosition, Quaternion localRotation)
		{
			foreach (SkirmishTeamParamsItem skirmishTeamParamsItem in ships)
			{
				Unit unit = CreateUnit(skirmishTeamParamsItem);
				unit.Faction = fleet.Faction;
				unit.transform.rotation = fleet.transform.localRotation;
				Person person = CreateNpcPerson(fleet.Faction);
				person.CurrentUnit = unit;
				unit.Components.PilotPerson = person;
				CreateNpcPilot(person).Fleet = fleet;
			}
			fleet.transform.localPosition = localPosition;
			fleet.transform.localRotation = localRotation;
			foreach (UnitComponentHolder ship in fleet.Ships)
			{
				ship.transform.localPosition = fleet.GetPilotFleetFormationSectorPosition(ship.PilotPerson.NpcPilot);
			}
		}

		private Unit CreateUnit(SkirmishTeamParamsItem skirmishTeamParamsItem)
		{
			Unit unit = spawnUnit(skirmishTeamParamsItem, InitialSector);
			unit.transform.localPosition = Vector3.zero;
			unit.Components.ScanRange += 8000f;
			return unit;
		}

		private GamePlayer CreatePlayer()
		{
			GamePlayer gamePlayer = UnityEngine.Object.Instantiate(PlayerPrefab);
			gamePlayer.Awake();
			gamePlayer.Person.Init();
			gamePlayer.Person.CustomName = (gamePlayer.Person.CustomShortName = GameController.Instance.DefaultPilotName);
			gamePlayer.Person.RefreshName();
			return gamePlayer;
		}

		private Unit spawnUnit(SkirmishTeamParamsItem item, Sector sector)
		{
			Unit unit = WorldHelper.SpawnUnit(item.UnitClass.UnitPrefab, sector, installDefaultComponents: false, addCargoLoadout: false);
			if (item.CustomUnitVariant == null)
			{
				unit.Components.InstallDefaultComponents();
				unit.Components.AddDefaultCargoLoadout();
			}
			else
			{
				CustomUnitVariantHelper.InstallComponentsAddCargoAndRename(item.CustomUnitVariant, unit);
			}
			if (scenarioData.RandomShipComponents)
			{
				ModdedUnitSeederSettings moddedUnitSettings = GameController.Instance.GameSettings.ModdedUnitSettings;
				ModdedUnitSeeder.ModUnit(unit, moddedUnitSettings);
				ModdedUnitSeeder.AddUnitEquipment(unit, moddedUnitSettings, 1f);
				unit.Components.FullyRechargeComponents();
			}
			return unit;
		}

		private Person CreateNpcPerson(Faction faction)
		{
			Person person = UnityEngine.Object.Instantiate(AIPilotPrefab);
			person.Init();
			person.Faction = faction;
			person.DestroyGameObjectOnKill = true;
			person.RandomizeNameAndPersonalityAndAssignDialog();
			person.AssignFirstPilotRankIfNull();
			person.Aggression = Maths.RandomFloatWithPower(0.3f, 1f, UnityEngine.Random.value);
			return person;
		}

		private NpcPilot CreateNpcPilot(Person npcPerson)
		{
			NpcPilot component = npcPerson.GetComponent<NpcPilot>();
			component.Init();
			component.Settings.AICheatAmmo = false;
			component.Settings.CombatEfficiency = Mathf.Lerp(0.4f, 0.95f, UnityEngine.Random.value);
			component.Settings.RestrictedWeaponPreference = UnityEngine.Random.Range(MinNpcRestrictedWeaponUsage, MaxNpcRestrictedWeaponUsage);
			component.DestroyWhenNotPilotting = true;
			return component;
		}

		private void PopulatePlayerCompatibleCargoClasses()
		{
			playerCompatibleCargoClasses.Clear();
			Unit playerUnit = Engine.PlayerUnit;
			if (!(playerUnit != null) || !Engine.LocalPlayer.Person.IsPilot)
			{
				return;
			}
			for (int i = 0; i < playerUnit.Components.Turrets.Count; i++)
			{
				ProjectileTurretComponent projectileTurretComponent = playerUnit.Components.Turrets[i] as ProjectileTurretComponent;
				if (!(projectileTurretComponent != null))
				{
					continue;
				}
				for (int j = 0; j < projectileTurretComponent.ProjectileTurretClass.CompatibleProjectiles.Count; j++)
				{
					ProjectileClass projectileClass = projectileTurretComponent.ProjectileTurretClass.CompatibleProjectiles[j];
					if (projectileClass.AmmoClass != null && !playerCompatibleCargoClasses.ContainsKey(projectileClass.AmmoClass))
					{
						playerCompatibleCargoClasses.Add(projectileClass.AmmoClass, projectileClass);
					}
				}
			}
		}

		public override void OnPlayerUnitChanged(EngineASX engineASX, Unit oldUnit)
		{
			base.OnPlayerUnitChanged(engineASX, oldUnit);
			if (!(oldUnit != null) || !oldUnit.IsValidAndNotDestroyed)
			{
				return;
			}
			using List<Person>.Enumerator enumerator = oldUnit.Components.Crew.GetEnumerator();
			if (enumerator.MoveNext())
			{
				Person current = enumerator.Current;
				oldUnit.Components.PilotPerson = current;
				if (oldUnit.Components.PilotPerson.NpcPilot == null)
				{
					oldUnit.Components.PilotPerson.NpcPilot = CreateNpcPilot(oldUnit.Components.PilotPerson);
				}
				oldUnit.Components.PilotPerson.NpcPilot.Fleet = oldUnit.Components.PilotPerson.NpcPilot.Faction.Fleets.FirstOrDefault();
			}
		}

		public override void OnPlayerFleetCreated(Fleet fleet)
		{
			base.OnPlayerFleetCreated(fleet);
			fleet.Settings.TargetInterceptionLowerDistance = (fleet.Settings.TargetInterceptionUpperDistance = float.MaxValue);
			fleet.Settings.Aggression = 1f;
		}
	}
}
