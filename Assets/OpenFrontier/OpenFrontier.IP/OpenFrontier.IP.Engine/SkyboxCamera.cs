using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class SkyboxCamera : MonoBehaviour
	{
		public Skybox Skybox;

		public Renderer SkyOverlayRenderer;

		public void SetSkyOverlayColor(Color color)
		{
			SkyOverlayRenderer.material.color = color;
		}

		public void SetTint(Color color)
		{
		}

		public void SetExposure(float blend)
		{
		}
	}
}
