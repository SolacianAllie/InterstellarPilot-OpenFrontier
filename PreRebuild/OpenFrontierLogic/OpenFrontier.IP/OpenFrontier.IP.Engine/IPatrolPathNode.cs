using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public interface IPatrolPathNode
	{
		Vector3 SectorPosition { get; set; }

		Sector Sector { get; }
	}
}
