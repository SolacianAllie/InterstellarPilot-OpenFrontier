using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public struct UnitCompassPoint
	{
		public Vector3 WorldPosition;

		public Color Color;

		public Vector3 Scale;

		public PlayerWaypointPath RelatedPath;

		public bool IsMissionWaypoint;

		public bool IsCustomWaypoint;

		public UnitClass UnitClass;

		public bool IsSpeaking;

		public bool IsSelectedTarget;
	}
}
