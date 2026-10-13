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

		/// <summary>
		/// True only for a missile that has actually acquired the unit the
		/// compass belongs to. UnitType.Projectile is not enough - that is
		/// every missile in the sky, most of which are flying past somebody
		/// else, and flagging those with the pulsing lock icon cried wolf.
		/// </summary>
		public bool IsMissileLockedOnTarget;
	}
}
