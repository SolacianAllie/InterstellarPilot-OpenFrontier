using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Avatars;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Core;
using OpenFrontier.IP.Engine.Factions.Bounty;
using OpenFrontier.IP.Engine.Factions.TradeNetwork;
using OpenFrontier.IP.Engine.Fleets.FleetFormations;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.Engine.Heatmaps;
using OpenFrontier.IP.Engine.MissionSpecs;
using OpenFrontier.IP.Engine.PilotRankings;
using OpenFrontier.IP.Scenarios;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.Engine.Factions
{
	public class Faction : MonoBehaviour, ICreditsSource
	{
		public delegate void UnitLostHandler(Unit lostUnit, Unit destroyerUnit, Faction sourceFaction);

		private FactionTradeNetwork tradeNetwork;

		public const float NeutralVirtue = 0.5f;

		public const float SlightlyHighVirtue = 0.6f;

		public const float HighVirtue = 0.7f;

		public const float SlightlyLowVirtue = 0.4f;

		public const float LowVirtue = 0.3f;

		internal bool CouldHaveFleetsWithRestrictedSpend;

		public List<AvatarProfile> PersonAvatarProfiles = new List<AvatarProfile>();

		public FactionIntelProcessor IntelProcessor;

		public FleetFormationStyle PreferredFormationStyle;

		internal bool WasSeeded;

		private FactionFleetComposition fleetComposition;

		public Color TeamColour = Color.white;

		public bool IsIgnoredByAI;

		private FactionTypeInfo factionTypeInfo;

		[SerializeField]
		private Sector homeSector;

		[SerializeField]
		private Vector3? homeSectorPosition;

		[SerializeField]
		private Person leaderPerson;

		public FactionIntelScanner IntelScanner;

		private int creditsReserve = -1;

		public FactionIntel Intel;

		public FactionHeatmap Heatmap;

		private List<CargoTrader> validTraderTargets = new List<CargoTrader>();

		private bool validTargetTargetsFoundRefinery;

		internal List<Sector> controlledSectors = new List<Sector>();

		private float? lastValidTraderTargetCacheTime;

		public const float MinOpinionForOtherFactionToDock = -0.4f;

		private long highestEverNetWorth;

		private double spawnTime;

		public int AdditionalRpProvision;

		public float MinAIUnitControllerCombatEfficiency = 0.25f;

		public float MaxAIUnitControllerCombatEfficiency = 1f;

		public const float WorstOpinion = -1f;

		public const float BestOpinion = 1f;

		public const float SlightlyFriendlyOpinion = 0.25f;

		public const float FriendlyOpinion = 0.5f;

		public const float UnfriendlyOpinion = -0.5f;

		public const float SlightlyUnfriendlyOpinion = -0.25f;

		public const float NeutralOpinion = 0f;

		private HashSet<int> autopilotExcludedSectors = new HashSet<int>();

		private float lastCachedNetWorthTime;

		private long cachedNetWorth;

		private FactionBountyBoard bountyBoard;

		public FactionAISettings AISettings;

		private float lastTransactionTime;

		private List<FactionTransaction> recentTransactions = new List<FactionTransaction>();

		private Faction cachedNearestControlledSectorFaction;

		private float lastCachedNearestControlledSectorFaction;

		public FactionStats Stats;

		public float TradeEfficiency = 0.5f;

		[FormerlySerializedAs("Agression")]
		public float Aggression = 0.5f;

		public float Virtue = 0.5f;

		public float Greed = 0.5f;

		public float Cooperation = 0.5f;

		[FormerlySerializedAs("AllowFactionDamageReaction")]
		public bool DynamicFactionAttitudes;

		[SerializeField]
		private int credits = 10000;

		private Dictionary<int, FactionAttitude> relationMap = new Dictionary<int, FactionAttitude>(10);

		private List<FactionAttitude> relations = new List<FactionAttitude>(30);

		private Dictionary<int, FactionRecentDamage> recentDamageReceived = new Dictionary<int, FactionRecentDamage>(20);

		public bool CreateMissions;

		public string Description;

		public bool DestroyWhenNoUnits;

		private EngineASX engine;

		private FactionAIBase factionAi;

		private List<Fleet> fleets = new List<Fleet>(10);

		public bool IsCivilian;

		public FactionType FactionType;

		private List<MissionSpec> missionSpecs = new List<MissionSpec>();

		public string Name;

		private float nextRecentAttacksChkTime;

		private float nextFactionAIUpdate;

		public PilotRankingSystem PilotRankingSystem;

		public float RequisitionPointMultiplier = 1f;

		public List<CargoClass> RestrictedTradeGoods = new List<CargoClass>();

		public string ShortName;

		public bool ShouldFactionShowMissionSpecs = true;

		public bool TradeIllegalGoods;

		public int UniqueId = -1;

		private const int defaultUnitCollectionCapacity = 30;

		private List<Unit> units = new List<Unit>(30);

		private Dictionary<UnitType, List<Unit>> unitsByType = new Dictionary<UnitType, List<Unit>>();

		private List<Person> people = new List<Person>(20);

		internal FactionSpawnerSpawnType SpawnType;

		private int generatedNameId = -1;

		private int generatedSuffixId = -1;

		private int hostilityTimeoutCheckIndex;

		private static Queue<int> recentDamageProcessQueue = new Queue<int>(10);

		public IEnumerable<Sector> ControlledSectors => controlledSectors;

		public HashSet<int> AutopilotExcludedSectors => autopilotExcludedSectors;

		public bool IsCivilianFromFactionType
		{
			get
			{
				switch (FactionType)
				{
				case FactionType.Trader:
				case FactionType.Scavenger:
				case FactionType.Miner:
				case FactionType.PassengerTransport:
				case FactionType.Explorer:
				case FactionType.EquipmentDealer:
				case FactionType.Bar:
					return true;
				default:
					return false;
				}
			}
		}

		public float NextFactionAIUpdate
		{
			get
			{
				return nextFactionAIUpdate;
			}
			set
			{
				nextFactionAIUpdate = value;
			}
		}

		public List<Unit> Units => units;

		public List<Person> People => people;

		public bool IsPlayerFaction
		{
			get
			{
				Faction localFaction = EngineASX.Instance.LocalFaction;
				if (localFaction != null)
				{
					return localFaction == this;
				}
				return false;
			}
		}

		/// <summary>
		/// Whether a person is playing this faction - this copy's player, or
		/// another one.
		/// </summary>
		/// <remarks>
		/// Deliberately beside <see cref="IsPlayerFaction"/> rather than
		/// widening it. That one means "mine", and the game asks it in
		/// fifteen places where "mine" is the whole point - the AI does not
		/// sell to you, does not target you, does not dock at your stations.
		/// Widening it would quietly change all fifteen, and the measure of
		/// this work is that a player who never opens the multiplayer menu
		/// cannot tell any of it happened.
		///
		/// So this is the separate question, and it is asked only where the
		/// answer is about how a faction should be drawn. With nobody else in
		/// the game it is true for exactly the one faction
		/// <see cref="IsPlayerFaction"/> is true for, so a singleplayer screen
		/// is unchanged - it just asks a second question.
		/// </remarks>
		public bool IsAPlayersFaction
		{
			get
			{
				if (IsPlayerFaction)
				{
					return true;
				}

				return PlayerFactions.IsPlayerFaction(UniqueId);
			}
		}

		public EngineASX Engine
		{
			get
			{
				return engine;
			}
			private set
			{
				if (!(engine != value))
				{
					return;
				}
				EngineASX engineASX = engine;
				engine = value;
				if (engineASX != null)
				{
					engineASX.DeregisterFaction(this);
				}
				if (engine != null)
				{
					if (UniqueId < 0)
					{
						UniqueId = engine.GetUniqueFactionId();
					}
					engine.RegisterFaction(this);
				}
			}
		}

		public FactionAIBase FactionAI
		{
			get
			{
				return factionAi;
			}
			set
			{
				if (factionAi != value)
				{
					factionAi = value;
				}
			}
		}

		public List<Fleet> Fleets => fleets;

		public List<MissionSpec> MissionSpecs => missionSpecs;

		public int Credits
		{
			get
			{
				return credits;
			}
			set
			{
				_ = 0;
				credits = Mathf.Clamp(value, 0, 1999999999);
			}
		}

		public FactionFleetComposition FleetComposition => fleetComposition;

		public FactionTypeInfo FactionTypeInfo
		{
			get
			{
				return factionTypeInfo;
			}
			set
			{
				factionTypeInfo = value;
			}
		}

		public bool IsAIFactionType
		{
			get
			{
				if (FactionType != FactionType.None)
				{
					return FactionType != FactionType.Player;
				}
				return false;
			}
		}

		public FactionSettings FactionSettings => GameController.Instance.GameSettings.FactionSettings;

		public FactionRecentDamageSettings RecentDamageSettings => EngineASX.Instance.GameSettings.FactionSettings.FactionRecentDamageSettings;

		public bool IsValidInGame => engine != null;

		public List<FactionTransaction> RecentTransactions
		{
			get
			{
				return recentTransactions;
			}
			set
			{
				recentTransactions = value;
			}
		}

		public float LastTransactionTime => lastTransactionTime;

		public FactionBountyBoard BountyBoard
		{
			get
			{
				return bountyBoard;
			}
			set
			{
				bountyBoard = value;
				if (bountyBoard != null)
				{
					bountyBoard.Faction = this;
				}
			}
		}

		public int GeneratedNameId
		{
			get
			{
				return generatedNameId;
			}
			set
			{
				generatedNameId = value;
			}
		}

		public int GeneratedSuffixId
		{
			get
			{
				return generatedSuffixId;
			}
			set
			{
				generatedSuffixId = value;
			}
		}

		public bool HasGeneratedName => generatedNameId > -1;

		public int CreditsReserve
		{
			get
			{
				if (creditsReserve == -1)
				{
					creditsReserve = RecalculateCreditsReserve();
				}
				return creditsReserve;
			}
			set
			{
				creditsReserve = Mathf.Clamp(value, 0, FactionConstants.MaxCreditsReserve);
			}
		}

		public double SpawnTime
		{
			get
			{
				return spawnTime;
			}
			set
			{
				spawnTime = value;
			}
		}

		public long HighestEverNetWorth
		{
			get
			{
				return highestEverNetWorth;
			}
			set
			{
				highestEverNetWorth = value;
			}
		}

		public bool IsMinor
		{
			get
			{
				if (cachedNetWorth == 0L)
				{
					UpdateNetWorth();
				}
				return cachedNetWorth < EngineASX.Instance.GameSettings.MinorFactionNetWorthThreshold;
			}
		}

		public Person LeaderPerson
		{
			get
			{
				return leaderPerson;
			}
			set
			{
				if (leaderPerson != value)
				{
					Person arg = leaderPerson;
					leaderPerson = value;
					if (leaderPerson != null)
					{
						leaderPerson.Faction = this;
					}
					if (LogWrapper.LogMsgs)
					{
						LogWrapper.Log($"Faction leader changed from {arg} to {leaderPerson}", this, 2);
					}
				}
			}
		}

		public List<FactionAttitude> Relations => relations;

		public bool IsFreelancer
		{
			get
			{
				if (AISettings != null)
				{
					return AISettings.PreferSingleShip;
				}
				return false;
			}
		}

		public int ControlledSectorCount => controlledSectors.Count;

		public bool IsGang
		{
			get
			{
				if (AISettings != null)
				{
					return AISettings.IsGang;
				}
				return false;
			}
		}

		public bool IsFreelancerOrGang
		{
			get
			{
				if (AISettings != null)
				{
					return AISettings.FixedShipCount > 0;
				}
				return false;
			}
		}

		public Sector HomeSector => homeSector;

		public Vector3? HomeSectorPosition
		{
			get
			{
				return homeSectorPosition;
			}
			set
			{
				homeSectorPosition = value;
			}
		}

		public bool ValidTraderTargetsFoundRefinery => validTargetTargetsFoundRefinery;

		public bool IsKnownToPlayer
		{
			get
			{
				Faction localFaction = EngineASX.Instance.LocalFaction;
				if (localFaction != null)
				{
					return localFaction.HasAttitudeToFaction(this);
				}
				return false;
			}
		}

		public FactionTradeNetwork TradeNetwork
		{
			get
			{
				return tradeNetwork;
			}
			set
			{
				tradeNetwork = value;
			}
		}

		public event UnitLostHandler UnitLost;

		public IEnumerable<FactionRecentDamage> GetRecentDamageItems()
		{
			return recentDamageReceived.Values;
		}

		public FactionRecentDamage GetRecentDamageItem(Faction faction)
		{
			if (recentDamageReceived.TryGetValue(faction.UniqueId, out var value))
			{
				return value;
			}
			return null;
		}

		public FactionRecentDamage GetOrCreateRecentDamageItem(Faction faction)
		{
			FactionRecentDamage value = null;
			if (recentDamageReceived.TryGetValue(faction.UniqueId, out value))
			{
				return value;
			}
			value = CreateNewRecentDamage(faction);
			recentDamageReceived.Add(faction.UniqueId, value);
			return value;
		}

		private FactionRecentDamage CreateNewRecentDamage(Faction faction)
		{
			return new FactionRecentDamage
			{
				TargetFaction = faction
			};
		}

		public void ToggleAllowSectorNavigation(Sector sector)
		{
			if (!(sector == null))
			{
				if (autopilotExcludedSectors.Contains(sector.UniqueId))
				{
					autopilotExcludedSectors.Remove(sector.UniqueId);
				}
				else
				{
					autopilotExcludedSectors.Add(sector.UniqueId);
				}
			}
		}

		internal void ClearGeneratedName()
		{
			if (generatedNameId > -1)
			{
				EngineASX.Instance.FactionNames.FreeName(generatedNameId);
			}
			if (generatedSuffixId > -1)
			{
				EngineASX.Instance.FactionNames.FreeSuffix(generatedSuffixId);
			}
			GeneratedNameId = -1;
			GeneratedSuffixId = -1;
		}

		internal void OnSectorNotControlled(Sector sector)
		{
			controlledSectors.Remove(sector);
		}

		internal void OnSectorControlled(Sector sector)
		{
			if (!controlledSectors.Contains(sector))
			{
				controlledSectors.Add(sector);
			}
			Intel.DiscoverSector(sector);
		}

		public bool RequestToJoinInWar(Faction requestor, FactionWarDeclaration warDeclaration)
		{
			bool flag = false;
			if (factionAi != null)
			{
				flag = factionAi.RequestToJoinWar(requestor, warDeclaration);
			}
			if (LogWrapper.LogMsgs)
			{
				if (requestor == warDeclaration.Aggressor)
				{
					LogWrapper.Log($"Faction {this} was requested to join an aggressive war against {warDeclaration.Defender}. Response: {flag}", this, 2);
				}
				else
				{
					LogWrapper.Log($"Faction {this} was requested to join a defensive war against {warDeclaration.Aggressor}. Response: {flag}", this, 2);
				}
			}
			return flag;
		}

		public void RemoveRecentDamageFrom(Faction faction)
		{
			recentDamageReceived.Remove(faction.UniqueId);
		}

		public string GetReputationDescription()
		{
			return FactionPersonalityDescriber.GetDescription(this);
		}

		internal string GetFactionHomeSectorDescription()
		{
			if (!(HomeSector != null))
			{
				return "-";
			}
			return HomeSector.Name;
		}

		public string GetFactionTypeDescription()
		{
			return GetFactionTypeDescription(this);
		}

		public static string GetFactionTypeDescription(Faction faction)
		{
			switch (faction.FactionType)
			{
			case FactionType.BountyHunter:
				return "Bounty hunter";
			case FactionType.Explorer:
				return "Explorer";
			case FactionType.EquipmentDealer:
				return "Equipment dealer";
			case FactionType.Bandit:
				return "Bandit";
			case FactionType.Mercenary:
				return "Mercenary";
			case FactionType.Miner:
				return "Miner";
			case FactionType.Empire:
				return "Empire";
			case FactionType.PassengerTransport:
				return "Passenger transporter";
			case FactionType.Bar:
				return "Bar";
			case FactionType.Trader:
				return "Trader";
			case FactionType.Scavenger:
				return "Scavenger";
			case FactionType.Security:
				return "Security";
			case FactionType.Outlaw:
				return "Outlaw";
			case FactionType.Generic:
			{
				if (faction.IsFreelancer)
				{
					return "Freelancer";
				}
				long num = faction.GetCachedNetWorth();
				if (num < 2000000)
				{
					return "Small outfit";
				}
				if (num < 20000000)
				{
					return "Corporation";
				}
				return "Megacorp";
			}
			default:
				return "Unknown";
			}
		}

		public FactionTypeInfo FindFactionTypeInfo()
		{
			foreach (FactionTypeInfo factionType in EngineASX.Instance.FactionTypes)
			{
				if (factionType.FactionType == FactionType)
				{
					return factionType;
				}
			}
			return null;
		}

		public void CreateAISettingsIfNull()
		{
			if (AISettings == null)
			{
				AISettings = GetComponent<FactionAISettings>();
				if (AISettings == null)
				{
					AISettings = gameObject.AddComponent<FactionAISettings>();
				}
			}
		}

		public FactionStats GetOrCreateStats()
		{
			if (Stats != null)
			{
				return Stats;
			}
			CreateStats();
			return Stats;
		}

		public void CreateStatsIfNull()
		{
			if (Stats == null)
			{
				CreateStats();
			}
		}

		private void CreateStats()
		{
			Stats = UnityObjectHelper.NewGameObject<FactionStats>(transform);
		}

		public static bool IsFactionHostileTo(Faction sourceFaction, Faction targetFaction)
		{
			if (sourceFaction != null && targetFaction != null)
			{
				return sourceFaction.IsHostileTo(targetFaction);
			}
			return false;
		}

		public static bool IsEitherFactionHostileTo(Faction sourceFaction, Faction targetFaction)
		{
			if (sourceFaction != null && targetFaction != null)
			{
				if (!sourceFaction.IsHostileTo(targetFaction))
				{
					return targetFaction.IsHostileTo(sourceFaction);
				}
				return true;
			}
			return false;
		}

		public static float NormalizeOpinion(float opinion)
		{
			return (opinion + 1f) / 2f;
		}

		public void Init()
		{
			Engine = EngineASX.Instance;
			nextRecentAttacksChkTime = Time.time + UnityEngine.Random.value * RecentDamageSettings.RecentAttacksDissipationChkRate;
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: Hello, I'm now starting", this, 3);
			}
			FactionTypeInfo = FindFactionTypeInfo();
			FindBountyBoard();
			if (Intel == null)
			{
				Intel = UnityObjectHelper.NewGameObject<FactionIntel>(transform);
				Intel.gameObject.name = "FactionIntel";
			}
			if (IntelScanner == null)
			{
				IntelScanner = UnityObjectHelper.NewGameObject<FactionIntelScanner>(transform);
				IntelScanner.gameObject.name = "FactionIntelScanner";
			}
			IntelProcessor = new FactionIntelProcessor(this);
			if (Heatmap == null && GameController.Instance.GameSettings.DebugSettings.FactionHeatmapEnabled)
			{
				Heatmap = UnityObjectHelper.NewGameObject<FactionHeatmap>(transform);
				Heatmap.gameObject.name = "FactionHeatmap";
			}
			fleetComposition = new FactionFleetComposition();
			fleetComposition.Faction = this;
			Intel.Init(this);
			IntelScanner.Init(this);
			if (Heatmap != null)
			{
				Heatmap.Init();
			}
			if (homeSector != null)
			{
				homeSector.AddHeadquarateredFaction(this);
			}
		}

		public virtual void OnNewGameFaction(Faction faction)
		{
			if (factionAi != null)
			{
				factionAi.OnNewGameFaction(faction);
			}
		}

		public bool IsAlliedTo(Faction faction)
		{
			if (faction != null)
			{
				FactionAttitude attitude = GetAttitude(faction);
				if (attitude != null)
				{
					return attitude.Neutrality == Neutrality.Allied;
				}
			}
			return false;
		}

		public bool IsHostileTo(Person person)
		{
			if (person != null)
			{
				return IsHostileTo(person.Faction);
			}
			return false;
		}

		public bool IsAffordableConsideringReserve(int creditsCost)
		{
			int num = 0;
			if (factionAi != null && AISettings != null && AISettings.IgnoreStationCreditsReserve)
			{
				num = CreditsReserve;
			}
			return Credits - num >= creditsCost;
		}

		public bool IsAffordable(int creditsCost)
		{
			return Credits >= creditsCost;
		}

		public bool IsHostileToOrAlwaysHostileTo(Faction faction)
		{
			return GetNeutralityWith(faction) == Neutrality.Hostile;
		}

		public bool IsHostileToOrAlwaysHostileTo(Unit unit)
		{
			if (unit.Faction == null)
			{
				return false;
			}
			return GetNeutralityWith(unit.Faction) == Neutrality.Hostile;
		}

		public bool IsHostileTo(Faction faction)
		{
			return GetNeutralityWithInternal(faction) == Neutrality.Hostile;
		}

		public bool IsAlwaysHostileToFaction(Faction faction)
		{
			if (factionAi != null && factionAi.IsAlwaysAtWarWithFaction(faction))
			{
				return true;
			}
			return false;
		}

		public Neutrality GetNeutralityWith(Faction faction)
		{
			if (IsAlwaysHostileToFaction(faction))
			{
				return Neutrality.Hostile;
			}
			return GetNeutralityWithInternal(faction);
		}

		public Neutrality GetNeutralityWithInternal(Faction faction)
		{
			if (faction != null && faction != this)
			{
				FactionAttitude attitude = GetAttitude(faction);
				if (attitude != null)
				{
					return attitude.Neutrality;
				}
			}
			return Neutrality.Neutral;
		}

		public bool IsHostileTo(Unit unit)
		{
			return IsHostileTo(unit.Faction);
		}

		public void SyncNeutralityIfAttitudeExists(Faction faction)
		{
			if (HasAttitudeToFaction(faction))
			{
				SyncNeutrality(faction);
			}
		}

		public void SyncNeutrality(Faction faction)
		{
			float opinion = faction.GetOpinion(this);
			Neutrality neutralityWithInternal = faction.GetNeutralityWithInternal(this);
			Neutrality neutralityWithInternal2 = GetNeutralityWithInternal(faction);
			if (neutralityWithInternal != neutralityWithInternal2)
			{
				SetNeutralityWith(faction, neutralityWithInternal);
			}
			SetOpinionWith(faction, opinion);
		}

		public void SetNeutralityWith(Faction faction, Neutrality neutrality)
		{
			if (GetNeutralityWithInternal(faction) != neutrality)
			{
				FactionAttitude orCreateAttitude = GetOrCreateAttitude(faction);
				bool isHostile = orCreateAttitude.IsHostile;
				orCreateAttitude.Neutrality = neutrality;
				if (neutrality == Neutrality.Hostile)
				{
					OnMadeHostileTo(faction, orCreateAttitude);
				}
				if (isHostile)
				{
					OnMadePeaceWith(faction, orCreateAttitude);
				}
			}
		}

		private void OnMadePeaceWith(Faction faction, FactionAttitude factionAttitude)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: Making peace with {faction}. Opinion: {factionAttitude.Opinion}", this, 1);
			}
		}

		private void OnMadeHostileTo(Faction faction, FactionAttitude factionAttitude)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: Now hostile to {faction}. Current opinion: {factionAttitude.Opinion}", this, 1);
			}
			UpdateHostilityCoolDownTime(factionAttitude, factionAttitude.Opinion);
			if (factionAttitude.Opinion > -0.25f)
			{
				factionAttitude.Opinion = -0.25f;
			}
			if (LogWrapper.LogMsgs && !factionAttitude.RestrictHostilityTimeout)
			{
				LogWrapper.Log($"{this}: I'll make peace with ({faction}) in {factionAttitude.HostilityEndTime - engine.ScenarioElapsedTime} seconds.", this, 2);
			}
		}

		public void MakePeace(Faction otherFaction)
		{
			if (IsHostileTo(otherFaction))
			{
				SetNeutralityWith(otherFaction, Neutrality.Neutral);
			}
			RemoveRecentDamage(otherFaction);
		}

		public void CreateTradeNetworkIfRequired()
		{
			if (tradeNetwork == null && !IsFreelancerOrGang)
			{
				FactionType factionType = FactionType;
				if (factionType == FactionType.Trader || factionType == FactionType.Player || factionType == FactionType.Generic)
				{
					tradeNetwork = new FactionTradeNetwork();
				}
			}
		}

		private void RemoveRecentDamage(Faction otherFaction)
		{
			if (otherFaction != null)
			{
				recentDamageReceived.Remove(otherFaction.UniqueId);
			}
		}

		public static void MakePeace(Faction faction1, Faction faction2)
		{
			faction1.MakePeace(faction2);
			faction2.MakePeace(faction1);
		}

		public void UndockAllShipsOfFaction(Faction faction)
		{
			foreach (Unit unit in units)
			{
				if (unit.IsDockable)
				{
					unit.UndockAllShipsOfFaction(faction);
				}
			}
		}

		public void SetAsPeaceTwoWay(Faction faction)
		{
			SetNeutralityWith(faction, Neutrality.Neutral);
			faction.SetNeutralityWith(this, Neutrality.Neutral);
		}

		public bool SetAsHostileTo(Faction faction, bool permanentWar = false, bool makeAlliesHostile = false)
		{
			if (faction.UniqueId == UniqueId)
			{
				Debug.LogError($"Faction \"{faction}\" cannot be an enemy of itself", this);
			}
			else if (GetNeutralityWithInternal(faction) != Neutrality.Hostile)
			{
				FactionAttitude orCreateAttitude = GetOrCreateAttitude(faction);
				SetNeutralityWith(faction, Neutrality.Hostile);
				orCreateAttitude.RestrictHostilityTimeout = permanentWar;
				if (makeAlliesHostile)
				{
					FactionAttitude[] array = relationMap.Values.ToArray();
					foreach (FactionAttitude factionAttitude in array)
					{
						if (factionAttitude != orCreateAttitude && factionAttitude.Neutrality == Neutrality.Allied && !factionAttitude.TargetFaction.IsAlliedTo(faction))
						{
							factionAttitude.TargetFaction.SetAsHostileTo(faction);
						}
					}
				}
				UndockAllShipsOfFaction(faction);
				return true;
			}
			return false;
		}

		public void SetAsNeutralWith(Faction faction)
		{
			SetNeutralityWith(faction, Neutrality.Neutral);
		}

		public void SetAsHostileToTwoWay(Faction faction)
		{
			SetAsHostileToTwoWay(faction, permanentWar: false);
		}

		public void SetOpinionWithTwoWay(Faction faction, float opinion)
		{
			SetOpinionWith(faction, opinion);
			faction.SetOpinionWith(this, opinion);
		}

		public void SetOpinionWith(Faction faction, float opinion)
		{
			GetOrCreateAttitude(faction)?.SetOpinionAndRecordTimeOfChange(opinion, this);
		}

		public void SetAsHostileToTwoWay(Faction faction, bool permanentWar)
		{
			SetAsHostileTo(faction, permanentWar);
			faction.SetAsHostileTo(this, permanentWar);
		}

		public void RemoveAttitude(Faction targetFaction)
		{
			SetAsNeutralWith(targetFaction);
			FactionAttitude attitude = GetAttitude(targetFaction);
			if (attitude != null)
			{
				relationMap.Remove(targetFaction.UniqueId);
				relations.Remove(attitude);
			}
		}

		public FactionAttitude CreateOrSetAttitude(Faction targetFaction, float opinion, Neutrality neutrality)
		{
			if (targetFaction != this)
			{
				FactionAttitude orCreateAttitude = GetOrCreateAttitude(targetFaction);
				orCreateAttitude.SetOpinionAndRecordTimeOfChange(opinion, this);
				orCreateAttitude.Neutrality = neutrality;
				return orCreateAttitude;
			}
			Debug.LogError($"Faction {this}:Cannot create an attitude to oneself", this);
			return null;
		}

		public void HandleOwnedUnitKilled(Unit lostUnit, UnitClass lostUnitClass, Unit attacker, Faction attackerFaction, DamageDirectType damageDirectType)
		{
			if (!IsValidInGame)
			{
				return;
			}
			if (factionAi != null)
			{
				factionAi.HandleOwnedUnitKilled(lostUnit, attacker, attackerFaction);
			}
			if (Heatmap != null && lostUnit.Sector != null && GameController.Instance.GameSettings.DebugSettings.FactionHeatmapEnabled)
			{
				Heatmap.UpdateHeatmapHeat(lostUnit.Sector, lostUnit.transform.position, 1f, expires: true);
			}
			if (attackerFaction != null && attackerFaction.UniqueId != UniqueId)
			{
				if (LogWrapper.LogMsgs)
				{
					string arg = "Unknown";
					if (lostUnit != null && lostUnit.Sector != null)
					{
						arg = lostUnit.Sector.Name;
					}
					LogWrapper.Log($"{this}: That bastard \"{attackerFaction.Name}\" has destroyed my unit in the \"{arg}\" sector", this, 2);
				}
				float damage = FactionRecentDamageController.CalculateDamageFromLostUnit(lostUnit, lostUnitClass, damageDirectType);
				HandleDamageFromSource(damage, attackerFaction, attacker, damageDirectType);
			}
			if (Stats != null)
			{
				Stats.RecordLostUnit(lostUnit);
			}
			if (UnitLost != null)
			{
				UnitLost(lostUnit, attacker, attackerFaction);
			}
		}

		public void HandleOwnedPersonKilled(Person person, Unit killerUnit, Faction killerFaction, DamageDirectType damageDirectType)
		{
			if (IsValidInGame)
			{
				float damage = FactionRecentDamageController.CalculateDamageFromLostPerson(person, damageDirectType);
				HandleDamageFromSource(damage, killerFaction, killerUnit, damageDirectType);
			}
		}

		public void HandleKilledAnotherFactionsUnit(Unit killedUnit, Unit killerUnit, Faction killedUnitFaction)
		{
			if (Stats != null)
			{
				Stats.RecordKilledUnit(killedUnit);
			}
		}

		public void CreateOrInitFactionAIIfNeeded()
		{
			if (factionAi == null)
			{
				factionAi = GetComponentInChildren<FactionAIBase>(includeInactive: true);
				if (factionAi != null)
				{
					factionAi.Init();
				}
				else if (IsAIFactionType)
				{
					if (factionTypeInfo == null)
					{
						throw new Exception($"Trying to create an AI for faction {this} but the faction type info is null. Faction type: {FactionType}");
					}
					CreateAndInitFactionAIFromType(factionTypeInfo.FactionAIType);
				}
			}
			else
			{
				factionAi.Init();
			}
		}

		public void CreateAndInitFactionAIFromType(FactionAIType aiType)
		{
			if (aiType != FactionAIType.None)
			{
				factionAi = FactionAIBase.CreateFactionAIType(transform, aiType);
				factionAi.Init();
			}
		}

		public void HandleDamageFromSource(float damage, Faction sourceFaction, Unit sourceUnit, DamageDirectType damageDirectionType)
		{
			if (sourceFaction != null && sourceFaction.UniqueId != UniqueId)
			{
				FactionRecentDamageController.HandleDamageFromSource(this, damage, sourceFaction, sourceUnit, damageDirectionType);
			}
		}

		public void ChangeOpinion(Faction faction, float change)
		{
			FactionAttitude orCreateAttitude = GetOrCreateAttitude(faction);
			if (orCreateAttitude != null)
			{
				ChangeOpinion(orCreateAttitude, change);
			}
		}

		internal void RemoveValidTraderTarget(Unit dock)
		{
			if (dock.Components != null && dock.Components.CargoTrader != null)
			{
				validTraderTargets.Remove(dock.Components.CargoTrader);
			}
		}

		public void ChangeOpinion(FactionAttitude attitude, float change)
		{
			attitude.SetOpinionAndRecordTimeOfChange(attitude.Opinion + change, this);
		}

		public float GetOpinionNormalized(Faction faction)
		{
			return NormalizeOpinion(GetOpinion(faction));
		}

		public float GetOpinion(Faction faction)
		{
			return GetAttitude(faction)?.Opinion ?? 0f;
		}

		public float? GetEffectiveOpinionOrNull(Faction faction)
		{
			if (IsHostileToOrAlwaysHostileTo(faction))
			{
				return -1f;
			}
			FactionAttitude attitude = GetAttitude(faction);
			if (attitude != null)
			{
				return attitude.Neutrality switch
				{
					Neutrality.Hostile => (float?)(-1f), 
					Neutrality.Allied => 1f, 
					_ => attitude.Opinion, 
				};
			}
			return null;
		}

		public FactionAttitude GetAttitude(Faction faction)
		{
			FactionAttitude value = null;
			if (relationMap.TryGetValue(faction.UniqueId, out value))
			{
				return value;
			}
			return null;
		}

		public long CalculateNetWorth()
		{
			long num = Credits;
			List<Unit> list = GetUnitsByType(UnitType.Station);
			if (list != null)
			{
				foreach (Unit item in list)
				{
					num += item.CalculateCurrentMoneyValue();
				}
			}
			List<Unit> list2 = GetUnitsByType(UnitType.Ship);
			if (list2 != null)
			{
				foreach (Unit item2 in list2)
				{
					num += item2.CalculateCurrentMoneyValue();
				}
			}
			List<Unit> list3 = GetUnitsByType(UnitType.Cargo);
			if (list3 != null)
			{
				foreach (Unit item3 in list3)
				{
					num += item3.CalculateCurrentMoneyValue();
				}
			}
			if (CouldHaveFleetsWithRestrictedSpend)
			{
				foreach (Fleet fleet in fleets)
				{
					foreach (FleetOrder item4 in fleet.OrderQueue)
					{
						num += item4.Credits;
					}
					if (fleet.ActiveOrder != null)
					{
						num += fleet.ActiveOrder.FleetOrder.Credits;
					}
				}
			}
			return num;
		}

		public void CreateAttitudeIfNone(Faction faction)
		{
			if (!relationMap.ContainsKey(faction.UniqueId))
			{
				CreateNewAttitude(faction);
			}
		}

		public FactionAttitude GetOrCreateAttitude(Faction faction)
		{
			FactionAttitude value = null;
			if (relationMap.TryGetValue(faction.UniqueId, out value))
			{
				return value;
			}
			return CreateNewAttitude(faction);
		}

		public bool HasAttitudeToFaction(Faction faction)
		{
			return relationMap.ContainsKey(faction.UniqueId);
		}

		public void DestroyAllFactionPeople()
		{
			Person[] array = People.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SafeDestroy();
			}
		}

		public void SafeDestroy()
		{
			ChangeHomeSector(null);
			if (units.Count > 0)
			{
				Unit[] array = Units.ToArray();
				for (int i = 0; i < array.Length; i++)
				{
					array[i].Faction = null;
				}
			}
			if (people.Count > 0)
			{
				Person[] array2 = People.ToArray();
				for (int i = 0; i < array2.Length; i++)
				{
					array2[i].Faction = null;
				}
			}
			if (engine != null)
			{
				ClearGeneratedName();
				engine.DeregisterFaction(this);
			}
			Engine = null;
			UnityEngine.Object.Destroy(gameObject);
		}

		public int GetValidShipCount()
		{
			int num = 0;
			foreach (Unit unit in units)
			{
				if (unit.IsValidAndNotDestroyed && unit.UnitType == UnitType.Ship)
				{
					num++;
				}
			}
			return num;
		}

		public int GetValidStationCount()
		{
			int num = 0;
			foreach (Unit unit in units)
			{
				if (unit.IsValidAndNotDestroyed && unit.UnitType == UnitType.Station)
				{
					num++;
				}
			}
			return num;
		}

		public int GetValidStationPurposeCount(StationPurpose stationPurpose)
		{
			int num = 0;
			foreach (Unit unit in units)
			{
				if (unit.IsValidAndNotDestroyed && unit.UnitType == UnitType.Station && unit.UnitClass.StationPurpose == stationPurpose)
				{
					num++;
				}
			}
			return num;
		}

		public int GetValidNonMinorStationCount()
		{
			List<Unit> value = null;
			int num = 0;
			if (unitsByType.TryGetValue(UnitType.Station, out value))
			{
				foreach (Unit item in value)
				{
					if (item.IsValidAndNotDestroyed && !item.IsMinorStation())
					{
						num++;
					}
				}
			}
			return num;
		}

		public int EstimateShipAndStationCount()
		{
			return GetCountOfUnitType(UnitType.Ship) + GetCountOfUnitType(UnitType.Station);
		}

		public int EstimateNonMinorShipAndStationCount()
		{
			return EstimateCountOfNonMinorShip() + EstimateCountOfNonMinorStation();
		}

		public int EstimateCountOfNonMinorShip()
		{
			int num = 0;
			List<Unit> list = GetUnitsByType(UnitType.Ship);
			if (list != null)
			{
				foreach (Unit item in list)
				{
					if (item != null && item.UnitClass.ShipType == ShipType.Normal)
					{
						num++;
					}
				}
			}
			return num;
		}

		public int EstimateCountOfNonMinorStation()
		{
			int num = 0;
			List<Unit> list = GetUnitsByType(UnitType.Station);
			if (list != null)
			{
				foreach (Unit item in list)
				{
					if (item != null && !item.IsMinorStation())
					{
						num++;
					}
				}
			}
			return num;
		}

		public int GetValidShipAndStationCount()
		{
			int num = 0;
			foreach (Unit unit in units)
			{
				if (unit.IsValidAndNotDestroyed && unit.IsStationOrShip())
				{
					num++;
				}
			}
			return num;
		}

		public bool AnyValidShipsAndStations()
		{
			foreach (Unit unit in units)
			{
				if (unit.IsValidAndNotDestroyed && unit.IsStationOrShip())
				{
					return true;
				}
			}
			return false;
		}

		public void AddUnit(Unit unit)
		{
			if (Units.Contains(unit))
			{
				return;
			}
			Units.Add(unit);
			List<Unit> value = null;
			if (!unitsByType.TryGetValue(unit.UnitType, out value))
			{
				value = new List<Unit>(30);
				unitsByType[unit.UnitType] = value;
			}
			value.Add(unit);
			CreditsReserve += unit.UnitClass.AICreditsReserve;
			if (fleetComposition.ShouldConsiderUnitClass(unit.UnitClass))
			{
				fleetComposition.AddUnit(unit);
			}
			unit.Faction = this;
			if (unit.IsDiscoverableType)
			{
				Intel.DiscoverUnit(unit);
			}
			if (Stats != null)
			{
				switch (unit.UnitType)
				{
				case UnitType.Ship:
					Stats.MostShipsOwned = Mathf.Max(Stats.MostShipsOwned, GetCountOfUnitType(UnitType.Ship));
					break;
				case UnitType.Station:
					Stats.MostStationsOwned = Mathf.Max(Stats.MostStationsOwned, GetCountOfUnitType(UnitType.Station));
					break;
				}
			}
		}

		public void RemoveUnit(Unit unit)
		{
			if (Units.Remove(unit))
			{
				if (unit.IsDiscoverableType)
				{
					Intel.DiscoverUnit(unit);
				}
				List<Unit> value = null;
				if (unitsByType.TryGetValue(unit.UnitType, out value))
				{
					value.Remove(unit);
				}
				CreditsReserve -= unit.UnitClass.AICreditsReserve;
				if (fleetComposition.ShouldConsiderUnitClass(unit.UnitClass))
				{
					fleetComposition.RemoveUnit(unit);
				}
				if (unit.Faction == this)
				{
					unit.Faction = null;
				}
			}
		}

		public void AddPilot(Person pilot)
		{
			if (!People.Contains(pilot))
			{
				People.Add(pilot);
				pilot.Faction = this;
			}
		}

		public void RemovePilot(Person pilot)
		{
			if (People.Remove(pilot) && pilot.Faction == this)
			{
				pilot.Faction = null;
			}
		}

		public void ClaimUnit(Unit unit, Fleet claimingFleet, bool silent = false)
		{
			if (unit.Faction != this)
			{
				unit.Faction = this;
				if (Stats != null)
				{
					Stats.RecordUnitClaimed(unit);
				}
				_ = IsPlayerFaction;
				EngineASX.Instance.NotifyUnitClaimed(unit, this, claimingFleet, silent);
				if (TurretControllerSpawner.ShouldAutomateTurrets(unit))
				{
					EngineASX.Instance.TurretControllerSpawner.AutomateTurrets(unit);
				}
			}
		}

		public float GetHostilityCoolDownTime(float currentOpinion)
		{
			float hostilityCoolDownTimePower = FactionSettings.HostilityCoolDownTimePower;
			hostilityCoolDownTimePower += (1f - Aggression) * FactionSettings.HostilityCoolDownTimeAggressionPower;
			float t = Mathf.Pow(1f - NormalizeOpinion(currentOpinion), hostilityCoolDownTimePower);
			return Mathf.Lerp(FactionSettings.MinHostilityCoolDownTime, FactionSettings.MaxHostilityCoolDownTime, t);
		}

		public bool RequestDock(Unit dockTarget, Unit requestor)
		{
			return RequestDock(dockTarget, (requestor != null) ? requestor.Faction : null);
		}

		public bool RequestDock(Unit dockTarget, Faction requestorFaction)
		{
			if (factionAi != null)
			{
				return factionAi.RequestDock(dockTarget, requestorFaction);
			}
			if (requestorFaction != null)
			{
				if (requestorFaction != this)
				{
					return RequestDockByAttitude(requestorFaction);
				}
				return true;
			}
			return false;
		}

		public bool RequestDockByAttitude(Faction requestorFaction)
		{
			if (IsHostileToOrAlwaysHostileTo(requestorFaction))
			{
				return false;
			}
			if (requestorFaction.IsCivilianFromFactionType)
			{
				return true;
			}
			_ = IsPlayerFaction;
			return GetOpinion(requestorFaction) >= -0.4f;
		}

		public IEnumerable<Faction> GetEnemies()
		{
			return GetFactionsWithNeutrality(Neutrality.Hostile);
		}

		public IEnumerable<Faction> GetAllies()
		{
			return GetFactionsWithNeutrality(Neutrality.Allied);
		}

		public IEnumerable<Faction> GetFactionsWithNeutrality(Neutrality neutrality)
		{
			foreach (FactionAttitude relation in relations)
			{
				if (relation.Neutrality == neutrality)
				{
					yield return relation.TargetFaction;
				}
			}
		}

		public IEnumerable<Faction> GetKnownFactionsWithOpinionLessThan(float opinion)
		{
			foreach (FactionAttitude relation in relations)
			{
				if (relation.TargetFaction != null)
				{
					float? effectiveOpinionOrNull = relation.TargetFaction.GetEffectiveOpinionOrNull(this);
					if (effectiveOpinionOrNull.HasValue && effectiveOpinionOrNull < opinion)
					{
						yield return relation.TargetFaction;
					}
				}
			}
		}

		public IEnumerable<Faction> GetKnownFactionsWithOpinionGreaterThan(float opinion)
		{
			foreach (FactionAttitude relation in relations)
			{
				if (relation.TargetFaction != null)
				{
					float? effectiveOpinionOrNull = relation.TargetFaction.GetEffectiveOpinionOrNull(this);
					if (effectiveOpinionOrNull.HasValue && effectiveOpinionOrNull > opinion)
					{
						yield return relation.TargetFaction;
					}
				}
			}
		}

		public bool WillBuyCargoType(CargoClass cargoClass)
		{
			return true;
		}

		public bool WillTradeCargoType(CargoClass cargoClass)
		{
			if (cargoClass.Legal || TradeIllegalGoods)
			{
				return !RestrictedTradeGoods.Contains(cargoClass);
			}
			return false;
		}

		public void RegisterNormalTradeWithFaction(Unit tradeStation, Faction otherFaction, float tradeValueToUs, FactionTransactionType transactionType, CargoClass relatedCargoClass = null, UnitClass relatedUnitClass = null, int? relatedCount = null, ICreditsSource creditsSource = null)
		{
			if (creditsSource == null)
			{
				creditsSource = this;
			}
			if (engine.World.AITradePurchaseRequiresCredits || IsPlayerFaction)
			{
				ApplyTransaction((int)tradeValueToUs, transactionType, otherFaction, tradeStation, relatedCargoClass, relatedUnitClass, FactionTransactionTaxType.None, relatedCount, creditsSource);
			}
			if (otherFaction != null && this != otherFaction)
			{
				if ((engine.World.AITradePurchaseRequiresCredits || otherFaction.IsPlayerFaction) && (relatedCargoClass == null || (!relatedCargoClass.IsDeployable && !relatedCargoClass.IsEquipment)))
				{
					otherFaction.ApplyTransaction(-(int)tradeValueToUs, transactionType, this, tradeStation, relatedCargoClass, relatedUnitClass, FactionTransactionTaxType.None, relatedCount);
				}
				otherFaction.ApplyTradeValueToAttitudes(this, Mathf.Abs(tradeValueToUs));
			}
			GiveTaxToControllingFaction(tradeStation, Mathf.Abs(tradeValueToUs), transactionType, relatedCargoClass, relatedUnitClass, relatedCount);
		}

		public void RegisterTaxedTradeWithFaction(Unit tradeStation, Faction otherFaction, float tradeValueToUs, FactionTransactionType transactionType, CargoClass relatedCargoClass = null, UnitClass relatedUnitClass = null, int? relatedCount = null)
		{
			if (engine.World.AITradePurchaseRequiresCredits || IsPlayerFaction)
			{
				ApplyTransaction((int)tradeValueToUs, transactionType, otherFaction, tradeStation, relatedCargoClass, relatedUnitClass, FactionTransactionTaxType.None, relatedCount);
			}
			if (otherFaction != null && this != otherFaction)
			{
				otherFaction.ApplyTradeValueToAttitudes(this, Mathf.Abs(tradeValueToUs * GameController.Instance.GameSettings.FactionSettings.TaxedTradeValueMultiplierForOpinionChange));
			}
			GiveTaxToControllingFaction(tradeStation, tradeValueToUs, transactionType, relatedCargoClass, relatedUnitClass, relatedCount);
		}

		private void GiveTaxToControllingFaction(Unit placeOfTrade, float tradeValueToUs, FactionTransactionType transactionType, CargoClass relatedCargoClass, UnitClass relatedUnitClass, int? relatedCount = null)
		{
			if (!(placeOfTrade == null))
			{
				Faction faction = placeOfTrade.GetFactionControllingSector();
				if (faction == null)
				{
					faction = placeOfTrade.Faction;
				}
				if (faction != null && faction != this)
				{
					float taxRate = ((transactionType == FactionTransactionType.ShipPurchase) ? EngineASX.Instance.EconomySettings.StationShipSaleTaxRate : EngineASX.Instance.EconomySettings.StationMiscTaxRate);
					FactionTransactionTaxType taxTypeFromTransactionType = GetTaxTypeFromTransactionType(transactionType);
					faction.ProvideTaxFromTransaction(tradeValueToUs, taxRate, this, placeOfTrade, taxTypeFromTransactionType, relatedCargoClass, relatedUnitClass, relatedCount);
				}
			}
		}

		private FactionTransactionTaxType GetTaxTypeFromTransactionType(FactionTransactionType transactionType)
		{
			return transactionType switch
			{
				FactionTransactionType.PassengerFare => FactionTransactionTaxType.PassengerFare, 
				FactionTransactionType.ShipPurchase => FactionTransactionTaxType.ShipSale, 
				FactionTransactionType.Trade => FactionTransactionTaxType.CargoSale, 
				_ => FactionTransactionTaxType.None, 
			};
		}

		public void ProvideTaxFromTransaction(float transactionValue, float taxRate, Faction otherFaction, Unit tradeLocation, FactionTransactionTaxType taxType, CargoClass relatedCargoClass = null, UnitClass relatedUnitClass = null, int? relatedCount = null)
		{
			float num = Mathf.Abs(transactionValue) * taxRate;
			if ((int)num > 0)
			{
				ApplyTransaction((int)num, FactionTransactionType.Tax, otherFaction, tradeLocation, relatedCargoClass, relatedUnitClass, taxType, relatedCount);
			}
		}

		private void ApplyTradeValueToAttitudes(Faction otherFaction, float tradeValue)
		{
			tradeValue /= 1000000f;
			float change = Mathf.Abs(tradeValue) * FactionSettings.TradeOpinionChangePerMillionCr;
			ChangeOpinion(otherFaction, change);
		}

		public void PowerUpStations()
		{
			foreach (Unit unit in units)
			{
				if (unit.UnitType == UnitType.Station)
				{
					unit.Components.SetComponentsPowered(powered: true);
				}
			}
		}

		private void Update()
		{
			if (!EngineASX.LoadedAndReady || !GameController.Instance.GameSettings.DebugSettings.FactionUpdateEnabled)
			{
				return;
			}
			if ((IsPlayerFaction || DynamicFactionAttitudes) && Time.time > nextRecentAttacksChkTime)
			{
				UpdateRecentDamageReceived();
				if (!IsPlayerFaction)
				{
					UpdateHostilities();
				}
				nextRecentAttacksChkTime = Time.time + RecentDamageSettings.RecentAttacksDissipationChkRate;
			}
			IntelProcessor.Update();
		}

		private void UpdateHostilities()
		{
			if (relations.Count == 0)
			{
				return;
			}
			hostilityTimeoutCheckIndex++;
			if (hostilityTimeoutCheckIndex >= relations.Count)
			{
				hostilityTimeoutCheckIndex = 0;
			}
			FactionAttitude factionAttitude = relations[hostilityTimeoutCheckIndex];
			if (factionAttitude.TargetFaction != null && factionAttitude.IsHostile && !factionAttitude.RestrictHostilityTimeout && engine.ScenarioElapsedTime > factionAttitude.HostilityEndTime && factionAi != null && factionAi.RequestEnforcePeace(factionAttitude))
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"{this}: Made peace with {factionAttitude.TargetFaction} because hostility end time is reached. Current opinion: {factionAttitude.Opinion}", this, 1);
				}
				EngineASX.Instance.DebugInfo.NumTimesFactionAIPeaceTreatiesSigned++;
			}
		}

		private void UpdateRecentDamageReceived()
		{
			if (recentDamageReceived.Count == 0)
			{
				return;
			}
			recentDamageProcessQueue.Clear();
			foreach (KeyValuePair<int, FactionRecentDamage> item in recentDamageReceived)
			{
				recentDamageProcessQueue.Enqueue(item.Key);
			}
			while (recentDamageProcessQueue.Count > 0)
			{
				int key = recentDamageProcessQueue.Dequeue();
				if (!recentDamageReceived.TryGetValue(key, out var value) || value.TargetFaction == null || !value.TargetFaction.IsValidInGame)
				{
					continue;
				}
				float num = value.RecentDamageReceived;
				if (!(num > 0f))
				{
					continue;
				}
				GetOrCreateAttitude(value.TargetFaction);
				if (num > value.lastAttackOpinionChangeRecentAttacks)
				{
					num = (value.RecentDamageReceived = num + RecentDamageSettings.MinDamagePerAttack);
					if (!IsPlayerFaction)
					{
						SetHostileIfRecentAttackThresholdBreach(value.TargetFaction, value.RecentDamageReceived);
						float change = (0f - (num - value.lastAttackOpinionChangeRecentAttacks)) * FactionSettings.OpinionChangeBasedOnRecentDamage;
						ChangeOpinion(value.TargetFaction, change);
					}
				}
				else if (Time.time - value.TimeOfLastDamageReceived > RecentDamageSettings.RecentAttacksTimeBeforeDissipation)
				{
					DissipateRecentDamageReceived(value);
					if (value.RecentDamageReceived <= 0f)
					{
						recentDamageReceived.Remove(key);
					}
				}
				if (value.RecentDamageReceived > EngineASX.Instance.GameSettings.FactionSettings.FactionRecentDamageSettings.RecentAttacksMaxValue)
				{
					value.RecentDamageReceived = EngineASX.Instance.GameSettings.FactionSettings.FactionRecentDamageSettings.RecentAttacksMaxValue;
				}
				value.lastAttackOpinionChangeRecentAttacks = value.RecentDamageReceived;
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"{Name}: I've had damage from {value.TargetFaction.Name}. My new opinion of him: {GetOpinion(value.TargetFaction)}", this, 2);
				}
			}
		}

		public void UpdateGeneratedName()
		{
			engine.FactionNames.SetNameAndShortNameFromGeneratedValues(this);
		}

		private void DissipateRecentDamageReceived(FactionRecentDamage recentDamage)
		{
			recentDamage.RecentDamageReceived -= RecentDamageSettings.RecentAttacksDissipationRatePerSecond * RecentDamageSettings.RecentAttacksDissipationChkRate;
		}

		private FactionAttitude CreateNewAttitude(Faction faction)
		{
			if (relationMap.ContainsKey(faction.UniqueId))
			{
				Debug.LogErrorFormat(this, "Faction \"{0}\" - already has attitude with faction {1}", this, faction);
				return null;
			}
			if (faction.engine == null)
			{
				Debug.LogErrorFormat(this, "Faction \"{0}\" - attempting to create attitude with uninitialised faction: \"{1}\"", this, faction);
			}
			if (faction == this)
			{
				Debug.LogError($"Faction \"{this}\" cannot set attitude with itself", this);
				return null;
			}
			FactionAttitude factionAttitude = new FactionAttitude();
			factionAttitude.CreatedTime = EngineASX.Instance.ScenarioElapsedTime;
			factionAttitude.TargetFaction = faction;
			relationMap[faction.UniqueId] = factionAttitude;
			relations.Add(factionAttitude);
			if (factionAi != null)
			{
				factionAi.OnNewAttitudeWithFaction(faction, factionAttitude);
			}
			return factionAttitude;
		}

		private float GetRecentAttacksThreshold(float opinion)
		{
			return RecentDamageSettings.RecentAttacksThreshold + RecentDamageSettings.RecentAttacksOpinionAddition * opinion - Aggression * RecentDamageSettings.RecentAttacksAgressionFactor;
		}

		/// <summary>
		/// Whether taking damage from that faction should not start a war.
		/// </summary>
		/// <remarks>
		/// Two people shooting each other is not a war between their
		/// countries. If it were, the first exchange of fire would drag every
		/// AI ship both of them own into it - which is not what a player who
		/// shoots at another player asked for, and is not recoverable by
		/// asking nicely afterwards.
		///
		/// So between two player factions, hostilities begin when a player
		/// says so, and not before. Everything else about the relationship is
		/// untouched: they are still not allied, their ships still defend
		/// themselves, and the damage still counts toward the opinion. Only
		/// the automatic declaration is withheld.
		///
		/// One check in one place, at the point where damage becomes a war.
		/// Everywhere else that decides hostility is asking whether the player
		/// meant it, which is the only question that can answer it.
		///
		/// False in a game with nobody else in it, so nothing changes for a
		/// singleplayer world - both sides fail the test for the same reason.
		/// </remarks>
		private bool TwoPlayersAreShootingAtEachOther(Faction damageInflictorFaction)
		{
			return PlayerFactions.IsPlayerFaction(UniqueId)
				&& PlayerFactions.IsPlayerFaction(damageInflictorFaction.UniqueId);
		}

		private bool SetHostileIfRecentAttackThresholdBreach(Faction damageInflictorFaction, float damageReceived)
		{
			if (damageInflictorFaction == this)
			{
				throw new ArgumentException($"{this}: Trying to examine attacks from same faction: ");
			}
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: Inspecting recent attacks.. Damage Received: {damageReceived}.", this, 2);
			}
			FactionAttitude attitude = GetAttitude(damageInflictorFaction);
			if (!attitude.PermanentPeace)
			{
				if (!attitude.IsHostile)
				{
					float recentAttacksThreshold = GetRecentAttacksThreshold(attitude.Opinion);
					if (damageReceived > recentAttacksThreshold
						&& !TwoPlayersAreShootingAtEachOther(damageInflictorFaction)
						&& (factionAi == null || factionAi.RequestPermissionToDeclareWarOn(damageInflictorFaction, isBeingAttacked: true)))
					{
						if (LogWrapper.LogMsgs)
						{
							LogWrapper.Log($"{this}: Damage threshold reached. Damage: {damageReceived} from {damageInflictorFaction}", this, 1);
						}
						FactionWarDeclarationsController factionWarDeclarationsController = new FactionWarDeclarationsController();
						FactionWarDeclaration factionWarDeclaration = factionWarDeclarationsController.CreateWarDeclaration(this, damageInflictorFaction, FactionWarMotivation.UnderAttack);
						factionWarDeclarationsController.ApplyWarDeclaration(factionWarDeclaration);
						if (people.Count > 0)
						{
							GenerateNotificationsForWar(damageInflictorFaction, factionWarDeclaration);
						}
						return true;
					}
				}
				if (attitude.IsHostile)
				{
					UpdateHostilityCoolDownTime(attitude, attitude.Opinion);
					if (LogWrapper.LogMsgs)
					{
						LogWrapper.Log($"{this}: extending hostilities with ({damageInflictorFaction}) to {attitude.HostilityEndTime - engine.ScenarioElapsedTime} seconds.", this, 2);
					}
				}
			}
			return false;
		}

		private void GenerateNotificationsForWar(Faction damageInflictorFaction, FactionWarDeclaration warDeclaration)
		{
			if (damageInflictorFaction.IsPlayerFaction)
			{
				FactionWarDeclarationMessageGenerator.GenerateAndSendForWarDeclaredOnPlayer(warDeclaration);
			}
			else if (EngineASX.Instance.LocalFaction != null && ShouldShowPlayerNotificationForNpcWar(warDeclaration))
			{
				FactionWarDeclarationMessageGenerator.GenerateAndSendForWarDeclaredBetweenNpcs(warDeclaration);
			}
		}

		private bool ShouldShowPlayerNotificationForNpcWar(FactionWarDeclaration warDeclaration)
		{
			if (warDeclaration.Aggressor.IsFreelancer || warDeclaration.Defender.IsFreelancer)
			{
				return false;
			}
			if (!warDeclaration.Aggressor.IsAIFactionType || !warDeclaration.Defender.IsAIFactionType)
			{
				return false;
			}
			if (warDeclaration.Aggressor.AISettings == null)
			{
				Debug.LogError($"Expecting NPC faction (aggressor) {warDeclaration.Aggressor} to have AI Settings", this);
				return false;
			}
			if (warDeclaration.Defender.AISettings == null)
			{
				Debug.LogError($"Expecting NPC faction (defender) {warDeclaration.Defender} to have AI Settings", this);
				return false;
			}
			if (warDeclaration.Aggressor.AISettings.HostileWithAll || warDeclaration.Defender.AISettings.HostileWithAll || warDeclaration.Aggressor.FactionType == FactionType.Bandit || warDeclaration.Defender.FactionType == FactionType.Bandit)
			{
				return false;
			}
			if (!EngineASX.Instance.LocalFaction.HasAttitudeToFaction(warDeclaration.Aggressor) || !EngineASX.Instance.LocalFaction.HasAttitudeToFaction(warDeclaration.Defender))
			{
				return false;
			}
			return true;
		}

		public void UpdateHostilityCoolDownTime(FactionAttitude attitude, float currentOpinion)
		{
			float hostilityCoolDownTime = GetHostilityCoolDownTime(currentOpinion);
			attitude.HostilityEndTime = engine.ScenarioElapsedTime + (double)hostilityCoolDownTime;
		}

		public List<Unit> GetUnitsByType(UnitType unitType)
		{
			List<Unit> value = null;
			if (unitsByType.TryGetValue(unitType, out value))
			{
				return value;
			}
			return null;
		}

		public int GetCountOfUnitType(UnitType unitType)
		{
			return GetUnitsByType(unitType)?.Count ?? 0;
		}

		public void HandleCargoStolen(Faction thievingFaction, Unit theivingUnit, int cargoValue)
		{
			if (factionAi != null)
			{
				factionAi.HandleCargoStolen(thievingFaction, theivingUnit, cargoValue);
			}
			if (!(thievingFaction != null))
			{
				return;
			}
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: Cargo stolen by {thievingFaction} Unit: {theivingUnit}. Applying damage...", this, 2);
			}
			if (theivingUnit.IsActiveInEngine)
			{
				Person pilotNearbyToPosition = GetPilotNearbyToPosition(theivingUnit.transform.position);
				if (pilotNearbyToPosition != null && !pilotNearbyToPosition.IsInCombat())
				{
					pilotNearbyToPosition.RaiseDialogEventRandomly(EngineASX.Instance.DialogEvents.CargoStolenByOther, 0.5f);
				}
			}
		}

		private Person GetPilotNearbyToPosition(Vector3 position, float radius = 500f)
		{
			int num = Physics.OverlapSphereNonAlloc(position, radius, EngineASX.ColliderCache, GameController.Instance.ShipsMask);
			for (int i = 0; i < num; i++)
			{
				Unit component = EngineASX.ColliderCache[i].GetComponent<Unit>();
				if (component != null && component.Faction == this && !component.Components.IsDocked && !component.Components.IsCloaked && component.Components.PilotPerson != null)
				{
					return component.Components.PilotPerson;
				}
			}
			return null;
		}

		public void OnUnitScanned(Unit scannedUnit, Unit scanningShip)
		{
			if (factionAi != null)
			{
				factionAi.OnUnitScanned(scannedUnit, scanningShip);
			}
		}

		[ContextMenu("AutoName GameObject")]
		public void AutoNameGameObject()
		{
			string text = $"Faction_{UniqueId}_";
			string text2 = GetShortNameElseLong();
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = "Unnamed";
			}
			text2 = text2 + "_" + Enum.GetName(typeof(FactionType), FactionType);
			if (engine != null && IsPlayerFaction)
			{
				text2 += "_Player";
			}
			gameObject.name = text + text2;
		}

		[ContextMenu("Discover all factions")]
		public void DiscoverAllFactions()
		{
			engine.Factions.TrimNulls();
			foreach (Faction faction in engine.Factions)
			{
				if (faction != this)
				{
					GetOrCreateAttitude(faction);
					faction.GetOrCreateAttitude(this);
				}
			}
		}

		public void ApplyTransaction(int creditsValue, FactionTransactionType transactionType, Faction otherFaction = null, Unit location = null, CargoClass relatedCargoClass = null, UnitClass relatedUnitClass = null, FactionTransactionTaxType taxType = FactionTransactionTaxType.None, int? relatedCount = null, ICreditsSource creditsSource = null)
		{
			if (creditsSource == null)
			{
				creditsSource = this;
			}
			if (creditsValue == 0)
			{
				return;
			}
			creditsSource.Credits += creditsValue;
			if (IsPlayerFaction && this == creditsSource)
			{
				FactionTransaction item = new FactionTransaction
				{
					CurrentBalance = Credits,
					TransactionType = transactionType,
					Location = location,
					OtherFaction = otherFaction,
					Value = creditsValue,
					RelatedCargoClass = relatedCargoClass,
					RelatedUnitClass = relatedUnitClass,
					TaxType = taxType,
					RelatedCount = relatedCount
				};
				recentTransactions.Insert(0, item);
				int num = recentTransactions.Count - engine.GameSettings.NumberOfFactionTransactionsHistory;
				if (num > 0)
				{
					recentTransactions.RemoveRange(engine.GameSettings.NumberOfFactionTransactionsHistory, num);
				}
			}
			if (Stats != null)
			{
				if (creditsValue > 0)
				{
					Stats.TotalRevenue += creditsValue;
				}
				else
				{
					Stats.TotalExpenditure += Mathf.Abs(creditsValue);
					if (transactionType == FactionTransactionType.EquipmentPurchase && location != null && location.UnitType == UnitType.Station)
					{
						Stats.TotalExpenditureOnStationUpgrades += Mathf.Abs(creditsValue);
					}
				}
			}
			lastTransactionTime = Time.time;
		}

		public float GetRecentDamageFrom(Faction targetFaction)
		{
			return GetRecentDamageItem(targetFaction)?.RecentDamageReceived ?? 0f;
		}

		public string GetShortFriendlyName()
		{
			if (FactionType == FactionType.Bar)
			{
				if (!string.IsNullOrEmpty(ShortName))
				{
					return "\"" + ShortName + "\"";
				}
				if (!string.IsNullOrEmpty(Name))
				{
					return "\"" + Name + "\"";
				}
			}
			return GetShortNameElseLong();
		}

		public string GetFriendlyName(bool shortName = false)
		{
			if (FactionType == FactionType.Bar)
			{
				if (!shortName || string.IsNullOrWhiteSpace(ShortName))
				{
					return "\"" + Name + "\"";
				}
				return "\"" + ShortName + "\"";
			}
			if (!shortName)
			{
				return GetLongNameElseShort();
			}
			return GetShortNameElseLong();
		}

		public string GetShortNameElseLong()
		{
			if (!string.IsNullOrEmpty(ShortName))
			{
				return ShortName;
			}
			if (!string.IsNullOrEmpty(Name))
			{
				return Name;
			}
			return "Unknown";
		}

		public string GetLongNameElseShort()
		{
			if (!string.IsNullOrEmpty(Name))
			{
				return Name;
			}
			if (!string.IsNullOrEmpty(ShortName))
			{
				return ShortName;
			}
			return "Unknown";
		}

		public float MarkupPriceMultiplier(TradeType tradeType, float priceMultiplier)
		{
			EngineEconomySettings economySettings = Engine.EconomySettings;
			if (economySettings.ApplyTraderMarkup)
			{
				float num = Mathf.Lerp(economySettings.TraderMarkupLower, economySettings.TraderMarkupUpper, Greed);
				priceMultiplier -= (float)tradeType * num;
				return priceMultiplier;
			}
			return priceMultiplier;
		}

		public int GetMarkedUpPrice(TradeType tradeType, float price)
		{
			float num = MarkupPriceMultiplier(tradeType, 1f);
			return Mathf.RoundToInt(price * num);
		}

		public int GetMarkedUpPriceAfterOpinionChange(TradeType tradeType, float price, Faction otherTrader, float priceMultiplier = 1f)
		{
			if (otherTrader != this)
			{
				float priceMultiplier2 = MarkupPriceMultiplier(tradeType, priceMultiplier);
				priceMultiplier2 = ApplyFactionOpinionToPriceMultiplier(tradeType, otherTrader, priceMultiplier2);
				return Mathf.RoundToInt(price * priceMultiplier2);
			}
			return Mathf.RoundToInt(price);
		}

		public float ApplyFactionOpinionToPriceMultiplier(TradeType tradeType, Faction otherTrader, float priceMultiplier)
		{
			if (otherTrader != null && otherTrader != this)
			{
				EngineEconomySettings economySettings = Engine.EconomySettings;
				if (economySettings.ApplyGoodRelationsMarkup)
				{
					float opinionPriceMultiplierChange = GetOpinionPriceMultiplierChange(tradeType, otherTrader, economySettings);
					priceMultiplier += opinionPriceMultiplierChange;
					if (priceMultiplier < 0f)
					{
						priceMultiplier = 0f;
					}
				}
			}
			return priceMultiplier;
		}

		public float GetOpinionPriceMultiplierChange(TradeType tradeType, Faction otherTrader, EngineEconomySettings ec)
		{
			float opinion = GetOpinion(otherTrader);
			return GetOpinionPriceMultiplierChange(tradeType, ec, opinion);
		}

		public static float GetOpinionPriceMultiplierChange(TradeType tradeType, EngineEconomySettings ec, float opinion)
		{
			float num = Mathf.Pow(opinion, ec.CargoGoodOpinionPower) * Mathf.Sign(opinion);
			if (tradeType == TradeType.Sell)
			{
				num *= -1f;
			}
			return num * ec.CargoGoodOpinionMultiplier;
		}

		public FactionBountyBoard FindOrCreateBountyBoard()
		{
			if (bountyBoard == null)
			{
				FindBountyBoard();
				if (bountyBoard == null)
				{
					BountyBoard = gameObject.AddComponent<FactionBountyBoard>();
				}
			}
			return bountyBoard;
		}

		public void FindBountyBoard()
		{
			if (bountyBoard == null)
			{
				BountyBoard = GetComponentInChildren<FactionBountyBoard>();
			}
		}

		public long GetCachedNetWorth()
		{
			if (IsNetWorthStale())
			{
				UpdateNetWorth();
			}
			return cachedNetWorth;
		}

		[ContextMenu("Cache Net Worth")]
		public void UpdateNetWorth()
		{
			cachedNetWorth = CalculateNetWorth();
			lastCachedNetWorthTime = Time.time;
		}

		private bool IsNetWorthStale()
		{
			if (lastCachedNetWorthTime != 0f)
			{
				return Time.time - lastCachedNetWorthTime > 30f;
			}
			return true;
		}

		public void OnGroupObjectiveTimeout(Fleet group, ActiveFleetOrder objective)
		{
			if (factionAi != null)
			{
				factionAi.OnGroupObjectiveTimeout(group, objective);
			}
		}

		public void RecordStartingStats()
		{
			if (Stats != null)
			{
				Stats.StartingCredits = credits;
				Stats.StartingNetWorth = GetCachedNetWorth();
				Stats.StartingShipCount = GetCountOfUnitType(UnitType.Ship);
				Stats.StartingStationCount = GetCountOfUnitType(UnitType.Station);
				Stats.StartingVirtue = Virtue;
			}
		}

		public int RecalculateCreditsReserve()
		{
			int num = GetBaseCreditsReserve();
			foreach (Unit unit in units)
			{
				num += unit.UnitClass.AICreditsReserve;
			}
			return num;
		}

		private int GetBaseCreditsReserve()
		{
			if (IsFreelancer && FactionType == FactionType.Trader)
			{
				return 5000;
			}
			return 0;
		}

		public void SetSpawnTimeToCurrent()
		{
			spawnTime = engine.ScenarioElapsedTime;
		}

		public void UpdateHighestEverNetWorth()
		{
			if (cachedNetWorth > highestEverNetWorth)
			{
				highestEverNetWorth = cachedNetWorth;
			}
		}

		public static float ClampOpinion(float value)
		{
			if (float.IsNaN(value))
			{
				return 0f;
			}
			return Mathf.Clamp(value, -1f, 1f);
		}

		public List<CargoTrader> GetValidTraderTargets()
		{
			RecacheValidTradeTargetIfNeeded();
			return validTraderTargets;
		}

		public void RecacheValidTradeTargetIfNeeded()
		{
			if (!lastValidTraderTargetCacheTime.HasValue || Time.time - lastValidTraderTargetCacheTime > GameController.Instance.GameSettings.PerformanceSettings.FactionTraderTargetCacheMaxAge)
			{
				RecacheValidTraderTargets();
			}
		}

		public void InvalidateTraderTargets()
		{
			lastValidTraderTargetCacheTime = float.MinValue;
		}

		public void RecacheValidTraderTargets()
		{
			validTargetTargetsFoundRefinery = false;
			validTraderTargets.Clear();
			FactionValidTradeTargetsController.Get(this, validTraderTargets, out validTargetTargetsFoundRefinery);
			lastValidTraderTargetCacheTime = Time.time;
		}

		public void CheckValidTraderTarget(Unit unit)
		{
			FactionValidTradeTargetsController.CheckValidTraderTarget(unit, this, validTraderTargets);
		}

		public void FindAndPickHomeSectorIfNull()
		{
			if (HomeSector == null)
			{
				ChangeHomeSector(PickHomeSectorFromOwnedUnits());
				if (HomeSector == null && factionTypeInfo != null)
				{
					ChangeHomeSector(FactionSpawner.GetBestHomeSectorForNewFactionType(factionTypeInfo, !IsFreelancer));
				}
				if (HomeSector == null)
				{
					Debug.LogWarningFormat(this, "{0}: Failed to pick home scene", this);
				}
			}
		}

		private Sector PickHomeSectorFromOwnedUnits()
		{
			return FactionHomeScenePicker.PickFromOwnedUnits(engine, this);
		}

		public float GetRelativeNetWorthTo(Faction faction)
		{
			return GetRelativeNetWorthTo(GetCachedNetWorth(), faction);
		}

		public static float GetRelativeNetWorthTo(long ourNetWorth, Faction faction)
		{
			long num = faction.GetCachedNetWorth();
			if (num == 0L)
			{
				return 100000f;
			}
			return (float)((double)ourNetWorth / (double)num);
		}

		public Sector CalculateNearestControlledSectorToHomeSector(out int jumpDistance)
		{
			jumpDistance = -1;
			if (HomeSector == null)
			{
				Debug.LogWarning("Home sector is null", this);
				return null;
			}
			if (HomeSector.ControllingFaction != null)
			{
				jumpDistance = 0;
				return HomeSector;
			}
			return HomeSector.CalculateNearestControlledSector(-1, out jumpDistance);
		}

		public Faction CalculateNearestControlledSectorFactionToHomeSector(out int jumpDistance)
		{
			jumpDistance = -1;
			if (HomeSector == null)
			{
				Debug.LogWarning("Home sector is null", this);
				return null;
			}
			if (CalculateNearestControlledSectorToHomeSector(out jumpDistance) != null)
			{
				return HomeSector.ControllingFaction;
			}
			return null;
		}

		public Faction GetNearestControlledSectorFactionToHomeSector()
		{
			if (IsNearestControlledSectorFactionStale())
			{
				CacheNearestControlledSectorFaction();
				lastCachedNearestControlledSectorFaction = Time.time;
			}
			return cachedNearestControlledSectorFaction;
		}

		private void CacheNearestControlledSectorFaction()
		{
			cachedNearestControlledSectorFaction = CalculateNearestControlledSectorFactionToHomeSector(out var _);
		}

		private bool IsNearestControlledSectorFactionStale()
		{
			if (lastCachedNearestControlledSectorFaction != 0f)
			{
				return Time.time - lastCachedNearestControlledSectorFaction > 60f;
			}
			return true;
		}

		public FactionBountyBoard GetNearestBountyBoardToHomeSector()
		{
			if (HomeSector == null)
			{
				return null;
			}
			Faction nearestControlledSectorFactionToHomeSector = GetNearestControlledSectorFactionToHomeSector();
			if (nearestControlledSectorFactionToHomeSector == null)
			{
				return null;
			}
			return nearestControlledSectorFactionToHomeSector.BountyBoard;
		}

		public void ChangeHomeSector(Sector newSector)
		{
			if (homeSector != newSector)
			{
				Sector sector = homeSector;
				homeSector = newSector;
				if (sector != null)
				{
					sector.RemoveHeadquarateredFaction(this);
				}
				if (homeSector != null)
				{
					homeSector.AddHeadquarateredFaction(this);
					Intel.DiscoverSector(homeSector);
				}
				if (sector != null && homeSector != null && LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Faction {this} changing home sector from {sector} to {homeSector}", this, 1);
				}
			}
		}

		public void CaptureUnit(Unit unit, bool silent = false)
		{
			if (unit.Faction == this)
			{
				return;
			}
			if (unit.Faction != null)
			{
				if (unit.Faction == this)
				{
					Debug.LogError("Should not be capturing owned unit", this);
					return;
				}
				if (unit.IsStationOrShip())
				{
					unit.Components.CaptureCooldownTime = EngineASX.Instance.ScenarioElapsedTime + GameController.Instance.GameSettings.GameplaySettings.TimeBeforeUnitRecapture;
					unit.Components.AutoTurretFireCooldownTime = Time.time + GameController.Instance.GameSettings.GameplaySettings.CapturedUnitAutoTurretCooldownTime;
					if (unit.Components.HangarComponent != null)
					{
						unit.Components.UndockAllDockedUnits();
					}
					if (unit.Components.PilotPerson != null)
					{
						unit.Components.PilotPerson = null;
					}
					if (unit.Components.Crew.Count > 0 && IsHostileTo(unit))
					{
						Person[] array = unit.Components.Crew.ToArray();
						for (int i = 0; i < array.Length; i++)
						{
							array[i].TryKill();
						}
					}
				}
				switch (unit.UnitType)
				{
				case UnitType.Ship:
					EngineASX.Instance.DebugInfo.NumShipsCaptured++;
					break;
				case UnitType.Station:
					EngineASX.Instance.DebugInfo.NumStationsCaptured++;
					break;
				default:
					Debug.LogError($"Not supported: Capturing unit of type: {unit.UnitType}");
					break;
				case UnitType.Cargo:
					break;
				}
			}
			if (unit.Destructable != null && unit.Destructable.HealthNormalized < 0.3f)
			{
				unit.Destructable.CurrentHealth += 0.2f * unit.UnitClass.maxHealth;
				unit.Destructable.ClampHealth();
			}
			Faction faction = unit.Faction;
			unit.Faction = this;
			EngineASX.Instance.NotifyUnitCaptured(unit, faction, silent);
		}

		public void RemoveAllPlacedBounty()
		{
			BountyHelper.RemoveBountiesPlacedByFaction(this);
		}

		public void RemoveRankingSystemCompletely()
		{
			PilotRankingSystem = null;
			foreach (Person person in people)
			{
				if (person != null)
				{
					person.ChangeRank(null);
				}
			}
		}

		public void OnIntelDatabaseChanged()
		{
			Intel.ClearCachedUniversePaths();
			InvalidateTraderTargets();
		}

		public bool RequestCapture(Unit unit)
		{
			if (factionAi != null)
			{
				return factionAi.RequestCapture(unit);
			}
			return true;
		}

		public void ChangeVirtue(float newVirtue)
		{
			if (!float.IsNaN(newVirtue))
			{
				Virtue = Mathf.Clamp01(newVirtue);
			}
		}

		public void RequestHelpAgainstAttackingFaction(FactionAIBase factionAIRequestor, Faction attackingFaction, Sector attackSector)
		{
			if (factionAi != null)
			{
				factionAi.ProcessRequestForHelp(factionAIRequestor, attackingFaction, attackSector);
			}
		}
	}
}
