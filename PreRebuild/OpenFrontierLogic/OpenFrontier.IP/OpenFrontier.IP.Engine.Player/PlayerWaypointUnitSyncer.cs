using System.Collections.Generic;
using OpenFrontier.IP.Engine.Core.Units;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Player
{
	public class PlayerWaypointUnitSyncer : MonoBehaviour
	{
		public Unit CustomWaypointPrefab;

		public Unit MissionWaypointPrefab;

		public const int MaxWaypointUnits = 8;

		private UnitWaypoint[] allWaypointUnits = new UnitWaypoint[8];

		private List<UnitWaypoint> activeWaypointUnits = new List<UnitWaypoint>();

		private double nextCheckMissionWaypointsTime;

		public double SyncWithWaypointsInterval { get; set; } = 2.0;

		private void Update()
		{
			if (!EngineASX.LoadedAndReady)
			{
				return;
			}
			for (int i = 0; i < activeWaypointUnits.Count; i++)
			{
				UnitWaypoint unitWaypoint = activeWaypointUnits[i];
				if (unitWaypoint == null)
				{
					activeWaypointUnits.RemoveAt(i);
					i--;
				}
				else if (!IsWaypointUnitValid(unitWaypoint))
				{
					ReturnWaypointUnitToPool(unitWaypoint);
					activeWaypointUnits.RemoveAt(i);
					i--;
				}
				else
				{
					UpdateWaypointUnit(unitWaypoint);
				}
			}
			if (Time.realtimeSinceStartupAsDouble > nextCheckMissionWaypointsTime)
			{
				nextCheckMissionWaypointsTime = Time.realtimeSinceStartupAsDouble + SyncWithWaypointsInterval;
				SyncWithPlayerWaypoints();
			}
		}

		private void SyncWithPlayerWaypoints()
		{
			if (!(EngineASX.Instance.LocalPlayer != null))
			{
				return;
			}
			PlayerWaypointController waypointController = EngineASX.Instance.LocalPlayer.WaypointController;
			CheckPath(waypointController.CustomPath);
			foreach (PlayerWaypointPath missionPath in waypointController.MissionPaths)
			{
				CheckPath(missionPath);
			}
		}

		private void CheckPath(PlayerWaypointPath path)
		{
			if (!IsWaypointRequiredForPath(path))
			{
				return;
			}
			UnitWaypoint orCreateUnitForPath = GetOrCreateUnitForPath(path);
			if (orCreateUnitForPath != null)
			{
				orCreateUnitForPath.PlayerWaypointPath = path;
				if (!activeWaypointUnits.Contains(orCreateUnitForPath))
				{
					activeWaypointUnits.Add(orCreateUnitForPath);
				}
			}
		}

		public bool IsWaypointRequiredForPath(PlayerWaypointPath path)
		{
			if (!path.HasWaypoint)
			{
				return false;
			}
			PlayerWaypoint value = path.Waypoint.Value;
			if (value.TargetUnit != null && EngineASX.Instance.LocalFaction.Intel.IsUnitDiscoveredUsingDefaultDiscoveryAge(value.TargetUnit))
			{
				return false;
			}
			return true;
		}

		private UnitWaypoint GetOrCreateUnitForPath(PlayerWaypointPath path)
		{
			if (path.UniqueId > allWaypointUnits.Length)
			{
				return null;
			}
			UnitWaypoint unitWaypoint = allWaypointUnits[path.UniqueId];
			if (unitWaypoint != null)
			{
				return unitWaypoint;
			}
			return CreatUnitForPath(path);
		}

		private UnitWaypoint CreatUnitForPath(PlayerWaypointPath path)
		{
			Unit original = CustomWaypointPrefab;
			if (!path.IsCustomPath)
			{
				original = MissionWaypointPrefab;
			}
			Unit unit = Object.Instantiate(original);
			unit.Init();
			UnitWaypoint component = unit.GetComponent<UnitWaypoint>();
			component.Init();
			component.PlayerWaypointPath = path;
			allWaypointUnits[path.UniqueId] = component;
			return component;
		}

		private void UpdateWaypointUnit(UnitWaypoint waypointunit)
		{
			Unit unit = waypointunit.Unit;
			PlayerWaypoint value = waypointunit.PlayerWaypointPath.Waypoint.Value;
			unit.Sector = value.GetTargetSector();
			unit.transform.localPosition = value.GetTargetSectorPosition();
		}

		private void ReturnWaypointUnitToPool(UnitWaypoint waypointunit)
		{
			waypointunit.Unit.Sector = null;
			waypointunit.PlayerWaypointPath = null;
		}

		private bool IsWaypointUnitValid(UnitWaypoint waypointunit)
		{
			if (waypointunit == null)
			{
				return false;
			}
			if (waypointunit.PlayerWaypointPath == null)
			{
				return false;
			}
			if (!IsWaypointRequiredForPath(waypointunit.PlayerWaypointPath))
			{
				return false;
			}
			return true;
		}
	}
}
