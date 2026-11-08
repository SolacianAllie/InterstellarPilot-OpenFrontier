using UnityEngine;

namespace OpenFrontier.IP.Engine.Settings
{
	public class BackgroundObjectsSettings : MonoBehaviour
	{
		public bool Enabled;

		public float GeneralBackgroundObjectScale = 0.8f;

		public float BackgroundPlanetScale = 0.2f;

		public float BackgroundPlanetMapDistanceToActualDistance = 1000f;

		public float BackgroundObjectMaxRenderDistance = 250000f;

		public float BackPlanetUnitPositionScaleFactor = 0.01f;

		public bool CreateDistantMoons;

		public bool CreateDistanceAsteroidFields;

		public int BackgroundObjectPlanetMaxJumpDistance = 2;

		public int BackgroundObjectAsteroidClusterMaxJumpDistance = 1;

		public float BackgroundAsteroidClusterScaleMultiplier = 8f;

		public float BackgroundAsteroidClusterRandomYPositionMultiplier = 0.4f;
	}
}
