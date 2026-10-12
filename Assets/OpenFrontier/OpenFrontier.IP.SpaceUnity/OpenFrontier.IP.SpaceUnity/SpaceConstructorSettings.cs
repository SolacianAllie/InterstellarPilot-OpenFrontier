using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.SpaceUnity
{
	public class SpaceConstructorSettings : MonoBehaviour
	{
		public List<SpaceConstructorNebulaColourSetting> NebulaColourSettings;

		public float MinStarsIntensity = 0.6f;

		public float MaxStarsIntensity = 1.5f;

		public float StarsIntensityPower = 2.4f;

		public int MinNebulaCount = 15;

		public int MaxNebulaCount = 30;

		public float NebulaCountPower = 1f;
	}
}
