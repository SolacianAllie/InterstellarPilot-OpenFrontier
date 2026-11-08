using UnityEngine;

namespace OpenFrontier.IP.Engine.Settings
{
	public class VideoSettings : MonoBehaviour
	{
		public FullScreenMode DefaultFullScreenMode;

		public int MinDisplayWidth = 1024;

		public int MinDisplayHeight = 768;

		public bool StaticBatchAsteroids = true;

		public float LaserScaleRate = 0.05f;

		public float StationUnderConstructionMinAlpha = 0.2f;

		public float StationUnderConstructionMaxAlpha = 0.8f;

		public float StationUnderConstructionAlphaFadeRate = 1000f;

		public float CloakMaxAlpha = 0.8f;

		// Runtime cloak visibility floors. The game SERIALIZES these from
		// 'Extracted/GameObject/VideoSettings.prefab' (this class is the
		// MonoBehaviour payload there) - both homes must move together.
		// Full cloak is 100% transparent: 0 = the hull parks and the fold
		// renderer itself turns off, nothing draws at all.
		public float CloakMinAlphaForPlayer = 0f;

		public float CloakMinAlpha = 0f;

		public float CloakAlphaPower = 4f;

		public float VelocityTrailFullAlphaSpeed = 80f;

		public float VelocityTrailMinAlpha = 0.25f;

		public float VelocityTrailMaxAlpha = 0.75f;

		public bool RenderAtmospheresOnDistantPlanets;
	}
}
