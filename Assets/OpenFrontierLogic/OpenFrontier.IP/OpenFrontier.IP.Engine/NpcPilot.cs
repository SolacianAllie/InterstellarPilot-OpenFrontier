using System;
using System.Collections.Generic;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.AutoTurrets;
using OpenFrontier.IP.Engine.Dialog;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.Engine.Npcs;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.Engine
{
	public class NpcPilot : UnitController
	{
		public enum AIAvoidType
		{
			None,
			Leisurely,
			Quick,
			MissileAvoid,
			OwnMine
		}

		public enum AIControllerCombatMode
		{
			Defensive,
			Offensive
		}

		private const float automationThresholdDistance = 800f;

		private Unit currentUnit;

		private UnitComponentHolder currentUnitComponents;

		private float nearestHostileTargetDistance = float.MaxValue;

		private NpcPilotTargetter targetter;

		private NpcTargetScanner targetScanner;

		private float nextAvoidMineCheckTime;

		public bool DestroyWhenNotPilotting;

		public bool DestroyWhenNoUnit = true;

		[NonSerialized]
		public EngineASX Engine;

		private bool hasInit;

		private float lastTimeBetweenUpdates;

		private Person person;

		private float lastCombatNavpointDistanceCheck;

		private bool hasStaticUnit;

		private int nextTurretAutoLoadIndex;

		private const float MoveThrottleRate = 0.5f;

		private AICombatNavigator combatNavigator;

		private HashSet<int> compatibleAmmoTypes;

		private float lastCachedCompatibleAmmoTypes = -1f;

		private const float maxAgeOfCachedCompatibleAmmoTypes = 20f;

		private NpcCargoCollector cargoCollector;

		private float componentUseCooldownTime;

		private const float pointDefenseDeployMinCooldown = 0.5f;

		private const float pointDefenseDeployMaxCooldown = 1f;

		private const float LowCapacitorThreshold = 0.15f;

		private const float LowCapacitorMinRecharge = 0.25f;

		private const float LowCapacitorMaxRecharge = 0.5f;

		public const float DragMultiplier = 6f;

		private const float minTimeBeforeSelectTarget = 3f;

		private const float minMissileAvoidCheckInterval = 0.25f;

		private const float maxMissileAvoidCheckInterval = 2f;

		[FormerlySerializedAs("aiGroup")]
		[SerializeField]
		private Fleet fleet;

		public bool AllowUseCloak = true;

		private bool angularArrive = true;

		private float avoidDistance;

		private GameObject avoidTarget;

		private float avoidTimeout;

		private AIAvoidType avoidType;

		private AIControllerCombatMode combatMode;

		private Unit combatTarget;

		private Faction combatTargetFaction;

		private bool combatTargetIsPriority;

		private Rigidbody combatTargetRigidBody;

		private NpcPilotSettings pilotSettings;

		private float countermeasureDeployNextCheckTime;

		private float desiredAngleY;

		private float desiredBearing;

		private float dockCoolDownTime;

		private float lowCapacitorCooldownCharge;

		public Vector3 FormationPosition = Vector3.zero;

		private float lastCombatTargetDistance;

		private Vector3 lastKnownCombatTargetSectorPosition = Vector3.zero;

		private float lowCapacitorCooldownTime;

		private float lastNavPointDistance;

		private bool lowCapacitorCooldownActive;

		private List<ProjectileTurretComponent> mineDropperTurrets = new List<ProjectileTurretComponent>(3);

		private bool movingToFormation;

		private NpcPilotNavpoint targetNavpoint;

		private NpcPilotNavpointType targetNavpointType;

		private float lastPathfindTime = -100f;

		private bool isNavpointFromPathfind;

		private List<Vector3> pathfindResult;

		private float nearestNonHostileTargetDistance = float.MaxValue;

		private float newDesiredTurn;

		private float nextAutoLoadTurretTime;

		private float nextMissileAvoidUpdate;

		private float nextMineSolutionCalc;

		private float nextCombatTargetSearch;

		private float pointDefenseDeployNextCheck;

		private Missile avoidingMissile;

		private int combatFiringCurrentCheckIndex = -1;

		private float nextCheckTurretTime;

		public Unit CurrentUnit
		{
			get
			{
				return currentUnit;
			}
			set
			{
				if (!(value != CurrentUnit))
				{
					return;
				}
				if (value != null)
				{
					value.Components.PilotPerson = person;
					return;
				}
				UnitComponentHolder unitComponentHolder = CurrentUnitComponents;
				if (unitComponentHolder != null && unitComponentHolder.PilotPerson == person)
				{
					unitComponentHolder.PilotPerson = null;
				}
			}
		}

		public float LastTimeBetweenUpates => lastTimeBetweenUpdates;

		public Fleet Fleet
		{
			get
			{
				return fleet;
			}
			set
			{
				if (fleet != value)
				{
					SetFleet(value);
				}
			}
		}

		public bool IsFleetLeader
		{
			get
			{
				if (fleet != null)
				{
					return this == fleet.Leader;
				}
				return false;
			}
		}

		public bool HasCombatTargetOrGroupInCombat
		{
			get
			{
				if (!(combatTarget != null))
				{
					if (fleet != null)
					{
						return fleet.InCombat;
					}
					return false;
				}
				return true;
			}
		}

		public float LastCombatTargetDistance => lastCombatTargetDistance;

		public Vector3 LastKnownCombatTargetSectorPosition => lastKnownCombatTargetSectorPosition;

		public Unit CombatTarget
		{
			get
			{
				return combatTarget;
			}
			private set
			{
				if (!(combatTarget != value))
				{
					return;
				}
				combatTargetRigidBody = null;
				Unit unit = combatTarget;
				combatTarget = value;
				if (unit != null && unit.Destructable != null)
				{
					unit.Destructable.Damaged -= CombatTargetDamaged;
				}
				if (CurrentUnit != null)
				{
					SetRestrictedWeaponsFireTime();
				}
				if (combatTarget != null)
				{
					combatTargetFaction = combatTarget.Faction;
					combatTarget.Destructable.Damaged += CombatTargetDamaged;
					UpdateCombatTargetDist();
					combatTargetRigidBody = combatTarget.RBody;
					UpdateLastKnownCombatTargetPosition();
					CreateCombatNavigatorIfNull();
					combatNavigator.CacheWeaponStats();
					if (Engine.LocalPlayer != null && combatTarget == Engine.PlayerUnit && !Engine.LocalPlayer.Person.IsPilot)
					{
					}
				}
				else
				{
					combatTargetFaction = null;
				}
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log(string.Format("{0}: Changed combat target from {1} to {2}", this, unit ? unit.ToString() : "NULL", combatTarget ? combatTarget.ToString() : "NULL"), this, 2);
				}
			}
		}

		public NpcPilotSettings Settings => pilotSettings;

		public AIControllerCombatMode CombatMode
		{
			get
			{
				return combatMode;
			}
			set
			{
				if (combatMode != value)
				{
					combatMode = value;
				}
			}
		}

		public GameObject AvoidTarget
		{
			get
			{
				return avoidTarget;
			}
			set
			{
				if (avoidTarget != value)
				{
					avoidTarget = value;
				}
			}
		}

		public bool MovingToFormation
		{
			get
			{
				return movingToFormation;
			}
			set
			{
				movingToFormation = value;
			}
		}

		public float NextCombatTargetSearch
		{
			get
			{
				return nextCombatTargetSearch;
			}
			set
			{
				nextCombatTargetSearch = value;
			}
		}

		public AICombatNavigator CombatNavigator => combatNavigator;

		public bool LowCapacitorCooldownActive
		{
			get
			{
				return lowCapacitorCooldownActive;
			}
			set
			{
				if (lowCapacitorCooldownActive != value)
				{
					lowCapacitorCooldownActive = value;
					if (lowCapacitorCooldownActive)
					{
						lowCapacitorCooldownCharge = UnityEngine.Random.Range(0.25f, 0.5f);
						lowCapacitorCooldownTime = Time.time;
					}
				}
			}
		}

		public Rigidbody UnitRigidBody => CurrentUnit.RBody;

		public Faction Faction
		{
			get
			{
				Person person = Person;
				if (person != null)
				{
					return person.Faction;
				}
				return null;
			}
		}

		public float ComponentUseCooldownTime
		{
			get
			{
				return componentUseCooldownTime;
			}
			set
			{
				componentUseCooldownTime = value;
			}
		}

		public bool HasComponentUseCooldownTimeExpired => Time.time > componentUseCooldownTime;

		public Vector3 SectorPosition => CurrentUnit.SectorPosition;

		public bool IsInCombat
		{
			get
			{
				if (!(fleet != null) || !fleet.InCombat)
				{
					return combatTarget != null;
				}
				return true;
			}
		}

		public Sector Sector
		{
			get
			{
				if (person == null)
				{
					return null;
				}
				Unit unit = person.CurrentUnit;
				if (unit != null)
				{
					return unit.Sector;
				}
				return null;
			}
		}

		public Person Person
		{
			get
			{
				return person;
			}
			private set
			{
				if (person != value)
				{
					_ = person;
					person = value;
					if (person != null)
					{
						person.FindNpcPilot();
					}
				}
			}
		}

		public UnitComponentHolder CurrentUnitComponents
		{
			get
			{
				return currentUnitComponents;
			}
			set
			{
				if (!(currentUnitComponents != value))
				{
					return;
				}
				_ = fleet;
				OnUnitChanging((value != null) ? value.Unit : null);
				UnitComponentHolder unitComponentHolder = CurrentUnitComponents;
				currentUnitComponents = value;
				currentUnit = ((currentUnitComponents != null) ? currentUnitComponents.Unit : null);
				if (unitComponentHolder != null)
				{
					unitComponentHolder.Unit.SectorChanged -= controlledUnit_SceneChanged;
					unitComponentHolder.PilotNpc = null;
				}
				if (currentUnitComponents != null)
				{
					CurrentUnitComponents.Unit.Init();
					currentUnitComponents.Unit.SectorChanged += controlledUnit_SceneChanged;
					person.CurrentUnit = CurrentUnit;
					if (currentUnitComponents.PilotPerson == null && currentUnitComponents.Unit.IsPilottable())
					{
						currentUnitComponents.PilotPerson = person;
					}
					transform.localPosition = Vector3.zero;
					if (currentUnitComponents.Unit.IsActiveInEngine)
					{
						OnUnitActive();
					}
					EvaluateComponents();
					hasStaticUnit = currentUnitComponents.Unit.IsStatic;
					if (fleet != null && unitComponentHolder != null)
					{
						fleet.RecacheControlledUnits();
					}
				}
				else
				{
					Fleet = null;
				}
			}
		}

		public NpcPilotNavpoint Navpoint => targetNavpoint;

		public NpcPilotNavpointType NavpointType => targetNavpointType;

		public bool IsPathfindingToNavpoint => isNavpointFromPathfind;

		public IEnumerable<Vector3> PathfindWaypoints => pathfindResult;

		public NpcTargetScanner TargetScanner => targetScanner;

		internal void OnPilotStatusChanged(bool isPilot)
		{
			if (isPilot)
			{
				SetupUnitForNpcPilotting();
			}
		}

		internal void OnPilotStatusChanging(UnitComponentHolder ship)
		{
			if (ship != null)
			{
				if (ship.AutoTurretModule != null)
				{
					ship.AutoTurretModule.FireMode = AutoTurretFireMode.PreferredTargetOnly;
				}
				ship.SetConventionalTurretsAutoFire(autoFire: false);
			}
		}

		public void Init()
		{
			if (!hasInit)
			{
				hasInit = true;
				targetter = new NpcPilotTargetter(this);
				if (person == null)
				{
					Person = GetComponent<Person>();
				}
				Engine = EngineASX.Instance;
				FindOrCreateControllerProfile();
				FindUnit();
				onInit();
			}
		}

		private void controlledUnit_SceneChanged(Unit sender, Sector oldScene)
		{
			OnUnitSectorChanged(oldScene);
		}

		private void CombatTargetDamaged(DestructableUnit sender, Unit inflictor, Faction inflictorFaction, float damage)
		{
			if (!(inflictorFaction != null))
			{
				return;
			}
			Faction faction = Faction;
			if (faction != null && inflictorFaction != faction && !faction.IsHostileTo(inflictorFaction))
			{
				FactionAttitude factionAttitude = faction.GetAttitude(inflictorFaction);
				float num = 0f;
				if (factionAttitude != null)
				{
					num = factionAttitude.Opinion;
				}
				else
				{
					factionAttitude = faction.GetOrCreateAttitude(inflictorFaction);
				}
				if (num < 1f)
				{
					float num2 = damage * GameController.Instance.GameSettings.FactionSettings.AssistedDamageOpinionEffect;
					factionAttitude.Opinion += num2;
				}
			}
		}

		private void UpdateLastKnownCombatTargetPosition()
		{
			lastKnownCombatTargetSectorPosition = combatTarget.SectorPosition;
		}

		public static float ApplyDesiredBearing(float desiredBearing, float spd, float acceleration, float maxSpd, float turnAggression)
		{
			if (spd == 0f)
			{
				return Mathf.Sign(desiredBearing);
			}
			if (Mathf.Sign(spd) != Mathf.Sign(desiredBearing))
			{
				return Mathf.Sign(desiredBearing);
			}
			float num = Maths.CalculateStoppingDistance(spd, acceleration * turnAggression);
			return (desiredBearing - num) / acceleration;
		}

		public override string ToString()
		{
			if (this != null)
			{
				return $"[AIU: {((CurrentUnitComponents != null && CurrentUnitComponents.Unit != null) ? CurrentUnitComponents.Unit.name : name)}]";
			}
			return null;
		}

		private bool UnitIsCurrentGroupWaypoint(Unit unit)
		{
			if (fleet != null)
			{
				WorldNavpoint? currentWaypoint = fleet.GetCurrentWaypoint();
				if (currentWaypoint.HasValue)
				{
					return currentWaypoint.Value.TargetSectorObject == unit;
				}
			}
			return false;
		}

		private bool UnitOverlapsGroupWaypoint(Unit unit)
		{
			if (fleet != null)
			{
				WorldNavpoint? currentWaypoint = fleet.GetCurrentWaypoint();
				if (currentWaypoint.HasValue && currentWaypoint.Value.TargetSectorObject == null && currentWaypoint.Value.GetTargetSector() == unit.Sector && unit.OverlapsSectorPosition(currentWaypoint.Value.GetTargetSectorPosition(), 1.5f))
				{
					return true;
				}
			}
			return false;
		}

		public void RequestAvoidUnit(Unit requestingUnit, float minDistance, float maxDistance)
		{
			if ((!IsFleetLeader || !UnitIsInFleet(requestingUnit)) && enabled && avoidTarget == null && CurrentUnit != null && CurrentUnit.IsActiveInEngine && EngineASX.Instance.GameSettings.ActiveSectorPathfindingSettings.UseCrappyAvoidanceAlgorithm && TargetBearingRequiresAvoid(requestingUnit.gameObject))
			{
				float a = Mathf.Min(180f / CurrentUnit.UnitClass.turnRate, 10f);
				a = Mathf.Max(a, 2f);
				StartAvoidTarget(requestingUnit.gameObject, maxDistance + CurrentUnit.UnitClass.ShieldRingRadius, a, AIAvoidType.Leisurely);
			}
		}

		private bool UnitIsInFleet(Unit requestingUnit)
		{
			return requestingUnit.GetFleet() == fleet;
		}

		public void UpdateTurretAutoLoad()
		{
			if (!(Time.time > nextAutoLoadTurretTime))
			{
				return;
			}
			if (CurrentUnitComponents.Turrets.Count > 0)
			{
				nextTurretAutoLoadIndex++;
				if (nextTurretAutoLoadIndex >= CurrentUnitComponents.Turrets.Count)
				{
					nextTurretAutoLoadIndex = 0;
				}
				ProjectileTurretComponent projectileTurretComponent = CurrentUnitComponents.Turrets[nextTurretAutoLoadIndex] as ProjectileTurretComponent;
				if (projectileTurretComponent != null)
				{
					AutoLoadTurret(projectileTurretComponent);
				}
			}
			nextAutoLoadTurretTime = Time.time + 0.1f;
		}

		public void AutoLoadAllTurrets()
		{
			foreach (TurretComponent turret in currentUnitComponents.Turrets)
			{
				ProjectileTurretComponent projectileTurretComponent = turret as ProjectileTurretComponent;
				if (projectileTurretComponent != null)
				{
					AutoLoadTurret(projectileTurretComponent);
				}
			}
		}

		private void AutoLoadTurret(ProjectileTurretComponent projectileTurret)
		{
			if (projectileTurret.AutoFireActive)
			{
				return;
			}
			bool usesAmmo = projectileTurret.UsesAmmo;
			if (!TurretRequiresChangeOfAmmo(projectileTurret, usesAmmo, combatTarget, lastCombatTargetDistance))
			{
				return;
			}
			float? mobileTargetDistance = null;
			if (combatTarget != null && !combatTarget.IsStatic && lastCombatTargetDistance < 1500f)
			{
				mobileTargetDistance = lastCombatTargetDistance;
			}
			if (Settings.AICheatAmmo)
			{
				if (projectileTurret.TrySelectRandomConventionalProjectileClass(ignoreAmmo: true, mobileTargetDistance) && usesAmmo)
				{
					CurrentUnit.CargoBayComponent.GiveMinCargo(projectileTurret.CurProjectileClass.AmmoClass, 3);
				}
			}
			else
			{
				projectileTurret.TrySelectRandomConventionalProjectileClass(ignoreAmmo: false, mobileTargetDistance);
			}
		}

		public static bool TurretRequiresChangeOfAmmo(ProjectileTurretComponent projectileTurret, bool usesAmmo, Unit combatTarget, float combatTargetDistance)
		{
			if (projectileTurret.CurProjectileClass == null)
			{
				return true;
			}
			if (projectileTurret.ProjectileTurretClass.CompatibleProjectiles.Count == 1)
			{
				return false;
			}
			if (usesAmmo && !projectileTurret.HasRequiredAmmo)
			{
				return true;
			}
			if (combatTarget != null && combatTargetDistance < 1500f)
			{
				if (combatTarget.IsStatic)
				{
					return true;
				}
				return projectileTurret.CurProjectileClass.EffectiveFiringRangeAgainstMobile < combatTargetDistance;
			}
			return false;
		}

		private bool SetNavpointToFormationPositionWhenIdle()
		{
			if (fleet.Ships.Count == 1)
			{
				return false;
			}
			return SetNavpointToFormationPosition();
		}

		private bool SetNavpointToFormationPosition()
		{
			if (Sector == fleet.Sector)
			{
				SetNavpointToArriveAtSectorPosition(GetFormationSectorPosition());
				if (!fleet.HasHostileTargets)
				{
					ResetNavpointSlowArrivalDefaults();
				}
				return true;
			}
			return false;
		}

		private Vector3 GetFormationSectorPosition()
		{
			return fleet.GetPilotFleetFormationSectorPosition(this);
		}

		private void SetNavpointToArriveAtSectorPosition(Vector3 sectorPosition)
		{
			targetNavpoint.SectorPosition = sectorPosition;
			targetNavpoint.Arrive = true;
			ResetNavpointArrivalDefaults();
		}

		public void HandleGroupPilotNavigated()
		{
			if (fleet != null)
			{
				fleet.HandleControllerReachedGroupNavpoint(this);
			}
			currentUnitComponents.lastKnownVelocity = Vector3.zero;
		}

		public Unit GetNearestNonHostileDist(out float nearestDistance)
		{
			nearestDistance = float.MaxValue;
			Unit result = null;
			Unit unit = CurrentUnit;
			int num = Physics.OverlapSphereNonAlloc(unit.transform.position, 300f, EngineASX.ColliderCache, GameController.Instance.ShipsAndStationsMask, QueryTriggerInteraction.Collide);
			for (int i = 0; i < num; i++)
			{
				Unit component = EngineASX.ColliderCache[i].GetComponent<Unit>();
				if (component != null && component.IsValidAndNotDestroyed && component.Sector == Sector && component != unit && (unit.Faction == null || !unit.Faction.IsHostileTo(component)))
				{
					float num2 = Vector3.Distance(transform.position, component.transform.position);
					if (num2 < nearestDistance)
					{
						nearestDistance = num2;
						result = component;
					}
				}
			}
			return result;
		}

		public void NotifyFleetOutOfCombatInteceptionState()
		{
			if (!fleet.InCombat)
			{
				CombatTarget = null;
				CombatMode = AIControllerCombatMode.Defensive;
			}
		}

		public void SetDesiredBearingFromTargetSectorPosition(Vector3 targetSectorPosition)
		{
			float yBearing = Unit.GetYBearing(targetSectorPosition - SectorPosition);
			SetDesiredBearing(yBearing);
		}

		public void SetDesiredBearing(float bearing)
		{
			desiredAngleY = bearing;
			UpdateDesiredBearing();
			if (requireTurn())
			{
				angularArrive = false;
			}
		}

		public void ClearDesiredBearing()
		{
			desiredAngleY = Unit.GetYBearing(CurrentUnit.transform.forward);
			AngularArrive();
		}

		public void UpdateMineDropperTurretsRefs()
		{
			mineDropperTurrets.Clear();
			if (!(CurrentUnit != null))
			{
				return;
			}
			for (int i = 0; i < CurrentUnit.Components.Turrets.Count; i++)
			{
				ProjectileTurretComponent projectileTurretComponent = CurrentUnit.Components.Turrets[i] as ProjectileTurretComponent;
				if (projectileTurretComponent != null && projectileTurretComponent.ProjectileTurretClass.ProjectileTurretType == TurretType.Mine)
				{
					mineDropperTurrets.Add(projectileTurretComponent);
				}
			}
		}

		public void StartAvoidTarget(GameObject gameObject, float distance, float duration, AIAvoidType avoidType)
		{
			avoidTarget = gameObject;
			avoidDistance = distance;
			avoidTimeout = Time.time + duration;
			this.avoidType = avoidType;
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: Avoiding: {gameObject}", this, 3);
			}
		}

		public void StartAvoidMissile(Missile missile)
		{
			avoidingMissile = missile;
			avoidTarget = missile.gameObject;
			avoidDistance = 3500f;
			avoidTimeout = Time.time + 20f;
			avoidType = AIAvoidType.MissileAvoid;
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: Avoiding: {gameObject}", this, 3);
			}
		}

		private static float GetDistance2D(Vector3 ourPosition, Vector3 targetPosition)
		{
			return Vector2.Distance(new Vector2(ourPosition.x, ourPosition.z), new Vector2(targetPosition.x, targetPosition.z));
		}

		private static float GetDistance2D(ref Vector3 ourPosition, ref Vector3 targetPosition)
		{
			return Vector2.Distance(new Vector2(ourPosition.x, ourPosition.z), new Vector2(targetPosition.x, targetPosition.z));
		}

		public void SetDockCooldownTime()
		{
			if (CurrentUnitComponents.Unit.IsOwnedByPlayer)
			{
				dockCoolDownTime = Time.time + Engine.GameSettings.AIDockPlayerOwnedMinCoolDownTime;
			}
			else
			{
				dockCoolDownTime = Time.time + Engine.GameSettings.AIDockMinCoolDownTime;
			}
		}

		protected void OnUnitSectorChanged(Sector oldScene)
		{
			ClearNavpoint();
			ClearAvoidTarget();
			ClearDesiredBearing();
			ClearPathfindingData();
		}

		protected void onInit()
		{
			cargoCollector = new NpcCargoCollector
			{
				Npc = this
			};
			nextCombatTargetSearch = Time.time + 3f;
			if (fleet != null)
			{
				SetFleet(fleet);
			}
			if (CurrentUnit != null)
			{
				EvaluateComponents();
			}
		}

		public void FindOrCreateControllerProfile()
		{
			FindControllerProfileIfNull();
			if (pilotSettings == null)
			{
				pilotSettings = gameObject.AddComponent<NpcPilotSettings>();
			}
		}

		private void FindControllerProfileIfNull()
		{
			if (pilotSettings == null)
			{
				pilotSettings = GetComponent<NpcPilotSettings>();
			}
		}

		private void EvaluateComponents()
		{
			UpdateMineDropperTurretsRefs();
		}

		protected void UpdateWhenInActiveSector()
		{
			UnitComponentHolder unitComponentHolder = CurrentUnitComponents;
			Unit unit = unitComponentHolder.Unit;
			if (!(unitComponentHolder.PilotPerson == Person))
			{
				return;
			}
			UpdateCommon(Time.deltaTime);
			if (hasStaticUnit)
			{
				return;
			}
			ClearNavpoint();
			if (!unitComponentHolder.IsDocked)
			{
				if (EngineASX.Instance.GameSettings.DebugSettings.NpcMissileAvoidanceEnabled)
				{
					UpdateMissileAvoidance();
				}
				if (avoidingMissile == null && avoidType == AIAvoidType.None && Sector.HasRecentMineDeployment && Time.time > nextAvoidMineCheckTime)
				{
					Projectile avoidMine = GetAvoidMine(out var _);
					if (avoidMine != null)
					{
						StartAvoidTarget(avoidMine.gameObject, GetMineAvoidDist(avoidMine.ProjectileClass.ExplosionClass.MaxDistance), 6f, AIAvoidType.Quick);
					}
					nextAvoidMineCheckTime = Time.time + UnityEngine.Random.Range(0f, 0.2f) + Mathf.Lerp(1f, 0.1f, pilotSettings.CombatEfficiency);
				}
				UpdateAvoidTarget();
			}
			else
			{
				SyncFleetPositionWithDock(unitComponentHolder);
			}
			if (targetNavpointType == NpcPilotNavpointType.None)
			{
				TrySetNavpoint();
			}
			if (!unitComponentHolder.IsDocked && unit.IsActiveInEngine)
			{
				float newDesiredThrottle = 0f;
				if (targetNavpointType != NpcPilotNavpointType.None)
				{
					MoveToNavpointInActiveSector(ref newDesiredThrottle);
				}
				else
				{
					ClearPathfindingData();
				}
				UpdateThrottle(unitComponentHolder, newDesiredThrottle);
				if (unitComponentHolder != null)
				{
					UpdateTurn();
				}
			}
		}

		private void SyncFleetPositionWithDock(UnitComponentHolder pilottedUnitComponents)
		{
			if (IsFleetLeader && !pilottedUnitComponents.DockUnit.IsStatic)
			{
				fleet.transform.localPosition = pilottedUnitComponents.DockUnit.SectorPosition;
			}
		}

		private void UpdateThrottle(UnitComponentHolder pilottedUnitComponents, float newDesiredThrottle)
		{
			UnitEngineComponent engineComponent = pilottedUnitComponents.EngineComponent;
			if (!(engineComponent == null))
			{
				newDesiredThrottle = Mathf.Clamp01(newDesiredThrottle);
				if (currentUnit.IsInActiveSector && currentUnit.ActiveUnit != null && currentUnit.ActiveUnit.LastDistanceFromCamera < 1500f)
				{
					engineComponent.EngineThrottle = Mathf.MoveTowards(engineComponent.EngineThrottle, newDesiredThrottle, Time.deltaTime * 0.5f);
				}
				else
				{
					engineComponent.EngineThrottle = newDesiredThrottle;
				}
			}
		}

		private void ClearNavpoint()
		{
			targetNavpointType = NpcPilotNavpointType.None;
		}

		protected void tickInactive(float elapsedTime)
		{
			UpdateCommon(elapsedTime);
			if (hasStaticUnit)
			{
				return;
			}
			ClearNavpoint();
			if (TrySetNavpoint())
			{
				if (targetNavpointType != NpcPilotNavpointType.None)
				{
					MoveToNavpointInactive(elapsedTime);
				}
				else
				{
					ClearThrottleAndBearing();
				}
			}
			else
			{
				ClearThrottleAndBearing();
			}
		}

		private void ClearThrottleAndBearing()
		{
			ClearThrottle();
			ClearDesiredBearing();
		}

		private void ClearThrottle()
		{
			CurrentUnit.Components.EngineThrottle = 0f;
		}

		protected void OnUnitChanging(Unit newUnit)
		{
			if (newUnit == null)
			{
				if (fleet != null)
				{
					fleet.RemoveNpc(this);
				}
				return;
			}
			newUnit.Init();
			newUnit.Components.SetComponentsPowered(powered: true);
			if (fleet != null)
			{
				newUnit.Faction = fleet.Faction;
				if (newUnit.Faction != null && newUnit.Faction.Intel != null)
				{
					newUnit.Faction.Intel.DiscoverUnit(newUnit);
				}
			}
		}

		public void SetCombatEfficiencyFromFactionRange(Faction newFaction)
		{
			FindOrCreateControllerProfile();
			if (pilotSettings != null)
			{
				pilotSettings.CombatEfficiency = UnityEngine.Random.Range(newFaction.MinAIUnitControllerCombatEfficiency, newFaction.MaxAIUnitControllerCombatEfficiency);
			}
		}

		protected void OnUnitActive()
		{
		}

		protected void OnUnitInactive()
		{
		}

		protected void notifyTurretAdded(ActiveTurret turret)
		{
			EvaluateComponents();
		}

		protected void notifyTurretRemoved(ActiveTurret turret)
		{
			EvaluateComponents();
		}

		protected void notifyMissileLocked(Missile missile, int missileLockCount)
		{
			if (missileLockCount == 1)
			{
				Person.RaiseDialogEventRandomly(Engine.DialogEvents.MissileLock, 2f);
			}
		}

		private void SetRestrictedWeaponsFireTime()
		{
			foreach (TurretComponent turret in CurrentUnit.Components.Turrets)
			{
				if (turret.TurretClass.RestrictAIUsage)
				{
					SetTurretNextFireTime(turret);
				}
			}
		}

		private bool CheckIfSeparatedFromGroup()
		{
			if (fleet != null && fleet.Sector != null && fleet.CurState != FleetState.EnteringGate && Sector != fleet.Sector)
			{
				return !CurrentUnit.HasSameRootUnitAs(fleet.LeaderUnit);
			}
			return false;
		}

		public void Tick(float elapsedTime)
		{
			if (EngineASX.LoadedAndReady && GameController.Instance.GameSettings.DebugSettings.NpcUpdateEnabled && currentUnitComponents != null && CurrentUnit.IsValidAndNotDestroyed && !CurrentUnit.IsUnderConstructionOrDismantling)
			{
				if (CurrentUnit.IsInActiveSector)
				{
					UpdateWhenInActiveSector();
				}
				else
				{
					tickInactive(elapsedTime);
				}
			}
		}

		public bool DestroyWhenNotNeeded()
		{
			if ((CurrentUnit == null || (DestroyWhenNotPilotting && (CurrentUnit.Components == null || CurrentUnit.Components.PilotPerson != person))) && DestroyWhenNoUnit)
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"{this}: Destroying because no controlled unit or not pilotting", this, 2);
				}
				SafeDestroy();
				return true;
			}
			return false;
		}

		public void SafeDestroy()
		{
			Fleet = null;
			if (currentUnitComponents != null)
			{
				if (currentUnitComponents.PilotNpc == this)
				{
					currentUnitComponents.PilotNpc = null;
				}
				CurrentUnit = null;
			}
			UnityEngine.Object.Destroy(this);
		}

		private void UpdateCommon(float elapsedTime)
		{
			if (!hasStaticUnit)
			{
				CreateRegroupOrderWhenSeparated();
			}
			StandDownFromCombatWhenRequired();
			if (fleet == null)
			{
				if (targetScanner == null)
				{
					targetScanner = new NpcTargetScanner(this);
				}
				targetScanner.Update(elapsedTime);
			}
			if (GameController.Instance.GameSettings.DebugSettings.NpcTargettingEnabled)
			{
				UpdateCombatTargetting(elapsedTime);
			}
			if (!CurrentUnitComponents.IsDocked)
			{
				if (!hasStaticUnit)
				{
					if (fleet != null && (fleet.CurState == FleetState.Idle || fleet.CurState == FleetState.MoveToTarget) && fleet.ActiveOrder is ActiveMineOrder mineOrder)
					{
						UpdateMining(mineOrder, elapsedTime);
					}
					if (CurrentUnitComponents.CargoBayComponent != null)
					{
						cargoCollector.Update();
					}
				}
				if (combatTarget != null)
				{
					UpdateTurretAutoLoad();
				}
				if (HasCombatTargetOrGroupInCombat)
				{
					UpdateCombat();
				}
				if (Engine.GameSettings.AICloakingUpdateEnabled)
				{
					UpdateCloaking();
				}
			}
			else if (combatTarget != null && combatMode == AIControllerCombatMode.Offensive && CanUndock())
			{
				CurrentUnitComponents.UndockIfDocked();
			}
		}

		private bool CanUndock()
		{
			if (!DockCooldownElapsed())
			{
				return false;
			}
			if (fleet != null)
			{
				return fleet.RequestUndock(this);
			}
			return true;
		}

		private void UpdateMining(ActiveMineOrder mineOrder, float elapsedTime)
		{
			if (!(mineOrder.MineTarget == null) && mineOrder.MineTarget.IsValidAndNotDestroyed && mineOrder.State == ActiveMineOrderState.Mining)
			{
				if (UseActiveMining())
				{
					UpdateMiningWhenActive(elapsedTime, mineOrder.MineTarget, mineOrder);
				}
				else
				{
					UpdateMiningWhenInactive(elapsedTime, mineOrder.MineTarget, mineOrder);
				}
			}
		}

		private void UpdateMiningWhenActive(float elapsedTime, Unit mineTarget, ActiveMineOrder mineOrder)
		{
			if (!HasComponentUseCooldownTimeExpired)
			{
				return;
			}
			foreach (TurretComponent turret in CurrentUnitComponents.Turrets)
			{
				if (turret.IsMiningLaser)
				{
					LaserTurretComponent laserTurretComponent = (LaserTurretComponent)turret;
					if (laserTurretComponent.IsReadyToFire(mineTarget))
					{
						laserTurretComponent.Fire(mineTarget);
						mineOrder.IsIdle = false;
						UpdateNextComponentUseCooldownTimeForWeaponsFire();
						break;
					}
				}
			}
		}

		public static float CalculateMineTurretDamage(LaserTurretComponent mineTurret)
		{
			return mineTurret.LaserTurretClass.GetRandomDamageFromEnergy() * mineTurret.LaserTurretClass.MiningDamageMultiplier;
		}

		public static float CalculateMineTurretDamagePerSecond(LaserTurretComponent mineTurret)
		{
			float num = mineTurret.LaserTurretClass.EnergyCost / mineTurret.LaserTurretClass.EnergyChargeRate;
			return CalculateMineTurretDamage(mineTurret) / num;
		}

		private void UpdateMiningWhenInactive(float elapsedTime, Unit mineTarget, ActiveMineOrder mineOrder)
		{
			foreach (TurretComponent turret in CurrentUnitComponents.Turrets)
			{
				if (!turret.IsMiningLaser)
				{
					continue;
				}
				LaserTurretComponent laserTurretComponent = (LaserTurretComponent)turret;
				if (!laserTurretComponent.IsReadyToFireIgnoringTarget() || !laserTurretComponent.IsTargetInFiringRange(Maths.GetDistanceIgnoreY(mineTarget.SectorPosition, CurrentUnit.SectorPosition) - 300f, ignoreMinFiringRange: true))
				{
					continue;
				}
				mineOrder.IsIdle = false;
				if (laserTurretComponent.TurretClass.AIConventionalWeapon)
				{
					currentUnitComponents.RegisterWeaponsFire();
				}
				laserTurretComponent.RemoveCharge();
				if (!mineTarget.Asteroid.RegisterDamage(CalculateMineTurretDamage(laserTurretComponent)))
				{
					continue;
				}
				CargoClass randomCargoYieldType = mineTarget.Asteroid.GetRandomCargoYieldType();
				if (!(randomCargoYieldType != null))
				{
					continue;
				}
				CargoBayComponent cargoBayComponent = fleet.Leader.CurrentUnit.CargoBayComponent;
				int num = mineTarget.Asteroid.GetRandomYieldQuantity();
				mineTarget.Asteroid.DecreaseYieldAndDestroyIfDepleted(num);
				if (fleet != null)
				{
					int num2 = UnityEngine.Random.Range(0, fleet.Ships.Count);
					int num3 = 0;
					while (num3 < fleet.Ships.Count && num > 0)
					{
						UnitComponentHolder unitComponentHolder = fleet.Ships[num2];
						if (unitComponentHolder != null && unitComponentHolder.CargoBayComponent != null)
						{
							int num4 = Mathf.Min(num, unitComponentHolder.CargoBayComponent.GetFreeSpaceFor(randomCargoYieldType));
							if (num4 > 0)
							{
								unitComponentHolder.CargoBayComponent.AddToCargo(randomCargoYieldType, num4, ignoreCapacity: true);
								num -= num4;
							}
						}
						num3++;
						num2++;
						if (num2 >= fleet.Ships.Count)
						{
							num2 = 0;
						}
					}
				}
				else
				{
					cargoBayComponent.AddToCargoIfFits(randomCargoYieldType, num);
				}
			}
		}

		private bool UseActiveMining()
		{
			Unit unit = CurrentUnit;
			if (unit.IsInActiveSector && unit.ActiveUnit != null && (unit.ActiveUnit.LastDistanceFromCamera == 0f || unit.ActiveUnit.LastDistanceFromCamera < 1000f))
			{
				return true;
			}
			return false;
		}

		private void CreateRegroupOrderWhenSeparated()
		{
			if (this.fleet != null && !IsFleetLeader && CheckIfSeparatedFromGroup())
			{
				Debug.LogWarning($"Separated from my fleet. Sector: {Sector} FleetSector: {this.fleet.Sector}. Joining...", this);
				Fleet fleet = UnityObjectHelper.NewGameObject<Fleet>();
				fleet.Sector = CurrentUnit.Sector;
				fleet.Init();
				fleet.Faction = this.fleet.Faction;
				JoinFleetOrder joinFleetOrder = UnityObjectHelper.NewGameObject<JoinFleetOrder>();
				joinFleetOrder.TargetFleet = this.fleet;
				joinFleetOrder.transform.SetParent(fleet.transform);
				joinFleetOrder.transform.localPosition = Vector3.zero;
				joinFleetOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
				Fleet = fleet;
				fleet.EnqueueOrder(joinFleetOrder);
			}
		}

		private void StandDownFromCombatWhenRequired()
		{
			if (fleet != null)
			{
				if (combatMode == AIControllerCombatMode.Offensive && fleet.CurState != FleetState.CombatInterception)
				{
					CombatMode = AIControllerCombatMode.Defensive;
				}
				if (combatTarget != null && !fleet.AllowAttack())
				{
					CombatTarget = null;
				}
			}
		}

		private void UpdateTurn()
		{
			Unit unit = CurrentUnit;
			if (unit.IsActiveInEngine)
			{
				ActiveUnitShip activeUnitShip = unit.ActiveUnit.ActiveUnitShip;
				ControlUnitTurn(activeUnitShip);
				if (activeUnitShip.DesiredTurn != newDesiredTurn)
				{
					activeUnitShip.DesiredTurn = GetShipNewDesiredTurn(activeUnitShip);
				}
			}
		}

		private float GetShipNewDesiredTurn(ActiveUnitShip activeUnitShip)
		{
			return Mathf.MoveTowards(activeUnitShip.DesiredTurn, newDesiredTurn, Engine.GameSettings.AIActivePilotTurnChangeRate * Time.deltaTime);
		}

		private void UpdateCombat()
		{
			CreateCombatNavigatorIfNull();
			if (combatTarget != null)
			{
				UpdateLastKnownCombatTargetPosition();
				if (!IsTargetValid(combatTarget) || combatTarget.Faction != combatTargetFaction)
				{
					OnInvalidCombatTarget();
				}
				else if (person.IsInActiveSector && IsCombatTargetOutOfDetectionRange())
				{
					OnInvalidCombatTarget();
				}
				if (CurrentUnit.IsFullyDecloaked)
				{
					ControlWeaponsInCombat();
				}
			}
			if (!hasStaticUnit)
			{
				UpdateCombatNavigation();
			}
		}

		private bool IsCombatTargetOutOfDetectionRange()
		{
			if (!currentUnit.Faction.Intel.IsUnitDiscoveredOrOwned(combatTarget, GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTimeWhenPlayerPilotting))
			{
				return true;
			}
			if (GameController.Instance.GameSettings.GameplaySettings.ClearNpcTargetWhenInvalidInstantly && !FactionIntel.IsStaticOrTreatAsStatic(combatTarget) && FactionIntelProcessor.IsTargetOutOfDetectionRange(currentUnit, combatTarget, lastCombatTargetDistance))
			{
				currentUnit.Faction.Intel.ChangeTimeOfDiscovery(combatTarget, 0f - GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTimeWhenPlayerPilotting);
				return true;
			}
			return false;
		}

		private void ControlWeaponsInCombat()
		{
			if (combatTarget != null && lastCombatTargetDistance < currentUnitComponents.HighestRangedTurret)
			{
				if (Engine.GameSettings.AIFiringUpdateEnabled)
				{
					if (CurrentUnitComponents.Turrets.Count == 0)
					{
						return;
					}
					if (!lowCapacitorCooldownActive)
					{
						if (currentUnit.IsInActiveSector)
						{
							UpdateCombatFiringWhenActive();
						}
						else
						{
							UpdateCombatFiringWhenInactive();
						}
						if (CurrentUnitComponents.Capacitor.ChargeNormalized < 0.15f)
						{
							LowCapacitorCooldownActive = true;
						}
					}
					else if (Time.time > lowCapacitorCooldownTime || CurrentUnit.Components.Capacitor.ChargeNormalized > lowCapacitorCooldownCharge)
					{
						LowCapacitorCooldownActive = false;
					}
				}
				if (CurrentUnit.IsInActiveSector && Time.time > nextMineSolutionCalc)
				{
					UpdateMineTurretFiring();
					nextMineSolutionCalc = Time.time + Engine.PerformanceSettings.AIActivePilotMineCalcInterval;
				}
			}
			if (CurrentUnit.IsInActiveSector && currentUnitComponents.CountermeasureTurret != null && HasComponentUseCooldownTimeExpired)
			{
				NpcPilotBehaviourCountermeasureSettings countermeasureSettings = Engine.GameSettings.AiUnitControllerSettings.CombatSettings.CountermeasureSettings;
				if (countermeasureSettings.UpdateEnabled)
				{
					UpdateCountermeasureDeployment(countermeasureSettings);
				}
			}
		}

		private void OnInvalidCombatTarget()
		{
			_ = combatTarget;
			CombatTarget = null;
		}

		private void OnLostContactWithTargetDueToCloak(Unit oldTarget)
		{
			if (Person != null && CurrentUnit.IsInActiveSector && oldTarget != null && !oldTarget.IsDestroyed && currentUnit.IsHostileTo(oldTarget))
			{
				Person.RaiseDialogEventRandomly(Engine.DialogEvents.LostTargetCloaked);
			}
			if (fleet != null)
			{
				fleet.ExtendCombatInterceptionTime(Engine.GameSettings.AILostCloakedTargetCoolDownTime);
			}
		}

		private void OnLostContactWithTarget(Unit oldTarget)
		{
			if (Person != null && CurrentUnit.IsInActiveSector && oldTarget != null && !oldTarget.IsDestroyed && currentUnit.IsHostileTo(oldTarget))
			{
				Person.RaiseDialogEventRandomly(Engine.DialogEvents.LostTarget);
			}
			if (fleet != null)
			{
				fleet.ExtendCombatInterceptionTime(Engine.GameSettings.AILostCombatCombatCoolDownTime);
			}
		}

		private void UpdateCombatNavigation()
		{
			CombatNavigator.Tick();
		}

		public void CreateCombatNavigatorIfNull()
		{
			if (combatNavigator == null)
			{
				combatNavigator = gameObject.AddComponent<AICombatNavigator>();
				combatNavigator.NpcPilot = this;
				combatNavigator.Init();
			}
		}

		private void UpdateAvoidTarget()
		{
			Transform transform = CurrentUnit.transform;
			if (avoidType == AIAvoidType.None)
			{
				return;
			}
			if (avoidTarget == null && avoidType != AIAvoidType.OwnMine)
			{
				ClearAvoidTarget();
				return;
			}
			Vector3 zero = Vector3.zero;
			zero = ((avoidType == AIAvoidType.OwnMine) ? transform.forward : (transform.position - avoidTarget.transform.position));
			if (Time.time > avoidTimeout || zero.magnitude > avoidDistance)
			{
				ClearAvoidTarget();
				return;
			}
			Vector3 vector = Vector3.Normalize(zero);
			if (avoidTarget != null && avoidType == AIAvoidType.Leisurely && targetNavpointType != NpcPilotNavpointType.None)
			{
				float bearingThreshold = 90f;
				if (!TargetBearingRequiresAvoid(avoidTarget, bearingThreshold))
				{
					ClearAvoidTarget();
				}
			}
			if (avoidType != AIAvoidType.None)
			{
				targetNavpointType = NpcPilotNavpointType.Avoid;
				Vector3 sectorPosition = SectorPosition + vector * 1000f;
				sectorPosition.y = 0f;
				targetNavpoint.SectorPosition = sectorPosition;
				targetNavpoint.Arrive = false;
				targetNavpoint.ThrottleDotThreshold = 0.1f;
			}
		}

		private bool TargetBearingRequiresAvoid(GameObject target, float bearingThreshold = 35f)
		{
			return Mathf.Abs(CurrentUnit.GetLocalYBearingToWorldPosition(target.transform.position)) < bearingThreshold;
		}

		private bool TrySetNavpoint()
		{
			if (!CurrentUnit.IsDocked && HasCombatTargetOrGroupInCombat && combatMode == AIControllerCombatMode.Offensive)
			{
				targetNavpointType = NpcPilotNavpointType.Combat;
				CreateCombatNavigatorIfNull();
				CombatNavigator.SetNavPointFromCombat(ref targetNavpoint);
				if (Time.time > lastCombatNavpointDistanceCheck + 30f)
				{
					if (targetNavpoint.SectorPosition.magnitude > GameController.Instance.GameSettings.UniverseBoundsSettings.MaxUnitDistanceFromOriginLowerBound)
					{
						targetNavpoint.SectorPosition = Vector3.Normalize(targetNavpoint.SectorPosition) * GameController.Instance.GameSettings.UniverseBoundsSettings.MaxUnitDistanceFromOriginLowerBound * 0.9f;
					}
					lastCombatNavpointDistanceCheck = Time.time;
				}
				return true;
			}
			if (fleet != null && UpdateNavpointFromFleet())
			{
				targetNavpointType = NpcPilotNavpointType.Fleet;
				if (!TryUndockForFleetNavpointWhenPossible())
				{
					return false;
				}
				return true;
			}
			return false;
		}

		public bool UpdateNavpointFromFleet()
		{
			switch (fleet.CurState)
			{
			case FleetState.Idle:
				if (CurrentUnit.IsDocked)
				{
					switch (fleet.GetPreferToDockWhenIdle())
					{
					case DockedPreference.Undock:
						if (DockCooldownElapsed())
						{
							CurrentUnit.Components.UndockIfDocked();
							if (this == fleet.Leader)
							{
								fleet.SetPositionToLeaderShipPosition();
							}
							return true;
						}
						return false;
					case DockedPreference.Dock:
						return false;
					case DockedPreference.DontCare:
						if (this != fleet.Leader && fleet.LeaderUnit.GetDockUnit() != CurrentUnit.GetDockUnit())
						{
							return SetNavpointToFormationPositionWhenIdle();
						}
						return false;
					}
				}
				return SetNavpointToFormationPositionWhenIdle();
			case FleetState.Regrouping:
				if (fleet.Leader != null && fleet.Leader.CurrentUnit.IsDocked && CurrentUnit.AtTheSameRootUnitAs(fleet.Leader.CurrentUnit))
				{
					HandleGroupPilotNavigated();
					return false;
				}
				return SetNavpointToFormationPosition();
			case FleetState.MoveToTarget:
			{
				AIObjectiveTarget navTarget = fleet.NavTarget;
				if (!navTarget.IsActive)
				{
					break;
				}
				if (navTarget.TargetType == WorldNavpointTargetType.Dock)
				{
					Unit dockUnit = currentUnitComponents.DockUnit;
					if (dockUnit != null && navTarget.TargetSectorObject is Unit otherUnit && dockUnit.HasSameRootUnitAs(otherUnit))
					{
						HandleGroupPilotNavigated();
						return false;
					}
				}
				if (navTarget.waypoints.Count > 0)
				{
					WorldNavpoint worldNavpoint = navTarget.waypoints[0];
					switch (worldNavpoint.TargetType)
					{
					case WorldNavpointTargetType.Dock:
						if (!(worldNavpoint.TargetSectorObject != null))
						{
							break;
						}
						if (Sector == fleet.Sector)
						{
							SetNavpointToArriveAtSectorPosition(GetFormationSectorPosition());
							if (!worldNavpoint.TargetSectorObject.IsStatic)
							{
								float currentMaxSpeed = currentUnitComponents.CurrentMaxSpeed;
								if (currentMaxSpeed > 0f && worldNavpoint.TargetSectorObject.GetSpeed() / currentMaxSpeed > 0.5f)
								{
									targetNavpoint.ArrivalThreshold = 125f;
									targetNavpoint.Arrive = false;
								}
							}
							return true;
						}
						return false;
					case WorldNavpointTargetType.Gate:
						if (SetNavpointToFormationPosition())
						{
							targetNavpoint.ArrivalThreshold = Mathf.Max(targetNavpoint.ArrivalThreshold, 100f);
							return true;
						}
						break;
					case WorldNavpointTargetType.None:
						if (SetNavpointToFormationPosition())
						{
							if (navTarget.waypoints.Count > 1)
							{
								targetNavpoint.ArrivalThreshold = Mathf.Max(targetNavpoint.ArrivalThreshold, 100f);
							}
							return true;
						}
						break;
					}
					break;
				}
				return SetNavpointToFormationPosition();
			}
			case FleetState.EnteringGate:
				if (CurrentUnit.Sector != fleet.WormholeBeingEntered.ActualTargetSector && fleet.RequestGateEntry(this))
				{
					SetNavpointToArriveAtSectorPosition(fleet.WormholeBeingEntered.Unit.SectorPosition);
					targetNavpoint.ArrivalThreshold = 80f;
					return true;
				}
				break;
			}
			return false;
		}

		private bool TryUndockForFleetNavpointWhenPossible()
		{
			if (!CurrentUnit.Components.IsDocked)
			{
				return true;
			}
			if (DockCooldownElapsed())
			{
				CurrentUnit.Components.UndockIfDocked();
				if (this == fleet.Leader)
				{
					fleet.SetPositionToLeaderShipPosition();
				}
				return true;
			}
			return false;
		}

		public bool DockCooldownElapsed()
		{
			return Time.time > dockCoolDownTime;
		}

		private void UpdateCombatTargetting(float elapsedTime)
		{
			if (fleet == null || fleet.AllowAttack())
			{
				if (combatTarget != null)
				{
					UpdateCombatTargetDist();
				}
				if (targetter.IsSearching)
				{
					targetter.ProcessSearch(elapsedTime);
					if (!targetter.IsSearching)
					{
						OnTargetterFinishedCombatTargetSearch(targetter.BestTarget);
					}
				}
				else if (Time.time > nextCombatTargetSearch)
				{
					targetter.StartNewSearch();
				}
			}
			else
			{
				CombatTarget = null;
				if (currentUnitComponents.AutoTurretModule != null)
				{
					currentUnitComponents.AutoTurretModule.FireMode = AutoTurretFireMode.Disabled;
				}
			}
		}

		private void OnTargetterFinishedCombatTargetSearch(Unit bestTarget)
		{
			nearestHostileTargetDistance = targetter.NearestHostileTargetDistance;
			float num = 0f;
			bool flag = false;
			if (combatTarget != null && !combatTarget.IsDestroyed && combatTarget.IsShip() && combatTarget.Sector == Sector)
			{
				flag = combatTarget.CloakState == CloakState.Cloaking || combatTarget.CloakState == CloakState.Cloaked;
			}
			Unit unit = combatTarget;
			if (bestTarget != null && bestTarget.IsValidAndNotDestroyed)
			{
				float aggression = Person.Aggression;
				if (fleet != null)
				{
					aggression = fleet.Settings.Aggression;
				}
				float num2 = Fleet.GetAttackTargetScoreThresholdFromAggression(aggression);
				if (Person.IsAutoPilot && fleet == null)
				{
					num2 = 0f;
				}
				if (fleet != null || targetter.BestTargetScore >= num2)
				{
					combatTargetIsPriority = false;
					CombatTarget = bestTarget;
					combatTargetIsPriority = targetter.BestTargetIsPriority;
					UnitAutoTurretModule autoTurretModule = CurrentUnitComponents.AutoTurretModule;
					if (autoTurretModule != null)
					{
						autoTurretModule.PreferredTurretTarget = combatTarget;
					}
					AIControllerCombatMode oldCombatMode = combatMode;
					AIControllerCombatMode aIControllerCombatMode = AIControllerCombatMode.Defensive;
					if (!hasStaticUnit)
					{
						if (fleet != null)
						{
							if (fleet.RequestInterception(this, combatTarget, targetter.BestTargetDistance, targetter.BestTargetScore, targetter.BestTargetIsPriority))
							{
								aIControllerCombatMode = OnGrantedIntercept(unit, oldCombatMode);
							}
						}
						else if (targetter.BestTargetScore > Fleet.GetInterceptScoreThresholdFromAggression(aggression))
						{
							aIControllerCombatMode = AIControllerCombatMode.Offensive;
						}
					}
					if (combatTarget != null && combatTarget != unit)
					{
						num += 10f;
						if ((unit != null) & flag)
						{
							OnLostContactWithTargetDueToCloak(unit);
						}
					}
					CombatMode = aIControllerCombatMode;
					if (combatTarget != unit && combatTarget.IsPlayerCurrentUnit)
					{
						EngineASX.Instance.NotifyPlayerTargettedByHostileNpc(CurrentUnit);
					}
					if (combatTarget != unit && combatTarget != null && (combatTarget.UnitType == UnitType.Ship || combatTarget.UnitType == UnitType.Station) && Person != null && CurrentUnit.IsInActiveSector)
					{
						if (combatMode == AIControllerCombatMode.Offensive)
						{
							string text = null;
							if (combatTarget.Faction != null)
							{
								text = ((!combatTarget.Faction.IsFreelancer) ? combatTarget.Faction.GetShortNameElseLong() : "freelancer");
							}
							else
							{
								text = "abandoned";
							}
							if (combatTarget.IsShip())
							{
								string item = combatTarget.GetClassAndSeriesName() + " " + combatTarget.GetShortDesignation();
								DialogRequestArguments value = DialogRequestArguments.Init();
								value.KeyValues.Add(("TargetUnit", item));
								value.KeyValues.Add(("TargetFaction", text));
								Person.RaiseDialogEventRandomly(Engine.DialogEvents.InterceptingShip, 0f, value);
							}
							else
							{
								DialogRequestArguments value2 = DialogRequestArguments.Init();
								value2.KeyValues.Add(("TargetStationOrTurret", combatTarget.IsTurret() ? "turret" : "station"));
								value2.KeyValues.Add(("TargetFaction", text));
								Person.RaiseDialogEventRandomly(Engine.DialogEvents.InterceptingStation, 0f, value2);
							}
						}
						else
						{
							Person.RaiseDialogEventRandomly(Engine.DialogEvents.GotTarget);
						}
					}
					if (combatMode == AIControllerCombatMode.Offensive && LogWrapper.LogMsgs)
					{
						LogWrapper.Log($"{this}: Group has given me permission to intercept target - {combatTarget}", this, 2);
					}
				}
				else
				{
					OnCouldntFindNewCombatTarget(flag, unit);
				}
			}
			else
			{
				OnCouldntFindNewCombatTarget(flag, unit);
			}
			if (combatTarget != null)
			{
				GetNearestNonHostileDist(out nearestNonHostileTargetDistance);
			}
			nextCombatTargetSearch = Time.time + num + UnityEngine.Random.Range(Engine.GameSettings.AISelectBestCombatTargetMinFrequency, Engine.GameSettings.AISelectBestCombatTargetMaxFrequency);
			if (currentUnitComponents.IsDocked)
			{
				nextCombatTargetSearch += 2f;
			}
			AutomateTurrets(ShouldTurretsBeAutomatedForCombatTarget());
		}

		private bool ShouldTurretsBeAutomatedForCombatTarget()
		{
			if (combatTarget == null)
			{
				return false;
			}
			if (combatMode == AIControllerCombatMode.Defensive)
			{
				return true;
			}
			if (combatTarget != null && lastCombatTargetDistance >= 800f && (combatNavigator == null || combatNavigator.NavigationMode != AICombatNavigationMode.AttackRun))
			{
				return true;
			}
			return false;
		}

		private void OnCouldntFindNewCombatTarget(bool wasTargetCloakedOrCloaking, Unit oldCombatTarget)
		{
			CombatTarget = null;
			if (oldCombatTarget != null)
			{
				if (wasTargetCloakedOrCloaking)
				{
					OnLostContactWithTargetDueToCloak(oldCombatTarget);
				}
				else
				{
					OnLostContactWithTarget(oldCombatTarget);
				}
			}
		}

		private AIControllerCombatMode OnGrantedIntercept(Unit oldCombatTarget, AIControllerCombatMode oldCombatMode)
		{
			AIControllerCombatMode aIControllerCombatMode = AIControllerCombatMode.Offensive;
			if (combatTarget != oldCombatTarget || aIControllerCombatMode != oldCombatMode)
			{
				_ = combatTarget.IsPlayerCurrentUnit;
			}
			return aIControllerCombatMode;
		}

		private void UpdateCombatTargetDist()
		{
			if (CurrentUnit.Sector != combatTarget.Sector)
			{
				lastCombatTargetDistance = 100000f;
			}
			else
			{
				lastCombatTargetDistance = GetDistance2D(CurrentUnit.SectorPosition, combatTarget.SectorPosition);
			}
		}

		private void UpdateCloaking()
		{
			CloakComponent cloakComponent = CurrentUnit.Components.CloakComponent;
			if (!(cloakComponent != null) || (fleet != null && fleet.ActiveOrder == null && Time.time - fleet.LastTimeActiveOrderChanged < 4f))
			{
				return;
			}
			if (GetPreferredCloak())
			{
				if (CurrentUnit.IsFullyDecloaked && cloakComponent.IsReadyToFireIgnoringTarget())
				{
					cloakComponent.StartCloak();
				}
			}
			else if (cloakComponent.CanCancelFire())
			{
				cloakComponent.StartDecloak();
			}
		}

		public float GetTargetInterceptionLowerDistance()
		{
			if (fleet != null)
			{
				return fleet.Settings.TargetInterceptionLowerDistance;
			}
			return EngineASX.Instance.GenericFleetPrefab.Settings.TargetInterceptionLowerDistance;
		}

		private bool GetPreferredCloak()
		{
			if (AllowUseCloak)
			{
				switch (combatMode)
				{
				case AIControllerCombatMode.Offensive:
					if (HasCombatTargetOrGroupInCombat && ((fleet != null && fleet.IsUnderAttack) || CombatTarget == null || lastCombatTargetDistance < GetTargetInterceptionLowerDistance()))
					{
						return false;
					}
					break;
				case AIControllerCombatMode.Defensive:
					if (currentUnitComponents.IsFullyDecloaked && nearestHostileTargetDistance < 350f)
					{
						return false;
					}
					break;
				}
				if (fleet != null)
				{
					return fleet.GetPrefersCloak();
				}
			}
			return false;
		}

		private void UpdateCombatFiringWhenActive()
		{
			if (!HasComponentUseCooldownTimeExpired)
			{
				return;
			}
			combatFiringCurrentCheckIndex++;
			if (combatFiringCurrentCheckIndex >= currentUnitComponents.Turrets.Count)
			{
				combatFiringCurrentCheckIndex = 0;
			}
			TurretComponent turretComponent = CurrentUnitComponents.Turrets[combatFiringCurrentCheckIndex];
			if (!(Time.time > nextCheckTurretTime))
			{
				return;
			}
			if ((!turretComponent.TurretClass.RestrictAIUsage || Time.time > turretComponent.AiNextRestrictedWeaponsFireTime) && turretComponent.TurretClass.AIConventionalWeapon && !turretComponent.AutoFireActive && turretComponent.IsReadyToFire(combatTarget))
			{
				turretComponent.Fire(combatTarget);
				UpdateNextComponentUseCooldownTimeForWeaponsFire();
				if (turretComponent.TurretClass.RestrictAIUsage)
				{
					SetTurretNextFireTime(turretComponent);
				}
			}
			nextCheckTurretTime = Time.time + Mathf.Lerp(0.15f, 0f, pilotSettings.CombatEfficiency);
		}

		private void UpdateCombatFiringWhenInactive()
		{
			foreach (TurretComponent turret in currentUnitComponents.Turrets)
			{
				if ((!turret.TurretClass.RestrictAIUsage || Time.time > turret.AiNextRestrictedWeaponsFireTime) && turret.TurretClass.AIConventionalWeapon && !turret.AutoFireActive && turret.IsReadyToFire(combatTarget, ignoreFiringArc: true))
				{
					turret.Fire(combatTarget);
					if (turret.TurretClass.RestrictAIUsage)
					{
						SetTurretNextFireTime(turret);
					}
				}
			}
		}

		private void UpdateCountermeasureDeployment(NpcPilotBehaviourCountermeasureSettings countermeasureSettings)
		{
			if (!(Time.time > countermeasureDeployNextCheckTime))
			{
				return;
			}
			countermeasureDeployNextCheckTime = Time.time + countermeasureSettings.UpdateInterval;
			float num = Mathf.Lerp(countermeasureSettings.MinDeploymentProbability, countermeasureSettings.MaxDeploymentProbability, pilotSettings.CombatEfficiency);
			if (UnityEngine.Random.value < num)
			{
				float nearestDist = 0f;
				Missile nearestMissileLock = CurrentUnit.GetNearestMissileLock(out nearestDist);
				if (nearestMissileLock != null)
				{
					DeployCountermeasuresForMissile(countermeasureSettings, nearestDist, nearestMissileLock);
				}
			}
		}

		private bool MissileFiredWithinReactionTime(Missile missile, NpcPilotBehaviourCountermeasureSettings settings)
		{
			float num = Mathf.Lerp(settings.MaxMissileLifetimeUpper, settings.MinMissileLifetimeLower, pilotSettings.CombatEfficiency);
			return missile.Projectile.LifeTime > (double)num;
		}

		private float GetCountermeasureMaxDeploymentRange(NpcPilotBehaviourCountermeasureSettings settings)
		{
			return Mathf.Lerp(settings.MaxDeploymentRangeUpper, settings.MaxDeploymentRangeLower, pilotSettings.CombatEfficiency);
		}

		private void DeployCountermeasuresForMissile(NpcPilotBehaviourCountermeasureSettings countermeasureSettings, float missileDistance, Missile missile)
		{
			if (MissileFiredWithinReactionTime(missile, countermeasureSettings))
			{
				float countermeasureMaxDeploymentRange = GetCountermeasureMaxDeploymentRange(countermeasureSettings);
				if (missileDistance < countermeasureMaxDeploymentRange && currentUnitComponents.CountermeasureTurret.IsReadyToFire(null) && countermeasureSettings.DeploymentEnabled)
				{
					currentUnitComponents.CountermeasureTurret.Fire(null);
					countermeasureDeployNextCheckTime = Time.time + UnityEngine.Random.Range(countermeasureSettings.MinDeployCooldownTime, countermeasureSettings.MaxDeployCooldownTime);
					UpdateNextComponentUseCooldownTimeForWeaponsFire();
				}
			}
		}

		private void UpdateMineTurretFiring()
		{
			if (!HasComponentUseCooldownTimeExpired || !(combatTarget != null) || mineDropperTurrets.Count <= 0)
			{
				return;
			}
			Unit unit = CurrentUnit;
			Rigidbody unitRigidBody = UnitRigidBody;
			if (!(unitRigidBody != null))
			{
				return;
			}
			float magnitude = unitRigidBody.linearVelocity.magnitude;
			Vector3 vector = Vector3.zero;
			if (combatTargetRigidBody != null)
			{
				vector = combatTargetRigidBody.linearVelocity;
			}
			NpcPilotBehaviourMineDropSettings npcPilotBehaviourMineDropSettings = GameController.Instance.GameSettings.NpcPilotBehaviourMineDropSettings;
			if (!(magnitude > EngineASX.Instance.GameSettings.AIMinSpeedToDeployMine))
			{
				return;
			}
			foreach (ProjectileTurretComponent mineDropperTurret in mineDropperTurrets)
			{
				if (!mineDropperTurret.IsReadyToFire(null))
				{
					continue;
				}
				ExplosionClass explosionClass = mineDropperTurret.CurProjectileClass.ExplosionClass;
				if (!(explosionClass != null) || (npcPilotBehaviourMineDropSettings.AIActivePilotMineCheckFriendlies && !(nearestNonHostileTargetDistance > explosionClass.MaxDistance)))
				{
					continue;
				}
				float maxMoveRange = mineDropperTurret.CurProjectileClass.MaxMoveRange;
				float num = maxMoveRange / mineDropperTurret.CurProjectileClass.MoveSpeed;
				float currentMaxSpeed = CurrentUnit.Components.CurrentMaxSpeed;
				Vector3 ourPosition = unit.transform.position + unit.transform.forward * currentMaxSpeed * num;
				Vector3 targetPosition = mineDropperTurret.transform.position + mineDropperTurret.transform.forward * maxMoveRange;
				float distance2D = GetDistance2D(ref ourPosition, ref targetPosition);
				float num2 = Mathf.Lerp(npcPilotBehaviourMineDropSettings.AvoidDamageDistanceMultiplierLower, npcPilotBehaviourMineDropSettings.AvoidDamageDistanceMultiplierUpper, pilotSettings.CombatEfficiency);
				if (distance2D > explosionClass.MaxDistance * num2)
				{
					Vector3 targetPosition2 = combatTarget.transform.position + vector * num;
					float distance2D2 = GetDistance2D(ref targetPosition, ref targetPosition2);
					float num3 = Mathf.Lerp(npcPilotBehaviourMineDropSettings.TargetDistanceMultiplierUpper, npcPilotBehaviourMineDropSettings.TargetDistanceMultiplierLower, pilotSettings.CombatEfficiency);
					if (distance2D2 < explosionClass.MaxDistance * num3)
					{
						mineDropperTurret.Fire(combatTarget);
						Sector.RegisterMineDeployment();
						StartAvoidTarget(null, explosionClass.MaxDistance, 5f, AIAvoidType.OwnMine);
						UpdateNextComponentUseCooldownTimeForWeaponsFire();
						break;
					}
				}
			}
		}

		private void SetTurretNextFireTime(TurretComponent t)
		{
			t.AiNextRestrictedWeaponsFireTime = Time.time + GetRestrictedWeaponFireDelayTime(t);
		}

		private float GetRestrictedWeaponFireDelayTime(TurretComponent t)
		{
			float f = pilotSettings.CombatEfficiency * 0.8f + pilotSettings.RestrictedWeaponPreference * 0.2f;
			return Mathf.Lerp(t.TurretClass.AIMaxTimeBeforeFire, t.TurretClass.AIMinTimeBeforeFire, Mathf.Pow(f, t.TurretClass.AiFireProbabilityPower));
		}

		private void ResetNavpointArrivalDefaults()
		{
			ResetNavpointArrivalDefaults(ref targetNavpoint);
		}

		private void ResetNavpointArrivalDefaults(ref NpcPilotNavpoint navpoint)
		{
			navpoint.ThrottleDotThreshold = GameController.Instance.GameSettings.NpcPiilotArrivalSettings.AIArrive_DefaultThrottleDotThreshold;
			navpoint.ArrivalThreshold = Mathf.Max(GameController.Instance.GameSettings.NpcPiilotArrivalSettings.AIArrive_MinArrivalDistance, CurrentUnit.UnitClass.ShieldRingRadius * GameController.Instance.GameSettings.NpcPiilotArrivalSettings.AIArrive_ArrivalDistanceRadiusMultiplier);
		}

		private void ResetNavpointSlowArrivalDefaults()
		{
			targetNavpoint.ThrottleDotThreshold = GameController.Instance.GameSettings.NpcPiilotArrivalSettings.AIArrive_SlowThrottleDotThreshold;
			targetNavpoint.ArrivalThreshold = Mathf.Max(GameController.Instance.GameSettings.NpcPiilotArrivalSettings.AIArrive_MinArrivalDistance * 0.5f, CurrentUnit.UnitClass.ShieldRingRadius * GameController.Instance.GameSettings.NpcPiilotArrivalSettings.AIArrive_ArrivalDistanceRadiusMultiplier);
		}

		private void SetFleet(Fleet value)
		{
			Fleet fleet = this.fleet;
			this.fleet = value;
			if (fleet != null)
			{
				fleet.RemoveNpc(this);
			}
			if (this.fleet != null)
			{
				if (CurrentUnit == null)
				{
					FindUnit();
				}
				this.fleet.AddNpc(this);
				if (CurrentUnit != null)
				{
					CurrentUnit.Faction = this.fleet.Faction;
				}
			}
		}

		private bool requireTurn()
		{
			return Mathf.Abs(desiredBearing) > 0.1f;
		}

		private void ControlUnitTurn(ActiveUnitShip activeShip)
		{
			if (!angularArrive)
			{
				UpdateDesiredBearing();
				if (Mathf.Abs(desiredBearing + activeShip.CurrentTurn * Time.deltaTime) < 0.1f)
				{
					AngularArrive();
					return;
				}
				UnitClass unitClass = CurrentUnitComponents.UnitClass;
				newDesiredTurn = ApplyDesiredBearing(desiredBearing, activeShip.CurrentTurn, unitClass.TurnAcceleration, unitClass.turnRate, Engine.GameSettings.ActiveUnitTurnArrivalAggression);
			}
		}

		private void UpdateDesiredBearing()
		{
			desiredBearing = Maths.WrapValue(desiredAngleY - CurrentUnit.transform.localEulerAngles.y, -180f, 180f);
		}

		private void AngularArrive()
		{
			newDesiredTurn = 0f;
			Unit unit = CurrentUnit;
			if (unit != null)
			{
				ActiveUnit activeUnit = unit.ActiveUnit;
				if (activeUnit != null)
				{
					ActiveUnitShip activeUnitShip = activeUnit.ActiveUnitShip;
					if (activeUnitShip != null)
					{
						activeUnitShip.RemoveForces();
					}
				}
			}
			angularArrive = true;
			CurrentUnit.ClearZAndXRotation();
		}

		private void MoveToNavpointInactive(float elapsedTime)
		{
			if (elapsedTime == 0f)
			{
				return;
			}
			UnitEngineComponent engineComponent = currentUnit.Components.EngineComponent;
			if (engineComponent == null)
			{
				return;
			}
			Vector3 vector = targetNavpoint.SectorPosition - SectorPosition;
			lastNavPointDistance = vector.magnitude;
			if (targetNavpointType == NpcPilotNavpointType.Fleet)
			{
				fleet.NotifyNpcPilotDistanceFromNavpoint(this, lastNavPointDistance);
			}
			float num = 0f;
			if (lastNavPointDistance > 0f)
			{
				float preferredThrottle = getPreferredThrottle();
				engineComponent.EngineThrottle = preferredThrottle;
				if (lastNavPointDistance < 2000f)
				{
					float currentMaxSpeed = currentUnitComponents.CurrentMaxSpeed;
					if (currentMaxSpeed > 0f)
					{
						float num2 = lastNavPointDistance / currentMaxSpeed;
						if (num2 < elapsedTime)
						{
							elapsedTime = num2;
						}
					}
				}
				num = UnitEngineClass.CalculateMaxSpeed(engineComponent.CalculateRelativeForceAndReduceCapacitor(reduceChargeEnergy: true, elapsedTime), currentUnit.UnitClass.Drag, currentUnit.Mass);
				vector.y = 0f;
			}
			if (lastNavPointDistance - num > targetNavpoint.ArrivalThreshold)
			{
				Vector3 vector2 = vector.normalized * num;
				CurrentUnit.transform.localPosition += vector2;
				currentUnitComponents.lastKnownVelocity = vector2 / elapsedTime;
				if (vector != Vector3.zero && vector.magnitude > currentUnit.UnitClass.ShieldRingRadius)
				{
					currentUnit.transform.localRotation = Quaternion.LookRotation(vector, Vector3.up);
				}
			}
			else
			{
				currentUnitComponents.lastKnownVelocity = Vector3.zero;
				CurrentUnit.transform.localPosition = targetNavpoint.SectorPosition;
				ArrivedAtNavpoint(targetNavpointType);
				if (fleet != null && (fleet.PilotCount > 1 || fleet.ActiveOrder != null))
				{
					currentUnit.transform.localRotation = fleet.transform.localRotation;
				}
				else if (vector != Vector3.zero && vector.magnitude > currentUnit.UnitClass.ShieldRingRadius)
				{
					currentUnit.transform.localRotation = Quaternion.LookRotation(vector, Vector3.up);
				}
			}
			CurrentUnit.UpdateGasCloud();
		}

		private void MoveToNavpointInActiveSector(ref float newDesiredThrottle)
		{
			bool flag = false;
			NpcPilotNavpoint navpoint = targetNavpoint;
			Vector3 navPointVector = targetNavpoint.SectorPosition - SectorPosition;
			lastNavPointDistance = navPointVector.magnitude;
			if (NavpointType == NpcPilotNavpointType.Fleet)
			{
				fleet.NotifyNpcPilotDistanceFromNavpoint(this, lastNavPointDistance);
			}
			bool flag2 = false;
			if (CanPathfind(lastNavPointDistance))
			{
				Unit unit = null;
				Vector3 forward = CurrentUnit.transform.forward;
				Vector3 currentVelocity = CurrentUnit.Velocity;
				Vector3 startRayPosition = CurrentUnit.transform.position;
				if (NavpointType == NpcPilotNavpointType.Fleet)
				{
					unit = fleet.CurrentNavpointTargetUnit;
					if (unit != null && unit.Sector == Sector && fleet.NavTarget.waypoints[0].TargetType == WorldNavpointTargetType.Dock && Vector3.Distance(CurrentUnit.SectorPosition, unit.SectorPosition) < CurrentUnit.UnitClass.ShieldRingRadius + unit.UnitClass.ShieldRingRadius + 5f)
					{
						newDesiredThrottle = 0f;
						ArrivedAtNavpoint(targetNavpointType);
						return;
					}
				}
				float width = currentUnitComponents.Width;
				if (Physics.SphereCast(new Ray(startRayPosition, forward), width / 2f, out var hitInfo, Mathf.Min(lastNavPointDistance, 80f), GameController.Instance.StaticNonOverlappingMask, QueryTriggerInteraction.Ignore))
				{
					Unit component = hitInfo.collider.GetComponent<Unit>();
					if (component.IsStatic && component != CurrentUnit && component != unit)
					{
						flag2 = true;
					}
				}
				if (RequiresNewPathfind())
				{
					if (pathfindResult == null)
					{
						pathfindResult = new List<Vector3>(8);
					}
					pathfindResult.Clear();
					float collisionDistance = 0f;
					if (flag2 || PathToNavpointIsBlocked(unit, ref startRayPosition, ref navPointVector, ref currentVelocity, width, out collisionDistance))
					{
						Vector3 startSectorPosition = CurrentUnit.SectorPosition + forward * currentUnit.Radius;
						if (collisionDistance > 0f && currentVelocity != Vector3.zero)
						{
							Vector3 vector = currentVelocity * 2f;
							if (vector.magnitude > collisionDistance)
							{
								vector = vector.normalized * collisionDistance / 2f;
							}
							startSectorPosition += vector;
						}
						EngineASX.Instance.NpcPathfindingController.NpcPathfinder.GetPathSectorPositionsNonAlloc(startSectorPosition, navpoint.SectorPosition, forward, CurrentUnit, fleet, pathfindResult);
						lastPathfindTime = Time.time;
						if (pathfindResult.Count == 0)
						{
							newDesiredThrottle = 0f;
							ArrivedAtNavpoint(targetNavpointType);
							return;
						}
					}
					else
					{
						ClearPathfindingData();
					}
				}
				if (pathfindResult != null && pathfindResult.Count > 0)
				{
					ResetNavpointArrivalDefaults(ref navpoint);
					flag = true;
					navpoint.ArrivalThreshold *= 1.2f;
					navpoint.SectorPosition = pathfindResult[0];
					navpoint.Arrive = true;
				}
			}
			navPointVector = navpoint.SectorPosition - SectorPosition;
			lastNavPointDistance = navPointVector.magnitude;
			if (lastNavPointDistance > navpoint.ArrivalThreshold)
			{
				SetDesiredBearingFromTargetSectorPosition(navpoint.SectorPosition);
				if (!flag2 | flag)
				{
					SetDesiredThrottle(navpoint, navPointVector, lastNavPointDistance, ref newDesiredThrottle);
				}
			}
			else if (flag)
			{
				if (pathfindResult.Count > 0)
				{
					pathfindResult.RemoveAt(0);
				}
				if (pathfindResult.Count == 0)
				{
					newDesiredThrottle = 0f;
					ArrivedAtNavpoint(targetNavpointType);
				}
			}
			else
			{
				newDesiredThrottle = 0f;
				ArrivedAtNavpoint(targetNavpointType);
			}
			isNavpointFromPathfind = flag;
		}

		private bool PathToNavpointIsBlocked(Unit fleetNavpointUnit, ref Vector3 startRayPosition, ref Vector3 navPointVector, ref Vector3 currentVelocity, float width, out float collisionDistance)
		{
			collisionDistance = 0f;
			Vector3 normalized = navPointVector.normalized;
			if (Physics.SphereCast(new Ray(startRayPosition, navPointVector.normalized), width / 2f, out var hitInfo, lastNavPointDistance, GameController.Instance.NonOVerlappingUnitsMask, QueryTriggerInteraction.Ignore))
			{
				collisionDistance = hitInfo.distance;
				float magnitude = currentVelocity.magnitude;
				Unit component = hitInfo.collider.GetComponent<Unit>();
				if (component == fleetNavpointUnit)
				{
					return false;
				}
				if (combatMode == AIControllerCombatMode.Offensive && component == combatTarget)
				{
					return false;
				}
				if (component.IsStatic)
				{
					return true;
				}
				if (fleet == null || component.GetFleet() != fleet || fleet.MoveSpeed < 5f)
				{
					if (component.Velocity != Vector3.zero)
					{
						float num = Vector3.Dot(normalized, component.Velocity.normalized);
						if (Mathf.Abs(num) < 0.8f)
						{
							return false;
						}
						if (num > 0.8f && collisionDistance > magnitude * 2f)
						{
							return false;
						}
					}
					return true;
				}
			}
			return false;
		}

		private bool CanPathfind(float targetDistance)
		{
			if (!GameController.Instance.GameSettings.DebugSettings.NpcPathfindingEnabled)
			{
				return false;
			}
			if (currentUnitComponents.Unit.CollisionCollider == null || currentUnitComponents.Unit.CollisionCollider.isTrigger)
			{
				return false;
			}
			if (targetDistance < GameController.Instance.GameSettings.AiUnitControllerSettings.RequirePathfindThresholdDistance)
			{
				return false;
			}
			return true;
		}

		private bool RequiresNewPathfind()
		{
			float num = Time.time - lastPathfindTime;
			if (num < GameController.Instance.GameSettings.AiUnitControllerSettings.PathfindFrequency)
			{
				return false;
			}
			if (num > 8f)
			{
				return true;
			}
			if (isNavpointFromPathfind && pathfindResult != null)
			{
				return pathfindResult.Count == 0;
			}
			return true;
		}

		private void ArrivedAtNavpoint(NpcPilotNavpointType navpointType)
		{
			ClearThrottle();
			switch (navpointType)
			{
			case NpcPilotNavpointType.Fleet:
				OnArrivedAtGroupNavpoint();
				break;
			case NpcPilotNavpointType.Combat:
				CombatNavigator.ArrivedAtNavpoint();
				break;
			}
			ClearPathfindingData();
		}

		private void ClearPathfindingData()
		{
			if (pathfindResult != null)
			{
				pathfindResult.Clear();
			}
			isNavpointFromPathfind = false;
			lastPathfindTime = float.MinValue;
		}

		private void SetDesiredThrottle(NpcPilotNavpoint navpoint, Vector3 navPointVector, float navPointDistance, ref float newDesiredThrottle)
		{
			float dotToNp = Vector3.Dot(CurrentUnit.transform.forward, navPointVector.normalized);
			float preferredThrottle = getPreferredThrottle();
			UnitComponentHolder unitComponentHolder = CurrentUnitComponents;
			newDesiredThrottle = 0f;
			if (navpoint.Arrive)
			{
				newDesiredThrottle = Arrive(preferredThrottle, unitComponentHolder.CurrentMaxSpeed, navpoint.ThrottleDotThreshold, navPointDistance, dotToNp);
			}
			else
			{
				newDesiredThrottle = preferredThrottle;
			}
		}

		private void OnArrivedAtGroupNavpoint()
		{
			HandleGroupPilotNavigated();
			if (fleet != null && (!CurrentUnit.IsActiveInEngine || Quaternion.Dot(CurrentUnit.transform.rotation, fleet.transform.rotation) < 0.98f))
			{
				SetDesiredBearingToMatchFleet();
			}
		}

		private void SetDesiredBearingToMatchFleet()
		{
			if (fleet.PilotCount > 1 || fleet.ActiveOrder != null)
			{
				SetDesiredBearingFromTargetSectorPosition(CurrentUnit.SectorPosition + fleet.transform.localRotation * Vector3.forward);
			}
		}

		private void UpdateMissileAvoidance()
		{
			if (!(Time.time > nextMissileAvoidUpdate))
			{
				return;
			}
			if (avoidingMissile != null && !EngineASX.Instance.MissileLockController.HasMissileLock(CurrentUnit))
			{
				avoidingMissile = null;
				if (avoidType == AIAvoidType.MissileAvoid)
				{
					ClearAvoidTarget();
				}
			}
			nextMissileAvoidUpdate = Time.time + Mathf.Lerp(2f, 0.25f, pilotSettings.CombatEfficiency);
			float nearestDist = 0f;
			Missile avoidMissile = GetAvoidMissile(out nearestDist);
			if (avoidMissile != null)
			{
				StartAvoidMissile(avoidMissile);
			}
		}

		private void ClearAvoidTarget()
		{
			avoidTarget = null;
			avoidingMissile = null;
			avoidType = AIAvoidType.None;
		}

		private Missile GetAvoidMissile(out float nearestDist)
		{
			return CurrentUnit.GetNearestMissileLock(out nearestDist);
		}

		private Projectile GetAvoidMine(out float nearestDist)
		{
			nearestDist = float.MaxValue;
			Projectile projectile = null;
			_ = Person.Sector;
			int num = Physics.OverlapSphereNonAlloc(transform.position, GameController.Instance.GameSettings.AIMineAvoidanceCheckDistance, EngineASX.ColliderCache, GameController.Instance.MineMask);
			Rigidbody rBody = CurrentUnitComponents.Unit.RBody;
			Vector3 vector = Vector3.zero;
			if (rBody != null)
			{
				vector = rBody.linearVelocity;
			}
			for (int i = 0; i < num; i++)
			{
				Projectile component = EngineASX.ColliderCache[i].GetComponent<Projectile>();
				if (component != null && component.ProjectileClass.IsWeapon && component.ProjectileClass.ExplosionClass != null)
				{
					float remainingTimeBeforeFuelDepletionAtMaxMoveSpeed = component.RemainingTimeBeforeFuelDepletionAtMaxMoveSpeed;
					Vector3 vector2 = vector * remainingTimeBeforeFuelDepletionAtMaxMoveSpeed;
					float distance2D = GetDistance2D(transform.position + vector2, component.transform.position);
					if (distance2D < component.ProjectileClass.ExplosionClass.MaxDistance * Engine.GameSettings.AIMineSafeDistanceMinMultiplier && (projectile == null || distance2D < nearestDist))
					{
						projectile = component;
						nearestDist = distance2D;
					}
				}
			}
			return projectile;
		}

		private float GetMineAvoidDist(float explosionRange)
		{
			return Mathf.Min(explosionRange + Engine.GameSettings.AIMaxMineAvoidDist, explosionRange * Engine.GameSettings.AIMineSafeDistanceMultiplier);
		}

		private float Arrive(float maxThrottle, float estimatedMaxSpd, float npThrottleDotThreshold, float navPointDistance, float dotToNp)
		{
			if (dotToNp > npThrottleDotThreshold)
			{
				float num = 0f;
				if (UnitRigidBody != null)
				{
					num = UnitRigidBody.linearVelocity.magnitude;
				}
				if (num == 0f)
				{
					return maxThrottle;
				}
				float num2 = num / UnitRigidBody.linearDamping * Engine.GameSettings.NpcPiilotArrivalSettings.AIArrive_StoppingDistMultiplier;
				if (num2 > navPointDistance)
				{
					return maxThrottle * Mathf.Pow(navPointDistance / num2, Engine.GameSettings.NpcPiilotArrivalSettings.AIArrive_StoppingPower) * Mathf.Pow(dotToNp, Engine.GameSettings.NpcPiilotArrivalSettings.AIArrive_TurnThrottlePower);
				}
				return maxThrottle * Mathf.Pow(dotToNp, Engine.GameSettings.NpcPiilotArrivalSettings.AIArrive_TurnThrottlePower);
			}
			return 0f;
		}

		private float getPreferredThrottle()
		{
			switch (targetNavpointType)
			{
			case NpcPilotNavpointType.Avoid:
				if (avoidType == AIAvoidType.Quick || avoidType == AIAvoidType.MissileAvoid || avoidType == AIAvoidType.OwnMine)
				{
					return 1f;
				}
				return Engine.GameSettings.AIPreferredThrottleWhenAvoiding;
			case NpcPilotNavpointType.Fleet:
				return fleet.PreferredSpeed / CurrentUnit.Components.CurrentMaxSpeed;
			case NpcPilotNavpointType.Combat:
				if (combatNavigator != null)
				{
					return combatNavigator.GetPreferredThrottle();
				}
				break;
			}
			return 1f;
		}

		public bool IsUnitInTargettingRange(Unit unit, float dist)
		{
			if (CurrentUnit.IsStatic)
			{
				return dist < CurrentUnitComponents.HighestRangedTurret + CurrentUnit.UnitClass.ShieldRingRadius - unit.UnitClass.ShieldRingRadius;
			}
			return true;
		}

		public bool IsTargetValid(Unit target)
		{
			if (target != null && target.IsValidAndNotDestroyed && target.Sector == CurrentUnit.Sector)
			{
				return target.IsTargettable(Person.Faction);
			}
			return false;
		}

		public void UpdateNextComponentUseCooldownTimeForWeaponsFire()
		{
			NpcPilotBehaviourSettings aiUnitControllerSettings = GameController.Instance.GameSettings.AiUnitControllerSettings;
			float num = Mathf.Lerp(aiUnitControllerSettings.MaxWeaponFireCooldownTime, aiUnitControllerSettings.MinWeaponFireCooldownTime, pilotSettings.CombatEfficiency);
			componentUseCooldownTime = Time.time + num;
		}

		public void UpdateNextComponentUseCooldownTimeForUndock()
		{
			NpcPilotBehaviourSettings aiUnitControllerSettings = GameController.Instance.GameSettings.AiUnitControllerSettings;
			float num = Mathf.Lerp(aiUnitControllerSettings.MaxUndockComponentUseCooldownTime, aiUnitControllerSettings.MinUndockComponentUseCooldownTime, pilotSettings.CombatEfficiency);
			componentUseCooldownTime = Time.time + num;
		}

		public void ValidateDistanceFromHanger(Unit hangerUnit)
		{
			float num = Vector3.Distance(SectorPosition, hangerUnit.SectorPosition);
			if (num > 500f)
			{
				Debug.LogWarning($"Ship {CurrentUnit} docking in a hanger ({hangerUnit}) from a distance of [{num}]");
			}
		}

		public float ScoreCargoToCollect(Unit cargoUnit, CargoClass cargoClass, int availableQuantity, CargoOwnership cargoOwnership)
		{
			if (cargoClass.IsReserved)
			{
				return float.MinValue;
			}
			float num = 0f;
			if (fleet != null)
			{
				num += fleet.ScoreCargoToCollect(this, cargoUnit, cargoClass, availableQuantity, cargoOwnership);
				if (num != 0f)
				{
					return num;
				}
				FleetCargoCollectionPreference cargoCollectionPreference = fleet.Settings.CargoCollectionPreference;
				if (cargoClass.IsEquipment)
				{
					if (IsCargoClassCompatible(cargoClass))
					{
						if ((cargoCollectionPreference & FleetCargoCollectionPreference.CompatibleEquipment) == 0)
						{
							return float.MinValue;
						}
					}
					else if ((cargoCollectionPreference & FleetCargoCollectionPreference.IncompatibleEquipment) == 0)
					{
						return float.MinValue;
					}
				}
				else if (cargoClass.IsTraded && (cargoCollectionPreference & FleetCargoCollectionPreference.TradableCargo) == 0)
				{
					return float.MinValue;
				}
			}
			else if (cargoOwnership == CargoOwnership.OwnedByOther)
			{
				return 0f;
			}
			if (cargoClass.IsEquipment)
			{
				if (IsCargoClassCompatible(cargoClass))
				{
					return 10000f + (float)availableQuantity;
				}
				int num2 = cargoClass.BasePrice * availableQuantity;
				if (!person.IsLocalFaction() && !((float)num2 > Mathf.Lerp(10000f, 0f, person.Greed)))
				{
					return 0f;
				}
				return (float)num2 * 0.1f;
			}
			int num3 = cargoClass.BasePrice * availableQuantity;
			if (!person.IsLocalFaction() && !((float)num3 > Mathf.Lerp(1000f, 0f, person.Greed)))
			{
				return 0f;
			}
			return num3;
		}

		public bool IsCargoClassCompatible(CargoClass cargoClass)
		{
			RecacheCompatibleAmmoTypesIfRequired();
			return compatibleAmmoTypes.Contains(cargoClass.UniqueId);
		}

		public void RecacheCompatibleAmmoTypesIfRequired()
		{
			if (compatibleAmmoTypes == null)
			{
				compatibleAmmoTypes = new HashSet<int>();
				CacheCompatibleAmmoTypes();
			}
			else if (Time.time - lastCachedCompatibleAmmoTypes > 20f)
			{
				CacheCompatibleAmmoTypes();
				lastCachedCompatibleAmmoTypes = Time.time;
			}
		}

		private void CacheCompatibleAmmoTypes()
		{
			compatibleAmmoTypes.Clear();
			CacheCompatibleAmmoTypes(CurrentUnit, compatibleAmmoTypes);
		}

		public static void CacheCompatibleAmmoTypes(Unit unit, HashSet<int> compatibleAmmoTypes)
		{
			compatibleAmmoTypes.Clear();
			if (!(unit.Components != null))
			{
				return;
			}
			UnitComponentHolder components = unit.Components;
			if (components.Turrets.Count <= 0)
			{
				return;
			}
			foreach (CargoClass item in components.YieldAllCompatibleAmmoCargoClasses())
			{
				if (!compatibleAmmoTypes.Contains(item.UniqueId))
				{
					compatibleAmmoTypes.Add(item.UniqueId);
				}
			}
		}

		public void NotifyUnitActiveChanged(bool active)
		{
			if (active)
			{
				OnUnitActive();
			}
			else
			{
				OnUnitInactive();
			}
		}

		public void NotifyTurretAdded(ActiveTurret turret)
		{
			notifyTurretAdded(turret);
		}

		public void NotifyTurretRemoved(ActiveTurret turret)
		{
			notifyTurretRemoved(turret);
		}

		public void NotifyMissileLock(Missile missile, int missileLockCount)
		{
			notifyMissileLocked(missile, missileLockCount);
		}

		public void NotifyComponentAdded(ComponentBase component)
		{
			component.UserPowered = true;
		}

		public void NotifyComponentRemoved(ComponentBase component)
		{
		}

		private void SetupUnitForNpcPilotting()
		{
			if (currentUnitComponents.AutoTurretModule != null)
			{
				currentUnitComponents.AutoTurretModule.FireMode = AutoTurretFireMode.Disabled;
			}
			foreach (TurretComponent turret in currentUnitComponents.Turrets)
			{
				if (!(turret is CloakComponent) && turret.CanCancelFire())
				{
					turret.CancelFiring();
				}
			}
			AutoLoadAllTurrets();
		}

		private void AutomateTurrets(bool enabled)
		{
			if (enabled)
			{
				currentUnitComponents.InitAutoTurretModuleIfNull();
				currentUnitComponents.AutoTurretModule.FireMode = AutoTurretFireMode.AnyTarget;
				currentUnitComponents.SetConventionalTurretsAutoFire(autoFire: true);
			}
			else if (currentUnitComponents.AutoTurretModule != null)
			{
				currentUnitComponents.AutoTurretModule.FireMode = AutoTurretFireMode.Disabled;
				currentUnitComponents.SetConventionalTurretsAutoFire(autoFire: false);
			}
		}

		public void FindUnit()
		{
			CurrentUnitComponents = UnityObjectHelper.FindInParent<UnitComponentHolder>(gameObject);
		}
	}
}
