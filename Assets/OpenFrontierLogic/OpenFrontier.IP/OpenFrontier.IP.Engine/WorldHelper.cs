using System;
using System.Collections.Generic;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.Comms;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Pathfinding;
using OpenFrontier.IP.Engine.TargetScanning;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.Engine
{
	public class WorldHelper
	{
		private static List<WorldNavpoint> waypoints = new List<WorldNavpoint>(32);

		public static bool ShouldShowBountyBoard(Faction faction)
		{
			if (faction.BountyBoard != null)
			{
				return faction.BountyBoard.HasAnyBounty();
			}
			return false;
		}

		public static Wormhole GetNearestWormhole(Sector sector, Vector3 sectorPosition, bool includeUnstable, out float nearestDist)
		{
			nearestDist = float.MaxValue;
			Wormhole wormhole = null;
			foreach (SectorNeighbour neighbour in sector.Neighbours)
			{
				if ((includeUnstable || neighbour.IsStableConnection) && neighbour.ConnectingGate != null && neighbour.ConnectingGate.Unit.IsValidAndNotDestroyed)
				{
					float num = Vector3.Distance(sectorPosition, neighbour.ConnectingGate.Unit.SectorPosition);
					if (wormhole == null || num < nearestDist)
					{
						nearestDist = num;
						wormhole = neighbour.ConnectingGate;
					}
				}
			}
			return wormhole;
		}

		public static NpcPilot SpawnNpcFromForPerson(Person pilot, UnitComponentHolder unit = null)
		{
			NpcPilot npcPilot = pilot.GetComponent<NpcPilot>();
			if (npcPilot == null)
			{
				npcPilot = pilot.gameObject.AddComponent<NpcPilot>();
			}
			npcPilot.Init();
			if (unit != null)
			{
				unit.PilotPerson = pilot;
			}
			return npcPilot;
		}

		public static NpcPilot SpawnRandomizedNpcFromPersonPrefab(Person prefab, UnitComponentHolder unit = null)
		{
			if (prefab != null)
			{
				Person person = SpawnRandomizedPerson(prefab);
				NpcPilot npcPilot = person.GetComponent<NpcPilot>();
				if (npcPilot == null)
				{
					npcPilot = person.gameObject.AddComponent<NpcPilot>();
				}
				npcPilot.Init();
				if (unit != null)
				{
					unit.PilotPerson = person;
				}
				return npcPilot;
			}
			return null;
		}

		public static Person SpawnRandomizedPerson(Person prefab)
		{
			Person person = SpawnPerson(prefab);
			person.RandomizeNameAndPersonalityAndAssignDialog();
			return person;
		}

		public static Person SpawnPerson(Person prefab)
		{
			Person person = UnityEngine.Object.Instantiate(prefab);
			person.Init();
			person.DestroyGameObjectOnKill = true;
			return person;
		}

		public static Unit FindNearestStableWormholeUnit(Sector sector, Vector3 sectorPosition, out float nearestDistance)
		{
			Unit unit = null;
			nearestDistance = 0f;
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Wormhole);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (!item.WormholeComponent.IsUnstable)
					{
						float num = Vector3.Distance(item.SectorPosition, sectorPosition);
						if (unit == null || num < nearestDistance)
						{
							unit = item;
							nearestDistance = num;
						}
					}
				}
			}
			return unit;
		}

		public static Unit SpawnUnitAndInstallComponentsAtSafePosition(Unit prefab, Sector sector, Vector3 sectorPosition, float spacingFromObjects = 0f, bool addCargoLoadout = true, bool isUnderConstruction = false)
		{
			sectorPosition = PhysicsNonOverlappingPositionFinder.FindSectorPosition(sector, sectorPosition, prefab.UnitClass.ShieldRingRadius + spacingFromObjects, GameController.Instance.NonOVerlappingUnitsMask);
			Unit unit = SpawnUnitAndInstallComponents(prefab, sector, addCargoLoadout, isUnderConstruction);
			unit.transform.localPosition = sectorPosition;
			unit.UpdateGasCloudIfNecessary();
			return unit;
		}

		public static Unit SpawnUnitAndInstallComponents(Unit prefab, Sector sector, Vector3 sectorPosition, float spacingFromObjects = 0f, bool addCargoLoadout = true, bool isUnderConstruction = false)
		{
			Unit unit = SpawnUnitAndInstallComponents(prefab, sector, addCargoLoadout, isUnderConstruction);
			unit.transform.localPosition = sectorPosition;
			unit.UpdateGasCloudIfNecessary();
			return unit;
		}

		internal static bool FactionHasOwnedScannerWithinScanRangeRangeOf(Faction localFaction, Sector sector, Vector3 sectorPosition, out Unit nearestScanner)
		{
			int num = Physics.OverlapSphereNonAlloc(sector.ToWorldPosition(sectorPosition), GameController.Instance.GameSettings.ScanRange, EngineASX.ColliderCache, GameController.Instance.ShipsAndStationsMask, QueryTriggerInteraction.Collide);
			float num2 = float.MaxValue;
			nearestScanner = null;
			for (int i = 0; i < num; i++)
			{
				Unit component = EngineASX.ColliderCache[i].GetComponent<Unit>();
				if (component != null && component.Faction == localFaction && component.IsValidAndNotDestroyed && component.Components.ShouldPerformScan() && component.Components.ConstructionState == ConstructionState.Constructed)
				{
					float num3 = Vector3.Distance(component.SectorPosition, sectorPosition);
					if (nearestScanner == null || num3 < num2)
					{
						nearestScanner = component;
						num2 = num3;
					}
				}
			}
			return nearestScanner != null;
		}

		public static Unit SpawnUnitAndInstallComponents(Unit prefab, Sector sector, bool addCargoLoadout = true, bool isUnderConstruction = false)
		{
			Unit component = UnityEngine.Object.Instantiate(prefab.gameObject).GetComponent<Unit>();
			component.Init(autoFindParents: false);
			component.Sector = sector;
			if (component.Components != null)
			{
				component.Components.InstallDefaultComponents();
				if (component.Components.RequiresShipName())
				{
					component.Components.AutoAssignShipName();
				}
				if (isUnderConstruction)
				{
					component.Components.StartConstruction();
				}
			}
			if (addCargoLoadout && component.Components != null)
			{
				component.Components.AddDefaultCargoLoadout();
			}
			return component;
		}

		public static Unit SpawnUnit(Unit prefab, Sector sector, bool installDefaultComponents = true, bool addCargoLoadout = true, bool isUnderConstruction = false)
		{
			Unit component = UnityEngine.Object.Instantiate(prefab.gameObject).GetComponent<Unit>();
			component.Init(autoFindParents: false);
			component.Sector = sector;
			if (component.Components != null)
			{
				if (installDefaultComponents)
				{
					component.Components.InstallDefaultComponents();
				}
				if (component.Components.RequiresShipName())
				{
					component.Components.AutoAssignShipName();
				}
				if (isUnderConstruction)
				{
					component.Components.StartConstruction();
				}
			}
			if (addCargoLoadout && component.Components != null)
			{
				component.Components.AddDefaultCargoLoadout();
			}
			return component;
		}

		public static bool Lerp(Sector startSector, Vector3 startSectorPosition, Sector endSector, Vector3 endSectorPosition, Faction faction, bool ignoreHeatmap, float lerp, out Sector sector, out Vector3 resultSectorPosition)
		{
			sector = null;
			resultSectorPosition = Vector3.zero;
			waypoints.Clear();
			if (AdvPathfinder.Pathfind(startSector, startSectorPosition, endSector, endSectorPosition, faction, ignoreHeatmap, waypoints).IsSuccess && Lerp(startSectorPosition, startSector, waypoints, lerp, out sector, out resultSectorPosition))
			{
				return true;
			}
			return false;
		}

		public static bool Lerp(Vector3 startSectorPosition, Sector startSector, List<WorldNavpoint> waypoints, float lerp, out Sector sector, out Vector3 sectorPosition)
		{
			sectorPosition = Vector3.zero;
			sector = null;
			lerp = Mathf.Clamp01(lerp);
			if (waypoints != null && waypoints.Count > 0)
			{
				if (waypoints.Count > 0)
				{
					float[] waypointDistanceCache = EngineASX.Instance.DistanceCalculator.waypointDistanceCache;
					float num = StationDistanceCalculator.CalculateWaypointsDistance(ref startSectorPosition, waypoints, waypointDistanceCache);
					sectorPosition = Vector3.zero;
					float num2 = num * lerp;
					int i = 0;
					float num3;
					for (num3 = 0f; i < waypoints.Count && num2 > num3 + waypointDistanceCache[i]; i++)
					{
						startSectorPosition = waypoints[i].GetTargetSectorPosition();
						startSector = waypoints[i].GetTargetSector();
						num3 += waypointDistanceCache[i];
					}
					sector = startSector;
					if (waypointDistanceCache[i] > 0f)
					{
						sectorPosition = Vector3.Lerp(startSectorPosition, waypoints[i].GetTargetSectorPosition(), (num2 - num3) / waypointDistanceCache[i]);
					}
					else
					{
						sectorPosition = startSectorPosition;
					}
					return true;
				}
				sector = waypoints[0].GetTargetSector();
				sectorPosition = waypoints[0].GetTargetSectorPosition();
				return sector != null;
			}
			return false;
		}

		public static Unit GetNearestSellerOfUnitClass(EngineASX engine, Sector originSector, Vector3 originSectorPosition, UnitClass unitClass)
		{
			float nearestDistance = float.MaxValue;
			Unit nearestUnit = null;
			EngineASX.Instance.EnumerateUnitsWithPredicate((Unit unit) =>
			{
				if (unit.IsValidAndNotDestroyed && unit.IsSellerOfUnitClass(unitClass))
				{
					float? totalDistanceBetweenSceneTargets = GetTotalDistanceBetweenSceneTargets(originSector, originSectorPosition, unit.Sector, unit.SectorPosition);
					if (totalDistanceBetweenSceneTargets.HasValue && totalDistanceBetweenSceneTargets.Value < nearestDistance)
					{
						nearestDistance = totalDistanceBetweenSceneTargets.Value;
						nearestUnit = unit;
					}
				}
			}, (Unit unit) => unit.IsDockable);
			return nearestUnit;
		}

		public static float? GetTotalDistanceBetweenSceneTargets(Sector sector1, Vector3 sectorPosition1, Sector sector2, Vector3 sectorPosition2)
		{
			return EngineASX.Instance.DistanceCalculator.CalculateDistance(sector1, sectorPosition1, sector2, sectorPosition2);
		}

		public static bool CanTransferCargoToUnit(Unit playerUnit, Unit targetUnit)
		{
			if (targetUnit != null && targetUnit.IsValidAndNotDestroyed && targetUnit != playerUnit && targetUnit.IsOwnedByPlayer && !targetUnit.IsUnderConstructionOrDismantling && targetUnit.CargoBayComponent != null && playerUnit != null && playerUnit.IsOwnedByPlayer && playerUnit.CargoBayComponent != null && !playerUnit.IsUnderConstructionOrDismantling)
			{
				if (playerUnit.Engine.GameSettings.CanTransferCargoFromAnyDistance)
				{
					return true;
				}
				if (Vector3.Distance(targetUnit.transform.position, playerUnit.transform.position) - targetUnit.UnitClass.ShieldRingRadius - playerUnit.GetRootUnit().UnitClass.ShieldRingRadius < playerUnit.Engine.GameSettings.TransferCargoDistance && targetUnit.Faction == playerUnit.Engine.LocalFaction)
				{
					return targetUnit.CargoBayComponent != null;
				}
				return false;
			}
			return false;
		}

		public static bool CanEnterUnit(Unit currentUnit, Unit unit, float distance)
		{
			if (currentUnit != null && currentUnit.IsValidAndNotDestroyed && unit != null && !unit.IsPlayerCurrentUnit && unit.IsValidAndNotDestroyed && unit.IsOwnedByPlayer && !unit.IsUnderConstructionOrDismantling && unit.IsStationOrShip() && unit.UnitClass.ShipType == ShipType.Normal)
			{
				distance -= unit.UnitClass.ShieldRingRadius;
				distance -= currentUnit.GetRootUnit().UnitClass.ShieldRingRadius;
				return CanEnterUnitAtDistance(distance);
			}
			return false;
		}

		public static bool CanEnterUnitAtDistance(float distance)
		{
			if (EngineASX.Instance.World.CanEnterShipFromAnyDistance)
			{
				return true;
			}
			return distance < GameController.Instance.GameSettings.EnterShipDistance;
		}

		public static bool CanEnterUnitFromCurrentUnit(Unit unit)
		{
			Unit localUnit = EngineASX.Instance.LocalUnit;
			if (localUnit == null)
			{
				return false;
			}
			float num = 0f;
			num = ((!(unit.Sector == localUnit.Sector)) ? 100000f : Vector3.Distance(unit.SectorPosition, localUnit.SectorPosition));
			return CanEnterUnit(localUnit, unit, num);
		}

		public static bool CanEnterUnitAtPosition(Sector sector, Vector3 sectorPosition)
		{
			Unit localUnit = EngineASX.Instance.LocalUnit;
			if (localUnit == null)
			{
				return false;
			}
			float num = 0f;
			num = ((!(sector == localUnit.Sector)) ? 100000f : Vector3.Distance(sectorPosition, localUnit.SectorPosition));
			return CanEnterUnitAtDistance(num);
		}

		public static Unit FindNearestStationOrWormhole(Sector sector, Vector3 sectorPosition)
		{
			Unit unit = null;
			float num = 0f;
			int num2 = Physics.OverlapSphereNonAlloc(sector.ToWorldPosition(sectorPosition), 1500f, EngineASX.ColliderCache, GameController.Instance.StationsAndWormholesMask);
			if (num2 > 0)
			{
				for (int i = 0; i < num2; i++)
				{
					Unit component = EngineASX.ColliderCache[0].GetComponent<Unit>();
					if (component != null && component.IsValidAndNotDestroyed)
					{
						return component;
					}
				}
			}
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Wormhole);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					float num3 = Vector3.Distance(sectorPosition, item.SectorPosition);
					if (unit == null || num3 < num)
					{
						unit = item;
						num = num3;
					}
				}
			}
			return unit;
		}

		public static bool AnyOverlappingStationOrWormhole(Sector sector, Vector3 sectorPosition, float checkRadius = 1500f)
		{
			int num = Physics.OverlapSphereNonAlloc(sector.ToWorldPosition(sectorPosition), checkRadius, EngineASX.ColliderCache, GameController.Instance.StationsAndWormholesMask);
			if (num > 0)
			{
				for (int i = 0; i < num; i++)
				{
					Unit component = EngineASX.ColliderCache[0].GetComponent<Unit>();
					if (component != null && component.IsValidAndNotDestroyed)
					{
						return true;
					}
				}
			}
			return false;
		}

		public static void SimpleSectorSearch(Sector startSector, int maxJumpDistance, Action<Sector> action, bool includeUnstable = false)
		{
			HashSet<int> hashSet = new HashSet<int>();
			Queue<(Sector, int)> queue = new Queue<(Sector, int)>();
			queue.Enqueue((EngineASX.Instance.ActiveSector, 0));
			hashSet.Add(startSector.UniqueId);
			while (queue.Count > 0)
			{
				var (sector, num) = queue.Dequeue();
				action(sector);
				foreach (SectorNeighbour neighbour in sector.Neighbours)
				{
					if ((includeUnstable || !neighbour.ConnectingGate.IsUnstable) && num < maxJumpDistance && !hashSet.Contains(neighbour.Sector.UniqueId))
					{
						hashSet.Add(neighbour.Sector.UniqueId);
						queue.Enqueue((neighbour.Sector, num + 1));
					}
				}
			}
		}

		public static bool AllowSetWaypointToUnit(Unit unit)
		{
			if (unit == null || !unit.IsValidAndNotDestroyed)
			{
				return false;
			}
			if (EngineASX.Instance.LocalUnit == null || EngineASX.Instance.LocalUnit.HasSameRootUnitAs(unit))
			{
				return false;
			}
			switch (unit.UnitType)
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

		public static bool AllowToggleWaypoint(Unit unit)
		{
			if (unit == null || !unit.IsValidAndNotDestroyed)
			{
				return false;
			}
			if (EngineASX.Instance.LocalUnit == null || EngineASX.Instance.LocalUnit.HasSameRootUnitAs(unit))
			{
				return false;
			}
			switch (unit.UnitType)
			{
			case UnitType.Ship:
			case UnitType.Station:
			case UnitType.Cargo:
			case UnitType.Wormhole:
			case UnitType.Asteroid:
			case UnitType.NavBuoy:
				return true;
			case UnitType.Waypoint:
				if (!unit.IsPlayerCustomPathTarget())
				{
					return unit.IsPlayerCustomPathMarker();
				}
				return true;
			default:
				return false;
			}
		}

		public static void EnterUnit(Unit unit)
		{
			EngineASX.Instance.ChangePlayerUnit(unit);
			EngineASX.Instance.PlayChangeShipAudio();
		}

		public static bool CanPlayerViewCargo(Unit unit)
		{
			if (unit != null && unit.CargoBayComponent != null)
			{
				return unit.IsOwnedByPlayer;
			}
			return false;
		}

		public static bool CanShowUnitContext(Unit unit)
		{
			if (unit == null || !unit.IsValidAndNotDestroyed)
			{
				return false;
			}
			if (unit.UnitType == UnitType.Waypoint)
			{
				return false;
			}
			return true;
		}

		public static bool ShouldShowCommsButton(Unit unit, out ICommsHandler commsHandler)
		{
			commsHandler = null;
			if (unit != null && unit.Components != null)
			{
				return CommsHelper.TryGetCommsHandlerFromUnit(unit, out commsHandler);
			}
			return false;
		}

		public static bool CanPlayerScanUnit(Unit unit, out Unit scanner)
		{
			scanner = null;
			if (!TargetScanUtils.CanPlayerScan(unit))
			{
				return false;
			}
			return FactionHasOwnedScannerWithinScanRangeRangeOf(EngineASX.Instance.LocalFaction, unit.Sector, unit.SectorPosition, out scanner);
		}

		public static bool CanShowUnitInfo(Unit unit)
		{
			if (unit != null && unit.IsValidAndNotDestroyed)
			{
				return unit.CanShowUnitInfo();
			}
			return false;
		}
	}
}
