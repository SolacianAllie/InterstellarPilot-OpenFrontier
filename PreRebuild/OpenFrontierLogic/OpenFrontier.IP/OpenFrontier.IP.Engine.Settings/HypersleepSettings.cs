using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Settings
{
	public class HypersleepSettings : MonoBehaviour
	{
		public float DefaultTimeMultiplier = 4f;

		public float TimeMultiplierChangeRate = 1f;

		public List<HypersleepTimescaleSetting> TimescaleSettings;
	}
}
