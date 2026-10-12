using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	public class AIPatrolPathNode : IPatrolPathNode
	{
		[FormerlySerializedAs("position")]
		[SerializeField]
		private Vector3 sectorPosition = Vector3.zero;

		[FormerlySerializedAs("scene")]
		[SerializeField]
		private Sector sector;

		public Sector Sector
		{
			get
			{
				return sector;
			}
			set
			{
				sector = value;
			}
		}

		public Vector3 SectorPosition
		{
			get
			{
				return sectorPosition;
			}
			set
			{
				sectorPosition = value;
			}
		}
	}
}
