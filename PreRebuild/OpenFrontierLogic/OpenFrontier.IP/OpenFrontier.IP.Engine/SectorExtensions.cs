using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.UI.Screens.Fleets;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public static class SectorExtensions
	{
		public static bool IsDiscoveredByLocalFaction(this Sector sector)
		{
			Faction localFaction = EngineASX.Instance.LocalFaction;
			if (localFaction != null)
			{
				return localFaction.Intel.IsSectorDiscovered(sector);
			}
			return false;
		}

		public static bool ContainsHostileStationsIgnoringIntel(this Sector sector, Faction faction)
		{
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item.IsHostileToOrAlwaysHostileToTwoWay(faction))
					{
						return true;
					}
				}
			}
			return false;
		}

		public static bool ContainsPlayerProperty(this Sector sector)
		{
			if (sector.Engine.LocalFaction != null)
			{
				foreach (Unit unit in sector.Engine.LocalFaction.Units)
				{
					if (unit.IsValidAndNotDestroyed && unit.Sector == sector)
					{
						return true;
					}
				}
			}
			return false;
		}

		public static bool ContainsPlayerStations(this Sector sector)
		{
			if (sector.Engine.LocalFaction != null)
			{
				List<Unit> unitsByType = sector.Engine.LocalFaction.GetUnitsByType(UnitType.Station);
				if (unitsByType != null)
				{
					foreach (Unit item in unitsByType)
					{
						if (item.IsValidAndNotDestroyed && item.Sector == sector)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		public static bool ContainsPlayerShips(this Sector sector)
		{
			if (sector.Engine.LocalFaction != null)
			{
				List<Unit> unitsByType = sector.Engine.LocalFaction.GetUnitsByType(UnitType.Ship);
				if (unitsByType != null)
				{
					foreach (Unit item in unitsByType)
					{
						if (item.IsValidAndNotDestroyed && item.Sector == sector)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		public static bool ContainsPlayerFleets(this Sector sector)
		{
			foreach (Fleet fleet in EngineASX.Instance.LocalFaction.Fleets)
			{
				if (fleet.Sector == sector && FleetsHelper.ShouldShowFleet(fleet))
				{
					return true;
				}
			}
			return false;
		}

		public static bool ContainsDiscoveredStationsHostileToPlayer(this Sector sector)
		{
			Faction localFaction = EngineASX.Instance.LocalFaction;
			if (localFaction != null && localFaction.Intel != null)
			{
				if (!localFaction.Intel.SectorHasRecentHostileStations(sector, double.MaxValue))
				{
					return false;
				}
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
				if (unitsByType != null)
				{
					foreach (Unit item in unitsByType)
					{
						if (item.IsValidAndNotDestroyed && localFaction.Intel.IsUnitDiscovered(item) && item.IsHostileToOrAlwaysHostileToTwoWay(EngineASX.Instance.LocalFaction))
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		public static bool ContainsDiscoveredShipsHostileToPlayer(this Sector sector)
		{
			Faction localFaction = EngineASX.Instance.LocalFaction;
			if (localFaction != null && localFaction.Intel != null)
			{
				if (!localFaction.Intel.SectorHasRecentHostileShips(sector, double.MaxValue))
				{
					return false;
				}
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Ship);
				if (unitsByType != null)
				{
					foreach (Unit item in unitsByType)
					{
						if (item.IsValidAndNotDestroyed && localFaction.Intel.IsUnitDiscovered(item) && item.IsHostileToOrAlwaysHostileToTwoWay(EngineASX.Instance.LocalFaction))
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		public static bool ContainsRecentlyDiscoveredStationsHostileToPlayer(this Sector sector, double maxAgeOfDiscovery = 30.0)
		{
			Faction localFaction = EngineASX.Instance.LocalFaction;
			if (localFaction != null && localFaction.Intel != null && localFaction.Intel.SectorHasRecentHostileStations(sector, maxAgeOfDiscovery))
			{
				return true;
			}
			return false;
		}

		public static bool ContainsRecentlyDiscoveredShipsHostileToPlayer(this Sector sector, double? maxAgeOfDiscovery = null)
		{
			Faction localFaction = EngineASX.Instance.LocalFaction;
			if (localFaction != null && localFaction.Intel != null && localFaction.Intel.SectorHasRecentHostileShips(sector, maxAgeOfDiscovery ?? ((double)GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTime)))
			{
				return true;
			}
			return false;
		}

		public static bool ContainsPlayerWaypointTarget(this Sector sector)
		{
			if (sector.ContainsPlayerCustomWaypointTarget())
			{
				return true;
			}
			if (sector.ContainsPlayerMissionWaypointTarget())
			{
				return true;
			}
			return false;
		}

		public static bool ContainsPlayerMissionWaypointTarget(this Sector sector)
		{
			foreach (PlayerWaypointPath missionPath in EngineASX.Instance.LocalPlayer.WaypointController.MissionPaths)
			{
				if (sector.ContainsPlayerWaypointTarget(missionPath))
				{
					return true;
				}
			}
			return false;
		}

		public static bool ContainsPlayerCustomWaypointTarget(this Sector sector)
		{
			GamePlayer localPlayer = EngineASX.Instance.LocalPlayer;
			if (sector.ContainsPlayerWaypointTarget(localPlayer.WaypointController.CustomPath))
			{
				return true;
			}
			return false;
		}

		public static bool ContainsPlayerWaypointTarget(this Sector sector, PlayerWaypointPath path)
		{
			if (path.HasWaypoint)
			{
				return path.Waypoint.Value.GetTargetSector() == sector;
			}
			return false;
		}

		public static bool IsOnPlayerWaypointPath(this Sector sector)
		{
			GamePlayer localPlayer = EngineASX.Instance.LocalPlayer;
			if (sector.IsOnPlayerWaypointPath(localPlayer.WaypointController.CustomPath))
			{
				return true;
			}
			foreach (PlayerWaypointPath missionPath in localPlayer.WaypointController.MissionPaths)
			{
				if (sector.IsOnPlayerWaypointPath(missionPath))
				{
					return true;
				}
			}
			return false;
		}

		public static bool IsOnPlayerWaypointPath(this Sector sector, PlayerWaypointPath path)
		{
			if (path.HasValidPath)
			{
				List<WorldNavpoint> waypoints = path.Waypoints;
				for (int i = 0; i < waypoints.Count; i++)
				{
					if (waypoints[i].Sector == sector)
					{
						return true;
					}
				}
			}
			return false;
		}

		public static Vector3 GetRandomSafeDeploymentSectorPosition(this Sector sector, float minGateDistanceMultiplier, float maxGateDistanceMultiplier, float objectRadius, LayerMask layerMask)
		{
			float actualGateDistance = sector.GetActualGateDistance();
			float num = Random.Range(actualGateDistance * minGateDistanceMultiplier, actualGateDistance * maxGateDistanceMultiplier);
			Vector3 checkSectorPosition = Maths.RandomXZDirection() * num;
			return PhysicsNonOverlappingPositionFinder.FindSectorPosition(sector, checkSectorPosition, objectRadius, layerMask);
		}

		public static Vector3 GetRandomSafeDeploymentSectorPosition(this Sector sector, float gateDistanceMultiplier, float objectRadius, LayerMask? mask = null)
		{
			if (!mask.HasValue)
			{
				mask = GameController.Instance.NonOVerlappingUnitsMask;
			}
			float num = sector.GetActualGateDistance() * gateDistanceMultiplier;
			Vector3 checkSectorPosition = Maths.RandomXZDirection() * num;
			return PhysicsNonOverlappingPositionFinder.FindSectorPosition(sector, checkSectorPosition, objectRadius, mask.Value);
		}

		public static Vector3? GetRandomSafeDeploymentSectorPositionOrNull(this Sector sector, float gateDistanceMultiplier, float objectRadius, LayerMask? layerMask = null)
		{
			float num = sector.GetActualGateDistance() * gateDistanceMultiplier;
			Vector3 checkSectorPosition = Maths.RandomXZDirection() * num;
			return PhysicsNonOverlappingPositionFinder.FindSectorPositionOrNull(sector, checkSectorPosition, objectRadius, layerMask ?? GameController.Instance.NonOVerlappingUnitsMask);
		}

		public static Vector3? GetRandomSafeDeploymentSectorPositionFromDistanceOrNull(this Sector sector, float gateDistance, float objectRadius, LayerMask? layerMask = null)
		{
			Vector3 checkSectorPosition = Maths.RandomXZDirection() * gateDistance;
			return PhysicsNonOverlappingPositionFinder.FindSectorPositionOrNull(sector, checkSectorPosition, objectRadius, layerMask ?? GameController.Instance.NonOVerlappingUnitsMask);
		}

		public static Vector3 GetRandomSectorPositionWithinGateDistance(this Sector sector)
		{
			float num = sector.GetActualGateDistance() * Random.value * 0.9f;
			return Maths.RandomXZDirection() * num;
		}

		public static Vector3 GetRandomSectorPositionWithinGateDistance(this Sector sector, float gateDistanceMultiplier)
		{
			float num = sector.GetActualGateDistance() * Random.value * gateDistanceMultiplier;
			return Maths.RandomXZDirection() * num;
		}

		public static Vector3 GetRandomSectorPositionWithinBounds(this Sector sector, float distanceMultiplier)
		{
			float num = GameController.Instance.GameSettings.UniverseBoundsSettings.MaxUnitDistanceFromOriginLowerBound * Random.value * distanceMultiplier;
			return Maths.RandomXZDirection() * num;
		}

		public static Vector3 GetRandomSectorPositionOutsideGateDistance(this Sector sector, float minGateDistanceMultiplier = 1.1f)
		{
			float num = 0.95f;
			float num2 = Random.Range(Mathf.Min(num, sector.GateDistanceMultiplier * minGateDistanceMultiplier), num);
			return Maths.RandomXZDirection() * GameController.Instance.GameSettings.UniverseBoundsSettings.MaxUnitDistanceFromOriginLowerBound * num2;
		}

		public static Vector3 GetRandomSectorPositionAtBoundary(this Sector sector, float minMultiplier = 0.85f, float maxMultiplier = 0.95f)
		{
			float num = Random.Range(minMultiplier, maxMultiplier);
			return Maths.RandomXZDirection() * GameController.Instance.GameSettings.UniverseBoundsSettings.MaxUnitDistanceFromOriginLowerBound * num;
		}

		public static Vector3 GetRandomFringeSectorPosition(this Sector sector)
		{
			float num = 0.9f;
			float num2 = Random.Range(Mathf.Min(sector.GateDistanceMultiplier * 1.2f, num), num);
			float num3 = GameController.Instance.GameSettings.UniverseBoundsSettings.MaxUnitDistanceFromOriginLowerBound * num2;
			return Geometry.RandomXZUnitVector() * num3;
		}

		public static Vector3 GetRandomSafeDeploymentSectorPosition(this Sector sector, float gateDistanceMultiplier, float objectRadius, LayerMask layerMask)
		{
			float num = sector.GetActualGateDistance() * gateDistanceMultiplier;
			Vector3 checkSectorPosition = Maths.RandomXZDirection() * num;
			return PhysicsNonOverlappingPositionFinder.FindSectorPosition(sector, checkSectorPosition, objectRadius, layerMask);
		}

		public static Vector3 GetRandomSafeDeploymentSectorPositionFromGateDistance(this Sector sector, float gateDistance, float objectRadius, LayerMask layerMask)
		{
			Vector3 checkSectorPosition = Maths.RandomXZDirection() * gateDistance;
			return PhysicsNonOverlappingPositionFinder.FindSectorPosition(sector, checkSectorPosition, objectRadius, layerMask);
		}

		public static bool HasStationsHostileToFaction(this Sector sector, Faction faction)
		{
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item != null && item.Faction != null && item.Faction.IsHostileToOrAlwaysHostileTo(faction))
					{
						return true;
					}
				}
			}
			return false;
		}

		public static int GetValidShipCount(this Sector sector)
		{
			return sector.GetUnitsByType(UnitType.Ship)?.Count ?? 0;
		}

		public static bool HasBandits(this Sector sector)
		{
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Ship);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item.Faction != null && item.Faction.FactionType == FactionType.Bandit)
					{
						return true;
					}
				}
			}
			List<Unit> unitsByType2 = sector.GetUnitsByType(UnitType.Station);
			if (unitsByType2 != null)
			{
				foreach (Unit item2 in unitsByType2)
				{
					if (item2.Faction != null && item2.Faction.FactionType == FactionType.Bandit)
					{
						return true;
					}
				}
			}
			return false;
		}

		public static int GetUnstableWormholeCount(this Sector sector)
		{
			int num = 0;
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Wormhole);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item != null && item.WormholeComponent.IsUnstable)
					{
						num++;
					}
				}
			}
			return num;
		}

		public static bool IsControlledByPlayer(this Sector sector)
		{
			if (sector.ControllingFaction != null)
			{
				return sector.ControllingFaction.IsPlayerFaction;
			}
			return false;
		}

		public static bool IsExcludedFromPlayerNavigation(this Sector sector)
		{
			if (EngineASX.Instance.LocalFaction != null)
			{
				return EngineASX.Instance.LocalFaction.AutopilotExcludedSectors.Contains(sector.UniqueId);
			}
			return false;
		}

		public static IEnumerable<Unit> GetMajorBanditStations(this Sector sector)
		{
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
			if (unitsByType == null)
			{
				yield break;
			}
			foreach (Unit item in unitsByType)
			{
				if (item.IsMajorBanditStation())
				{
					yield return item;
				}
			}
		}

		public static Unit GetSectorHQ(this Sector sector)
		{
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item != null && item.UnitClass.StationPurpose == StationPurpose.SectorControl)
					{
						return item;
					}
				}
			}
			return null;
		}
	}
}
