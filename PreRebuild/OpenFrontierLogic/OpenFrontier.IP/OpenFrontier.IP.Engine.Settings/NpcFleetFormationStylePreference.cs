using System;
using OpenFrontier.IP.Engine.Fleets.FleetFormations;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Settings
{
	[Serializable]
	public class NpcFleetFormationStylePreference : IWeighted
	{
		[SerializeField]
		private float weight = 1f;

		public FleetFormationStyle FleetFormationStyle;

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
