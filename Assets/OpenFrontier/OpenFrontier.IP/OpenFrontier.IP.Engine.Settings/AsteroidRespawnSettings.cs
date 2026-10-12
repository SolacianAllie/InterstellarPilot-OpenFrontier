using UnityEngine;

namespace OpenFrontier.IP.Engine.Settings
{
	public class AsteroidRespawnSettings : MonoBehaviour
	{
		public double MinTimeBeforeAsteroidRespawnUpper = 21600.0;

		public double MinTimeBeforeAsteroidRespawnLower = 21600.0;

		public float SectorRespawnCooldownTimeLower = 180f;

		public float SectorRespawnCooldownTimeUpper = 18000f;
	}
}
