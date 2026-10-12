using UnityEngine;

namespace OpenFrontier.IP.Engine.Fleets.ActiveOrders
{
	public struct UnitSearchSectorItem
	{
		public Sector Sector { get; set; }

		public float DistanceToThisSector { get; set; }

		public Vector3 EntrySectorPosition { get; set; }

		public int JumpDistanceToThisSector { get; set; }

		public UnitSearchSectorItem(Sector sector, float distanceToThisSector, Vector3 entrySectorPosition, int jumpDistanceToThisSector)
		{
			this = default;
			Sector = sector;
			DistanceToThisSector = distanceToThisSector;
			EntrySectorPosition = entrySectorPosition;
			JumpDistanceToThisSector = jumpDistanceToThisSector;
		}
	}
}
