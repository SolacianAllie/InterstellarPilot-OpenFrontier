using System.Collections.Generic;
using OpenFrontier.IP.Engine.Pathfinding;
using OpenFrontier.IP.UI;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class PlayerWaypointPath
	{
		public bool AutoRemoveWaypoint;

		private Sector lastKnownPlayerScene;

		private float nextWaypointPathCalc;

		private PlayerWaypoint? waypoint;

		private PlayerWaypointController waypointController;

		private bool waypointHasChanged;

		private List<WorldNavpoint> waypointPath = new List<WorldNavpoint>();

		public float WaypointPathCalculationFrequency = 2f;

		public int UniqueId { get; set; }

		public bool IsCustomPath { get; set; }

		public Color WaypointColor
		{
			get
			{
				if (IsCustomPath)
				{
					return GameController.Instance.GameSettings.ColorSettings.WaypointCustomColor;
				}
				return GameController.Instance.GameSettings.ColorSettings.WaypointMissionColor;
			}
		}

		public PlayerWaypoint? Waypoint
		{
			get
			{
				return waypoint;
			}
			set
			{
				waypoint = value;
				if (!waypoint.HasValue)
				{
					waypointPath.Clear();
				}
				NotifyWaypointChanged();
			}
		}

		public List<WorldNavpoint> Waypoints => waypointPath;

		public bool HasValidPath => HasPath;

		public GamePlayer Player => WaypointController.Player;

		public PlayerWaypointController WaypointController => waypointController;

		public bool HasWaypoint => waypoint.HasValue;

		public bool HasPath
		{
			get
			{
				if (waypoint.HasValue)
				{
					return waypointPath.Count > 0;
				}
				return false;
			}
		}

		public PlayerWaypointPath(PlayerWaypointController waypointController)
		{
			this.waypointController = waypointController;
		}

		public void NotifyWaypointChanged()
		{
			waypointHasChanged = true;
		}

		public bool IsFirstNavpoint(Unit unit)
		{
			if (HasPath)
			{
				return waypointPath[0].TargetSectorObject == unit;
			}
			return false;
		}

		public bool GetFirstWaypoint(ref WorldNavpoint n)
		{
			if (HasPath)
			{
				n = waypointPath[0];
				return true;
			}
			return false;
		}

		public bool LostTrackOfCustomPathTarget(Unit unit)
		{
			float maxAgeOfDiscovery = (unit.IsInActiveSector ? GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTimeActiveSector : GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTime);
			return !waypoint.Value.TargetUnit.IsDiscoveredByFaction(EngineASX.Instance.LocalFaction, maxAgeOfDiscovery);
		}

		public void UpdatePath(bool forcePathRefresh = false)
		{
			if (!(Player != null) || !(Player.Person != null) || !(Player.Sector != null) || !waypoint.HasValue)
			{
				return;
			}
			PlayerWaypoint value = waypoint.Value;
			if (value.IsValid())
			{
				if (IsCustomPath && value.TargetUnit != null && LostTrackOfCustomPathTarget(value.TargetUnit))
				{
					UIController.Instance.QuickMsg.AddMessage("Lost track of target waypoint");
					Waypoint = PlayerWaypoint.FromSectorPosition(value.GetTargetSector(), value.GetTargetSectorPosition());
					AutoRemoveWaypoint = true;
				}
				else if (IsPlayerAtWaypoint() && AutoRemoveWaypoint)
				{
					UIController.Instance.QuickMsg.AddMessage("Waypoint Reached");
					Waypoint = null;
				}
				else if (value.IsMovingTarget && value.TargetUnit.Sector == Player.Sector)
				{
					waypointPath.Clear();
					waypointPath.Add(WorldNavpoint.FromUnit(value.TargetUnit));
					lastKnownPlayerScene = Player.Sector;
				}
				else if (forcePathRefresh || ((Player.Sector != lastKnownPlayerScene || waypointHasChanged || !HasPath || value.IsMovingTarget) && Time.time > nextWaypointPathCalc))
				{
					waypointHasChanged = false;
					lastKnownPlayerScene = Player.Sector;
					PathFind();
					nextWaypointPathCalc = Time.time + WaypointPathCalculationFrequency;
				}
			}
			else
			{
				Waypoint = null;
			}
		}

		private void PathFind()
		{
			waypointPath.Clear();
			PlayerWaypoint value = waypoint.Value;
			if (!AdvPathfinder.Pathfind(Player.Sector, Player.Person.CurrentUnit.SectorPosition, value.GetTargetSector(), value.GetTargetSectorPosition(), EngineASX.Instance.LocalFaction, ignoreHeat: true, waypointPath).IsSuccess)
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log("Could not calculate player waypoint path", Player, 2);
				}
			}
			else
			{
				WorldNavpoint value2 = waypointPath[waypointPath.Count - 1];
				value2.TargetSectorObject = value.TargetUnit;
				waypointPath[waypointPath.Count - 1] = value2;
			}
		}

		private bool IsPlayerAtWaypoint()
		{
			if (waypoint.Value.TargetUnit != null)
			{
				return Unit.GetRoot(Player.Person.CurrentUnit) == Unit.GetRoot(waypoint.Value.TargetUnit);
			}
			return false;
		}
	}
}
