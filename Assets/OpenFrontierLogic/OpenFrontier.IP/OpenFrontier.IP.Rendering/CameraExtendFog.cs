using UnityEngine;

namespace OpenFrontier.IP.Rendering
{
	public class CameraExtendFog : MonoBehaviour
	{
		private float lastFogStart;

		private float lastFogEnd;

		public float FogStartMultiplier = 4f;

		public float FogEndMultiplier = 4f;

		private void OnPreRender()
		{
			if (RenderSettings.fog)
			{
				lastFogStart = RenderSettings.fogStartDistance;
				lastFogEnd = RenderSettings.fogEndDistance;
				RenderSettings.fogStartDistance = lastFogStart * FogStartMultiplier;
				RenderSettings.fogEndDistance = lastFogEnd * FogEndMultiplier;
			}
		}

		private void OnPostRender()
		{
			if (RenderSettings.fog)
			{
				RenderSettings.fogStartDistance = lastFogStart;
				RenderSettings.fogEndDistance = lastFogEnd;
			}
		}
	}
}
