using System;
using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.SpaceUnity;
using UnityEngine;
using Random = System.Random;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.Engine
{
	public class Sector : MonoBehaviour
	{
		private struct ConnectingSceneNode
		{
			public int Jumps;

			public Sector Sector;

			public ConnectingSceneNode(Sector sector, int jumps)
			{
				Sector = sector;
				Jumps = jumps;
			}
		}

		public double AsteroidRespawnCooldownTime;

		public float LightDirectionFudge;

		private double lastTimeChangedControl;

		private List<Faction> factionsHeadquarteredInThisSector = new List<Faction>(8);

		private int jumpDistanceToNearestControlledSector = -1;

		[SerializeField]
		private Quaternion backgroundRotation = Quaternion.identity;

		public SectorType SectorType;

		public int RandomSeed = -1;

		private Dictionary<UnitType, List<Unit>> unitsByType = new Dictionary<UnitType, List<Unit>>();

		private static Dictionary<Sector, int> connectingSectors = new Dictionary<Sector, int>(100);

		private static Queue<ConnectingSceneNode> connectingSceneNodeQueue = new Queue<ConnectingSceneNode>();

		public int UniqueId = -1;

		public string ActiveSceneResourceName;

		public Quaternion LightRotation = Quaternion.identity;

		public string Description = string.Empty;

		private EngineASX engine;

		public float GateDistanceMultiplier = 1f;

		private bool hasInit;

		public Vector3 MapPosition = Vector3.zero;

		public string Name = "Scene";

		private List<SectorNeighbour> neighbors = new List<SectorNeighbour>(3);

		public float SecurityLevel = 0.5f;

		public float UnclampedSecurityLevel = 0.5f;

		public string ThumbnailImageName;

		public Color SkyTintColor = Color.gray;

		public float SkyExposure = 0.8f;

		public Color AmbientLightColor = new Color(0.3f, 0.3f, 0.3f, 1f);

		public Color DirectionLightColor = new Color(0.8f, 0.8f, 0.8f, 1f);

		public AsteroidType AsteroidType;

		private Faction controllingFaction;

		private bool hasPlanets;

		private bool hasGasClouds;

		private bool hasAsteroidClusters;

		private float distanceFromUniverseCenter = -1f;

		private float distanceFromUniverseCenter01 = -1f;

		private float lastTimeMineDeployed = -100f;

		public Faction ControllingFaction => controllingFaction;

		public bool IsActive
		{
			get
			{
				if (engine != null)
				{
					return this == engine.ActiveSector;
				}
				return false;
			}
		}

		/// <summary>
		/// Whether this sector is simulated every frame despite not being the
		/// one on screen.
		/// </summary>
		/// <remarks>
		/// The engine normally ticks only the active sector per frame and
		/// round-robins the rest at roughly one unit every three seconds,
		/// because an unwatched sector is a sector where nothing visible is
		/// happening. That assumption is false in a session: a player two
		/// jumps away is watching their sector, and ships that only move once
		/// every three seconds look exactly like the sync being broken.
		///
		/// So somebody has to say which sectors are watched, and the
		/// multiplayer host does - it knows where every player's ship is.
		/// Nothing else sets this, which means single-player behaviour is
		/// untouched and a sector nobody is in goes back to ticking slowly the
		/// moment its last player leaves.
		///
		/// Set through <see cref="SetSimulatesEveryFrame"/> rather than
		/// assigned, so the count of sectors claiming this stays even when two
		/// players are in one place.
		/// </remarks>
		public bool SimulatesEveryFrame { get; private set; }

		private int fullRateClaims;

		/// <summary>
		/// Claims or releases this sector's need to be simulated every frame.
		/// </summary>
		/// <remarks>
		/// A count rather than a flag because two players can be in one
		/// sector, and the first of them to leave must not switch the tick
		/// rate back down underneath the second.
		/// </remarks>
		public void SetSimulatesEveryFrame(bool wanted)
		{
			if (wanted == SimulatesEveryFrame)
			{
				return;
			}

			if (wanted)
			{
				fullRateClaims++;
			}
			else
			{
				fullRateClaims--;

				// Someone setting this to false without having claimed it
				// would drive the count negative and leave the sector stuck
				// on full rate forever, which is a bug that only shows up
				// after a player has left.
				if (fullRateClaims < 0)
				{
					fullRateClaims = 0;
				}
			}

			SimulatesEveryFrame = fullRateClaims > 0;
		}

		public List<SectorNeighbour> Neighbours => neighbors;

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
					engineASX.DeregisterSector(this);
				}
				if (engine != null)
				{
					if (UniqueId < 0)
					{
						UniqueId = engine.GetUniqueSectorId();
					}
					engine.RegisterSector(this);
				}
			}
		}

		public bool IsValid => engine != null;

		public Quaternion BackgroundRotation
		{
			get
			{
				return backgroundRotation;
			}
			set
			{
				backgroundRotation = value;
			}
		}

		public bool HasPlanets
		{
			get
			{
				return hasPlanets;
			}
			set
			{
				hasPlanets = value;
			}
		}

		public bool HasGasClouds
		{
			get
			{
				return hasGasClouds;
			}
			set
			{
				hasGasClouds = value;
			}
		}

		public bool HasAsteroidClusters
		{
			get
			{
				return hasAsteroidClusters;
			}
			set
			{
				hasAsteroidClusters = value;
			}
		}

		public bool IsDeepSpace
		{
			get
			{
				if (!HasPlanets)
				{
					return !HasAsteroidClusters;
				}
				return false;
			}
		}

		public int JumpDistanceToNearestControlledSector
		{
			get
			{
				return jumpDistanceToNearestControlledSector;
			}
			set
			{
				jumpDistanceToNearestControlledSector = value;
			}
		}

		public float AdjustedSecurityLevel => SecurityLevel - FringeSectorRating;

		public float AdjustedSecurityLevel01 => (1f - FringeSectorRating + SecurityLevel) / 2f;

		public float FringeSectorRating => Mathf.Clamp01((float)jumpDistanceToNearestControlledSector / 5f);

		public float DistanceFromUniverseCenter
		{
			get
			{
				return distanceFromUniverseCenter;
			}
			set
			{
				distanceFromUniverseCenter = value;
			}
		}

		public float DistanceFromUniverseCenter01
		{
			get
			{
				return distanceFromUniverseCenter01;
			}
			set
			{
				distanceFromUniverseCenter01 = value;
			}
		}

		public IEnumerable<Faction> FactionsHeadquartered => factionsHeadquarteredInThisSector;

		public double LastTimeChangedControl
		{
			get
			{
				return lastTimeChangedControl;
			}
			set
			{
				lastTimeChangedControl = value;
			}
		}

		public bool HasCustomAppearanceSettings => GetComponent<CustomSectorAppearance>() != null;

		public bool HasRecentMineDeployment => Time.time - lastTimeMineDeployed < 10f;

		public void ChangeControllingFaction(Faction faction, bool setTimeOfChange)
		{
			if (controllingFaction != faction)
			{
				Faction faction2 = controllingFaction;
				controllingFaction = faction;
				if (faction2 != null)
				{
					faction2.OnSectorNotControlled(this);
				}
				if (controllingFaction != null)
				{
					controllingFaction.OnSectorControlled(this);
				}
				EngineASX.Instance.NotifySectorControlChanged(this, faction2, controllingFaction);
				if (setTimeOfChange)
				{
					lastTimeChangedControl = EngineASX.Instance.ScenarioElapsedTime;
				}
			}
		}

		[ContextMenu("Initialise")]
		public void Init()
		{
			if (!hasInit)
			{
				hasInit = true;
				if (RandomSeed == -1)
				{
					AssignRandomSeed();
				}
				Engine = EngineASX.Instance;
			}
		}

		public void AssignRandomSeed()
		{
			RandomSeed = GetRandomSeed();
		}

		private static int GetRandomSeed()
		{
			return UnityEngine.Random.Range(0, int.MaxValue);
		}

		internal string GetSectorSecurityLevelDescription()
		{
			if (SecurityLevel <= 0f)
			{
				return "None";
			}
			if (SecurityLevel > 0.8f)
			{
				return "Very secure";
			}
			if (SecurityLevel > 0.6f)
			{
				return "Secure";
			}
			if (SecurityLevel > 0.4f)
			{
				return "Insecure";
			}
			return "Very insecure";
		}

		public Wormhole GetConnectingGate(Sector neighboringSector, bool includeUnstable = false)
		{
			foreach (SectorNeighbour neighbor in neighbors)
			{
				if (neighbor.Sector == neighboringSector && (includeUnstable || neighbor.IsStableConnection))
				{
					return neighbor.ConnectingGate;
				}
			}
			return null;
		}

		public SectorNeighbour? GetNeighbour(Wormhole wormhole)
		{
			foreach (SectorNeighbour neighbor in neighbors)
			{
				if (neighbor.ConnectingGate == wormhole)
				{
					return neighbor;
				}
			}
			return null;
		}

		public void AddNeighbor(Sector targetSector, Wormhole connectingGate)
		{
			if (targetSector == null)
			{
				throw new NullReferenceException("scene");
			}
			if (connectingGate == null)
			{
				throw new NullReferenceException("connectingGate");
			}
			RemoveNeighbor(targetSector, connectingGate);
			neighbors.Add(new SectorNeighbour(targetSector, connectingGate));
		}

		public bool RemoveNeighbor(Sector neighboringSector, Wormhole connectingGate)
		{
			for (int i = 0; i < neighbors.Count; i++)
			{
				if (neighbors[i].Sector == neighboringSector && neighbors[i].ConnectingGate == connectingGate)
				{
					neighbors.RemoveAt(i);
					i--;
					return true;
				}
			}
			return false;
		}

		public int CalculateSectorJumpDistance(Sector target)
		{
			connectingSectors.Clear();
			connectingSceneNodeQueue.Clear();
			ConnectingSceneNode item = new ConnectingSceneNode(this, 0);
			connectingSceneNodeQueue.Enqueue(item);
			while (connectingSceneNodeQueue.Count > 0)
			{
				ConnectingSceneNode connectingSceneNode = connectingSceneNodeQueue.Dequeue();
				if (connectingSceneNode.Sector == target)
				{
					return connectingSceneNode.Jumps;
				}
				if (connectingSectors.ContainsKey(connectingSceneNode.Sector))
				{
					continue;
				}
				connectingSectors.Add(connectingSceneNode.Sector, connectingSceneNode.Jumps);
				foreach (SectorNeighbour neighbor in connectingSceneNode.Sector.neighbors)
				{
					if (neighbor.IsStableConnection && !connectingSectors.ContainsKey(neighbor.Sector))
					{
						ConnectingSceneNode item2 = new ConnectingSceneNode(neighbor.Sector, connectingSceneNode.Jumps + 1);
						connectingSceneNodeQueue.Enqueue(item2);
					}
				}
			}
			return -1;
		}

		public int GetJumpDistanceTo(Sector scene)
		{
			return engine.GetSectorJumpCount(this, scene);
		}

		public int GetJumpDistanceToAdjustedForDisconnected(Sector scene)
		{
			int sectorJumpCount = engine.GetSectorJumpCount(this, scene);
			if (sectorJumpCount >= 0)
			{
				return sectorJumpCount;
			}
			return 999;
		}

		public bool IsNeighbourOfIgnoringUnstable(Sector sourceSector)
		{
			foreach (SectorNeighbour neighbor in sourceSector.neighbors)
			{
				if (neighbor.IsStableConnection && neighbor.Sector == this)
				{
					return true;
				}
			}
			return false;
		}

		public bool IsNeighbourOfIncludingUnstable(Sector sourceSector)
		{
			foreach (SectorNeighbour neighbor in sourceSector.neighbors)
			{
				if (neighbor.Sector == this)
				{
					return true;
				}
			}
			return false;
		}

		public void UpdateAllUnitsVisibility()
		{
			UpdateUnitsVisibility(UnitType.Ship);
			UpdateUnitsVisibility(UnitType.Cargo);
			UpdateUnitsVisibility(UnitType.Station);
			UpdateUnitsVisibility(UnitType.Planet);
			UpdateUnitsVisibility(UnitType.Asteroid);
			UpdateUnitsVisibility(UnitType.AsteroidCluster);
			UpdateUnitsVisibility(UnitType.Debris);
			UpdateUnitsVisibility(UnitType.Wormhole);
			UpdateUnitsVisibility(UnitType.Projectile);
			UpdateUnitsVisibility(UnitType.GasCloud);
		}

		public void RefreshSectorUnitsActive()
		{
			UnitType[] unitTypes = EngineASX.Instance.unitTypes;
			foreach (UnitType key in unitTypes)
			{
				if (!unitsByType.TryGetValue(key, out var value) || value == null)
				{
					continue;
				}
				foreach (Unit item in value)
				{
					if (item != null)
					{
						item.RefreshIsInActiveSector();
						item.UpdateIsActive();
					}
				}
			}
		}

		private void UpdateUnitsVisibility(UnitType unitType)
		{
			List<Unit> list = GetUnitsByType(unitType);
			if (list == null)
			{
				return;
			}
			foreach (Unit item in list)
			{
				if (item.ActiveUnit != null)
				{
					item.ActiveUnit.UpdateVisibility(immediate: true);
				}
			}
		}

		public bool UnitsIntersecting(Unit u1, Unit u2, out float dist)
		{
			dist = Vector3.Distance(u1.transform.position, u2.transform.position);
			float num = u1.UnitClass.ShieldRingRadius + u2.UnitClass.ShieldRingRadius;
			return dist < num;
		}

		public void SeparateUnit(Unit unit, Unit staticUnit)
		{
			float dist = 0f;
			if (UnitsIntersecting(unit, staticUnit, out dist))
			{
				SeparateUnit(unit, staticUnit, dist);
			}
		}

		public void SeparateOverlappingShips()
		{
			List<Unit> list = GetUnitsByType(UnitType.Ship);
			if (list == null)
			{
				return;
			}
			List<Unit> list2 = GetUnitsByType(UnitType.Station);
			if (list2 != null)
			{
				foreach (Unit item in list2)
				{
					int num = Physics.OverlapSphereNonAlloc(item.transform.position, item.Radius, EngineASX.ColliderCache, GameController.Instance.ShipsMask);
					if (item.CollisionCollider == null || item.IsMinorStation())
					{
						continue;
					}
					for (int i = 0; i < num; i++)
					{
						Unit component = EngineASX.ColliderCache[i].GetComponent<Unit>();
						if (component.IsValidAndNotDestroyed && !component.IsDocked && component.CollisionCollider != null && !component.IsPlayerCurrentUnit && item.CollisionCollider.bounds.Intersects(component.CollisionCollider.bounds))
						{
							Vector3? vector = PhysicsNonOverlappingPositionFinder.FindSectorPositionOrNull(this, component.SectorPosition, component.UnitClass.DisplayData.Radius, GameController.Instance.NonOVerlappingUnitsMask);
							if (vector.HasValue)
							{
								component.transform.localPosition = vector.Value;
							}
						}
					}
				}
			}
			foreach (Unit item2 in list)
			{
				if (!(item2 != null) || item2.IsPlayerCurrentUnit || item2.IsDocked || !item2.IsValidAndNotDestroyed || !(item2.CollisionCollider != null))
				{
					continue;
				}
				int num2 = Physics.OverlapSphereNonAlloc(item2.transform.position, item2.Radius, EngineASX.ColliderCache, GameController.Instance.ShipsMask);
				for (int j = 0; j < num2; j++)
				{
					Unit component2 = EngineASX.ColliderCache[j].GetComponent<Unit>();
					if (component2 != null && component2 != item2 && component2.IsValidAndNotDestroyed && !component2.IsDocked && component2.CollisionCollider != null && !component2.IsPlayerCurrentUnit && item2.CollisionCollider.bounds.Intersects(component2.CollisionCollider.bounds))
					{
						Vector3? vector2 = PhysicsNonOverlappingPositionFinder.FindSectorPositionOrNull(this, item2.SectorPosition, item2.UnitClass.DisplayData.Radius, GameController.Instance.NonOVerlappingUnitsMask);
						if (vector2.HasValue)
						{
							item2.transform.localPosition = vector2.Value;
						}
					}
				}
			}
		}

		public void SeparateUnit(Unit unit, Unit staticUnit, float dist)
		{
			float num = unit.UnitClass.ShieldRingRadius + staticUnit.UnitClass.ShieldRingRadius;
			if (dist < num)
			{
				unit.transform.Translate(Vector3.Normalize(unit.transform.position - staticUnit.transform.position) * (num - dist) * 1.1f, Space.World);
			}
		}

		public float GetActualGateDistance()
		{
			if (engine == null)
			{
				engine = EngineASX.Instance;
			}
			return GetActualGateDistance(engine.World);
		}

		public float GetActualGateDistance(WorldBase world)
		{
			return world.GateDistance * GateDistanceMultiplier;
		}

		private void OnDestroy()
		{
			Engine = null;
		}

		public int GetCountOfUnitType(UnitType unitType)
		{
			return GetUnitsByType(unitType)?.Count ?? 0;
		}

		public int GetCountOfStationPurpose(StationPurpose stationPurpose)
		{
			int num = 0;
			List<Unit> list = GetUnitsByType(UnitType.Station);
			if (list != null)
			{
				foreach (Unit item in list)
				{
					if (item != null && item.UnitClass.StationPurpose == stationPurpose)
					{
						num++;
					}
				}
			}
			return num;
		}

		internal void AutoNameGameObject()
		{
			gameObject.name = $"Sector_{UniqueId}_{Name}_{ActiveSceneResourceName}";
		}

		public Vector3 ToLocalPosition(Vector3 worldPosition)
		{
			return worldPosition - transform.position;
		}

		public Vector3 ToWorldPosition(Vector3 sectorPosition)
		{
			return transform.localPosition + sectorPosition;
		}

		public override string ToString()
		{
			if (this != null)
			{
				return $"[Sector \"{UniqueId}\" ID:{Name}]";
			}
			return "Sector";
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

		internal void RemoveUnit(Unit unit)
		{
			List<Unit> value = null;
			if (unitsByType.TryGetValue(unit.UnitType, out value))
			{
				value.Remove(unit);
			}
		}

		internal void AddUnit(Unit unit)
		{
			List<Unit> value = null;
			if (!unitsByType.TryGetValue(unit.UnitType, out value))
			{
				value = new List<Unit>(16);
				unitsByType[unit.UnitType] = value;
			}
			value.Add(unit);
		}

		public void RefreshSectorContentType()
		{
			hasAsteroidClusters = DetermineHasAsteroidClusters();
			hasPlanets = DetermineHasPlanets();
			hasGasClouds = DetermineHasGasClouds();
		}

		public bool DetermineHasPlanets()
		{
			return GetCountOfUnitType(UnitType.Planet) > 0;
		}

		private bool DetermineHasGasClouds()
		{
			return GetCountOfUnitType(UnitType.GasCloud) > 0;
		}

		public bool DetermineHasAsteroidClusters()
		{
			return GetCountOfUnitType(UnitType.AsteroidCluster) > 0;
		}

		public void RefreshSecurityLevel()
		{
			UnclampedSecurityLevel = CalculateUnclampedSecurityLevel();
			SecurityLevel = Mathf.Clamp01(UnclampedSecurityLevel);
		}

		public float CalculateUnclampedSecurityLevel()
		{
			float combatRatingToSecurityLevelMultiplier = GameController.Instance.GameSettings.SectorSecuritySettings.CombatRatingToSecurityLevelMultiplier;
			return 0f + GetSecurityFromUnitsOfType(UnitType.Station, combatRatingToSecurityLevelMultiplier) + GetSecurityFromUnitsOfType(UnitType.Ship, combatRatingToSecurityLevelMultiplier);
		}

		public float GetSecurityFromUnitsOfType(UnitType unitType, float multiplier)
		{
			float num = 0f;
			List<Unit> list = GetUnitsByType(unitType);
			if (list != null)
			{
				foreach (Unit item in list)
				{
					float securityFromFactionMultiplier = GetSecurityFromFactionMultiplier(item.Faction);
					if (securityFromFactionMultiplier != 0f)
					{
						num += GetUnitSecurityRating(multiplier, item) * securityFromFactionMultiplier;
					}
				}
			}
			return num;
		}

		private float GetSecurityFromFactionMultiplier(Faction faction)
		{
			if (faction == null || faction.FactionType == FactionType.None || faction.FactionTypeInfo == null)
			{
				return 0f;
			}
			if (faction.FactionType == FactionType.Player)
			{
				return 0.5f;
			}
			return faction.FactionTypeInfo.SecurityRatingMultiplier;
		}

		private static float GetUnitSecurityRating(float combatRatingToSecurityLevelMultiplier, Unit station)
		{
			return station.UnitClass.CombatRating * combatRatingToSecurityLevelMultiplier;
		}

		public bool IsSectorANeighbourOfControllingFaction(Faction faction, bool includeUnstable = false)
		{
			if (controllingFaction == faction)
			{
				return false;
			}
			foreach (SectorNeighbour neighbor in neighbors)
			{
				if ((includeUnstable || neighbor.IsStableConnection) && neighbor.Sector.controllingFaction == faction)
				{
					return true;
				}
			}
			return false;
		}

		public bool IsSectorAnUncontrolledBorderSector(bool includeUnstable = false)
		{
			if (controllingFaction != null)
			{
				return false;
			}
			foreach (SectorNeighbour neighbor in neighbors)
			{
				if ((includeUnstable || neighbor.IsStableConnection) && neighbor.Sector.controllingFaction != null)
				{
					return true;
				}
			}
			return false;
		}

		public bool IsSectorAControlledBorderSector(Faction faction, bool includeUnstable = false)
		{
			if (controllingFaction != faction)
			{
				return false;
			}
			foreach (SectorNeighbour neighbor in neighbors)
			{
				if ((includeUnstable || neighbor.IsStableConnection) && neighbor.Sector.controllingFaction != faction)
				{
					return true;
				}
			}
			return false;
		}

		public Sector CalculateNearestControlledSector(int maxJumpDistance, out int jumpDistance)
		{
			jumpDistance = -1;
			if (ControllingFaction != null)
			{
				jumpDistance = 0;
				return this;
			}
			return SectorFinder.FindNearestSectorWithinJumpDistanceOfSimple(this, maxJumpDistance, includeUnstableWormholes: false, (Sector e) => e.ControllingFaction != null, out jumpDistance);
		}

		public void RefreshJumpDistanceToNearestControlledSector()
		{
			JumpDistanceToNearestControlledSector = CalculateJumpDistanceToNearestControlledSector();
		}

		public int CalculateJumpDistanceToNearestControlledSector()
		{
			if (controllingFaction != null)
			{
				return 0;
			}
			if (EngineASX.Instance.ControlledSectorCount == 0)
			{
				return -1;
			}
			foreach (SectorNeighbour neighbor in neighbors)
			{
				if (!neighbor.ConnectingGate.IsUnstable && neighbor.Sector.controllingFaction != null)
				{
					return 1;
				}
			}
			int num = -1;
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				if (sector != this && sector.controllingFaction != null)
				{
					int jumpDistanceTo = sector.GetJumpDistanceTo(this);
					if (jumpDistanceTo > -1 && (num < 0 || jumpDistanceTo < num))
					{
						num = jumpDistanceTo;
					}
				}
			}
			return num;
		}

		internal void RemoveHeadquarateredFaction(Faction faction)
		{
			factionsHeadquarteredInThisSector.Remove(faction);
		}

		internal void AddHeadquarateredFaction(Faction faction)
		{
			factionsHeadquarteredInThisSector.Add(faction);
		}

		[ContextMenu("Regenerate space background with new seed")]
		public void GenerateSpaceBackgroundWithNewSeed()
		{
			RandomSeed = (int)DateTime.Now.Ticks;
			GenerateSpaceBackground();
		}

		[ContextMenu("Generate space background")]
		public void GenerateSpaceBackground()
		{
			CustomSectorAppearance component = GetComponent<CustomSectorAppearance>();
			if (component != null)
			{
				System.Random random = new System.Random(RandomSeed);
				EngineASX.Instance.SpaceConstructor.Generate(random, component.SpaceConstructorParams);
			}
			else
			{
				EngineASX.Instance.SpaceConstructor.Generate(RandomSeed, GameController.Instance.GameSettings.SpaceConstructorSettings);
			}
		}

		public void RemoveCustomSectorAppearanceSettings()
		{
			CustomSectorAppearance component = GetComponent<CustomSectorAppearance>();
			if (component != null)
			{
				UnityEngine.Object.DestroyImmediate(component);
			}
		}

		public CustomSectorAppearance CreateCustomSectorAppearanceSettings()
		{
			CustomSectorAppearance customSectorAppearance = gameObject.AddComponent<CustomSectorAppearance>();
			customSectorAppearance.SpaceConstructorParams = new SpaceConstructorParams();
			return customSectorAppearance;
		}

		public CustomSectorAppearance GetOrCreateCustomAppearanceSettings()
		{
			CustomSectorAppearance customSectorAppearance = GetComponent<CustomSectorAppearance>();
			if (customSectorAppearance == null)
			{
				customSectorAppearance = CreateCustomSectorAppearanceSettings();
			}
			return customSectorAppearance;
		}

		public void RegisterMineDeployment()
		{
			lastTimeMineDeployed = Time.time;
		}
	}
}
