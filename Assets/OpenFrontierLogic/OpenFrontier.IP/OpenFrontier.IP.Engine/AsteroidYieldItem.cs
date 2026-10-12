using System;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	public class AsteroidYieldItem : IWeighted
	{
		[SerializeField]
		private CargoClass cargoClass;

		[SerializeField]
		private float weight;

		public CargoClass CargoClass
		{
			get
			{
				return cargoClass;
			}
			set
			{
				cargoClass = value;
			}
		}

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
