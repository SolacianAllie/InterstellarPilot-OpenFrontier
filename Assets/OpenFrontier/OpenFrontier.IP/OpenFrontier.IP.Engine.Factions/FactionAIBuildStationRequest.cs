using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public struct FactionAIBuildStationRequest
	{
		public UnitClass UnitClass;

		public Sector Sector;

		public Vector3 SectorPosition;

		public double RequestTime;

		public FactionAIBuildStationRequest(UnitClass unitClass, Sector sector, Vector3 sectorPosition, double scenarioTime)
		{
			this = default;
			UnitClass = unitClass;
			Sector = sector;
			SectorPosition = sectorPosition;
			RequestTime = scenarioTime;
		}
	}
}
