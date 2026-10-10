using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.AutoTurrets;
using OpenFrontier.IP.Engine.CargoFactory;
using OpenFrontier.IP.Engine.CombatRatings;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.IP.Engine.Core.Units.UnitComponents;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Settings;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace OpenFrontier.IP.Engine
{
	[RequireComponent(typeof(Unit))]
	public class UnitComponentHolder : MonoBehaviour
	{
		public enum MoveTypes
		{
			Normal,
			Hyperdrive
		}

		internal Vector3 lastKnownVelocity = Vector3.zero;

		private float timePilotStatusChanged = -1f;

		internal float ClaimCooldownTime;

		private float estimatedPointDefenceEffectiveness;

		internal bool hasAutoFireTurrets;

		public Transform ComponentBaysRoot;

		private bool anyTurretUsesAmmo;

		private double captureCooldownTime;

		private float nextScanTime;

		internal float lastUpdate;

		private float lastTimeDetectionRangeUpdated;

		private bool isArmedWithConventionalWeapons;

		private float cachedCombatRating = -1f;

		private float lowestRangedTurret;

		private float highestRangedTurret;

		private float highestRangedPointDefenceTurret;

		internal int conventionalTurretCount;

		private float lastTimeFiredWeapons = float.MinValue;

		private AutoRepairUnit autoRepairComponent;

		private UnitShipTrader shipTrader;

		private bool isModified;

		[SerializeField]
		private float constructionProgress = 1f;

		[SerializeField]
		private ConstructionState constructionState;

		private static List<Person> pilotCache = new List<Person>();

		private static List<ComponentBase> reorderCache = new List<ComponentBase>(20);

		private ActiveUnitComponents activeUnitComponents;

		[NonSerialized]
		private List<ComponentBay> bays = new List<ComponentBay>(10);

		[NonSerialized]
		public CapacitorComponent Capacitor;

		private CargoBayComponent cargoBayComponent;

		public float CargoCapacity;

		[NonSerialized]
		public CloakComponent CloakComponent;

		private List<Person> people = new List<Person>();

		private float currentMaxSpeedIgnoringDamage;

		private UnitHangarBay dockedInHangarBay;

		private EngineASX engine;

		[NonSerialized]
		public UnitEngineComponent EngineComponent;

		[NonSerialized]
		public UnitCargoFactory FactoryComponent;

		private UnitHangar hangarComponent;

		private bool hasInit;

		private HyperdriveComponent hyperdrive;

		[NonSerialized]
		public float ImmobilizeTimeout;

		[NonSerialized]
		public bool IsMobile = true;

		private List<PassengerGroup> passengerGroups = new List<PassengerGroup>(1);

		[NonSerialized]
		public PassengerModuleComponent PassengerModule;

		[FormerlySerializedAs("pilot")]
		[SerializeField]
		private Person pilotPerson;

		[NonSerialized]
		public PwrGeneratorComponent PowerGenerator;

		public float ScanRange = 1500f;

		[NonSerialized]
		public ShieldComponent ShieldComponent;

		private TractorTurretComponent tractorTurretComponent;

		[SerializeField]
		private string shipName;

		private int shipNameId = -1;

		[NonSerialized]
		public List<TurretComponent> Turrets = new List<TurretComponent>();

		private ProjectileTurretComponent cachedCountermeasureTurret;

		private Unit unit;

		private List<ComponentBase> unitComponents = new List<ComponentBase>(10);

		private CargoTrader cargoTrader;

		private UnitAutoTurretModule autoTurretModule;

		private PointDefenceTurretModule pointDefenceTurretModule;

		internal bool anyComponentRequiresRecharge = true;

		private float lastTimePlayedShieldSound = float.MinValue;

		public float? AutoTurretFireCooldownTime { get; set; }

		public bool IsModified
		{
			get
			{
				return isModified;
			}
			set
			{
				isModified = value;
			}
		}

		public int FreePassengerSpace => PassengerCapacity - PassengerCount;

		public int PassengerCapacity
		{
			get
			{
				if (PassengerModule != null)
				{
					return PassengerModule.PassengerModuleClass.PassengerCapacity;
				}
				return 0;
			}
		}

		public int PassengerCount
		{
			get
			{
				int num = 0;
				for (int i = 0; i < passengerGroups.Count; i++)
				{
					num += passengerGroups[i].PassengerCount;
				}
				return num;
			}
		}

		public CargoBayComponent CargoBayComponent
		{
			get
			{
				return cargoBayComponent;
			}
			set
			{
				cargoBayComponent = value;
			}
		}

		public CargoTrader CargoTrader => cargoTrader;

		public List<PassengerGroup> PassengerGroups => passengerGroups;

		public TractorTurretComponent TractorTurret
		{
			get
			{
				return tractorTurretComponent;
			}
			internal set
			{
				tractorTurretComponent = value;
			}
		}

		public bool HasInit => hasInit;

		public float CapacitorCharge
		{
			get
			{
				if (Capacitor != null)
				{
					return Capacitor.Charge;
				}
				return 0f;
			}
			set
			{
				if (Capacitor != null)
				{
					Capacitor.Charge = value;
				}
			}
		}

		public float EngineThrottle
		{
			get
			{
				if (EngineComponent != null)
				{
					return EngineComponent.EngineThrottle;
				}
				return 0f;
			}
			set
			{
				if (EngineComponent != null)
				{
					EngineComponent.EngineThrottle = Mathf.Clamp01(value);
				}
			}
		}

		public float CurrentMaxSpeedIgnoringDamage => currentMaxSpeedIgnoringDamage;

		public float CurrentMaxSpeed
		{
			get
			{
				if (EngineComponent != null)
				{
					return EngineComponent.GetCurrentMaxSpeedConsideringDamage(unit);
				}
				return 0f;
			}
		}

		public bool IsCloaked
		{
			get
			{
				if (CloakComponent != null)
				{
					return CloakComponent.State == CloakState.Cloaked;
				}
				return false;
			}
		}

		public bool IsFullyDecloaked
		{
			get
			{
				if (!(CloakComponent == null))
				{
					return CloakComponent.State == CloakState.Decloaked;
				}
				return true;
			}
		}

		public List<ComponentBase> UnitComponents => unitComponents;

		public bool ShieldEnabled
		{
			get
			{
				if (ShieldComponent != null)
				{
					return GetIsShieldEnabled(ShieldComponent);
				}
				return false;
			}
		}

		public bool HasDisabledShield
		{
			get
			{
				ShieldComponent shieldComponent = ShieldComponent;
				if (shieldComponent == null)
				{
					return false;
				}
				return !GetIsShieldEnabled(shieldComponent);
			}
		}

		public UnitClass UnitClass => unit.UnitClass;

		public EngineASX Engine => engine;

		public Unit Unit
		{
			get
			{
				return unit;
			}
			private set
			{
				if (!(unit != value))
				{
					return;
				}
				unit = value;
				if (unit != null)
				{
					unit.Components = this;
					if (unit.Sector != null)
					{
						OnUnitChangedScene(null);
					}
				}
			}
		}

		public Person PilotPerson
		{
			get
			{
				return pilotPerson;
			}
			set
			{
				if (pilotPerson != value)
				{
					Person person = pilotPerson;
					if (person != null)
					{
						person.OnPilotStatusChanging(this);
					}
					pilotPerson = value;
					OnPilotChanged(person);
				}
			}
		}

		public bool IsDocked => dockedInHangarBay != null;

		public UnitHangarBay DockedInHangarBay
		{
			get
			{
				return dockedInHangarBay;
			}
			set
			{
				SetDockedInHangarBay(value, updateGameObjectParent: true);
			}
		}

		public UnitHangar DockedInHangar
		{
			get
			{
				if (dockedInHangarBay != null)
				{
					return dockedInHangarBay.Hangar;
				}
				return null;
			}
		}

		public Unit DockUnit
		{
			get
			{
				UnitHangar dockedInHangar = DockedInHangar;
				if (dockedInHangar != null)
				{
					return dockedInHangar.UnitComponents.unit;
				}
				return null;
			}
		}

		public List<Person> Crew => people;

		public string ShipName
		{
			get
			{
				return shipName;
			}
			set
			{
				shipName = value;
				RemoveShipNameIndex();
			}
		}

		public UnitHangar HangarComponent
		{
			get
			{
				return hangarComponent;
			}
			set
			{
				if (hangarComponent != value)
				{
					hangarComponent = value;
				}
			}
		}

		public List<ComponentBay> Bays => bays;

		public int ShipNameId => shipNameId;

		public ActiveUnitComponents ActiveUnitComponents
		{
			get
			{
				return activeUnitComponents;
			}
			set
			{
				activeUnitComponents = value;
			}
		}

		public HyperdriveComponent Hyperdrive
		{
			get
			{
				return hyperdrive;
			}
			set
			{
				hyperdrive = value;
				NotifyHyperdriveStateChange();
			}
		}

		public MoveTypes MoveType
		{
			get
			{
				if (hyperdrive != null && hyperdrive.IsActivated)
				{
					return MoveTypes.Hyperdrive;
				}
				return MoveTypes.Normal;
			}
		}

		public CloakState CloakState
		{
			get
			{
				if (CloakComponent != null)
				{
					return CloakComponent.State;
				}
				return CloakState.Decloaked;
			}
		}

		public AutoRepairUnit AutoRepairComponent
		{
			get
			{
				return autoRepairComponent;
			}
			set
			{
				autoRepairComponent = value;
			}
		}

		public bool CanAutoRepair
		{
			get
			{
				if (unit.Faction == null)
				{
					return false;
				}
				_ = unit.UnitType;
				_ = 1;
				return true;
			}
		}

		public bool ShouldRechargeComponents => !IsUnderConstructionOrDismantling;

		public float TimePilotStatusChanged => timePilotStatusChanged;

		public bool IsUnderConstruction => constructionState == ConstructionState.Constructing;

		public bool IsUnderConstructionOrDismantling => constructionState != ConstructionState.Constructed;

		public ConstructionState ConstructionState
		{
			get
			{
				return constructionState;
			}
			set
			{
				constructionState = value;
			}
		}

		public float ConstructionProgress
		{
			get
			{
				return constructionProgress;
			}
			set
			{
				constructionProgress = Mathf.Clamp01(value);
			}
		}

		public NpcPilot PilotNpc
		{
			get
			{
				if (PilotPerson != null)
				{
					return PilotPerson.NpcPilot;
				}
				return null;
			}
			set
			{
				if ((bool)value)
				{
					PilotPerson = value.Person;
				}
				else
				{
					PilotPerson = null;
				}
			}
		}

		public bool IsDockable
		{
			get
			{
				if (unit.IsValidAndNotDestroyed && !IsUnderConstructionOrDismantling)
				{
					return hangarComponent != null;
				}
				return false;
			}
		}

		public UnitShipTrader CustomShipTrader => GetComponent<UnitShipTrader>();

		public UnitShipTrader ShipTrader => shipTrader;

		public float Width => unit.UnitClass.DisplayData.Width;

		public UnitAutoTurretModule AutoTurretModule
		{
			get
			{
				return autoTurretModule;
			}
			set
			{
				autoTurretModule = value;
			}
		}

		public PointDefenceTurretModule PointDefenceTurretModule
		{
			get
			{
				return pointDefenceTurretModule;
			}
			set
			{
				pointDefenceTurretModule = value;
			}
		}

		public float LowestRangeTurret => lowestRangedTurret;

		public float HighestRangedTurret => highestRangedTurret;

		public float HighestRangePointDefenceTurret => highestRangedPointDefenceTurret;

		public int LastConventionalTurretCount => conventionalTurretCount;

		public bool IsArmedWithConventionalWeapons
		{
			get
			{
				return isArmedWithConventionalWeapons;
			}
			set
			{
				isArmedWithConventionalWeapons = value;
			}
		}

		public bool AnyTurretUsesAmmo
		{
			get
			{
				return anyTurretUsesAmmo;
			}
			set
			{
				anyTurretUsesAmmo = value;
			}
		}

		public float NextScanTime
		{
			get
			{
				return nextScanTime;
			}
			set
			{
				nextScanTime = value;
			}
		}

		public double CaptureCooldownTime
		{
			get
			{
				return captureCooldownTime;
			}
			set
			{
				captureCooldownTime = value;
			}
		}

		public float CombatRating
		{
			get
			{
				if (cachedCombatRating >= 0f)
				{
					return cachedCombatRating;
				}
				CacheCombatRating();
				return cachedCombatRating;
			}
		}

		public ProjectileTurretComponent CountermeasureTurret
		{
			get
			{
				return cachedCountermeasureTurret;
			}
			set
			{
				cachedCountermeasureTurret = value;
			}
		}

		public float EstimatedPointDefenceEffectiveness => estimatedPointDefenceEffectiveness;

		public void RefreshIsArmed()
		{
			isArmedWithConventionalWeapons = false;
			foreach (TurretComponent turret in Turrets)
			{
				if (turret.TurretClass.AIConventionalWeapon)
				{
					isArmedWithConventionalWeapons = true;
					break;
				}
			}
		}

		public void RefreshAnyTurretUsesAmmo()
		{
			anyTurretUsesAmmo = false;
			foreach (TurretComponent turret in Turrets)
			{
				if (turret.TurretClass.UsesAmmo)
				{
					anyTurretUsesAmmo = true;
					break;
				}
			}
		}

		private bool GetIsShieldEnabled(ShieldComponent shieldComponent)
		{
			if (shieldComponent.IsPoweredAndEnergySupplied)
			{
				if (!IsFullyDecloaked)
				{
					return GameController.Instance.GameSettings.GameplaySettings.ShieldsUpWhenCloaked;
				}
				return true;
			}
			return false;
		}

		private void SetDockedInHangarBay(UnitHangarBay value, bool updateGameObjectParent)
		{
			if (!(dockedInHangarBay != value))
			{
				return;
			}
			UnitHangarBay unitHangarBay = DockedInHangarBay;
			Unit rootUnit = unit.GetRootUnit();
			dockedInHangarBay = value;
			lastKnownVelocity = Vector3.zero;
			UpdateParent();
			if (dockedInHangarBay != null)
			{
				CancelFiringOnAllTurrets();
				SetDockUnitFactionToOursIfNull();
				SetFactionToDockUnitFactionIfNull();
				dockedInHangarBay.DockedUnit = this;
				OnDockedInHangar();
			}
			else
			{
				if (unitHangarBay != null)
				{
					Quaternion localRotation = Quaternion.identity;
					Vector3 localPosition;
					if (rootUnit.IsInActiveSector)
					{
						localPosition = rootUnit.GetSafeUndockSectorPosition(unit);
						localRotation = rootUnit.GetUndockRotation(unit);
					}
					else
					{
						localPosition = rootUnit.GetUndockSectorPosition(unit, unit.UnitClass.ShieldRingRadius);
					}
					transform.localPosition = localPosition;
					transform.localRotation = localRotation;
					unit.UpdateGasCloud();
				}
				OnNotDockedInHangar();
			}
			if (unitHangarBay != null)
			{
				unitHangarBay.DockedUnit = null;
			}
			OnDockedInHangarChanged(unitHangarBay, updateGameObjectParent);
		}

		private void SetFactionToDockUnitFactionIfNull()
		{
			Unit dockUnit = DockUnit;
			if (dockUnit != null && dockUnit.Faction != null && Unit.Faction == null)
			{
				Unit.Faction = dockUnit.Faction;
			}
		}

		private void SetDockUnitFactionToOursIfNull()
		{
			Unit dockUnit = DockUnit;
			if (dockUnit != null && dockUnit.Faction == null)
			{
				dockUnit.Faction = Unit.Faction;
			}
		}

		private void OnNotDockedInHangar()
		{
			unit.RemoveForcesAndControlInputs();
		}

		private void OnDockedInHangarChanged(UnitHangarBay oldBay, bool updateGameObjectParent)
		{
			unit.UpdateIsActive();
			if (unit.ActiveUnit != null)
			{
				unit.ActiveUnit.UpdateVisibility(immediate: true);
			}
			if (updateGameObjectParent)
			{
				UpdateParent();
			}
			engine.NotifyUnitHangarBayChanged(unit, oldBay, dockedInHangarBay);
		}

		private void OnDockedInHangar()
		{
			if (autoTurretModule != null)
			{
				autoTurretModule.ClearTargets();
			}
			unit.Sector = DockUnit.Sector;
			if (CloakComponent != null)
			{
				CloakComponent.State = CloakState.Decloaked;
			}
			if (hyperdrive != null)
			{
				hyperdrive.State = HyperdriveComponent.HyperDriveState.Deactivated;
			}
		}

		public void Init(Unit unit)
		{
			this.unit = unit;
			if (hasInit)
			{
				return;
			}
			hasInit = true;
			engine = EngineASX.Instance;
			UpdateShipTraderReference();
			cargoBayComponent = GetComponent<CargoBayComponent>();
			cargoBayComponent.UnitComponents = this;
			foreach (ComponentBay componentsInImmediateChild in ((ComponentBaysRoot != null) ? ComponentBaysRoot : transform).GetComponentsInImmediateChildren<ComponentBay>(includeInactive: false))
			{
				componentsInImmediateChild.Init(this);
			}
			cargoTrader = GetComponent<CargoTrader>();
			if (cargoTrader != null)
			{
				cargoTrader.Init();
			}
			UnitComponentTrader component = GetComponent<UnitComponentTrader>();
			if (component != null)
			{
				component.Init();
			}
			UnitCargoFactory component2 = GetComponent<UnitCargoFactory>();
			if (component2 != null)
			{
				component2.Init();
			}
			UnitHangar componentInChildren = GetComponentInChildren<UnitHangar>();
			if (componentInChildren != null)
			{
				componentInChildren.Init(this);
			}
			if (autoRepairComponent == null)
			{
				autoRepairComponent = GetComponent<AutoRepairUnit>();
			}
			if (autoRepairComponent != null)
			{
				autoRepairComponent.Init(this.unit);
			}
			UpdateMaxSpeed();
			UpdateTurretRanges();
			lastUpdate = Time.time;
		}

		public bool RequiresShipName()
		{
			if (unit.UnitType == UnitType.Ship)
			{
				return unit.UnitClass.ShipType == ShipType.Normal;
			}
			return false;
		}

		public void UpdateShipTraderReference()
		{
			shipTrader = FindShipTrader();
		}

		public void AddRandomDefaultCargoLoadout(float minRandomQuantityPercentage, float maxRandomQuantityPercentage)
		{
			UnitCargoLoadout defaultCargoLoadout = GetDefaultCargoLoadout();
			if (defaultCargoLoadout != null && !(defaultCargoLoadout is UnitCargoLoadoutAuto))
			{
				defaultCargoLoadout.ApplyRandomQuantity(unit, minRandomQuantityPercentage, maxRandomQuantityPercentage);
			}
		}

		public void AddDefaultCargoLoadout()
		{
			UnitCargoLoadout defaultCargoLoadout = GetDefaultCargoLoadout();
			if (defaultCargoLoadout != null && !(defaultCargoLoadout is UnitCargoLoadoutAuto))
			{
				defaultCargoLoadout.Apply(unit);
			}
		}

		public UnitCargoLoadout GetDefaultCargoLoadout()
		{
			UnitCargoLoadout component = unit.GetComponent<UnitCargoLoadout>();
			if (component == null)
			{
				UnitCargoLoadoutProfile component2 = unit.UnitClass.GetComponent<UnitCargoLoadoutProfile>();
				if (component2 != null)
				{
					return component2.CargoLoadout;
				}
			}
			return component;
		}

		public void InstallDefaultComponents(bool checkForOverride = false)
		{
			foreach (ComponentBay bay in bays)
			{
				InstallBayDefaultComponent(bay, checkForOverride);
			}
			UpdateMaxSpeed();
		}

		public void InstallBayDefaultComponent(ComponentBay bay, bool checkForOverride = false)
		{
			ComponentClass componentClass = bay.InitialComponentClass;
			if (checkForOverride)
			{
				ComponentBayOverride component = bay.GetComponent<ComponentBayOverride>();
				if (component != null)
				{
					componentClass = component.ComponentClass;
				}
			}
			if (componentClass != null)
			{
				bay.InstallComponent(componentClass);
			}
			if (bay.InstalledComponent != null)
			{
				bay.InstalledComponent.RechargeFull();
			}
		}

		public void Update()
		{
			if (hasInit && unit.Engine != null && unit.IsInActiveSector)
			{
				Tick();
			}
		}

		/// <summary>
		/// Whether this copy simulates the ship, and therefore whether
		/// <see cref="Tick"/> runs at all.
		/// </summary>
		/// <remarks>
		/// One place, because <see cref="Tick"/> is one place. Everything a
		/// ship does lives in it - the pilot's AI, the components, auto-turrets,
		/// point defence, construction, scanning, detection - and it is reached
		/// from exactly two callers: Unity's per-frame Update for the active
		/// sector, and the engine's round-robin for the rest.
		///
		/// So a ship that is not this copy's to simulate simply does not tick.
		/// Not frozen - not asked. Freezing it was the previous approach and it
		/// only ever covered the physics: the AI above it kept deciding
		/// things, and a partially stilled ship is the worst of both.
		///
		/// The check is on IsThisCopyAClient first, so a singleplayer game
		/// never gets as far as asking which ship this is.
		/// </remarks>
		private bool ShouldSimulateThisShip()
		{
			if (!Authority.IsThisCopyAClient)
			{
				return true;
			}

			if (Authority.OwnsUnit(unit))
			{
				return true;
			}

			Authority.Declined();

			return false;
		}

		/// <summary>
		/// Recomputes how far this ship's sensors reach, once a second.
		/// </summary>
		/// <remarks>
		/// Deliberately not behind the simulation gate. See the call in Tick.
		///
		/// Skipped for a ship that is not built or is gone, because the class
		/// is what the range is measured from and a ship with none has no
		/// range to report.
		/// </remarks>
		private void RefreshDetectionRadiusIfDue()
		{
			if (unit.IsDestroyed || !hasInit
				|| constructionState != ConstructionState.Constructed)
			{
				return;
			}

			if (Time.time <= lastTimeDetectionRangeUpdated + 1f)
			{
				return;
			}

			unit.UpdateDetectionRadius();
			lastTimeDetectionRangeUpdated = Time.time;

			Authority.NoteDerivedValueRefreshed();
		}

		public void Tick()
		{
			if (!ShouldSimulateThisShip())
			{
				// A copy that is not flying this ship still knows how far its
				// sensors reach.
				//
				// Detection range is arithmetic on the ship's own class, the
				// gas cloud it is in, and its cloak, shields and power. None
				// of that is a decision, and none of it is something the host
				// would have to send - both copies hold the same class and the
				// same components, so both compute the same number.
				//
				// It was inside the gated section, which left every ship this
				// copy is not flying holding whatever the world happened to be
				// loaded with. That value is derived, so it is not saved: a
				// freshly loaded ship starts at zero, and a remote player's
				// ship was seeing nothing at all.
				RefreshDetectionRadiusIfDue();

				return;
			}

			if (lastUpdate == 0f)
			{
				lastUpdate = Time.time;
			}
			if (!GameController.Instance.GameSettings.DebugSettings.UnitUpdateEnabled)
			{
				return;
			}
			float num = Time.time - lastUpdate;
			if (num == 0f)
			{
				return;
			}
			lastUpdate = Time.time;
			if (unit.IsDestroyed || !hasInit)
			{
				return;
			}
			switch (constructionState)
			{
			case ConstructionState.Constructing:
				TickConstruction(num);
				break;
			case ConstructionState.Dismantling:
				TickDismantling(num);
				break;
			case ConstructionState.Constructed:
				UpdateComponents(num);
				if (PilotPerson != null && PilotPerson.NpcPilot != null)
				{
					PilotPerson.NpcPilot.Tick(num);
				}
				if (Time.time > nextScanTime && ShouldPerformScan())
				{
					PerformScan();
					nextScanTime = Time.time + (unit.IsInActiveSector ? GameController.Instance.GameSettings.IntelSettings.UnitScanWhenActiveFrequency : GameController.Instance.GameSettings.IntelSettings.UnitScanWhenInactiveFrequency);
				}
				RefreshDetectionRadiusIfDue();
				if (!IsDocked && IsFullyDecloaked)				{
					if (autoTurretModule != null)
					{
						autoTurretModule.Update(num);
					}
					if (pointDefenceTurretModule != null)
					{
						pointDefenceTurretModule.Update(num);
					}
				}
				if (autoRepairComponent != null && CanAutoRepair)
				{
					autoRepairComponent.Tick(num);
				}
				if (FactoryComponent != null && FactoryComponent.enabled)
				{
					FactoryComponent.Tick(num);
				}
				if (!IsMobile && ImmobilizeTimeout >= 0f && Time.time > ImmobilizeTimeout)
				{
					IsMobile = true;
				}
				break;
			}
		}

		public bool ShouldPerformScan()
		{
			if (unit.Faction != null && ScanRange > 0f)
			{
				return dockedInHangarBay == null;
			}
			return false;
		}

		public void PerformScan()
		{
			unit.Faction.Intel.PerformScan(unit.Sector, unit.SectorPosition, ScanRange + unit.UnitClass.ShieldRingRadius, unit);
		}

		public void PerformImmediateScan(bool callOnNewUnitScanned)
		{
			unit.Faction.Intel.PerformScanImmediate(unit.Sector, unit.SectorPosition, ScanRange + unit.UnitClass.ShieldRingRadius, unit, callOnNewUnitScanned);
		}

		public void OnUnitDamagedButNotDestroyed(ref UnitDamageInfo damageInfo, float baseDamage, Unit sourceUnit, Faction sourceFaction, Person ourPilot)
		{
			if (!unit.Sector.IsActive || !(unit.ActiveUnit != null))
			{
				return;
			}
			if (unit.ActiveUnit.LastDistanceFromCamera < 1200f)
			{
				if (damageInfo.AnyShieldDepleted)
				{
					PlayShieldsDownAudio();
				}
				else if (damageInfo.ShieldDamage > 0f)
				{
					PlayShieldHitAudio();
				}
			}
			if (unit == EngineASX.Instance.HudCamera.SpectateTarget && ((damageInfo.ShieldDamage > 0f && GameController.Instance.PlayerOptions.General_CameraShakeOnShieldHit) || (damageInfo.HullDamage > 0f && GameController.Instance.PlayerOptions.General_CameraShakeOnHullHit)))
			{
				EngineASX.Instance.MainCamera.ShakeModule.StartShake(GameController.Instance.GameSettings.DamageShakeDuration, baseDamage * GameController.Instance.GameSettings.DamageToShakeMagnitudeMultiplier);
			}
			if (pilotPerson != null)
			{
				if (damageInfo.AnyShieldDepleted)
				{
					pilotPerson.RaiseDialogEventRandomly(engine.DialogEvents.ShieldsDown);
				}
				else
				{
					RaiseAttackedDialogEvents(damageInfo.ShieldDamage > 0f, sourceFaction);
				}
			}
		}

		public void OnAboutToBeDestroyed(Unit sourceUnit)
		{
			if (unit.IsInActiveSector && sourceUnit != null && sourceUnit.IsInActiveSector && unit.IsStationOrShip() && sourceUnit != this && sourceUnit.Components != null && sourceUnit.Components.PilotPerson != null && sourceUnit.IsHostileTo(unit))
			{
				sourceUnit.Components.PilotPerson.RaiseDialogEventRandomly(EngineASX.Instance.DialogEvents.DestroyedTarget);
			}
			if (pilotPerson != null && pilotPerson.Sector.IsActive)
			{
				pilotPerson.RaiseDialogEventRandomly(engine.DialogEvents.Destroyed);
			}
		}

		private void RaiseAttackedDialogEvents(bool shieldDamage, Faction sourceFaction)
		{
			if (!(pilotPerson != null) || !(pilotPerson.Faction != sourceFaction))
			{
				return;
			}
			if (pilotPerson.Faction != null && sourceFaction != null && !pilotPerson.Faction.IsHostileTo(sourceFaction))
			{
				if (pilotPerson.Faction.IsAlliedTo(sourceFaction))
				{
					pilotPerson.RaiseDialogEventRandomly(engine.DialogEvents.AttackedByFriendly);
					return;
				}
				float opinion = pilotPerson.Faction.GetOpinion(sourceFaction);
				if (opinion < -0.25f)
				{
					pilotPerson.RaiseDialogEventRandomly(engine.DialogEvents.AttackedByUnfriendly);
				}
				else if (opinion > 0.25f)
				{
					pilotPerson.RaiseDialogEventRandomly(engine.DialogEvents.AttackedByFriendly);
				}
				else
				{
					pilotPerson.RaiseDialogEventRandomly(engine.DialogEvents.AttackedByNeutral);
				}
			}
			else if (shieldDamage)
			{
				pilotPerson.RaiseDialogEventRandomly(engine.DialogEvents.TakeDamage);
			}
			else
			{
				pilotPerson.RaiseDialogEventRandomly(engine.DialogEvents.HullDamage);
			}
		}

		public void CleanupUnit()
		{
			EngineASX.Instance.DestroyJobsAtUnit(unit);
			RemoveSectorControlIfApplicable();
			DestroyCrew();
			SetDockedInHangarBay(null, updateGameObjectParent: false);
			UndockAllDockedUnits();
			RemoveShipNameIndex();
		}

		public void RemoveSectorControlIfApplicable()
		{
			if (unit.Sector != null && unit.UnitClass.StationPurpose == StationPurpose.SectorControl)
			{
				unit.Sector.ChangeControllingFaction(null, setTimeOfChange: true);
			}
		}

		public float GetCapacitorNormalized()
		{
			if (Capacitor != null)
			{
				return Capacitor.ChargeNormalized;
			}
			return 0f;
		}

		public int GetCargoCountOf(CargoClass cargoClass)
		{
			if (CargoBayComponent != null)
			{
				return CargoBayComponent.GetCountOf(cargoClass);
			}
			return 0;
		}

		public void FullyRechargeComponents()
		{
			for (int i = 0; i < unitComponents.Count; i++)
			{
				unitComponents[i].RechargeFull();
			}
		}

		public void UpdateComponents(float elapsedTime)
		{
			if (elapsedTime == 0f)
			{
				return;
			}
			CloakComponent cloakComponent = CloakComponent;
			if (cloakComponent != null)
			{
				cloakComponent.UpdateCloak(elapsedTime);
			}
			if (anyComponentRequiresRecharge)
			{
				bool flag = false;
				foreach (ComponentBase unitComponent in unitComponents)
				{
					if (unitComponent.RequiresRecharge())
					{
						flag = true;
						if (unitComponent.UserPowered && unitComponent.AutoChargeEnabled)
						{
							unitComponent.RechargeTick(elapsedTime);
						}
					}
				}
				anyComponentRequiresRecharge = flag;
			}
			if (!hasAutoFireTurrets)
			{
				return;
			}
			foreach (TurretComponent turret in Turrets)
			{
				if (turret.AutoFire && turret.UserPowered)
				{
					turret.UpdateAutoFire(elapsedTime);
				}
			}
		}

		public bool GetIfAnyComponentNeedsRecharge()
		{
			foreach (ComponentBase unitComponent in unitComponents)
			{
				if (unitComponent.RequiresRecharge())
				{
					return true;
				}
			}
			return false;
		}

		public void RegisterComponentBay(ComponentBay bay)
		{
			if (bay == null)
			{
				throw new NullReferenceException("bay");
			}
			if (!bays.Contains(bay))
			{
				bays.Add(bay);
			}
		}

		public void DeregisterComponentBay(ComponentBay bay)
		{
			if (bays.Contains(bay))
			{
				bays.Remove(bay);
			}
		}

		public void RegisterComponent(ComponentBase component)
		{
			if (component == null)
			{
				throw new NullReferenceException("component");
			}
			if (!unitComponents.Contains(component))
			{
				int i;
				for (i = 0; i < unitComponents.Count && component.GetRechargePriority() < unitComponents[i].GetRechargePriority(); i++)
				{
				}
				unitComponents.Insert(i, component);
			}
		}

		public void DeregisterComponent(ComponentBase component)
		{
			if (unitComponents.Contains(component))
			{
				unitComponents.Remove(component);
			}
		}

		public void DecreaseComponentRechargePriority(ComponentBase component)
		{
			if (unitComponents.Count > 1)
			{
				int num = unitComponents.IndexOf(component);
				if (num < unitComponents.Count - 1)
				{
					int index = num + 1;
					ComponentBase value = unitComponents[index];
					unitComponents[num] = value;
					unitComponents[index] = component;
				}
			}
		}

		public void IncreaseComponentRechargePriority(ComponentBase component)
		{
			if (unitComponents.Count > 1)
			{
				int num = unitComponents.IndexOf(component);
				if (num > 1)
				{
					int index = num - 1;
					ComponentBase value = unitComponents[index];
					unitComponents[num] = value;
					unitComponents[index] = component;
				}
			}
		}

		public int CountComponentsOfType(ComponentType componentType)
		{
			int num = 0;
			foreach (ComponentBase unitComponent in unitComponents)
			{
				if (unitComponent.ComponentClass.ComponentType.GetEntityId() == componentType.GetEntityId())
				{
					num++;
				}
			}
			return num;
		}

		public bool SupportsAdditionalComponentOfType(ComponentType componentType)
		{
			int num = CountComponentsOfType(componentType) + 1;
			if (num >= componentType.MinCount)
			{
				return num <= componentType.MaxCount;
			}
			return false;
		}

		public bool VerifyHasRequiredComponents()
		{
			bool result = true;
			foreach (ComponentType componentsType in engine.ComponentsTypes)
			{
				int num = CountComponentsOfType(componentsType);
				if (num < componentsType.MinCount || num > componentsType.MaxCount)
				{
					Debug.LogWarning($"{this}: unit does not have required count of component type \"{componentsType}\". Current Count: {num}, Min: {componentsType.MinCount}, Max: {componentsType.MaxCount}", this);
					result = false;
				}
			}
			return result;
		}

		public float ReduceDamageFromArmour(ComponentDamageSettings settings, float hullArmourRating, float hullDamageNormalized, float damage)
		{
			float armourToDamageAbsorbtionMin = settings.ArmourToDamageAbsorbtionMin;
			armourToDamageAbsorbtionMin += hullArmourRating * settings.ArmourRatingMultiplier;
			armourToDamageAbsorbtionMin += (1f - hullDamageNormalized) * settings.AbsorbtionFromHullHealthPercent;
			if (armourToDamageAbsorbtionMin > settings.ArmourToDamageAbsorbtionMax * 0.9f)
			{
				armourToDamageAbsorbtionMin = settings.ArmourToDamageAbsorbtionMax * 0.9f;
			}
			float num = Maths.RandomFloatWithPower(armourToDamageAbsorbtionMin, settings.ArmourToDamageAbsorbtionMax, settings.ArmourAbsorbtionPower - hullArmourRating);
			damage *= 1f - num;
			if (damage < 0f)
			{
				return 0f;
			}
			return damage;
		}

		public void ApplyHullDamageToComponents(float baseDamage, float hullHealthBeforeDamage)
		{
			ComponentDamageSettings componentDamageSettings = GameController.Instance.GameSettings.ComponentDamageSettings;
			if (unitComponents.Count == 0)
			{
				return;
			}
			float armourRating = GetArmourRating(UnitClass.HullType);
			baseDamage *= Maths.RandomFloatWithPower(componentDamageSettings.MinComponentDamagePercent, componentDamageSettings.MaxComponentDamagePercent, componentDamageSettings.ComponentDamagePercentPower);
			float damage = baseDamage * componentDamageSettings.BaseDamageToComponentDamage;
			float num = hullHealthBeforeDamage / unit.UnitClass.maxHealth;
			damage = ReduceDamageFromArmour(componentDamageSettings, armourRating, 1f - num, damage);
			int num2 = 0;
			while (damage > 0f && num2 < componentDamageSettings.MaxIterations)
			{
				ComponentBase componentBase = unitComponents[UnityEngine.Random.Range(0, unitComponents.Count)];
				if (componentBase.ComponentClass.ComponentType.CanBeDamaged)
				{
					float num3 = Mathf.Clamp01(Maths.RandomFloatWithPower(componentDamageSettings.MinDamageToOneComponent, componentDamageSettings.MaxDamageToOneComponent, componentDamageSettings.DamageToOneComponentPower)) * damage;
					damage -= num3;
					float num4 = 1f - componentBase.HealthNormalized;
					if (num4 > 0.5f)
					{
						num3 *= Mathf.Lerp(1f, 0.1f, (num4 - 0.5f) / 0.5f);
					}
					componentBase.ApplyDamage(num3);
				}
				num2++;
			}
		}

		private static float GetArmourRating(ShipHullType hullType)
		{
			return hullType switch
			{
				ShipHullType.Scout => 0.08f, 
				ShipHullType.Fighter => 0.1f, 
				ShipHullType.Frigate => 0.12f, 
				ShipHullType.Destroyer => 0.16f, 
				ShipHullType.Cruiser => 0.2f, 
				ShipHullType.Battleship => 0.24f, 
				_ => 1f, 
			};
		}

		public void DamageShields(float baseDamage, int shieldIndex, ShieldDamageType shieldDamageType, ref UnitDamageInfo damageInfo, ref float remainingDamage)
		{
			switch (shieldDamageType)
			{
			case ShieldDamageType.Envelope:
			{
				float currentTotalShieldPoints = ShieldComponent.CurrentTotalShieldPoints;
				damageInfo.ShieldDamage = Mathf.Min(baseDamage, currentTotalShieldPoints);
				if (remainingDamage >= currentTotalShieldPoints)
				{
					ShieldComponent.DepleteAllShields();
				}
				else
				{
					float b = remainingDamage / 6f;
					float num4 = damageInfo.ShieldDamage;
					for (int j = 0; j < 6; j++)
					{
						float shieldPoints2 = ShieldComponent.GetShieldPoints(j);
						float num5 = Mathf.Min(num4, Mathf.Min(shieldPoints2, b));
						if (!damageInfo.AnyShieldDepleted)
						{
							damageInfo.AnyShieldDepleted = num5 >= shieldPoints2;
						}
						ShieldComponent.ChangeShieldPoints(j, 0f - num5);
						num4 -= num5;
					}
				}
				remainingDamage -= damageInfo.ShieldDamage;
				break;
			}
			case ShieldDamageType.Normal:
			{
				if (shieldIndex <= -1)
				{
					break;
				}
				float shieldPoints3 = ShieldComponent.GetShieldPoints(shieldIndex);
				if (shieldPoints3 > 0f)
				{
					damageInfo.ShieldDamage = Mathf.Min(baseDamage, shieldPoints3);
					remainingDamage -= damageInfo.ShieldDamage;
					ShieldComponent.ChangeShieldPoints(shieldIndex, 0f - damageInfo.ShieldDamage);
					if (ShieldComponent.IsShieldDepleted(shieldIndex))
					{
						damageInfo.AnyShieldDepleted = true;
					}
				}
				break;
			}
			case ShieldDamageType.Spread:
			{
				if (shieldIndex <= -1)
				{
					break;
				}
				int num = shieldIndex - 1;
				for (int i = 0; i < 3; i++)
				{
					int num2 = WrapShieldIndex(num + i);
					float shieldPoints = ShieldComponent.GetShieldPoints(num2);
					if (shieldPoints > 0f)
					{
						float num3 = Mathf.Min((num2 == shieldIndex) ? (baseDamage * 0.5f) : (baseDamage * 0.25f), shieldPoints);
						damageInfo.ShieldDamage += num3;
						ShieldComponent.ChangeShieldPoints(num2, 0f - num3);
						if (ShieldComponent.IsShieldDepleted(num2))
						{
							damageInfo.AnyShieldDepleted = true;
						}
					}
				}
				remainingDamage -= damageInfo.ShieldDamage;
				break;
			}
			case ShieldDamageType.Ignore:
				break;
			}
		}

		public string GetEditorName(Unit editorUnit)
		{
			string arg = ((editorUnit.Faction != null) ? editorUnit.Faction.GetShortNameElseLong() : "Abandoned");
			string arg2 = ((editorUnit.UnitClass != null) ? editorUnit.UnitClass.GetClassAndSeriesName(shortName: true) : $"{editorUnit.UnitType}_No_class");
			return $"{arg2}_{editorUnit.UniqueId}_{arg}";
		}

		public void UpdateMaxSpeed()
		{
			currentMaxSpeedIgnoringDamage = CalculateMaxSpeed();
		}

		public float CalculateMaxSpeed()
		{
			if (EngineComponent != null)
			{
				Rigidbody rBody = unit.RBody;
				if (rBody != null)
				{
					return EngineComponent.EngineClass.CalculateMaxSpeed(rBody);
				}
				return EngineComponent.EngineClass.CalculateMaxSpeed(UnitClass.Drag, unit.Mass);
			}
			return 0f;
		}

		public int WrapShieldIndex(int index)
		{
			return Maths.WrapValue(index, 0, 6);
		}

		public void SetComponentsEnabled(bool enabled)
		{
			for (int i = 0; i < unitComponents.Count; i++)
			{
				unitComponents[i].gameObject.SetActive(enabled);
			}
		}

		public void PlayShieldHitAudio()
		{
			AudioSource shieldHitAudio = GetShieldHitAudio();
			if (shieldHitAudio != null)
			{
				engine.PlayPooledAudioSource(shieldHitAudio, transform.position);
			}
		}

		public void PlayShieldsDownAudio()
		{
			if (Time.time > lastTimePlayedShieldSound + 0.1f)
			{
				AudioSource shieldDownAudio = GetShieldDownAudio();
				if (shieldDownAudio != null)
				{
					engine.PlayPooledAudioSource(shieldDownAudio, transform.position);
					lastTimePlayedShieldSound = Time.time;
				}
			}
		}

		public void PlayShieldsUpAudio()
		{
			if (Time.time > lastTimePlayedShieldSound + 0.1f)
			{
				AudioSource shieldUpAudio = GetShieldUpAudio();
				if (shieldUpAudio != null)
				{
					engine.PlayPooledAudioSource(shieldUpAudio, transform.position);
					lastTimePlayedShieldSound = Time.time;
				}
			}
		}

		public int GetCurrentComponentsMoneyValue(bool ignoreDamage)
		{
			int num = 0;
			foreach (ComponentBase unitComponent in unitComponents)
			{
				num = ((!ignoreDamage) ? (num + unitComponent.CalculateMoneyValue()) : (num + unitComponent.ComponentClass.GetActualCost()));
			}
			return num;
		}

		public void Kill(Faction attackerFaction, KillData killData)
		{
			if (PilotNpc != null)
			{
				PilotNpc.CurrentUnit = null;
			}
			KillCrew(attackerFaction, killData);
			KillDockedShips(killData);
			EngineThrottle = 0f;
			SetComponentsEnabled(enabled: false);
		}

		public void OnUnitChangedScene(Sector oldScene)
		{
			if (hangarComponent != null)
			{
				hangarComponent.UpdateDockedUnitsScene();
			}
		}

		public ComponentBay[] FindComponentBays()
		{
			return transform.GetComponentsInChildren<ComponentBay>();
		}

		public void Immobilize(float duration)
		{
			IsMobile = false;
			ImmobilizeTimeout = Time.time + duration;
		}

		public void NotifyCollisionDamage(float forceOfImpact)
		{
			if (engine.GameSettings.GameplaySettings.DecloakUnitsWhenCollisionDamage && !IsFullyDecloaked)
			{
				CloakComponent.State = CloakState.Decloaking;
			}
			float num = forceOfImpact * engine.GameSettings.UnitCollisionImmobilizationFactor;
			if (num > engine.GameSettings.UnitCollisionMaxImmobileTime)
			{
				num = engine.GameSettings.UnitCollisionMaxImmobileTime;
			}
			Immobilize(num);
		}

		public void RemovePilotControl()
		{
			if (EngineComponent != null)
			{
				EngineComponent.EngineThrottle = 0f;
			}
			if (unit.ActiveUnit != null && unit.ActiveUnit.ActiveUnitShip != null)
			{
				unit.ActiveUnit.ActiveUnitShip.DesiredTurn = 0f;
			}
		}

		public bool TryDockInHangar(UnitHangar hangar)
		{
			if (hangar != null)
			{
				if (unit.IsDestroyed)
				{
					return false;
				}
				if (!hangar.UnitComponents.IsDockable)
				{
					return false;
				}
				if (DockedInHangar == hangar)
				{
					return true;
				}
				UnitHangarBay bestUnoccupiedBay = hangar.GetBestUnoccupiedBay(this);
				if (bestUnoccupiedBay != null)
				{
					DockedInHangarBay = bestUnoccupiedBay;
					return true;
				}
			}
			return false;
		}

		public bool TryDockInUnit(Unit unit)
		{
			if (Authority.Refused(
				"docking at a station",
				"moved ship " + (unit == null ? "?" : unit.UniqueId.ToString())
					+ " inside a hangar the host does not know about"))
			{
				return false;
			}

			if (unit != null)
			{
				UnitHangar hangar = unit.GetHangar();
				if (hangar != null)
				{
					return TryDockInHangar(hangar);
				}
			}
			Debug.LogError("Attemping to dock at null target", this);
			return false;
		}

		public void FindParents()
		{
			UnitHangarBay unitHangarBay = (DockedInHangarBay = FindDockHangarBay());
			if (unitHangarBay == null)
			{
				UnitHangar unitHangar = UnityObjectHelper.FindInParentsOrSelf<UnitHangar>(gameObject);
				if (unitHangar != null)
				{
					TryDockInHangar(unitHangar);
				}
			}
			if (dockedInHangarBay == null)
			{
				unit.Sector = UnityObjectHelper.FindInParentsOrSelf<Sector>(gameObject);
			}
		}

		public void UpdateParent()
		{
			if (DockUnit != null)
			{
				SetParentToDockingBay();
			}
			else if (unit.Sector != null)
			{
				gameObject.transform.SetParent(unit.Sector.transform, worldPositionStays: true);
			}
			else
			{
				gameObject.transform.SetParent(null, worldPositionStays: true);
			}
		}

		public bool UndockIfDocked()
		{
			// Docked-ness is not on the wire. UnitState carries a transform
			// and nothing else, so there is no field for it and nothing to
			// correct it with afterwards: a client that docked locally put its
			// ship inside a hangar the host has never heard of, and the two
			// copies' UnitHangar.Items stayed different for the rest of the
			// session. It shows - the sector map filters on IsDocked, and the
			// joiner factory skips docked ships when it takes a snapshot.
			//
			// Undocking is refused for the same reason, and for one more:
			// only the host knows what is in the hangar, so only the host can
			// take a ship out of it.
			if (Authority.Refused("undocking", "moved a ship out of a hangar"))
			{
				return false;
			}

			if (IsDocked)
			{
				DockedInHangarBay = null;
				return true;
			}
			return false;
		}

		public UnitHangarBay FindDockHangarBay()
		{
			if (gameObject.transform.parent != null)
			{
				return UnityObjectHelper.FindInParentsOrSelf<UnitHangarBay>(gameObject.transform.parent.gameObject);
			}
			return null;
		}

		public void RestoreComponentsHealth()
		{
			foreach (ComponentBase unitComponent in unitComponents)
			{
				unitComponent.HealthPoints = unitComponent.ComponentClass.MaxHealthPoints;
			}
		}

		public void AssignAndReserveShipName(ShipNames shipNames, int shipNameId)
		{
			this.shipNameId = shipNameId;
			shipName = shipNames.GetShipNameById(shipNameId);
			shipNames.ReserveShipName(shipNameId);
		}

		public void ClearShipName()
		{
			EngineASX.Instance.ShipNames.UnreserveShipName(ShipNameId);
			shipNameId = -1;
		}

		public void OnUnitActive()
		{
			for (int i = 0; i < unitComponents.Count; i++)
			{
				unitComponents[i].OnUnitActive();
			}
		}

		public void OnUnitInactive()
		{
			for (int i = 0; i < unitComponents.Count; i++)
			{
				unitComponents[i].OnUnitInactive();
			}
		}

		public void DropCargoOnDestroyed()
		{
			if (!(CargoBayComponent != null))
			{
				return;
			}
			float num = Mathf.Lerp(engine.GameSettings.DestroyedUnitMinDroppedCargo, engine.GameSettings.DestroyedUnitMaxDroppedCargo, Mathf.Pow(UnityEngine.Random.value, engine.GameSettings.DestroyedUnitCargoDropPower));
			foreach (KeyValuePair<CargoClass, int> cargo2 in CargoBayComponent.Cargos)
			{
				int num2 = Mathf.RoundToInt(num * (float)cargo2.Value);
				if (num2 > 0)
				{
					Cargo cargo = unit.CreateLootItem(cargo2.Key, num2);
					if (engine.GameSettings.RetainLootOwnership)
					{
						cargo.Unit.Faction = unit.Faction;
					}
				}
			}
		}

		public void AddDefaultProjectileAmmo()
		{
			AddDefaultProjectileAmmo(1f, ignoreCapacity: false);
		}

		public void AddDefaultProjectileAmmo(float countMultiplier, bool ignoreCapacity)
		{
			for (int i = 0; i < Turrets.Count; i++)
			{
				ProjectileTurretClass projectileTurretClass = Turrets[i].TurretClass as ProjectileTurretClass;
				if (projectileTurretClass != null)
				{
					AddDefaultProjectileAmmo(countMultiplier, projectileTurretClass, ignoreCapacity);
				}
			}
		}

		public void AddDefaultProjectileAmmo(float countMultiplier, ProjectileTurretClass p, bool ignoreCapacity)
		{
			for (int i = 0; i < p.CompatibleProjectiles.Count; i++)
			{
				if (p.CompatibleProjectiles[i].AmmoClass != null && p.CompatibleProjectiles[i].AmmoRequirement > 0)
				{
					CargoBayComponent.AddToCargo(p.CompatibleProjectiles[i].AmmoClass, Mathf.CeilToInt((float)p.CompatibleProjectiles[i].DefaultAmmoComplement * countMultiplier), ignoreCapacity);
				}
			}
		}

		[Obsolete]
		public TractorTurretComponent GetTractorTurret()
		{
			return tractorTurretComponent;
		}

		public bool IsCargoClassCompatibleWithAnyComponent(CargoClass cargoClass)
		{
			foreach (ComponentBase unitComponent in unitComponents)
			{
				if (unitComponent.CanUseCargoClass(cargoClass))
				{
					return true;
				}
			}
			return false;
		}

		public IEnumerable<CargoClass> YieldAllCompatibleAmmoCargoClasses()
		{
			foreach (TurretComponent turret in Turrets)
			{
				if (!(turret is ProjectileTurretComponent projectileTurretComponent))
				{
					continue;
				}
				foreach (ProjectileClass compatibleProjectile in projectileTurretComponent.ProjectileTurretClass.CompatibleProjectiles)
				{
					if (compatibleProjectile.AmmoClass != null)
					{
						yield return compatibleProjectile.AmmoClass;
					}
				}
			}
		}

		public ComponentBay GetBayById(int id)
		{
			foreach (ComponentBay bay in bays)
			{
				if (bay.Id == id)
				{
					return bay;
				}
			}
			return null;
		}

		public ComponentBay GetBayByName(string name)
		{
			foreach (ComponentBay bay in bays)
			{
				if (bay.name == name)
				{
					return bay;
				}
			}
			return null;
		}

		public void NotifyHyperdriveStateChange()
		{
			if (unit.ActiveUnit != null && unit.ActiveUnit.UnitRigidBody != null)
			{
				unit.ActiveUnit.UnitRigidBody.isKinematic = hyperdrive != null && hyperdrive.IsActivated;
			}
		}

		public bool CanUndock()
		{
			return IsDocked;
		}

		public void AddPassengerGroup(PassengerGroup p)
		{
			passengerGroups.Add(p);
		}

		public bool RemovePassengerGroup(PassengerGroup p)
		{
			passengerGroups.Remove(p);
			return false;
		}

		[ContextMenu("Remove all components charge")]
		public void RemoveAllComponentsCharge()
		{
			foreach (ComponentBase unitComponent in unitComponents)
			{
				unitComponent.RemoveCharge();
			}
		}

		public void SetComponentsPowered(bool powered)
		{
			foreach (ComponentBase unitComponent in unitComponents)
			{
				if (unitComponent.CanChangeUserPowered)
				{
					unitComponent.UserPowered = powered;
				}
			}
		}

		public void ValidateDuplicateComponentBays()
		{
			foreach (int item in from e in FindComponentBays()
				group e by e.Id into e
				where e.Count() > 1
				select e.Key)
			{
				Debug.LogError($"Unit {name} has bays with a duplicate bay id: {item}", this);
			}
		}

		[ContextMenu("Auto Assign Ship Name")]
		public void AutoAssignShipNameEditor()
		{
			ShipNameUsage usage = (GetComponent<Unit>().UnitClass.IsCivilian ? ShipNameUsage.Any : ShipNameUsage.Any);
			shipName = null;
			shipNameId = -1;
			ShipNames shipNames = UnityObjectHelper.FindComponent<ShipNames>();
			if (shipNames != null)
			{
				TryAutoAssignShipName(shipNames, usage);
			}
		}

		public void AutoAssignShipName()
		{
			ShipNameUsage usage = (UnitClass.IsCivilian ? (ShipNameUsage.Unspecified | ShipNameUsage.CivilianOnly) : (ShipNameUsage.Unspecified | ShipNameUsage.MilitaryOnly));
			TryAutoAssignShipName(engine.ShipNames, usage);
		}

		private void TryAutoAssignShipName(ShipNames shipNames, ShipNameUsage usage)
		{
			if (string.IsNullOrEmpty(ShipName) && shipNames != null)
			{
				ClearShipName();
				string foundName = null;
				int foundId = -1;
				if (shipNames.GetAndReserveUniqueShipName(ShipNameNeutraility.Any, usage, out foundName, out foundId))
				{
					shipNameId = foundId;
					shipName = foundName;
				}
				else
				{
					shipName = shipNames.GetNonUniqueShipName(ShipNameNeutraility.Any, usage);
					shipNameId = -1;
				}
			}
		}

		private void RemoveCrew()
		{
			pilotCache.Clear();
			for (int i = 0; i < people.Count; i++)
			{
				pilotCache.Add(people[i]);
			}
			for (int j = 0; j < pilotCache.Count; j++)
			{
				pilotCache[j].CurrentUnit = null;
			}
		}

		private void DestroyCrew()
		{
			pilotCache.Clear();
			for (int i = 0; i < people.Count; i++)
			{
				pilotCache.Add(people[i]);
			}
			for (int j = 0; j < pilotCache.Count; j++)
			{
				pilotCache[j].SafeDestroy();
			}
		}

		private void KillCrew(Faction attackingFaction, KillData killData)
		{
			pilotCache.Clear();
			for (int i = 0; i < people.Count; i++)
			{
				if (people[i] != null)
				{
					pilotCache.Add(people[i]);
					killData?.KilledPeople.Add(new KilledPerson
					{
						Faction = people[i].Faction,
						PersonId = people[i].UniqueId,
						Person = people[i]
					});
				}
			}
			for (int j = 0; j < pilotCache.Count; j++)
			{
				if (engine != null)
				{
					engine.NotifyPilotAboutToBeKilled(pilotCache[j], attackingFaction);
				}
				pilotCache[j].TryKill();
			}
		}

		public void KillDockedShips(KillData killData)
		{
			if (!(hangarComponent != null) || hangarComponent.DockedUnitCount <= 0)
			{
				return;
			}
			UnitComponentHolder[] array = hangarComponent.DockedShips.ToArray();
			foreach (UnitComponentHolder unitComponentHolder in array)
			{
				DestructableUnit destructable = unitComponentHolder.unit.Destructable;
				if (destructable != null && !destructable.IsInvulnerable && destructable.AllowDestruction)
				{
					destructable.KillNestedUnit(killData.KillerUnit, killData.KillerFaction, killData);
				}
				else
				{
					unitComponentHolder.UndockIfDocked();
				}
			}
		}

		public int UndockAllDockedUnits()
		{
			int num = 0;
			if (hangarComponent != null && hangarComponent.DockedUnitCount > 0)
			{
				UnitComponentHolder[] array = hangarComponent.DockedShips.ToArray();
				for (int i = 0; i < array.Length; i++)
				{
					array[i].DockedInHangarBay = null;
					num++;
				}
			}
			return num;
		}

		public float CalculateDetectionRange(float detectionRange)
		{
			float num = 1f;
			if (unit.UnitType == UnitType.Ship && (EngineComponent == null || !EngineComponent.UserPowered))
			{
				num -= 0.25f;
			}
			if (ShieldComponent == null || !ShieldComponent.UserPowered)
			{
				num -= 0.4f;
			}
			if (PowerGenerator == null || !PowerGenerator.UserPowered)
			{
				num -= 0.25f;
			}
			bool flag = false;
			if (CloakComponent != null)
			{
				flag = CloakComponent.State == CloakState.Cloaked;
				num *= CloakComponent.GetDetectionRangeMultiplier();
			}
			detectionRange *= num;
			if (!flag && Time.time < lastTimeFiredWeapons + 6f)
			{
				detectionRange += GameController.Instance.GameSettings.GameplaySettings.WeaponsFireSensorRangeChange;
			}
			return detectionRange;
		}

		private AudioSource GetShieldHitAudio()
		{
			AudioSource shieldHitAudioSource = unit.ActiveUnit.ActiveUnitClass.ShieldHitAudioSource;
			if (shieldHitAudioSource != null)
			{
				return shieldHitAudioSource;
			}
			return engine.DefaultShieldHitAudioSource;
		}

		private AudioSource GetShieldDownAudio()
		{
			AudioSource shieldDownAudioSource = unit.ActiveUnit.ActiveUnitClass.ShieldDownAudioSource;
			if (shieldDownAudioSource != null)
			{
				return shieldDownAudioSource;
			}
			return engine.DefaultShieldDownAudioSource;
		}

		private AudioSource GetShieldUpAudio()
		{
			AudioSource shieldUpAudioSource = unit.ActiveUnit.ActiveUnitClass.ShieldUpAudioSource;
			if (shieldUpAudioSource != null)
			{
				return shieldUpAudioSource;
			}
			return engine.DefaultShieldUpAudioSource;
		}

		internal void OnPilotChanged(Person oldPilot)
		{
			if (oldPilot != null)
			{
				if (oldPilot.NpcPilot != null && oldPilot.NpcPilot.CurrentUnit == Unit)
				{
					oldPilot.NpcPilot.CurrentUnit = null;
				}
				oldPilot.NotifyPilotStatusChanged(this);
			}
			if (pilotPerson != null)
			{
				pilotPerson.Init();
				unit.Faction = pilotPerson.Faction;
				pilotPerson.CurrentUnit = unit;
				if (pilotPerson.NpcPilot != null)
				{
					pilotPerson.NpcPilot.CurrentUnitComponents = this;
				}
				pilotPerson.NotifyPilotStatusChanged(this);
			}
			else
			{
				RemovePilotControl();
			}
			timePilotStatusChanged = Time.time;
		}

		private void SetParentToDockingBay()
		{
			gameObject.transform.SetParent(dockedInHangarBay.gameObject.transform, worldPositionStays: true);
			gameObject.transform.localPosition = Vector3.zero;
		}

		private void RemoveShipNameIndex()
		{
			if (shipNameId > -1)
			{
				if (engine != null && engine.ShipNames != null)
				{
					engine.ShipNames.UnreserveShipName(shipNameId);
				}
				shipNameId = -1;
			}
		}

		public void CompleteConstruction()
		{
			constructionState = ConstructionState.Constructed;
			constructionProgress = 1f;
			engine.NotifyUnitConstructionFinished(unit);
		}

		public void StartConstruction()
		{
			constructionState = ConstructionState.Constructing;
			constructionProgress = 0f;
			SetHealthForConstructionProgress();
		}

		public void CompleteDismantling()
		{
			EngineASX.Instance.NotifyUnitFinishingDismantling(unit);
			RefundCreditsPostDismantling();
			MovePeopleFromDismantledUnit();
			unit.SafeDestroy();
		}

		private void RefundCreditsPostDismantling()
		{
			if (unit.Faction != null)
			{
				float num = ((unit.UnitType == UnitType.Station) ? GameController.Instance.GameSettings.GameplaySettings.DismantledStationRefundPercent : GameController.Instance.GameSettings.GameplaySettings.DismantledShipRefundPercent);
				int num2 = Mathf.RoundToInt((float)unit.CalculateCurrentMoneyValue() * num);
				if (num2 > 0)
				{
					unit.Faction.ApplyTransaction(num2, FactionTransactionType.UnitDismantled, null, null, null, UnitClass);
				}
			}
		}

		public void StartDismantle()
		{
			if (constructionState != ConstructionState.Dismantling)
			{
				constructionState = ConstructionState.Dismantling;
			}
			if (!IsFullyDecloaked)
			{
				CloakComponent.StartDecloak();
			}
			MovePeopleFromDismantledUnit();
			UndockAllDockedUnits();
		}

		private void MovePeopleFromDismantledUnit()
		{
			if (unit.Faction != null && unit.Faction.FactionAI != null && unit.Faction.LeaderPerson != null && unit.Faction.LeaderPerson.CurrentUnit == unit)
			{
				unit.Faction.FactionAI.PlaceLeaderAtBestStation();
			}
			if (unit.Components.people.Count <= 0)
			{
				return;
			}
			Person[] array = unit.Components.people.ToArray();
			foreach (Person person in array)
			{
				if (person.IsLocalPlayer)
				{
					if (!EngineASX.Instance.World.TryRespawn(person))
					{
						person.TryKill();
					}
				}
				else
				{
					person.CurrentUnit = null;
				}
			}
		}

		public void SetHealthForConstructionProgress()
		{
			float healthNormalized = engine.GameSettings.ConstructionInitialHealth + (1f - engine.GameSettings.ConstructionInitialHealth) * constructionProgress;
			unit.Destructable.HealthNormalized = healthNormalized;
		}

		public void TickConstruction(float elapsedTime)
		{
			float num = elapsedTime / engine.GameSettings.ConstructionTimePerHp * UnitClass.ConstructionRateMultiplier;
			float maxHealth = unit.UnitClass.maxHealth;
			constructionProgress += num / maxHealth;
			unit.Destructable.CurrentHealth += num * (1f - engine.GameSettings.ConstructionInitialHealth);
			if (unit.Destructable.CurrentHealth > maxHealth)
			{
				unit.Destructable.CurrentHealth = maxHealth;
			}
			if (constructionProgress >= 1f)
			{
				CompleteConstruction();
			}
		}

		public void TickDismantling(float elapsedTime)
		{
			float num = elapsedTime / engine.GameSettings.DismantlingTimePerHp * UnitClass.ConstructionRateMultiplier;
			float maxHealth = unit.UnitClass.maxHealth;
			constructionProgress -= num / maxHealth;
			if (constructionProgress <= 0f)
			{
				CompleteDismantling();
			}
		}

		public void RefreshIsModded()
		{
			IsModified = CalculateIsModded();
		}

		public bool CalculateIsModded()
		{
			foreach (ComponentBay bay in bays)
			{
				if (bay.IsModded)
				{
					return true;
				}
			}
			return false;
		}

		public void CancelFiringOnAllTurrets()
		{
			foreach (TurretComponent turret in Turrets)
			{
				if (turret.CanCancelFire())
				{
					turret.CancelFiring();
				}
			}
		}

		public UnitShipTrader FindShipTrader()
		{
			UnitShipTrader component = GetComponent<UnitShipTrader>();
			if (component != null)
			{
				return component;
			}
			return UnitClass.GetComponent<UnitShipTrader>();
		}

		public void RegisterWeaponsFire()
		{
			lastTimeFiredWeapons = Time.time;
		}

		public void InitAutoTurretModuleIfNull()
		{
			if (autoTurretModule == null)
			{
				autoTurretModule = new UnitAutoTurretModule(this);
			}
		}

		public void InitPointDefenceAutoTurretModuleIfNull()
		{
			if (pointDefenceTurretModule == null)
			{
				pointDefenceTurretModule = new PointDefenceTurretModule(this);
			}
		}

		public void UpdateTurretRanges()
		{
			lowestRangedTurret = float.MaxValue;
			highestRangedTurret = float.MinValue;
			highestRangedPointDefenceTurret = float.MinValue;
			foreach (TurretComponent turret in Turrets)
			{
				UpdateTurretRangesWithTurret(turret);
			}
		}

		public void SetConventionalTurretsAutoFire(bool autoFire)
		{
			for (int i = 0; i < Turrets.Count; i++)
			{
				if (Turrets[i].CanAutoFire())
				{
					Turrets[i].AutoFire = autoFire;
				}
			}
		}

		public void UpdateTurretRangesWithTurret(TurretComponent turret)
		{
			if (turret.TurretClass.IsPointDefence)
			{
				highestRangedPointDefenceTurret = Mathf.Max(highestRangedPointDefenceTurret, turret.TurretClass.MaxFiringRange);
				return;
			}
			highestRangedTurret = Mathf.Max(highestRangedTurret, turret.TurretClass.MaxFiringRange);
			lowestRangedTurret = Mathf.Min(lowestRangedTurret, turret.TurretClass.MaxFiringRange);
		}

		private int GetConventionalTurretCount()
		{
			int num = 0;
			foreach (TurretComponent turret in Turrets)
			{
				if (turret.TurretClass.AIConventionalWeapon)
				{
					num++;
				}
			}
			return num;
		}

		public void InvalidateCombatRating()
		{
			cachedCombatRating = -1f;
		}

		[ContextMenu("Cache Combat Rating")]
		public void CacheCombatRating()
		{
			cachedCombatRating = CalculateCombatRating();
		}

		public float CalculateCombatRating()
		{
			float num = 0f;
			foreach (TurretComponent turret in Turrets)
			{
				num += CalculateCombatRatingFromTurretClass(turret.TurretClass);
			}
			return CombatRatingHelper.CalculateCombatRating(num, UnitClass.maxHealth, (ShieldComponent != null) ? ShieldComponent.ShieldClass.Capacities[0] : 0f, CalculateMaxSpeed());
		}

		public static float CalculateCombatRatingFromTurretClass(TurretClass turretClass)
		{
			return turretClass.CalculatedCombatRating;
		}

		public void UpdateEstimatedPointDefenceEffectiveness()
		{
			estimatedPointDefenceEffectiveness = CalculateEstimatedPointDefenceEffectiveness();
		}

		public float CalculateEstimatedPointDefenceEffectiveness()
		{
			float num = 0f;
			foreach (TurretComponent turret in Turrets)
			{
				if (turret.TurretClass.IsPointDefence)
				{
					float aIPointDefenseEffectiveness = turret.TurretClass.AIPointDefenseEffectiveness;
					num = num + aIPointDefenseEffectiveness - num * aIPointDefenseEffectiveness;
				}
			}
			return num;
		}
	}
}
