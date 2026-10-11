using System;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.IP.Engine.Dialog;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.GasClouds;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.Engine
{
	public class Unit : SectorObject
	{
		public delegate void KilledHandler(Unit sender, DestroyedUnitArgs args);

		public delegate void SectorChangedHandler(Unit sender, Sector oldSector);

		public string ClassName;

		private bool isValidAndNotDestroyed;

		private float initTime;

		public float Mass = 1f;

		private float detectionRadius;

		internal float LastTimeEnteredWormhole = -100f;

		internal int internalUnitIndex = -1;

		public int Seed = -1;

		public Collider CollisionCollider;

		private UnitGasCloud unitGasCloud;

		public float Radius = 1f;

		public Collider DamageCollider;

		private ActiveUnit activeUnit;

		[SerializeField]
		private Asteroid asteroid;

		[NonSerialized]
		public Cargo CargoComponent;

		private UnitComponentHolder components;

		private DestructableUnit destructable;

		[NonSerialized]
		public UnitDebris DebrisComponent;

		private EngineASX engine;

		[SerializeField]
		private Faction faction;

		public bool hasInit;

		private bool isActiveInEngine;

		private Wormhole wormholeComponent;

		private Rigidbody rBody;

		public int RpProvision;

		private TractorTurretComponent tractorer;

		[SerializeField]
		[FormerlySerializedAs("UniqueId")]
		private int uniqueId = -1;

		[SerializeField]
		private UnitClass unitClass;

		public string UnitName;

		public string UnitShortName;

		private string designation;

		private string shortDesignation;

		public bool IsDiscoverableType => UnitTypeIsDiscoverable(UnitType);

		public int UniqueId => uniqueId;

		public Faction Faction
		{
			get
			{
				return faction;
			}
			set
			{
				if (faction != value)
				{
					if (this != null && LogWrapper.LogMsgs)
					{
						LogWrapper.Log(string.Format("{0}: I'm Changing faction from {1} to {2}", this, (faction != null) ? faction.ToString() : "NULL", (value != null) ? value.ToString() : "NULL"), this, 2);
					}
					Faction oldFaction = faction;
					EngineASX.Instance.RemoveUnitFromShipsByFactionType(this);
					faction = value;
					OnFactionChanged(oldFaction);
				}
			}
		}

		public bool IsPlayerCurrentUnit
		{
			get
			{
				Unit localUnit = EngineASX.Instance.LocalUnit;
				if (localUnit != null)
				{
					return this == localUnit;
				}
				return false;
			}
		}

		public bool IsPlayerCurrentUnitOrRoot
		{
			get
			{
				Unit playerUnit = engine.PlayerUnit;
				if (playerUnit != null)
				{
					if (!(this == playerUnit))
					{
						return this == playerUnit.GetRootUnit();
					}
					return true;
				}
				return false;
			}
		}

		public bool IsSameRootUnitAsPlayer
		{
			get
			{
				Unit playerUnit = engine.PlayerUnit;
				if (playerUnit != null)
				{
					return GetRootUnit() == playerUnit.GetRootUnit();
				}
				return false;
			}
		}

		public bool IsArmed
		{
			get
			{
				if (components != null)
				{
					if (!IsUnderConstructionOrDismantling)
					{
						return components.IsArmedWithConventionalWeapons;
					}
					return false;
				}
				return unitClass.IsArmed;
			}
		}

		public UnitType UnitType => UnitClass.UnitType;

		public bool ShieldEnabled
		{
			get
			{
				if (components != null)
				{
					return components.ShieldEnabled;
				}
				return false;
			}
		}

		public NpcPilot NpcPilot
		{
			get
			{
				if (components != null && components.PilotPerson != null)
				{
					return components.PilotPerson.NpcPilot;
				}
				return null;
			}
		}

		public bool IsFullyCloaked
		{
			get
			{
				if (components != null)
				{
					return components.IsCloaked;
				}
				return false;
			}
		}

		public CloakState CloakState
		{
			get
			{
				if (components != null)
				{
					return components.CloakState;
				}
				return CloakState.Decloaked;
			}
		}

		public CloakComponent CloakComponent
		{
			get
			{
				if (components != null)
				{
					return components.CloakComponent;
				}
				return null;
			}
		}

		public bool IsFullyDecloaked
		{
			get
			{
				if (!(components == null))
				{
					return components.IsFullyDecloaked;
				}
				return true;
			}
		}

		public UnitClass UnitClass => unitClass;

		public EngineASX Engine
		{
			get
			{
				return engine;
			}
			private set
			{
				SetEngine(value);
			}
		}

		public Rigidbody RBody => rBody;

		public Asteroid Asteroid
		{
			get
			{
				return asteroid;
			}
			set
			{
				asteroid = value;
			}
		}

		public TractorTurretComponent Tractorer
		{
			get
			{
				return tractorer;
			}
			set
			{
				if (tractorer != value)
				{
					tractorer = value;
				}
			}
		}

		public bool IsActiveInEngine
		{
			get
			{
				return isActiveInEngine;
			}
			set
			{
				if (isActiveInEngine != value)
				{
					isActiveInEngine = value;
					if (isActiveInEngine)
					{
						OnUnitActive();
					}
					else
					{
						OnUnitInactive();
					}
				}
			}
		}

		public ActiveUnit ActiveUnit
		{
			get
			{
				return activeUnit;
			}
			set
			{
				activeUnit = value;
			}
		}

		public UnitComponentHolder Components
		{
			get
			{
				return components;
			}
			set
			{
				components = value;
			}
		}

		public bool IsDocked
		{
			get
			{
				if (components != null)
				{
					return components.IsDocked;
				}
				return false;
			}
		}

		public CargoBayComponent CargoBayComponent
		{
			get
			{
				if (components != null)
				{
					return components.CargoBayComponent;
				}
				return null;
			}
		}

		public override bool IsStatic
		{
			get
			{
				if (!gameObject.isStatic && UnitType != UnitType.Station && UnitType != UnitType.Wormhole)
				{
					return UnitType == UnitType.Asteroid;
				}
				return true;
			}
		}

		public bool IsOwnedByPlayer
		{
			get
			{
				Faction localFaction = EngineASX.Instance.LocalFaction;
				if (localFaction != null)
				{
					return faction == localFaction;
				}
				return false;
			}
		}

		public float CurrentSpeed
		{
			get
			{
				if (!IsStatic)
				{
					Rigidbody rigidbody = RBody;
					if (rigidbody != null)
					{
						return rigidbody.linearVelocity.magnitude;
					}
				}
				return 0f;
			}
		}

		public float SecondsSinceInit => Time.time - initTime;

		public bool IsValidAndNotDestroyed => isValidAndNotDestroyed;

		public override bool IsValid
		{
			get
			{
				if (engine != null && UniqueId > -1 && gameObject.activeSelf)
				{
					return Sector != null;
				}
				return false;
			}
		}

		public bool IsDockable
		{
			get
			{
				if (components != null && components.IsDockable)
				{
					return faction != null;
				}
				return false;
			}
		}

		public bool CanDock
		{
			get
			{
				if (IsValidAndNotDestroyed)
				{
					return IsMobile;
				}
				return false;
			}
		}

		public UnitShipTrader ShipTrader
		{
			get
			{
				if (components != null)
				{
					return components.ShipTrader;
				}
				return null;
			}
		}

		public bool HasComponentTrader => GetComponentTrader() != null;

		public bool IsUnderConstruction
		{
			get
			{
				if (components != null)
				{
					return components.IsUnderConstruction;
				}
				return false;
			}
		}

		public bool IsUnderConstructionOrDismantling
		{
			get
			{
				if (components != null)
				{
					return components.ConstructionState != ConstructionState.Constructed;
				}
				return false;
			}
		}

		public Wormhole WormholeComponent => wormholeComponent;

		public bool IsDestroyed
		{
			get
			{
				if (destructable != null)
				{
					return destructable.IsDestroyed;
				}
				return false;
			}
		}

		public DestructableUnit Destructable => destructable;

		public UnitGasCloud UnitGasCloud
		{
			get
			{
				return unitGasCloud;
			}
			set
			{
				unitGasCloud = value;
			}
		}

		public bool ShowWithOwnershipColours => UnitClass.ShowWithOwnershipColours;

		public bool IsMobile
		{
			get
			{
				if (UnitType == UnitType.Ship && unitClass.ShipType == ShipType.Normal)
				{
					return components.EngineComponent != null;
				}
				return false;
			}
		}

		public override Vector3 SectorPosition
		{
			get
			{
				if (components == null || components.DockedInHangar == null)
				{
					return transform.localPosition;
				}
				return transform.localPosition + components.DockUnit.SectorPosition;
			}
		}

		public override bool ShouldZeroPositionY
		{
			get
			{
				switch (UnitType)
				{
				case UnitType.Ship:
				case UnitType.Station:
				case UnitType.Wormhole:
				case UnitType.Asteroid:
				case UnitType.AsteroidCluster:
				case UnitType.GasCloud:
					return true;
				default:
					return false;
				}
			}
		}

		public override float ObjectRadius => Radius;

		public float CombatRating
		{
			get
			{
				if (components != null)
				{
					return components.CombatRating;
				}
				return 0f;
			}
		}

		public override Vector3 Velocity
		{
			get
			{
				if (rBody != null)
				{
					return rBody.linearVelocity;
				}
				return Vector3.zero;
			}
		}

		public bool CanHaveFaction
		{
			get
			{
				switch (unitClass.UnitType)
				{
				case UnitType.Ship:
				case UnitType.Station:
				case UnitType.Cargo:
				case UnitType.NavBuoy:
				case UnitType.Projectile:
					return true;
				default:
					return false;
				}
			}
		}

		public CargoTrader CargoTrader
		{
			get
			{
				if (components != null)
				{
					return components.CargoTrader;
				}
				return null;
			}
		}

		public event KilledHandler Killed;

		public event SectorChangedHandler SectorChanged;

		public static bool UnitTypeIsTargettable(UnitType unitType)
		{
			switch (unitType)
			{
			case UnitType.Ship:
			case UnitType.Station:
			case UnitType.Cargo:
			case UnitType.Wormhole:
			case UnitType.Asteroid:
			case UnitType.NavBuoy:
			case UnitType.Waypoint:
				return true;
			default:
				return false;
			}
		}

		public static bool UnitTypeIsDiscoverable(UnitType unitType)
		{
			switch (unitType)
			{
			case UnitType.Ship:
			case UnitType.Station:
			case UnitType.Cargo:
			case UnitType.Wormhole:
			case UnitType.Asteroid:
			case UnitType.NavBuoy:
				return true;
			default:
				return false;
			}
		}

		public void SetUniqueId(int value)
		{
			uniqueId = value;
		}

		public void SetFactionOnly(Faction newFaction)
		{
			faction = newFaction;
		}

		public void TransformForUndockFrom(Unit playerRootUnit)
		{
			transform.localPosition = playerRootUnit.GetSafeUndockSectorPosition(this);
			transform.localRotation = playerRootUnit.GetUndockRotation(this);
		}

		internal void SetEngine(EngineASX value)
		{
			if (!(engine != value))
			{
				return;
			}
			EngineASX engineASX = engine;
			engine = value;
			if (engineASX != null)
			{
				engineASX.MissileLockController.RemoveMissileLocks(this);
				engineASX.DeregisterUnit(this);
			}
			if (engine != null)
			{
				if (UniqueId < 0)
				{
					SetUniqueId(engine.GetUniqueUnitId());
				}
				RefreshIsValidAndNotDestroyed();
				engine.RegisterUnit(this);
			}
			else
			{
				if (UniqueId >= 0)
				{
					SetUniqueId(-1);
				}
				RefreshIsValidAndNotDestroyed();
			}
		}

		public bool OverlapsSectorPosition(Vector3 sectorPosition, float radiusMultiplier)
		{
			return Vector3.Distance(SectorPosition, sectorPosition) < UnitClass.ShieldRingRadius * radiusMultiplier;
		}

		internal void RaiseKilled(DestroyedUnitArgs args)
		{
			if (Killed != null)
			{
				Killed(this, args);
			}
		}

		public static float GetYBearing(Vector3 p1, Vector3 p2)
		{
			Vector3 vector = p2 - p1;
			return Mathf.Atan2(vector.x, vector.z) * 57.29578f;
		}

		public static float GetYBearing(Vector3 movement)
		{
			return Mathf.Atan2(movement.x, movement.z) * 57.29578f;
		}

		public float GetLocalYBearingToWorldPosition(Vector3 position)
		{
			return Geometry.WrapDegreesForBearing(GetYBearing(transform.position, position) - transform.eulerAngles.y);
		}

		public static Unit GetRoot(Unit unit)
		{
			if (unit != null)
			{
				return unit.GetRootUnit();
			}
			return null;
		}

		public override string ToString()
		{
			if (this != null)
			{
				return $"[Unit \"{UniqueId}\" ID:{name}]";
			}
			return "Unit";
		}

		public override float GetSpeed()
		{
			return CurrentSpeed;
		}

		[ContextMenu("Initialise")]
		public void InitAndFindParents()
		{
			Init();
		}

		public void Init(bool autoFindParents = true)
		{
			if (!hasInit)
			{
				hasInit = true;
				if (Seed < 0)
				{
					Seed = UnityEngine.Random.Range(0, int.MaxValue);
				}
				Engine = EngineASX.Instance;
				if (CollisionCollider != null)
				{
					SetCollisionColliderEnabled(enabled: false);
				}
				if (TryGetComponent<DestructableUnit>(out var component))
				{
					destructable = component;
					destructable.Init(this);
				}
				if (TryGetComponent<Wormhole>(out var component2))
				{
					wormholeComponent = component2;
					wormholeComponent.Init(this);
				}
				if (TryGetComponent<UnitComponentHolder>(out var component3))
				{
					components = component3;
					components.Init(this);
				}
				if (TryGetComponent<Cargo>(out var component4))
				{
					CargoComponent = component4;
					CargoComponent.Init(this);
				}
				if (TryGetComponent<AsteroidCluster>(out var component5))
				{
					component5.Init(this);
				}
				if (asteroid != null)
				{
					asteroid.Init(this);
				}
				ApplyUnitClass();
				if (autoFindParents)
				{
					FindParents();
				}
				if (faction != null)
				{
					OnFactionChanged(null);
				}
				UpdateDetectionRadius();
				engine.OnUnitInitialised(this);
				initTime = Time.time;
			}
		}

		private void OnEnable()
		{
			if (engine != null)
			{
				UpdateIsActive();
			}
		}

		public void CleanupUnit()
		{
			if (engine != null)
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"{this}: Unit Cleanup", this, 2);
				}
				DestroyActiveUnit();
				if (components != null)
				{
					components.CleanupUnit();
				}
				SetSector(null, updateGameObjectParent: false);
				Faction = null;
				Engine = null;
			}
		}

		public int GetShieldIndex(Vector3 worldPosition)
		{
			return Mathf.Clamp((int)(Maths.WrapValue(GetYBearing(transform.InverseTransformPoint(worldPosition)) + 30f, 0f, 360f) / 60f), 0, 5);
		}

		public void GenerateLoot()
		{
			if (!(unitClass != null))
			{
				return;
			}
			if (engine.World.GenerateUnitLoot)
			{
				UnitLootData component = GetComponent<UnitLootData>();
				if (component != null)
				{
					for (int i = 0; i < component.LootItems.Count; i++)
					{
						UnitLootItem unitLootItem = component.LootItems[i];
						if (unitLootItem != null && UnityEngine.Random.value < unitLootItem.SpawnProbability)
						{
							int num = UnityEngine.Random.Range(unitLootItem.MinQuantity, unitLootItem.MaxQuantity + 1);
							if (num > 0)
							{
								CreateLootItem(unitLootItem.CargoClass, num);
							}
						}
					}
				}
				else
				{
					GameSettings gameSettings = engine.GameSettings;
					for (int j = 0; j < gameSettings.LootableCargoClasses.Length; j++)
					{
						CargoClass cargoClass = gameSettings.LootableCargoClasses[j];
						int num2 = Mathf.CeilToInt((float)unitClass.SaleCost / gameSettings.LootableCargoShipValueDivisors[j]);
						if (num2 < gameSettings.LootableCargoMinValues[j])
						{
							num2 = gameSettings.LootableCargoMinValues[j];
						}
						int num3 = Mathf.CeilToInt((float)num2 * gameSettings.LootableCargoMaxMultiplier);
						if (num3 > gameSettings.LootableCargoMaxValues[j])
						{
							num3 = gameSettings.LootableCargoMaxValues[j];
						}
						int quantity = UnityEngine.Random.Range(num2, num3 + 1);
						CreateLootItem(cargoClass, quantity);
					}
				}
			}
			if (engine.GameSettings.DestroyedUnitsCreateDebris && (UnitType == UnitType.Ship || UnitType == UnitType.Station))
			{
				unitClass.CreateDebris(engine, Sector, SectorPosition);
			}
		}

		public Cargo CreateLootItem(CargoClass cargoClass, int quantity)
		{
			if (Sector == null)
			{
				Debug.LogError("Attempting to create cargo item with null scene", this);
			}
			float num = UnityEngine.Random.Range(unitClass.ShieldRingRadius * 0.2f, unitClass.ShieldRingRadius * 0.4f);
			Vector3 vector = Maths.RandomXZDirection();
			Vector3 sectorPosition = SectorPosition + vector * num;
			Cargo cargo = CreateCargoItem(cargoClass, quantity, sectorPosition);
			GameObject gameObject = cargo.gameObject;
			if (IsActiveInEngine && gameObject.GetComponent<Rigidbody>() != null)
			{
				ApplyCargoForces(vector, gameObject.GetComponent<Rigidbody>());
			}
			return cargo;
		}

		public Cargo CreateCargoItem(CargoClass cargoClass, int quantity, Vector3 sectorPosition)
		{
			return CreateCargoItem(cargoClass, engine.CargoContainerPrefab, quantity, Sector, sectorPosition);
		}

		public static Cargo CreateCargoItem(CargoClass cargoClass, Cargo prefab, int quantity, Sector sector, Vector3 sectorPosition)
		{
			if (cargoClass == null)
			{
				Debug.LogError("Cannot create cargo item. Null cargo class");
				return null;
			}
			Cargo cargo = UnityObjectHelper.InstantiateAndGetComponent(prefab, sector.transform);
			cargo.transform.SetParent(sector.transform, worldPositionStays: true);
			cargo.transform.localPosition = sectorPosition;
			cargo.transform.localRotation = UnityEngine.Random.rotation;
			cargo.GetComponent<Unit>().Init(autoFindParents: false);
			cargo.Expires = true;
			cargo.CargoClass = cargoClass;
			cargo.SetSpawnTime();
			cargo.Unit.Sector = sector;
			cargo.Quantity = quantity;
			cargo.SetHealthBasedOnVolume();
			return cargo;
		}

		public void AwardPlayerWithKilledUnitXp(Faction attackerFaction)
		{
			if (engine.GameSettings.PlayerXpEnabled && unitClass.XpValue > 0 && attackerFaction != null && attackerFaction.IsPlayerFaction && faction != null && faction.IsHostileTo(attackerFaction))
			{
				engine.LocalPlayer.AddXp(unitClass.XpValue);
			}
		}

		public void UpdateRigidBodyOnDeath()
		{
			Rigidbody component = GetComponent<Rigidbody>();
			if (component != null)
			{
				component.freezeRotation = false;
				component.constraints = RigidbodyConstraints.FreezePositionY;
				component.angularDamping = engine.GameSettings.UnitDefaultAngularDrag;
				_ = Quaternion.Euler(0f, UnityEngine.Random.value * 360f, 0f) * Vector3.forward;
				float y = UnityEngine.Random.Range(engine.GameSettings.UnitDesctructionMinTorqueMultiplier, 1f);
				Vector3 vector = new Vector3(0f, y, 0f);
				component.AddTorque(vector * component.mass * engine.GameSettings.UnitDesctructionTorqueMultiplier, ForceMode.Impulse);
			}
		}

		/// <summary>A unit has left the world.</summary>
		/// <remarks>
		/// The mirror of EngineASX.UnitCreated, and from the same place in the
		/// same spirit: the host tells clients everything, and the one place
		/// a unit goes away is here rather than in a list of places.
		///
		/// Raised before the object is destroyed, so a listener can still see
		/// what it was.
		/// </remarks>
		public event System.Action<Unit> UnitGone;

		public void SafeDestroy()
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: DestroyGameObject", this, 2);
			}

			UnitGone?.Invoke(this);
			CleanupUnit();
			UnityEngine.Object.Destroy(gameObject);
		}

		public bool IsHostileTo(Unit otherUnit)
		{
			if (otherUnit != null)
			{
				return IsHostileTo(otherUnit.faction);
			}
			return false;
		}

		public bool IsHostileToTwoWay(Unit otherUnit)
		{
			if (otherUnit != null)
			{
				if (!IsHostileTo(otherUnit.faction))
				{
					return otherUnit.IsHostileTo(this);
				}
				return true;
			}
			return false;
		}

		public bool IsHostileTo(Faction otherFaction)
		{
			if (faction != null && otherFaction != null)
			{
				return faction.IsHostileTo(otherFaction);
			}
			return false;
		}

		public bool IsHostileToTwoWay(Faction otherFaction)
		{
			if (otherFaction == faction)
			{
				return false;
			}
			if (faction != null && otherFaction != null)
			{
				if (!faction.IsHostileTo(otherFaction))
				{
					return otherFaction.IsHostileTo(faction);
				}
				return true;
			}
			return false;
		}

		public bool IsHostileToOrAlwaysHostileToTwoWay(Faction otherFaction)
		{
			if (otherFaction == faction)
			{
				return false;
			}
			if (faction != null && otherFaction != null)
			{
				if (!faction.IsHostileToOrAlwaysHostileTo(otherFaction))
				{
					return otherFaction.IsHostileToOrAlwaysHostileTo(faction);
				}
				return true;
			}
			return false;
		}

		private bool CalculateIsValidAndNotDestroyed()
		{
			if (IsValid)
			{
				return !IsDestroyed;
			}
			return false;
		}

		internal void RefreshIsValidAndNotDestroyed()
		{
			isValidAndNotDestroyed = CalculateIsValidAndNotDestroyed();
		}

		public bool IsTargettable(Faction sourceFaction)
		{
			if (IsValidAndNotDestroyed && !IsDocked)
			{
				if (IsFullyCloaked && !GameController.Instance.GameSettings.GameplaySettings.CanTargetCloakedUnits)
				{
					if (faction != null)
					{
						if (!(faction == sourceFaction))
						{
							return faction.IsAlliedTo(sourceFaction);
						}
						return true;
					}
					return false;
				}
				return true;
			}
			return false;
		}

		public bool IsTargettableQuick()
		{
			if (isValidAndNotDestroyed && !IsDocked)
			{
				if (IsFullyCloaked)
				{
					return GameController.Instance.GameSettings.GameplaySettings.CanTargetCloakedUnits;
				}
				return true;
			}
			return false;
		}

		public bool IsCollisionDamageApplied(Unit otherUnit, float forceOfImpact)
		{
			Faction faction = otherUnit.Faction;
			if (faction == null || (Faction != null && Faction.IsPlayerFaction) || faction.IsPlayerFaction || engine.GameSettings.AICollisionDamageEnabled)
			{
				return forceOfImpact > engine.GameSettings.UnitCollisionDamageForceThreshold;
			}
			return false;
		}

		public void EnterWormhole(Wormhole wormhole, float? distanceFromWormholeMultiplier = null)
		{
			Sector actualTargetSector = wormhole.ActualTargetSector;
			if (actualTargetSector != null)
			{
				Vector3 vector = ((!distanceFromWormholeMultiplier.HasValue) ? wormhole.GetTargetSectorPosition() : wormhole.GetSafeTargetSectorPositionWithExitDistanceMultiplier(distanceFromWormholeMultiplier.Value));
				Quaternion targetRotation = wormhole.GetTargetRotation();
				PlayerWormholeEntryAudio();
				if (faction != null)
				{
					FactionIntel intel = faction.Intel;
					if (intel != null)
					{
						intel.EnterWormhole(wormhole);
						intel.DiscoverUnit(wormhole.Unit);
						intel.DiscoverSector(actualTargetSector);
					}
				}
				EngineASX.Instance.RecentWormholeEntries.RegisterWormholeEntry(this, wormhole);
				if (components != null && components.TractorTurret != null && components.TractorTurret.TractorTarget != null)
				{
					if (components.TractorTurret.IsPullingUnit)
					{
						if (components.TractorTurret.CanPullUnitThroughWormhole(components.TractorTurret.TractorTarget))
						{
							Unit tractorTarget = components.TractorTurret.TractorTarget;
							tractorTarget.Sector = actualTargetSector;
							Vector3 value = components.TractorTurret.transform.InverseTransformPoint(tractorTarget.transform.position);
							float num = Mathf.Max(components.TractorTurret.LaserTurretClass.MaxFiringRange, Engine.GameSettings.TractorBeamSettings.TractorBreakDistance) * 0.5f;
							value = Vector3.Normalize(value) * num;
							tractorTarget.transform.localPosition = vector + targetRotation * value;
							tractorTarget.RemoveForcesAndControlInputs();
						}
						else
						{
							components.TractorTurret.StopPullingUnit();
						}
					}
					else
					{
						components.TractorTurret.TractorTarget = null;
					}
				}
				Sector = actualTargetSector;
				transform.localPosition = vector;
				transform.localRotation = targetRotation;
				RemoveForcesAndControlInputs();
				LastTimeEnteredWormhole = Time.time;
				if (components != null)
				{
					components.lastKnownVelocity = Vector3.zero;
					components.NextScanTime = Mathf.Max(components.NextScanTime, Time.time + GameController.Instance.GameSettings.GameplaySettings.WormholeEntryScanCooldownTime);
				}
				PlayerWormholeExitAudio();
				OnMoved();
			}
			else
			{
				Debug.LogWarning($"{this} attempted to enter gate with null target scene", wormhole);
			}
		}

		private void PlayerWormholeEntryAudio()
		{
			if (!IsPlayerCurrentUnit && IsActiveInEngine && activeUnit != null && activeUnit.LastDistanceFromCamera < 1000f)
			{
				engine.PlayPooledAudioSource(engine.JumpGateEnterAudioSourcePrefab, transform.position);
			}
		}

		private void PlayerWormholeExitAudio()
		{
			if (!IsPlayerCurrentUnit && IsActiveInEngine && activeUnit != null && activeUnit.LastDistanceFromCamera < 1000f)
			{
				engine.PlayPooledAudioSource(engine.JumpGateExitAudioSourcePrefab, transform.position);
			}
		}

		public void RemoveForcesAndControlInputs()
		{
			if (components != null)
			{
				components.EngineThrottle = 0f;
			}
			if (activeUnit != null && activeUnit.ActiveUnitShip != null)
			{
				activeUnit.ActiveUnitShip.RemoveForces();
			}
			if (rBody != null)
			{
				Rigidbody rigidbody = rBody;
				Vector3 linearVelocity = (rBody.angularVelocity = Vector3.zero);
				rigidbody.linearVelocity = linearVelocity;
			}
		}

		[ContextMenu("Find Parents")]
		public void FindParents()
		{
			if (components != null)
			{
				components.FindParents();
			}
			else
			{
				FindSectorFromParents();
			}
		}

		public void FindSectorFromParents()
		{
			Sector = UnityObjectHelper.FindInParentsOrSelf<Sector>(gameObject);
		}

		public void SetRBody()
		{
			rBody = GetComponent<Rigidbody>();
		}

		public int CalculateCurrentMoneyValue(bool includeCargo = true, bool includeComponents = true)
		{
			int baseCost = unitClass.BaseCost;
			baseCost += unitClass.CalculateDefaultHullValue(EngineASX.Instance.EconomySettings);
			if (destructable != null)
			{
				baseCost -= destructable.HullRepairCost;
			}
			if (components != null)
			{
				if (includeComponents)
				{
					baseCost += components.GetCurrentComponentsMoneyValue(ignoreDamage: false);
				}
				if (includeCargo && components.CargoBayComponent != null)
				{
					baseCost += components.CargoBayComponent.CalculateCargoValue();
				}
			}
			if (baseCost < 0)
			{
				return 0;
			}
			return baseCost;
		}

		public int CalculateCurrentHullValue(EngineEconomySettings economySettings)
		{
			return unitClass.CalculateHullValue(destructable.CurrentHealth, economySettings);
		}

		public void UpdateIsActive()
		{
			IsActiveInEngine = CalculateShouldBeActive();
		}

		public bool CalculateShouldBeActive()
		{
			if (engine != null && Sector != null && !IsDocked)
			{
				return Sector == engine.ActiveSector;
			}
			return false;
		}

		[ContextMenu("AutoName GameObject")]
		public void AutoNameGameObject()
		{
			gameObject.name = GetEditorName();
		}

		public string GetEditorName()
		{
			switch (unitClass.UnitType)
			{
			case UnitType.Projectile:
				return gameObject.name;
			case UnitType.Wormhole:
				return GetComponent<Wormhole>().GetEditorName();
			case UnitType.Cargo:
				return GetComponent<Cargo>().GetEditorName();
			case UnitType.Ship:
			case UnitType.Station:
				return GetComponent<UnitComponentHolder>().GetEditorName(this);
			default:
			{
				string arg = ((Faction != null) ? Faction.GetShortNameElseLong() : "No-faction");
				string arg2 = ((UnitClass != null) ? $"{UnitClass.UnitType}_{ClassName}" : $"{UnitClass.UnitType}_No_class");
				return $"{arg2}_{UniqueId}_{arg}";
			}
			}
		}

		public void ResetMass()
		{
			if (UnitType != UnitType.Projectile && rBody != null)
			{
				rBody.mass = Mass;
			}
		}

		public float GetMass()
		{
			if (rBody != null)
			{
				return rBody.mass;
			}
			return 0f;
		}

		public bool IsPlayerOrAllied()
		{
			GamePlayer localPlayer = Engine.LocalPlayer;
			if (localPlayer != null)
			{
				if (faction != null && localPlayer.Faction != null && (faction == localPlayer.Faction || faction.IsAlliedTo(localPlayer.Faction)))
				{
					return true;
				}
				return this == localPlayer.Person.CurrentUnit;
			}
			return false;
		}

		public bool HasMissileLock()
		{
			return engine.MissileLockController.HasMissileLock(this);
		}

		public Missile GetNearestMissileLock(out float nearestDist)
		{
			return engine.MissileLockController.GetNearestMissileLock(this, out nearestDist);
		}

		public Missile GetNearestNonDisruptedMissileLock(out float nearestDist)
		{
			return engine.MissileLockController.GetNearestNonDisruptedMissileLock(this, out nearestDist);
		}

		public UnitComponentTrader GetComponentTrader()
		{
			return GetComponent<UnitComponentTrader>();
		}

		public bool IsRootUnit()
		{
			return GetRootUnit() == this;
		}

		public Unit GetRootUnit()
		{
			if (components != null && components.DockUnit != null)
			{
				return components.DockUnit.GetRootUnit();
			}
			return this;
		}

		public bool IsScannedByPlayer()
		{
			if (engine.Hud != null)
			{
				return engine.Hud.AutoScanner.IsUnitInScanCache(this);
			}
			return false;
		}

		public int GetJumpDistTo(Unit unit)
		{
			return GetJumpDistTo(unit.Sector);
		}

		public int GetJumpDistTo(Sector scene)
		{
			return Sector.CalculateSectorJumpDistance(scene);
		}

		protected override void OnUpdateParent()
		{
			if (components != null)
			{
				components.UpdateParent();
			}
			else
			{
				base.OnUpdateParent();
			}
		}

		protected override void OnSectorChanged(Sector oldSector)
		{
			base.OnSectorChanged(oldSector);
			RefreshIsValidAndNotDestroyed();
			IsActiveInEngine = false;
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: Changed scene from \"{oldSector}\" to \"{Sector}\"", this, 3);
			}
			if (components != null)
			{
				components.OnUnitChangedScene(oldSector);
			}
			if (oldSector != null)
			{
				oldSector.RemoveUnit(this);
			}
			if (Sector != null)
			{
				Sector.AddUnit(this);
			}
			if (faction != null && IsDiscoverableType)
			{
				faction.Intel.DiscoverUnit(this);
			}
			ChangeGasCloud(null);
			UpdateIsActive();
			UnitType unitType = UnitType;
			if (((uint)(unitType - 1) <= 1u || unitType == UnitType.Cargo) && IsOwnedByPlayer)
			{
				EngineASX.Instance.RaisePlayerPropertyChangedSector(this, oldSector, Sector);
			}
			if (SectorChanged != null)
			{
				SectorChanged(this, oldSector);
			}
		}

		private void Start()
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: Hello, I'm now active", this, 2);
			}
		}

		public bool IsDestroyedUnitReadyToCleanup()
		{
			if (!(activeUnit == null) && (bool)activeUnit.DestructionAnimGameObject)
			{
				return !activeUnit.DestructionAnimGameObject.IsPlaying;
			}
			return true;
		}

		public void ApplyUnitClass()
		{
			if (destructable != null && destructable.CurrentHealth < 0f)
			{
				destructable.RestoreHealth();
			}
			ResetMass();
		}

		private void OnFactionChanged(Faction oldFaction)
		{
			UnitType unitType = UnitType;
			if ((uint)(unitType - 1) <= 1u || unitType == UnitType.Cargo || unitType == UnitType.NavBuoy)
			{
				if (oldFaction != null)
				{
					oldFaction.RemoveUnit(this);
				}
				if (faction != null)
				{
					faction.AddUnit(this);
					EngineASX.Instance.AddUnitToShipsByFactionType(this);
				}
			}
			unitType = UnitType;
			if (((uint)(unitType - 1) <= 1u || unitType == UnitType.Cargo) && Sector != null && (IsOwnedByPlayer || (oldFaction != null && oldFaction.IsPlayerFaction)))
			{
				if (IsOwnedByPlayer)
				{
					EngineASX.Instance.RaisePlayerPropertyChangedSector(this, null, Sector);
				}
				else
				{
					EngineASX.Instance.RaisePlayerPropertyChangedSector(this, Sector, null);
				}
			}
		}

		public AudioSource GetHullHitAudio()
		{
			AudioSource hullHitAudioSource = activeUnit.ActiveUnitClass.HullHitAudioSource;
			if (hullHitAudioSource != null)
			{
				return hullHitAudioSource;
			}
			return engine.DefaultHullHitAudioSource;
		}

		private void ApplyCargoForces(Vector3 spawnDirection, Rigidbody rb)
		{
			GameSettings gameSettings = Engine.GameSettings;
			float num = UnityEngine.Random.Range(gameSettings.LootContainerMinAngularVelocity, gameSettings.LootContainerMaxAngularVelocity);
			rb.angularVelocity = UnityEngine.Random.rotationUniform * Vector3.forward * UnityEngine.Random.value * num;
			float num2 = 60f;
			Geometry.RotateVectorRandomlyXY(ref spawnDirection, 0f - num2, num2);
			float num3 = UnityEngine.Random.Range(gameSettings.LootSpawnMinForce, gameSettings.LootSpawnMaxForce);
			rb.AddForce(num3 * spawnDirection, ForceMode.Force);
		}

		private void OnCollisionEnter(Collision collisionInfo)
		{
			if (activeUnit != null && activeUnit.DestructionAnimGameObject != null && activeUnit.DestructionAnimGameObject.IsPlaying)
			{
				activeUnit.DestructionAnimGameObject.CreateFinalExplosion();
				return;
			}
			TryRaiseNpcCrashDialogEvent(collisionInfo);
			if (engine.GameSettings.CollisionDamageEnabled)
			{
				ApplyCollisionDamage(collisionInfo);
			}
		}

		private void TryRaiseNpcCrashDialogEvent(Collision collisionInfo)
		{
			if (!(components != null) || !(components.PilotPerson != null) || components.PilotPerson.IsLocalPlayer || !(components.PilotPerson.Faction != null) || !(activeUnit != null) || IsFullyCloaked || !(activeUnit.LastDistanceFromCamera < 2000f))
			{
				return;
			}
			Unit component = collisionInfo.gameObject.GetComponent<Unit>();
			if (!(component != null) || components.PilotPerson.Faction.IsHostileTo(component) || !(component != null) || !(component.components != null) || !(component.components.PilotPerson != null) || !(component.components.PilotPerson.Faction != null) || !(component.GetFleet() != this.GetFleet()) || !(collisionInfo.impulse.magnitude > GameController.Instance.GameSettings.DialogEventSettings.CollisionEventMinImpulseMagnitude))
			{
				return;
			}
			Vector3 value = transform.InverseTransformPoint(component.transform.position);
			if (Vector3.Dot(Vector3.forward, Vector3.Normalize(value)) < GameController.Instance.GameSettings.DialogEventSettings.CollisionEventMaxDotVector)
			{
				if (component.components.PilotPerson.Faction == components.PilotPerson.Faction && component.components.PilotPerson.OutRanks(components.PilotPerson))
				{
					string title = component.components.PilotPerson.Title;
					DialogRequestArguments value2 = DialogRequestArguments.Init("TargetPilotTitle", title);
					components.PilotPerson.RaiseDialogEventRandomly(EngineASX.Instance.DialogEvents.NpcCrashedIntoSuperior, UnityEngine.Random.Range(0.2f, 0.5f), value2);
				}
				else if (component.components.PilotPerson.Faction != components.PilotPerson.Faction)
				{
					components.PilotPerson.RaiseDialogEventRandomly(EngineASX.Instance.DialogEvents.CrashedIntoNpcShip, UnityEngine.Random.Range(0.2f, 0.5f));
				}
			}
		}

		private void ApplyCollisionDamage(Collision collisionInfo)
		{
			float magnitude = collisionInfo.relativeVelocity.magnitude;
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: OnCollisionEnter. ForceOfImpact: {magnitude}", this, 2);
			}
			Unit component = collisionInfo.gameObject.GetComponent<Unit>();
			Faction sourceFaction = ((component != null) ? component.Faction : null);
			if (component != null && IsCollisionDamageApplied(component, magnitude))
			{
				ApplyCollisionDamage(magnitude, component, sourceFaction);
			}
			if (components != null)
			{
				components.NotifyCollisionDamage(magnitude);
			}
		}

		private void ApplyCollisionDamage(float forceOfImpact, Unit sourceUnit, Faction sourceFaction)
		{
		}

		public string GetActiveUnitResourceName()
		{
			return unitClass.ActiveUnitClassName;
		}

		public void ReloadActiveUnit()
		{
			OnUnitActive();
		}

		private void OnUnitActive()
		{
			DestroyActiveUnit();
			CreateActiveUnit();
			if (CollisionCollider != null && unitClass.DisplayData != null && !UnitClass.DisplayData.NormalCollisionDisabled && GameController.Instance.GameSettings.DebugSettings.CollisionEnabled && !GameController.Instance.GameSettings.PerformanceSettings.DisableCollidersInActiveSector)
			{
				SetCollisionColliderEnabled(enabled: true);
			}
			if (unitClass.IsPilottable)
			{
				Vector3 position = transform.position;
				position.y = 0f;
				transform.position = position;
			}
			if (components != null)
			{
				components.OnUnitActive();
			}
			if (NpcPilot != null)
			{
				NpcPilot.NotifyUnitActiveChanged(active: true);
			}
		}

		private void CreateActiveUnit()
		{
			string activeUnitResourceName = GetActiveUnitResourceName();
			if (string.IsNullOrEmpty(activeUnitResourceName))
			{
				return;
			}
			ActiveUnit activeUnit = GameController.Instance.ActiveUnitPrefabCache.GetActiveUnit(engine, activeUnitResourceName);
			if (activeUnit != null)
			{
				ActiveUnit activeUnit2 = UnityEngine.Object.Instantiate(activeUnit, transform);
				activeUnit2.transform.localPosition = Vector3.zero;
				activeUnit2.transform.localRotation = Quaternion.identity;
				if (activeUnit2.TryGetComponent<ActiveUnit>(out var component))
				{
					component.Unit = this;
					component.Init();
					if (UnitType == UnitType.Planet)
					{
						ActiveUnitMoon component2 = component.GetComponent<ActiveUnitMoon>();
						if (component2 != null)
						{
							component2.Init();
						}
					}
					if (component.ActiveUnitClass.RigidBodyCreate)
					{
						CreateRigidbodyIfNull();
						rBody.mass = component.Unit.Mass;
						rBody.linearDamping = component.Unit.unitClass.Drag;
						rBody.angularDamping = component.ActiveUnitClass.RigidBodyAngularDrag;
						rBody.useGravity = false;
						if (components != null && components.lastKnownVelocity != Vector3.zero)
						{
							rBody.linearVelocity = components.lastKnownVelocity;
						}
						if (component.ActiveUnitClass.RigidBodyApplyConstraints)
						{
							if (component.ActiveUnitClass.RigidBodyFreezeY)
							{
								rBody.constraints = (RigidbodyConstraints)116;
							}
							else
							{
								rBody.constraints = RigidbodyConstraints.FreezeRotation;
							}
						}
					}
				}
				else
				{
					Debug.LogError($"Active unit \"{unitClass.ActiveUnitClassName}\" does not have an ActiveUnit component", this);
				}
				ActiveUnitComponents component3 = activeUnit2.GetComponent<ActiveUnitComponents>();
				if (component3 != null)
				{
					component3.UnitComponents = components;
				}
			}
			else
			{
				Debug.LogError("Cannot load active unit \"" + activeUnitResourceName + "\"", this);
			}
		}

		private void CreateRigidbodyIfNull()
		{
			if (rBody == null)
			{
				if (TryGetComponent<Rigidbody>(out var component))
				{
					rBody = component;
				}
				else
				{
					rBody = gameObject.AddComponent<Rigidbody>();
				}
			}
		}

		private void OnUnitInactive()
		{
			if (rBody != null && activeUnit != null && activeUnit.ActiveUnitClass.RigidBodyCreate)
			{
				UnityEngine.Object.DestroyImmediate(rBody);
				rBody = null;
			}
			if (components != null)
			{
				components.OnUnitInactive();
			}
			if (activeUnit != null && activeUnit.ActiveUnitShip != null)
			{
				activeUnit.ActiveUnitShip.OnUnitInactive();
			}
			DestroyActiveUnit();
			if (NpcPilot != null)
			{
				NpcPilot.NotifyUnitActiveChanged(active: false);
			}
			if (CollisionCollider != null)
			{
				SetCollisionColliderEnabled(enabled: false);
			}
		}

		private void DestroyActiveUnit()
		{
			if (activeUnit != null)
			{
				activeUnit.SafeDestroy();
				activeUnit = null;
			}
		}

		public bool IsAttackingFleet(Fleet fleet)
		{
			return engine.FleetRecentAttacksLog.FleetRecentlyAttackedBy(this, fleet);
		}

		public bool IsAttackingFaction(Faction targetFaction)
		{
			if (faction != null)
			{
				return targetFaction.GetRecentDamageFrom(faction) > 0f;
			}
			return false;
		}

		public bool IsAttackingUnit(Unit unit)
		{
			return engine.UnitRecentAttacksLogsByInflictor.IsUnitAttackingUnit(this, unit);
		}

		public void PlayEjectCargoAudioSource()
		{
			if (Engine.EjectCargoAudioSourcePrefab != null)
			{
				AudioSource audioSource = UnityObjectHelper.InstantiateAndGetComponent(Engine.EjectCargoAudioSourcePrefab);
				audioSource.transform.position = transform.position;
				audioSource.Play();
			}
		}

		public bool IsUnderAttack()
		{
			return engine.UnitRecentAttacksLog.IsUnderAttack(this);
		}

		public void ChangeGasCloud(UnitGasCloud newUnitGasCloud)
		{
			if (unitGasCloud != newUnitGasCloud)
			{
				unitGasCloud = newUnitGasCloud;
			}
			UpdateDetectionRadius();
		}

		public void UpdateDetectionRadius()
		{
			detectionRadius = CalculateDetectionRange();
		}

		public float CalculateDetectionRange()
		{
			float num = unitClass.DetectionRange;
			if (unitGasCloud != null)
			{
				num *= unitGasCloud.GetDetectionRangeMultiplier();
			}
			if (components != null)
			{
				num = components.CalculateDetectionRange(num);
			}
			return num;
		}

		public float GetDetectionRange()
		{
			return detectionRadius;
		}

		public void UpdateGasCloud()
		{
			ChangeGasCloud(GasCloudHelper.GetGasCloudAtWorldPosition(Sector, transform.position));
		}

		public void SetCollisionColliderEnabled(bool enabled)
		{
			if (CollisionCollider != null)
			{
				CollisionCollider.isTrigger = !enabled;
			}
		}

		public bool GetCollisionColliderEnabled()
		{
			return !CollisionCollider.isTrigger;
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
			UnitDesignationBuilder.CalculateUnitDesignation(Seed, out designation, out shortDesignation);
		}

		public override bool ShouldTreatAtStatic()
		{
			if (UnitType != UnitType.Ship)
			{
				return true;
			}
			return base.ShouldTreatAtStatic();
		}

		public void OnMoved()
		{
			UpdateGasCloudIfNecessary();
			if (components != null)
			{
				components.lastKnownVelocity = Vector3.zero;
			}
		}

		public void UpdateGasCloudIfNecessary()
		{
			if (UnitTypeIsDiscoverable(UnitType))
			{
				UpdateGasCloud();
			}
		}

		public string GetClassAndSeriesName(bool shortName = false)
		{
			string text = GetClassAndSeriesNameInternal(shortName);
			if (Components != null && Components.IsModified)
			{
				text += "+";
			}
			return text;
		}

		private string GetClassAndSeriesNameInternal(bool shortName)
		{
			string className = GetClassName(shortName);
			if (UnitType == UnitType.Ship)
			{
				UnitSeries unitSeries = unitClass.UnitSeries;
				if (unitSeries != null)
				{
					if (!string.IsNullOrEmpty(className))
					{
						if (shortName && !string.IsNullOrWhiteSpace(unitSeries.ShortName))
						{
							return $"{unitSeries.ShortName}-{className}";
						}
						return $"{unitSeries.Name}-{className}";
					}
					if (!shortName || string.IsNullOrWhiteSpace(unitSeries.ShortName))
					{
						return unitSeries.Name;
					}
					return unitSeries.ShortName;
				}
			}
			return className;
		}

		public string GetClassName(bool shortName = false)
		{
			if (shortName && !string.IsNullOrEmpty(UnitClass.ShortClassName))
			{
				return UnitClass.ShortClassName;
			}
			return ClassName;
		}

		public void TryPlayCargoDoorAudio()
		{
			if (IsInActiveSector)
			{
				Unit rootUnit = GetRootUnit();
				if (rootUnit.activeUnit != null && rootUnit.activeUnit.LastDistanceFromCamera < 1000f)
				{
					EngineASX.Instance.PlayCollectCargoAudio(rootUnit.transform.position);
				}
			}
		}

		public Sprite GetRenderSprite()
		{
			return UnitClass.GetRenderSprite();
		}

		public Sprite GetIconSprite()
		{
			return UnitClass.GetIconSprite();
		}

		public float GetMaxHealth()
		{
			if (UnitType == UnitType.Cargo)
			{
				return CargoComponent.GetHealthBasedOnVolume();
			}
			return unitClass.maxHealth;
		}
	}
}
