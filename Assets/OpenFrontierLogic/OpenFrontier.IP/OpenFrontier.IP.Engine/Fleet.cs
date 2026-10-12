using System;
using System.Collections.Generic;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Core;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.IP.Engine.Dialog;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets;
using OpenFrontier.IP.Engine.Fleets.FleetFormations;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.Engine.Npcs;
using OpenFrontier.IP.Engine.Pathfinding;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.Engine
{
	public class Fleet : SectorObject
	{
		public delegate void AllObjectivesRemovedHandler(Fleet sender);

		public delegate void AllPilotsRemovedHandler(Fleet sender);

		private float lastTimeActiveOrderChanged = -100f;

		internal float rearmCooldown;

		private FleetFormation fleetFormation;

		private FleetTargetScanner targetScanner;

		public FactionStrategy FleetStrategy;

		private bool isArmed;

		private float lastCachedHealthStatsTime = -100f;

		private float lastCachedHealth = -1f;

		private float lastCachedMaxHealth = -1f;

		private float lastCachedShieldHealth = -1f;

		private float lastCachedMaxShieldHealth = -1f;

		private float cachedTotalCargoCapacity;

		private float lastTimeCachedTotalCargoCapacity = float.MinValue;

		private float cachedTradableCargoValue;

		private float cachedTradableCargoLoad;

		private float cachedFreeCargoCapacity;

		private float cachedIncompatibleEquipmentValue;

		private float cachedIncompatibleEquipmentLoad;

		private float cachedCompatibleEquipmentLoad;

		private float lastTimeCachedCargoUsageStats = float.MinValue;

		public string Name;

		public int Seed = -1;

		private string designation;

		private string shortDesignation;

		internal float lastAiIssuedOrderTime = float.MinValue;

		private List<ShipHullType> npcShipHullTypes = new List<ShipHullType>();

		public bool ExcludeFromFactionAI;

		private const float regroupCheckFrequency = 4f;

		private float nextRegroupCheck;

		private static List<CargoClass> cargoBayClassesCache = new List<CargoClass>();

		private float offsetFromJumpgate;

		private bool canAllUnitsCloak;

		public FleetSettings Settings;

		public const int MaxFleetPilots = 8;

		public float? LastTimeoutTime;

		private const float minSpdMultiplier = 1.2f;

		private const float combatInterceptionTimeoutTime = 90f;

		private const float regroupTimeout = 60f;

		private float lastGroupMinMoveSpeedCalculation;

		private float minMoveSpeed = -1f;

		private Vector3 wormholeExitSectorPosition = Vector3.zero;

		private Quaternion wormholeExitRotation = Quaternion.identity;

		private float cumulativeMovementSinceLastIdle;

		private float cumulativeMovement;

		private float lastTimeCheckMovementPerSecond;

		private float movementPerSecond;

		private ActiveFleetOrder activeOrder;

		private float lastTimeInCombat;

		private float combatInterceptionTimeout;

		private List<NpcPilot> pilots = new List<NpcPilot>(10);

		private List<UnitComponentHolder> npcShips = new List<UnitComponentHolder>(10);

		private FleetState curState;

		private EngineASX engine;

		private Wormhole enteringGate;

		[SerializeField]
		private Faction faction;

		private float movementSpeed;

		private bool hasInit;

		private SectorTarget homeBase;

		public const float NpcCargoCollectScore_DontCare = 0f;

		private bool inCombat;

		private float lastUpdate;

		private NpcPilot leader;

		private const float jumpSpdRefValue = 50f;

		private float nextOrderPathFind;

		private float nextUpdateMoveSpeedTime;

		private float cachedCombatRating;

		public const float UpdateMoveSpeedIntervalInactive = 3f;

		public const float UpdateMoveSpeedIntervalActive = 1f;

		[FormerlySerializedAs("ObjectiveQueue")]
		public List<FleetOrder> OrderQueue = new List<FleetOrder>(3);

		private float preferredSpeed;

		private float regroupCoolDownTime;

		private AIObjectiveTarget navTarget = new AIObjectiveTarget();

		public int UniqueId = -1;

		public bool IsMovingSignificantly => movementPerSecond > 1f;

		public float ActualMovementPerSecond => movementPerSecond;

		public Unit HomeBaseUnit
		{
			get
			{
				if (homeBase != null && homeBase.HadSceneObject && homeBase.TargetUnit != null)
				{
					return homeBase.TargetUnit;
				}
				return null;
			}
		}

		public Sector HomeSector
		{
			get
			{
				if (homeBase != null && homeBase.IsValid())
				{
					return homeBase.GetTargetSector();
				}
				return null;
			}
		}

		public Sector HomeSectorOrFactionHomeSector
		{
			get
			{
				Sector homeSector = HomeSector;
				if (homeSector == null)
				{
					return faction.HomeSector;
				}
				return homeSector;
			}
		}

		public Vector3 HomeSectorPosition
		{
			get
			{
				if (homeBase != null && homeBase.IsValid())
				{
					return homeBase.GetTargetSectorPosition();
				}
				return Vector3.zero;
			}
		}

		public ActiveFleetOrder ActiveOrder
		{
			get
			{
				return activeOrder;
			}
			set
			{
				if (!(activeOrder != value))
				{
					return;
				}
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"{this}: Changing order from {activeOrder} to {value}", this, 2);
				}
				ClearTarget();
				if (curState == FleetState.MoveToTarget)
				{
					CurState = FleetState.Idle;
				}
				ClearCombatInterception();
				ActiveFleetOrder activeFleetOrder = activeOrder;
				activeOrder = value;
				lastTimeActiveOrderChanged = Time.time;
				if (activeFleetOrder != null)
				{
					activeFleetOrder.SafeDestroy();
				}
				if (activeOrder != null)
				{
					if (activeOrder.FleetOrder.UniqueId < 0)
					{
						Debug.LogWarning("Fleet order has not been initialized", activeOrder.FleetOrder);
					}
					NpcPilotsControlShips();
					OrderQueue.Remove(activeOrder.FleetOrder);
					activeOrder.Engine = engine;
					activeOrder.Fleet = this;
					activeOrder.StartTime = engine.ScenarioElapsedTime;
				}
				else if (!HasAnyOrders && AllObjectivesRemoved != null)
				{
					AllObjectivesRemoved(this);
				}
			}
		}

		public float LastTimeActiveOrderChanged => lastTimeActiveOrderChanged;

		public float MoveSpeed => movementSpeed;

		public bool InCombat
		{
			get
			{
				return inCombat;
			}
			private set
			{
				if (inCombat != value)
				{
					inCombat = value;
					if (LogWrapper.LogMsgs)
					{
						LogWrapper.Log($"{this}: Changed InCombat - {inCombat}", this, 2);
					}
				}
			}
		}

		public float PreferredSpeed => preferredSpeed;

		public Unit LeaderUnit
		{
			get
			{
				_ = leader;
				if (leader != null)
				{
					return leader.CurrentUnit;
				}
				return null;
			}
		}

		public NpcPilot Leader
		{
			get
			{
				return leader;
			}
			set
			{
				if (leader != value)
				{
					leader = value;
					if (leader != null && (leader.Sector != Sector || Maths.GetDistanceIgnoreY(SectorPosition, leader.SectorPosition) > 2000f))
					{
						SetPositionToLeaderShipPosition();
					}
				}
			}
		}

		public bool HasHostileTargets => targetScanner.HostileTargets.Count > 0;

		public bool IsActive => true;

		public FleetState CurState
		{
			get
			{
				return curState;
			}
			internal set
			{
				if (curState == value)
				{
					return;
				}
				FleetState fleetState = curState;
				if (fleetState == FleetState.EnteringGate)
				{
					WormholeBeingEntered = null;
				}
				curState = value;
				if (curState != FleetState.MoveToTarget)
				{
					ClearTarget();
				}
				if (fleetState == FleetState.MoveToTarget)
				{
					navTarget.HasAttemptedToCalculatedPath = false;
				}
				if (fleetState == FleetState.CombatInterception)
				{
					SetPositionToLeaderShipPosition();
				}
				FleetState fleetState2 = curState;
				if (fleetState2 == FleetState.MoveToTarget || fleetState2 != FleetState.Regrouping)
				{
					return;
				}
				regroupCoolDownTime = Time.time + 60f;
				foreach (NpcPilot pilot in pilots)
				{
					pilot.MovingToFormation = true;
				}
			}
		}

		public List<NpcPilot> NpcPilots => pilots;

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
					engineASX.DeregisterFleet(this);
				}
				if (engine != null)
				{
					if (UniqueId < 0)
					{
						UniqueId = engine.GetUniqueFleetId();
					}
					engine.RegisterFleet(this);
				}
				else if (UniqueId >= 0)
				{
					UniqueId = -1;
				}
			}
		}

		public Faction Faction
		{
			get
			{
				return faction;
			}
			set
			{
				if (engine == null && value != null)
				{
					throw new Exception("Cannot assign fleet to faction without first initializing");
				}
				if (faction != value)
				{
					Faction oldFaction = faction;
					faction = value;
					OnFactionChanged(oldFaction);
				}
			}
		}

		public List<WorldNavpoint> Path => navTarget.waypoints;

		public List<AIGroupHostileTarget> HostileTargets => targetScanner.HostileTargets;

		public bool IsIdle
		{
			get
			{
				if (curState == FleetState.Idle)
				{
					return !navTarget.IsActive;
				}
				return false;
			}
		}

		public AIObjectiveTarget NavTarget => navTarget;

		public bool IsAboutToEnterGate
		{
			get
			{
				if (curState == FleetState.EnteringGate)
				{
					return true;
				}
				if (navTarget.IsActive && navTarget.waypoints.Count > 0 && navTarget.waypoints[0].TargetType == WorldNavpointTargetType.Gate)
				{
					return true;
				}
				return false;
			}
		}

		public bool IsUnderAttack => engine.FleetRecentAttacksLog.FleetRecentlyAttacked(this, 30f);

		public List<UnitComponentHolder> Ships => npcShips;

		public bool HasAnyOrders
		{
			get
			{
				if (!(activeOrder != null))
				{
					return OrderQueue.Count > 0;
				}
				return true;
			}
		}

		public bool IsMobile => true;

		public bool IdleAndNoObjectives
		{
			get
			{
				if (IsIdle)
				{
					return !HasAnyOrders;
				}
				return false;
			}
		}

		public override bool IsValid
		{
			get
			{
				if (engine != null && Sector != null)
				{
					return UniqueId > -1;
				}
				return false;
			}
		}

		public List<ShipHullType> NpcShipHullTypes => npcShipHullTypes;

		public float LastTimeInCombat
		{
			get
			{
				return lastTimeInCombat;
			}
			set
			{
				lastTimeInCombat = value;
			}
		}

		public Wormhole WormholeBeingEntered
		{
			get
			{
				return enteringGate;
			}
			set
			{
				if (enteringGate != value)
				{
					enteringGate = value;
					if (enteringGate != null)
					{
						UpdateRandomWormholeExitPosition(enteringGate);
					}
				}
			}
		}

		public bool RecentlyTimedOutObjective
		{
			get
			{
				if (LastTimeoutTime.HasValue)
				{
					return Time.time - LastTimeoutTime < 120f;
				}
				return false;
			}
		}

		public int PilotCount => pilots.Count;

		public override bool ShouldZeroPositionY => true;

		public Unit CurrentNavpointTargetUnit
		{
			get
			{
				if (CurState != FleetState.MoveToTarget)
				{
					return null;
				}
				if (navTarget.IsActive && navTarget.waypoints.Count > 0)
				{
					return navTarget.waypoints[0].TargetSectorObject as Unit;
				}
				return null;
			}
		}

		public bool IsArmed => isArmed;

		public bool HasHomeBase => homeBase != null;

		public SectorTarget HomeBase => homeBase;

		public bool IsHomeBaseValid
		{
			get
			{
				if (homeBase != null)
				{
					if (!homeBase.IsValid())
					{
						return false;
					}
					if (homeBase.TargetUnit != null)
					{
						return IsHomeBaseUnitValid(homeBase.TargetUnit, this);
					}
					return true;
				}
				return false;
			}
		}

		public override float ObjectRadius => VeryBasicFleetRadiusCalculation();

		public FleetTargetScanner TargetScanner => targetScanner;

		public FleetFormation FleetFormation
		{
			get
			{
				return fleetFormation;
			}
			set
			{
				fleetFormation = value;
			}
		}

		public bool IsOwnedByPlayer
		{
			get
			{
				if (faction != null)
				{
					return EngineASX.Instance.LocalFaction == faction;
				}
				return false;
			}
		}

		public event AllPilotsRemovedHandler AllPilotsRemoved;

		public event AllObjectivesRemovedHandler AllObjectivesRemoved;

		public float VeryBasicFleetRadiusCalculation()
		{
			if (Ships.Count == 1)
			{
				return Ships[0].Unit.Radius;
			}
			return (float)Ships.Count * EngineASX.Instance.GameSettings.DistanceSettings.VeryBasicFleetRadiusCalculationPerUnit;
		}

		private bool IsCargoOwnershipCompatible(CargoOwnership cargoOwnership, Faction ownerFaction)
		{
			if (faction.FactionAI != null)
			{
				if ((cargoOwnership == CargoOwnership.OwnedByOther || cargoOwnership == CargoOwnership.OwnedByHostile) && !faction.FactionAI.RequestStealCargo(ownerFaction, cargoOwnership))
				{
					return false;
				}
			}
			else if (cargoOwnership == CargoOwnership.OwnedByOther)
			{
				return false;
			}
			return true;
		}

		public float ScoreCargoToCollect(NpcPilot aIUnitController, Unit cargoUnit, CargoClass cargoClass, int availableQuantity, CargoOwnership cargoOwnership)
		{
			if (activeOrder != null)
			{
				float num = activeOrder.ScoreCargoToCollect(aIUnitController, cargoUnit, cargoClass, availableQuantity, cargoOwnership);
				if (num != 0f)
				{
					if (num == float.MaxValue || IsCargoOwnershipCompatible(cargoOwnership, cargoUnit.Faction))
					{
						return num;
					}
					return float.MinValue;
				}
			}
			if (!faction.WillTradeCargoType(cargoClass))
			{
				return float.MinValue;
			}
			if (!IsCargoOwnershipCompatible(cargoOwnership, cargoUnit.Faction))
			{
				return float.MinValue;
			}
			if (Settings.CargoCollectionPreference == FleetCargoCollectionPreference.Nothing)
			{
				return float.MinValue;
			}
			switch (FleetStrategy)
			{
			case FactionStrategy.Scavenge:
				return ScoreCargoCollectFromValue(cargoClass, availableQuantity);
			case FactionStrategy.Mine:
				if (!cargoClass.IsOre)
				{
					return 0f;
				}
				return 1f;
			case FactionStrategy.Trade:
				if (!cargoClass.IsTraded)
				{
					return 0f;
				}
				return 1f;
			case FactionStrategy.Escort:
				if (cargoClass.IsOre && cargoOwnership == CargoOwnership.OwnedByUs)
				{
					return float.MinValue;
				}
				break;
			}
			return 0f;
		}

		private float ScoreCargoCollectFromValue(CargoClass cargoClass, int availableQuantity)
		{
			if (!((float)(availableQuantity * cargoClass.BasePrice) > 3000f + (1f - faction.Greed) * 10000f))
			{
				return 0f;
			}
			return 1f;
		}

		internal void OnGroupObjectiveIimeout(ActiveFleetOrder aIActiveObjective)
		{
			LastTimeoutTime = Time.time;
			if (Faction != null)
			{
				Faction.OnGroupObjectiveTimeout(this, aIActiveObjective);
			}
		}

		internal void OnOrderCompleted(ActiveFleetOrder activeFleetOrder, bool silent)
		{
			if (!silent && OrderQueue.Count == 0 && activeFleetOrder.FleetOrder.CompletionMode == FleetOrderCompletionMode.Destroy)
			{
				SendPlayerCompletedMessage(activeFleetOrder);
			}
		}

		private void SendPlayerCompletedMessage(ActiveFleetOrder activeFleetOrder)
		{
			if (LeaderUnit != null && this.IsPlayerFaction() && Settings.NotifyWhenOrderComplete)
			{
				EngineASX.Instance.OnPlayerFleetCompletedOrder(activeFleetOrder);
			}
		}

		public float GetDistanceScorePerMetreTravelled()
		{
			return GetDistanceScorePerMetreTravelled(GetCachedMinMoveSpeed());
		}

		public float GetDistanceScorePerMetreTravelled(float groupMinSpd)
		{
			float num = engine.EconomySettings.AITradeSearchScorePerMetreTravelled;
			if (groupMinSpd < 50f)
			{
				num *= 1f + Mathf.Clamp01((50f - groupMinSpd) / 50f) * 2f;
			}
			return num;
		}

		private void Awake()
		{
			offsetFromJumpgate = UnityEngine.Random.Range(-30f, 30f);
		}

		private void ClearCombatInterception()
		{
			if (curState != FleetState.CombatInterception)
			{
				return;
			}
			CurState = FleetState.Idle;
			foreach (NpcPilot pilot in pilots)
			{
				pilot.NotifyFleetOutOfCombatInteceptionState();
			}
		}

		private void ClearStateAndTarget()
		{
			CurState = FleetState.Idle;
			ClearTarget();
		}

		public float GetInterceptScoreThresholdFromAggression()
		{
			return GetInterceptScoreThresholdFromAggression(Settings.Aggression);
		}

		public static float GetInterceptScoreThresholdFromAggression(float aggression)
		{
			return (1f - aggression) * EngineASX.Instance.GameSettings.NpcTargetSearchSettings.InterceptTargetScoreThreshold;
		}

		public float GetAttackTargetScoreThresholdFromAggression()
		{
			return GetAttackTargetScoreThresholdFromAggression(Settings.Aggression);
		}

		public static float GetAttackTargetScoreThresholdFromAggression(float aggression)
		{
			return (1f - aggression) * EngineASX.Instance.GameSettings.NpcTargetSearchSettings.AttackTargetScoreThreshold;
		}

		public void Init()
		{
			if (!hasInit)
			{
				AssignFleetFormationIfNull();
				targetScanner = new FleetTargetScanner(this);
				targetScanner.SetNextTargetScanTime();
				Engine = EngineASX.Instance;
				if (Seed < 0)
				{
					Seed = UnityEngine.Random.Range(0, int.MaxValue);
				}
				if (Settings == null)
				{
					Settings = gameObject.AddComponent<FleetSettings>();
				}
				if (Settings.TargetInterceptionUpperDistance <= 0f)
				{
					Settings.TargetInterceptionUpperDistance = EngineASX.Instance.GenericFleetPrefab.Settings.TargetInterceptionUpperDistance;
				}
				if (Settings.TargetInterceptionLowerDistance <= 0f)
				{
					Settings.TargetInterceptionLowerDistance = EngineASX.Instance.GenericFleetPrefab.Settings.TargetInterceptionLowerDistance;
				}
				lastUpdate = Time.time;
				hasInit = true;
				if (faction != null)
				{
					OnFactionChanged(null);
				}
			}
		}

		private void AssignFleetFormationIfNull()
		{
			if (fleetFormation == null)
			{
				fleetFormation = EngineASX.Instance.GetDefaultFleetFormation();
			}
		}

		private void Update()
		{
			if (IsInActiveSector)
			{
				Tick();
			}
		}

		public void Tick()
		{
			if (!EngineASX.LoadedAndReady || !GameController.Instance.GameSettings.DebugSettings.FleetUpdateEnabled || !(engine != null) || !(faction != null) || engine.IsPaused)
			{
				return;
			}

			// A copy that does not own the world does not run fleets.
			//
			// The gate on UnitComponentHolder.Tick() stops the ships, but a
			// Fleet is a MonoBehaviour in its own right and ticks from its own
			// Update - so orders, formation, target selection and the idle
			// state machine all carried on running on a client while the ships
			// they were supposed to drive stood still. Two copies of an order,
			// one of them authoritative, is the exact thing this whole
			// arrangement exists to prevent.
			//
			// Gated on being a client at all, rather than on who owns the
			// ships. The rule is that no fleet simulates on a copy which is not
			// the world. Gating per-ship would let a player's own fleet run
			// orders locally and fight the stick, because that ship IS owned
			// by the client while it is being flown by hand.
			//
			// It does not touch the autopilot handoff. That is claim and
			// release on a ship, and it is a different path entirely.
			if (Authority.IsThisCopyAClient)
			{
				Authority.Declined();

				return;
			}

			if (leader == null && pilots.Count > 0)
			{
				DesignateLeader();
			}
			float elapsedTime = Time.time - lastUpdate;
			lastUpdate = Time.time;
			UpdateMoveSpeed();
			targetScanner.Update(elapsedTime);
			UpdateIsInCombat();
			if (activeOrder != null)
			{
				activeOrder.Tick(elapsedTime);
			}
			SyncSectorWithLeader();
			switch (curState)
			{
			case FleetState.Idle:
				UpdateIdleFleet();
				break;
			case FleetState.CombatInterception:
				UpdateGroupCombatInterception();
				SetPositionToLeaderShipPosition();
				break;
			case FleetState.Regrouping:
				UpdateGroupRegrouping();
				break;
			case FleetState.MoveToTarget:
				if (activeOrder != null)
				{
					UpdateMoveToTargetState(elapsedTime);
				}
				else
				{
					CurState = FleetState.Idle;
				}
				break;
			case FleetState.EnteringGate:
				UpdateGroupEnteringGate();
				break;
			}
			if (Time.time > lastTimeCheckMovementPerSecond + 4f)
			{
				if (cumulativeMovement > 0f)
				{
					movementPerSecond = cumulativeMovement / (Time.time - lastTimeCheckMovementPerSecond);
				}
				else
				{
					movementPerSecond = 0f;
				}
				cumulativeMovement = 0f;
				lastTimeCheckMovementPerSecond = Time.time;
			}
		}

		private void UpdateRandomWormholeExitPosition(Wormhole enteringWormhole)
		{
			wormholeExitSectorPosition = enteringWormhole.GetSafeTargetSectorPositionWithExitDistanceMultiplier(2f);
			wormholeExitRotation = Quaternion.identity;
			if (enteringWormhole.TargetGate != null)
			{
				wormholeExitRotation = Quaternion.Euler(0f, enteringWormhole.TargetGate.transform.rotation.eulerAngles.y, 0f);
			}
			float num = WormholeBeingEntered.Unit.UnitClass.DisplayData.Radius * 1.5f;
			wormholeExitSectorPosition += wormholeExitRotation * Vector3.right * Mathf.Lerp(0f - num, num, UnityEngine.Random.value);
		}

		public Vector3 GetLocalFormationPositionForPilot(NpcPilot pilot)
		{
			if (activeOrder != null)
			{
				return pilot.FormationPosition * activeOrder.GetFormationScale();
			}
			return pilot.FormationPosition * GetDefaultFormationScale();
		}

		public float GetDefaultFormationScale()
		{
			return Mathf.Lerp(GameController.Instance.GameSettings.FormationSettings.MaxFormationScale, GameController.Instance.GameSettings.FormationSettings.MinFormationScale, Settings.FormationTightness);
		}

		public void HandleControllerReachedGroupNavpoint(NpcPilot controller)
		{
			switch (CurState)
			{
			case FleetState.EnteringGate:
				if (WormholeBeingEntered != null)
				{
					if (controller.IsFleetLeader && Sector != WormholeBeingEntered.ActualTargetSector)
					{
						SetPositionToWormholeExit(WormholeBeingEntered);
					}
					if (controller.Sector == WormholeBeingEntered.Sector)
					{
						if (Engine.ActiveSector != null && Engine.LocalPlayer != null && Engine.LocalPlayer.Person.CurrentUnit != null && Engine.LocalPlayer.Person.CurrentUnit.GetRootUnit() == controller.CurrentUnit)
						{
							Engine.TryStartSectorTransition(controller.CurrentUnit, WormholeBeingEntered, WormholeBeingEntered.ActualTargetSector);
						}
						else
						{
							controller.CurrentUnit.EnterWormhole(WormholeBeingEntered);
							if (controller.IsFleetLeader && Sector != WormholeBeingEntered.ActualTargetSector)
							{
								SetPositionToLeaderShipPosition();
							}
							controller.CurrentUnit.transform.localPosition = wormholeExitSectorPosition + wormholeExitRotation * Vector3.back * engine.GameSettings.JumpGateExitDistance + wormholeExitRotation * controller.FormationPosition;
							controller.CurrentUnit.UpdateGasCloud();
							if (controller.CurrentUnit.ActiveUnit != null)
							{
								controller.CurrentUnit.ActiveUnit.PropelFromWormhole();
							}
						}
					}
					if (activeOrder != null)
					{
						activeOrder.OnFleetEnteredWormhole(WormholeBeingEntered);
					}
				}
				else if (LogWrapper.LogMsgs)
				{
					Debug.LogErrorFormat(this, "{0}: Entering gate but no gate reference", this);
				}
				break;
			case FleetState.Regrouping:
				if (Leader.CurrentUnit.IsDocked)
				{
					if (controller.DockCooldownElapsed() && controller.CurrentUnitComponents.TryDockInHangar(Leader.CurrentUnitComponents.DockedInHangarBay.Hangar))
					{
						controller.SetDockCooldownTime();
						controller.MovingToFormation = false;
					}
				}
				else
				{
					controller.MovingToFormation = false;
				}
				break;
			case FleetState.MoveToTarget:
				if (navTarget.waypoints.Count == 1 && navTarget.waypoints[0].TargetType == WorldNavpointTargetType.Dock)
				{
					if (navTarget.waitingForLeaderToReachNavPoint && navTarget.TargetSectorObject is Unit unit && (controller.CurrentUnitComponents.DockUnit == unit || TryToDockControllerAtNavpoint(controller, unit)) && controller.IsFleetLeader)
					{
						NotifyLeaderReachedNavpoint();
					}
				}
				else if (controller.IsFleetLeader)
				{
					NotifyLeaderReachedNavpoint();
				}
				break;
			}
		}

		private void SetPositionToWormholeExit(Wormhole wormholeBeingEntered)
		{
			Sector = wormholeBeingEntered.ActualTargetSector;
			transform.localPosition = wormholeExitSectorPosition;
			transform.localRotation = wormholeExitRotation;
		}

		private bool TryToDockControllerAtNavpoint(NpcPilot controller, Unit dock)
		{
			if (dock == null || !dock.IsDockable)
			{
				return false;
			}
			if (controller.DockCooldownElapsed())
			{
				if (dock.Faction != faction && !dock.Faction.RequestDock(dock, faction))
				{
					activeOrder.OnNpcUnableToDockAtNavpoint(controller, dock, UnableToDockReason.Refused);
					return false;
				}
				if (dock.Components.HangarComponent != null)
				{
					if (dock.Components.HangarComponent.IsFull)
					{
						activeOrder.OnNpcUnableToDockAtNavpoint(controller, dock, UnableToDockReason.DockingBaysFull);
						if (faction != null)
						{
							faction.RemoveValidTraderTarget(dock);
						}
						EngineASX.Instance.ExcludeUnitFromNpcTraderTargets(dock);
						return false;
					}
					if (controller.CurrentUnitComponents.TryDockInUnit(dock))
					{
						controller.SetDockCooldownTime();
						return true;
					}
				}
				if (activeOrder != null)
				{
					activeOrder.OnNpcUnableToDockAtNavpoint(controller, dock, UnableToDockReason.Other);
				}
			}
			return false;
		}

		private void UpdateIdleFleet()
		{
			if (pilots.Count <= 0)
			{
				return;
			}
			if (activeOrder == null && pilots.Count > 0 && Sector != null && GameController.Instance.GameSettings.DebugSettings.AIGroupAutoObjectiveAssignEnabled)
			{
				AssignNextOrder();
			}
			if (navTarget.IsActive)
			{
				CurState = FleetState.MoveToTarget;
			}
			else if (activeOrder != null)
			{
				activeOrder.ResetTargetPosition();
			}
			if (IsIdle && Time.time > nextRegroupCheck)
			{
				nextRegroupCheck = Time.time + 4f;
				if (leader != null && leader.CurrentUnit.IsDocked && !AllUnitsAtDock(leader.CurrentUnitComponents.DockUnit))
				{
					SetTargetToGroupLeader();
				}
			}
		}

		private void UpdateIsInCombat()
		{
			bool flag = false;
			if (curState == FleetState.CombatInterception)
			{
				flag = true;
			}
			else if (engine.FleetRecentAttacksLog.FleetRecentlyAttacked(this, 30f))
			{
				flag = true;
			}
			InCombat = flag;
			if (inCombat)
			{
				lastTimeInCombat = Time.time;
			}
		}

		public bool AllowAttack()
		{
			if (activeOrder != null)
			{
				return activeOrder.CanFleetAttack();
			}
			return Settings.AllowAttack;
		}

		public bool AllowCombatInterception()
		{
			if (activeOrder != null)
			{
				return activeOrder.CanFleetIntercept();
			}
			return Settings.AllowCombatInterception;
		}

		private void UpdateGroupCombatInterception()
		{
			if (!AllowAttack() || !AllowCombatInterception() || pilots.Count == 0 || Time.time > combatInterceptionTimeout || !HasPilotsIntercepting())
			{
				ClearCombatInterception();
				if (activeOrder != null)
				{
					activeOrder.OnReturningFromCombatState();
				}
			}
		}

		internal bool RequestUndock(NpcPilot npcPilot)
		{
			if (activeOrder == null)
			{
				return true;
			}
			return activeOrder.RequestUndock(npcPilot);
		}

		private void UpdateGroupRegrouping()
		{
			if (Time.time > regroupCoolDownTime || GetPilotsMovingToFormationCount() == 0)
			{
				CurState = FleetState.Idle;
			}
		}

		private int GetPilotsMovingToFormationCount()
		{
			int num = 0;
			foreach (NpcPilot pilot in pilots)
			{
				if (pilot.MovingToFormation)
				{
					num++;
				}
			}
			return num;
		}

		private void UpdateMoveToTargetState(float elapsedTime)
		{
			if (navTarget.IsActive && Sector != null)
			{
				if (navTarget.MaxPilotDistFromNavpoint.HasValue && RequiresRegroup(navTarget.MaxPilotDistFromNavpoint.Value) && RequestRegroup())
				{
					StartRegroup();
				}
				else
				{
					if (pilots.Count <= 0)
					{
						return;
					}
					if (navTarget.GetIsValid() != AIObjectiveTarget.InvalidNavpointResult.Valid)
					{
						ClearTarget();
						CurState = FleetState.Idle;
						return;
					}
					if ((!navTarget.HasAttemptedToCalculatedPath || navTarget.IsMovingTarget) && (navTarget.HasStalePath() || Time.time > nextOrderPathFind || navTarget.GetTargetSector() == leader.Sector))
					{
						if (RequiresPathFind())
						{
							if (!FindPathToTargetWrapper())
							{
								if (LogWrapper.LogMsgs)
								{
									Debug.LogFormat(this, "Ai group {0} failed to find path to target. Moving to idle state", name);
								}
								if (navTarget.SourceOrder != null)
								{
									navTarget.SourceOrder.OnFleetFailedToFindPathToTarget();
								}
								ClearTarget();
								CurState = FleetState.Idle;
							}
						}
						else
						{
							navTarget.waitingForLeaderToReachNavPoint = true;
						}
					}
					if (navTarget.IsActive)
					{
						if (navTarget.HasAttemptedToCalculatedPath)
						{
							if (navTarget.waypoints.Count > 0)
							{
								WorldNavpoint worldNavpoint = navTarget.waypoints[0];
								bool isLastWaypoint = navTarget.waypoints.Count == 1;
								bool waitingForLeaderToReachNavPoint = NavigateToWaypoint(ref worldNavpoint, elapsedTime, isLastWaypoint);
								navTarget.waitingForLeaderToReachNavPoint = waitingForLeaderToReachNavPoint;
							}
							else
							{
								navTarget.waitingForLeaderToReachNavPoint = true;
							}
						}
					}
					else
					{
						CurState = FleetState.Idle;
					}
				}
			}
			else
			{
				CurState = FleetState.Idle;
			}
		}

		private bool RequestRegroup()
		{
			if (IsUnderAttack)
			{
				return false;
			}
			if (activeOrder == null)
			{
				return true;
			}
			return activeOrder.RequestFleetRegroup();
		}

		private void StartRegroup()
		{
			CurState = FleetState.Regrouping;
		}

		private bool IsNavpointValid(WorldNavpoint np, out string message)
		{
			message = null;
			if (np.TargetType == WorldNavpointTargetType.None && np.GetTargetSector() != Sector)
			{
				message = $"Target type is None but the target sector {np.GetTargetSector()} is not the same as the current sector ${Sector}";
				return false;
			}
			if ((np.TargetType == WorldNavpointTargetType.Dock || np.TargetType == WorldNavpointTargetType.Gate) && np.TargetRootSceneObject == null)
			{
				message = $"The target type is {np.TargetType} but the target is null";
				return false;
			}
			if ((np.TargetType == WorldNavpointTargetType.Dock || np.TargetType == WorldNavpointTargetType.Gate) && np.GetTargetSector() != Sector)
			{
				message = $"The target type is {np.TargetType} but the target sector {np.GetTargetSector()} is not the same as the current sector {Sector}";
				return false;
			}
			if (!UnitOutOfBoundsValidator.IsLocalPositionWithinBounds(np.GetTargetSectorPosition()))
			{
				message = $"The target type is outside of the bounds of the {np.GetTargetSector()} sector. Local position: {np.GetTargetSectorPosition()}";
				return false;
			}
			return true;
		}

		private bool RequiresRegroup(float maxDistanceFromNavpoint)
		{
			if (!IsUnderAttack && pilots.Count > 1 && maxDistanceFromNavpoint > engine.GameSettings.AIPilotRegroupDist)
			{
				return !ArePilotsAllAtSameDock();
			}
			return false;
		}

		private void UpdateGroupEnteringGate()
		{
			if (WormholeBeingEntered == null)
			{
				Debug.LogWarning("Leaving EnteringGate state because EnteringGate is null");
				CurState = FleetState.Idle;
			}
			else if (GetPilotsNotInGateTargetSector() == 0)
			{
				CurState = FleetState.Idle;
			}
		}

		private void SyncSectorWithLeader()
		{
			if (leader != null && Sector != leader.Sector)
			{
				SetPositionToLeaderShipPosition();
			}
		}

		public void NotifyLeaderReachedNavpoint()
		{
			if (curState == FleetState.MoveToTarget && navTarget.IsActive && navTarget.waitingForLeaderToReachNavPoint)
			{
				OnLeaderReachedNavpoint();
			}
		}

		private void OnLeaderReachedNavpoint()
		{
			if (navTarget.waypoints.Count > 0 && HandleWaypointReached())
			{
				RemoveNextWaypoint();
				navTarget.waitingForLeaderToReachNavPoint = false;
			}
			if (navTarget.waypoints.Count == 0)
			{
				navTarget.waitingForLeaderToReachNavPoint = false;
				OnAllWaypointsRemoved();
			}
		}

		private bool HandleWaypointReached()
		{
			WorldNavpoint worldNavpoint = navTarget.waypoints[0];
			if (worldNavpoint.TargetSectorObject != null)
			{
				switch (worldNavpoint.TargetType)
				{
				case WorldNavpointTargetType.Dock:
					if (!AllUnitsAtDock((Unit)worldNavpoint.TargetSectorObject))
					{
						return false;
					}
					break;
				case WorldNavpointTargetType.Gate:
					InitGateEntry();
					break;
				}
			}
			return true;
		}

		private void OnAllWaypointsRemoved()
		{
			ClearTarget();
			ActiveFleetOrder sourceOrder = navTarget.SourceOrder;
			if (sourceOrder != null && sourceOrder == activeOrder)
			{
				activeOrder.OnGroupReachedTarget();
				if (activeOrder != null && activeOrder.IsValid)
				{
					activeOrder.ResetTargetPosition();
				}
			}
		}

		public void RemoveNpc(NpcPilot pilot)
		{
			if (!pilots.Remove(pilot))
			{
				return;
			}
			if (pilot.Fleet == this)
			{
				pilot.Fleet = null;
			}
			canAllUnitsCloak = CheckCanAllUnitCloak();
			if (pilots.Count == 0)
			{
				isArmed = false;
				npcShips.Clear();
				npcShipHullTypes.Clear();
				if (activeOrder != null)
				{
					activeOrder.OnInvalid();
				}
				Leader = null;
				cachedCombatRating = 0f;
				OnAllPilotsRemoved();
			}
			else
			{
				if (leader == pilot)
				{
					leader = null;
				}
				if (leader == null || IdleAndNoObjectives)
				{
					DesignateLeader();
				}
				CacheControlledUnits();
				CacheControllerHullTypes();
				UpdateSimpleCombatRating();
			}
			InvalidateCargoUsageStats();
			InvalidateCargoCapacityStats();
			if (pilots.Count > 0)
			{
				SetFormationPositions();
			}
		}

		internal void RecacheControlledUnits()
		{
			CacheControlledUnits();
			CacheControllerHullTypes();
			UpdateSimpleCombatRating();
			InvalidateCargoUsageStats();
			InvalidateCargoCapacityStats();
		}

		private void CacheControlledUnits()
		{
			isArmed = false;
			npcShips.Clear();
			foreach (NpcPilot pilot in pilots)
			{
				if (pilot.CurrentUnitComponents != null)
				{
					AddNpcShipInternal(pilot.CurrentUnitComponents);
					isArmed = isArmed || pilot.CurrentUnit.IsArmed;
				}
			}
		}

		private void AddNpcShipInternal(UnitComponentHolder ship)
		{
			npcShips.Add(ship);
		}

		private void AddNpcPilotInternal(NpcPilot npcPilot)
		{
			UnitComponentHolder currentUnitComponents = npcPilot.CurrentUnitComponents;
			if (pilots.Count == 0 || currentUnitComponents == null)
			{
				pilots.Add(npcPilot);
				return;
			}
			int i;
			for (i = 0; i < pilots.Count && (pilots[i].CurrentUnitComponents == null || currentUnitComponents.UnitClass.RelativeShipSaleCost > pilots[i].CurrentUnitComponents.UnitClass.RelativeShipSaleCost); i++)
			{
			}
			pilots.Insert(i, npcPilot);
		}

		private void CacheControllerHullTypes()
		{
			npcShipHullTypes.Clear();
			foreach (UnitComponentHolder ship in Ships)
			{
				if (ship != null && ship.Unit != null)
				{
					npcShipHullTypes.Add(ship.UnitClass.HullType);
				}
			}
		}

		private void OnAllPilotsRemoved()
		{
			if (AllPilotsRemoved != null)
			{
				AllPilotsRemoved(this);
			}
			if (Settings.DestroyWhenNoPilots)
			{
				SafeDestroy();
			}
		}

		public void SafeDestroy()
		{
			ClearOrders();
			Faction = null;
			NpcPilot[] array = pilots.ToArray();
			foreach (NpcPilot npcPilot in array)
			{
				if (npcPilot.Fleet == this)
				{
					npcPilot.Fleet = null;
				}
			}
			pilots.Clear();
			npcShips.Clear();
			Engine = null;
			UnityEngine.Object.Destroy(gameObject);
		}

		public void AddNpc(NpcPilot npcPilot)
		{
			if (npcPilot == null)
			{
				Debug.LogError("Trying to add null pilot", this);
			}
			else
			{
				if (pilots.Contains(npcPilot))
				{
					return;
				}
				if (npcPilot.Person != null)
				{
					if (npcPilot.CurrentUnit == null)
					{
						Debug.LogError($"{this}: Cannot add pilot {npcPilot} to the fleet because it does not have a controlled unit", this);
						return;
					}
					if (npcPilot.CurrentUnit.Components == null || !npcPilot.CurrentUnit.IsPilottable())
					{
						Debug.LogError($"{this}: Cannot add pilot {npcPilot} to the fleet because it is not pilottable", this);
						return;
					}
					npcPilot.Person.Faction = faction;
					if (npcPilot.CurrentUnit.Faction != faction)
					{
						npcPilot.CurrentUnit.Faction = faction;
					}
					canAllUnitsCloak = CheckCanAllUnitCloak();
					AddNpcPilotInternal(npcPilot);
					AddNpcShipInternal(npcPilot.CurrentUnitComponents);
					NpcShipHullTypes.Add(npcPilot.CurrentUnit.UnitClass.HullType);
					isArmed = isArmed || npcPilot.CurrentUnit.IsArmed;
					cachedCombatRating += npcPilot.CurrentUnit.CombatRating;
					npcPilot.Fleet = this;
					if (leader == null || IdleAndNoObjectives)
					{
						DesignateLeader();
					}
					SetFormationPositions();
					InvalidateCargoUsageStats();
					InvalidateCargoCapacityStats();
				}
				else
				{
					Debug.LogError($"Cannot add AIUnitController {npcPilot} as it is missing Person component", this);
				}
			}
		}

		private void LogIfMaxPilotsExceeded()
		{
			if (pilots.Count == 8)
			{
				Debug.LogWarningFormat(this, "{0}: Adding pilot will exceed maximum allowable pilots ({1})", name, 8);
			}
		}

		public void DesignateLeader()
		{
			float num = 0f;
			NpcPilot npcPilot = null;
			foreach (NpcPilot pilot in pilots)
			{
				if (!(pilot != null))
				{
					continue;
				}
				Unit currentUnit = pilot.CurrentUnit;
				if (currentUnit != null)
				{
					float maxHealth = currentUnit.UnitClass.maxHealth;
					if (maxHealth > num)
					{
						npcPilot = pilot;
						num = maxHealth;
					}
				}
			}
			Leader = npcPilot;
		}

		public void SetFormationPositions()
		{
			if (pilots.Count > 0)
			{
				if (pilots.Count == 1)
				{
					leader.FormationPosition = Vector3.zero;
				}
				else
				{
					AssignCachedFormationPositions();
				}
			}
		}

		public float CalculatePreferredMoveSpeed(float minMovementSpeed)
		{
			float num = minMovementSpeed * 1.2f;
			if (navTarget.IsActive && navTarget.PreferredMoveSpeedMultiplier.HasValue)
			{
				num *= navTarget.PreferredMoveSpeedMultiplier.Value;
			}
			return num;
		}

		public void UpdateMoveSpeed()
		{
			if (!(Time.time > nextUpdateMoveSpeedTime))
			{
				return;
			}
			preferredSpeed = CalculatePreferredSpeed();
			movementSpeed = CalculateMoveSpeed();
			if (curState == FleetState.MoveToTarget && navTarget.waypoints.Count > 0 && !IsUnderAttack && navTarget.MaxPilotDistFromNavpoint > engine.GameSettings.RegroupSpdChangeThreshold)
			{
				float num = 1f - (navTarget.MaxPilotDistFromNavpoint.Value - engine.GameSettings.RegroupSpdChangeThreshold) / (engine.GameSettings.RegroupSpdChangeThresholdUpper - engine.GameSettings.RegroupSpdChangeThreshold);
				if (num < 0f)
				{
					num = 0f;
				}
				movementSpeed *= num;
			}
			navTarget.MaxPilotDistFromNavpoint = null;
			nextUpdateMoveSpeedTime = Time.time + (IsInActiveSector ? 1f : 3f);
		}

		public void NotifyNpcPilotDistanceFromNavpoint(NpcPilot npcPilot, float distance)
		{
			if (!navTarget.MaxPilotDistFromNavpoint.HasValue)
			{
				navTarget.MaxPilotDistFromNavpoint = distance;
			}
			else
			{
				navTarget.MaxPilotDistFromNavpoint = Mathf.Max(navTarget.MaxPilotDistFromNavpoint.Value, distance);
			}
		}

		public float CalculateMaxPilotDistFromNavpoint(WorldNavpoint target)
		{
			float num = 0f;
			foreach (NpcPilot pilot in pilots)
			{
				if (pilot.CurrentUnit != null && (target.TargetType != WorldNavpointTargetType.Dock || !(pilot.CurrentUnit.GetRootUnit() == target.TargetRootSceneObject)) && pilot.Sector == Sector)
				{
					Vector3 pilotFleetFormationSectorPosition = GetPilotFleetFormationSectorPosition(pilot);
					num = Mathf.Max(num, Maths.GetDistanceIgnoreY(pilot.CurrentUnit.SectorPosition, pilotFleetFormationSectorPosition));
				}
			}
			return num;
		}

		public void ExtendCombatInterceptionTime(float time)
		{
			if (curState == FleetState.CombatInterception)
			{
				combatInterceptionTimeout = Mathf.Max(combatInterceptionTimeout, Time.time + time);
			}
		}

		public Vector3 GetPilotFleetFormationSectorPosition(NpcPilot pilot)
		{
			return SectorPosition + transform.localRotation * GetLocalFormationPositionForPilot(pilot);
		}

		public float CalculateMoveSpeed()
		{
			float cachedMinMoveSpeed = GetCachedMinMoveSpeed();
			if (IsUnderAttack)
			{
				return cachedMinMoveSpeed * 2f;
			}
			return Mathf.Min(GetCachedMinMoveSpeed(), preferredSpeed);
		}

		public void SetPositionToLeaderShipPosition()
		{
			if (!(leader != null))
			{
				return;
			}
			Unit currentUnit = leader.CurrentUnit;
			if (currentUnit != null && currentUnit.Sector != null)
			{
				if (Sector != currentUnit.Sector)
				{
					Sector = currentUnit.Sector;
					ClearTarget();
				}
				transform.localPosition = currentUnit.SectorPosition;
				transform.localRotation = currentUnit.transform.localRotation;
			}
		}

		public DockedPreference GetPreferToDockWhenIdle()
		{
			if (activeOrder != null)
			{
				DockedPreference preferToDockWhenIdle = activeOrder.GetPreferToDockWhenIdle();
				if (preferToDockWhenIdle != DockedPreference.DontCare)
				{
					return preferToDockWhenIdle;
				}
			}
			return Settings.PreferToDock;
		}

		public void NpcPilotsControlShips()
		{
			foreach (NpcPilot pilot in pilots)
			{
				if (pilot.CurrentUnit != null && pilot.CurrentUnit.Faction == faction && pilot.CurrentUnit.IsPilottable())
				{
					pilot.CurrentUnit.Components.PilotPerson = pilot.Person;
				}
			}
		}

		public void AssignNextOrderIfNoCurrentOrder()
		{
			if (activeOrder == null)
			{
				AssignNextOrder();
			}
		}

		public void AssignNextOrder()
		{
			ActiveFleetOrder activeFleetOrder = TakeNextOrder();
			if (activeFleetOrder != null)
			{
				ActiveOrder = activeFleetOrder;
				ActiveOrder.Init();
			}
		}

		public bool ShouldRaiseDialogEvent()
		{
			if (IsInActiveSector && leader != null)
			{
				return !inCombat;
			}
			return false;
		}

		private void RaiseDialogForNewOrder()
		{
			if (IsInActiveSector && leader != null && !IsUnderAttack && leader.Person != faction.LeaderPerson && faction.LeaderPerson != null)
			{
				DialogRequestArguments value = DialogRequestArguments.Init("TargetPilotTitle", faction.LeaderPerson.Title);
				leader.Person.RaiseDialogEventRandomly(EngineASX.Instance.DialogEvents.FleetReceivedNewOrder, 0f, value, (!IsOwnedByPlayer) ? 0.4f : 1f);
			}
		}

		public void DiscoverOwnUnits()
		{
			if (!(faction.Intel != null))
			{
				return;
			}
			foreach (NpcPilot pilot in pilots)
			{
				faction.Intel.DiscoverUnit(pilot.CurrentUnit);
			}
		}

		public float GetAdditionalCombatTargetPriority(Unit combatTarget)
		{
			if (activeOrder != null)
			{
				return activeOrder.GetAdditionalCombatTargetPriority(combatTarget);
			}
			return 0f;
		}

		public void AddSpecificTargets(List<AISpecificTarget> targets)
		{
			if (activeOrder != null)
			{
				activeOrder.AddSpecificTargets(targets);
			}
		}

		public bool RequestInterceptionInternal(NpcPilot requestor, Unit target, float requestorsDistToTarget, float targetScore, bool priorityTarget)
		{
			if (!priorityTarget && !IsUnitTargettedByOtherPilots(requestor, target))
			{
				if (targetScore >= GetInterceptScoreThresholdFromAggression())
				{
					if (!(requestorsDistToTarget < Settings.TargetInterceptionLowerDistance))
					{
						if (curState == FleetState.CombatInterception)
						{
							return requestorsDistToTarget < Settings.TargetInterceptionUpperDistance;
						}
						return false;
					}
					return true;
				}
				return false;
			}
			return true;
		}

		public bool IsUnitTargettedByOtherPilots(NpcPilot exclusion, Unit target)
		{
			foreach (NpcPilot pilot in pilots)
			{
				if (pilot != exclusion && pilot.CombatMode == NpcPilot.AIControllerCombatMode.Offensive && pilot.CombatTarget == target)
				{
					return true;
				}
			}
			return false;
		}

		public bool IsUnderAttackBy(Unit unit)
		{
			return engine.FleetRecentAttacksLog.FleetRecentlyAttackedBy(unit, this);
		}

		public bool RequestInterception(NpcPilot requestor, Unit target, float requestorsDistToTarget, float targetScore, bool priorityTarget)
		{
			if (!isArmed || !requestor.CurrentUnit.IsArmed)
			{
				return false;
			}
			if (CanMoveIntoCombatState())
			{
				bool flag = false;
				flag = ((!(activeOrder != null)) ? RequestInterceptionInternal(requestor, target, requestorsDistToTarget, targetScore, priorityTarget) : activeOrder.RequestInterception(requestor, target, requestorsDistToTarget, targetScore, priorityTarget));
				if (flag)
				{
					CurState = FleetState.CombatInterception;
					combatInterceptionTimeout = Time.time + 90f;
				}
				return flag;
			}
			return false;
		}

		private bool CanMoveIntoCombatState()
		{
			FleetState fleetState = curState;
			if ((uint)fleetState <= 2u || fleetState == FleetState.CombatInterception)
			{
				if (AllowAttack())
				{
					return AllowCombatInterception();
				}
				return false;
			}
			return false;
		}

		public bool AllControllersDockCooldownElapsed()
		{
			foreach (NpcPilot pilot in pilots)
			{
				if (pilot.CurrentUnitComponents.IsDocked && !pilot.DockCooldownElapsed())
				{
					return false;
				}
			}
			return true;
		}

		public void UndockAll()
		{
			foreach (NpcPilot pilot in pilots)
			{
				if (pilot.CurrentUnit.Components.UndockIfDocked())
				{
					pilot.SetDockCooldownTime();
				}
			}
		}

		public void DockAll(Unit dock)
		{
			foreach (NpcPilot pilot in pilots)
			{
				if (pilot.CurrentUnit.Components.TryDockInUnit(dock))
				{
					pilot.SetDockCooldownTime();
				}
			}
			SetPositionToLeaderShipPosition();
		}

		public bool CanDockAllAtUnit(Unit dock)
		{
			UnitHangar hangar = dock.GetHangar();
			if (hangar != null && dock.IsDockable)
			{
				return hangar.CanUnitsFitInHangar(Ships);
			}
			return false;
		}

		public bool CanDockAllAtUnitIgnoreOccupancy(Unit dock)
		{
			UnitHangar hangar = dock.GetHangar();
			if (hangar != null)
			{
				return hangar.CanUnitsFitInHangarIgnoreOccupancy(Ships);
			}
			return false;
		}

		[Obsolete("Does not handle failure to dock")]
		public void DockAllAtHomeBase()
		{
			foreach (NpcPilot pilot in pilots)
			{
				if (pilot.CurrentUnit.Components.TryDockInUnit(HomeBaseUnit))
				{
					pilot.SetDockCooldownTime();
				}
			}
		}

		public void ClearTarget()
		{
			navTarget.IsActive = false;
		}

		public void SetTargetToUnit(ActiveFleetOrder requestor, Unit unit)
		{
			SetTargetToSectorObject(requestor, unit);
			if (unit.IsDocked)
			{
				navTarget.RelativeTargetPosition = Geometry.RandomXZUnitVector() * unit.GetRootUnit().UnitClass.DisplayData.Radius * 2.5f;
			}
		}

		public void SetTargetToFleet(ActiveFleetOrder requestor, Fleet fleet)
		{
			SetTargetToSectorObject(requestor, fleet);
		}

		public void SetTargetToSectorObject(ActiveFleetOrder requestor, SectorObject t)
		{
			if (t == null)
			{
				Debug.LogError($"Fleet {this} given target object from {requestor} which is null", this);
			}
			navTarget.Reset();
			navTarget.OriginalSector = t.Sector;
			navTarget.TargetSectorObject = t;
			navTarget.TargetType = WorldNavpointTargetType.None;
			OnTargetSet(requestor);
		}

		public void SetTargetToCurrentSectorPosition(ActiveFleetOrder requestor)
		{
			SetTargetToSectorWorldPosition(requestor, Sector, leader.CurrentUnit.transform.position);
		}

		public void SetTargetToGroupLeader()
		{
			if (leader != null)
			{
				if (leader.CurrentUnit.IsDocked)
				{
					SetTargetToDock(null, leader.CurrentUnit.Components.DockUnit);
				}
				else
				{
					SetTargetToSectorWorldPosition(null, Sector, leader.CurrentUnit.transform.position);
				}
			}
			else
			{
				Debug.LogError("Group does not have a leader. Cannot set target", this);
			}
		}

		public void SetTargetToDock(ActiveFleetOrder requestor, Unit dockTarget)
		{
			if (dockTarget != null)
			{
				if (dockTarget.IsValidAndNotDestroyed && !AllUnitsAtDock(dockTarget))
				{
					navTarget.Reset();
					navTarget.TargetSectorObject = dockTarget;
					navTarget.TargetType = WorldNavpointTargetType.Dock;
					OnTargetSet(requestor);
				}
			}
			else
			{
				Debug.LogErrorFormat(this, "{0}: Attempting to set group dock target to null dock target", this);
			}
		}

		public void SetTargetToSectorPosition(ActiveFleetOrder requestor, Sector sector, Vector3 sectorPosition)
		{
			if (sector == null)
			{
				Debug.LogError($"Fleet {this}: Null sector specified. Ignoring request");
				return;
			}
			navTarget.Reset();
			navTarget.OriginalSector = sector;
			navTarget.SectorPosition = sectorPosition;
			navTarget.TargetType = WorldNavpointTargetType.None;
			OnTargetSet(requestor);
		}

		public void SetTargetToSectorWorldPosition(ActiveFleetOrder requestor, Sector sector, Vector3 worldPosition)
		{
			SetTargetToSectorPosition(requestor, sector, worldPosition - sector.transform.position);
		}

		public void SetTargetToWormhole(ActiveFleetOrder requestor, Wormhole wormhole)
		{
			if (!navTarget.IsActive || navTarget.TargetType != WorldNavpointTargetType.Gate || !(navTarget.TargetSectorObject == wormhole))
			{
				navTarget.Reset();
				navTarget.TargetSectorObject = wormhole.Unit;
				navTarget.TargetType = WorldNavpointTargetType.Gate;
				OnTargetSet(requestor);
			}
		}

		public bool FindPathToTargetWrapper()
		{
			if (navTarget.IsActive)
			{
				navTarget.LastPathCalculationTargetSector = navTarget.GetTargetSector();
				navTarget.HasAttemptedToCalculatedPath = true;
				nextOrderPathFind = Time.time + engine.GameSettings.AITimeBetweenPathfinding;
				if (FindPathToTarget())
				{
					return true;
				}
			}
			return false;
		}

		public bool RequiresPathFind()
		{
			if (navTarget.IsActive)
			{
				return !AllShipsDockedAtNavTarget();
			}
			return false;
		}

		private bool AllShipsDockedAtNavTarget()
		{
			if (navTarget.TargetType != WorldNavpointTargetType.Dock)
			{
				return false;
			}
			if (!leader.CurrentUnitComponents.IsDocked || leader.CurrentUnitComponents.DockUnit != navTarget.TargetSectorObject)
			{
				return false;
			}
			return ArePilotsAllAtSameDock();
		}

		public bool FindPathToTarget()
		{
			navTarget.waypoints.Clear();
			if (leader != null && leader.CurrentUnit != null)
			{
				if (Sector != leader.CurrentUnit.Sector)
				{
					SetPositionToLeaderShipPosition();
				}
				Sector targetSector = navTarget.GetTargetSector();
				WorldNavpointTargetType targetType = navTarget.TargetType;
				SectorObject targetSectorObject = navTarget.TargetSectorObject;
				float targetArrivalThreshold = navTarget.ArrivalThreshold;
				Vector3 targetSectorPosition = navTarget.GetTargetSectorPosition();
				if (targetSectorObject != null && !targetSectorObject.ShouldTreatAtStatic() && navTarget.TargetSectorObject is Unit targetUnit)
				{
					if (!AdjustTargetForLastKnownPosition(targetUnit, ref targetSector, ref targetSectorPosition, ref targetSectorObject, ref targetType, ref targetArrivalThreshold))
					{
						return false;
					}
					if (navTarget.RelativeTargetPosition != Vector3.zero)
					{
						targetSectorPosition += targetSectorObject.transform.localRotation * navTarget.RelativeTargetPosition;
					}
				}
				if (Sector != null && targetSector != null)
				{
					if (AdvPathfinder.Pathfind(Sector, SectorPosition, targetSector, targetSectorPosition, faction, ignoreHeat: false, navTarget.waypoints).IsSuccess)
					{
						if (navTarget.waypoints.Count > 0)
						{
							WorldNavpoint worldNavpoint = navTarget.waypoints[navTarget.waypoints.Count - 1];
							worldNavpoint.TargetSectorObject = targetSectorObject;
							worldNavpoint.TargetType = targetType;
							if (navTarget.waypoints.Count > 1 || (Sector == navTarget.GetTargetSector() && Vector3.Distance(SectorPosition, navTarget.waypoints[0].SectorPosition) > GetRequiredDistToTarget(ref worldNavpoint)))
							{
								worldNavpoint.ArrivalThreshold = navTarget.ArrivalThreshold;
							}
							navTarget.waypoints[navTarget.waypoints.Count - 1] = worldNavpoint;
							return true;
						}
					}
					else
					{
						Debug.LogWarning($"Fleet {this}: Pathfind from {Sector} to {targetSector} has failed for order {activeOrder}", this);
					}
				}
				else
				{
					Debug.LogWarning($"Trying to pathfind but don't have start or end scene. Scene: {Sector} TargetScene: {targetSector}", this);
				}
			}
			return false;
		}

		private bool AdjustTargetForLastKnownPosition(Unit targetUnit, ref Sector targetSector, ref Vector3 targetSectorPosition, ref SectorObject targetSectorObject, ref WorldNavpointTargetType targetType, ref float targetArrivalThreshold)
		{
			WorldNavpoint? lastKnownPosition = faction.Intel.GetLastKnownPosition(targetUnit, 60f);
			if (!lastKnownPosition.HasValue)
			{
				return false;
			}
			WorldNavpoint value = lastKnownPosition.Value;
			if (value.TargetType == WorldNavpointTargetType.Gate)
			{
				if (value.TargetSectorObject.Sector == Sector)
				{
					targetSectorObject = value.TargetSectorObject;
					targetSector = value.GetTargetSector();
					targetSectorPosition = value.GetTargetSectorPosition();
					targetType = WorldNavpointTargetType.Gate;
				}
			}
			else
			{
				targetSector = value.GetTargetSector();
				targetSectorPosition = value.GetTargetSectorPosition();
			}
			return true;
		}

		public bool RequestGateEntry(NpcPilot pilot)
		{
			return true;
		}

		public bool ArePilotsAllAtSameDock()
		{
			Unit unit = null;
			foreach (NpcPilot pilot in pilots)
			{
				if (unit == null)
				{
					if (pilot.CurrentUnit.Components.DockUnit != null)
					{
						unit = pilot.CurrentUnit.Components.DockUnit;
					}
				}
				else if (pilot.CurrentUnit.Components.DockUnit != unit)
				{
					return false;
				}
			}
			return unit != null;
		}

		public void DestroyPilots()
		{
			NpcPilot[] array = pilots.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Fleet = null;
				array[i].Person.SafeDestroy();
			}
		}

		public void DestroyPilotsAndShips()
		{
			NpcPilot[] array = pilots.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Fleet = null;
				Unit currentUnit = array[i].CurrentUnit;
				array[i].Person.SafeDestroy();
				if ((bool)currentUnit)
				{
					currentUnit.SafeDestroy();
				}
			}
		}

		public Sector GetHomeSectorOrCurrent()
		{
			Sector homeSector = HomeSector;
			if (homeSector != null)
			{
				return homeSector;
			}
			if (faction.FactionAI != null && faction.HomeSector != null)
			{
				return faction.HomeSector;
			}
			return Sector;
		}

		public Vector3 GetHomeSectorPositionOrCurrent()
		{
			if (HomeBaseUnit != null)
			{
				return HomeBaseUnit.SectorPosition;
			}
			return transform.localPosition;
		}

		[ContextMenu("Auto Name GameObject")]
		public void AutoNameGameObject()
		{
			gameObject.name = GetGameObjectName();
		}

		public string GetGameObjectName()
		{
			string text = ((UniqueId > -1) ? $"_{UniqueId}" : string.Empty);
			string text2 = ((faction != null) ? faction.GetShortNameElseLong() : "no_faction");
			string text3 = ((!string.IsNullOrWhiteSpace(Name)) ? ("_" + Name) : string.Empty);
			return "Fleet" + text + "_" + text2 + text3;
		}

		public bool ContainsUnit(Unit unit)
		{
			for (int i = 0; i < pilots.Count; i++)
			{
				if (pilots[i].CurrentUnit == unit)
				{
					return true;
				}
			}
			return false;
		}

		public void EnqueueOrder(FleetOrder order, bool raiseDialog = true)
		{
			int count = OrderQueue.Count;
			InsertOrderInQueue(order, OrderQueue.Count);
			if (count == 0 && raiseDialog)
			{
				RaiseDialogForNewOrder();
			}
		}

		private void ValidateNewOrder(FleetOrder order)
		{
			string text = order.Validate(this);
			if (text != null)
			{
				Debug.LogError($"Fleet {this}: Validation failed for order {order}: {text}");
			}
		}

		public void InsertOrderAndRequeueActive(FleetOrder order)
		{
			if (!OrderQueue.Contains(order) && !(order == ActiveOrder))
			{
				if (activeOrder != null)
				{
					FleetOrder fleetOrder = activeOrder.FleetOrder;
					activeOrder.FleetOrder = null;
					activeOrder.SafeDestroy();
					OrderQueue.Insert(0, fleetOrder);
				}
				InitOrderAndInsertInQueue(order, 0);
			}
		}

		private void InitOrderAndInsertInQueue(FleetOrder order, int index)
		{
			order.Init();
			OrderQueue.Insert(index, order);
			order.transform.SetParent(transform);
			order.transform.localPosition = Vector3.zero;
		}

		public void InsertOrderInQueue(FleetOrder order, int index)
		{
			if (!OrderQueue.Contains(order) && !(order == ActiveOrder))
			{
				InitOrderAndInsertInQueue(order, index);
			}
		}

		public void ValidateShouldBeAddingOrder()
		{
			if (faction != null && faction.FactionAI != null)
			{
				if (!FactionAIBase.HasFleetOrderCooldownTimeElapsed(this))
				{
					Debug.LogWarning($"Shouldn't be ordering this fleet {this} right now - order cooldown hasn't elapsed", this);
				}
				else if (IsAboutToEnterGate)
				{
					Debug.LogWarning($"Shouldn't be ordering this fleet {this} right now - about to enter gate", this);
				}
				else if (OrderQueue.Count >= 8)
				{
					Debug.LogWarning($"Shouldn't be ordering this fleet {this} right now - max order cap reached", this);
				}
			}
		}

		public bool AllUnitsInSector(Sector scene)
		{
			foreach (NpcPilot pilot in pilots)
			{
				if (pilot.CurrentUnit != null && pilot.CurrentUnit.Sector != scene)
				{
					return false;
				}
			}
			return true;
		}

		public bool AllUnitsAtDock(Unit unit)
		{
			foreach (NpcPilot pilot in pilots)
			{
				if (pilot.CurrentUnit != null && pilot.CurrentUnit.Components.DockUnit != unit)
				{
					return false;
				}
			}
			return true;
		}

		public bool GetPrefersCloak()
		{
			if (activeOrder != null)
			{
				switch (activeOrder.GetCloakPreference())
				{
				case FleetOrderCloakPreference.Always:
					return true;
				case FleetOrderCloakPreference.None:
					return false;
				case FleetOrderCloakPreference.OnlyIfAllCanCloak:
					return canAllUnitsCloak;
				}
			}
			return Settings.PreferCloak;
		}

		private bool CheckCanAllUnitCloak()
		{
			foreach (NpcPilot pilot in pilots)
			{
				if (pilot.CurrentUnitComponents != null && pilot.CurrentUnitComponents.CloakComponent == null)
				{
					return false;
				}
			}
			return true;
		}

		public void SetPilotsFleet(Fleet fleet)
		{
			NpcPilot[] array = pilots.ToArray();
			foreach (NpcPilot npcPilot in array)
			{
				if (npcPilot != null)
				{
					npcPilot.Fleet = fleet;
				}
			}
		}

		public void ClearOrders()
		{
			ActiveOrder = null;
			foreach (FleetOrder item in OrderQueue)
			{
				item.SafeDestroy(this);
			}
			OrderQueue.Clear();
		}

		public WorldNavpoint? GetCurrentWaypoint()
		{
			if (navTarget.IsActive && navTarget.waypoints.Count > 0)
			{
				return navTarget.waypoints[0];
			}
			return null;
		}

		public FleetOrder GetCurrentOrNextOrder()
		{
			if (ActiveOrder != null)
			{
				return ActiveOrder.FleetOrder;
			}
			if (OrderQueue.Count > 0)
			{
				return OrderQueue[0];
			}
			return null;
		}

		public FleetOrder GetLastOrder()
		{
			if (OrderQueue.Count > 0)
			{
				return OrderQueue[OrderQueue.Count - 1];
			}
			if (ActiveOrder != null)
			{
				return ActiveOrder.FleetOrder;
			}
			return null;
		}

		private bool HasPilotsIntercepting()
		{
			foreach (NpcPilot pilot in pilots)
			{
				if (pilot.CombatTarget != null && pilot.CombatMode == NpcPilot.AIControllerCombatMode.Offensive)
				{
					return true;
				}
			}
			return false;
		}

		private void ClearTargetIfInvalid()
		{
			if (navTarget.IsActive && navTarget.GetIsValid() != AIObjectiveTarget.InvalidNavpointResult.Valid)
			{
				ClearTarget();
			}
		}

		private int GetPilotsNotInGateTargetSector()
		{
			Sector actualTargetSector = WormholeBeingEntered.ActualTargetSector;
			int num = 0;
			foreach (NpcPilot pilot in pilots)
			{
				if (pilot.CurrentUnit.Sector != actualTargetSector)
				{
					num++;
				}
			}
			return num;
		}

		private void RemoveNextWaypoint()
		{
			if (navTarget.waypoints.Count > 0)
			{
				navTarget.waypoints.RemoveAt(0);
			}
		}

		private void InitGateEntry()
		{
			WormholeBeingEntered = navTarget.waypoints[0].TargetSectorObject.GetComponent<Wormhole>();
			if (WormholeBeingEntered == null)
			{
				Debug.LogError("Group is entering gate but TargetUnit does not have a JumpGate component", this);
			}
			if (IsInActiveSector && leader != null && PilotCount > 1 && !inCombat)
			{
				leader.Person.RaiseDialogEventRandomly(EngineASX.Instance.DialogEvents.FleetMovingToNewSector, 0f, DialogRequestArguments.Init("TargetSector", WormholeBeingEntered.ActualTargetSector.Name));
			}
			CurState = FleetState.EnteringGate;
		}

		private bool NavigateToWaypoint(ref WorldNavpoint worldNavpoint, float elapsedTime, bool isLastWaypoint)
		{
			Vector3 zero = Vector3.zero;
			Vector3 vector = worldNavpoint.SectorPosition;
			if (worldNavpoint.TargetType == WorldNavpointTargetType.Gate)
			{
				vector = worldNavpoint.TargetSectorObject.SectorPosition + worldNavpoint.TargetSectorObject.transform.forward * 200f + worldNavpoint.TargetSectorObject.transform.right * offsetFromJumpgate;
			}
			vector.y = 0f;
			zero = vector - SectorPosition;
			float magnitude = zero.magnitude;
			float num = movementSpeed * elapsedTime;
			if (!Sector.IsActive)
			{
				num *= engine.GameSettings.InactiveGroupMoveSpdMultiplier;
			}
			float requiredDistToTarget = GetRequiredDistToTarget(ref worldNavpoint);
			if (num > magnitude - requiredDistToTarget)
			{
				if (isLastWaypoint)
				{
					if (navTarget.MatchTargetOrienation && navTarget.TargetSectorObject != null && NavTarget.TargetSectorObject.GetSpeed() < 5f)
					{
						transform.localRotation = navTarget.TargetSectorObject.transform.rotation;
					}
					else if (navTarget.Rotation.HasValue)
					{
						transform.localRotation = navTarget.Rotation.Value;
					}
				}
				if (!Sector.IsActive)
				{
					float num2 = magnitude - requiredDistToTarget;
					if (num2 > 0f)
					{
						Vector3 vector2 = Vector3.Normalize(zero);
						transform.localPosition += vector2 * num2;
					}
				}
				return true;
			}
			if (num > 0f)
			{
				cumulativeMovementSinceLastIdle += num;
				if (cumulativeMovementSinceLastIdle > 500f)
				{
					activeOrder.IsIdle = false;
					cumulativeMovementSinceLastIdle = 0f;
				}
				Vector3 vector3 = Vector3.Normalize(zero);
				Vector3 localPosition = transform.localPosition + vector3 * num;
				localPosition.y = 0f;
				transform.localPosition = localPosition;
				transform.localRotation = Quaternion.LookRotation(vector3, Vector3.up);
			}
			cumulativeMovement += num;
			return false;
		}

		private float GetRequiredDistToTarget(ref WorldNavpoint worldNavpoint)
		{
			float num = worldNavpoint.ArrivalThreshold + VeryBasicFleetRadiusCalculation();
			if (worldNavpoint.TargetSectorObject != null)
			{
				num += worldNavpoint.TargetSectorObject.ObjectRadius;
			}
			return num;
		}

		private void AssignCachedFormationPositions()
		{
			int num = pilots.Count - 2;
			if (num >= fleetFormation.Offsets.GetLength(0))
			{
				num = fleetFormation.Offsets.GetLength(0) - 1;
			}
			Vector3[] array = fleetFormation.Offsets[num];
			float num2 = 0f;
			float aIFormationMinUnitDist = engine.GameSettings.FormationSettings.AIFormationMinUnitDist;
			for (int i = 0; i < pilots.Count; i++)
			{
				num2 = Mathf.Max(num2, pilots[i].CurrentUnit.UnitClass.ShieldRingRadius);
			}
			float num3 = Mathf.Max(aIFormationMinUnitDist, num2 * engine.GameSettings.FormationSettings.AIFormationUnitRadiusMultiplier) * fleetFormation.FormationStyle.ScaleFudge;
			for (int j = 0; j < pilots.Count; j++)
			{
				Vector3 zero = Vector3.zero;
				zero = ((j >= array.Length) ? (new Vector3(0f, 0f, -4f) + array[j % array.Length]) : array[j]);
				pilots[j].FormationPosition = Vector3.Scale(zero, new Vector3(num3, num3, num3));
			}
		}

		private float CalculatePreferredSpeed()
		{
			float minMovementSpeed = GetCachedMinMoveSpeed() * 1.2f;
			return CalculatePreferredMoveSpeed(minMovementSpeed);
		}

		public float GetCachedMinMoveSpeed()
		{
			if (minMoveSpeed < 0f || Time.time - lastGroupMinMoveSpeedCalculation > 20f)
			{
				minMoveSpeed = AIGroupMinMoveSpeedCalculator.Calculate(this);
				lastGroupMinMoveSpeedCalculation = Time.time;
			}
			return minMoveSpeed;
		}

		private ActiveFleetOrder TakeNextOrder()
		{
			while (OrderQueue.Count > 0)
			{
				FleetOrder fleetOrder = OrderQueue[0];
				OrderQueue.RemoveAt(0);
				if (fleetOrder != null)
				{
					return fleetOrder.CreateActiveFleetOrder();
				}
			}
			return null;
		}

		public float CalculateCargoUsage()
		{
			float num = 0f;
			foreach (NpcPilot npcPilot in NpcPilots)
			{
				if (npcPilot.CurrentUnit != null && npcPilot.CurrentUnit.CargoBayComponent != null)
				{
					num += npcPilot.CurrentUnit.CargoBayComponent.Usage;
				}
			}
			return num;
		}

		public int GetTotalCapacityForCargoCount(CargoClass cargoClass)
		{
			int num = 0;
			foreach (NpcPilot npcPilot in NpcPilots)
			{
				num += npcPilot.CurrentUnit.CargoBayComponent.GetFreeSpaceFor(cargoClass);
			}
			return num;
		}

		public float CalculateTotalCargoCapacity()
		{
			float num = 0f;
			foreach (NpcPilot npcPilot in NpcPilots)
			{
				UnitComponentHolder currentUnitComponents = npcPilot.CurrentUnitComponents;
				if (currentUnitComponents != null && currentUnitComponents.CargoBayComponent != null)
				{
					num += currentUnitComponents.CargoBayComponent.Capacity;
				}
			}
			return num;
		}

		public float GetCachedTotalCargoCapacity()
		{
			if (Time.time > lastTimeCachedTotalCargoCapacity + 120f)
			{
				cachedTotalCargoCapacity = CalculateTotalCargoCapacity();
				lastTimeCachedTotalCargoCapacity = Time.time;
			}
			return cachedTotalCargoCapacity;
		}

		public float GetFreeCargoSpace()
		{
			float num = 0f;
			foreach (NpcPilot npcPilot in NpcPilots)
			{
				if (npcPilot.CurrentUnit != null && npcPilot.CurrentUnit.CargoBayComponent != null)
				{
					num += npcPilot.CurrentUnit.CargoBayComponent.FreeSpace;
				}
			}
			return num;
		}

		public float GetCachedFreeCargoSpace()
		{
			if (Time.time > lastTimeCachedCargoUsageStats + 60f)
			{
				CacheCargoUsageStats();
				lastTimeCachedCargoUsageStats = Time.time;
			}
			return cachedFreeCargoCapacity;
		}

		public float GetCachedCargoUsage()
		{
			return GetCachedTotalCargoCapacity() - GetCachedFreeCargoSpace();
		}

		[ContextMenu("Cache cargo usage stats")]
		public void CacheCargoUsageStats()
		{
			GetEquipmentCargo(out cachedIncompatibleEquipmentLoad, out cachedIncompatibleEquipmentValue, out cachedCompatibleEquipmentLoad);
			cachedTradableCargoValue = GetTradableCargoValue();
			cachedFreeCargoCapacity = GetFreeCargoSpace();
			cachedTradableCargoLoad = GetTradableCargoLoad();
		}

		public void GetEquipmentCargo(out float incompatibleEquipmentLoad, out float incompatibleEquipmentValue, out float compatibleEquipmentLoad)
		{
			incompatibleEquipmentLoad = 0f;
			incompatibleEquipmentValue = 0f;
			compatibleEquipmentLoad = 0f;
			foreach (NpcPilot npcPilot in NpcPilots)
			{
				if (!(npcPilot.CurrentUnit != null) || !(npcPilot.CurrentUnit.CargoBayComponent != null) || !(npcPilot.CurrentUnit.CargoBayComponent.EquipmentLoad > 0f))
				{
					continue;
				}
				foreach (KeyValuePair<CargoClass, int> cargo in npcPilot.CurrentUnit.CargoBayComponent.Cargos)
				{
					if (!npcPilot.IsCargoClassCompatible(cargo.Key))
					{
						incompatibleEquipmentLoad += cargo.Key.Volume * (float)cargo.Value;
						incompatibleEquipmentValue += cargo.Key.BasePrice * cargo.Value;
					}
					else
					{
						compatibleEquipmentLoad += cargo.Key.Volume * (float)cargo.Value;
					}
				}
			}
		}

		public float GetTradableCargoValue()
		{
			float num = 0f;
			foreach (NpcPilot npcPilot in NpcPilots)
			{
				if (npcPilot.CurrentUnit != null && npcPilot.CurrentUnit.CargoBayComponent != null)
				{
					num += npcPilot.CurrentUnit.CargoBayComponent.TradableCargoValue;
				}
			}
			return num;
		}

		public float GetTradableCargoLoad()
		{
			float num = 0f;
			foreach (NpcPilot npcPilot in NpcPilots)
			{
				if (npcPilot.CurrentUnit != null && npcPilot.CurrentUnit.CargoBayComponent != null)
				{
					num += npcPilot.CurrentUnit.CargoBayComponent.TradableCargoLoad;
				}
			}
			return num;
		}

		public float GetCachedTradableCargoLoad()
		{
			CacheCargoStatsIfStale();
			return cachedTradableCargoLoad;
		}

		public float GetCachedTradableCargoValue()
		{
			CacheCargoStatsIfStale();
			return cachedTradableCargoValue;
		}

		public float GetCachedIncompatibleEquipmentValue()
		{
			CacheCargoStatsIfStale();
			return cachedIncompatibleEquipmentValue;
		}

		public float GetCachedIncompatibleEquipmentLoad()
		{
			CacheCargoUsageStats();
			return cachedIncompatibleEquipmentLoad;
		}

		public float GetCachedCompatibleEquipmentLoad()
		{
			CacheCargoUsageStats();
			return cachedCompatibleEquipmentLoad;
		}

		private void CacheCargoStatsIfStale()
		{
			if (Time.time > lastTimeCachedCargoUsageStats + 60f)
			{
				CacheCargoUsageStats();
				lastTimeCachedCargoUsageStats = Time.time;
			}
		}

		public float GetFreeCargoSpace01()
		{
			float num = CalculateTotalCargoCapacity();
			if (num > 0f)
			{
				return GetFreeCargoSpace() / num;
			}
			return 0f;
		}

		public void InvalidateCargoCapacityStats()
		{
			lastTimeCachedCargoUsageStats = float.MinValue;
			lastTimeCachedTotalCargoCapacity = float.MinValue;
		}

		public void InvalidateCargoUsageStats()
		{
			lastTimeCachedCargoUsageStats = float.MinValue;
		}

		public float GetCachedFreeCargoSpace01()
		{
			float num = GetCachedTotalCargoCapacity();
			if (num > 0f)
			{
				return GetCachedFreeCargoSpace() / num;
			}
			return 0f;
		}

		public int GetCargoCount(CargoClass cargoClass)
		{
			int num = 0;
			foreach (NpcPilot npcPilot in NpcPilots)
			{
				num += npcPilot.CurrentUnit.CargoBayComponent.GetCountOf(cargoClass);
			}
			return num;
		}

		private void OnFactionChanged(Faction oldFaction)
		{
			if (oldFaction != null)
			{
				oldFaction.Fleets.Remove(this);
			}
			if (faction != null)
			{
				faction.Fleets.Add(this);
			}
		}

		private void OnTargetSet(ActiveFleetOrder requestor)
		{
			navTarget.waitingForLeaderToReachNavPoint = false;
			navTarget.waypoints.Clear();
			navTarget.SourceOrder = requestor;
			navTarget.IsActive = true;
			navTarget.HasAttemptedToCalculatedPath = false;
		}

		public float GetCachedSimpleCombatRating()
		{
			return cachedCombatRating;
		}

		public void UpdateSimpleCombatRating()
		{
			cachedCombatRating = CalculateSimpleCombatRating();
		}

		public float CalculateSimpleCombatRating()
		{
			float num = 0f;
			foreach (NpcPilot pilot in pilots)
			{
				UnitComponentHolder currentUnitComponents = pilot.CurrentUnitComponents;
				if (currentUnitComponents != null)
				{
					num += currentUnitComponents.Unit.CombatRating;
				}
			}
			return num;
		}

		internal IEnumerable<CargoClass> GetCargoBayClasses()
		{
			cargoBayClassesCache.Clear();
			foreach (NpcPilot pilot in pilots)
			{
				Unit currentUnit = pilot.CurrentUnit;
				if (!(currentUnit != null))
				{
					continue;
				}
				foreach (CargoClass cargoClass in currentUnit.CargoBayComponent.CargoClasses)
				{
					if (!cargoBayClassesCache.Contains(cargoClass))
					{
						cargoBayClassesCache.Add(cargoClass);
					}
				}
			}
			return cargoBayClassesCache;
		}

		public bool HasTradableCargosUsingCache()
		{
			return GetCachedTradableCargoValue() > 0f;
		}

		public bool CalculateHasTradableCargos()
		{
			foreach (NpcPilot pilot in pilots)
			{
				UnitComponentHolder currentUnitComponents = pilot.CurrentUnitComponents;
				if (currentUnitComponents != null && currentUnitComponents.CargoBayComponent.HasTradableCargos())
				{
					return true;
				}
			}
			return false;
		}

		public void SetOrder(FleetOrder order)
		{
			ClearOrders();
			EnqueueOrder(order);
			AssignNextOrder();
		}

		public void OnFleetOrderMaxDurationReached(ActiveFleetOrder order)
		{
			if (faction != null && faction.IsPlayerFaction && order.Fleet != null && order.Fleet.PilotCount > 0)
			{
				EngineASX.Instance.OnPlayerFleetMaxDurationReached(order);
			}
		}

		public void OnInvalidFleetOrder(ActiveFleetOrder order, string message)
		{
			if (faction != null && faction.IsPlayerFaction && order.Fleet != null && order.Fleet.PilotCount > 0)
			{
				EngineASX.Instance.OnPlayerFleetInvalidOrder(order, message);
			}
		}

		public bool HasAnyShipGotAMiningTurret()
		{
			foreach (UnitComponentHolder ship in Ships)
			{
				if (ship != null && ship.Unit != null && ship.Unit.HasMiningLaser())
				{
					return true;
				}
			}
			return false;
		}

		public bool HasAnyShipGotATractorTurret()
		{
			foreach (UnitComponentHolder ship in Ships)
			{
				if (ship != null && ship.Unit != null && ship.TractorTurret != null)
				{
					return true;
				}
			}
			return false;
		}

		public bool HasMiningEquipment()
		{
			if (HasAnyShipGotAMiningTurret())
			{
				return HasAnyShipGotATractorTurret();
			}
			return false;
		}

		public bool IsAnyShipDocked()
		{
			foreach (UnitComponentHolder ship in Ships)
			{
				if (ship != null && ship.IsDocked)
				{
					return true;
				}
			}
			return false;
		}

		public bool AreAllShipsUndocked()
		{
			foreach (UnitComponentHolder ship in Ships)
			{
				if (ship != null && ship.IsDocked)
				{
					return false;
				}
			}
			return true;
		}

		public string GetFriendlyName(bool shortName = true)
		{
			if (!string.IsNullOrEmpty(Name))
			{
				return Name;
			}
			if (shortName)
			{
				return "Fleet " + GetShortDesignation();
			}
			return "Fleet " + GetDesignation();
		}

		public string GetFriendlyNameNoPrefix(bool shortName = false)
		{
			if (!string.IsNullOrEmpty(Name))
			{
				return Name;
			}
			if (shortName)
			{
				return GetShortDesignation();
			}
			return GetDesignation();
		}

		public string GetShortDesignation()
		{
			if (designation == null)
			{
				UpdateDesignation();
			}
			return shortDesignation;
		}

		public string GetDesignation()
		{
			if (designation == null)
			{
				UpdateDesignation();
			}
			return designation;
		}

		private void UpdateDesignation()
		{
			if (Seed < 0)
			{
				Debug.LogWarning("Seed was not set", this);
			}
			UnitDesignationBuilder.CalculateFleetDesignation(Seed, out designation, out shortDesignation);
		}

		public float GetActualMaxHealth()
		{
			float num = 0f;
			foreach (UnitComponentHolder ship in Ships)
			{
				if (ship != null)
				{
					num += ship.Unit.UnitClass.maxHealth;
				}
			}
			return num;
		}

		public float GetActualHealth()
		{
			float num = 0f;
			foreach (UnitComponentHolder ship in Ships)
			{
				if (ship != null && ship.Unit != null && ship.Unit.Destructable != null)
				{
					num += ship.Unit.Destructable.CurrentHealth;
				}
			}
			return num;
		}

		public float GetActualMaxShieldHealth()
		{
			float num = 0f;
			foreach (UnitComponentHolder ship in Ships)
			{
				if (ship != null && ship.ShieldComponent != null)
				{
					num += ship.ShieldComponent.ShieldClass.TotalCapacity;
				}
			}
			return num;
		}

		public float GetActualShieldHealth()
		{
			float num = 0f;
			foreach (UnitComponentHolder ship in Ships)
			{
				if (ship != null && ship.ShieldComponent != null)
				{
					num += ship.ShieldComponent.CurrentTotalShieldPoints;
				}
			}
			return num;
		}

		public float GetCachedMaxHealth()
		{
			if (HealthStatsStale())
			{
				CacheHealthStats();
			}
			return lastCachedMaxHealth;
		}

		public float GetCachedHealth()
		{
			if (HealthStatsStale())
			{
				CacheHealthStats();
			}
			return lastCachedHealth;
		}

		public float GetCachedMaxShieldHealth()
		{
			if (HealthStatsStale())
			{
				CacheHealthStats();
			}
			return lastCachedMaxShieldHealth;
		}

		public float GetCachedShieldHealth()
		{
			if (HealthStatsStale())
			{
				CacheHealthStats();
			}
			return lastCachedShieldHealth;
		}

		private bool HealthStatsStale()
		{
			return Time.time - lastCachedHealthStatsTime > 5f;
		}

		public void CacheHealthStats()
		{
			lastCachedHealth = GetActualHealth();
			lastCachedMaxHealth = GetActualMaxHealth();
			lastCachedShieldHealth = GetActualShieldHealth();
			lastCachedMaxShieldHealth = GetActualMaxShieldHealth();
			lastCachedHealthStatsTime = Time.time;
		}

		public void SetHomeBaseToUnit(Unit unit)
		{
			SetHomeBase(SectorTarget.FromUnit(unit));
		}

		public void SetHomeBaseToSectorPosition(Sector sector, Vector3 sectorPosition)
		{
			SetHomeBase(SectorTarget.FromSectorPosition(sector, sectorPosition));
		}

		public void SetHomeBase(SectorTarget sectorTarget)
		{
			homeBase = sectorTarget;
		}

		public static bool IsHomeBaseUnitValid(Unit unit, Fleet fleet)
		{
			if (unit != null && unit.IsDockable && unit.Faction != null)
			{
				if (!(unit.Faction == fleet.faction))
				{
					return !unit.IsHostileToOrAlwaysHostileToTwoWay(fleet.faction);
				}
				return true;
			}
			return false;
		}

		public bool CanUnitsFitInHangarIgnoreOccupancy(Unit unit)
		{
			UnitHangar hangar = unit.GetHangar();
			if (hangar != null)
			{
				return hangar.CanUnitsFitInHangarIgnoreOccupancy(npcShipHullTypes);
			}
			return false;
		}

		public string GetFleetStatus()
		{
			return GetFleetStatusText(curState);
		}

		public static string GetFleetStatusText(FleetState fleetState)
		{
			return fleetState switch
			{
				FleetState.CombatInterception => "In Combat", 
				FleetState.Docking => "Docking", 
				FleetState.EnteringGate => "Entering wormhole", 
				FleetState.Regrouping => "Regrouping", 
				_ => null, 
			};
		}
	}
}
