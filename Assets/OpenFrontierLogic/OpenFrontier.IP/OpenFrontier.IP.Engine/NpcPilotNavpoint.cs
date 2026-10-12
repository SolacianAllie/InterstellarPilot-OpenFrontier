using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public struct NpcPilotNavpoint
	{
		public Vector3 SectorPosition;

		public bool Arrive;

		public float ThrottleDotThreshold;

		public float ArrivalThreshold;
	}
}
