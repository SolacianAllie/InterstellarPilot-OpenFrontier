using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.TraderHeatmap
{
	public struct TraderHeatmapTarget
	{
		public Sector Sector;

		public Vector3 SectorPosition;

		public float Score;

		public Faction TargetFaction;
	}
}
