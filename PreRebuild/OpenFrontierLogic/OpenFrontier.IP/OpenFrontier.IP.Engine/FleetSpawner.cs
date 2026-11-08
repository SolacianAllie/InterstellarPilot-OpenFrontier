using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.Engine
{
	public class FleetSpawner : MonoBehaviour
	{
		private const float spawnCheckFreq = 2f;

		public bool AllowRespawnInActiveScene;

		private EngineASX engine;

		[FormerlySerializedAs("GroupFaction")]
		public Faction Faction;

		[FormerlySerializedAs("GroupHomeBase")]
		public Unit HomeBaseUnit;

		[FormerlySerializedAs("GroupHomeScene")]
		public Sector HomeSector;

		[FormerlySerializedAs("GroupPrefab")]
		public Fleet Prefab;

		private bool hasInit;

		public float InitialSpawnTimeRandomness;

		[FormerlySerializedAs("MaxGroupUnitCount")]
		public int MaxUnitCount = 1;

		public float MaxTimeBeforeSpawn = 120f;

		[FormerlySerializedAs("MinGroupUnitCount")]
		public int MinUnitCount = 1;

		public float MinTimeBeforeSpawn = 60f;

		public string NamePrefix;

		private double nextSpawnCheck;

		public double NextSpawnTime;

		private List<FleetOrder> orders = new List<FleetOrder>(4);

		public List<Person> PilotPrefabs = new List<Person>();

		public bool RespawnWhenNoObjectives = true;

		public bool RespawnWhenNoPilots = true;

		private Sector sector;

		public string ShipDesignation;

		public string ShipName;

		public int SpawnCounter;

		[NonSerialized]
		public Unit SpawnDock;

		private Fleet spawnedGroup;

		public float SpawnTimeRandomness = 120f;

		public List<UnitClass> UnitClasses = new List<UnitClass>(3);

		public Fleet SpawnedGroup
		{
			get
			{
				return spawnedGroup;
			}
			set
			{
				if (spawnedGroup != value)
				{
					Fleet oldGroup = spawnedGroup;
					spawnedGroup = value;
					OnSpawnGroupChanged(oldGroup);
				}
			}
		}

		public Sector Sector
		{
			get
			{
				return sector;
			}
			set
			{
				sector = value;
			}
		}

		public List<FleetOrder> Objectives => orders;

		public void Init()
		{
			if (!hasInit)
			{
				engine = EngineASX.Instance;
				engine.FleetSpawners.Add(this);
				hasInit = true;
				FindScene();
				FindSpawnDock();
				UpdateObjectives();
				NextSpawnTime += engine.ScenarioElapsedTime + (double)(UnityEngine.Random.value * InitialSpawnTimeRandomness);
				if (SpawnedGroup != null)
				{
					OnSpawnGroupChanged(null);
				}
			}
		}

		public void UpdateObjectives()
		{
			orders.AddRange(from e in GetComponentsInChildren<FleetOrder>(includeInactive: true)
				orderby e.name
				select e);
		}

		public void FindScene()
		{
			sector = UnityObjectHelper.FindInParentsOrSelf<Sector>(gameObject);
		}

		public void FindSpawnDock()
		{
			SpawnDock = UnityObjectHelper.FindInParentsOrSelf<Unit>(gameObject);
		}

		public void Update()
		{
			if (hasInit && sector != null && sector.IsActive)
			{
				Tick();
			}
		}

		public void Tick()
		{
			if (!EngineASX.LoadedAndReady || !hasInit || !(sector != null) || !(SpawnedGroup == null) || !((double)Time.time > nextSpawnCheck))
			{
				return;
			}
			nextSpawnCheck = Time.time + 2f;
			if (engine.ScenarioElapsedTime > NextSpawnTime && CanSpawn())
			{
				SpawnedGroup = SpawnGroup();
				if (spawnedGroup != null)
				{
					AddOrdersToFleet(spawnedGroup);
					SpawnCounter++;
				}
			}
		}

		private bool CanSpawnAtSpawnDock()
		{
			return SpawnDock.IsDockable;
		}

		public bool CanSpawn()
		{
			if (engine.GameSettings.AllowAiGroupSpawners)
			{
				if ((!(SpawnDock != null) || !CanSpawnAtSpawnDock()) && sector.IsActive && !AllowRespawnInActiveScene && !(engine.PlayerUnit == null))
				{
					return Vector3.Distance(transform.position, engine.PlayerUnit.transform.position) > engine.GameSettings.AIGroupSpawnMinPlayerDistance;
				}
				return true;
			}
			return false;
		}

		public Fleet SpawnGroup()
		{
			FleetSpawnParams fleetSpawnParams = CreateSpawnParams();
			if (fleetSpawnParams != null)
			{
				return FleetSpawn.SpawnGroupFromParams(fleetSpawnParams);
			}
			return null;
		}

		public void AddOrdersToFleet(Fleet group)
		{
			for (int i = 0; i < orders.Count; i++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(orders[i].gameObject);
				gameObject.transform.SetParent(group.transform);
				FleetOrder component = gameObject.GetComponent<FleetOrder>();
				component.transform.localPosition = Vector3.zero;
				group.EnqueueOrder(component);
			}
		}

		public void SetNextSpawnTime()
		{
			NextSpawnTime = engine.ScenarioElapsedTime + (double)UnityEngine.Random.Range(MinTimeBeforeSpawn, MaxTimeBeforeSpawn + SpawnTimeRandomness * UnityEngine.Random.value);
		}

		public void DestroySpawnedGroup()
		{
			Fleet fleet = spawnedGroup;
			if ((bool)fleet)
			{
				SpawnedGroup = null;
				fleet.DestroyPilotsAndShips();
				UnityEngine.Object.Destroy(fleet.gameObject);
			}
		}

		private void OnDestroy()
		{
			SpawnedGroup = null;
			if (engine != null)
			{
				engine.FleetSpawners.Remove(this);
			}
		}

		private FleetSpawnParams CreateSpawnParams()
		{
			if (Prefab != null)
			{
				if (UnitClasses.Count > 0)
				{
					int num = UnityEngine.Random.Range(MinUnitCount, MaxUnitCount + 1);
					if (num > 0)
					{
						FleetSpawnParams fleetSpawnParams = new FleetSpawnParams();
						fleetSpawnParams.TargetSector = sector;
						fleetSpawnParams.TargetSectorPosition = sector.ToLocalPosition(transform.position);
						fleetSpawnParams.FleetPrefab = Prefab;
						fleetSpawnParams.Ships = new List<FleetSpawnShipParams>();
						fleetSpawnParams.HomeBase = HomeBaseUnit;
						fleetSpawnParams.HomeSector = HomeSector;
						fleetSpawnParams.Faction = Faction;
						fleetSpawnParams.TargetDock = SpawnDock;
						for (int i = 0; i < num; i++)
						{
							FleetSpawnShipParams item = new FleetSpawnShipParams
							{
								PilotPrefab = PilotPrefabs.GetRandom(),
								UnitClass = UnitClasses.GetRandom()
							};
							fleetSpawnParams.Ships.Add(item);
						}
						return fleetSpawnParams;
					}
					Debug.LogWarning($"{this}: AIGroupSpawner has unit zero count", this);
				}
				else
				{
					Debug.LogWarning($"{this}: AIGroupSpawner has no unit prefabs set", this);
				}
			}
			else
			{
				Debug.LogWarning($"{this}: AIGroupSpawner has no group prefab set", this);
			}
			return null;
		}

		private void OnSpawnGroupChanged(Fleet oldGroup)
		{
			if (oldGroup != null)
			{
				oldGroup.AllObjectivesRemoved -= spawnedGroup_AllObjectivesRemoved;
				oldGroup.AllPilotsRemoved -= spawnedGroup_AllPilotsRemoved;
			}
			if (spawnedGroup != null)
			{
				spawnedGroup.AllObjectivesRemoved += spawnedGroup_AllObjectivesRemoved;
				spawnedGroup.AllPilotsRemoved += spawnedGroup_AllPilotsRemoved;
			}
			if (spawnedGroup == null)
			{
				SetNextSpawnTime();
			}
		}

		private void spawnedGroup_AllPilotsRemoved(Fleet sender)
		{
			if (RespawnWhenNoPilots)
			{
				Debug.LogWarning($"Destroying group \"{spawnedGroup}\" because no pilots left", this);
				DestroySpawnedGroup();
			}
		}

		private void spawnedGroup_AllObjectivesRemoved(Fleet sender)
		{
			if (RespawnWhenNoObjectives)
			{
				Debug.LogWarning($"Destroying group \"{spawnedGroup}\" because no objectives left", this);
				DestroySpawnedGroup();
			}
		}
	}
}
