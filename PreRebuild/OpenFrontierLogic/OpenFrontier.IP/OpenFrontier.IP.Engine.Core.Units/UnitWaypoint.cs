using UnityEngine;

namespace OpenFrontier.IP.Engine.Core.Units
{
	public class UnitWaypoint : MonoBehaviour
	{
		private Unit unit;

		public PlayerWaypointPath PlayerWaypointPath { get; set; }

		public Unit Unit => unit;

		public void Init()
		{
			unit = GetComponent<Unit>();
		}
	}
}
