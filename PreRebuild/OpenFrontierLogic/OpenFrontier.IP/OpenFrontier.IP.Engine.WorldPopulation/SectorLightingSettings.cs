using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldPopulation
{
	public class SectorLightingSettings : MonoBehaviour
	{
		public float SkyMinSaturation = 0.5f;

		public float SkyMaxSaturation = 1f;

		public float SkyMinValue = 1f;

		public float SkyMaxValue = 1f;

		public float SkyMinExposure = 0.45f;

		public float SkyMaxExposure = 1f;

		public float SkyExposurePower = 0.5f;

		public float SkyMinHue;

		public float SkyMaxHue = 1f;
	}
}
