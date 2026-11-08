using System;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	public class MissionManagerWeighting : IWeighted
	{
		public MissionManagerMissionType MissionType = MissionManagerMissionType.DeliverShip;

		[SerializeField]
		private float weight = 1f;

		public float Weight
		{
			get
			{
				return weight;
			}
			set
			{
				weight = value;
			}
		}
	}
}
