using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class StationDistanceCalculator : MonoBehaviour
	{
		public StationDistanceCache StationDistanceCache;

		public PathFinder PathFinder;

		public float[] waypointDistanceCache = new float[50];

		private List<WorldNavpoint> waypointCache = new List<WorldNavpoint>(50);

		public float? GetDistance(Unit unit1, Unit unit2)
		{
			if (unit1 != null && unit2 != null)
			{
				if (unit1 == unit2)
				{
					return 0f;
				}
				float? cachedDistance = StationDistanceCache.GetCachedDistance(unit1, unit2);
				if (cachedDistance.HasValue)
				{
					return cachedDistance.Value;
				}
				float? result = CalculateDistance(unit1, unit2);
				if (result.HasValue)
				{
					StationDistanceCache.CacheDistance(unit1, unit2, result.Value);
					return result;
				}
				if (LogWrapper.LogMsgs)
				{
					Debug.LogErrorFormat(this, "StationDistanceCalculator: Failed to calculate distance from {0} to {1}", unit1, unit2);
				}
			}
			else if (LogWrapper.LogMsgs)
			{
				Debug.LogError("StationDistanceCalculator: Provided with null unit");
			}
			return null;
		}

		public float? CalculateDistance(Unit unit1, Unit unit2)
		{
			return CalculateDistance(unit1.Sector, unit1.SectorPosition, unit2.Sector, unit2.SectorPosition);
		}

		public float? CalculateDistance(Sector startSector, Vector3 startSectorPosition, Sector endSector, Vector3 endSectorPosition)
		{
			waypointCache.Clear();
			if (PathFinder.PathfindIgnoringIntel(startSector, startSectorPosition, endSector, endSectorPosition, waypointCache))
			{
				ExtendDistanceCacheAsNeeded(waypointCache);
				return CalculateWaypointsDistance(ref startSectorPosition, waypointCache, waypointDistanceCache);
			}
			return null;
		}

		public static float CalculateWaypointsDistance(ref Vector3 startSectorPosition, List<WorldNavpoint> waypoints, float[] waypointDistanceCache)
		{
			float num = 0f;
			for (int i = 0; i < waypoints.Count; i++)
			{
				if (i == 0)
				{
					num += (waypointDistanceCache[i] = Vector3.Distance(startSectorPosition, waypoints[i].GetTargetSectorPosition()));
				}
				else if (waypoints[i].GetTargetSector() != waypoints[i - 1].GetTargetSector())
				{
					waypointDistanceCache[i] = 0f;
				}
				else
				{
					num += (waypointDistanceCache[i] = Vector3.Distance(waypoints[i - 1].GetTargetSectorPosition(), waypoints[i].GetTargetSectorPosition()));
				}
			}
			return num;
		}

		private void ExtendDistanceCacheAsNeeded(List<WorldNavpoint> waypoints)
		{
			if (waypoints.Count > waypointDistanceCache.Length)
			{
				waypointDistanceCache = new float[waypoints.Count * 2];
			}
		}
	}
}
