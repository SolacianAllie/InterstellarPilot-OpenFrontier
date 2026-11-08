using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class PlayerWaypointController : MonoBehaviour
	{
		public const int CustomWaypointPathId = 0;

		private PlayerWaypointPath customPath;

		public EngineASX Engine;

		private List<PlayerWaypointPath> missionPathCache = new List<PlayerWaypointPath>(16);

		private List<PlayerWaypointPath> activeMissionPaths = new List<PlayerWaypointPath>(16);

		public int MaxMissionPaths = 8;

		private static List<PlayerWaypoint> waypointCache = new List<PlayerWaypoint>();

		public List<PlayerWaypointPath> MissionPaths => activeMissionPaths;

		public List<PlayerWaypointPath> MissionPathCache => missionPathCache;

		public GamePlayer Player => Engine.LocalPlayer;

		public PlayerWaypoint? CustomPathWaypoint
		{
			get
			{
				if (CustomPath != null)
				{
					return CustomPath.Waypoint;
				}
				return null;
			}
		}

		public PlayerWaypointPath CustomPath => customPath;

		public void Init()
		{
			customPath = new PlayerWaypointPath(this)
			{
				IsCustomPath = true,
				UniqueId = 0
			};
			for (int i = 0; i < MaxMissionPaths; i++)
			{
				missionPathCache.Add(new PlayerWaypointPath(this)
				{
					IsCustomPath = false,
					UniqueId = i + 1
				});
			}
		}

		private void Update()
		{
			UpdateMissionPathWaypoints();
			customPath.UpdatePath();
			foreach (PlayerWaypointPath activeMissionPath in activeMissionPaths)
			{
				activeMissionPath.UpdatePath();
			}
		}

		public void UpdateMissionPathWaypoints()
		{
			GamePlayer player = Player;
			RepopulateWaypointCache(player);
			ApplyWaypointsToMissionPaths();
		}

		public void RefreshActiveMissionPaths()
		{
			foreach (PlayerWaypointPath activeMissionPath in activeMissionPaths)
			{
				activeMissionPath.UpdatePath(forcePathRefresh: true);
			}
		}

		public void RefreshAllPaths()
		{
			RefreshActiveMissionPaths();
			RefreshCustomPath();
		}

		private void RefreshCustomPath()
		{
			CustomPath.UpdatePath(forcePathRefresh: true);
		}

		private void ApplyWaypointsToMissionPaths()
		{
			activeMissionPaths.Clear();
			int num = Mathf.Min(missionPathCache.Count, waypointCache.Count);
			for (int i = 0; i < num; i++)
			{
				PlayerWaypointPath playerWaypointPath = missionPathCache[i];
				playerWaypointPath.Waypoint = waypointCache[i];
				activeMissionPaths.Add(playerWaypointPath);
			}
		}

		private void RepopulateWaypointCache(GamePlayer player)
		{
			waypointCache.Clear();
			if (player != null && Player.ActiveMission != null)
			{
				PopulateWaypointCacheFromMission(Player.ActiveMission);
			}
		}

		public void PopulateWaypointCacheFromMission(Mission mission)
		{
			mission.AddWaypointsToList(waypointCache);
		}
	}
}
